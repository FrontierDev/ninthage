using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative actor auras and aura effects.</summary>
    public sealed class ServerAuraManager : MonoBehaviour
    {
        private static ServerAuraManager _instance;
        /// <summary>Gets the active server aura manager.</summary>
        public static ServerAuraManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the aura manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        private readonly Dictionary<int, AuraContext> _activeAuras = new();
        private readonly ServerSimulationMutationQueue<AuraMutation> _pendingMutations = new();
        private readonly HashSet<int> _pendingRemovalInstanceIDs = new();
        private readonly HashSet<int> _pendingRefreshInstanceIDs = new();
        private AuraMutation[] _commitSnapshot;
        private int _nextInstanceID;

        private enum AuraMutationKind : byte
        {
            Apply,
            Dispel,
            RemoveAll,
        }

        private readonly struct AuraMutation
        {
            internal AuraMutation(
                AuraMutationKind kind,
                AuraDefinition definition,
                Actor caster,
                Actor target,
                Guid targetID,
                int instanceID,
                int stacks)
            {
                Kind = kind;
                Definition = definition;
                Caster = caster;
                Target = target;
                TargetID = targetID;
                InstanceID = instanceID;
                Stacks = stacks;
            }

            internal AuraMutationKind Kind { get; }
            internal AuraDefinition Definition { get; }
            internal Actor Caster { get; }
            internal Actor Target { get; }
            internal Guid TargetID { get; }
            internal int InstanceID { get; }
            internal int Stacks { get; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerAuraManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            ActorEndpoint.onServerApplyAura += OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick += OnAuraTick;
            ActorEndpoint.onServerAuraExpired += OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled += OnAuraDispelled;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerApplyAura -= OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick -= OnAuraTick;
            ActorEndpoint.onServerAuraExpired -= OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled -= OnAuraDispelled;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;

            _activeAuras.Clear();
            _pendingMutations.Clear();
            _pendingRemovalInstanceIDs.Clear();
            _pendingRefreshInstanceIDs.Clear();
            _commitSnapshot = null;
            _instance = null;
            _initialized = false;
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            Entity targetEntity = Entity.Null;
            bool hasTargetEntity = TryFindActorEntity(actor.ID, out targetEntity);

            var cancelledInstanceIDs = new List<int>();
            foreach (var pair in _activeAuras)
            {
                AuraContext context = pair.Value;
                if (context.TargetID == actor.ID)
                {
                    cancelledInstanceIDs.Add(pair.Key);
                }
            }

            var cancelledContexts = new List<AuraContext>();
            foreach (int instanceID in cancelledInstanceIDs)
            {
                if (!_activeAuras.Remove(instanceID, out var context))
                    continue;

                _pendingRemovalInstanceIDs.Remove(instanceID);
                _pendingRefreshInstanceIDs.Remove(instanceID);
                cancelledContexts.Add(context);
            }

            if (hasTargetEntity && _entityManager.HasBuffer<Systems.AuraElement>(targetEntity))
            {
                var buffer = _entityManager.GetBuffer<Systems.AuraElement>(targetEntity);
                buffer.Clear();
            }

            foreach (AuraContext context in cancelledContexts)
                context.Definition?.AuraBehaviour?.OnRemoved(context.Caster, context.Target ?? actor);
        }

        internal void PreparePendingMutations()
        {
            _commitSnapshot = _pendingMutations.TakeSnapshot();
        }

        internal void CommitPendingMutations()
        {
            AuraMutation[] mutations = _commitSnapshot ?? _pendingMutations.TakeSnapshot();
            _commitSnapshot = null;
            foreach (AuraMutation mutation in mutations)
            {
                switch (mutation.Kind)
                {
                    case AuraMutationKind.Apply:
                        _pendingRefreshInstanceIDs.Remove(mutation.InstanceID);
                        if (!TryGetCurrentActor(mutation.TargetID, mutation.Target, out var target)
                            || mutation.Caster == null
                            || !TryGetCurrentActor(mutation.Caster.ID, mutation.Caster, out _))
                        {
                            Debug.Warning($"Discarding aura application for retired target {mutation.TargetID}.");
                            continue;
                        }

                        ApplyAuraNow(mutation.Definition, mutation.Caster, target, mutation.Stacks);
                        break;
                    case AuraMutationKind.Dispel:
                        _pendingRemovalInstanceIDs.Remove(mutation.InstanceID);
                        if (TryGetCurrentActor(mutation.TargetID, mutation.Target, out target))
                            DispelAuraNow(target, mutation.InstanceID);
                        break;
                    case AuraMutationKind.RemoveAll:
                        RemoveAllAurasNow(mutation.TargetID);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mutation.Kind), mutation.Kind, "Unknown aura mutation kind.");
                }
            }
        }

        private void OnApplyAuraRequest(AuraDefinition definition, Actor caster, Actor target, int stacks)
        {
            if (definition == null || caster == null || target == null)
                return;

            Debug.Log($"Received request to apply aura '{definition.DisplayName}' from {caster.Name} to {target.Name}");
            ApplyAura(definition, caster, target, stacks);
        }

        /// <summary>Applies an aura definition to a target actor.</summary>
        /// <param name="definition">The aura definition to apply.</param>
        /// <param name="caster">The actor applying the aura.</param>
        /// <param name="target">The actor receiving the aura.</param>
        /// <param name="stacks">The number of stacks to apply.</param>
        /// <returns><see langword="true"/> when the aura was applied.</returns>
        public bool ApplyAura(AuraDefinition definition, Actor caster, Actor target, int stacks = 1)
        {
            if (definition == null || caster == null || target == null)
                return false;

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(target.ID, target, out _))
                    return false;

                int refreshedInstanceID = 0;
                if (TryFindActiveAuraInstance(target.ID, definition.DefinitionID, out int activeInstanceID))
                {
                    refreshedInstanceID = activeInstanceID;
                    _pendingRefreshInstanceIDs.Add(activeInstanceID);
                }

                try
                {
                    _pendingMutations.Enqueue(
                        new AuraMutation(
                            AuraMutationKind.Apply,
                            definition,
                            caster,
                            target,
                            target.ID,
                            refreshedInstanceID,
                            stacks));
                }
                catch
                {
                    if (refreshedInstanceID != 0)
                        _pendingRefreshInstanceIDs.Remove(refreshedInstanceID);
                    throw;
                }
                return true;
            }

            if (!TryGetCurrentActor(target.ID, target, out _))
                return false;

            return ApplyAuraNow(definition, caster, target, stacks);
        }

        private bool ApplyAuraNow(AuraDefinition definition, Actor caster, Actor target, int stacks)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != target.ID) continue;

                    long firstProgressTickID = GetFirstAuraProgressTickID();
                    int instanceID;
                    if (!TryResolveStacking(
                            definition,
                            caster,
                            target,
                            entity,
                            stacks,
                            firstProgressTickID,
                            out instanceID))
                        return false;

                    var ctx = _activeAuras[instanceID];

                    // Execute OnApply phase components.
                    ctx.Phase = AuraPhase.OnApply;
                    foreach (var auraComponent in definition.Components)
                    {
                        if (auraComponent.CastPhase == AuraPhase.OnApply)
                            ExecuteAuraComponent(ctx, auraComponent);
                    }

                    ActorEndpoint.onServerAuraApplied?.Invoke(target.ID, definition.DefinitionID, instanceID);
                    definition.AuraBehaviour.OnApplied(caster, target, stacks);

                    Debug.Log($"Aura '{definition.DisplayName}' applied to {target.Name} by {caster.Name} (instance {instanceID})");

                    target.Observers_ApplyAura(definition.DefinitionID, instanceID, ctx.Duration, ctx.Stacks);

                    return true;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {target.Name} (ID: {target.ID})");
            return false;
        }

        private bool TryResolveStacking(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            Entity entity,
            int stacksToApply,
            long firstProgressTickID,
            out int instanceID)
        {
            var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            instanceID = -1;

            switch (definition.StackBehavior)
            {
                case AuraStackBehavior.Condition_Extend:
                    {
                        // Single instance per aura def on the target. New applications add stacks
                        // (up to maxStacks) and extend the shared duration for ALL stacks.
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionID.ToString() != definition.DefinitionID) continue;
                            if (!_activeAuras.TryGetValue(entry.InstanceID, out var existing)) continue;

                            // Extend duration for entire aura.
                            InitializeAuraTiming(
                                definition,
                                firstProgressTickID,
                                definition.BaseDuration,
                                out entry.ExpiryTickID,
                                out entry.NextTickID,
                                out entry.TickIntervalTicks);
                            entry.RemainingDuration = definition.BaseDuration;
                            entry.TimeSinceLastTick = 0f;
                            buffer[i] = entry;

                            existing.Duration = definition.BaseDuration;

                            // Add stacks (even at max stacks, duration is still extended).
                            int stacksAdded = 0;
                            for (int s = 0; s < stacksToApply && existing.Stacks < definition.MaxStacks; s++)
                            {
                                existing.Stacks++;
                                existing.ConditionStacks.Add(new ConditionStack(caster, definition.BaseDuration));
                                stacksAdded++;
                            }

                            instanceID = entry.InstanceID;
                            return true;
                        }

                        // First application — create new instance with condition tracking.
                        instanceID = CreateConditionAuraInstance(
                            definition,
                            caster,
                            target,
                            buffer,
                            firstProgressTickID,
                            stacksToApply);
                        return true;
                    }

                case AuraStackBehavior.Condition_Stack:
                    {
                        // Single instance per aura def on the target. Each stack tracks its own
                        // duration independently. The aura's overall duration is the max of all stacks.
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionID.ToString() != definition.DefinitionID) continue;
                            if (!_activeAuras.TryGetValue(entry.InstanceID, out var existing)) continue;

                            // Add multiple stacks
                            for (int s = 0; s < stacksToApply && existing.Stacks < definition.MaxStacks; s++)
                            {
                                existing.Stacks++;
                                existing.ConditionStacks.Add(new ConditionStack(caster, definition.BaseDuration));
                            }

                            // Overall duration = max of all individual stack durations.
                            float maxDuration = 0f;
                            foreach (var s in existing.ConditionStacks)
                                if (s.Duration > maxDuration) maxDuration = s.Duration;
                            existing.Duration = maxDuration;

                            InitializeAuraTiming(
                                definition,
                                firstProgressTickID,
                                existing.Duration,
                                out entry.ExpiryTickID,
                                out entry.NextTickID,
                                out entry.TickIntervalTicks);
                            entry.RemainingDuration = existing.Duration;
                            entry.TimeSinceLastTick = 0f;
                            buffer[i] = entry;

                            instanceID = entry.InstanceID;
                            return true;
                        }

                        // First application.
                        instanceID = CreateConditionAuraInstance(
                            definition,
                            caster,
                            target,
                            buffer,
                            firstProgressTickID,
                            stacksToApply);
                        return true;
                    }

                default:
                    return false;
            }
        }

        private int CreateAuraInstance(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            DynamicBuffer<Systems.AuraElement> buffer,
            long firstProgressTickID)
        {
            int instanceID = _nextInstanceID++;
            InitializeAuraTiming(
                definition,
                firstProgressTickID,
                definition.BaseDuration,
                out long expiryTickID,
                out long nextTickID,
                out long tickIntervalTicks);

            var ctx = new AuraContext(definition, caster, target);
            _activeAuras[instanceID] = ctx;

            buffer.Add(new Systems.AuraElement
            {
                InstanceID = instanceID,
                AuraDefinitionID = definition.DefinitionID,
                RemainingDuration = definition.BaseDuration,
                TickInterval = definition.BaseTickInterval,
                TimeSinceLastTick = 0f,
                ExpiryTickID = expiryTickID,
                NextTickID = nextTickID,
                TickIntervalTicks = tickIntervalTicks
            });

            return instanceID;
        }

        private int CreateConditionAuraInstance(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            DynamicBuffer<Systems.AuraElement> buffer,
            long firstProgressTickID,
            int stackCount = 1)
        {
            int instanceID = _nextInstanceID++;
            InitializeAuraTiming(
                definition,
                firstProgressTickID,
                definition.BaseDuration,
                out long expiryTickID,
                out long nextTickID,
                out long tickIntervalTicks);

            int stacksToCreate = Mathf.Min(stackCount, definition.MaxStacks);
            var conditionStacks = new List<ConditionStack>();
            for (int i = 0; i < stacksToCreate; i++)
            {
                conditionStacks.Add(new ConditionStack(caster, definition.BaseDuration));
            }

            var ctx = new AuraContext(definition, caster, target)
            {
                ConditionStacks = conditionStacks,
                Stacks = stacksToCreate,
                Duration = definition.BaseDuration
            };
            _activeAuras[instanceID] = ctx;

            buffer.Add(new Systems.AuraElement
            {
                InstanceID = instanceID,
                AuraDefinitionID = definition.DefinitionID,
                RemainingDuration = definition.BaseDuration,
                TickInterval = definition.BaseTickInterval,
                TimeSinceLastTick = 0f,
                ExpiryTickID = expiryTickID,
                NextTickID = nextTickID,
                TickIntervalTicks = tickIntervalTicks
            });

            return instanceID;
        }

        private void OnAuraTick(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (_pendingRemovalInstanceIDs.Contains(instanceID)
                || _pendingRefreshInstanceIDs.Contains(instanceID)
                || !_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID)
                return;

            if (!TryGetCurrentActor(targetActorID, null, out var target))
                return;

            // For Condition auras, tick down individual stack durations and remove expired stacks.
            if (ctx.ConditionStacks != null && ctx.Definition.StackBehavior == AuraStackBehavior.Condition_Stack && ctx.Definition.BaseTickInterval > 0f)
            {
                for (int i = ctx.ConditionStacks.Count - 1; i >= 0; i--)
                {
                    ctx.ConditionStacks[i].Duration -= ctx.TickInterval;
                    if (ctx.ConditionStacks[i].Duration <= 0f)
                    {
                        ctx.ConditionStacks.RemoveAt(i);
                        ctx.Stacks--;
                    }
                }
            }

            ctx.Phase = AuraPhase.OnTick;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnTick)
                    ExecuteAuraComponent(ctx, component);
            }

            target.Observers_TickAura(instanceID, ctx.Stacks, ctx.Duration);
        }

        private void OnAuraExpired(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (_pendingRemovalInstanceIDs.Contains(instanceID)
                || !_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID
                || !_activeAuras.Remove(instanceID))
                return;

            _pendingRemovalInstanceIDs.Remove(instanceID);
            _pendingRefreshInstanceIDs.Remove(instanceID);

            if (!TryGetCurrentActor(targetActorID, null, out var target))
                return;

            ctx.Phase = AuraPhase.OnExpire;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnExpire)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            target.Observers_ExpireAura(instanceID);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' expired on {target.Name} (instance {instanceID})");
        }

        private void OnAuraDispelled(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (!_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID
                || !_activeAuras.Remove(instanceID))
                return;

            _pendingRemovalInstanceIDs.Remove(instanceID);
            _pendingRefreshInstanceIDs.Remove(instanceID);

            if (!TryGetCurrentActor(targetActorID, null, out var target))
                return;

            ctx.Phase = AuraPhase.OnDispel;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnDispel)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            target.Observers_DispelAura(instanceID);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' dispelled from {target.Name} (instance {instanceID})");
        }

        private void ExecuteAuraComponent(AuraContext ctx, AuraComponent auraComponent)
        {
            var targets = auraComponent.TargetDefinition != null
                ? auraComponent.TargetDefinition.Evaluate(ctx)
                : new List<Actor> { ctx.Target };

            foreach (var target in targets)
                auraComponent.EffectDefinition.Execute(ctx, target);
        }

        /// <summary>Dispels one aura instance from a target actor.</summary>
        /// <param name="target">The actor whose aura is dispelled.</param>
        /// <param name="instanceID">The aura instance identifier.</param>
        /// <returns><see langword="true"/> when the aura was dispelled.</returns>
        public bool DispelAura(Actor target, int instanceID)
        {
            if (target == null
                || !_activeAuras.TryGetValue(instanceID, out var context)
                || context.TargetID != target.ID
                || _pendingRemovalInstanceIDs.Contains(instanceID))
            {
                return false;
            }

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(target.ID, target, out _))
                    return false;

                _pendingRemovalInstanceIDs.Add(instanceID);
                try
                {
                    _pendingMutations.Enqueue(
                        new AuraMutation(
                            AuraMutationKind.Dispel,
                            null,
                            null,
                            target,
                            target.ID,
                            instanceID,
                            0));
                }
                catch
                {
                    _pendingRemovalInstanceIDs.Remove(instanceID);
                    throw;
                }
                return true;
            }

            return DispelAuraNow(target, instanceID);
        }

        private bool DispelAuraNow(Actor target, int instanceID)
        {
            if (!_activeAuras.TryGetValue(instanceID, out var context)
                || context.TargetID != target.ID
                || context.Definition == null)
            {
                return false;
            }

            string auraDefID = context.Definition.DefinitionID;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != target.ID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].InstanceID != instanceID) continue;

                        buffer.RemoveAt(i);
                        break;
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }

            ActorEndpoint.onServerAuraDispelled?.Invoke(target.ID, auraDefID, instanceID);
            return true;
        }

        /// <summary>Removes all active auras from an actor.</summary>
        /// <param name="actorID">The actor identifier.</param>
        public void RemoveAllAuras(Guid actorID)
        {
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(actorID, null, out _))
                    return;

                foreach (var pair in _activeAuras)
                {
                    if (pair.Value.TargetID == actorID)
                        _pendingRemovalInstanceIDs.Add(pair.Key);
                }

                try
                {
                    _pendingMutations.Enqueue(
                        new AuraMutation(
                            AuraMutationKind.RemoveAll,
                            null,
                            null,
                            null,
                            actorID,
                            0,
                            0));
                }
                catch
                {
                    foreach (var pair in _activeAuras)
                    {
                        if (pair.Value.TargetID == actorID)
                            _pendingRemovalInstanceIDs.Remove(pair.Key);
                    }
                    throw;
                }
                return;
            }

            RemoveAllAurasNow(actorID);
        }

        private void RemoveAllAurasNow(Guid actorID)
        {
            bool hasCurrentActor = TryGetCurrentActor(actorID, null, out _);

            var contexts = new List<AuraContext>();
            var instanceIDs = new List<int>();
            foreach (var pair in _activeAuras)
            {
                if (pair.Value.TargetID != actorID)
                    continue;

                instanceIDs.Add(pair.Key);
                contexts.Add(pair.Value);
            }

            foreach (int instanceID in instanceIDs)
            {
                _activeAuras.Remove(instanceID);
                _pendingRemovalInstanceIDs.Remove(instanceID);
                _pendingRefreshInstanceIDs.Remove(instanceID);
            }

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                    buffer.Clear();
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }

            if (!hasCurrentActor)
                return;

            foreach (AuraContext context in contexts)
            {
                context.Phase = AuraPhase.OnExpire;
                foreach (var auraComponent in context.Definition.Components)
                {
                    if (auraComponent.CastPhase == AuraPhase.OnExpire)
                        ExecuteAuraComponent(context, auraComponent);
                }
            }
        }

        private static long GetFirstAuraProgressTickID()
        {
            long currentSimulationTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentSimulationTickID > 0)
                return currentSimulationTickID;

            return checked(ServerSimulationClock.CurrentTickID + 1L);
        }

        private static void InitializeAuraTiming(
            AuraDefinition definition,
            long firstProgressTickID,
            float duration,
            out long expiryTickID,
            out long nextTickID,
            out long tickIntervalTicks)
        {
            if (firstProgressTickID <= 0)
                throw new InvalidOperationException("Aura timing requires a positive first progress tick.");

            float fixedDeltaTime = GetConfiguredSimulationTickDuration();
            long durationTicks = GetNonShorteningDurationTicks(duration, fixedDeltaTime);
            tickIntervalTicks = GetNonShorteningIntervalTicks(definition.BaseTickInterval, fixedDeltaTime);
            expiryTickID = checked(firstProgressTickID + durationTicks - 1L);
            nextTickID = tickIntervalTicks == 0
                ? 0
                : checked(firstProgressTickID + tickIntervalTicks - 1L);
        }

        private static float GetConfiguredSimulationTickDuration()
        {
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("Aura timing requires initialized server simulation configuration.");

            ServerSimulationClock.ValidateConfiguration(GameConfigurationManager.Config);
            return GameConfigurationManager.Config.ServerSimulationTickDuration;
        }

        private static long GetNonShorteningDurationTicks(float duration, float fixedDeltaTime)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new InvalidOperationException("An active aura requires a finite positive duration.");

            return GetCeilingTickCount(duration, fixedDeltaTime);
        }

        private static long GetNonShorteningIntervalTicks(float interval, float fixedDeltaTime)
        {
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval < 0f)
                throw new InvalidOperationException("An aura tick interval requires a finite non-negative duration.");
            if (interval == 0f)
                return 0;

            return GetCeilingTickCount(interval, fixedDeltaTime);
        }

        private static long GetCeilingTickCount(float seconds, float fixedDeltaTime)
        {
            if (float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime) || fixedDeltaTime <= 0f)
                throw new InvalidOperationException("Aura timing requires a finite positive simulation tick duration.");

            // Preserve the approved non-shortening conversion used by authoritative spell timing.
            decimal secondsDecimal = decimal.Parse(
                seconds.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            decimal tickDurationDecimal = decimal.Parse(
                fixedDeltaTime.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            long tickCount = checked((long)Math.Ceiling(secondsDecimal / tickDurationDecimal));
            return Math.Max(1L, tickCount);
        }

        private bool TryFindActiveAuraInstance(Guid targetID, string auraDefinitionID, out int instanceID)
        {
            foreach (var pair in _activeAuras)
            {
                AuraContext context = pair.Value;
                if (context.TargetID == targetID
                    && context.Definition != null
                    && context.Definition.DefinitionID == auraDefinitionID)
                {
                    instanceID = pair.Key;
                    return true;
                }
            }

            instanceID = 0;
            return false;
        }

        private bool TryGetCurrentActor(Guid actorID, Actor expectedActor, out Actor currentActor)
        {
            if (ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActor(actorID, out currentActor)
                || currentActor == null
                || (expectedActor != null && currentActor != expectedActor))
            {
                currentActor = null;
                return false;
            }

            return true;
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

        /// <summary>Determines whether an actor has an aura definition active.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="auraDefinitionID">The aura definition identifier.</param>
        /// <returns><see langword="true"/> when the aura is active.</returns>
        public bool HasAura(Guid actorID, string auraDefinitionID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].AuraDefinitionID.ToString() == auraDefinitionID)
                            return true;
                    }
                    return false;
                }
            }
            finally
            {
                entities.Dispose();
            }
            return false;
        }

        /// <summary>Gets the active stack count for an actor's aura.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="auraDefinitionID">The aura definition identifier.</param>
        /// <returns>The active stack count, or zero when the aura is absent.</returns>
        public int GetAuraStackCount(Guid actorID, string auraDefinitionID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                    int count = 0;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].AuraDefinitionID.ToString() == auraDefinitionID)
                            count++;
                    }
                    return count;
                }
            }
            finally
            {
                entities.Dispose();
            }
            return 0;
        }
    }
}
