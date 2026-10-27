using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.UI;
using Game.Shared.Persistence;
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
                _characterSelectionWindow.PopulateCharacterList(characters);
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

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
            _selectedCharacter = character;
            _characterSelectionWindow.onCharacterSelected?.Invoke(character);
        }

        private void OnCharacterListReceived(List<CharacterData> characters)
        {
            _characterSelectionWindow.PopulateCharacterList(characters);
        }

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
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("CharacterSelection");
            UI_Manager.Instance.HideWindow("CharacterSelect");
            UI_Manager.Instance.HideWindow("CharacterCreation");
            UI_Manager.Instance.HideWindow("MainMenu");
            // UI_Manager.Instance.ShowWindow("GameUI");
        }
    }
}
