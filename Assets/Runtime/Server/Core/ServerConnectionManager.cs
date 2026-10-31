using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains authoritative connection, account, and session state.</summary>
    public partial class ServerConnectionManager : PurrMonoBehaviour, INetworkPressureProvider
    {
        private const double NETWORK_TELEMETRY_INTERVAL_SECONDS = 1d;
        private const double NETWORK_PRESSURE_PAYLOAD_BYTES_PER_SECOND = 16384d;
        private const double NETWORK_PRESSURE_MESSAGES_PER_SECOND = 256d;
        private const int NETWORK_PRESSURE_ENTER_INTERVALS = 3;
        private const double NETWORK_PRESSURE_RECOVERY_PAYLOAD_BYTES_PER_SECOND = 12288d;
        private const double NETWORK_PRESSURE_RECOVERY_MESSAGES_PER_SECOND = 192d;
        private const int NETWORK_PRESSURE_RECOVERY_INTERVALS = 5;
        private const float CHECKPOINT_INTERVAL_SECONDS = 30f;

        private struct ConnectionTelemetryWindow
        {
            public long PayloadBytes;
            public long MessageCount;
        }

        private struct ConnectionPressureTracker
        {
            public bool IsPressured;
            public int ConsecutiveOverThresholdIntervals;
            public int ConsecutiveRecoveryIntervals;
        }

        private static ServerConnectionManager _instance;
        /// <summary>Gets the active server connection manager.</summary>
        public static ServerConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServerConnectionManager();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        private bool _acceptingCharacterControlPlane = true;
        /// <summary>Gets whether the connection manager is initialized.</summary>
        public static bool Initialized => _initialized;

        /// <summary>Gets whether ordinary character-control requests may begin.</summary>
        internal bool IsAcceptingCharacterControlPlane => _acceptingCharacterControlPlane;

        /// <summary>Gets whether one or more live transport connections are under outbound rate pressure.</summary>
        public bool HasPressuredConnections => PressuredConnectionCount > 0;

        /// <summary>Gets the number of live transport connections currently under outbound rate pressure.</summary>
        public int PressuredConnectionCount => _pressuredConnectionCount;

        private NetworkManager _networkManager;
        private ITransport _transport;
        private Coroutine _periodicCheckpointCoroutine;

        private Dictionary<PlayerID, ConnectedAccountContext> _connectedAccounts = new();
        private readonly Dictionary<Guid, PlayerSessionData> _admittedSessionsByGameplaySessionID = new();
        private readonly Dictionary<PlayerID, Guid> _gameplaySessionIDsByPlayerID = new();
        // Current outbound interval only; _transport.connections remains the live connection authority.
        private Dictionary<Connection, ConnectionTelemetryWindow> _outboundTelemetryWindows = new();
        private Dictionary<Connection, ConnectionPressureTracker> _connectionPressureTrackers = new();
        private int _pressuredConnectionCount;
        private double _networkTelemetryElapsedSeconds;
        private bool _udpStatisticsEnabledByTelemetry;
        private bool _hasUdpStatisticsBaseline;
        private long _udpNativeBytesSentBaseline;
        private long _udpNativePacketsSentBaseline;
        private long _udpPacketLossBaseline;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;
        }

        /// <summary>
        /// Subscribes to NetworkManager events when running as server. Automatically handled by PurrNet.
        /// </summary>
        public override void Subscribe(NetworkManager manager, bool asServer)
        {
            if (!asServer || _initialized)
                return;

            _acceptingCharacterControlPlane = true;
            _networkManager = manager;
            _transport = manager.rawTransport;
            NetworkPressureQuery.Register(this);
            if (_transport != null)
            {
                _transport.onConnected += OnTransportConnected;
                _transport.onDisconnected += OnTransportDisconnected;
                _transport.onDataReceived += OnTransportDataReceived;
                _transport.onDataSent += OnTransportDataSent;
            }
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
            PerformanceTelemetry.SessionStarted += OnPerformanceSessionStarted;
            PerformanceTelemetry.SessionEnded += OnPerformanceSessionEnded;
            if (PerformanceTelemetry.IsEnabled)
            {
                ResetOutboundTelemetryWindow();
                BeginUdpStatisticsTelemetry();
            }
            _initialized = true;
            BeginLogoutEligibilitySubscription();
            _periodicCheckpointCoroutine = StartCoroutine(RunPeriodicCharacterCheckpoints());
        }

        /// <summary>Unsubscribes the manager from NetworkManager events.</summary>
        public override void Unsubscribe(NetworkManager manager, bool asServer)
        {
            if (!asServer)
                return;

            StopPeriodicCharacterCheckpoints();
            NetworkPressureQuery.Unregister(this);
            EndUdpStatisticsTelemetry();
            if (_transport != null)
            {
                _transport.onConnected -= OnTransportConnected;
                _transport.onDisconnected -= OnTransportDisconnected;
                _transport.onDataReceived -= OnTransportDataReceived;
                _transport.onDataSent -= OnTransportDataSent;
                _transport = null;
            }
            manager.onPlayerJoined -= OnPlayerJoined;
            manager.onPlayerLeft -= OnPlayerLeft;
            PerformanceTelemetry.SessionStarted -= OnPerformanceSessionStarted;
            PerformanceTelemetry.SessionEnded -= OnPerformanceSessionEnded;
            UnsubscribeFromLogoutEligibility();
            ResetOutboundTelemetryWindow();
            ClearConnectionPressureState();
            _networkManager = null;
            _connectedAccounts.Clear();
            _admittedSessionsByGameplaySessionID.Clear();
            _gameplaySessionIDsByPlayerID.Clear();
            _initialized = false;
        }

        /// <summary>Stops new ordinary character-control requests while preserving cleanup work.</summary>
        internal void StopAcceptingCharacterControlPlane()
        {
            _acceptingCharacterControlPlane = false;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            _networkTelemetryElapsedSeconds += Time.unscaledDeltaTime;
            if (_networkTelemetryElapsedSeconds < NETWORK_TELEMETRY_INTERVAL_SECONDS)
                return;

            double intervalSeconds = _networkTelemetryElapsedSeconds;
            if (double.IsNaN(intervalSeconds)
                || double.IsInfinity(intervalSeconds)
                || intervalSeconds <= 0d)
            {
                throw new InvalidOperationException(
                    "The network telemetry interval must be finite and greater than zero.");
            }

            long aggregatePayloadBytes = 0L;
            long aggregateMessageCount = 0L;
            if (_transport != null && _transport.connections.Count > 0)
            {
                foreach (Connection connection in _transport.connections)
                {
                    _outboundTelemetryWindows.TryGetValue(connection, out ConnectionTelemetryWindow window);
                    aggregatePayloadBytes = checked(aggregatePayloadBytes + window.PayloadBytes);
                    aggregateMessageCount = checked(aggregateMessageCount + window.MessageCount);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT,
                        window.PayloadBytes,
                        intervalSeconds: intervalSeconds);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT,
                        window.MessageCount,
                        intervalSeconds: intervalSeconds);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_PER_SECOND,
                        window.PayloadBytes / intervalSeconds,
                        intervalSeconds: intervalSeconds);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_MESSAGES_PER_SECOND,
                        window.MessageCount / intervalSeconds,
                        intervalSeconds: intervalSeconds);

                    UpdateConnectionPressure(connection, window, intervalSeconds);

                    if (_transport.measuresRoundTripTime)
                    {
                        int roundTripTimeMilliseconds = _transport.GetRoundTripTime(connection, true);
                        if (roundTripTimeMilliseconds >= 0)
                        {
                            PerformanceTelemetry.TryRecord(
                                NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_ROUND_TRIP_TIME,
                                roundTripTimeMilliseconds,
                                intervalSeconds: intervalSeconds);
                        }
                    }
                }
            }

            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_AGGREGATE_PAYLOAD_BYTES_PER_SECOND,
                aggregatePayloadBytes / intervalSeconds,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_AGGREGATE_MESSAGES_PER_SECOND,
                aggregateMessageCount / intervalSeconds,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_LIVE_CONNECTION_COUNT,
                _transport?.connections.Count ?? 0,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_AUTHENTICATED_CLIENT_COUNT,
                _connectedAccounts.Count,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_IN_WORLD_CLIENT_COUNT,
                _admittedSessionsByGameplaySessionID.Count,
                intervalSeconds: intervalSeconds);

            if (PerformanceTelemetry.IsEnabled)
                RecordUdpStatisticsInterval(intervalSeconds);

            ResetOutboundTelemetryWindow();
        }
        #endregion

        private void OnPerformanceSessionStarted()
        {
            ResetOutboundTelemetryWindow();
            BeginUdpStatisticsTelemetry();
        }

        private void OnPerformanceSessionEnded()
        {
            EndUdpStatisticsTelemetry();
            ResetOutboundTelemetryWindow();
        }

        private void ResetOutboundTelemetryWindow()
        {
            _outboundTelemetryWindows.Clear();
            _networkTelemetryElapsedSeconds = 0d;
        }

        private void UpdateConnectionPressure(
            Connection connection,
            ConnectionTelemetryWindow window,
            double intervalSeconds)
        {
            _connectionPressureTrackers.TryGetValue(connection, out ConnectionPressureTracker tracker);
            double payloadBytesPerSecond = window.PayloadBytes / intervalSeconds;
            double messagesPerSecond = window.MessageCount / intervalSeconds;
            if (!tracker.IsPressured)
            {
                if (payloadBytesPerSecond >= NETWORK_PRESSURE_PAYLOAD_BYTES_PER_SECOND
                    || messagesPerSecond >= NETWORK_PRESSURE_MESSAGES_PER_SECOND)
                {
                    tracker.ConsecutiveOverThresholdIntervals++;
                    if (tracker.ConsecutiveOverThresholdIntervals >= NETWORK_PRESSURE_ENTER_INTERVALS)
                    {
                        tracker.IsPressured = true;
                        tracker.ConsecutiveRecoveryIntervals = 0;
                        _pressuredConnectionCount++;
                        Debug.Warning(
                            $"Connection {connection} entered provisional Stage 1.5 outbound rate pressure "
                            + $"({payloadBytesPerSecond} payload bytes/sec, {messagesPerSecond} messages/sec). "
                            + "No authoritative traffic was dropped.");
                    }
                }
                else
                {
                    tracker.ConsecutiveOverThresholdIntervals = 0;
                }
            }
            else if (payloadBytesPerSecond < NETWORK_PRESSURE_RECOVERY_PAYLOAD_BYTES_PER_SECOND
                && messagesPerSecond < NETWORK_PRESSURE_RECOVERY_MESSAGES_PER_SECOND)
            {
                tracker.ConsecutiveRecoveryIntervals++;
                if (tracker.ConsecutiveRecoveryIntervals >= NETWORK_PRESSURE_RECOVERY_INTERVALS)
                {
                    tracker.IsPressured = false;
                    tracker.ConsecutiveOverThresholdIntervals = 0;
                    tracker.ConsecutiveRecoveryIntervals = 0;
                    _pressuredConnectionCount--;
                    Debug.Log(
                        $"Connection {connection} recovered from provisional Stage 1.5 outbound rate pressure "
                        + $"({payloadBytesPerSecond} payload bytes/sec, {messagesPerSecond} messages/sec). "
                        + "No authoritative traffic was dropped.");
                }
            }
            else
            {
                tracker.ConsecutiveRecoveryIntervals = 0;
            }

            _connectionPressureTrackers[connection] = tracker;
        }

        private void ClearConnectionPressureState()
        {
            _connectionPressureTrackers.Clear();
            _pressuredConnectionCount = 0;
        }

        private void BeginUdpStatisticsTelemetry()
        {
            if (_transport is not UDPTransport udpTransport)
            {
                ClearUdpStatisticsState();
                return;
            }

            if (!udpTransport.statisticsEnabled)
            {
                udpTransport.SetStatisticsEnabled(true);
                _udpStatisticsEnabledByTelemetry = true;
            }
            else if (!_hasUdpStatisticsBaseline)
            {
                _udpStatisticsEnabledByTelemetry = false;
            }

            CaptureUdpStatisticsBaseline(udpTransport);
        }

        private void EndUdpStatisticsTelemetry()
        {
            if (_transport is UDPTransport udpTransport && _udpStatisticsEnabledByTelemetry)
                udpTransport.SetStatisticsEnabled(false);

            ClearUdpStatisticsState();
        }

        private void RecordUdpStatisticsInterval(double intervalSeconds)
        {
            if (_transport is not UDPTransport udpTransport)
                return;

            if (!_hasUdpStatisticsBaseline)
            {
                CaptureUdpStatisticsBaseline(udpTransport);
                return;
            }

            long nativeBytesSent = udpTransport.nativeBytesSent;
            long nativePacketsSent = udpTransport.nativePacketsSent;
            long packetLoss = udpTransport.nativePacketLoss;
            if (nativeBytesSent < _udpNativeBytesSentBaseline
                || nativePacketsSent < _udpNativePacketsSentBaseline
                || packetLoss < _udpPacketLossBaseline)
            {
                CaptureUdpStatisticsBaseline(udpTransport);
                return;
            }

            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_NATIVE_BYTES_SENT,
                nativeBytesSent - _udpNativeBytesSentBaseline,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_NATIVE_PACKETS_SENT,
                nativePacketsSent - _udpNativePacketsSentBaseline,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_PACKET_LOSS_COUNT,
                packetLoss - _udpPacketLossBaseline,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_NATIVE_BYTES_PER_SECOND,
                (nativeBytesSent - _udpNativeBytesSentBaseline) / intervalSeconds,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_NATIVE_PACKETS_PER_SECOND,
                (nativePacketsSent - _udpNativePacketsSentBaseline) / intervalSeconds,
                intervalSeconds: intervalSeconds);
            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_TRANSPORT_UDP_PACKET_LOSS_EVENTS_PER_SECOND,
                (packetLoss - _udpPacketLossBaseline) / intervalSeconds,
                intervalSeconds: intervalSeconds);

            CaptureUdpStatisticsBaseline(udpTransport);
        }

        private void CaptureUdpStatisticsBaseline(UDPTransport udpTransport)
        {
            _udpNativeBytesSentBaseline = udpTransport.nativeBytesSent;
            _udpNativePacketsSentBaseline = udpTransport.nativePacketsSent;
            _udpPacketLossBaseline = udpTransport.nativePacketLoss;
            _hasUdpStatisticsBaseline = true;
        }

        private void ClearUdpStatisticsState()
        {
            _udpStatisticsEnabledByTelemetry = false;
            _hasUdpStatisticsBaseline = false;
            _udpNativeBytesSentBaseline = 0L;
            _udpNativePacketsSentBaseline = 0L;
            _udpPacketLossBaseline = 0L;
        }

        private void OnTransportConnected(Connection connection, bool asServer)
        {
            if (!asServer)
                return;

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTED_COUNT, 1);
        }

        private void OnTransportDisconnected(Connection connection, DisconnectReason reason, bool asServer)
        {
            if (asServer)
            {
                _outboundTelemetryWindows.Remove(connection);
                if (_connectionPressureTrackers.TryGetValue(connection, out ConnectionPressureTracker tracker)
                    && tracker.IsPressured)
                {
                    _pressuredConnectionCount--;
                }
                _connectionPressureTrackers.Remove(connection);
                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_DISCONNECTED_COUNT, 1);
            }
        }

        private void OnTransportDataReceived(Connection connection, ByteData data, bool asServer)
        {
            if (asServer && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED, data.length);
        }

        private void OnTransportDataSent(Connection connection, ByteData data, bool asServer)
        {
            if (!asServer)
                return;

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_PAYLOAD_BYTES_SENT, data.length);

            _outboundTelemetryWindows.TryGetValue(connection, out ConnectionTelemetryWindow window);
            window.PayloadBytes = checked(window.PayloadBytes + (long)data.length);
            window.MessageCount = checked(window.MessageCount + 1L);
            _outboundTelemetryWindows[connection] = window;
        }

        /// <summary>Gets whether the supplied live transport connection is currently under outbound rate pressure.</summary>
        public bool IsConnectionPressured(Connection connection)
        {
            return _connectionPressureTrackers.TryGetValue(connection, out ConnectionPressureTracker tracker)
                && tracker.IsPressured;
        }

        /// <summary>Gets whether the supplied player currently maps to a pressured transport connection.</summary>
        public bool IsPlayerPressured(PlayerID playerID)
        {
            if (_networkManager == null)
                return false;

            var playerModule = _networkManager.playerModule;
            if (playerModule == null || !playerModule.TryGetConnection(playerID, out Connection connection))
                return false;

            return IsConnectionPressured(connection);
        }
        private void OnPlayerJoined(PlayerID playerID, bool isReconnect, bool asServer)
        {
            if (!asServer)
                return;

            if (_networkManager == null || _networkManager.playerModule == null)
            {
                Debug.Error($"Player {playerID} joined before the server network manager was ready; authentication failed closed.");
                return;
            }

            if (!_networkManager.playerModule.TryGetConnection(playerID, out Connection connection))
            {
                Debug.Error($"Could not resolve a live connection for joined player {playerID}; authentication failed closed.");
                return;
            }

            if (!Game.Shared.Networking.LoginAuthenticator.TryTakeAuthenticatedSession(
                    connection,
                    out AuthenticatedSessionContext authenticatedSession)
                || authenticatedSession == null)
            {
                Debug.Error($"Player {playerID} joined without an authenticated session for its exact connection; authentication failed closed.");
                return;
            }

            StartCoroutine(InitializeAuthenticatedPlayer(playerID, authenticatedSession, isReconnect));
        }
        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            if (!asServer)
                return;

            Debug.Log($"Player left: {playerID}");

            if (!TryGetSessionData(playerID, out PlayerSessionData session)
                || session == null)
            {
                _connectedAccounts.TryGetValue(playerID, out ConnectedAccountContext accountContext);
                _connectedAccounts.Remove(playerID);
                if (accountContext != null)
                    StartCoroutine(DisconnectPublishedGameplaySession(accountContext.GameplaySessionID));
                return;
            }

            Guid gameplaySessionID = session.GameplaySessionID;
            PlayerSessionData.SessionLifecycleState previousState = session.LifecycleState;
            CancelSessionDeadline(session);
            DetachPlayerFromAdmittedSession(playerID, session);
            _connectedAccounts.Remove(playerID);
            StartCoroutine(DisconnectPublishedGameplaySession(gameplaySessionID));

            if (previousState == PlayerSessionData.SessionLifecycleState.Finalizing
                || previousState == PlayerSessionData.SessionLifecycleState.Unsettled)
            {
                return;
            }

            StartDisconnectGrace(session);
        }

        private IEnumerator InitializeAuthenticatedPlayer(
            PlayerID playerID,
            AuthenticatedSessionContext authenticatedSession,
            bool isReconnect)
        {
            if (authenticatedSession == null)
            {
                Debug.Error($"Player {playerID} joined without an authenticated session context; authentication failed closed.");
                yield break;
            }

            if (!authenticatedSession.WasReclaimed && authenticatedSession.SelectedCharacterID.HasValue)
            {
                Debug.Warning(
                    $"Player {playerID} authenticated with selected character {authenticatedSession.SelectedCharacterID.Value}; "
                    + "a created gameplay session cannot carry a selected character.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            if (authenticatedSession.WasReclaimed && authenticatedSession.SelectedCharacterID.HasValue)
            {
                yield return InitializeReclaimedPlayer(playerID, authenticatedSession);
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            bool persistenceCoordinatorUnavailable = false;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not access the World persistence coordinator for '{authenticatedSession.Username}': {exception.Message}");
                coordinator = null;
                persistenceCoordinatorUnavailable = true;
            }

            if (persistenceCoordinatorUnavailable)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            if (coordinator == null)
            {
                Debug.Error(
                    $"World persistence coordinator is unavailable for authenticated username '{authenticatedSession.Username}'.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            Task<MasterSessionProtocol.CharacterRosterResponse> rosterTask;
            bool rosterRequestFailed = false;
            try
            {
                rosterTask = coordinator.GetCharacterRosterAsync(
                    authenticatedSession.GameplaySessionID,
                    authenticatedSession.AccountID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not load the Master roster for '{authenticatedSession.Username}': {exception.Message}");
                rosterTask = null;
                rosterRequestFailed = true;
            }

            if (rosterRequestFailed || rosterTask == null)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            while (!rosterTask.IsCompleted)
                yield return null;

            MasterSessionProtocol.CharacterRosterResponse sessionRosterResponse;
            bool rosterLoadingFailed = false;
            try
            {
                sessionRosterResponse = rosterTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Master roster loading failed for '{authenticatedSession.Username}': {exception.Message}");
                sessionRosterResponse = null;
                rosterLoadingFailed = true;
            }

            if (rosterLoadingFailed)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            if (sessionRosterResponse == null
                || sessionRosterResponse.SessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted
                || sessionRosterResponse.PersistenceResponse == null
                || sessionRosterResponse.PersistenceResponse.Result
                    != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error(
                    $"Master roster loading for '{authenticatedSession.Username}' returned session result "
                    + $"{sessionRosterResponse?.SessionResult} and persistence result "
                    + $"{sessionRosterResponse?.PersistenceResponse?.Result}. "
                    + "Authentication failed closed.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            List<Game.Shared.Persistence.CharacterData> roster;
            bool rosterProjectionFailed = false;
            try
            {
                roster = CharacterPersistenceProjection.ToActiveRoster(sessionRosterResponse.PersistenceResponse);
            }
            catch (Exception exception)
            {
                Debug.Error($"Master roster projection failed for '{authenticatedSession.Username}': {exception.Message}");
                roster = null;
                rosterProjectionFailed = true;
            }

            if (rosterProjectionFailed)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            if (!IsPlayerConnected(playerID))
            {
                Debug.Warning($"Player {playerID} disconnected before Master account publication.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            ConnectedAccountContext accountContext;
            bool accountContextCreationFailed = false;
            try
            {
                accountContext = new ConnectedAccountContext(
                    authenticatedSession.Username,
                    authenticatedSession.GameplaySessionID,
                    authenticatedSession.AccountID,
                    authenticatedSession.HomeWorldID,
                    authenticatedSession.Revision,
                    authenticatedSession.WasReclaimed,
                    authenticatedSession.SelectedCharacterID);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Could not publish the authenticated account context for '{authenticatedSession.Username}': {exception.Message}");
                accountContext = null;
                accountContextCreationFailed = true;
            }

            if (accountContextCreationFailed)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            _connectedAccounts[playerID] = accountContext;
            Game.Shared.Networking.AccountEndpoint.Client_UpdateCharacterList(playerID, roster);
            Debug.Log(
                $"Player authenticated: {authenticatedSession.Username} "
                + $"(AccountID: {authenticatedSession.AccountID}, Reconnect: {isReconnect})");
        }

        private IEnumerator DisconnectUnpublishedGameplaySession(Guid gameplaySessionID)
        {
            yield return DisconnectGameplaySession(gameplaySessionID, "unpublished");
        }

        private IEnumerator DisconnectPublishedGameplaySession(Guid gameplaySessionID)
        {
            yield return DisconnectGameplaySession(gameplaySessionID, "published");
        }

        private IEnumerator DisconnectGameplaySession(Guid gameplaySessionID, string sessionStateDescription)
        {
            Task<MasterSessionProtocol.SessionResponse> disconnectTask;
            try
            {
                disconnectTask = ServerInitialization.SessionCoordinator.DisconnectAsync(
                    gameplaySessionID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Could not disconnect {sessionStateDescription} gameplay session {gameplaySessionID}: {exception.Message}");
                yield break;
            }

            if (disconnectTask == null)
            {
                Debug.Error(
                    $"Master returned no disconnect task for {sessionStateDescription} gameplay session {gameplaySessionID}.");
                yield break;
            }

            while (!disconnectTask.IsCompleted)
                yield return null;

            try
            {
                MasterSessionProtocol.SessionResponse response = disconnectTask.GetAwaiter().GetResult();
                if (response == null)
                {
                    Debug.Error(
                        $"Master returned no response while disconnecting {sessionStateDescription} gameplay session {gameplaySessionID}.");
                    yield break;
                }

                if (response.Result != MasterSessionProtocol.GameplaySessionResult.Accepted
                    && response.Result != MasterSessionProtocol.GameplaySessionResult.StaleSession)
                {
                    Debug.Error(
                        $"Master gameplay-session disconnect for {sessionStateDescription} session {gameplaySessionID} "
                        + $"returned {response.Result}; cleanup was not accepted.");
                }
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Master gameplay-session disconnect for {sessionStateDescription} session {gameplaySessionID} failed: {exception.Message}");
            }
        }

        private bool TryCaptureAndQueueFinalCheckpoint(
            PlayerSessionData session,
            out string failureReason)
        {
            failureReason = null;

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                failureReason = $"persistence coordinator is unavailable: {exception.Message}";
                return false;
            }

            if (coordinator == null)
            {
                failureReason = "persistence coordinator is unavailable.";
                return false;
            }

            ServerTimeAuthority timeAuthority = ServerInitialization.TimeAuthority;
            if (timeAuthority == null)
            {
                failureReason = "server time authority is unavailable.";
                return false;
            }

            DateTimeOffset capturedAtUtc;
            try
            {
                capturedAtUtc = timeAuthority.ReadObservation().UtcInstant;
            }
            catch (Exception exception)
            {
                failureReason = $"server UTC observation failed: {exception.Message}";
                coordinator.MarkCheckpointFailure(
                    session.CharacterID,
                    MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                return false;
            }

            WorldPersistenceCheckpoint checkpoint;
            try
            {
                if (!CharacterLiveCheckpointCapture.TryCapture(
                    session,
                    capturedAtUtc,
                    out checkpoint,
                    out failureReason))
                {
                    coordinator.MarkCheckpointFailure(
                        session.CharacterID,
                        MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                    return false;
                }
            }
            catch (Exception exception)
            {
                failureReason = $"live checkpoint capture threw: {exception.Message}";
                coordinator.MarkCheckpointFailure(
                    session.CharacterID,
                    MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                return false;
            }

            MasterPersistenceProtocol.PersistenceResult result;
            try
            {
                result = coordinator.QueueCharacterCheckpoint(session.CharacterID, checkpoint);
            }
            catch (Exception exception)
            {
                failureReason = $"checkpoint queue threw: {exception.Message}";
                coordinator.MarkCheckpointFailure(
                    session.CharacterID,
                    MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                return false;
            }

            if (result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                failureReason = $"checkpoint queue returned {result}.";
                coordinator.MarkCheckpointFailure(session.CharacterID, result);
                return false;
            }

            return true;
        }

        /// <summary>Captures and queues one synchronous checkpoint for each currently active player session.</summary>
        internal void CaptureAndQueueOnlineCharacterCheckpoints()
        {
            if (!_initialized || ServerInitialization.IsFatalShutdownRequested)
                return;

            ServerTimeAuthority timeAuthority = ServerInitialization.TimeAuthority;
            if (timeAuthority == null)
            {
                Debug.Warning("Periodic character checkpoint capture skipped because time authority is unavailable.");
                return;
            }

            DateTimeOffset capturedAtUtc;
            try
            {
                capturedAtUtc = timeAuthority.ReadObservation().UtcInstant;
            }
            catch (Exception exception)
            {
                Debug.Warning($"Periodic character checkpoint capture could not read server UTC: {exception.Message}");
                return;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Warning($"Periodic character checkpoint capture could not access persistence: {exception.Message}");
                return;
            }

            if (coordinator == null)
            {
                Debug.Warning("Periodic character checkpoint capture skipped because persistence is unavailable.");
                return;
            }

            bool finalShutdownCapture = ServerShutdownController.IsShutdownRequested;
            List<PlayerSessionData> sessionSnapshot =
                new(_admittedSessionsByGameplaySessionID.Values);
            sessionSnapshot.Sort(
                (first, second) =>
                {
                    Guid firstCharacterID = first?.CharacterID ?? Guid.Empty;
                    Guid secondCharacterID = second?.CharacterID ?? Guid.Empty;
                    return firstCharacterID.CompareTo(secondCharacterID);
                });

            int rejectedCount = 0;
            string firstRejection = null;
            foreach (PlayerSessionData session in sessionSnapshot)
            {
                if (!_initialized
                    || ServerInitialization.IsFatalShutdownRequested
                    || session == null
                    || session.LifecycleState == PlayerSessionData.SessionLifecycleState.Finalizing
                    || session.PlayerActor == null
                    || session.AccountID == Guid.Empty
                    || session.CharacterID == Guid.Empty
                    || !ServerActorManager.IsInitialized
                    || !ServerActorManager.Instance.TryGetActor(session.PlayerActor.ID, out Actor registeredActor)
                    || !ReferenceEquals(registeredActor, session.PlayerActor))
                {
                    continue;
                }

                WorldPersistenceCheckpoint checkpoint;
                string failureReason;
                try
                {
                    if (!CharacterLiveCheckpointCapture.TryCapture(
                        session,
                        capturedAtUtc,
                        out checkpoint,
                        out failureReason))
                    {
                        if (finalShutdownCapture)
                        {
                            coordinator.MarkCheckpointFailure(
                                session.CharacterID,
                                MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                        }
                        rejectedCount++;
                        firstRejection ??= $"character {session.CharacterID}: {failureReason}";
                        continue;
                    }
                }
                catch (Exception exception)
                {
                    if (finalShutdownCapture)
                    {
                        coordinator.MarkCheckpointFailure(
                            session.CharacterID,
                            MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                    }
                    rejectedCount++;
                    firstRejection ??= $"character {session.CharacterID}: capture threw {exception.Message}";
                    continue;
                }

                try
                {
                    MasterPersistenceProtocol.PersistenceResult result =
                        coordinator.QueueCharacterCheckpoint(session.CharacterID, checkpoint);
                    if (result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                    {
                        if (finalShutdownCapture)
                        {
                            coordinator.MarkCheckpointFailure(session.CharacterID, result);
                        }
                        rejectedCount++;
                        firstRejection ??= $"character {session.CharacterID}: queue returned {result}";
                    }
                }
                catch (Exception exception)
                {
                    if (finalShutdownCapture)
                    {
                        coordinator.MarkCheckpointFailure(
                            session.CharacterID,
                            MasterPersistenceProtocol.PersistenceResult.InvalidRequest);
                    }
                    rejectedCount++;
                    firstRejection ??= $"character {session.CharacterID}: queue threw {exception.Message}";
                }
            }

            if (rejectedCount > 0)
            {
                Debug.Warning(
                    $"Periodic character checkpoint cycle rejected {rejectedCount} capture(s). "
                    + firstRejection);
            }
        }

        /// <summary>Stops the main-thread periodic checkpoint scheduler.</summary>
        internal void StopPeriodicCharacterCheckpoints()
        {
            if (_periodicCheckpointCoroutine == null)
                return;

            StopCoroutine(_periodicCheckpointCoroutine);
            _periodicCheckpointCoroutine = null;
        }

        private IEnumerator RunPeriodicCharacterCheckpoints()
        {
            while (_initialized && !ServerInitialization.IsFatalShutdownRequested)
            {
                yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                if (!_initialized || ServerInitialization.IsFatalShutdownRequested)
                    break;

                try
                {
                    CaptureAndQueueOnlineCharacterCheckpoints();
                }
                catch (Exception exception)
                {
                    Debug.Warning($"Periodic character checkpoint capture failed: {exception.Message}");
                }

                WorldPersistenceCoordinator coordinator;
                try
                {
                    coordinator = ServerInitialization.PersistenceCoordinator;
                }
                catch (Exception exception)
                {
                    Debug.Warning($"Periodic character checkpoint flush skipped: {exception.Message}");
                    continue;
                }

                if (coordinator == null)
                {
                    Debug.Warning("Periodic character checkpoint flush skipped because persistence is unavailable.");
                    continue;
                }

                Task flushTask;
                try
                {
                    flushTask = coordinator.FlushPendingCheckpointsAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Debug.Warning($"Periodic character checkpoint flush could not start: {exception.Message}");
                    continue;
                }

                while (!flushTask.IsCompleted)
                    yield return null;

                try
                {
                    flushTask.GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    Debug.Warning($"Periodic character checkpoint flush failed: {exception.Message}");
                }
            }

            _periodicCheckpointCoroutine = null;
        }

        /// <summary>Stores session data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="data">The session data to store.</param>
        public void SetSessionData(PlayerID playerID, PlayerSessionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.GameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(data));
            if (data.PlayerID != playerID)
                throw new ArgumentException("The session data player does not match the supplied Player ID.", nameof(data));
            if (_gameplaySessionIDsByPlayerID.ContainsKey(playerID))
                throw new InvalidOperationException("The Player ID already has an admitted gameplay session.");
            if (_admittedSessionsByGameplaySessionID.ContainsKey(data.GameplaySessionID))
            {
                throw new InvalidOperationException(
                    "The gameplay session ID already has an admitted session.");
            }

            _admittedSessionsByGameplaySessionID.Add(data.GameplaySessionID, data);
            _gameplaySessionIDsByPlayerID.Add(playerID, data.GameplaySessionID);
            Debug.Log(
                $"Set session data for GameplaySessionID {data.GameplaySessionID}: Character GUID {data.CharacterID}");
        }

        /// <summary>Gets the Master-owned account context for a connected player.</summary>
        internal bool TryGetAccountContext(PlayerID playerID, out ConnectedAccountContext accountContext)
        {
            return _connectedAccounts.TryGetValue(playerID, out accountContext);
        }

        /// <summary>Gets whether the transport still reports a live connection for the player.</summary>
        internal bool IsPlayerConnected(PlayerID playerID)
        {
            return _networkManager != null
                && _networkManager.playerModule != null
                && _networkManager.playerModule.TryGetConnection(playerID, out _);
        }

        /// <summary>Clears the local world-entry session for a player.</summary>
        internal void ClearSessionData(PlayerID playerID)
        {
            if (!_gameplaySessionIDsByPlayerID.TryGetValue(playerID, out Guid gameplaySessionID))
                return;

            _gameplaySessionIDsByPlayerID.Remove(playerID);
            if (!_admittedSessionsByGameplaySessionID.Remove(gameplaySessionID))
            {
                throw new InvalidOperationException(
                    "The Player ID attachment had no corresponding admitted gameplay session.");
            }
        }

        /// <summary>Gets an admitted session by its Master-owned gameplay-session identity.</summary>
        /// <param name="gameplaySessionID">The gameplay-session identity to resolve.</param>
        /// <param name="session">The admitted session, when present.</param>
        /// <returns><see langword="true"/> when the gameplay session is admitted.</returns>
        internal bool TryGetAdmittedSession(Guid gameplaySessionID, out PlayerSessionData session)
        {
            if (gameplaySessionID == Guid.Empty)
            {
                session = null;
                return false;
            }

            return _admittedSessionsByGameplaySessionID.TryGetValue(gameplaySessionID, out session);
        }

        private void DetachPlayerFromAdmittedSession(
            PlayerID playerID,
            PlayerSessionData session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.GameplaySessionID == Guid.Empty)
                throw new InvalidOperationException("An admitted session must have a gameplay session ID.");
            if (!session.HasPlayerAttachment)
                throw new InvalidOperationException("The admitted session has no live Player ID attachment.");
            if (session.PlayerID != playerID)
                throw new InvalidOperationException("The supplied Player ID is not attached to the admitted session.");
            if (!_gameplaySessionIDsByPlayerID.TryGetValue(playerID, out Guid gameplaySessionID)
                || gameplaySessionID != session.GameplaySessionID)
            {
                throw new InvalidOperationException(
                    "The admitted session Player ID attachment does not match the primary session identity.");
            }

            _gameplaySessionIDsByPlayerID.Remove(playerID);
            session.HasPlayerAttachment = false;

            if (session.PlayerActor != null
                && ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(playerID, out Actor registeredActor)
                && ReferenceEquals(registeredActor, session.PlayerActor))
            {
                ChunkSubscriptions.UnregisterPlayerActor(playerID);
            }
        }

        private void AttachPlayerToAdmittedSession(
            PlayerID playerID,
            PlayerSessionData session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.GameplaySessionID == Guid.Empty)
                throw new InvalidOperationException("An admitted session must have a gameplay session ID.");
            if (session.HasPlayerAttachment)
                throw new InvalidOperationException("The admitted session already has a live Player ID attachment.");
            if (_gameplaySessionIDsByPlayerID.ContainsKey(playerID))
                throw new InvalidOperationException("The new Player ID already has an admitted gameplay session.");

            foreach (Guid attachedGameplaySessionID in _gameplaySessionIDsByPlayerID.Values)
            {
                if (attachedGameplaySessionID == session.GameplaySessionID)
                {
                    throw new InvalidOperationException(
                        "The admitted gameplay session is already attached to another Player ID.");
                }
            }

            if (session.PlayerActor == null)
                throw new InvalidOperationException("A live actor is required to attach an admitted session.");
            if (!_admittedSessionsByGameplaySessionID.TryGetValue(
                    session.GameplaySessionID,
                    out PlayerSessionData admittedSession)
                || !ReferenceEquals(admittedSession, session))
            {
                throw new InvalidOperationException("The admitted session is not present in the primary session registry.");
            }

            session.PlayerID = playerID;
            session.HasPlayerAttachment = true;
            _gameplaySessionIDsByPlayerID.Add(playerID, session.GameplaySessionID);
            ChunkSubscriptions.RegisterPlayerActor(playerID, session.PlayerActor);
        }

        private void RemoveAdmittedSession(Guid gameplaySessionID)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (!_admittedSessionsByGameplaySessionID.TryGetValue(
                    gameplaySessionID,
                    out PlayerSessionData session)
                || session == null)
            {
                throw new InvalidOperationException(
                    $"The gameplay session {gameplaySessionID} was not admitted.");
            }

            if (_gameplaySessionIDsByPlayerID.TryGetValue(
                    session.PlayerID,
                    out Guid attachedGameplaySessionID)
                && attachedGameplaySessionID == gameplaySessionID)
            {
                _gameplaySessionIDsByPlayerID.Remove(session.PlayerID);
            }

            foreach (KeyValuePair<PlayerID, Guid> attachment in _gameplaySessionIDsByPlayerID)
            {
                if (attachment.Value == gameplaySessionID
                    && attachment.Key != session.PlayerID)
                {
                    throw new InvalidOperationException(
                        $"Gameplay session {gameplaySessionID} is attached to an unexpected Player ID.");
                }
            }

            if (session.DeadlineRegistration.IsValid)
                session.DeadlineRegistration.Cancel();
            session.DeadlineRegistration = default;
            session.DeadlineTickID = 0;

            if (!_admittedSessionsByGameplaySessionID.Remove(gameplaySessionID))
            {
                throw new InvalidOperationException(
                    $"The admitted gameplay session {gameplaySessionID} disappeared during removal.");
            }
        }

        private bool TryGetSessionData(PlayerID playerID, out PlayerSessionData session)
        {
            if (!_gameplaySessionIDsByPlayerID.TryGetValue(playerID, out Guid gameplaySessionID))
            {
                session = null;
                return false;
            }

            if (!_admittedSessionsByGameplaySessionID.TryGetValue(gameplaySessionID, out session))
            {
                throw new InvalidOperationException(
                    "The Player ID attachment had no corresponding admitted gameplay session.");
            }

            return true;
        }

        /// <summary>Gets session data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <returns>The player's session data, or <see langword="null"/> when absent.</returns>
        public PlayerSessionData GetSessionData(PlayerID playerID)
        {
            if (TryGetSessionData(playerID, out PlayerSessionData session))
                return session;

            Debug.Error($"Attempted to get session data for player {playerID}, but no session data found.");
            return null;
        }

    }
}
