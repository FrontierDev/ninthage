using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
        private UI.UI_CharacterCreationWindow _characterCreationWindow;

        [FormerlySerializedAs("sceneCamera")]
        [SerializeField] private CinemachineCamera _sceneCamera;

        [Header("Selected Options")]
        [FormerlySerializedAs("_selectedClassId")]
        [SerializeField] private string _selectedClassID;
        [FormerlySerializedAs("_selectedRaceId")]
        [SerializeField] private string _selectedRaceID;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("CharacterCreationManager is already initialized!");
                return;
            }
            _instance = this;

            _characterCreationWindow = UI.UI_CharacterCreationWindow.Instance;
            _characterCreationWindow.ShowImmediate();

            // Subscribe to character creation-related events.
            Game.Shared.Networking.AccountEndpoint.onCharacterCreated += OnCharacterCreated;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            Game.Shared.Networking.AccountEndpoint.onCharacterCreated -= OnCharacterCreated;
            _instance = null;
        }

        private void Start()
        {
            _sceneCamera.Prioritize();
        }

        /// <summary>
        /// Executes the client-side <c>SelectClass</c> operation.
        /// </summary>
        public void SelectClass(ClassDefinition def)
        {
            Debug.Log($"Selected class: {def.DefinitionID}");
            _selectedClassID = def.DefinitionID;
            _characterCreationWindow.onClassSelected?.Invoke(def);
        }

        /// <summary>
        /// Gets the value provided by <c>GetCurrentClassID</c>.
        /// </summary>
        public string GetCurrentClassID() { return _selectedClassID; }

        /// <summary>
        /// Executes the client-side <c>SelectRace</c> operation.
        /// </summary>
        public void SelectRace(RaceDefinition def)
        {
            Debug.Log($"Selecteed race: {def.DefinitionID}");
            _selectedRaceID = def.DefinitionID;
            _characterCreationWindow.onRaceSelected?.Invoke(def);
        }

        /// <summary>
        /// Gets the value provided by <c>GetCurrentRaceID</c>.
        /// </summary>
        public string GetCurrentRaceID() { return _selectedRaceID; }

        /// <summary>
        /// Executes the client-side <c>CreateCharacter</c> operation.
        /// </summary>
        public void CreateCharacter(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(_selectedClassID) || string.IsNullOrEmpty(_selectedRaceID))
            {
                Debug.Error("Cannot create character: Class or Race not selected.");
                return;
            }

            Game.Shared.Networking.AccountEndpoint.Server_TryCreateCharacter(new Game.Shared.Persistence.NewCharacterData
            {
                FirstName = firstName,
                LastName = lastName,
                ClassID = _selectedClassID,
                RaceID = _selectedRaceID
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
