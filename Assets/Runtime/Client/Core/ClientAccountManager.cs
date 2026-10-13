using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.UI;
using Game.Shared;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientAccountManager</c> implementation.
    /// </summary>
    public sealed class ClientAccountManager : MonoBehaviour
    {
        private static ClientAccountManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientAccountManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        private static string _currentUsername;

        // List of the current account's characters.
        private static List<CharacterData> _characterList;
        private static bool _received = false;

        // Get the current active character.
        private static CharacterData _activeCharacter;
        /// <summary>
        /// Gets the <c>ActiveCharacter</c> value.
        /// </summary>
        public static CharacterData ActiveCharacter => _activeCharacter;
        private static Actor _playerActor;
        /// <summary>
        /// Gets the <c>PlayerActor</c> value.
        /// </summary>
        public static Actor PlayerActor => _playerActor;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            // Subscribe to account-related events 
            Game.Shared.Networking.AccountEndpoint.onCharacterListReceived += UpdateCharacterList;
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld += OnEnterWorld;
            Game.Shared.Networking.ActorEndpoint.onActorOwnershipTaken += OnActorOwnershipTaken;
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterEndpoint.onClientLearnedSpell += OnClientLearnedSpell;

            _initialized = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            Game.Shared.Networking.AccountEndpoint.onCharacterListReceived -= UpdateCharacterList;
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld -= OnEnterWorld;
            Game.Shared.Networking.ActorEndpoint.onActorOwnershipTaken -= OnActorOwnershipTaken;
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterEndpoint.onClientLearnedSpell -= OnClientLearnedSpell;
            _instance = null;
        }

        private void OnEnterWorld(CharacterData characterData, string sceneName)
        {
            _activeCharacter = characterData;
            Debug.Log($"Entering world with character '{characterData.Name}' in scene '{sceneName}'.");
        }

        private void OnActorOwnershipTaken(Actor actor)
        {
            Debug.Log($"Taking control of {actor.Name} (ID: {actor.ID})");
            _playerActor = actor;
            Game.Client.Utility.PlaceholderText.LocalContainer = actor.GetComponent<ActorStatContainer>();
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned?.Invoke(actor);
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            _playerActor = actor;
            ((PlayerActor)actor).SetCharacterData(_activeCharacter);
            actor.GetComponent<ActorSpellcaster>().LoadKnownSpells(_activeCharacter.KnownSpellIDs);
            actor.GetComponent<PlayerInventory>().LoadInventory(_activeCharacter);
            actor.GetComponent<PlayerEquipment>().LoadEquipment(_activeCharacter);
            actor.GetComponent<PlayerExperience>().LoadExperience(_activeCharacter);
            actor.GetComponent<PlayerTalents>().LoadTalents(_activeCharacter);
            actor.GetComponent<PlayerReputation>().LoadReputations(_activeCharacter);
            actor.GetComponent<PlayerQuests>().LoadQuests(_activeCharacter);
            actor.GetComponent<ActorEvents>().LoadClassEvents(_activeCharacter);

            // Intialize the special resource UI if applicable.
            UI_SpecialResourcePanel.Instance.OnPlayerActorAssigned(actor);

            Debug.Log($"Player actor assigned: {actor.Name} (ID: {actor.ID})");
        }

        private void OnClientLearnedSpell(string spellID)
        {
            if (_activeCharacter != null) _activeCharacter.KnownSpellIDs.Add(spellID);
            else Debug.Error($"Received learned spell '{spellID}' but no active character is set.");
        }
        #endregion

        /// <summary>
        /// Executes the client-side <c>UpdateCharacterList</c> operation.
        /// </summary>
        public static void UpdateCharacterList(List<CharacterData> newCharacterList)
        {
            _received = true;
            _characterList = newCharacterList;
            Debug.Log($"Updated character list with {newCharacterList.Count} characters.");
        }

        /// <summary>
        /// Executes the client-side <c>HasReceivedCharacters</c> operation.
        /// </summary>
        public static bool HasReceivedCharacters() => _received;

        /// <summary>
        /// Executes the client-side <c>TryGetCharacterList</c> operation.
        /// </summary>
        public static bool TryGetCharacterList(out List<CharacterData> characterList)
        {
            if (_characterList != null)
            {
                characterList = _characterList;
                return true;
            }
            else
            {
                characterList = null;
                Debug.Warning("Character list is not available yet.");
                return false;
            }
        }

        /// <summary>
        /// Gets the value provided by <c>GetCharacterData</c>.
        /// </summary>
        public static CharacterData GetCharacterData()
        {
            if (_activeCharacter != null)
            {
                return _activeCharacter;
            }
            else
            {
                Debug.Warning("No active character selected.");
                return null;
            }
        }
    }
}
