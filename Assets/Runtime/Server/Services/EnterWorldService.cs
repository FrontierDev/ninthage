using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using PurrNet;
using System.IO.Compression;
using UnityEngine;

namespace Game.Server.Services
{
    /// <summary>Validates character ownership and enters the player into the world.</summary>
    public sealed class EnterWorldService
    {
        private PlayerID playerId;
        private string characterGuid;

        /// <summary>Validates the selected character and starts player spawning.</summary>
        /// <param name="_playerId">The requesting player's network identifier.</param>
        /// <param name="_characterGuid">The selected character identifier.</param>
        public EnterWorldService(PlayerID _playerId, string _characterGuid)
        {
            playerId = _playerId;
            characterGuid = _characterGuid;

            if (!ValidateCharacterGUID())
                return;

            var accountData = ServerConnectionManager.Instance.GetAccountData(playerId);
            // For simplicity, we'll just send the player to a hardcoded scene.
            // In a real implementation, this could be based on the character's location or other factors.
            ServerConnectionManager.Instance.SetSessionData(playerId, new PlayerSessionData(playerId, accountData.Username, characterGuid));
            ServerSpawnManager.Instance.SpawnPlayer(playerId);
        }

        private bool ValidateCharacterGUID()
        {
            // Check that the character GUID is valid and belongs to the player.
            if (string.IsNullOrEmpty(characterGuid))
            {
                Debug.Error($"Player {playerId} provided an empty character GUID.");
                return false;
            }

            PlayerAccountData accountData = ServerConnectionManager.Instance.GetAccountData(playerId);
            if (accountData == null)
            {
                Debug.Error($"No account data found for player {playerId}.");
                return false;
            }

            CharacterData characterData = accountData.Characters.Find(c => c.GUID == characterGuid);
            if (characterData == null)
            {
                Debug.Error($"Character with GUID {characterGuid} not found for player {playerId}.");
                return false;
            }

            return true;
        }
    }
}
