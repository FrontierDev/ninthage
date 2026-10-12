using Debug = Game.Shared.FormattedDebug;
using Game.Shared.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Calculates and applies healing to a spell target.</summary>
    public class SpellEffect_Heal : SpellEffectDefinition
    {
        /// <summary>Gets or sets the base healing amount.</summary>
        public float BaseHealing;
        /// <summary>Gets or sets the stat scaling entries.</summary>
        public List<StatScaling> StatScaling = new();

        /// <summary>Gets or sets whether healing uses a projectile.</summary>
        public bool UsesProjectile = false;
        /// <summary>Gets or sets the projectile addressable path.</summary>
        public string ProjectileAddressablePath;
        /// <summary>Gets or sets the projectile speed.</summary>
        public float ProjectileSpeed;

        /// <summary>Gets or sets whether healing applies an aura.</summary>
        public bool ApplyAura = false;
        /// <summary>Gets or sets the aura applied with healing.</summary>
        public AuraDefinition Aura;
        /// <summary>Gets or sets the aura stack count.</summary>
        public int AuraStacks = 1;

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        /// <summary>Gets the caster events raised by this effect.</summary>
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        /// <summary>Gets the target events raised by this effect.</summary>
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        /// <summary>Creates a copy of this effect.</summary>
        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Heal)MemberwiseClone();

            clone.BaseHealing = this.BaseHealing;
            clone.StatScaling = new(this.StatScaling);
            clone.UsesProjectile = this.UsesProjectile;
            clone.ProjectileAddressablePath = this.ProjectileAddressablePath;
            clone.ProjectileSpeed = this.ProjectileSpeed;
            clone.ApplyAura = this.ApplyAura;
            clone.Aura = this.Aura;
            clone.AuraStacks = this.AuraStacks;
            clone.casterEvents = new(this.casterEvents);
            clone.targetEvents = new(this.targetEvents);

            return clone;
        }

        /// <summary>Calculates and applies the configured healing.</summary>
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
                ActorEndpoint.onServerSpellCastDelayedAction?.Invoke(delay, () =>
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
                ActorEndpoint.onServerApplyAura?.Invoke(Aura, ctx.Caster, target, AuraStacks);

        }
    }
}
