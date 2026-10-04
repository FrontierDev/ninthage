using System.Collections.Generic;
using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellEffect_Heal : SpellEffectDefinition
    {
        public float BaseHealing;
        public List<StatScaling> StatScaling = new();

        public bool UsesProjectile = false;
        public string ProjectileAddressablePath;
        public float ProjectileSpeed;

        public bool ApplyAura = false;
        public AuraDefinition Aura;
        public int AuraStacks = 1;

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Heal)MemberwiseClone();

            clone.BaseHealing = this.BaseHealing;
            clone.StatScaling = new List<StatScaling>(this.StatScaling);
            clone.UsesProjectile = this.UsesProjectile;
            clone.ProjectileAddressablePath = this.ProjectileAddressablePath;
            clone.ProjectileSpeed = this.ProjectileSpeed;
            clone.ApplyAura = this.ApplyAura;
            clone.Aura = this.Aura;
            clone.AuraStacks = this.AuraStacks;
            clone.casterEvents = new List<SpellActorEvent>(this.casterEvents);
            clone.targetEvents = new List<SpellActorEvent>(this.targetEvents);

            return clone;
        }

        public override void Execute(SpellContext ctx, Actor target)
        {
            // Calculate the healing now.
            int healing = CalculateHealing(ctx);

            // If this spell effect uses a projectile, we need to delay the healing application
            // until the projectile "hits" the target.
            if (UsesProjectile && ProjectileSpeed > 0f)
            {
                float distance = Vector3.Distance(ctx.Caster.transform.position, target.transform.position);
                float delay = distance / ProjectileSpeed;

                ctx.Caster.GetComponent<ActorVFXController>()
                    ?.Observer_SpawnProjectile(target, ProjectileAddressablePath, ProjectileSpeed);

                Actor capturedTarget = target;
                ActorService.onServerSpellCastDelayedAction?.Invoke(delay, () =>
                {
                    if (capturedTarget != null)
                        ApplyHealing(ctx, capturedTarget, healing);
                });
            }
            else
            {
                ApplyHealing(ctx, target, healing);
            }
        }

        private int CalculateHealing(SpellContext ctx)
        {
            float healing = BaseHealing;
            var casterStats = ctx.Caster.GetComponent<ActorStatContainer>();
            foreach (var scaling in StatScaling)
            {
                if (casterStats.TryGetStat(scaling.Stat.DefinitionId, out var stat))
                    healing += stat.CurrentValue * scaling.Coefficient;
            }
            healing *= GaussianTable.Next();
            return Mathf.RoundToInt(healing);
        }

        private void ApplyHealing(SpellContext ctx, Actor target, int healing)
        {
            var targetStats = target.GetComponent<ActorStatContainer>();
            if (!targetStats.TryGetStat("health", out var health))
                return;

            float newCurrent = Mathf.Min(health.EffectiveMaximum, health.CurrentValue + healing);
            targetStats.SetStat("health", new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            Debug.Log($"{ctx.Caster.name} healed {healing} health for {target.name}. " +
                      $"Target health: {newCurrent}/{health.EffectiveMaximum}");

            var snapshot = targetStats.BuildSnapshot(ActorStatReplicationMode.Observers);
            targetStats.Observers_ReceiveSnapshot(snapshot);

            // Receive the combat log on the observers of the caster.
            ctx.Caster.Observers_ReceivedCombatLogEntry(new CombatLogEntry
            {
                Source = ctx.Caster,
                Target = target,
                Amount = healing,
                EntryType = CombatHistoryEntryType.Heal,
                ResultType = CombatLogResultType.Hit,
            });

            // Apply aura if applicable
            if (ApplyAura && Aura != null)
                ActorService.onServerApplyAura?.Invoke(Aura, ctx.Caster, target, AuraStacks);

        }
    }
}