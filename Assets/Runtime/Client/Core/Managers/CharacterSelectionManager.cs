using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.UI;
using Game.Shared.Persistence;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>CharacterSelectionManager</c> implementation.
    /// </summary>
    public sealed class CharacterSelectionManager : MonoBehaviour
    {
        private static CharacterSelectionManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static CharacterSelectionManager Instance => _instance;

        // Internal UI references.
        private UI.UI_CharacterSelectWindow _characterSelectionWindow;

        [FormerlySerializedAs("sceneCamera")]
        [SerializeField] private CinemachineCamera _sceneCamera;

        private CharacterData _selectedCharacter;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("CharacterSelectionManager is already initialized!");
                return;
            }
            _instance = this;

            _characterSelectionWindow = UI.UI_CharacterSelectWindow.Instance;
            _characterSelectionWindow.ShowImmediate();

            // Subscribe to character selection-related events.
            Game.Shared.Networking.AccountEndpoint.onCharacterListReceived += OnCharacterListReceived;
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld += OnEnteringWorld;

            if (Game.Client.ClientAccountManager.TryGetCharacterList(out List<CharacterData> characters))
                OnCharacterListReceived(characters);
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            if (ClientConnectionManager.Initialized)
                ClientConnectionManager.Instance.ClearCharacterSelectionBackground();

            Game.Shared.Networking.AccountEndpoint.onCharacterListReceived -= OnCharacterListReceived;
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld -= OnEnteringWorld;
            _instance = null;
        }

        private void Start()
        {
            _sceneCamera.Prioritize();
        }

        /// <summary>
        /// Executes the client-side <c>SelectCharacter</c> operation.
        /// </summary>
        public void SelectCharacter(CharacterData character)
        {
            if (character == null)
            {
                _selectedCharacter = null;
                if (ClientConnectionManager.Initialized)
                    ClientConnectionManager.Instance.ClearCharacterSelectionBackground();
                _characterSelectionWindow.onCharacterSelected?.Invoke(null);
                return;
            }

            if (!Game.Client.ClientAccountManager.TryGetCharacterList(
                    out List<CharacterData> characters))
            {
                return;
            }

            CharacterData currentCharacter = FindCharacterByGUID(characters, character.GUID);
            if (currentCharacter == null)
                return;

            _selectedCharacter = currentCharacter;
            if (ClientConnectionManager.Initialized)
                ClientConnectionManager.Instance.SelectCharacterSelectionBackground(currentCharacter.RaceID);
            _characterSelectionWindow.onCharacterSelected?.Invoke(currentCharacter);
        }

        private void OnCharacterListReceived(List<CharacterData> characters)
        {
            _characterSelectionWindow.PopulateCharacterList(characters);

            CharacterData selectedCharacter = null;
            if (_selectedCharacter != null)
                selectedCharacter = FindCharacterByGUID(characters, _selectedCharacter.GUID);

            if (selectedCharacter == null && characters != null && characters.Count > 0)
                selectedCharacter = characters[0];

            _selectedCharacter = selectedCharacter;
            if (ClientConnectionManager.Initialized)
                ClientConnectionManager.Instance.SelectCharacterSelectionBackground(_selectedCharacter?.RaceID);
            _characterSelectionWindow.onCharacterSelected?.Invoke(_selectedCharacter);
        }

        private static CharacterData FindCharacterByGUID(
            IReadOnlyList<CharacterData> characters,
            string characterGUID)
        {
            if (characters == null)
                return null;

            for (int index = 0; index < characters.Count; index++)
            {
                CharacterData character = characters[index];
                if (character != null &&
                    string.Equals(character.GUID, characterGUID, StringComparison.Ordinal))
                {
                    return character;
                }
            }

            return null;
        }

        /// <summary>Gets the currently selected account character.</summary>
        public CharacterData SelectedCharacter => _selectedCharacter;

        /// <summary>
        /// Executes the client-side <c>EnterWorld</c> operation.
        /// </summary>
        public void EnterWorld()
        {
            if (_selectedCharacter == null)
            {
                Debug.Warning("No character selected, cannot enter world.");
                return;
            }

            Game.Shared.Networking.AccountEndpoint.Server_RequestEnterWorld(_selectedCharacter.GUID);
        }

        private void OnEnteringWorld(CharacterData characterData, string sceneName)
        {
            Debug.Log("Entering world...");
            if (ClientConnectionManager.Initialized)
                ClientConnectionManager.Instance.ClearCharacterSelectionBackground();
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("CharacterSelection");
            UI_Manager.Instance.HideWindow("CharacterSelect");
            UI_Manager.Instance.HideWindow("CharacterCreation");
            UI_Manager.Instance.HideWindow("MainMenu");
            // UI_Manager.Instance.ShowWindow("GameUI");
        }
    }
}
