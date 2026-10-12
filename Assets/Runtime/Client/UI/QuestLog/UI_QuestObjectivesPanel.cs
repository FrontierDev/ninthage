
using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestObjectivesPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestObjectivesPanel : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject container;
        [SerializeField] private UI_QuestObjectivesEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private QuestDefinition def;

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(QuestDefinition def)
        {
            this.def = def;
            PopulateList(true);
        }

        /// <summary>
        /// Executes the client-side <c>UpdateEntries</c> operation.
        /// </summary>
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

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
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
