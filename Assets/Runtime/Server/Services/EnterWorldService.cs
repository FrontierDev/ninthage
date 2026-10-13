using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Server.Networking;
using Game.Server.Persistence;
using PurrNet;
using System.IO.Compression;
using UnityEngine;

namespace Game.Server.Services
{
    /// <summary>Validates character ownership and enters the player into the world.</summary>
    public sealed class EnterWorldService
    {
        private PlayerID _playerID;
        private string _characterGUID;

        /// <summary>Validates the selected character and starts player spawning.</summary>
        /// <param name="playerID">The requesting player's network identifier.</param>
        /// <param name="characterGUID">The selected character identifier.</param>
        public EnterWorldService(PlayerID playerID, string characterGUID)
        {
            _playerID = playerID;
            _characterGUID = characterGUID;

            if (!ValidateCharacterGUID())
                return;

            var accountData = ServerConnectionManager.Instance.GetAccountData(_playerID);
            // For simplicity, we'll just send the player to a hardcoded scene.
            // In a real implementation, this could be based on the character's location or other factors.
            ServerConnectionManager.Instance.SetSessionData(_playerID, new PlayerSessionData(_playerID, accountData.Username, _characterGUID));
            ServerSpawnManager.Instance.SpawnPlayer(_playerID);
        }

        private bool ValidateCharacterGUID()
        {
            // Check that the character GUID is valid and belongs to the player.
            if (string.IsNullOrEmpty(_characterGUID))
            {
                Debug.Error($"Player {_playerID} provided an empty character GUID.");
                return false;
            }

            PlayerAccountData accountData = ServerConnectionManager.Instance.GetAccountData(_playerID);
            if (accountData == null)
            {
                Debug.Error($"No account data found for player {_playerID}.");
                return false;
            }

            CharacterData characterData = accountData.Characters.Find(c => c.GUID == _characterGUID);
            if (characterData == null)
            {
                Debug.Error($"Character with GUID {_characterGUID} not found for player {_playerID}.");
                return false;
            }

            return true;
        }
    }
}
