using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using System.Collections.Generic;
using Game.Shared.Persistence;
using Unity.Cinemachine;
using Game.Client.UI;

namespace Game.Client
{
    public sealed class CharacterSelectionManager : MonoBehaviour
    {
        private static CharacterSelectionManager _instance;
        public static CharacterSelectionManager Instance => _instance;

        // Internal UI references.
        private UI.UI_CharacterSelectWindow characterSelectionWindow;

        [SerializeField] private CinemachineCamera sceneCamera;

        private CharacterData _selectedCharacter;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("CharacterSelectionManager is already initialized!");
                return;
            }
            _instance = this;

            characterSelectionWindow = UI.UI_CharacterSelectWindow.Instance;
            characterSelectionWindow.ShowImmediate();

            // Subscribe to character selection-related events.
            Game.Shared.Networking.AccountService.onCharacterListReceived += OnCharacterListReceived;
            Game.Shared.Networking.AccountService.onEnteringWorld += OnEnteringWorld;

            if (Game.Client.ClientAccountManager.TryGetCharacterList(out List<CharacterData> characters))
                characterSelectionWindow.PopulateCharacterList(characters);
        }

        private void Start()
        {
            sceneCamera.Prioritize();
        }

        public void SelectCharacter(CharacterData character)
        {
            _selectedCharacter = character;
            characterSelectionWindow.onCharacterSelected?.Invoke(character);
        }

        private void OnCharacterListReceived(List<CharacterData> characters)
        {
            characterSelectionWindow.PopulateCharacterList(characters);
        }

        public void EnterWorld()
        {
            if (_selectedCharacter == null)
            {
                Debug.Warning("No character selected, cannot enter world.");
                return;
            }

            Game.Shared.Networking.AccountService.Server_RequestEnterWorld(_selectedCharacter.Guid);
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