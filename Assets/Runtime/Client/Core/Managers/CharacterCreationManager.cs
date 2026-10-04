using Game.Shared.Data;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    public sealed class CharacterCreationManager : MonoBehaviour
    {
        private static CharacterCreationManager _instance;
        public static CharacterCreationManager Instance => _instance;

        // Internal UI references.
        private UI.UI_CharacterCreationWindow characterCreationWindow;

        [SerializeField] private CinemachineCamera sceneCamera;

        [Header("Selected Options")]
        [SerializeField] private string _selectedClassId;
        [SerializeField] private string _selectedRaceId;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("CharacterCreationManager is already initialized!");
                return;
            }
            _instance = this;

            characterCreationWindow = UI.UI_CharacterCreationWindow.Instance;
            characterCreationWindow.ShowImmediate();

            // Subscribe to character creation-related events.
            Game.Shared.Networking.AccountService.onCharacterCreated += OnCharacterCreated;
        }

        private void Start()
        {
            sceneCamera.Prioritize();
        }

        public void SelectClass(ClassDefinition def)
        {
            Debug.Log($"Selected class: {def.DefinitionId}");
            _selectedClassId = def.DefinitionId;
            characterCreationWindow.onClassSelected?.Invoke(def);
        }

        public string GetCurrentClassId() { return _selectedClassId; }

        public void SelectRace(RaceDefinition def)
        {
            Debug.Log($"Selecteed race: {def.DefinitionId}");
            _selectedRaceId = def.DefinitionId;
            characterCreationWindow.onRaceSelected?.Invoke(def);
        }

        public string GetCurrentRaceId() { return _selectedRaceId; }

        public void CreateCharacter(string characterName)
        {
            if (string.IsNullOrEmpty(_selectedClassId) || string.IsNullOrEmpty(_selectedRaceId))
            {
                Debug.Error("Cannot create character: Class or Race not selected.");
                return;
            }

            Game.Shared.Networking.AccountService.Server_TryCreateCharacter(new Game.Shared.Persistence.NewCharacterData
            {
                Name = characterName,
                ClassID = _selectedClassId,
                RaceID = _selectedRaceId
            });
        }

        private void OnCharacterCreated()
        {
            // Unload the character creation screen 
            // and load the character selection screen, which should now have the new character in the list.
            SceneManager.UnloadSceneAsync("CharacterCreation");
            SceneManager.LoadSceneAsync("CharacterSelection", LoadSceneMode.Additive);
            UI.UI_Manager.Instance.HideWindow("CharacterCreation");
            UI.UI_Manager.Instance.ShowWindow("CharacterSelect");
        }
    }
}