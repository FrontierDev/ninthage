using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterFocusStatsPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterFocusStatsPanel : UI_Panel, IListPanel
    {
        private static UI_CharacterFocusStatsPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterFocusStatsPanel Instance => _instance;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_CharacterStatEntry statEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => statEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private List<ActorStatDefinition> focusStatDefinitions = new();
        private ActorStatContainer currentStats;

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

            focusStatDefinitions = def.FocusStats as List<ActorStatDefinition> ?? new List<ActorStatDefinition>();
            PopulateList(forceClear: true);

            if (ClientAccountManager.PlayerActor != null)
            {
                var stats = ClientAccountManager.PlayerActor.GetComponent<ActorStatContainer>();
                if (stats != null)
                {
                    currentStats = stats;
                    Refresh();
                }
            }
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            // Only update for the player's own actor.
            if (ClientAccountManager.PlayerActor == null) return;
            if (actor.ID != ClientAccountManager.PlayerActor.ID) return;
            currentStats = statContainer;
            Refresh();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();

            if (currentStats == null) return;

            foreach (var entry in currentEntries)
            {
                if ((UI_CharacterStatEntry)entry is UI_CharacterStatEntry statEntry)
                {
                    statEntry.Refresh(currentStats);
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
            if (ClientAccountManager.PlayerActor == null) return;

            if (forceClear)
            {
                ClearEntries();
            }

            int index = 0;
            foreach (var statDef in focusStatDefinitions)
            {
                var entryObj = Instantiate(statEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_CharacterStatEntry>();
                entry.Initialize(statDef, index);
                entry.Register(this);
                currentEntries.Add(entry);
                index++;
            }
        }
    }
}
