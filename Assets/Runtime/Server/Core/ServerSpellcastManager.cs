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

namespace Game.Server.Core
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
        private readonly Dictionary<Guid, long> _activeCastIDs = new();
        private readonly List<(float RemainingTime, Action onCallback)> _pendingActions = new();
        private readonly ServerSimulationMutationQueue<SpellMutation> _pendingMutations = new();
        private SpellMutation[] _commitSnapshot;
        private long _nextCastID;

        private enum SpellMutationKind : byte
        {
            Start,
            Interrupt,
        }

        private readonly struct SpellMutation
        {
            internal SpellMutation(
                SpellMutationKind kind,
                Guid actorID,
                Actor caster,
                SpellOverride spell,
                SpellContext context,
                string spellID,
                long castID)
            {
                Kind = kind;
                ActorID = actorID;
                Caster = caster;
                Spell = spell;
                Context = context;
                SpellID = spellID;
                CastID = castID;
            }

            internal SpellMutationKind Kind { get; }
            internal Guid ActorID { get; }
            internal Actor Caster { get; }
            internal SpellOverride Spell { get; }
            internal SpellContext Context { get; }
            internal string SpellID { get; }
            internal long CastID { get; }
        }


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerSpellCastManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            ActorEndpoint.onServerSpellCastStarted += OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick += OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted += OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastDelayedAction += OnSpellCastDelayedAction;
            ActorEndpoint.onServerSpellCastInterrupted += OnSpellCastInterrupted;
            ActorEndpoint.onServerRequestSpellCastCancel += OnSpellCastCancelRequested;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;

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
            _activeCastIDs.Clear();
            _pendingActions.Clear();
            _pendingMutations.Clear();
            _commitSnapshot = null;

            ActorEndpoint.onServerSpellCastStarted -= OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick -= OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted -= OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastInterrupted -= OnSpellCastInterrupted;
            ActorEndpoint.onServerSpellCastDelayedAction -= OnSpellCastDelayedAction;
            ActorEndpoint.onServerRequestSpellCastCancel -= OnSpellCastCancelRequested;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;

            _instance = null;
            _initialized = false;
        }

        private void OnSpellCastCancelRequested(Actor actor)
        {
            TryInterruptCast(actor.ID);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            _activeSpellCasts.Remove(actor.ID);
            _activeCastIDs.Remove(actor.ID);
        }

        internal void PreparePendingMutations()
        {
            _commitSnapshot = _pendingMutations.TakeSnapshot();
        }

        internal void CommitPendingMutations()
        {
            SpellMutation[] mutations = _commitSnapshot ?? _pendingMutations.TakeSnapshot();
            _commitSnapshot = null;
            foreach (SpellMutation mutation in mutations)
            {
                switch (mutation.Kind)
                {
                    case SpellMutationKind.Start:
                        if (mutation.Caster == null
                            || ServerActorManager.Instance == null
                            || !ServerActorManager.Instance.TryGetActor(mutation.ActorID, out var currentCaster)
                            || currentCaster != mutation.Caster)
                        {
                            Debug.Warning($"Discarding spell start for retired actor {mutation.ActorID}.");
                            continue;
                        }

                        if (!OnSpellCastStartedNow(currentCaster, mutation.Spell)
                            && mutation.Context != null
                            && !_activeSpellCasts.ContainsKey(mutation.ActorID))
                        {
                            RestoreActiveCast(mutation.ActorID, mutation.Context, mutation.CastID);
                        }
                        break;
                    case SpellMutationKind.Interrupt:
                        CompleteDeferredInterrupt(mutation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mutation.Kind), mutation.Kind, "Unknown spell mutation kind.");
                }
            }
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
            if (caster == null || spell == null)
                return;

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                _activeSpellCasts.TryGetValue(caster.ID, out var previousContext);
                _activeCastIDs.TryGetValue(caster.ID, out var previousCastID);
                InvalidateActiveCast(caster.ID);
                _pendingMutations.Enqueue(
                    new SpellMutation(
                        SpellMutationKind.Start,
                        caster.ID,
                        caster,
                        spell,
                        previousContext,
                        null,
                        previousCastID));
                return;
            }

            OnSpellCastStartedNow(caster, spell);
        }

        private bool OnSpellCastStartedNow(Actor caster, SpellOverride spell)
        {
            if (!IsValidated(caster, spell)) return false;

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != caster.ID) continue;

                    // Create the spell context here.
                    SpellContext ctx = new(spell, caster, caster.Target);
                    ctx.PositionTarget = spell.PositionTarget;
                    long castID = checked(++_nextCastID);
                    InvalidateActiveCast(caster.ID);
                    _activeSpellCasts[caster.ID] = ctx;
                    _activeCastIDs[caster.ID] = castID;

                    // Skip ECS and event invocation if the cast time is 0 or less 
                    // - just complete the cast immediately.
                    if (ctx.IsInstantCast())
                    {
                        // Trigger GCD for instant casts.
                        if (spell.BaseDefinition.TriggersGCD)
                            ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, ServerCooldownManager.GCD_ID, GameConfigurationManager.Config.GlobalCooldownDuration);

                        // Instant cast - skip straight to completion.
                        CompleteCastNow(caster.ID, spell.BaseDefinition.DefinitionID, castID, ctx);
                        return true;
                    }
                    else
                    {
                        _entityManager.SetComponentData(entity, new Systems.SpellCastComponent
                        {
                            SpellID = spell.BaseDefinition.DefinitionID,
                            CastID = castID,
                            RemainingCastTime = spell.CastTime,
                            TotalCastTime = spell.CastTime,
                            TotalTicks = spell.TotalTicks,
                            TimeSinceLastTick = 0f

                        });
                        _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, true);

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

                    return true;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {caster.Name} (ID: {caster.ID})");
            return false;
        }

        /// <summary>
        /// Handles spell cast ticks for channelled spells. 
        /// This is where we apply the tick effects and deduct tick-based resource costs.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastTick(Guid actorID, string spellID)
        {
            if (!TryResolveCastEvent(actorID, spellID, out long castID)
                || !TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            ctx.Phase = SpellCastPhase.OnChannelTick;

            foreach (var component in ctx.Spell.Components)
            {
                if (component.CastPhase == SpellCastPhase.OnChannelTick)
                {
                    ApplySpellEffect(ctx, component);
                    // Cast may have been interrupted by target evaluation
                    if (!IsCurrentCast(actorID, spellID, castID, ctx)) return;
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

            if (IsCurrentCast(actorID, spellID, castID, ctx))
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
            if (!TryResolveInterruptCast(actorID, spellID, out long castID)
                || !TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                InvalidateActiveCast(actorID);
                _pendingMutations.Enqueue(
                    new SpellMutation(
                        SpellMutationKind.Interrupt,
                        actorID,
                        null,
                        null,
                        ctx,
                        spellID,
                        castID));
                return;
            }

            InterruptCastNow(actorID, spellID, castID, ctx);
        }

        private void CompleteDeferredInterrupt(SpellMutation mutation)
        {
            if (_activeCastIDs.TryGetValue(mutation.ActorID, out long activeCastID)
                && activeCastID != mutation.CastID)
            {
                return;
            }

            InterruptCastNow(mutation.ActorID, mutation.SpellID, mutation.CastID, mutation.Context);
        }

        private void InterruptCastNow(Guid actorID, string spellID, long castID, SpellContext ctx)
        {
            if (_activeCastIDs.TryGetValue(actorID, out long activeCastID))
            {
                if (activeCastID != castID
                    || !_activeSpellCasts.TryGetValue(actorID, out var activeContext)
                    || activeContext != ctx)
                {
                    return;
                }

                InvalidateActiveCast(actorID);
            }

            DisableCastComponent(actorID, castID);

            if (ctx?.Caster == null
                || ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActor(actorID, out var currentCaster)
                || currentCaster != ctx.Caster)
            {
                return;
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
            if (!TryResolveCastEvent(actorID, spellID, out long castID)
                || !TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            if (ServerECSManager.TryGetCurrentSimulationEvent(out var currentEvent)
                && (currentEvent.Kind != ServerSimulationEventKind.SpellCastCompleted
                    || currentEvent.ActorID != actorID
                    || currentEvent.CastID != castID))
            {
                QueueCompletionForNextBoundary(actorID, spellID, castID, currentEvent.SourceEntity);
                return;
            }

            if (ServerECSManager.IsMutationBoundaryActive
                && !ServerECSManager.TryGetCurrentSimulationEvent(out currentEvent))
            {
                if (TryFindActorEntity(actorID, out Entity sourceEntity))
                    QueueCompletionForNextBoundary(actorID, spellID, castID, sourceEntity);
                return;
            }

            CompleteCastNow(actorID, spellID, castID, ctx);
        }

        private void CompleteCastNow(Guid actorID, string spellID, long castID, SpellContext ctx)
        {
            if (!TryGetCurrentCast(actorID, spellID, castID, out var activeContext)
                || activeContext != ctx)
            {
                return;
            }

            InvalidateActiveCast(actorID);

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

        private bool TryResolveCastEvent(Guid actorID, string spellID, out long castID)
        {
            if (ServerECSManager.TryGetCurrentSimulationEvent(out var simulationEvent))
            {
                if ((simulationEvent.Kind != ServerSimulationEventKind.SpellCastTick
                        && simulationEvent.Kind != ServerSimulationEventKind.SpellCastCompleted)
                    || simulationEvent.ActorID != actorID
                    || simulationEvent.DefinitionID.ToString() != spellID
                    || simulationEvent.CastID <= 0)
                {
                    castID = 0;
                    return false;
                }

                castID = simulationEvent.CastID;
                return true;
            }

            return _activeCastIDs.TryGetValue(actorID, out castID) && castID > 0;
        }

        private bool TryResolveInterruptCast(Guid actorID, string spellID, out long castID)
        {
            if (ServerECSManager.TryGetCurrentSimulationEvent(out var simulationEvent)
                && (simulationEvent.Kind == ServerSimulationEventKind.SpellCastTick
                    || simulationEvent.Kind == ServerSimulationEventKind.SpellCastCompleted)
                && simulationEvent.ActorID == actorID
                && simulationEvent.DefinitionID.ToString() == spellID)
            {
                castID = simulationEvent.CastID;
                return castID > 0;
            }

            return _activeCastIDs.TryGetValue(actorID, out castID) && castID > 0;
        }

        private bool TryGetCurrentCast(Guid actorID, string spellID, long castID, out SpellContext context)
        {
            if (!_activeSpellCasts.TryGetValue(actorID, out context)
                || !_activeCastIDs.TryGetValue(actorID, out long activeCastID)
                || activeCastID != castID
                || context?.Spell?.BaseDefinition?.DefinitionID != spellID)
            {
                context = null;
                return false;
            }

            return true;
        }

        private bool IsCurrentCast(Guid actorID, string spellID, long castID, SpellContext context)
        {
            return TryGetCurrentCast(actorID, spellID, castID, out var currentContext)
                && currentContext == context;
        }

        private void InvalidateActiveCast(Guid actorID)
        {
            _activeSpellCasts.Remove(actorID);
            _activeCastIDs.Remove(actorID);
        }

        private void RestoreActiveCast(Guid actorID, SpellContext context, long castID)
        {
            _activeSpellCasts[actorID] = context;
            _activeCastIDs[actorID] = castID;

            if (castID <= 0 || !TryFindActorEntity(actorID, out Entity entity))
                return;

            var component = _entityManager.GetComponentData<Systems.SpellCastComponent>(entity);
            if (component.CastID == castID)
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, true);
        }

        private void DisableCastComponent(Guid actorID, long castID)
        {
            if (castID <= 0)
                return;

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var actor = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (actor.ActorID != actorID)
                        continue;

                    var component = _entityManager.GetComponentData<Systems.SpellCastComponent>(entity);
                    if (component.CastID == castID)
                        _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, false);
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private bool TryFindActorEntity(Guid actorID, out Entity actorEntity)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (_entityManager.GetComponentData<Systems.ActorComponent>(entity).ActorID != actorID)
                        continue;

                    actorEntity = entity;
                    return true;
                }
            }
            finally
            {
                entities.Dispose();
            }

            actorEntity = Entity.Null;
            return false;
        }

        private void QueueCompletionForNextBoundary(Guid actorID, string spellID, long castID, Entity sourceEntity)
        {
            ServerECSManager.QueueSimulationEvent(
                ServerSimulationEventKind.SpellCastCompleted,
                actorID,
                sourceEntity,
                spellID,
                castID: castID);
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
