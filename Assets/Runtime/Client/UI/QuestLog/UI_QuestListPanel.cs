using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_QuestListPanel : UI_Panel, IListPanel
    {
        private static UI_QuestListPanel instance;
        public static UI_QuestListPanel Instance => instance;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_QuestListEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        private PlayerQuests quests;
        public PlayerQuests Quests => quests;

        protected override void Awake()
        {
            Game.Shared.Networking.ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            instance = this;
            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            quests = actor.GetComponent<PlayerQuests>();
            if (quests != null)
            {
                quests.onUpdate += OnQuestsUpdated;
            }

            PopulateList(forceClear: true);
        }

        private void OnQuestsUpdated()
        {
            Refresh();
        }

        public override void Refresh()
        {
            base.Refresh();

            var activeQuestIds = new System.Collections.Generic.HashSet<string>(
                quests.ActiveQuests.Select(q => q.Quest.DefinitionId));

            // Remove entries for quests no longer active
            for (int i = currentEntries.Count - 1; i >= 0; i--)
            {
                var entry = currentEntries[i] as UI_QuestListEntry;
                if (entry == null || !activeQuestIds.Contains(entry.Data.DefinitionId))
                {
                    Destroy(currentEntries[i].gameObject);
                    currentEntries.RemoveAt(i);
                }
            }

            // Add entries for new quests and re-initialize existing ones
            int index = 0;
            foreach (var questProgress in quests.ActiveQuests)
            {
                var existing = currentEntries
                    .FirstOrDefault(e => (e as UI_QuestListEntry)?.Data.DefinitionId == questProgress.Quest.DefinitionId)
                    as UI_QuestListEntry;

                if (existing != null)
                {
                    existing.Initialize(questProgress.Quest, index);
                }
                else
                {
                    var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
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

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
                ClearEntries();

            int index = 0;
            foreach (var quest in quests.ActiveQuests)
            {
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_QuestListEntry>();
                entry.Initialize(quest.Quest, index);
                entry.Register(this);
                index++;
            }
        }
    }
}