using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterSpecialBar : UI_Panel, IListPanel
    {
        private static UI_CharacterSpecialBar _instance;
        public static UI_CharacterSpecialBar Instance => _instance;

        private ActorStatContainer playerStatContainer;
        private ActorStatDefinition trackedStat;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_SpecialResourceCounter counterPrefab;
        [SerializeField] private List<Sprite> counters;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => counterPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        private int currentMax = 0;
        private int currentValue = 0;

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;

            _instance = this;
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            playerStatContainer = playerActor.GetComponent<ActorStatContainer>();

            // Get the tracked stat from the class definition.
            var classId = ClientAccountManager.ActiveCharacter.ClassID;
            var def = ClassDefinitionLibrary.Instance.GetDefinition(classId);
            trackedStat = def.BaseStats.FirstOrDefault(x => x.definition.Tags.Any(tag => tag == "special"))?.definition;

            if (trackedStat == null)
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

                if (playerStatContainer != null)
                {
                    StatService.onStatsUpdated += OnStatsUpdated;
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

        public override void Refresh()
        {
            if (playerStatContainer == null || trackedStat == null) return;
            base.Refresh();

            if (!playerStatContainer.TryGetStat(trackedStat.DefinitionId, out var stat)) return;

            if (Mathf.FloorToInt(stat.EffectiveMaximum) != currentMax)
            {
                currentMax = Mathf.FloorToInt(stat.EffectiveMaximum);
                PopulateList(true);
            }

            currentValue = Mathf.FloorToInt(stat.CurrentValue);
            for (int i = 0; i < currentEntries.Count; i++)
            {
                var counter = currentEntries[i] as UI_SpecialResourceCounter;
                if (counter != null)
                {
                    counter.SetFilled(i < currentValue);
                }
            }
        }

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
            for (int i = 0; i < currentMax; i++)
            {
                var entry = Instantiate(counterPrefab, container.transform);
                entry.Initialize(trackedStat.Icon, index);
                entry.Register(this);
                index++;
            }
        }
    }
}