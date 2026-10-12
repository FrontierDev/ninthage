using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterStatList</c> implementation.
    /// </summary>
    public sealed class UI_CharacterStatList : UI_Panel, IListPanel
    {
        private static UI_CharacterStatList _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterStatList Instance => _instance;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("statEntryPrefab")]
        [SerializeField] private GameObject _statEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _statEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        [FormerlySerializedAs("statDefinitions")]
        [SerializeField] private List<ActorStatDefinition> _statDefinitions = new();
        [FormerlySerializedAs("focusStatDefinitions")]
        [SerializeField] private List<ActorStatDefinition> _focusStatDefinitions = new();
        [FormerlySerializedAs("relevantStatDefinitions")]
        [SerializeField] private List<ActorStatDefinition> _relevantStatDefinitions = new();
        private ActorStatContainer _currentStats;

        protected override void Awake()
        {
            _instance = this;

            Game.Shared.Networking.StatEndpoint.onStatsUpdated += OnStatsUpdated;
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;

            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            // Which stats should the focus stats panel display? This comes from
            // the character's class definition.
            var charClassID = ClientAccountManager.GetCharacterData()?.ClassID;
            var def = ClassDefinitionLibrary.Instance.GetDefinition(charClassID);

            _focusStatDefinitions = def.FocusStats as List<ActorStatDefinition> ?? new List<ActorStatDefinition>();
            _relevantStatDefinitions = def.RelevantStats as List<ActorStatDefinition> ?? new List<ActorStatDefinition>();
            PopulateList(forceClear: true);

            if (ClientAccountManager.PlayerActor != null)
            {
                var stats = ClientAccountManager.PlayerActor.GetComponent<ActorStatContainer>();
                if (stats != null)
                {
                    _currentStats = stats;
                    Refresh();
                }
            }
        }


        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();

            if (_currentStats == null) return;

            foreach (var entry in currentEntries)
            {
                if ((UI_CharacterStatEntry)entry is UI_CharacterStatEntry statEntry)
                {
                    statEntry.Refresh(_currentStats);
                }
            }
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            // Only update for the player's own actor.
            if (ClientAccountManager.PlayerActor == null) return;
            if (actor.ID != ClientAccountManager.PlayerActor.ID) return;
            _currentStats = statContainer;
            Refresh();
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
            if (ClientAccountManager.PlayerActor == null) return;

            if (forceClear)
            {
                ClearEntries();
            }

            int index = 0;
            foreach (var statDef in _statDefinitions)
            {
                if (!_focusStatDefinitions.Contains(statDef) && !_relevantStatDefinitions.Contains(statDef))
                    continue;

                var entryObj = Instantiate(_statEntryPrefab, _container.transform);
                var entry = entryObj.GetComponent<UI_CharacterStatEntry>();
                entry.Initialize(statDef, index);
                entry.Register(this);
                currentEntries.Add(entry);
                index++;
            }
        }
    }
}
