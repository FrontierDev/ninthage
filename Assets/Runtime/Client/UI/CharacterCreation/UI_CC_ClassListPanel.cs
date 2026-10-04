using UnityEngine;
using System.Collections.Generic;
using ClassDefinitionLibrary = Game.Shared.Data.ClassDefinitionLibrary;
using Debug = Game.Shared.FormattedDebug;
using Game.Shared.Data;

namespace Game.Client.UI
{
    public class UI_CC_ClassListPanel : UI_Panel, IListPanel
    {
        [Header("Character List Settings")]
        [SerializeField] private GameObject contentContainer;
        [SerializeField] private GameObject entryPrefab;
        GameObject IListPanel.contentContainer => contentContainer;
        GameObject IListPanel.entryPrefab => entryPrefab;

        private ClassDefinitionLibrary classDefinitionLibrary;

        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        private void Start()
        {
            classDefinitionLibrary = ClassDefinitionLibrary.Instance;
            if (classDefinitionLibrary == null)
            {
                Debug.Error("Could not load ClassDefinitionLibrary");
                return;
            }

            UI_CharacterCreationWindow.Instance.onClassSelected += OnClassSelected;

            PopulateList();
        }

        public override void Refresh()
        {
            PopulateList(true);
        }

        public override void Show()
        {
            base.Show();
            if (CharacterCreationManager.Instance != null && CharacterCreationManager.Instance.GetCurrentClassId() != "")
            {
                UI_CharacterCreationWindow.Instance.ShowPanel("ClassInfo");
            }
            else
            {
                UI_CC_NextStageButton.Instance?.Hide();
            }

            UI_CC_NextStageButton.Instance?.SetCurrentPanel("ClassSelection");
        }

        public override void Hide()
        {
            base.Hide();
            if (CharacterCreationManager.Instance == null || CharacterCreationManager.Instance.GetCurrentClassId() == "")
                UI_CharacterCreationWindow.Instance.HidePanel("ClassInfo");
        }

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ((IListPanel)this).ClearList();

            int _index = 0;
            foreach (var classDef in classDefinitionLibrary.GetAllDefinitions())
            {
                var entryObj = GameObject.Instantiate(entryPrefab, contentContainer.transform);
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