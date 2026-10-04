using System.Collections.Generic;
using System.Linq;
using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class AuraEffect_Heal : AuraEffectDefinition
    {
        public override string Description => $"Healing {BaseHealing} health per tick.";

        public float BaseHealing;
        public List<StatScaling> StatScaling = new();

        private const string StatHealth = "health";

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
            if (!targetStats.TryGetStat(StatHealth, out var health))
                return;

            float newCurrent = Mathf.Min(health.EffectiveMaximum, health.CurrentValue + healing);
            targetStats.SetStat(StatHealth, new StatInstance(
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
                    if (casterStats.TryGetStat(scaling.Stat.DefinitionId, out var stat))
                        healing += stat.CurrentValue * scaling.Coefficient;
                }
            }
            healing *= GaussianTable.Next();
            return Mathf.RoundToInt(healing);
        }
    }
}
