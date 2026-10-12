using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

using Game.Shared.Runtime.Actors;
namespace Game.Client.UI
{
    public sealed class UI_TargetResourceBar : UI_ProgressBar
    {
        [SerializeField] private List<ActorStatDefinition> trackedResources = new();
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text percentageText;

        private void Awake()
        {
            Game.Shared.Networking.ActorEndpoint.onClientLateTargetedActorChanged += OnClientLateTargetedActorChanged;
            Game.Shared.Networking.StatEndpoint.onStatsUpdated += OnStatsUpdated;
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            if (ClientAccountManager.PlayerActor == null) return;

            if (ClientAccountManager.PlayerActor.Target == actor)
            {
                foreach (var resourceDef in trackedResources)
                {
                    if (statContainer.TryGetStat(resourceDef.DefinitionID, out StatInstance stat))
                    {
                        float progress = stat.EffectiveMaximum > 0 ? stat.CurrentValue / stat.EffectiveMaximum : 0f;
                        SetProgress(progress);

                        if (valueText != null)
                            valueText.text = $"{stat.CurrentValue:0}";

                        if (percentageText != null)
                            percentageText.text = $"{progress * 100:0}%";

                        return;
                    }
                }

                SetProgress(0f);
            }
        }

        private void OnClientLateTargetedActorChanged(Game.Shared.Runtime.Actors.Actor oldTarget, Game.Shared.Runtime.Actors.Actor newTarget)
        {
            if (ClientAccountManager.PlayerActor == null) return;

            if (newTarget != null)
            {
                ActorStatContainer actorStatContainer = newTarget.GetComponent<ActorStatContainer>();

                foreach (var resourceDef in trackedResources)
                {
                    if (actorStatContainer.TryGetStat(resourceDef.DefinitionID, out StatInstance stat))
                    {
                        float progress = stat.EffectiveMaximum > 0 ? stat.CurrentValue / stat.EffectiveMaximum : 0f;
                        SetProgress(progress);

                        if (valueText != null)
                            valueText.text = $"{stat.CurrentValue:0}";

                        if (percentageText != null)
                            percentageText.text = $"{progress * 100:0}%";

                        Show();
                        return;
                    }
                }

                Hide();
            }
            else
            {
                Hide();
            }
        }

    }
}
