
using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_QuestObjectivesPanel : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject container;
        [SerializeField] private UI_QuestObjectivesEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        private QuestDefinition def;

        public void SetQuestDefinition(QuestDefinition def)
        {
            this.def = def;
            PopulateList(true);
        }

        public void UpdateEntries(int[] progress)
        {
            for (int i = 0; i < progress.Length; i++)
            {
                (currentEntries[i] as UI_QuestObjectivesEntry).UpdateEntry(progress[i]);
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in currentEntries)
            {
                Destroy(entry.gameObject);
            }
            currentEntries.Clear();
        }

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ClearEntries();

            int index = 0;
            foreach (QuestObjective objective in def.Objectives)
            {
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_QuestObjectivesEntry>();
                entry.Initialize(objective, index);
                entry.Register(this);
                index++;
            }
        }
    }
}