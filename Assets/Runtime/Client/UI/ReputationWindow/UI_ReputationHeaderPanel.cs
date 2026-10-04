using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_ReputationHeaderPanel : UI_Panel, IListPanel
    {
        private static UI_ReputationHeaderPanel _instance;
        public static UI_ReputationHeaderPanel Instance => _instance;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_ReputationHeaderEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        private FactionCategory currentCategory = FactionCategory.Primary;
        private PlayerReputation playerReputation;

        protected override void Awake()
        {
            _instance = this;

            ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterService.onClientReputationUpdated += OnClientReputationUpdated;

            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            playerReputation = actor.GetComponent<PlayerReputation>();
        }

        public void OnClientReputationUpdated(string factionID, int reputation, int delta)
        {
            if (playerReputation == null) return;

            if (currentEntries.Count == 0) PopulateList(true);

            foreach (var entry in currentEntries)
            {
                if (entry is UI_ReputationHeaderEntry repEntry)
                {
                    var def = repEntry.Data;
                    if (def != null && playerReputation.Reputations.TryGetValue(def.DefinitionId, out int rep))
                    {
                        repEntry.UpdateEntry(rep);
                    }
                }
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

            int i = 0;
            Debug.Log($"Populating reputation header for category {currentCategory} with {playerReputation.Reputations.Count} reputations");
            foreach (var rep in playerReputation.Reputations)
            {
                var def = FactionDefinitionLibrary.Instance.GetDefinition(rep.Key);
                Debug.Log($"Checking reputation entry for faction {def.DisplayName} with category {def.Category} against current category {currentCategory}");
                if (def.Category == currentCategory)
                {
                    var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                    var entry = entryObj.GetComponent<UI_ReputationHeaderEntry>();
                    entry.Initialize(def, i);
                    entry.Register(this);
                    entry.UpdateEntry(rep.Value);
                    i++;
                }
            }

            if (currentEntries.Count > 0)
                currentEntries[0].GetComponent<UI_ReputationHeaderEntry>().OnClick(null);
        }
    }
}