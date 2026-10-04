using Debug = Game.Shared.FormattedDebug;
using NewCharacterData = Game.Shared.Persistence.NewCharacterData;
using PurrNet;
using Game.Server.Services;

namespace Game.Server.Networking
{
    public static class AccountServiceHooks
    {
        public static void RegisterHooks()
        {
            Game.Shared.Networking.AccountService.onCharacterCreationRequest += OnCharacterCreationRequest;
            Game.Shared.Networking.AccountService.onEnterWorldRequest += OnEnterWorldRequest;
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