using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterSpecialBar</c> implementation.
    /// </summary>
    public sealed class UI_CharacterSpecialBar : UI_Panel, IListPanel
    {
        private static UI_CharacterSpecialBar _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterSpecialBar Instance => _instance;

        private ActorStatContainer _playerStatContainer;
        private ActorStatDefinition _trackedStat;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("counterPrefab")]
        [SerializeField] private UI_SpecialResourceCounter _counterPrefab;
        [FormerlySerializedAs("counters")]
        [SerializeField] private List<Sprite> _counters;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _counterPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private int _currentMax = 0;
        private int _currentValue = 0;

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;

            _instance = this;
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            _playerStatContainer = playerActor.GetComponent<ActorStatContainer>();

            // Get the tracked stat from the class definition.
            var classID = ClientAccountManager.ActiveCharacter.ClassID;
            var def = ClassDefinitionLibrary.Instance.GetDefinition(classID);
            _trackedStat = def.BaseStats.FirstOrDefault(x => x.definition.Tags.Any(tag => tag == "special"))?.definition;

            if (_trackedStat == null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                if (_playerStatContainer != null)
                {
                    StatEndpoint.onStatsUpdated += OnStatsUpdated;
                    Refresh();
                }
            }
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            if (actor == ClientAccountManager.PlayerActor)
            {
                Refresh();
            }
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            if (_playerStatContainer == null || _trackedStat == null) return;
            base.Refresh();

            if (!_playerStatContainer.TryGetStat(_trackedStat.DefinitionID, out var stat)) return;

            if (Mathf.FloorToInt(stat.EffectiveMaximum) != _currentMax)
            {
                _currentMax = Mathf.FloorToInt(stat.EffectiveMaximum);
                PopulateList(true);
            }

            _currentValue = Mathf.FloorToInt(stat.CurrentValue);
            for (int i = 0; i < currentEntries.Count; i++)
            {
                var counter = currentEntries[i] as UI_SpecialResourceCounter;
                if (counter != null)
                {
                    counter.SetFilled(i < _currentValue);
                }
            }
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
            {
                foreach (var entry in currentEntries)
                {
                    Destroy(entry.gameObject);
                }
                currentEntries.Clear();
            }

            int index = 0;
            for (int i = 0; i < _currentMax; i++)
            {
                var entry = Instantiate(_counterPrefab, _container.transform);
                entry.Initialize(_trackedStat.Icon, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
