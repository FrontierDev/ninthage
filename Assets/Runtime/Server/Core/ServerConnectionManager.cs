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
        }

        /// <summary>Unsubscribes the manager from NetworkManager events.</summary>
        public override void Unsubscribe(NetworkManager manager, bool asServer)
        {
            if (!asServer)
                return;

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

            string username = Game.Shared.Networking.LoginAuthenticator.GetNextValidatedUsername();
            bool isNewPlayer = Game.Shared.Networking.LoginAuthenticator.GetNextPlayerIsNew();
            StartCoroutine(InitializeAuthenticatedPlayer(playerID, username, isNewPlayer, isReconnect));
        }
        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            if (!asServer)
                return;

            Debug.Log($"Player left: {playerID}");

            Guid characterID = Guid.Empty;
            if (_sessionData.TryGetValue(playerID, out PlayerSessionData session))
            {
                characterID = session.CharacterID;
                Actor playerActor = session.PlayerActor;
                if (playerActor != null)
                {
                    if (ServerStatManager.IsInitialized)
                        ServerStatManager.Instance.UnregisterActor(playerActor);

                    playerActor.RetireFromServer();

                    NetworkIdentity identity = playerActor.GetComponent<NetworkIdentity>();
                    if (identity != null)
                        identity.Despawn();
                }
            }

            _connectedAccounts.Remove(playerID);
            ClearSessionData(playerID);
            if (characterID != Guid.Empty)
                StartCoroutine(ReleaseCharacterAfterDisconnect(characterID));
        }

        private IEnumerator InitializeAuthenticatedPlayer(
            PlayerID playerID,
            string username,
            bool isNewPlayer,
            bool isReconnect)
        {
            if (string.IsNullOrWhiteSpace(username) || username == "Unknown")
            {
                Debug.Error($"Player {playerID} joined without a validated username; authentication failed closed.");
                yield break;
            }

            bool masterProvisioningPending;
            try
            {
                if (!TemporaryCredentialStore.TryGetCredential(
                    username,
                    out _,
                    out _,
                    out masterProvisioningPending))
                {
                    Debug.Error($"No temporary credential exists for authenticated username '{username}'. Authentication failed closed.");
                    yield break;
                }
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not inspect temporary credential for '{username}': {exception.Message}");
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not access the World persistence coordinator for '{username}': {exception.Message}");
                yield break;
            }

            if (coordinator == null)
            {
                Debug.Error($"World persistence coordinator is unavailable for authenticated username '{username}'.");
                yield break;
            }

            Task<MasterPersistenceProtocol.AccountIdentityResponse> resolveTask;
            try
            {
                resolveTask = coordinator.ResolveAccountAsync(username, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not resolve Master account for '{username}': {exception.Message}");
                yield break;
            }

            while (!resolveTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.AccountIdentityResponse accountResponse;
            try
            {
                accountResponse = resolveTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Master account resolution failed for '{username}': {exception.Message}");
                yield break;
            }

            if (accountResponse == null)
            {
                Debug.Error($"Master returned no account response for '{username}'. Authentication failed closed.");
                yield break;
            }

            if (accountResponse.Result == MasterPersistenceProtocol.PersistenceResult.AccountNotFound
                && masterProvisioningPending)
            {
                Task<MasterPersistenceProtocol.AccountIdentityResponse> createTask;
                try
                {
                    createTask = coordinator.CreateAccountAsync(username, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Debug.Error($"Could not create Master account for '{username}': {exception.Message}");
                    yield break;
                }

                while (!createTask.IsCompleted)
                    yield return null;

                try
                {
                    accountResponse = createTask.GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    Debug.Error($"Master account creation failed for '{username}': {exception.Message}");
                    yield break;
                }

                if (accountResponse == null
                    || accountResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    Debug.Error(
                        $"Master account creation for '{username}' returned {accountResponse?.Result}. Authentication failed closed.");
                    yield break;
                }

                try
                {
                    if (!TemporaryCredentialStore.MarkMasterProvisioned(username))
                    {
                        Debug.Error($"Temporary credential '{username}' disappeared while provisioning its Master account.");
                        yield break;
                    }
                }
                catch (Exception exception)
                {
                    Debug.Error($"Could not mark temporary credential '{username}' as provisioned: {exception.Message}");
                    yield break;
                }
            }
            else if (accountResponse.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (isNewPlayer)
                {
                    try
                    {
                        if (!TemporaryCredentialStore.RemoveCredential(username))
                        {
                            Debug.Error($"Temporary credential '{username}' disappeared after a durable account collision.");
                            yield break;
                        }
                        Debug.Warning($"New-player credential '{username}' collided with an existing Master account; authentication failed closed.");
                        yield break;
                    }
                    catch (Exception exception)
                    {
                        Debug.Error($"Could not remove colliding temporary credential '{username}': {exception.Message}");
                        yield break;
                    }
                }
                else if (masterProvisioningPending)
                {
                    try
                    {
                        if (!TemporaryCredentialStore.MarkMasterProvisioned(username))
                        {
                            Debug.Error($"Temporary credential '{username}' disappeared while finalizing Master provisioning.");
                            yield break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.Error($"Could not mark temporary credential '{username}' as provisioned: {exception.Message}");
                        yield break;
                    }
                }
            }
            else
            {
                Debug.Error(
                    $"Master account resolution for '{username}' returned {accountResponse.Result}. Authentication failed closed.");
                yield break;
            }

            if (!accountResponse.AccountID.HasValue
                || !accountResponse.HomeWorldID.HasValue
                || !accountResponse.Revision.HasValue
                || accountResponse.AccountID.Value == Guid.Empty
                || accountResponse.HomeWorldID.Value == Guid.Empty
                || accountResponse.Revision.Value < 1)
            {
                Debug.Error($"Master returned incomplete account identity for '{username}'. Authentication failed closed.");
                yield break;
            }
            if (accountResponse.HomeWorldID.Value != coordinator.WorldID)
            {
                Debug.Error($"Account '{username}' belongs to home World {accountResponse.HomeWorldID.Value}, not this World {coordinator.WorldID}.");
                yield break;
            }

            Task<MasterPersistenceProtocol.CharacterRosterResponse> rosterTask;
            try
            {
                rosterTask = coordinator.GetCharacterRosterAsync(accountResponse.AccountID.Value, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not load the Master roster for '{username}': {exception.Message}");
                yield break;
            }

            while (!rosterTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.CharacterRosterResponse rosterResponse;
            try
            {
                rosterResponse = rosterTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Master roster loading failed for '{username}': {exception.Message}");
                yield break;
            }

            if (rosterResponse == null
                || rosterResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error($"Master roster loading for '{username}' returned {rosterResponse?.Result}. Authentication failed closed.");
                yield break;
            }

            List<Game.Shared.Persistence.CharacterData> roster;
            try
            {
                roster = CharacterPersistenceProjection.ToActiveRoster(rosterResponse);
            }
            catch (Exception exception)
            {
                Debug.Error($"Master roster projection failed for '{username}': {exception.Message}");
                yield break;
            }

            if (!IsPlayerConnected(playerID))
            {
                Debug.Warning($"Player {playerID} disconnected before Master account publication.");
                yield break;
            }

            _connectedAccounts[playerID] = new ConnectedAccountContext(
                username,
                accountResponse.AccountID.Value,
                accountResponse.HomeWorldID.Value,
                accountResponse.Revision.Value);
            Game.Shared.Networking.AccountEndpoint.Client_UpdateCharacterList(playerID, roster);
            Debug.Log($"Player authenticated: {username} (AccountID: {accountResponse.AccountID.Value}, Reconnect: {isReconnect})");
        }

        private IEnumerator ReleaseCharacterAfterDisconnect(Guid characterID)
        {
            WorldPersistenceCoordinator coordinator;
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
