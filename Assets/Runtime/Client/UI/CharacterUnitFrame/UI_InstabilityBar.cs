
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InstabilityBar</c> implementation.
    /// </summary>
    public class UI_InstabilityBar : UI_ProgressBar, ISpecialResourceBar
    {
        [SerializeField] private ActorStatContainer playerStatContainer;

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(Actor playerActor)
        {
            if (playerActor != null)
            {
                playerStatContainer = playerActor.GetComponent<ActorStatContainer>();
                if (playerStatContainer != null)
                {
                    StatEndpoint.onStatsUpdated += OnStatsUpdated;
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
