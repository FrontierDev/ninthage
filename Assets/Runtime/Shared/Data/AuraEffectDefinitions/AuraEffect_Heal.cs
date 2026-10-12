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

            // Condition auras: each stack's healing scales from its own caster's stats.
            if (ctx.ConditionStacks != null && ctx.ConditionStacks.Count > 0)
            {
                ExecuteConditionHealing(ctx, target, targetStats);
                return;
            }

            var casterStats = ctx.Caster != null ? ctx.Caster.GetComponent<ActorStatContainer>() : null;

            int healing = CalculateHealing(casterStats);
            if (healing <= 0) return;

            ApplyHealing(ctx.Caster, targetStats, target, healing);
            if (ctx.Caster != null)
                NotifyHealing(ctx.Caster, target, healing);
        }

        private void ExecuteConditionHealing(AuraContext ctx, Actor target, ActorStatContainer targetStats)
        {
            // Group stacks by caster to batch healing and combat log entries.
            var casterHealing = new Dictionary<Actor, int>();

            foreach (var stack in ctx.ConditionStacks)
            {
                if (stack.Caster == null) continue;
                var casterStats = stack.Caster.GetComponent<ActorStatContainer>();
                int healing = CalculateHealing(casterStats);
                if (healing <= 0) continue;

                if (casterHealing.ContainsKey(stack.Caster))
                    casterHealing[stack.Caster] += healing;
                else
                    casterHealing[stack.Caster] = healing;
            }

            // Apply each caster's healing separately so threat/credit is attributed correctly.
            foreach (var kvp in casterHealing)
            {
                if (kvp.Value <= 0) continue;
                ApplyHealing(kvp.Key, targetStats, target, kvp.Value);
                NotifyHealing(kvp.Key, target, kvp.Value);
            }
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
            targetStats.Observers_ReceiveSnapshot(snapshot);
        }

        private void NotifyHealing(Actor source, Actor target, int healing)
        {
            source.Observers_ReceivedCombatLogEntry(new CombatLogEntry
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
