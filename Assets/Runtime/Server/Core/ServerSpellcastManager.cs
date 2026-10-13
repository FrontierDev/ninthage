using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Services;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Server
{
    /// <summary>Maintains server-authoritative spell casts and effects.</summary>
    public sealed class ServerSpellCastManager : MonoBehaviour
    {
        private static ServerSpellCastManager _instance;
        /// <summary>Gets the active server spell-cast manager.</summary>
        public static ServerSpellCastManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the spell-cast manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        // Store active spell casts by actor ID for quick access during ticks and completion.
        private readonly Dictionary<Guid, SpellContext> _activeSpellCasts = new();
        private readonly List<(float RemainingTime, Action Callback)> _pendingActions = new();


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerSpellCastManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ECS.ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

            ActorEndpoint.onServerSpellCastStarted += OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick += OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted += OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastDelayedAction += OnSpellCastDelayedAction;
            ActorEndpoint.onServerSpellCastInterrupted += OnSpellCastInterrupted;
            ActorEndpoint.onServerRequestSpellCastCancel += OnSpellCastCancelRequested;

            _initialized = true;
        }

        private void Update()
        {
            for (int i = _pendingActions.Count - 1; i >= 0; i--)
            {
                var (time, callback) = _pendingActions[i];
                time -= Time.deltaTime;
                if (time <= 0f)
                {
                    _pendingActions.RemoveAt(i);
                    callback?.Invoke();
                }
                else
                {
                    _pendingActions[i] = (time, callback);
                }
            }
        }

        private void OnDestroy()
        {
            _activeSpellCasts.Clear();

            ActorEndpoint.onServerSpellCastStarted -= OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick -= OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted -= OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastInterrupted -= OnSpellCastInterrupted;
            ActorEndpoint.onServerSpellCastDelayedAction -= OnSpellCastDelayedAction;
            ActorEndpoint.onServerRequestSpellCastCancel -= OnSpellCastCancelRequested;

            _instance = null;
            _initialized = false;
        }

        private void OnSpellCastCancelRequested(Actor actor)
        {
            TryInterruptCast(actor.ID);
        }

        #region Spell Cast Lifecycle
        /// <summary>
        /// Internal, server-side validation for spell casts.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="spellDef"></param>
        /// <returns></returns>
        private bool IsValidated(Actor caster, SpellOverride spell)
        {
            // Cooldown gate: block if this spell is on individual cooldown.
            if (ServerCooldownManager.Instance.IsOnCooldown(caster.ID, spell.BaseDefinition.DefinitionID))
            {
                return false;
            }

            // GCD gate: block if a GCD-triggering spell is attempted while GCD is active.
            if (spell.BaseDefinition.TriggersGCD && ServerCooldownManager.Instance.IsOnCooldown(caster.ID, ServerCooldownManager.GCD_ID))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the start of a spell cast. This is where we create the SpellContext, calculate the actual cast time 
        /// after modifiers, and initialize the ECS components for the spell cast.
        /// All validation is done on the ActorSpellcaster before it is passed to the SpellCastManager, 
        /// so we can assume at this point that the spell can be cast and we just need to set it up.
        /// </summary>
        private void OnSpellCastStarted(Actor caster, SpellOverride spell)
        {
            if (!IsValidated(caster, spell)) return;

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorID != caster.ID) continue;

                    // Create the spell context here.
                    SpellContext ctx = new(spell, caster, caster.Target);
                    ctx.PositionTarget = spell.PositionTarget;
                    _activeSpellCasts[caster.ID] = ctx;

                    // Skip ECS and event invocation if the cast time is 0 or less 
                    // - just complete the cast immediately.
                    if (ctx.IsInstantCast())
                    {
                        // Trigger GCD for instant casts.
                        if (spell.BaseDefinition.TriggersGCD)
                            ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, ServerCooldownManager.GCD_ID, GameConfigurationManager.Config.GlobalCooldownDuration);

                        // Instant cast - skip straight to completion.
                        OnSpellCastCompleted(caster.ID, spell.BaseDefinition.DefinitionID);
                        return;
                    }
                    else
                    {
                        _entityManager.SetComponentData(entity, new ECS.SpellCastComponent
                        {
                            SpellID = spell.BaseDefinition.DefinitionID,
                            RemainingCastTime = spell.CastTime,
                            TotalCastTime = spell.CastTime,
                            TotalTicks = spell.TotalTicks,
                            TimeSinceLastTick = 0f

                        });
                        _entityManager.SetComponentEnabled<ECS.SpellCastComponent>(entity, true);

                        caster.GetComponent<ActorSpellcaster>()
                              ?.Observers_StartSpellCast(spell.BaseDefinition.DefinitionID, spell.CastTime);

                        Debug.Log($"Spell cast started: {caster.Name} casting '{spell.BaseDefinition.DisplayName}' ({spell.CastTime}s)");
                    }

                    // Trigger GCD for casted spells (at cast start).
                    if (spell.BaseDefinition.TriggersGCD)
                    {
                        ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, ServerCooldownManager.GCD_ID, GameConfigurationManager.Config.GlobalCooldownDuration);

                        if (caster.Owner != null)
                            caster.GetComponent<ActorSpellcaster>()?.Client_StartGlobalCooldown(caster.Owner.Value, GameConfigurationManager.Config.GlobalCooldownDuration);
                    }

                    // Channelled spells start their individual cooldown immediately.
                    if (spell.BaseDefinition.IsChanneled && spell.Cooldown > 0f)
                    {
                        ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, spell.BaseDefinition.DefinitionID, spell.Cooldown);

                        if (caster.Owner != null)
                            caster.GetComponent<ActorSpellcaster>()?.Client_StartSpellCooldown(caster.Owner.Value, spell.BaseDefinition.DefinitionID, spell.Cooldown);
                    }

                    // Apply resource costs here.
                    var statContainer = caster.GetComponent<ActorStatContainer>();
                    foreach (var cost in spell.BaseDefinition.BaseResourceCosts)
                    {
                        if (cost.CastPhase != SpellCastPhase.OnCastStart) continue;

                        var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                        float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                        statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                            stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                            stat.EffectiveMaximum, newCurrent));

                        if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                            statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                        else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                            statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
                    }

                    return;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {caster.Name} (ID: {caster.ID})");
        }

        /// <summary>
        /// Handles spell cast ticks for channelled spells. 
        /// This is where we apply the tick effects and deduct tick-based resource costs.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastTick(Guid actorID, string spellID)
        {
            if (!_activeSpellCasts.TryGetValue(actorID, out var ctx))
                return;

            ctx.Phase = SpellCastPhase.OnChannelTick;

            foreach (var component in ctx.Spell.Components)
            {
                if (component.CastPhase == SpellCastPhase.OnChannelTick)
                {
                    ApplySpellEffect(ctx, component);
                    // Cast may have been interrupted by target evaluation
                    if (!_activeSpellCasts.ContainsKey(actorID)) return;
                }
            }

            var statContainer = ctx.Caster.GetComponent<ActorStatContainer>();
            foreach (var cost in ctx.Spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnChannelTick) continue;

                var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, newCurrent));

                if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                    statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                    statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
            }

            ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_TickSpellCast(spellID);
        }

        /// <summary>
        /// This method allows scheduling of delayed actions related to a spell cast, 
        /// such as delayed damage or buffs that occur after the cast completes. Primarily, this
        /// occurs when a spell has a projectile and when we want the damage to occur when the
        /// impact occurs rather than at cast completion.
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="action"></param>
        private void OnSpellCastDelayedAction(float delay, Action action)
        {
            _pendingActions.Add((delay, action));
        }

        /// <summary>
        /// Handles spell cast interruption. This can be triggered by the caster (e.g. by moving or using another action that interrupts casting)
        /// or by external factors (e.g. being hit by an interrupting attack). This method is responsible for cleaning up the active spell cast context, disabling the ECS components, refunding resources if applicable, and notifying clients of the interruption.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastInterrupted(Guid actorID, string spellID)
        {
            if (!_activeSpellCasts.Remove(actorID, out var ctx))
                return;

            // Disable the ECS timer
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;
                    _entityManager.SetComponentEnabled<ECS.SpellCastComponent>(entity, false);
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }

            // Refund resources based on RefundOnInterrupt
            // TODO: Once resource cost deduction is implemented, refund here using
            // ctx.Definition.BaseResourceCosts[i].RefundOnInterrupt

            // Notify clients
            ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_InterruptSpellCast(spellID);
            Debug.Log($"Spell cast interrupted: {ctx.Caster.Name} stopped casting '{spellID}'");
        }


        /// <summary>
        /// Handles the completion of a spell cast. This is where we apply the OnCastEnd effects, start cooldowns, and notify clients that the cast has completed so they can resolve the spell effects and targets.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastCompleted(Guid actorID, string spellID)
        {
            if (!_activeSpellCasts.Remove(actorID, out var ctx))
            {
                Debug.Warning($"Spell cast completed but no active context found (ID: {actorID})");
                return;
            }

            // Set the current cast phase to OnCastEnd.
            ctx.Phase = SpellCastPhase.OnCastEnd;

            // Apply OnCastEnd components using ctx.
            // TODO: Modifier components.
            foreach (var component in ctx.Spell.Components)
            {
                if (component.CastPhase == SpellCastPhase.OnCastEnd)
                    ApplySpellEffect(ctx, component);
            }

            // Start individual spell cooldown if defined (channelled spells already started theirs at cast start).
            if (ctx.Spell.Cooldown > 0f && !ctx.Spell.BaseDefinition.IsChanneled)
            {
                ActorEndpoint.onServerStartCooldown?.Invoke(actorID, spellID, ctx.Spell.Cooldown);

                if (ctx.Caster.Owner != null)
                {
                    var owner = ctx.Caster.Owner.Value;
                    ctx.Caster.GetComponent<ActorSpellcaster>()?.Client_StartSpellCooldown(owner, spellID, ctx.Spell.Cooldown);
                    Debug.Log($"Started cooldown for spell '{spellID}' on actor {ctx.Caster.Name} (ID: {actorID}) with duration {ctx.Spell.Cooldown}s");
                }
            }

            // Apply resource costs.
            var statContainer = ctx.Caster.GetComponent<ActorStatContainer>();
            foreach (var cost in ctx.Spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnCastEnd) continue;

                var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, newCurrent));

                if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                    statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                    statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
            }

            // If it was a weapon skill, add experience to the character's weapon skill.
            if (ctx.Spell.BaseDefinition.Tags.Contains("weapon") && ctx.Caster.TryGetComponent<PlayerEquipment>(out var equipment))
            {
                // This should be based on the player's weapon skill level.
                int experienceGained = 4;

                // We need to get the type of weapon the player has equipped.
                if (equipment.TryGetItem(ItemSlot.MainHand, out var mainHandItem))
                {
                    new CharacterWeaponSkillService((PlayerActor)ctx.Caster, mainHandItem.BaseItem.WeaponType, experienceGained);
                }
                else
                {
                    Debug.Warning($"Player {ctx.Caster.Name} cast a weapon skill without a weapon equipped.");
                }
            }

            // Notify the observers that the cast has completed, 
            // so they can resolve the spell effects and targets.
            ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_FinishSpellCast(spellID);
            ctx.Caster.GetComponent<ActorEvents>()?.onServerSpellCastComplete?.Invoke(ctx);
            Debug.Log($"Spell cast completed: {ctx.Caster.Name} finished '{spellID}'");
        }
        #endregion

        private void ApplySpellEffect(SpellContext ctx, SpellComponent component)
        {
            List<Actor> targets = component.TargetDefinition.Evaluate(ctx);
            foreach (var target in targets)
                component.EffectDefinition.Execute(ctx, target);
        }

        /// <summary>Determines whether an actor currently has an active spell cast.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <returns><see langword="true"/> when the actor is casting.</returns>
        public bool IsCasting(Guid actorID)
        {
            return _activeSpellCasts.ContainsKey(actorID);
        }

        /// <summary>Attempts to interrupt an actor's active spell cast.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <returns><see langword="true"/> when an active cast was interrupted.</returns>
        public bool TryInterruptCast(Guid actorID)
        {
            if (!_activeSpellCasts.TryGetValue(actorID, out var ctx))
                return false;

            ActorEndpoint.onServerSpellCastInterrupted?.Invoke(actorID, ctx.Spell.BaseDefinition.DefinitionID);
            return true;
        }
    }
}
