using Game.Shared;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestListPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestListPanel : UI_Panel, IListPanel
    {
        private static UI_QuestListPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_QuestListPanel Instance => _instance;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_QuestListEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private PlayerQuests _quests;
        /// <summary>
        /// Gets the <c>Quests</c> value.
        /// </summary>
        public PlayerQuests Quests => _quests;

        protected override void Awake()
        {
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            _instance = this;
            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            if (_quests != null)
                _quests.onUpdate -= OnQuestsUpdated;
            _quests = actor.GetComponent<PlayerQuests>();
            if (_quests != null)
            {
                _quests.onUpdate -= OnQuestsUpdated;
                _quests.onUpdate += OnQuestsUpdated;
            }

            PopulateList(forceClear: true);
        }

        private void OnQuestsUpdated()
        {
            Refresh();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();

            var activeQuestIds = new System.Collections.Generic.HashSet<string>(
                _quests.ActiveQuests.Select(q => q.Quest.DefinitionID));

            // Remove entries for quests no longer active
            for (int i = currentEntries.Count - 1; i >= 0; i--)
            {
                var entry = currentEntries[i] as UI_QuestListEntry;
                if (entry == null || !activeQuestIds.Contains(entry.Data.DefinitionID))
                {
                    Destroy(currentEntries[i].gameObject);
                    currentEntries.RemoveAt(i);
                }
            }

            // Add entries for new quests and re-initialize existing ones
            int index = 0;
            foreach (var questProgress in _quests.ActiveQuests)
            {
                var existing = currentEntries
                    .FirstOrDefault(e => (e as UI_QuestListEntry)?.Data.DefinitionID == questProgress.Quest.DefinitionID)
                    as UI_QuestListEntry;

                if (existing != null)
                {
                    existing.Initialize(questProgress.Quest, index);
                }
                else
                {
                    var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                    var entry = entryObj.GetComponent<UI_QuestListEntry>();
                    entry.Initialize(questProgress.Quest, index);
                    entry.Register(this);
                }
                index++;
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
            if (forceClear)
                ClearEntries();

            int index = 0;
            foreach (var quest in _quests.ActiveQuests)
            {
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_QuestListEntry>();
                entry.Initialize(quest.Quest, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
