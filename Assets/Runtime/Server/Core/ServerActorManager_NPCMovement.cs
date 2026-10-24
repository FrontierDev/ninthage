using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server.Core
{
    public sealed partial class ServerActorManager
    {
        private static readonly PerformanceMetricDefinition FULL_ENTITY_SCAN_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_FULL_ENTITY_SCAN_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private static readonly PerformanceMetricDefinition GAMEOBJECT_SYNC_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_ECS_GAMEOBJECT_SYNC_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private const int TICK_ENTITY_SCAN_SAMPLE_CAPACITY = PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC;

        private long _pendingTickID = -1;
        private int _pendingTickEntityScanCount;
        private int _pendingTickGameObjectSyncCount;
        private bool _hasPendingTickSyncContribution;

        private struct NPCNavigationState
        {
            public Actor Actor;
            public NavMeshAgent Agent;
        }

        private readonly Dictionary<Guid, NPCNavigationState> _npcNavigationStates = new();

        private void UpdateNPCNavigationRegistration(Actor actor, bool hasEnabledAgent)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (!hasEnabledAgent || actor.Owner != null)
            {
                _npcNavigationStates.Remove(actor.ID);
                return;
            }

            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} was classified as NavMesh-driven without an enabled NavMeshAgent.");
            }

            if (_npcNavigationStates.TryGetValue(actor.ID, out var existing)
                && ReferenceEquals(existing.Actor, actor)
                && ReferenceEquals(existing.Agent, agent))
            {
                return;
            }

            _npcNavigationStates[actor.ID] = new NPCNavigationState
            {
                Actor = actor,
                Agent = agent
            };
        }

        private void RemoveNPCNavigationRegistration(Actor actor)
        {
            if (actor == null)
                return;

            if (_npcNavigationStates.TryGetValue(actor.ID, out var state)
                && ReferenceEquals(state.Actor, actor))
            {
                _npcNavigationStates.Remove(actor.ID);
            }
        }

        private static float GetNPCPursuitStoppingDistance(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (actor.Target == null)
                throw new InvalidOperationException($"NPC {actor.ID} has no pursuit target.");

            var autoAttackSpell = GameConfigurationManager.Config.AutoAttackSpell;
            if (autoAttackSpell == null)
            {
                throw new InvalidOperationException(
                    "NPC melee pursuit requires the configured auto-attack spell range.");
            }

            float stoppingDistance = autoAttackSpell.BaseRange
                + actor.HitboxRadius
                + actor.Target.HitboxRadius;
            if (float.IsNaN(stoppingDistance)
                || float.IsInfinity(stoppingDistance)
                || stoppingDistance < 0f)
            {
                throw new InvalidOperationException("The NPC pursuit stopping distance is invalid.");
            }

            return stoppingDistance;
        }

        internal void DropNPCTarget(Actor npc)
        {
            if (npc == null)
                return;
            if (npc.Owner != null)
            {
                throw new InvalidOperationException(
                    $"Actor '{npc.ID}' is player-owned and cannot use NPC target-drop handling.");
            }

            if (!npc.SpatialContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"NPC '{npc.ID}' has an invalid spatial context.");
            }

            Actor target = npc.Target;
            if (target != null && !target.SpatialContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"NPC target '{target.ID}' has an invalid spatial context.");
            }

            if (target != null)
                npc.SetTargetFromServer(null);

            if (_npcAggroStates.TryGetValue(npc.ID, out var aggroState))
            {
                if (!ReferenceEquals(aggroState.NPC, npc))
                {
                    throw new InvalidOperationException(
                        $"NPC aggro state for '{npc.ID}' belongs to a different actor instance.");
                }

                ClearPendingAggro(ref aggroState);
                _npcAggroStates[npc.ID] = aggroState;
            }

            if (_npcNavigationStates.TryGetValue(npc.ID, out var navigationState)
                && ReferenceEquals(navigationState.Actor, npc)
                && navigationState.Agent != null
                && navigationState.Agent == npc.GetComponent<NavMeshAgent>()
                && navigationState.Agent.enabled
                && navigationState.Agent.isOnNavMesh
                && navigationState.Agent.hasPath)
            {
                navigationState.Agent.ResetPath();
            }

            ActorEndpoint.onServerStopAutoAttack?.Invoke(npc);
        }

        private void UpdateNPCNavigation()
        {
            foreach (var kvp in new List<KeyValuePair<Guid, NPCNavigationState>>(_npcNavigationStates))
            {
                Guid actorID = kvp.Key;
                NPCNavigationState state = kvp.Value;
                if (state.Actor == null
                    || state.Actor.Owner != null
                    || state.Agent == null
                    || state.Actor.GetComponent<NavMeshAgent>() != state.Agent
                    || !state.Agent.enabled)
                {
                    _npcNavigationStates.Remove(actorID);
                    continue;
                }

                if (state.Actor.isDead)
                {
                    _npcNavigationStates.Remove(actorID);
                    continue;
                }

                if (!state.Actor.SpatialContext.IsValid)
                {
                    throw new InvalidOperationException(
                        $"NPC '{state.Actor.ID}' has an invalid spatial context.");
                }

                Actor target = state.Actor.Target;
                if (target == null)
                {
                    if (state.Agent.isOnNavMesh && state.Agent.hasPath)
                        state.Agent.ResetPath();
                    continue;
                }

                if (!target.SpatialContext.IsValid)
                {
                    throw new InvalidOperationException(
                        $"NPC target '{target.ID}' has an invalid spatial context.");
                }

                if (!state.Actor.SpatialContext.IsCompatibleWith(target.SpatialContext))
                {
                    DropNPCTarget(state.Actor);
                    continue;
                }

                if (target.isDead)
                {
                    DropNPCTarget(state.Actor);
                    continue;
                }

                if (!TryGetAcceptedWorldPosition(target.ID, out WorldPosition targetWorldPosition))
                {
                    throw new InvalidOperationException(
                        $"NPC target {target.ID} has no live canonical ECS position.");
                }

                if (state.Agent.pathPending)
                    continue;
                if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                {
                    throw new InvalidOperationException(
                        "NPC navigation requires an initialized server physics manager.");
                }
                if (!state.Agent.isOnNavMesh)
                {
                    throw new InvalidOperationException(
                        $"NPC {state.Actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
                }

                state.Agent.stoppingDistance = GetNPCPursuitStoppingDistance(state.Actor);
                Vector3 physicalTargetPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(targetWorldPosition);
                if (!state.Agent.SetDestination(physicalTargetPosition))
                {
                    throw new InvalidOperationException(
                        $"NPC {state.Actor.ID} rejected pursuit destination for target {target.ID}. "
                        + $"agentPosition={state.Agent.transform.position}; "
                        + $"nextPosition={state.Agent.nextPosition}; "
                        + $"targetWorld=({targetWorldPosition.x}, {targetWorldPosition.y}, {targetWorldPosition.z}); "
                        + $"requestedPhysical=({physicalTargetPosition.x}, {physicalTargetPosition.y}, {physicalTargetPosition.z}); "
                        + $"agentTypeID={state.Agent.agentTypeID}; "
                        + $"areaMask={state.Agent.areaMask}; "
                        + $"isOnNavMesh={state.Agent.isOnNavMesh}; "
                        + $"pathPending={state.Agent.pathPending}.");
                }
            }
        }

        private void EnsureNPCMovementComponents(Entity entity, Actor actor)
        {
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actor);
            UpdateNPCNavigationRegistration(actor, hasEnabledAgent);

            if (_entityManager.HasComponent<Systems.MotionStateComponent>(entity))
            {
                var motion = _entityManager.GetComponentData<Systems.MotionStateComponent>(entity);
                motion.UsesNavMeshAgent = hasEnabledAgent;
                _entityManager.SetComponentData(entity, motion);
            }
            else
            {
                _entityManager.AddComponentData(entity, new Systems.MotionStateComponent
                {
                    VerticalVelocity = 0f,
                    IsGrounded = true,
                    TimeSinceLastGround = 0f,
                    UsesNavMeshAgent = hasEnabledAgent
                });
            }

            if (!_entityManager.HasComponent<Systems.GroundDetectionComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new Systems.GroundDetectionComponent
                {
                    groundDetectionRadius = GameConfigurationManager.Config.GroundDetectionRadius,
                    lastGroundCheck = new Unity.Mathematics.float3(0, -1, 0)
                });
            }
        }

        private static bool HasEnabledNavMeshAgent(Actor actor)
        {
            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
                return false;

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Enabled NPC NavMesh movement requires an initialized server physics manager.");
            }
            if (!ServerPhysicsManager.Instance.TryGetNavigationAgentType(out int agentTypeID))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent but no continuous NavMesh is registered.");
            }
            if (agent.agentTypeID != agentTypeID)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} NavMeshAgent type {agent.agentTypeID} does not match continuous NavMesh agent type {agentTypeID}.");
            }
            if (!agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            return true;
        }

        private void SynchronizeNPCPositionsToECS()
        {
            TraverseNPCMovementEntities(SynchronizeNPCPositionToECS);
        }

        private void SynchronizeNPCTransformsFromECS()
        {
            TraverseNPCMovementEntities(SynchronizeNPCTransformFromECS);
        }

        private void TraverseNPCMovementEntities(
            Func<Entity, Systems.ActorComponent, Actor, bool> synchronize)
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            int fullScanCount = entities.Length;
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.Owner != default)
                        continue;
                    if (!_actorObjects.TryGetValue(component.ActorID, out var actorObj) || actorObj == null)
                        continue;

                    if (synchronize(entity, component, actorObj) && telemetryEnabled)
                        syncCount++;
                }
            }
            finally
            {
                entities.Dispose();
            }

            if (telemetryEnabled)
            {
                RecordTickSyncContribution(
                    fullScanCount,
                    syncCount,
                    ServerECSManager.CurrentSimulationTickID);
            }
        }

        private bool SynchronizeNPCPositionToECS(
            Entity entity,
            Systems.ActorComponent component,
            Actor actorObj)
        {
            var motion = _entityManager.GetComponentData<Systems.MotionStateComponent>(entity);
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actorObj);
            UpdateNPCNavigationRegistration(actorObj, hasEnabledAgent);
            if (motion.UsesNavMeshAgent != hasEnabledAgent)
            {
                motion.UsesNavMeshAgent = hasEnabledAgent;
                _entityManager.SetComponentData(entity, motion);
            }

            if (!hasEnabledAgent)
                return false;

            WorldPosition worldPosition = ServerPhysicsManager.Instance.ToWorldPosition(
                actorObj.transform.position);
            component.WorldPositionD = new Unity.Mathematics.double3(
                worldPosition.x,
                worldPosition.y,
                worldPosition.z);
            _entityManager.SetComponentData(entity, component);
            return true;
        }

        private bool SynchronizeNPCTransformFromECS(
            Entity entity,
            Systems.ActorComponent component,
            Actor actorObj)
        {
            if (_entityManager.GetComponentData<Systems.MotionStateComponent>(entity).UsesNavMeshAgent)
                return false;

            var worldPosition = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);
            if (!worldPosition.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} has an invalid canonical ECS world position.");
            }

            actorObj.transform.position = ServerPhysicsManager.Instance.ToPhysicalPosition(worldPosition);
            return true;
        }

        private void RecordTickSyncContribution(int fullScanCount, int gameObjectSyncCount, long tickID)
        {
            if (tickID <= 0)
                throw new InvalidOperationException("Actor bridge telemetry requires an active authoritative simulation tick.");

            if (_hasPendingTickSyncContribution && _pendingTickID != tickID)
                throw new InvalidOperationException("Actor bridge telemetry contributions crossed authoritative simulation ticks before the previous tick was flushed.");

            if (!_hasPendingTickSyncContribution)
            {
                _pendingTickID = tickID;
                _pendingTickEntityScanCount = 0;
                _pendingTickGameObjectSyncCount = 0;
                _hasPendingTickSyncContribution = true;
            }

            _pendingTickEntityScanCount += fullScanCount;
            _pendingTickGameObjectSyncCount += gameObjectSyncCount;
        }

        private void FlushPendingTickSyncContribution()
        {
            if (!_hasPendingTickSyncContribution)
                return;

            try
            {
                PerformanceTelemetry.TryRecord(
                    FULL_ENTITY_SCAN_COUNT_METRIC,
                    _pendingTickEntityScanCount,
                    _pendingTickID);
                PerformanceTelemetry.TryRecord(
                    GAMEOBJECT_SYNC_COUNT_METRIC,
                    _pendingTickGameObjectSyncCount,
                    _pendingTickID);
            }
            finally
            {
                _pendingTickID = -1;
                _pendingTickEntityScanCount = 0;
                _pendingTickGameObjectSyncCount = 0;
                _hasPendingTickSyncContribution = false;
            }
        }
    }
}
