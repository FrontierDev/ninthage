using PurrNet;
using Debug = Game.Shared.FormattedDebug;
using CharacterData = Game.Shared.Persistence.CharacterData;
using Game.Server.Networking;
using UnityEngine;
using System.IO.Compression;


namespace Game.Server.Services
{
    public sealed class EnterWorldService
    {
        private PlayerID playerId;
        private string characterGuid;

        public EnterWorldService(PlayerID _playerId, string _characterGuid)
        {
            playerId = _playerId;
            characterGuid = _characterGuid;

            var accountData = ServerConnectionManager.Instance.GetAccountData(playerId);
            var characterData = accountData.Characters.Find(c => c.Guid == characterGuid);

            if (ValidateCharacterGUID())
            {
                // For simplicity, we'll just send the player to a hardcoded scene. 
                // In a real implementation, this could be based on the character's location or other factors.
                ServerConnectionManager.Instance.SetSessionData(playerId, new PlayerSessionData(playerId, accountData.Username, characterGuid));
                ServerSpawnManager.Instance.SpawnPlayer(playerId);
            }
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

            CharacterData characterData = accountData.Characters.Find(c => c.Guid == characterGuid);
            if (characterData == null)
            {
                Debug.Error($"Character with GUID {characterGuid} not found for player {playerId}.");
                return false;
            }

            return true;
        }
    }
}