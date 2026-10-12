using Debug = Game.Shared.FormattedDebug;
using Game.Server.Services;
using Game.Shared.Networking;
using NewCharacterData = Game.Shared.Persistence.NewCharacterData;
using PurrNet;

namespace Game.Server.Networking
{
    /// <summary>
    /// Registers server handlers for account-related endpoint requests.
    /// </summary>
    public static class AccountBindings
    {
        /// <summary>
        /// Registers account request handlers with the shared account endpoint.
        /// </summary>
        public static void Register()
        {
            AccountEndpoint.onCharacterCreationRequest += OnCharacterCreationRequest;
            AccountEndpoint.onEnterWorldRequest += OnEnterWorldRequest;
        }

        private static void OnCharacterCreationRequest(PlayerID sender, NewCharacterData newCharacterData)
        {
            Debug.Log($"Received character creation request from player {sender} for character '{newCharacterData.Name}'");
            var characterCreationService = new CharacterCreationService(sender, newCharacterData);
        }

        private static void OnEnterWorldRequest(PlayerID sender, string characterGuid)
        {
            Debug.Log($"Received enter world request from player {sender} for character ID '{characterGuid}'");
            var enterWorldService = new EnterWorldService(sender, characterGuid);
        }
    }
}
