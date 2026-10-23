using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Shared;
using Game.Shared.Performance;
using Game.Shared.Utility;
using System;
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

        // Authoritative movement bridges contribute to one observation for each simulation tick.
        private long _pendingTickID = -1;
        private int _pendingTickEntityScanCount;
        private int _pendingTickGameObjectSyncCount;
        private bool _hasPendingTickSyncContribution;

        private void EnsureNPCMovementComponents(Entity entity, Actor actor)
        {
            // Components remain attached when ownership returns to a player; the owner checks in
            // ActorUpdateSystem and both movement bridges keep player movement out of this path.
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actor);

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
            return agent != null && agent.enabled;
        }

        private static SimulationRegionDescriptor GetVerifiedActorRegion(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "NPC movement synchronization requires an initialized server physics manager.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(actor.ID, out var descriptor))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} is not bound to an active simulation region.");
            }

            if (!descriptor.Key.Context.IsCompatibleWith(actor.SpatialContext))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} region context is incompatible with the actor context.");
            }

            return descriptor;
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
            if (motion.UsesNavMeshAgent != hasEnabledAgent)
            {
                motion.UsesNavMeshAgent = hasEnabledAgent;
                _entityManager.SetComponentData(entity, motion);
            }

            if (!hasEnabledAgent)
                return false;

            var region = GetVerifiedActorRegion(actorObj);
            var worldPosition = ServerPhysicsManager.Instance.ToWorld(
                region,
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
            // The pre-tick bridge resolved ownership for this tick. A NavMesh agent remains
            // the sole transform writer; ECS-owned NPCs receive the completed simulation state.
            if (_entityManager.GetComponentData<Systems.MotionStateComponent>(entity).UsesNavMeshAgent)
                return false;

            var region = GetVerifiedActorRegion(actorObj);
            var worldPosition = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);
            actorObj.transform.position = ServerPhysicsManager.Instance.ToRegionLocal(
                region,
                worldPosition);
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
