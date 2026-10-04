using UnityEngine;
using System.Collections.Generic;
using Game.Shared.Persistence;
using Debug = Game.Shared.FormattedDebug;
using Game.Shared;
using Game.Client.UI;

namespace Game.Client
{
    public sealed class ClientAccountManager : MonoBehaviour
    {
        private static ClientAccountManager _instance;
        public static ClientAccountManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientAccountManager();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        private static string _currentUsername;

        // List of the current account's characters.
        private static List<CharacterData> _characterList;
        private static bool _received = false;

        // Get the current active character.
        private static CharacterData _activeCharacter;
        public static CharacterData ActiveCharacter => _activeCharacter;
        private static Actor _playerActor;
        public static Actor PlayerActor => _playerActor;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            // Subscribe to account-related events 
            Game.Shared.Networking.AccountService.onCharacterListReceived += UpdateCharacterList;
            Game.Shared.Networking.AccountService.onEnteringWorld += OnEnterWorld;
            Game.Shared.Networking.ActorService.onActorOwnershipTaken += OnActorOwnershipTaken;
            Game.Shared.Networking.ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterService.onClientLearnedSpell += OnClientLearnedSpell;

            _initialized = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            Game.Shared.Networking.AccountService.onCharacterListReceived -= UpdateCharacterList;
            Game.Shared.Networking.AccountService.onEnteringWorld -= OnEnterWorld;
            Game.Shared.Networking.ActorService.onActorOwnershipTaken -= OnActorOwnershipTaken;
            Game.Shared.Networking.ActorService.onPlayerActorAssigned -= OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterService.onClientLearnedSpell -= OnClientLearnedSpell;
            _instance = null;
        }

        private void OnEnterWorld(CharacterData characterData, string sceneName)
        {
            _activeCharacter = characterData;
            Debug.Log($"Entering world with character '{characterData.Name}' in scene '{sceneName}'.");
        }

        private void OnActorOwnershipTaken(Actor actor)
        {
            Debug.Log($"Taking control of {actor.Name} (ID: {actor.Id})");
            _playerActor = actor;
            Game.Client.Utility.PlaceholderText.LocalContainer = actor.GetComponent<ActorStatContainer>();
            Game.Shared.Networking.ActorService.onPlayerActorAssigned?.Invoke(actor);
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

            Debug.Log($"Player actor assigned: {actor.Name} (ID: {actor.Id})");
        }

        private void OnClientLearnedSpell(string spellID)
        {
            if (_activeCharacter != null) _activeCharacter.KnownSpellIDs.Add(spellID);
            else Debug.Error($"Received learned spell '{spellID}' but no active character is set.");
        }
        #endregion

        public static void UpdateCharacterList(List<CharacterData> newCharacterList)
        {
            _received = true;
            _characterList = newCharacterList;
            Debug.Log($"Updated character list with {newCharacterList.Count} characters.");
        }

        public static bool HasReceivedCharacters() => _received;

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