using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using System;
using System.Collections.Generic;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Handles player account-related RPCs, such as sending character 
    /// lists to clients upon successful authentication. 
    /// This class is shared between server and client, 
    /// but currently only contains server-to-client RPCs. 
    /// Future expansions may include client-to-server RPCs for account management actions.
    /// </summary>
    public static class AccountEndpoint
    {
        // Client callbacks.
        /// <summary>Raised when the client receives its character list.</summary>
        public static Action<List<CharacterData>> onCharacterListReceived;
        /// <summary>Raised when the client is instructed to enter the world.</summary>
        public static Action<CharacterData, string> onEnteringWorld;
        /// <summary>Raised after the client has entered the world.</summary>
        public static Action onEnteredWorld;

        // Server callbacks
        /// <summary>Raised when the server receives a character creation request.</summary>
        public static Action<PlayerID, Game.Shared.Persistence.NewCharacterData> onCharacterCreationRequest;
        /// <summary>Raised when character creation succeeds.</summary>
        public static Action onCharacterCreated;
        /// <summary>Raised when the server receives an enter-world request.</summary>
        public static Action<PlayerID, string> onEnterWorldRequest;

        [TargetRpc]
        /// <summary>Sends the character list to a client.</summary>
        public static void Client_UpdateCharacterList(PlayerID target, List<CharacterData> characterList, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received character list with {characterList.Count} characters.");
            onCharacterListReceived?.Invoke(characterList);
        }

        [ServerRpc]
        /// <summary>Requests character creation on the server.</summary>
        public static void Server_TryCreateCharacter(Game.Shared.Persistence.NewCharacterData newCharacterData, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received request to create character '{newCharacterData.Name}' from player {rpcInfo.sender}.");
            onCharacterCreationRequest?.Invoke(rpcInfo.sender, newCharacterData);
        }

        [TargetRpc]
        /// <summary>Notifies a client of the character creation result.</summary>
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
        /// <summary>Requests entry into the world for a character.</summary>
        public static void Server_RequestEnterWorld(string characterGUID, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received request to enter world with character ID {characterGUID} from player {rpcInfo.sender}.");
            onEnterWorldRequest?.Invoke(rpcInfo.sender, characterGUID);
        }

        [TargetRpc]
        /// <summary>Instructs a client to enter the world scene.</summary>
        public static void Client_EnterWorld(PlayerID target, CharacterData characterData, string sceneName, RPCInfo rpcInfo = default)
        {
            Debug.Log($"Received command to enter world with scene '{sceneName}'.");
            onEnteringWorld?.Invoke(characterData, sceneName);
        }
    }
}
