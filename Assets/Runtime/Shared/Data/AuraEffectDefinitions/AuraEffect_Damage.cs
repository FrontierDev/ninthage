using System.Collections.Generic;
using System.Linq;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class AuraEffect_Damage : AuraEffectDefinition
    {
        public override string Description => $"Taking {BaseDamage} {string.Join("/", DamageSchools.Select(ds => ds.DisplayName))} damage.";

        public float BaseDamage;
        public List<StatScaling> StatScaling = new();
        public List<DamageSchoolDefinition> DamageSchools = new();

        private const string StatHealth = "health";

        public override void Execute(AuraContext ctx, Actor target)
        {
            if (target == null) return;

            var targetStats = target.GetComponent<ActorStatContainer>();
            if (targetStats == null) return;

            // Condition auras: each stack's damage scales from its own caster's stats.
            if (ctx.ConditionStacks != null && ctx.ConditionStacks.Count > 0)
            {
                ExecuteConditionDamage(ctx, target, targetStats);
                return;
            }

            var casterStats = ctx.Caster != null ? ctx.Caster.GetComponent<ActorStatContainer>() : null;

            int damage = CalculateDamage(casterStats);
            damage = Mathf.RoundToInt(damage * CalculateMitigation(targetStats));
            if (damage <= 0) return;

            ApplyDamage(ctx.Caster, targetStats, target, damage);
            if (ctx.Caster != null)
                NotifyDamage(ctx.Caster, target, targetStats, damage);
        }

        private void ExecuteConditionDamage(AuraContext ctx, Actor target, ActorStatContainer targetStats)
        {
            // Group stacks by caster to batch damage and combat log entries.
            var casterDamage = new Dictionary<Actor, int>();

            foreach (var stack in ctx.ConditionStacks)
            {
                if (stack.Caster == null) continue;
                var casterStats = stack.Caster.GetComponent<ActorStatContainer>();
                int damage = CalculateDamage(casterStats);
                damage = Mathf.RoundToInt(damage * CalculateMitigation(targetStats));
                if (damage <= 0) continue;

                if (casterDamage.ContainsKey(stack.Caster))
                    casterDamage[stack.Caster] += damage;
                else
                    casterDamage[stack.Caster] = damage;
            }

            // Apply each caster's damage separately so threat/aggro is attributed correctly.
            foreach (var kvp in casterDamage)
            {
                if (kvp.Value <= 0) continue;
                ApplyDamage(kvp.Key, targetStats, target, kvp.Value);
                NotifyDamage(kvp.Key, target, targetStats, kvp.Value);
            }
        }

        private void ApplyDamage(Actor source, ActorStatContainer targetStats, Actor target, int damage)
        {
            if (!targetStats.TryGetStat(StatHealth, out var health))
                return;

            float newCurrent = Mathf.Max(0f, health.CurrentValue - damage);
            targetStats.SetStat(StatHealth, new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            if (target.TryGetComponent<NPCBehavior>(out var npcBehavior))
                npcBehavior.OnDamaged(source, damage);

            var snapshot = targetStats.BuildSnapshot(ActorStatReplicationMode.Observers);
            targetStats.Observers_ReceiveSnapshot(snapshot);
        }

        private void NotifyDamage(Actor source, Actor target, ActorStatContainer targetStats, int damage)
        {
            source.Observers_ReceivedCombatLogEntry(new CombatLogEntry
            {
                Source = source,
                Target = target,
                Amount = damage,
                EntryType = CombatHistoryEntryType.Damage,
                ResultType = CombatLogResultType.Hit,
                HitType = SpellHitType.Ability,
                DamageTypes = DamageSchools
            });
        }

        private int CalculateDamage(ActorStatContainer casterStats)
        {
            float damage = BaseDamage;
            if (casterStats != null)
            {
                foreach (var scaling in StatScaling)
                {
                    if (casterStats.TryGetStat(scaling.Stat.DefinitionId, out var stat))
                        damage += stat.CurrentValue * scaling.Coefficient;
                }
            }
            damage *= GaussianTable.Next();
            return Mathf.RoundToInt(damage);
        }

        private float CalculateMitigation(ActorStatContainer targetStats)
        {
            float maxMitigation = 0f;
            for (int i = 0; i < DamageSchools.Count; i++)
                maxMitigation = Mathf.Max(maxMitigation, DamageSchools[i].CalculateMitigation(targetStats));
            return 1f - maxMitigation;
        }
    }
}