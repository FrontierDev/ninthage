using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ReputationHeaderPanel</c> implementation.
    /// </summary>
    public sealed class UI_ReputationHeaderPanel : UI_Panel, IListPanel
    {
        private static UI_ReputationHeaderPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_ReputationHeaderPanel Instance => _instance;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_ReputationHeaderEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private FactionCategory _currentCategory = FactionCategory.Primary;
        private PlayerReputation _playerReputation;

        protected override void Awake()
        {
            _instance = this;

            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterEndpoint.onClientReputationUpdated += OnClientReputationUpdated;

            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            _playerReputation = actor.GetComponent<PlayerReputation>();
        }

        /// <summary>
        /// Handles the <c>OnClientReputationUpdated</c> callback.
        /// </summary>
        public void OnClientReputationUpdated(string factionID, int reputation, int delta)
        {
            if (_playerReputation == null) return;

            if (currentEntries.Count == 0) PopulateList(true);

            foreach (var entry in currentEntries)
            {
                if (entry is UI_ReputationHeaderEntry repEntry)
                {
                    var def = repEntry.Data;
                    if (def != null && _playerReputation.Reputations.TryGetValue(def.DefinitionID, out int rep))
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

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ClearEntries();

            int i = 0;
            Debug.Log($"Populating reputation header for category {_currentCategory} with {_playerReputation.Reputations.Count} reputations");
            foreach (var rep in _playerReputation.Reputations)
            {
                var def = FactionDefinitionLibrary.Instance.GetDefinition(rep.Key);
                Debug.Log($"Checking reputation entry for faction {def.DisplayName} with category {def.Category} against current category {_currentCategory}");
                if (def.Category == _currentCategory)
                {
                    var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
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
