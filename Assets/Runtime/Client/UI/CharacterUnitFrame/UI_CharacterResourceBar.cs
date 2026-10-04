using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public class UI_CharacterResourceBar : UI_ProgressBar
    {
        [SerializeField] private ActorStatContainer playerStatContainer;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text percentageText;
        [SerializeField] private List<ActorStatDefinition> trackedResources = new();

        private void Awake()
        {
            Game.Shared.Networking.ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            SetMaterial(graphics.material);
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            if (playerActor != null)
            {
                playerStatContainer = playerActor.GetComponent<ActorStatContainer>();
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

        private void Refresh()
        {
            if (playerStatContainer == null) return;

            // For now we just display the first resource stat we find, if any.
            foreach (var def in trackedResources)
            {
                if (playerStatContainer.TryGetStat(def.DefinitionId, out var stat))
                {
                    var currentValue = stat.CurrentValue;
                    var maxValue = stat.EffectiveMaximum;

                    SetProgress(currentValue / maxValue);
                    if (valueText != null)
                        valueText.text = $"{currentValue:0}";

                    if (percentageText != null)
                        percentageText.text = $"{(maxValue > 0 ? (currentValue / maxValue) * 100 : 0):0}%";

                    return;
                }
            }
        }
    }
}