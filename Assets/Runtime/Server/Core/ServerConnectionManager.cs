using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Server.Persistence;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Transports;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains authoritative connection, account, and session state.</summary>
    public class ServerConnectionManager : PurrMonoBehaviour
    {
        private const double NETWORK_TELEMETRY_INTERVAL_SECONDS = 1d;

        private struct ConnectionTelemetryWindow
        {
            public long PayloadBytes;
            public long MessageCount;
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

        private NetworkManager _networkManager;
        private ITransport _transport;

        private Dictionary<PlayerID, PlayerAccountData> _connectedPlayers = new();
        private Dictionary<PlayerID, PlayerSessionData> _sessionData = new();
        // Current outbound interval only; _transport.connections remains the live connection authority.
        private Dictionary<Connection, ConnectionTelemetryWindow> _outboundTelemetryWindows = new();
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
            _networkManager = null;
            _connectedPlayers.Clear();
            _sessionData.Clear();
            _initialized = false;
        }

        private void Update()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            _networkTelemetryElapsedSeconds += Time.unscaledDeltaTime;
            if (_networkTelemetryElapsedSeconds < NETWORK_TELEMETRY_INTERVAL_SECONDS)
                return;

            double intervalSeconds = _networkTelemetryElapsedSeconds;
            if (_transport != null && _transport.connections.Count > 0)
            {
                foreach (Connection connection in _transport.connections)
                {
                    _outboundTelemetryWindows.TryGetValue(connection, out ConnectionTelemetryWindow window);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_PAYLOAD_BYTES_SENT,
                        window.PayloadBytes,
                        intervalSeconds: intervalSeconds);
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.SERVER_TRANSPORT_CONNECTION_MESSAGE_COUNT_SENT,
                        window.MessageCount,
                        intervalSeconds: intervalSeconds);

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
            RecordLiveConnectionCount();
        }

        private void OnTransportDisconnected(Connection connection, DisconnectReason reason, bool asServer)
        {
            if (asServer)
            {
                _outboundTelemetryWindows.Remove(connection);
                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_DISCONNECTED_COUNT, 1);
                RecordLiveConnectionCount();
            }
        }

        private void OnTransportDataReceived(Connection connection, ByteData data, bool asServer)
        {
            if (asServer && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_PAYLOAD_BYTES_RECEIVED, data.length);
        }

        private void OnTransportDataSent(Connection connection, ByteData data, bool asServer)
        {
            if (!asServer || !PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_PAYLOAD_BYTES_SENT, data.length);

            _outboundTelemetryWindows.TryGetValue(connection, out ConnectionTelemetryWindow window);
            window.PayloadBytes = checked(window.PayloadBytes + (long)data.length);
            window.MessageCount = checked(window.MessageCount + 1L);
            _outboundTelemetryWindows[connection] = window;
        }

        private void RecordLiveConnectionCount()
        {
            if (!PerformanceTelemetry.IsEnabled || _transport == null)
                return;

            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.SERVER_LIVE_CONNECTION_COUNT,
                _transport.connections.Count);
        }

        private void OnPlayerJoined(PlayerID playerID, bool isReconnect, bool asServer)
        {
            if (!asServer)
                return;

            RecordLiveConnectionCount();
            var username = Game.Shared.Networking.LoginAuthenticator.GetNextValidatedUsername();

            if (Game.Server.Persistence.PlayerDatabase.TryGetPlayerAccount(username, out var accountData))
            {
                Debug.Log($" ✓ Player authenticated: {username} (Reconnect: {isReconnect})");
                Debug.Log($" - # of Characters: {accountData.Characters.Count}");

                // Send character list to client
                Game.Shared.Networking.AccountEndpoint.Client_UpdateCharacterList(playerID, accountData.Characters);
            }
            else
            {
                Debug.Error($"Player authenticated with username '{username}' not found in database. This should not happen.");
                return;
            }

            _connectedPlayers[playerID] = accountData;
            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.SERVER_AUTHENTICATED_CLIENT_COUNT,
                    _connectedPlayers.Count);
            }
            Debug.Log($"Player joined: {playerID} ({username})");
        }

        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            if (!asServer)
                return;

            Debug.Log($"Player left: {playerID}");

            // Persist resource state (health, mana, etc.) before cleanup.
            if (_sessionData.TryGetValue(playerID, out var session) &&
                _connectedPlayers.TryGetValue(playerID, out var account) &&
                session.PlayerActor != null)
            {
                var charData = account.Characters.Find(c => c.GUID == session.CharacterID);
                if (charData != null)
                {
                    Persistence.PlayerDatabase.SavePlayerAccount(account);
                }

                Actor playerActor = session.PlayerActor;

                // Unregister from stat tracking before retiring and despawning the player actor.
                if (ServerStatManager.IsInitialized)
                    ServerStatManager.Instance.UnregisterActor(playerActor);

                playerActor.RetireFromServer();

                NetworkIdentity identity = playerActor.GetComponent<NetworkIdentity>();
                if (identity != null)
                    identity.Despawn();
            }

            _connectedPlayers.Remove(playerID);
            _sessionData.Remove(playerID);
            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.SERVER_AUTHENTICATED_CLIENT_COUNT,
                    _connectedPlayers.Count);
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.SERVER_IN_WORLD_CLIENT_COUNT,
                    _sessionData.Count);
            }
            RecordLiveConnectionCount();
        }

        /// <summary>Stores session data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="data">The session data to store.</param>
        public void SetSessionData(PlayerID playerID, PlayerSessionData data)
        {
            _sessionData[playerID] = data;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.SERVER_IN_WORLD_CLIENT_COUNT,
                    _sessionData.Count);
            Debug.Log($"Set session data for player {playerID}: Character GUID {data.CharacterID}");
        }

        /// <summary>Stores account data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="accountData">The account data to store.</param>
        public void SetAccountData(PlayerID playerID, PlayerAccountData accountData)
        {
            if (_connectedPlayers.ContainsKey(playerID))
            {
                _connectedPlayers[playerID] = accountData;
            }
            else
            {
                _connectedPlayers.Add(playerID, accountData);
            }
            Debug.Log($"Set account data for player {playerID}: {accountData.Username}");
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

        /// <summary>Gets account data for a connected player.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <returns>The player's account data, or <see langword="null"/> when absent.</returns>
        public PlayerAccountData GetAccountData(PlayerID playerID)
        {
            if (_connectedPlayers.TryGetValue(playerID, out var accountData))
            {
                return accountData;
            }
            else
            {
                Debug.Error($"Attempted to get account data for player {playerID}, but they are not in the connected players list.");
                return null;
            }
        }
    }
}
