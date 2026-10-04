
using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public class UI_InstabilityBar : UI_ProgressBar, ISpecialResourceBar
    {
        [SerializeField] private ActorStatContainer playerStatContainer;

        public void Initialize(Actor playerActor)
        {
            if (playerActor != null)
            {
                playerStatContainer = playerActor.GetComponent<ActorStatContainer>();
                if (playerStatContainer != null)
                {
                    StatService.onStatsUpdated += OnStatsUpdated;
                    SetMaterial(graphics.material);
                    SetProgress(0f);
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

            if (playerStatContainer.TryGetStat("instability", out var stat))
            {
                var currentValue = stat.CurrentValue;
                var maxValue = stat.EffectiveMaximum;

                SetProgress(currentValue / maxValue);
                return;
            }
        }
    }
}
