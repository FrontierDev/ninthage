using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Server
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
        private int _nextInstanceID;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerAuraManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = Core.ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            ActorEndpoint.onServerApplyAura += OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick += OnAuraTick;
            ActorEndpoint.onServerAuraExpired += OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled += OnAuraDispelled;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerApplyAura -= OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick -= OnAuraTick;
            ActorEndpoint.onServerAuraExpired -= OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled -= OnAuraDispelled;

            _activeAuras.Clear();
            _instance = null;
            _initialized = false;
        }

        private void OnApplyAuraRequest(AuraDefinition definition, Actor caster, Actor target, int stacks)
        {
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
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != target.ID) continue;

                    int instanceID;
                    if (!TryResolveStacking(definition, caster, target, entity, stacks, out instanceID))
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

        private bool TryResolveStacking(AuraDefinition definition, Actor caster, Actor target, Entity entity, int stacksToApply, out int instanceID)
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
                        instanceID = CreateConditionAuraInstance(definition, caster, target, buffer, stacksToApply);
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

                            entry.RemainingDuration = existing.Duration;
                            entry.TimeSinceLastTick = 0f;
                            buffer[i] = entry;

                            instanceID = entry.InstanceID;
                            return true;
                        }

                        // First application.
                        instanceID = CreateConditionAuraInstance(definition, caster, target, buffer, stacksToApply);
                        return true;
                    }

                default:
                    return false;
            }
        }

        private int CreateAuraInstance(AuraDefinition definition, Actor caster, Actor target, DynamicBuffer<Systems.AuraElement> buffer)
        {
            int instanceID = _nextInstanceID++;

            var ctx = new AuraContext(definition, caster, target);
            _activeAuras[instanceID] = ctx;

            buffer.Add(new Systems.AuraElement
            {
                InstanceID = instanceID,
                AuraDefinitionID = definition.DefinitionID,
                RemainingDuration = definition.BaseDuration,
                TickInterval = definition.BaseTickInterval,
                TimeSinceLastTick = 0f
            });

            return instanceID;
        }

        private int CreateConditionAuraInstance(AuraDefinition definition, Actor caster, Actor target, DynamicBuffer<Systems.AuraElement> buffer, int stackCount = 1)
        {
            int instanceID = _nextInstanceID++;

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
                TimeSinceLastTick = 0f
            });

            return instanceID;
        }

        private void OnAuraTick(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (!_activeAuras.TryGetValue(instanceID, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorID, out var target))
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
            if (!_activeAuras.Remove(instanceID, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorID, out var target))
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
            if (!_activeAuras.Remove(instanceID, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorID, out var target))
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

                        string auraDefID = buffer[i].AuraDefinitionID.ToString();
                        buffer.RemoveAt(i);

                        ActorEndpoint.onServerAuraDispelled?.Invoke(target.ID, auraDefID, instanceID);

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

        /// <summary>Removes all active auras from an actor.</summary>
        /// <param name="actorID">The actor identifier.</param>
        public void RemoveAllAuras(Guid actorID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        var entry = buffer[i];
                        if (_activeAuras.Remove(entry.InstanceID, out var ctx))
                        {
                            if (ServerActorManager.Instance.TryGetActor(actorID, out var target))
                            {
                                ctx.Phase = AuraPhase.OnExpire;
                                foreach (var auraComponent in ctx.Definition.Components)
                                {
                                    if (auraComponent.CastPhase == AuraPhase.OnExpire)
                                        ExecuteAuraComponent(ctx, auraComponent);
                                }
                            }
                        }
                        buffer.RemoveAt(i);
                    }
                    return;
                }
            }
            finally
            {
                entities.Dispose();
            }
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
