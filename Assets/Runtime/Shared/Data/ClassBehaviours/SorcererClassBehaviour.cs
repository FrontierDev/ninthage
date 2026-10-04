using System.Collections.Generic;
using System.Linq;
using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    // SorcererClassBehaviour.cs — one asset handles all sorcerer passives
    [CreateAssetMenu(menuName = "NinthAge/Class Behaviours/Sorcerer")]
    public class SorcererClassBehaviour : ClassBehaviour
    {
        private const float UnstableThreshold = 0.4f;
        private const float CriticalThreshold = 0.8f;
        private const float UnstableDamagePerTick = 1f;
        private const float CriticalDamagePerTick = 5f;
        private const float DecayDelay = 6f;      // seconds after last instability cast before decay begins
        private const float DecayPerSecond = 3f;   // instability lost per second whilst decaying
        private const float UnstableSurgeRatio = 1.5f;
        private const float CriticalSurgeRatio = 3f;
        private const float SurgeDecayPerSecond = 0.03f; // 3% of max per second

        [SerializeField] private DamageSchoolDefinition instabilityDamageSchool;

        // Per-actor timestamp of the last instability-generating spell cast.
        // ScriptableObject is shared, so we key by actor instance.
        private readonly Dictionary<Actor, float> _lastInstabilityTime = new();
        private readonly Dictionary<Actor, float> _lastSpellCastTime = new();

        private struct RiftData
        {
            public Vector3 Position;
            public float Radius;
            public int VFXId;
        }
        private readonly Dictionary<Actor, RiftData> _activeRifts = new();

        [SerializeField] private GameObject resourcePrefab;
        public override GameObject ResourcePrefab => resourcePrefab;

        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerSpellCastComplete += OnServerSpellCastComplete;

            // Start a permanent 1s monitor ticker that handles instability decay.
            _lastInstabilityTime[actor] = float.MaxValue; // no cast yet — decay is immediately eligible but instability is 0 so no-ops
            ActorService.onServerAddTicker?.Invoke(actor, 1f, float.MaxValue, "sorcerer_instability_decay",
                OnInstabilityDecayTick, null);

            // Start a permanent 1s ticker that decays surge when not casting.
            _lastSpellCastTime[actor] = float.MaxValue;
            ActorService.onServerAddTicker?.Invoke(actor, 1f, float.MaxValue, "sorcerer_surge_decay",
                OnSurgeDecayTick, null);
        }

        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerSpellCastComplete -= OnServerSpellCastComplete;

            _lastInstabilityTime.Remove(actor);
            _lastSpellCastTime.Remove(actor);
            _activeRifts.Remove(actor);

            // Remove any active instability/surge tickers.
            ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_instability_decay");
            ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_unstable");
            ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_critical");
            ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_surge_decay");
            ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_rift");
        }

        private void OnServerSpellCastComplete(SpellContext ctx)
        {
            // Generate instability if the spell has the "instability" tag. 
            if (ctx.Spell.BaseDefinition.Tags.Any(tag => tag == "instability"))
            {
                GenerateInstability(ctx.Caster, 10f);
            }

            // Generate surge based on mana cost and current instability.
            GenerateSurgeFromSpell(ctx);
            _lastSpellCastTime[ctx.Caster] = Time.time;

            // If the spell has the "rift" tag, pulse an existing rift if present 
            // (placing a new one is handled by a separate spell effect that calls PlaceRift).
            if (ctx.Spell.BaseDefinition.Tags.Any(tag => tag == "pulse_rift"))
            {
                if (_activeRifts.TryGetValue(ctx.Caster, out var rift))
                    PulseRift(ctx.Caster, rift);
            }
        }

        /// <summary>
        /// Registers a Rift.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="position"></param>
        /// <param name="duration"></param>
        /// <param name="radius"></param>
        /// <param name="vfxId"></param>
        public void RegisterRift(Actor caster, Vector3 position, float duration, float radius, int vfxId)
        {
            _activeRifts[caster] = new RiftData { Position = position, Radius = radius, VFXId = vfxId };

            // Replace any existing rift ticker.
            ActorService.onServerRemoveTickerByTag?.Invoke(caster, "sorcerer_rift");
            ActorService.onServerAddTicker?.Invoke(caster, duration, duration, "sorcerer_rift",
                null, a => _activeRifts.Remove(a));
        }

        /// <summary>
        /// Applies rift damage to all valid targets within the active rift area.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="rift"></param>
        private void PulseRift(Actor caster, RiftData rift)
        {
            var colliders = Physics.OverlapSphere(rift.Position, rift.Radius);
            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<Actor>();
                if (target == null || target == caster || target.isDead) continue;
                ApplyRiftPulseDamage(caster, target);
            }

            caster.GetComponent<ActorVFXController>()?.Observer_TriggerEffect(rift.VFXId);
        }


        /// <summary>
        /// Applies rift damage to a target actor.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private static void ApplyRiftPulseDamage(Actor source, Actor target)
        {
            var targetStats = target.GetComponent<ActorStatContainer>();
            if (targetStats == null) return;
            if (!targetStats.TryGetStat("health", out var health)) return;

            const float damage = 10f;
            float newCurrent = Mathf.Max(0f, health.CurrentValue - damage);
            targetStats.SetStat("health", new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            targetStats.Observers_ReceiveSnapshot(targetStats.BuildSnapshot(ActorStatReplicationMode.Observers));
        }

        [ToDo("Increase the actor's haste rating according to their surge.")]
        /// <summary>
        /// Generates surge based on the mana cost of a spell and the caster's current instability.
        /// </summary>
        /// <param name="ctx"></param>
        private void GenerateSurgeFromSpell(SpellContext ctx)
        {
            var actor = ctx.Caster;
            var statContainer = actor.GetComponent<ActorStatContainer>();
            if (statContainer == null) return;

            // Determine current instability tier.
            var instability = statContainer.GetStat("instability");
            float maxInstability = instability.EffectiveMaximum > 0f ? instability.EffectiveMaximum : 100f;
            float instabilityPercent = instability.CurrentValue / maxInstability;

            float surgeRatio;
            if (instabilityPercent >= CriticalThreshold)
                surgeRatio = CriticalSurgeRatio;
            else if (instabilityPercent >= UnstableThreshold)
                surgeRatio = UnstableSurgeRatio;
            else
                return; // No surge outside of unstable/critical.

            // Sum all mana costs that were paid at OnCastEnd (the phase that just completed).
            float totalManaCost = 0f;
            foreach (var cost in ctx.Spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnCastEnd) continue;
                if (cost.ResourceType.DefinitionId != "mana") continue;
                totalManaCost += cost.Amount;
            }

            if (totalManaCost <= 0f) return;

            float surgeGain = totalManaCost * surgeRatio;

            if (!statContainer.TryGetStat("surge", out var surge)) return;

            float newSurge = Mathf.Min(surge.CurrentValue + surgeGain, surge.EffectiveMaximum);
            statContainer.SetStat("surge", new StatInstance(
                surge.BaseValue, surge.FlatModifier, surge.PercentModifier,
                surge.EffectiveMaximum, newSurge));

            var surgeDef = ActorStatDefinitionLibrary.Instance.GetDefinition("surge");
            if (surgeDef.ReplicationMode == ActorStatReplicationMode.Observers)
                statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (surgeDef.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.Client_ReceiveSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));

            Debug.Log($"Surge generated: {surgeGain} ({totalManaCost} mana × {surgeRatio} ratio) for {actor.Name}");
        }

        /// <summary>
        /// Generates instability for the caster, applying appropriate tickers for 
        /// damage over time and state changes.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="amount"></param>
        private void GenerateInstability(Actor actor, float amount)
        {
            Debug.Log($"Generating {amount} instability for {actor.name}");

            _lastInstabilityTime[actor] = Time.time;

            var statContainer = actor.GetComponent<ActorStatContainer>();
            var stat = statContainer.GetStat("instability");
            var def = ActorStatDefinitionLibrary.Instance.GetDefinition("instability");

            float maxInstability = stat.EffectiveMaximum > 0f ? stat.EffectiveMaximum : 100f;
            float oldPercent = stat.CurrentValue / maxInstability;
            float newCurrent = Mathf.Clamp(stat.CurrentValue + amount, 0f, stat.EffectiveMaximum);
            float newPercent = newCurrent / maxInstability;

            statContainer.SetStat("instability", new StatInstance(
                stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                stat.EffectiveMaximum, newCurrent));

            if (def.ReplicationMode == ActorStatReplicationMode.Observers)
                statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.Client_ReceiveSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));

            UpdateInstabilityState(actor, oldPercent, newPercent);
        }

        /// <summary>
        /// Updates the actor's state based on their current instability percentage, 
        /// adding or removing tickers as necessary.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="oldPercent"></param>
        /// <param name="newPercent"></param>
        private void UpdateInstabilityState(Actor actor, float oldPercent, float newPercent)
        {
            bool wasCritical = oldPercent >= CriticalThreshold;
            bool wasUnstable = oldPercent >= UnstableThreshold && !wasCritical;
            bool nowCritical = newPercent >= CriticalThreshold;
            bool nowUnstable = newPercent >= UnstableThreshold && !nowCritical;

            // Remove states that no longer apply.
            if (wasCritical && !nowCritical)
                ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_critical");
            if (wasUnstable && !nowUnstable)
                ActorService.onServerRemoveTickerByTag?.Invoke(actor, "sorcerer_unstable");

            // Add states that are newly entered.
            if (nowCritical && !wasCritical)
                ActorService.onServerAddTicker?.Invoke(actor, 1f, float.MaxValue, "sorcerer_critical",
                    a => ApplyInstabilityDamage(a, CriticalDamagePerTick), null);
            if (nowUnstable && !wasUnstable)
                ActorService.onServerAddTicker?.Invoke(actor, 1f, float.MaxValue, "sorcerer_unstable",
                    a => ApplyInstabilityDamage(a, UnstableDamagePerTick), null);
        }

        /// <summary>
        /// Handles instability decay.
        /// </summary>
        /// <param name="actor"></param>
        private void OnInstabilityDecayTick(Actor actor)
        {
            if (actor == null) return;
            if (!_lastInstabilityTime.TryGetValue(actor, out float lastTime)) return;
            if (Time.time - lastTime < DecayDelay) return;

            var statContainer = actor.GetComponent<ActorStatContainer>();
            if (statContainer == null) return;

            var stat = statContainer.GetStat("instability");
            if (stat.CurrentValue <= 0f) return;

            var def = ActorStatDefinitionLibrary.Instance.GetDefinition("instability");
            float maxInstability = stat.EffectiveMaximum > 0f ? stat.EffectiveMaximum : 100f;
            float oldPercent = stat.CurrentValue / maxInstability;
            float newCurrent = Mathf.Max(0f, stat.CurrentValue - DecayPerSecond);
            float newPercent = newCurrent / maxInstability;

            statContainer.SetStat("instability", new StatInstance(
                stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                stat.EffectiveMaximum, newCurrent));

            if (def.ReplicationMode == ActorStatReplicationMode.Observers)
                statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.Client_ReceiveSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));

            UpdateInstabilityState(actor, oldPercent, newPercent);
        }

        /// <summary>
        /// Handles surge decay when not casting, based on time since last spell cast.
        /// </summary>
        /// <param name="actor"></param>
        private void OnSurgeDecayTick(Actor actor)
        {
            if (actor == null) return;
            if (!_lastSpellCastTime.TryGetValue(actor, out float lastCast)) return;
            if (Time.time - lastCast < DecayDelay) return;

            var statContainer = actor.GetComponent<ActorStatContainer>();
            if (statContainer == null) return;

            if (!statContainer.TryGetStat("surge", out var surge)) return;
            if (surge.CurrentValue <= 0f) return;

            float decayAmount = surge.EffectiveMaximum * SurgeDecayPerSecond;
            float newSurge = Mathf.Max(0f, surge.CurrentValue - decayAmount);

            statContainer.SetStat("surge", new StatInstance(
                surge.BaseValue, surge.FlatModifier, surge.PercentModifier,
                surge.EffectiveMaximum, newSurge));

            var surgeDef = ActorStatDefinitionLibrary.Instance.GetDefinition("surge");
            if (surgeDef.ReplicationMode == ActorStatReplicationMode.Observers)
                statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (surgeDef.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.Client_ReceiveSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
        }

        [ToDo("Base instability damage on something other than a hardcoded value, and include damage type/school.")]
        /// <summary>
        /// Deals instability damage to the actor, called by tickers when in unstable/critical states.
        /// </summary>
        /// <param name="actor">The actor receiving instability damage.</param>
        /// <param name="damage">The amount of instability damage to apply.</param>
        private void ApplyInstabilityDamage(Actor actor, float damage)
        {
            if (actor == null) return;
            var statContainer = actor.GetComponent<ActorStatContainer>();
            if (statContainer == null) return;
            if (!statContainer.TryGetStat("health", out var health)) return;

            float newCurrent = Mathf.Max(0f, health.CurrentValue - damage);
            statContainer.SetStat("health", new StatInstance(
                health.BaseValue, health.FlatModifier, health.PercentModifier,
                health.EffectiveMaximum, newCurrent));

            actor.Client_ReceiveCombatLogEntry(actor.Owner.Value, new CombatLogEntry
            {
                Source = actor,
                Target = actor,
                Amount = (int)damage,
                EntryType = CombatHistoryEntryType.Damage,
                ResultType = CombatLogResultType.Hit,
                HitType = SpellHitType.Ability,
                DamageTypes = instabilityDamageSchool != null
                    ? new List<DamageSchoolDefinition> { instabilityDamageSchool }
                    : new List<DamageSchoolDefinition>()
            });

            statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
        }
    }
}