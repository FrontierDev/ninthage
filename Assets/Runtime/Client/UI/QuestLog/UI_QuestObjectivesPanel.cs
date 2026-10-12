
using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestObjectivesPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestObjectivesPanel : UI_Panel, IListPanel
    {
        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_QuestObjectivesEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private QuestDefinition _def;

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(QuestDefinition def)
        {
            this._def = def;
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
            foreach (QuestObjective objective in _def.Objectives)
            {
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_QuestObjectivesEntry>();
                entry.Initialize(objective, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
