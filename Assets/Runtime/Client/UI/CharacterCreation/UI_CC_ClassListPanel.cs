using ClassDefinitionLibrary = Game.Shared.Data.ClassDefinitionLibrary;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_ClassListPanel</c> implementation.
    /// </summary>
    public class UI_CC_ClassListPanel : UI_Panel, IListPanel
    {
        [Header("Character List Settings")]
        [FormerlySerializedAs("contentContainer")]
        [SerializeField] private GameObject _contentContainer;
        [FormerlySerializedAs("entryPrefab")]
        [SerializeField] private GameObject _entryPrefab;
        GameObject IListPanel.contentContainer => _contentContainer;
        GameObject IListPanel.entryPrefab => _entryPrefab;

        private ClassDefinitionLibrary _classDefinitionLibrary;

        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private void Start()
        {
            _classDefinitionLibrary = ClassDefinitionLibrary.Instance;
            if (_classDefinitionLibrary == null)
            {
                Debug.Error("Could not load ClassDefinitionLibrary");
                return;
            }

            UI_CharacterCreationWindow.Instance.onClassSelected += OnClassSelected;

            PopulateList();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            PopulateList(true);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            if (CharacterCreationManager.Instance != null && CharacterCreationManager.Instance.GetCurrentClassID() != "")
            {
                UI_CharacterCreationWindow.Instance.ShowPanel("ClassInfo");
            }
            else
            {
                UI_CC_NextStageButton.Instance?.Hide();
            }

            UI_CC_NextStageButton.Instance?.SetCurrentPanel("ClassSelection");
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            base.Hide();
            if (CharacterCreationManager.Instance == null || CharacterCreationManager.Instance.GetCurrentClassID() == "")
                UI_CharacterCreationWindow.Instance.HidePanel("ClassInfo");
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ((IListPanel)this).ClearList();

            int _index = 0;
            foreach (var classDef in _classDefinitionLibrary.GetAllDefinitions())
            {
                var entryObj = GameObject.Instantiate(_entryPrefab, _contentContainer.transform);
                var entry = entryObj.GetComponent<UI_ListEntry>() as UI_CC_ClassListEntry;
                if (entry == null)
                {
                    Debug.Error("Entry prefab does not have a UI_ListEntry component");
                    continue;
                }

                entry.Initialize(classDef, _index);
                entry.Register(this);
                _index++;
            }
        }

        private void OnClassSelected(ClassDefinition def)
        {
            // Show the next stage button.
            UI_CC_NextStageButton.Instance.SetNextPanel("FinalizeCharacter");
        }
    }
}
