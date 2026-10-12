using ClassDefinitionLibrary = Game.Shared.Data.ClassDefinitionLibrary;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_RaceListPanel</c> implementation.
    /// </summary>
    public class UI_CC_RaceListPanel : UI_Panel, IListPanel
    {
        [Header("Race List Settings")]
        [SerializeField] private GameObject contentContainer;
        [SerializeField] private GameObject entryPrefab;
        GameObject IListPanel.contentContainer => contentContainer;
        GameObject IListPanel.entryPrefab => entryPrefab;

        private RaceDefinitionLibrary raceDefinitionLibrary;

        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private void Start()
        {
            raceDefinitionLibrary = RaceDefinitionLibrary.Instance;
            if (raceDefinitionLibrary == null)
            {
                Debug.Error("Could not load RaceDefinitionLibrary");
                return;
            }

            UI_CharacterCreationWindow.Instance.onRaceSelected += OnRaceSelected;

            PopulateList();
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            if (CharacterCreationManager.Instance != null && CharacterCreationManager.Instance.GetCurrentRaceId() != "")
            {
                UI_CharacterCreationWindow.Instance.ShowPanel("RaceInfo");
            }
            else
            {
                UI_CC_NextStageButton.Instance?.Hide();
            }

            UI_CC_NextStageButton.Instance?.SetCurrentPanel("RaceSelection");
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            base.Hide();
            if (CharacterCreationManager.Instance == null || CharacterCreationManager.Instance.GetCurrentRaceId() == "")
                UI_CharacterCreationWindow.Instance.HidePanel("RaceInfo");
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            PopulateList(true);
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ((IListPanel)this).ClearList();

            int _index = 0;
            foreach (var raceDef in raceDefinitionLibrary.GetAllDefinitions())
            {
                var entryObj = GameObject.Instantiate(entryPrefab, contentContainer.transform);
                var entry = entryObj.GetComponent<UI_ListEntry>() as UI_CC_RaceListEntry;
                if (entry == null)
                {
                    Debug.Error("Entry prefab does not have a UI_ListEntry component");
                    continue;
                }

                entry.Initialize(raceDef, _index);
                entry.Register(this);
                _index++;
            }
        }

        private void OnRaceSelected(RaceDefinition def)
        {
            // Show the next stage button.
            UI_CC_NextStageButton.Instance.SetNextPanel("ClassSelection");
        }
    }
}
