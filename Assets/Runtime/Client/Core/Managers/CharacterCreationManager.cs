using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>CharacterCreationManager</c> implementation.
    /// </summary>
    public sealed class CharacterCreationManager : MonoBehaviour
    {
        private static CharacterCreationManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
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
            Game.Shared.Networking.AccountEndpoint.onCharacterCreated += OnCharacterCreated;
        }

        private void Start()
        {
            sceneCamera.Prioritize();
        }

        /// <summary>
        /// Executes the client-side <c>SelectClass</c> operation.
        /// </summary>
        public void SelectClass(ClassDefinition def)
        {
            Debug.Log($"Selected class: {def.DefinitionID}");
            _selectedClassId = def.DefinitionID;
            characterCreationWindow.onClassSelected?.Invoke(def);
        }

        /// <summary>
        /// Gets the value provided by <c>GetCurrentClassId</c>.
        /// </summary>
        public string GetCurrentClassId() { return _selectedClassId; }

        /// <summary>
        /// Executes the client-side <c>SelectRace</c> operation.
        /// </summary>
        public void SelectRace(RaceDefinition def)
        {
            Debug.Log($"Selecteed race: {def.DefinitionID}");
            _selectedRaceId = def.DefinitionID;
            characterCreationWindow.onRaceSelected?.Invoke(def);
        }

        /// <summary>
        /// Gets the value provided by <c>GetCurrentRaceId</c>.
        /// </summary>
        public string GetCurrentRaceId() { return _selectedRaceId; }

        /// <summary>
        /// Executes the client-side <c>CreateCharacter</c> operation.
        /// </summary>
        public void CreateCharacter(string characterName)
        {
            if (string.IsNullOrEmpty(_selectedClassId) || string.IsNullOrEmpty(_selectedRaceId))
            {
                Debug.Error("Cannot create character: Class or Race not selected.");
                return;
            }

            Game.Shared.Networking.AccountEndpoint.Server_TryCreateCharacter(new Game.Shared.Persistence.NewCharacterData
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
