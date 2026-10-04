using System.Collections.Generic;
using System.Linq;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    public enum SpellWeaponDamageMode
    {
        None,
        MainHand,
        OffHand,
        Both
    }

    [System.Serializable]
    public class SpellEffect_Damage : SpellEffectDefinition
    {
        public float BaseDamage;
        public SpellWeaponDamageMode WeaponDamageMode = SpellWeaponDamageMode.None;
        public List<StatScaling> StatScaling = new();
        public List<DamageSchoolDefinition> DamageSchools = new();
        public SpellHitType HitType = SpellHitType.Auto;
        public SpellDamageType DamageType = SpellDamageType.Spell;
        public bool AlwaysHits = false;

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

        private const string StatDodgeChance = "dodge_chance";
        private const string StatParryChance = "parry_chance";
        private const string StatBlockChance = "block_chance";
        private const string StatSpellResistance = "spell_resistance";
        private const string StatSpellExpertise = "spell_expertise";
        private const string StatHealth = "health";

        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Damage)MemberwiseClone();
            clone.BaseDamage = this.BaseDamage;
            clone.WeaponDamageMode = this.WeaponDamageMode;
            clone.HitType = this.HitType;
            clone.DamageType = this.DamageType;
            clone.AlwaysHits = this.AlwaysHits;
            clone.UsesProjectile = this.UsesProjectile;
            clone.ProjectileAddressablePath = this.ProjectileAddressablePath;
            clone.ProjectileSpeed = this.ProjectileSpeed;
            clone.ApplyAura = this.ApplyAura;
            clone.Aura = this.Aura; // If AuraDefinition is mutable, consider cloning it too
            clone.AuraStacks = this.AuraStacks;

            // Deep copy lists
            clone.StatScaling = this.StatScaling
                .Select(s => new StatScaling
                {
                    Stat = s.Stat, // If Stat is a reference type, clone if needed
                    Coefficient = s.Coefficient
                }).ToList();

            clone.DamageSchools = this.DamageSchools
                .Select(ds => ds) // If DamageSchoolDefinition is mutable, clone if needed
                .ToList();

            clone.casterEvents = new List<SpellActorEvent>(this.casterEvents);
            clone.targetEvents = new List<SpellActorEvent>(this.targetEvents);

            return clone;
        }

        public override void Execute(SpellContext ctx, Actor target)
        {
            var casterStats = ctx.Caster.GetComponent<ActorStatContainer>();
            int damage = CalculateDamage(casterStats);

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
                        ApplyDamage(ctx, casterStats, capturedTarget, damage);
                });
            }
            else
            {
                ApplyDamage(ctx, casterStats, target, damage);
            }
        }

        private int CalculateDamage(ActorStatContainer casterStats)
        {
            float damage = BaseDamage;

            switch (WeaponDamageMode)
            {
                case SpellWeaponDamageMode.MainHand:
                    damage += casterStats.GetComponent<Actor>().GetWeaponDamage(ItemSlot.MainHand);
                    break;
                case SpellWeaponDamageMode.OffHand:
                    damage += casterStats.GetComponent<Actor>().GetWeaponDamage(ItemSlot.OffHand);
                    break;
                case SpellWeaponDamageMode.Both:
                    damage += casterStats.GetComponent<Actor>().GetWeaponDamage(ItemSlot.MainHand);
                    damage += casterStats.GetComponent<Actor>().GetWeaponDamage(ItemSlot.OffHand);
                    break;
            }

            foreach (var scaling in StatScaling)
            {
                if (casterStats.TryGetStat(scaling.Stat.DefinitionId, out var stat))
                    damage += stat.CurrentValue * scaling.Coefficient;
            }
            damage *= GaussianTable.Next();

            return Mathf.RoundToInt(damage);
        }

        private void ApplyDamage(SpellContext ctx, ActorStatContainer casterStats, Actor target, int damage)
        {
            var targetStats = target.GetComponent<ActorStatContainer>();
            int casterLevel = casterStats.GetComponent<Actor>().GetLevel();
            int targetLevel = target.GetLevel();

            if (!IsHit(casterStats, casterLevel, targetLevel))
            {
                ctx.Caster.Observers_ReceivedCombatLogEntry(new CombatLogEntry
                {
                    Source = ctx.Caster,
                    Target = target,
                    Amount = 0,
                    EntryType = CombatHistoryEntryType.Damage,
                    ResultType = CombatLogResultType.Miss,
                    HitType = HitType,
                    DamageTypes = DamageSchools
                });
                return;
            }

            if (IsResisted(casterStats, targetStats, out var resistResult))
            {
                ctx.Caster.Observers_ReceivedCombatLogEntry(new CombatLogEntry
                {
                    Source = ctx.Caster,
                    Target = target,
                    Amount = 0,
                    EntryType = CombatHistoryEntryType.Damage,
                    ResultType = resistResult,
                    HitType = HitType,
                    DamageTypes = DamageSchools
                });
                return;
            }

            damage = Mathf.RoundToInt(damage * CalculateMitigation(targetStats));

            // Check for critical hit
            bool isCrit = IsCriticalHit(casterStats, targetStats, casterLevel, targetLevel);
            if (isCrit)
                damage = Mathf.RoundToInt(damage * GameConfigurationManager.Config.CritDamageMultiplier);

            // Subtract absorption.
            // TO DO

            // Apply damage to target's health
            if (!targetStats.TryGetStat(StatHealth, out var health))
                return;

            float newCurrent = Mathf.Max(0f, health.CurrentValue - damage);
            targetStats.SetStat(StatHealth, new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            if (target.TryGetComponent<NPCBehavior>(out var npcBehavior))
                npcBehavior.onDamaged?.Invoke(ctx.Caster, damage);

            // Broadcast combat log entry to caster's observers
            var snapshot = targetStats.BuildSnapshot(ActorStatReplicationMode.Observers);
            targetStats.Observers_ReceiveSnapshot(snapshot);

            // Invoke events on the caster based on the hit result
            InvokeEvents(SpellActorEventTarget.Caster, ctx, target);
            InvokeEvents(SpellActorEventTarget.Target, ctx, target);

            ctx.Caster.Observers_ReceivedCombatLogEntry(new CombatLogEntry
            {
                Source = ctx.Caster,
                Target = target,
                Amount = damage,
                EntryType = CombatHistoryEntryType.Damage,
                ResultType = isCrit ? CombatLogResultType.CriticalHit : CombatLogResultType.Hit,
                HitType = HitType,
                DamageTypes = DamageSchools
            });

            // Apply aura if applicable
            if (ApplyAura && Aura != null)
                ActorService.onServerApplyAura?.Invoke(Aura, ctx.Caster, target, AuraStacks);
        }

        private void InvokeEvents(SpellActorEventTarget mode, SpellContext ctx, Actor target)
        {
            // Fire configured actor events on the specified target
            var events = mode == SpellActorEventTarget.Caster ? CasterEvents : TargetEvents;
            if (events.Count > 0)
            {
                var actorEvents = (mode == SpellActorEventTarget.Caster ? ctx.Caster : target).GetComponent<ActorEvents>();
                if (actorEvents != null)
                {
                    foreach (var evt in events)
                    {
                        switch (evt)
                        {
                            case SpellActorEvent.onAutoAttackHit:
                                actorEvents.onServerAutoAttackHit?.Invoke(ctx, target);
                                break;
                            case SpellActorEvent.onAutoAttackTaken:
                                actorEvents.onServerAutoAttackTaken?.Invoke(ctx, target);
                                break;
                        }
                    }
                }
            }
        }

        private bool IsHit(ActorStatContainer casterStats, int casterLevel, int targetLevel)
        {
            if (AlwaysHits) return true;

            ActorStatDefinition hitChanceStat = DamageType switch
            {
                SpellDamageType.Melee => GameConfigurationManager.Config.MeleeHitChanceStat,
                SpellDamageType.Ranged => GameConfigurationManager.Config.RangedHitChanceStat,
                SpellDamageType.Spell => GameConfigurationManager.Config.SpellHitChanceStat,
                _ => null
            };

            float hitPercent = hitChanceStat != null
                ? casterStats.GetStat(hitChanceStat.DefinitionId)?.CurrentValue ?? 0f
                : 0f;

            var config = GameConfigurationManager.Config;
            int levelDiff = Mathf.Max(0, targetLevel - casterLevel);
            float missChance = config.BaseMissChance + (levelDiff * config.MissChancePerLevelPenalty) - hitPercent;
            missChance = Mathf.Clamp(missChance, 0f, config.MaximumMissChance);

            return Random.value * 100f >= missChance;
        }

        private float CalculateMitigation(ActorStatContainer targetStats)
        {
            float maxMitigation = 0f;
            for (int i = 0; i < DamageSchools.Count; i++)
                maxMitigation = Mathf.Max(maxMitigation, DamageSchools[i].CalculateMitigation(targetStats));
            return 1f - maxMitigation;
        }

        private bool IsResisted(ActorStatContainer casterStats, ActorStatContainer targetStats, out CombatLogResultType resultType)
        {
            resultType = CombatLogResultType.Miss;

            switch (DamageType)
            {
                case SpellDamageType.Melee:
                    {
                        float dodge = targetStats.TryGetStat(StatDodgeChance, out var dodgeStat) ? dodgeStat.CurrentValue : 5f;
                        float parry = targetStats.TryGetStat(StatParryChance, out var parryStat) ? parryStat.CurrentValue : 5f;
                        float block = targetStats.TryGetStat(StatBlockChance, out var blockStat) ? blockStat.CurrentValue : 5f;
                        float total = dodge + parry + block;
                        if (total <= 0f) return false;

                        float pick = Random.value * total;
                        float chance;
                        if (pick < dodge)
                        {
                            resultType = CombatLogResultType.Dodged;
                            chance = dodge;
                        }
                        else if (pick < dodge + parry)
                        {
                            resultType = CombatLogResultType.Parried;
                            chance = parry;
                        }
                        else
                        {
                            resultType = CombatLogResultType.Blocked;
                            chance = block;
                        }

                        return Random.value * 100f < chance;
                    }

                case SpellDamageType.Ranged:
                    {
                        float dodge = targetStats.TryGetStat(StatDodgeChance, out var dodgeStat) ? dodgeStat.CurrentValue : 5f;
                        float block = targetStats.TryGetStat(StatBlockChance, out var blockStat) ? blockStat.CurrentValue : 5f;
                        float total = dodge + block;
                        if (total <= 0f) return false;

                        float pick = Random.value * total;
                        float chance;
                        if (pick < dodge)
                        {
                            resultType = CombatLogResultType.Dodged;
                            chance = dodge;
                        }
                        else
                        {
                            resultType = CombatLogResultType.Blocked;
                            chance = block;
                        }

                        return Random.value * 100f < chance;
                    }

                case SpellDamageType.Spell:
                    {
                        float resist = targetStats.TryGetStat(StatSpellResistance, out var resistStat) ? resistStat.CurrentValue : 5f;
                        float expertise = casterStats.TryGetStat(StatSpellExpertise, out var expertiseStat) ? expertiseStat.CurrentValue : 5f;
                        float finalResist = Mathf.Clamp(resist - expertise, 0f, 100f);
                        if (Random.value * 100f < finalResist)
                        {
                            resultType = CombatLogResultType.Resisted;
                            return true;
                        }
                        return false;
                    }
            }

            return false;
        }

        private bool IsCriticalHit(ActorStatContainer casterStats, ActorStatContainer targetStats, int casterLevel, int targetLevel)
        {
            ActorStatDefinition critChanceStat = DamageType switch
            {
                SpellDamageType.Melee => GameConfigurationManager.Config.MeleeCritChanceStat,
                SpellDamageType.Ranged => GameConfigurationManager.Config.RangedCritChanceStat,
                SpellDamageType.Spell => GameConfigurationManager.Config.SpellCritChanceStat,
                _ => null
            };

            var config = GameConfigurationManager.Config;
            float critPercent = critChanceStat != null
                ? casterStats.GetStat(critChanceStat.DefinitionId)?.CurrentValue ?? 0f
                : 0f;

            float critResistance = config.CritResistanceStat != null
                ? targetStats.GetStat(config.CritResistanceStat.DefinitionId)?.CurrentValue ?? 0f
                : 0f;
            critPercent = Mathf.Max(0f, critPercent - critResistance);

            int levelDiff = Mathf.Max(0, targetLevel - casterLevel);
            float critChance = config.BaseCritChance + critPercent - (levelDiff * config.CritChancePerLevelPenalty);
            critChance = Mathf.Clamp(critChance, 0f, 100f);

            return Random.value * 100f < critChance;
        }
    }
}