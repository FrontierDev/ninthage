using System;
using System.Collections.Generic;
using PurrNet;
using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Handles player account-related RPCs, such as sending character 
    /// lists to clients upon successful authentication. 
    /// This class is shared between server and client, 
    /// but currently only contains server-to-client RPCs. 
    /// Future expansions may include client-to-server RPCs for account management actions.
    /// </summary>
    public static class AccountService
    {
        // Client callbacks.
        public static Action<List<CharacterData>> onCharacterListReceived;
        public static Action<CharacterData, string> onEnteringWorld;
        public static Action<Actor> onPlayerActorAssigned;
        public static Action onEnteredWorld;

        // Server callbacks
        public static Action<PlayerID, Game.Shared.Persistence.NewCharacterData> onCharacterCreationRequest;
        public static Action onCharacterCreated;
        public static Action<PlayerID, string> onEnterWorldRequest;

        [TargetRpc]
        public static void Client_UpdateCharacterList(PlayerID target, List<CharacterData> characterList, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received character list with {characterList.Count} characters.");
            onCharacterListReceived?.Invoke(characterList);
        }

        [TargetRpc]
        public static void Client_SetPlayerActor(PlayerID target, NetworkIdentity actorIdentity, RPCInfo rpcInfo = default)
        {
            var actor = actorIdentity.GetComponent<Actor>();
            onPlayerActorAssigned?.Invoke(actor);
        }

        [ServerRpc]
        public static void Server_TryCreateCharacter(Game.Shared.Persistence.NewCharacterData newCharacterData, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received request to create character '{newCharacterData.Name}' from player {rpcInfo.sender}.");
            onCharacterCreationRequest?.Invoke(rpcInfo.sender, newCharacterData);
        }

        [TargetRpc]
        public static void Client_NotifyCharacterCreationResult(PlayerID target, bool success, string message, RPCInfo rpcInfo = default)
        {
            if (success)
            {
                Debug.Log($"Character creation successful: {message}");
                onCharacterCreated?.Invoke();
            }
            else
            {
                Debug.Warning($"Character creation failed: {message}");
            }
        }

        [ServerRpc]
        public static void Server_RequestEnterWorld(string characterGuid, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received request to enter world with character ID {characterGuid} from player {rpcInfo.sender}.");
            onEnterWorldRequest?.Invoke(rpcInfo.sender, characterGuid);
        }

        [TargetRpc]
        public static void Client_EnterWorld(PlayerID target, CharacterData characterData, string sceneName, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received command to enter world with scene '{sceneName}'.");
            onEnteringWorld?.Invoke(characterData, sceneName);
        }
    }
}