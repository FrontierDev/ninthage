using System;
using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server
{
    public sealed class ServerAuraManager : MonoBehaviour
    {
        private static ServerAuraManager _instance;
        public static ServerAuraManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private EntityManager entityManager;
        private EntityQuery actorComponentQuery;

        private readonly Dictionary<int, AuraContext> _activeAuras = new();
        private int _nextInstanceId;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerAuraManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ECS.ServerECSManager.DefaultWorld;
            entityManager = world.EntityManager;
            actorComponentQuery = entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

            ActorService.onServerApplyAura += OnApplyAuraRequest;
            ActorService.onServerAuraTick += OnAuraTick;
            ActorService.onServerAuraExpired += OnAuraExpired;
            ActorService.onServerAuraDispelled += OnAuraDispelled;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorService.onServerAuraTick -= OnAuraTick;
            ActorService.onServerAuraExpired -= OnAuraExpired;
            ActorService.onServerAuraDispelled -= OnAuraDispelled;

            _activeAuras.Clear();
            _instance = null;
            _initialized = false;
        }

        private void OnApplyAuraRequest(AuraDefinition definition, Actor caster, Actor target, int stacks)
        {
            Debug.Log($"Received request to apply aura '{definition.DisplayName}' from {caster.Name} to {target.Name}");
            ApplyAura(definition, caster, target, stacks);
        }

        public bool ApplyAura(AuraDefinition definition, Actor caster, Actor target, int stacks = 1)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != target.Id) continue;

                    int instanceId;
                    if (!TryResolveStacking(definition, caster, target, entity, stacks, out instanceId))
                        return false;

                    var ctx = _activeAuras[instanceId];

                    // Execute OnApply phase components.
                    ctx.Phase = AuraPhase.OnApply;
                    foreach (var auraComponent in definition.Components)
                    {
                        if (auraComponent.CastPhase == AuraPhase.OnApply)
                            ExecuteAuraComponent(ctx, auraComponent);
                    }

                    ActorService.onServerAuraApplied?.Invoke(target.Id, definition.DefinitionId, instanceId);
                    definition.AuraBehaviour.OnApplied(caster, target, stacks);

                    Debug.Log($"Aura '{definition.DisplayName}' applied to {target.Name} by {caster.Name} (instance {instanceId})");

                    target.Observers_ApplyAura(definition.DefinitionId, instanceId, ctx.Duration, ctx.Stacks);

                    return true;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {target.Name} (ID: {target.Id})");
            return false;
        }

        private bool TryResolveStacking(AuraDefinition definition, Actor caster, Actor target, Entity entity, int stacksToApply, out int instanceId)
        {
            var buffer = entityManager.GetBuffer<ECS.AuraElement>(entity);
            instanceId = -1;

            switch (definition.StackBehavior)
            {
                case AuraStackBehavior.Condition_Extend:
                    {
                        // Single instance per aura def on the target. New applications add stacks
                        // (up to maxStacks) and extend the shared duration for ALL stacks.
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionId.ToString() != definition.DefinitionId) continue;
                            if (!_activeAuras.TryGetValue(entry.InstanceId, out var existing)) continue;

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

                            instanceId = entry.InstanceId;
                            return true;
                        }

                        // First application — create new instance with condition tracking.
                        instanceId = CreateConditionAuraInstance(definition, caster, target, buffer, stacksToApply);
                        return true;
                    }

                case AuraStackBehavior.Condition_Stack:
                    {
                        // Single instance per aura def on the target. Each stack tracks its own
                        // duration independently. The aura's overall duration is the max of all stacks.
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionId.ToString() != definition.DefinitionId) continue;
                            if (!_activeAuras.TryGetValue(entry.InstanceId, out var existing)) continue;

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

                            instanceId = entry.InstanceId;
                            return true;
                        }

                        // First application.
                        instanceId = CreateConditionAuraInstance(definition, caster, target, buffer, stacksToApply);
                        return true;
                    }

                default:
                    return false;
            }
        }

        private int CreateAuraInstance(AuraDefinition definition, Actor caster, Actor target, DynamicBuffer<ECS.AuraElement> buffer)
        {
            int instanceId = _nextInstanceId++;

            var ctx = new AuraContext(definition, caster, target);
            _activeAuras[instanceId] = ctx;

            buffer.Add(new ECS.AuraElement
            {
                InstanceId = instanceId,
                AuraDefinitionId = definition.DefinitionId,
                RemainingDuration = definition.BaseDuration,
                TickInterval = definition.BaseTickInterval,
                TimeSinceLastTick = 0f
            });

            return instanceId;
        }

        private int CreateConditionAuraInstance(AuraDefinition definition, Actor caster, Actor target, DynamicBuffer<ECS.AuraElement> buffer, int stackCount = 1)
        {
            int instanceId = _nextInstanceId++;

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
            _activeAuras[instanceId] = ctx;

            buffer.Add(new ECS.AuraElement
            {
                InstanceId = instanceId,
                AuraDefinitionId = definition.DefinitionId,
                RemainingDuration = definition.BaseDuration,
                TickInterval = definition.BaseTickInterval,
                TimeSinceLastTick = 0f
            });

            return instanceId;
        }

        private void OnAuraTick(Guid targetActorId, string auraDefId, int instanceId)
        {
            if (!_activeAuras.TryGetValue(instanceId, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorId, out var target))
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

            target.Observers_TickAura(instanceId, ctx.Stacks, ctx.Duration);
        }

        private void OnAuraExpired(Guid targetActorId, string auraDefId, int instanceId)
        {
            if (!_activeAuras.Remove(instanceId, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorId, out var target))
                return;

            ctx.Phase = AuraPhase.OnExpire;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnExpire)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            target.Observers_ExpireAura(instanceId);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' expired on {target.Name} (instance {instanceId})");
        }

        private void OnAuraDispelled(Guid targetActorId, string auraDefId, int instanceId)
        {
            if (!_activeAuras.Remove(instanceId, out var ctx))
                return;

            if (!ServerActorManager.Instance.TryGetActor(targetActorId, out var target))
                return;

            ctx.Phase = AuraPhase.OnDispel;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnDispel)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            target.Observers_DispelAura(instanceId);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' dispelled from {target.Name} (instance {instanceId})");
        }

        private void ExecuteAuraComponent(AuraContext ctx, AuraComponent auraComponent)
        {
            var targets = auraComponent.TargetDefinition != null
                ? auraComponent.TargetDefinition.Evaluate(ctx)
                : new List<Actor> { ctx.Target };

            foreach (var target in targets)
                auraComponent.EffectDefinition.Execute(ctx, target);
        }

        public bool DispelAura(Actor target, int instanceId)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != target.Id) continue;

                    var buffer = entityManager.GetBuffer<ECS.AuraElement>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].InstanceId != instanceId) continue;

                        string auraDefId = buffer[i].AuraDefinitionId.ToString();
                        buffer.RemoveAt(i);

                        ActorService.onServerAuraDispelled?.Invoke(target.Id, auraDefId, instanceId);

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

        public void RemoveAllAuras(Guid actorId)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actorId) continue;

                    var buffer = entityManager.GetBuffer<ECS.AuraElement>(entity);
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        var entry = buffer[i];
                        if (_activeAuras.Remove(entry.InstanceId, out var ctx))
                        {
                            if (ServerActorManager.Instance.TryGetActor(actorId, out var target))
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

        public bool HasAura(Guid actorId, string auraDefinitionId)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actorId) continue;

                    var buffer = entityManager.GetBuffer<ECS.AuraElement>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].AuraDefinitionId.ToString() == auraDefinitionId)
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

        public int GetAuraStackCount(Guid actorId, string auraDefinitionId)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actorId) continue;

                    var buffer = entityManager.GetBuffer<ECS.AuraElement>(entity);
                    int count = 0;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].AuraDefinitionId.ToString() == auraDefinitionId)
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
