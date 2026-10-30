using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Networking;
using Game.Shared.Runtime.Spellcasting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
namespace Game.Shared.Data.AuraEffectDefinitions
{
    /// <summary>Applies healing to an aura target.</summary>
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    public class AuraEffect_Heal : AuraEffectDefinition
    {
        /// <summary>Gets the generated effect description.</summary>
        public override string Description => $"Healing {BaseHealing} health per tick.";

        /// <summary>Gets or sets the base healing amount.</summary>
        public float BaseHealing;
        /// <summary>Gets or sets stat scaling entries.</summary>
        public List<StatScaling> StatScaling = new();

        private const string STAT_HEALTH = "health";

        /// <summary>Executes healing for an aura target.</summary>
        public override void Execute(AuraContext ctx, Actor target)
        {
            if (target == null) return;

            var targetStats = target.GetComponent<ActorStatContainer>();
            if (targetStats == null) return;

            var casterStats = ctx.Caster != null ? ctx.Caster.GetComponent<ActorStatContainer>() : null;

            int stackCount = ctx.Definition.StackBehavior == AuraStackBehavior.IndependentDuration
                ? ctx.Stacks
                : 1;
            int healing = 0;
            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                healing += CalculateHealing(casterStats);
            if (healing <= 0) return;

            ApplyHealing(ctx.Caster, targetStats, target, healing);
            if (ctx.Caster != null)
                NotifyHealing(ctx.Caster, target, healing);
        }

        private void ApplyHealing(Actor source, ActorStatContainer targetStats, Actor target, int healing)
        {
            if (!targetStats.TryGetStat(STAT_HEALTH, out var health))
                return;

            float newCurrent = Mathf.Min(health.EffectiveMaximum, health.CurrentValue + healing);
            targetStats.SetStat(STAT_HEALTH, new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            var snapshot = targetStats.BuildSnapshot(ActorStatReplicationMode.Observers);
            targetStats.PublishObserverSnapshot(snapshot);
        }

        private void NotifyHealing(Actor source, Actor target, int healing)
        {
            source.PublishCombatLogToObservers(new CombatLogEntry
            {
                Source = source,
                Target = target,
                Amount = healing,
                EntryType = CombatHistoryEntryType.Heal,
                ResultType = CombatLogResultType.Hit,
            });
        }

        private int CalculateHealing(ActorStatContainer casterStats)
        {
            float healing = BaseHealing;
            if (casterStats != null)
            {
                foreach (var scaling in StatScaling)
                {
                    if (casterStats.TryGetStat(scaling.Stat.DefinitionID, out var stat))
                        healing += stat.CurrentValue * scaling.Coefficient;
                }
            }
            healing *= GaussianTable.Next();
            return Mathf.RoundToInt(healing);
        }
    }
}
