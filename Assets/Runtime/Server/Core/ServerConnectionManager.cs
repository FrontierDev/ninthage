using UnityEngine;
using PurrNet;
using Debug = Game.Shared.FormattedDebug;
using System.Collections;
using System.Collections.Generic;
using Game.Server.Networking;

namespace Game.Server
{
    public class ServerConnectionManager : PurrMonoBehaviour
    {
        private static ServerConnectionManager _instance;
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
        public static bool Initialized => _initialized;

        private NetworkManager networkManager;

        private Dictionary<PlayerID, PlayerAccountData> connectedPlayers = new Dictionary<PlayerID, PlayerAccountData>();
        private Dictionary<PlayerID, PlayerSessionData> sessionData = new Dictionary<PlayerID, PlayerSessionData>();

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
            if (asServer)
            {
                manager.onPlayerJoined += OnPlayerJoined;
                manager.onPlayerLeft += OnPlayerLeft;
                _initialized = true;
            }
        }

        public override void Unsubscribe(NetworkManager manager, bool asServer)
        {
            if (asServer)
            {
                manager.onPlayerJoined -= OnPlayerJoined;
                manager.onPlayerLeft -= OnPlayerLeft;
                _initialized = false;
            }
        }
        #endregion

        private void OnPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
        {
            var username = Game.Shared.Authentication.LoginAuthenticator.GetNextValidatedUsername();

            if (Game.Server.Persistence.PlayerDatabase.TryGetPlayerAccount(username, out var accountData))
            {
                Debug.Log($" ✓ Player authenticated: {username} (Reconnect: {isReconnect})");
                Debug.Log($" - # of Characters: {accountData.Characters.Count}");

                // Send character list to client
                Game.Shared.Networking.AccountService.Client_UpdateCharacterList(playerId, accountData.Characters);
            }
            else
            {
                Debug.Error($"Player authenticated with username '{username}' not found in database. This should not happen.");
                return;
            }

            connectedPlayers[playerId] = accountData;
            Debug.Log($"Player joined: {playerId} ({username})");
        }

        private void OnPlayerLeft(PlayerID playerId, bool asServer)
        {
            Debug.Log($"Player left: {playerId}");

            // Persist resource state (health, mana, etc.) before cleanup.
            if (sessionData.TryGetValue(playerId, out var session) &&
                connectedPlayers.TryGetValue(playerId, out var account) &&
                session.PlayerActor != null)
            {
                var charData = account.Characters.Find(c => c.Guid == session.CharacterId);
                if (charData != null)
                {
                    Persistence.PlayerDatabase.SavePlayerAccount(account);
                }

                // Unregister from stat tracking and despawn the player actor.
                if (ServerStatManager.IsInitialized)
                    ServerStatManager.Instance.UnregisterActor(session.PlayerActor);

                var identity = session.PlayerActor.GetComponent<PurrNet.NetworkIdentity>();
                if (identity != null)
                    identity.Despawn();
            }

            connectedPlayers.Remove(playerId);
            sessionData.Remove(playerId);
        }

        public void SetSessionData(PlayerID playerId, PlayerSessionData data)
        {
            sessionData[playerId] = data;
            Debug.Log($"Set session data for player {playerId}: Character GUID {data.CharacterId}");
        }

        public void SetAccountData(PlayerID playerId, PlayerAccountData accountData)
        {
            if (connectedPlayers.ContainsKey(playerId))
            {
                connectedPlayers[playerId] = accountData;
            }
            else
            {
                connectedPlayers.Add(playerId, accountData);
            }
            Debug.Log($"Set account data for player {playerId}: {accountData.Username}");
        }

        public PlayerSessionData GetSessionData(PlayerID playerId)
        {
            if (sessionData.TryGetValue(playerId, out var data))
            {
                return data;
            }
            else
            {
                Debug.Error($"Attempted to get session data for player {playerId}, but no session data found.");
                return null;
            }
        }

        public PlayerAccountData GetAccountData(PlayerID playerId)
        {
            if (connectedPlayers.TryGetValue(playerId, out var accountData))
            {
                return accountData;
            }
            else
            {
                Debug.Error($"Attempted to get account data for player {playerId}, but they are not in the connected players list.");
                return null;
            }
        }
    }
}