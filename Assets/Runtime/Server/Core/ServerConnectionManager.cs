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
    public class ServerConnectionManager : PurrMonoBehaviour, INetworkPressureProvider
    {
        private const double NETWORK_TELEMETRY_INTERVAL_SECONDS = 1d;
        private const double NETWORK_PRESSURE_PAYLOAD_BYTES_PER_SECOND = 16384d;
        private const double NETWORK_PRESSURE_MESSAGES_PER_SECOND = 256d;
        private const int NETWORK_PRESSURE_ENTER_INTERVALS = 3;
        private const double NETWORK_PRESSURE_RECOVERY_PAYLOAD_BYTES_PER_SECOND = 12288d;
        private const double NETWORK_PRESSURE_RECOVERY_MESSAGES_PER_SECOND = 192d;
        private const int NETWORK_PRESSURE_RECOVERY_INTERVALS = 5;
        private const float CHECKPOINT_INTERVAL_SECONDS = 30f;
        private const int FINAL_CHECKPOINT_FLUSH_ATTEMPTS = 4;

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
        /// <summary>Gets whether the connection manager is initialized.</summary>
        public static bool Initialized => _initialized;

        /// <summary>Gets whether one or more live transport connections are under outbound rate pressure.</summary>
        public bool HasPressuredConnections => PressuredConnectionCount > 0;

        /// <summary>Gets the number of live transport connections currently under outbound rate pressure.</summary>
        public int PressuredConnectionCount => _pressuredConnectionCount;

        private NetworkManager _networkManager;
        private ITransport _transport;
        private Coroutine _periodicCheckpointCoroutine;

        private Dictionary<PlayerID, ConnectedAccountContext> _connectedAccounts = new();
        private Dictionary<PlayerID, PlayerSessionData> _sessionData = new();
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
            ResetOutboundTelemetryWindow();
            ClearConnectionPressureState();
            _networkManager = null;
            _connectedAccounts.Clear();
            _sessionData.Clear();
            _initialized = false;
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
                _sessionData.Count,
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

            Guid characterID = Guid.Empty;
            bool requiresFinalCheckpoint = false;
            bool finalCheckpointQueuedSuccessfully = true;
            if (_sessionData.TryGetValue(playerID, out PlayerSessionData session))
            {
                characterID = session.CharacterID;
                Actor playerActor = session.PlayerActor;
                if (playerActor != null)
                {
                    requiresFinalCheckpoint = true;
                    finalCheckpointQueuedSuccessfully = TryCaptureAndQueueFinalCheckpoint(
                        session,
                        out string finalCheckpointFailureReason);
                    if (!finalCheckpointQueuedSuccessfully)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} could not be captured or queued: "
                            + finalCheckpointFailureReason);
                    }

                    if (ServerStatManager.IsInitialized)
                        ServerStatManager.Instance.UnregisterActor(playerActor);

                    playerActor.RetireFromServer();

                    NetworkIdentity identity = playerActor.GetComponent<NetworkIdentity>();
                    if (identity != null)
                        identity.Despawn();
                }
            }

            _connectedAccounts.TryGetValue(playerID, out ConnectedAccountContext accountContext);
            _connectedAccounts.Remove(playerID);
            ClearSessionData(playerID);
            if (accountContext != null)
                StartCoroutine(DisconnectPublishedGameplaySession(accountContext.GameplaySessionID));

            if (characterID != Guid.Empty)
            {
                if (requiresFinalCheckpoint && !finalCheckpointQueuedSuccessfully)
                {
                    Debug.Error(
                        $"Character lease {characterID} will not be released because its final checkpoint "
                        + "was not acknowledged for queueing.");
                }
                else
                {
                    StartCoroutine(ReleaseCharacterAfterDisconnect(
                        characterID,
                        requiresFinalCheckpoint));
                }
            }
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

            if (authenticatedSession.SelectedCharacterID.HasValue)
            {
                Debug.Warning(
                    $"Player {playerID} authenticated with selected character {authenticatedSession.SelectedCharacterID.Value}; "
                    + "selected-character reclaim requires the later reconnect flow.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
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

            Task<MasterPersistenceProtocol.CharacterRosterResponse> rosterTask;
            bool rosterRequestFailed = false;
            try
            {
                rosterTask = coordinator.GetCharacterRosterAsync(
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

            MasterPersistenceProtocol.CharacterRosterResponse rosterResponse;
            bool rosterLoadingFailed = false;
            try
            {
                rosterResponse = rosterTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Master roster loading failed for '{authenticatedSession.Username}': {exception.Message}");
                rosterResponse = null;
                rosterLoadingFailed = true;
            }

            if (rosterLoadingFailed)
            {
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            if (rosterResponse == null
                || rosterResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error(
                    $"Master roster loading for '{authenticatedSession.Username}' returned {rosterResponse?.Result}. "
                    + "Authentication failed closed.");
                yield return DisconnectUnpublishedGameplaySession(authenticatedSession.GameplaySessionID);
                yield break;
            }

            List<Game.Shared.Persistence.CharacterData> roster;
            bool rosterProjectionFailed = false;
            try
            {
                roster = CharacterPersistenceProjection.ToActiveRoster(rosterResponse);
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

        private IEnumerator ReleaseCharacterAfterDisconnect(
            Guid characterID,
            bool requiresFinalCheckpoint)
        {
            WorldPersistenceCoordinator coordinator;
            bool finalCheckpointQueuedSuccessfully = !requiresFinalCheckpoint;
            int finalCheckpointFlushAttempts = 0;
            while (requiresFinalCheckpoint && !finalCheckpointQueuedSuccessfully)
            {
                if (IsDisconnectReleaseStopping())
                    yield break;

                finalCheckpointFlushAttempts++;

                coordinator = null;
                bool coordinatorAcquired = false;
                try
                {
                    coordinator = ServerInitialization.PersistenceCoordinator;
                    coordinatorAcquired = true;
                }
                catch (Exception exception)
                {
                    Debug.Warning(
                        $"Final checkpoint release for character {characterID} is waiting for persistence: "
                        + exception.Message);
                }

                if (!coordinatorAcquired)
                {
                    if (finalCheckpointFlushAttempts >= FINAL_CHECKPOINT_FLUSH_ATTEMPTS)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} remains unsettled because persistence "
                            + "is unavailable; the newest pending checkpoint is retained and the lease will not be released.");
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                    continue;
                }

                if (coordinator == null)
                {
                    Debug.Warning(
                        $"Final checkpoint release for character {characterID} is waiting for persistence.");
                    if (finalCheckpointFlushAttempts >= FINAL_CHECKPOINT_FLUSH_ATTEMPTS)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} remains unsettled because persistence "
                            + "is unavailable; the newest pending checkpoint is retained and the lease will not be released.");
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                    continue;
                }

                Task flushTask;
                bool flushStarted = false;
                try
                {
                    flushTask = coordinator.FlushPendingCheckpointsAsync(CancellationToken.None);
                    flushStarted = true;
                }
                catch (Exception exception)
                {
                    Debug.Warning(
                        $"Final checkpoint flush for character {characterID} could not start: "
                        + exception.Message);
                    flushTask = null;
                }

                if (!flushStarted)
                {
                    if (finalCheckpointFlushAttempts >= FINAL_CHECKPOINT_FLUSH_ATTEMPTS)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} remains unsettled because its flush could not start; "
                            + "the newest pending checkpoint is retained and the lease will not be released.");
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                    continue;
                }

                while (!flushTask.IsCompleted)
                    yield return null;

                bool flushSucceeded = false;
                try
                {
                    flushTask.GetAwaiter().GetResult();
                    flushSucceeded = true;
                }
                catch (Exception exception)
                {
                    Debug.Warning(
                        $"Final checkpoint flush for character {characterID} failed: "
                        + exception.Message);
                }

                if (!flushSucceeded)
                {
                    if (finalCheckpointFlushAttempts >= FINAL_CHECKPOINT_FLUSH_ATTEMPTS)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} remains unsettled after "
                            + $"{finalCheckpointFlushAttempts} flush attempts; the newest pending checkpoint is retained "
                            + "and the lease will not be released.");
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                    continue;
                }

                if (!coordinator.TryGetPendingCheckpointState(
                    characterID,
                    out bool hasPendingCheckpoint,
                    out MasterPersistenceProtocol.PersistenceResult? blockingResult))
                {
                    Debug.Error(
                        $"Final checkpoint state for character {characterID} is unavailable; "
                        + "the lease will not be released.");
                    yield break;
                }

                if (blockingResult.HasValue)
                {
                    Debug.Error(
                        $"Final checkpoint for character {characterID} is permanently blocked with result "
                        + $"{blockingResult.Value}; the lease will not be released.");
                    yield break;
                }

                if (hasPendingCheckpoint)
                {
                    if (finalCheckpointFlushAttempts >= FINAL_CHECKPOINT_FLUSH_ATTEMPTS)
                    {
                        Debug.Error(
                            $"Final checkpoint for character {characterID} remains unsettled after "
                            + $"{finalCheckpointFlushAttempts} flush attempts; the newest pending checkpoint is retained "
                            + "and the lease will not be released.");
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(CHECKPOINT_INTERVAL_SECONDS);
                    continue;
                }

                finalCheckpointQueuedSuccessfully = true;
            }

            if (IsDisconnectReleaseStopping())
                yield break;

            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Warning($"Could not access persistence coordinator while releasing character {characterID}: {exception.Message}");
                yield break;
            }

            if (coordinator == null)
            {
                Debug.Warning($"Persistence coordinator unavailable while releasing character {characterID}; continuing disconnect cleanup.");
                yield break;
            }

            Task<MasterPersistenceProtocol.PersistenceResponse> releaseTask;
            try
            {
                releaseTask = coordinator.ReleaseCharacterAsync(characterID, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Warning($"Could not start persistence release for character {characterID}: {exception.Message}");
                yield break;
            }

            while (!releaseTask.IsCompleted)
                yield return null;

            try
            {
                MasterPersistenceProtocol.PersistenceResponse response = releaseTask.GetAwaiter().GetResult();
                if (response == null || response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                    Debug.Warning($"Persistence release for character {characterID} returned {response?.Result}; no retry will be attempted.");
            }
            catch (Exception exception)
            {
                Debug.Warning($"Persistence release failed for character {characterID}: {exception.Message}");
            }
        }

        private static bool IsDisconnectReleaseStopping()
        {
            return ServerInitialization.IsFatalShutdownRequested
                || ServerShutdownController.IsShutdownRequested;
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
            List<KeyValuePair<PlayerID, PlayerSessionData>> sessionSnapshot =
                new(_sessionData);
            sessionSnapshot.Sort(
                (first, second) =>
                {
                    Guid firstCharacterID = first.Value?.CharacterID ?? Guid.Empty;
                    Guid secondCharacterID = second.Value?.CharacterID ?? Guid.Empty;
                    return firstCharacterID.CompareTo(secondCharacterID);
                });

            int rejectedCount = 0;
            string firstRejection = null;
            foreach (KeyValuePair<PlayerID, PlayerSessionData> entry in sessionSnapshot)
            {
                PlayerSessionData session = entry.Value;
                if (!_initialized
                    || ServerInitialization.IsFatalShutdownRequested
                    || session == null
                    || session.PlayerActor == null
                    || session.AccountID == Guid.Empty
                    || session.CharacterID == Guid.Empty
                    || !_sessionData.TryGetValue(entry.Key, out PlayerSessionData liveSession)
                    || liveSession != session
                    || !IsPlayerConnected(entry.Key))
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
            _sessionData[playerID] = data;
            Debug.Log($"Set session data for player {playerID}: Character GUID {data.CharacterID}");
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
            _sessionData.Remove(playerID);
        }

        /// <summary>Gets session data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <returns>The player's session data, or <see langword="null"/> when absent.</returns>
        public PlayerSessionData GetSessionData(PlayerID playerID)
        {
            if (_sessionData.TryGetValue(playerID, out var data))
            {
                return data;
            }
            else
            {
                Debug.Error($"Attempted to get session data for player {playerID}, but no session data found.");
                return null;
            }
        }

    }
}
