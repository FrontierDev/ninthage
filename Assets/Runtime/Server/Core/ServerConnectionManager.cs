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
            _initialized = true;
        }

        /// <summary>Unsubscribes the manager from NetworkManager events.</summary>
        public override void Unsubscribe(NetworkManager manager, bool asServer)
        {
            if (!asServer)
                return;

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
            _networkManager = null;
            _connectedPlayers.Clear();
            _sessionData.Clear();
            _initialized = false;
        }
        #endregion

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
            if (asServer && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.SERVER_TRANSPORT_PAYLOAD_BYTES_SENT, data.length);
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
