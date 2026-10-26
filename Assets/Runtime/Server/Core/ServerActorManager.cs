using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>
    /// The server actor manager handles all server-side actors, whether they are NPCs or PCs.
    /// Player movement proposals are owner-authored and validated at the authoritative
    /// simulation boundary before they reach ECS.
    /// </summary>
    public sealed partial class ServerActorManager : MonoBehaviour
    {
        #region Nested types

        internal readonly struct AcceptedVisibilityMovement
        {
            internal Actor Actor { get; }
            internal WorldPosition PreviousPosition { get; }
            internal WorldPosition CurrentPosition { get; }
            internal ActorSpatialContext Context { get; }

            internal AcceptedVisibilityMovement(
                Actor actor,
                WorldPosition previousPosition,
                WorldPosition currentPosition,
                ActorSpatialContext context)
            {
                Actor = actor;
                PreviousPosition = previousPosition;
                CurrentPosition = currentPosition;
                Context = context;
            }
        }

        private readonly struct ActorRegistration
        {
            internal ActorRegistration(
                Actor actor,
                Entity entity,
                NetworkIdentity identity)
            {
                Actor = actor;
                Entity = entity;
                Identity = identity;
            }

            internal Actor Actor { get; }
            internal Entity Entity { get; }
            internal NetworkIdentity Identity { get; }
        }

        #endregion

        #region Static state and performance metrics

        private static readonly PerformanceMetricDefinition UPDATE_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition LATE_UPDATE_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_LATE_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition NPC_AGGRO_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_NPC_AGGRO_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_schedule");
        private static readonly PerformanceMetricDefinition NPC_PERCEPTION_PLAYER_CENTER_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_PLAYER_CENTER_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_perception",
            "server_schedule");
        private static readonly PerformanceMetricDefinition NPC_PERCEPTION_UNIQUE_CELL_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_CELL_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_perception",
            "server_schedule");
        private static readonly PerformanceMetricDefinition NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_perception",
            "server_schedule");
        private static readonly PerformanceMetricDefinition NPC_PERCEPTION_LOCAL_PAIR_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_LOCAL_PAIR_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_perception",
            "server_schedule");
        private static readonly PerformanceMetricDefinition NPC_PERCEPTION_DECISION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_DECISION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_perception",
            "server_schedule");
        private static readonly PerformanceMetricDefinition ECS_ACTOR_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_ECS_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition NETWORK_GAMEOBJECT_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NETWORK_GAMEOBJECT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition TOTAL_ALLOCATED_MEMORY_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_TOTAL_ALLOCATED_BYTES,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition MONO_USED_MEMORY_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_MONO_USED_BYTES,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition MONO_HEAP_MEMORY_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_MONO_HEAP_BYTES,
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition GC_GEN0_COLLECTION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_GC_GEN0_COLLECTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition GC_GEN1_COLLECTION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_GC_GEN1_COLLECTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition GC_GEN2_COLLECTION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_MEMORY_GC_GEN2_COLLECTION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "memory",
            "server_update");
        private static readonly PerformanceMetricDefinition OVERLAP_SEARCH_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_OVERLAP_SEARCH_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_schedule");
        private static readonly PerformanceMetricDefinition COLLIDER_CANDIDATE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_COLLIDER_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_schedule");
        private static readonly PerformanceMetricDefinition OWNER_CHANGE_ENTITY_SCAN_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_OWNER_CHANGE_ENTITY_SCAN_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "actor_owner_change");
        private static readonly PerformanceMetricDefinition INTEREST_CHANGE_ENTITY_SCAN_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_INTEREST_CHANGE_ENTITY_SCAN_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "actor_interest_change");
        private static readonly PerformanceMetricDefinition ALLOC_HEAVY_OPERATION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_ALLOC_HEAVY_OPERATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_schedule");
        private static readonly PerformanceMetricDefinition SPAWN_EVENT_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_SPAWN_EVENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "lifetime");
        private static readonly PerformanceMetricDefinition FULL_NPC_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTORS_FULL_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_activity",
            "server_update");
        private static readonly PerformanceMetricDefinition NEARBY_NPC_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTORS_NEARBY_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_activity",
            "server_update");
        private static readonly PerformanceMetricDefinition DORMANT_ACTOR_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTORS_DORMANT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_activity",
            "server_update");
        private static readonly PerformanceMetricDefinition NPC_ACTIVITY_REEVALUATION_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_ACTIVITY_REEVALUATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager.npc_activity",
            "activity_candidate_batch");
        private static readonly PerformanceMetricDefinition NPC_ACTIVITY_TRANSITION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_NPC_ACTIVITY_TRANSITION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_activity",
            "activity_candidate_batch");
        private static readonly PerformanceMetricDefinition SPATIAL_CANDIDATE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPATIAL_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_update");
        private static readonly PerformanceMarker UPDATE_MARKER = new(UPDATE_DURATION_METRIC, "Server.ActorManager.Update");
        private static readonly PerformanceMarker LATE_UPDATE_MARKER = new(LATE_UPDATE_DURATION_METRIC, "Server.ActorManager.LateUpdate");
        private static readonly PerformanceMarker NPC_AGGRO_MARKER = new(NPC_AGGRO_DURATION_METRIC, "Server.ActorManager.ScheduledNPCAggro");
        private static readonly PerformanceMarker NPC_ACTIVITY_REEVALUATION_MARKER = new(
            NPC_ACTIVITY_REEVALUATION_DURATION_METRIC,
            "Server.ActorManager.ReevaluateNPCActivityCandidates");
        private const int ACTOR_EVENT_SCAN_SAMPLE_CAPACITY = 1024;

        private static ServerActorManager _instance;
        /// <summary>Gets the active server actor manager.</summary>
        public static ServerActorManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the actor manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(UPDATE_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(LATE_UPDATE_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_AGGRO_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_PERCEPTION_PLAYER_CENTER_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_PERCEPTION_UNIQUE_CELL_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_PERCEPTION_LOCAL_PAIR_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NPC_PERCEPTION_DECISION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(POSITION_VALIDATION_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(PLAYER_ECS_SYNC_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(PLAYER_MOVEMENT_WORK_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(PENDING_POSITION_PROPOSAL_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(DUE_POSITION_VALIDATION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(ECS_ACTOR_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NETWORK_GAMEOBJECT_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(TOTAL_ALLOCATED_MEMORY_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(MONO_USED_MEMORY_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(MONO_HEAP_MEMORY_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(GC_GEN0_COLLECTION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(GC_GEN1_COLLECTION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(GC_GEN2_COLLECTION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(OVERLAP_SEARCH_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(COLLIDER_CANDIDATE_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(FULL_ENTITY_SCAN_COUNT_METRIC, TICK_ENTITY_SCAN_SAMPLE_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GAMEOBJECT_SYNC_COUNT_METRIC, TICK_ENTITY_SCAN_SAMPLE_CAPACITY);
            PerformanceTelemetry.RegisterMetric(OWNER_CHANGE_ENTITY_SCAN_COUNT_METRIC, ACTOR_EVENT_SCAN_SAMPLE_CAPACITY);
            PerformanceTelemetry.RegisterMetric(INTEREST_CHANGE_ENTITY_SCAN_COUNT_METRIC, ACTOR_EVENT_SCAN_SAMPLE_CAPACITY);
            PerformanceTelemetry.RegisterMetric(ALLOC_HEAVY_OPERATION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_EVENT_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(FULL_NPC_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(NEARBY_NPC_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(DORMANT_ACTOR_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                NPC_ACTIVITY_REEVALUATION_DURATION_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                NPC_ACTIVITY_TRANSITION_COUNT_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                NPC_NAVIGATION_DURATION_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                NPC_NAVIGATION_SYNC_DURATION_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                NPC_NAVIGATION_ACTIVE_COUNT_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                SPATIAL_CANDIDATE_COUNT_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        #endregion

        #region Actor and ECS state

        // ECS Management.
        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;
        private EntityQuery _acceptedPositionDirtyQuery;

        private readonly Dictionary<Guid, ActorRegistration> _actors = new();
        private readonly HashSet<PlayerID> _pendingSpatialContextTransfers = new();
        private readonly List<AcceptedVisibilityMovement> _acceptedVisibilityMovements = new();
        private readonly List<Guid> _riftQueryCandidateIDs = new();
        private readonly HashSet<Guid> _npcActivityCandidateIDs = new();
        private readonly List<Guid> _npcActivityCandidateQueryBuffer = new();
        private readonly List<Guid> _npcActivityPlayerQueryBuffer = new();
        // Telemetry projections only; NPCActivityComponent.State remains the activity authority.
        private int _fullNPCCount;
        private int _nearbyNPCCount;
        private int _dormantNPCCount;
        private NetworkManager _networkManager;
        private ServerSpatialIndex _spatialIndex;

        #endregion

        #region Actor queries

        /// <summary>Finds the server actor associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="actor">Receives the actor when found.</param>
        /// <returns><see langword="true"/> when an actor was found.</returns>
        public bool TryGetActor(Guid actorID, out Actor actor)
        {
            if (_actors.TryGetValue(actorID, out ActorRegistration registration))
            {
                actor = registration.Actor;
                return true;
            }

            actor = null;
            return false;
        }

        /// <summary>Queries candidate actor identifiers from the owned spatial index.</summary>
        public int QuerySpatialCandidates(
            ActorSpatialContext context,
            WorldPosition center,
            double radius,
            List<Guid> resultBuffer,
            out int examinedCandidateCount,
            Predicate<Guid> predicate = null)
        {
            return GetSpatialIndex().QueryCandidates(
                context,
                center,
                radius,
                resultBuffer,
                out examinedCandidateCount,
                predicate);
        }

        /// <summary>Queries actor identifiers within an exact range from the owned spatial index.</summary>
        public int QuerySpatialInRange(
            ActorSpatialContext context,
            WorldPosition center,
            double radius,
            List<Guid> resultBuffer,
            Func<Guid, WorldPosition> acceptedPositionResolver,
            out int examinedCandidateCount,
            bool includeVerticalDistance = true,
            Predicate<Guid> predicate = null)
        {
            return GetSpatialIndex().QueryInRange(
                context,
                center,
                radius,
                resultBuffer,
                acceptedPositionResolver,
                out examinedCandidateCount,
                includeVerticalDistance,
                predicate);
        }

        private List<Actor> QueryActorsInRange(
            ActorSpatialContext context,
            WorldPosition center,
            double radius)
        {
            _riftQueryCandidateIDs.Clear();
            QuerySpatialInRange(
                context,
                center,
                radius,
                _riftQueryCandidateIDs,
                ResolveAcceptedPosition,
                out _,
                includeVerticalDistance: true);

            var actors = new List<Actor>(_riftQueryCandidateIDs.Count);
            foreach (Guid actorID in _riftQueryCandidateIDs)
            {
                if (!TryGetActor(actorID, out Actor actor) || actor == null)
                    throw new InvalidOperationException(
                        $"Spatial index contains actor {actorID}, but the authoritative actor registry does not.");
                if (!actor.SpatialContext.IsValid || !context.IsCompatibleWith(actor.SpatialContext))
                    throw new InvalidOperationException(
                        $"Spatial index returned actor {actorID} with an invalid or incompatible spatial context.");

                actors.Add(actor);
            }

            return actors;

            WorldPosition ResolveAcceptedPosition(Guid actorID)
            {
                if (!TryGetAcceptedWorldPosition(actorID, out WorldPosition position))
                    throw new InvalidOperationException(
                        $"Spatial index contains actor {actorID}, but it has no accepted world position.");
                return position;
            }
        }

        /// <summary>Finds the authoritative ECS entity associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="entity">Receives the authoritative ECS entity when found.</param>
        /// <returns><see langword="true"/> when the actor has a live authoritative entity.</returns>
        internal bool TryGetActorEntity(Guid actorID, out Entity entity)
        {
            if (_actors.TryGetValue(actorID, out ActorRegistration registration))
            {
                entity = registration.Entity;
                return true;
            }

            entity = default;
            return false;
        }

        /// <summary>Finds the authoritative network identity associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="identity">Receives the authoritative network identity when found.</param>
        /// <returns><see langword="true"/> when the actor has a live authoritative network identity.</returns>
        internal bool TryGetActorIdentity(Guid actorID, out NetworkIdentity identity)
        {
            if (_actors.TryGetValue(actorID, out ActorRegistration registration))
            {
                identity = registration.Identity;
                return true;
            }

            identity = null;
            return false;
        }

        /// <summary>Attempts to get an actor's canonical accepted absolute world position.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="position">Receives the canonical accepted absolute world position when found.</param>
        /// <returns><see langword="true"/> when the actor identifier has a registered ECS entity mapping.</returns>
        /// <remarks>A mapped ECS entity is read directly; ECS lookup failures are not treated as an absent actor.</remarks>
        internal bool TryGetAcceptedWorldPosition(
            Guid actorID,
            out WorldPosition position)
        {
            if (!_actors.TryGetValue(actorID, out ActorRegistration registration))
            {
                position = default;
                return false;
            }

            var component = _entityManager.GetComponentData<Systems.ActorComponent>(registration.Entity);
            position = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);

            return true;
        }

        /// <summary>Gets the authoritative center-to-center distance between two actors.</summary>
        /// <param name="first">The first live actor.</param>
        /// <param name="second">The second live actor.</param>
        /// <returns>The finite, nonnegative distance between accepted absolute positions.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when actor contexts are incompatible, accepted positions are unavailable, or the distance is invalid.
        /// </exception>
        internal double GetAcceptedCenterDistance(
            Actor first,
            Actor second)
        {
            ActorSpatialContext firstContext = first.SpatialContext;
            ActorSpatialContext secondContext = second.SpatialContext;
            if (!firstContext.IsCompatibleWith(secondContext))
            {
                throw new InvalidOperationException(
                    $"Cannot calculate actor distance for incompatible spatial contexts ({first.ID}, {second.ID}).");
            }

            if (!TryGetAcceptedWorldPosition(first.ID, out var firstPosition))
            {
                throw new InvalidOperationException(
                    $"Actor {first.ID} has no live accepted world position.");
            }
            if (!TryGetAcceptedWorldPosition(second.ID, out var secondPosition))
            {
                throw new InvalidOperationException(
                    $"Actor {second.ID} has no live accepted world position.");
            }

            double deltaX = firstPosition.x - secondPosition.x;
            double deltaY = firstPosition.y - secondPosition.y;
            double deltaZ = firstPosition.z - secondPosition.z;
            double distance = Math.Sqrt(
                deltaX * deltaX
                + deltaY * deltaY
                + deltaZ * deltaZ);
            if (double.IsNaN(distance)
                || double.IsInfinity(distance)
                || distance < 0d)
            {
                throw new InvalidOperationException(
                    $"Actor distance between {first.ID} and {second.ID} is not finite and nonnegative.");
            }

            return distance;
        }

        /// <summary>Gets the authoritative surface-to-surface distance between two actors.</summary>
        /// <param name="first">The first live actor.</param>
        /// <param name="second">The second live actor.</param>
        /// <returns>The finite, nonnegative distance after subtracting both hitbox radii.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when actor contexts, accepted positions, hitbox radii or the result are invalid.
        /// </exception>
        internal double GetAcceptedSurfaceDistance(
            Actor first,
            Actor second)
        {
            double centerDistance = GetAcceptedCenterDistance(first, second);
            float firstRadius = first.HitboxRadius;
            float secondRadius = second.HitboxRadius;
            if (float.IsNaN(firstRadius)
                || float.IsInfinity(firstRadius)
                || firstRadius < 0f)
            {
                throw new InvalidOperationException($"Actor {first.ID} has an invalid hitbox radius.");
            }
            if (float.IsNaN(secondRadius)
                || float.IsInfinity(secondRadius)
                || secondRadius < 0f)
            {
                throw new InvalidOperationException($"Actor {second.ID} has an invalid hitbox radius.");
            }

            double distance = Math.Max(
                0d,
                centerDistance
                    - firstRadius
                    - secondRadius);
            return distance;
        }

        #endregion

        #region NPC aggro state

        // NPC aggro tracking.
        private struct NPCAggroState
        {
            public Actor NPC;
            public NPCBehavior Behavior;
            public Actor PendingTarget;
            public long PendingAcquisitionTickID;
        }
        private readonly Dictionary<Guid, NPCAggroState> _npcAggroStates = new();
        private readonly HashSet<ActorSpatialContext> _npcPerceptionContexts = new();
        private readonly List<PlayerActor> _npcPerceptionContextPlayers = new();
        private readonly List<WorldPosition> _npcPerceptionContextPlayerPositions = new();
        private readonly HashSet<Guid> _npcPerceptionCandidateIDs = new();
        private ServerSimulationRegistrationHandle _npcAggroSchedule;
        private int _npcAggroDecisionIntervalTicks;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerActorManager is already initialized.");
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("Server spatial indexing requires initialized game configuration.");

            InitializePlayerValidationSchedule();
            _npcAggroDecisionIntervalTicks = GetNPCAggroDecisionIntervalTicks();
            _spatialIndex = new ServerSpatialIndex(GameConfigurationManager.Config.SpatialIndexCellWidth);
            _instance = this;
            _networkManager = NetworkManager.main;

            ActorEndpoint.onServerActorSpawned += OnActorSpawned;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged += OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged += OnActorInterestChanged;
            ActorEndpoint.onActorDeath += OnActorDeath;
            ActorEndpoint.onServerNPCDamaged += OnNPCDamaged;
            ActorEndpoint.onServerInstanceValidationToggleRequested += OnInstanceValidationToggleRequested;
            ActorEndpoint.onServerQueryActorsInRange += QueryActorsInRange;
            MovementEndpoint.onServerPositionProposal += OnServerPositionProposal;
            _networkManager.onPlayerLeft += OnPlayerLeft;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));
            _acceptedPositionDirtyQuery = _entityManager.CreateEntityQuery(
                typeof(Systems.ActorComponent),
                typeof(Systems.AcceptedPositionDirtyComponent));

            _initialized = true;
            _npcAggroSchedule = ServerSimulationClock.RegisterLowerRate(
                _npcAggroDecisionIntervalTicks,
                UpdateNPCAggro);
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            _initialized = false;

            _npcAggroSchedule.Cancel();
            _npcAggroSchedule = default;

            ActorEndpoint.onServerActorSpawned -= OnActorSpawned;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged -= OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged -= OnActorInterestChanged;
            ActorEndpoint.onActorDeath -= OnActorDeath;
            ActorEndpoint.onServerNPCDamaged -= OnNPCDamaged;
            ActorEndpoint.onServerInstanceValidationToggleRequested -= OnInstanceValidationToggleRequested;
            ActorEndpoint.onServerQueryActorsInRange -= QueryActorsInRange;
            MovementEndpoint.onServerPositionProposal -= OnServerPositionProposal;
            if (_networkManager != null)
                _networkManager.onPlayerLeft -= OnPlayerLeft;

            _actors.Clear();
            _playerActors.Clear();
            _periodicValidationBaselines.Clear();
            _lastValidationTickIDs.Clear();
            _nextValidationTickIDs.Clear();
            _violationCounters.Clear();
            _pendingPositionProposals.Clear();
            _lastProposalOrder.Clear();
            _playerMovementWork.Clear();
            _playerMovementWorkSet.Clear();
            if (_playerValidationBuckets != null)
            {
                for (int i = 0; i < _playerValidationBuckets.Length; i++)
                    _playerValidationBuckets[i].Clear();

                _playerValidationBuckets = null;
            }
            _playerValidationIntervalTicks = 0;
            _pendingSpatialContextTransfers.Clear();
            _npcAggroStates.Clear();
            _npcPerceptionContexts.Clear();
            _npcPerceptionContextPlayers.Clear();
            _npcPerceptionContextPlayerPositions.Clear();
            _npcPerceptionCandidateIDs.Clear();
            _npcNavigationStates.Clear();
            _activeNPCNavigationIDs.Clear();
            _acceptedVisibilityMovements.Clear();
            _riftQueryCandidateIDs.Clear();
            _npcActivityCandidateIDs.Clear();
            _npcActivityCandidateQueryBuffer.Clear();
            _npcActivityPlayerQueryBuffer.Clear();
            _fullNPCCount = 0;
            _nearbyNPCCount = 0;
            _dormantNPCCount = 0;
            _pendingTickID = -1;
            _pendingTickEntityScanCount = 0;
            _pendingTickGameObjectSyncCount = 0;
            _hasPendingTickSyncContribution = false;
            _spatialIndex = null;
            _instance = null;
            _networkManager = null;
        }

        #endregion

        #region Server simulation

        private ServerSpatialIndex GetSpatialIndex()
        {
            if (_spatialIndex == null)
                throw new InvalidOperationException("The server spatial index is not initialized.");

            return _spatialIndex;
        }

        private void Update()
        {
            UpdateNPCNavigation();
            using var markerScope = UPDATE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            if (telemetryEnabled)
            {
                PerformanceTelemetry.TryRecord(ECS_ACTOR_COUNT_METRIC, _actorComponentQuery.CalculateEntityCount());
                PerformanceTelemetry.TryRecord(NETWORK_GAMEOBJECT_COUNT_METRIC, _actors.Count);
                PerformanceTelemetry.TryRecord(FULL_NPC_COUNT_METRIC, _fullNPCCount);
                PerformanceTelemetry.TryRecord(NEARBY_NPC_COUNT_METRIC, _nearbyNPCCount);
                PerformanceTelemetry.TryRecord(DORMANT_ACTOR_COUNT_METRIC, _dormantNPCCount);
                PerformanceTelemetry.TryRecord(
                    TOTAL_ALLOCATED_MEMORY_METRIC,
                    UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
                PerformanceTelemetry.TryRecord(
                    MONO_USED_MEMORY_METRIC,
                    UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
                PerformanceTelemetry.TryRecord(
                    MONO_HEAP_MEMORY_METRIC,
                    UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong());
                PerformanceTelemetry.TryRecord(
                    GC_GEN0_COLLECTION_COUNT_METRIC,
                    GC.CollectionCount(0));
                PerformanceTelemetry.TryRecord(
                    GC_GEN1_COLLECTION_COUNT_METRIC,
                    GC.CollectionCount(1));
                PerformanceTelemetry.TryRecord(
                    GC_GEN2_COLLECTION_COUNT_METRIC,
                    GC.CollectionCount(2));
            }
        }

        private static int GetNPCAggroDecisionIntervalTicks()
        {
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("NPC aggro scheduling requires initialized server simulation configuration.");

            var configuration = GameConfigurationManager.Config;
            ServerSimulationClock.ValidateConfiguration(configuration);

            float intervalSeconds = configuration.NPCAggroDecisionIntervalSeconds;
            if (float.IsNaN(intervalSeconds)
                || float.IsInfinity(intervalSeconds)
                || intervalSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    "NPCAggroDecisionIntervalSeconds must be finite and strictly positive.");
            }

            float fixedDeltaTime = configuration.ServerSimulationTickDuration;
            double tickCountEstimate = intervalSeconds / (double)fixedDeltaTime;
            if (double.IsInfinity(tickCountEstimate) || tickCountEstimate > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "NPCAggroDecisionIntervalSeconds cannot be represented by a positive integer tick stride.");
            }

            long intervalTicks = ServerSimulationClock.GetNonShorteningTickCount(
                intervalSeconds,
                fixedDeltaTime);
            if (intervalTicks <= 0 || intervalTicks > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "NPCAggroDecisionIntervalSeconds must convert to a positive integer tick stride.");
            }

            return checked((int)intervalTicks);
        }

        private void LateUpdate()
        {
            if (!PerformanceTelemetry.IsEnabled) return;
            using var markerScope = LATE_UPDATE_MARKER.Begin();
            // Tick-scoped bridge telemetry is flushed by ProcessAuthoritativeTickCompleted.
        }

        #endregion

        #region Actor lifecycle

        /// <summary>
        /// Handles the spawn event after an explicit server spawn handoff has registered the
        /// resolved absolute position.
        /// </summary>
        /// <param name="actor">The actor published by the server spawn lifecycle.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the lifecycle publishes an actor without first supplying its resolved position.
        /// </exception>
        private void OnActorSpawned(Actor actor)
        {
            if (actor == null || _actors.ContainsKey(actor.ID))
                return;

            throw new InvalidOperationException(
                $"Actor '{actor.ID}' was published without its resolved authoritative spawn position. "
                + "Register the spawn with ServerActorManager.RegisterSpawnedActor first.");
        }

        /// <summary>
        /// Registers a server spawn with its already-resolved absolute position before the spawn event.
        /// </summary>
        /// <param name="actor">The actor being registered.</param>
        /// <param name="spawnPosition">The server-resolved absolute position to hand to ECS.</param>
        internal void RegisterSpawnedActor(Actor actor, WorldPosition spawnPosition)
        {
            OnActorSpawned(actor, spawnPosition);
        }

        /// <summary>
        /// Creates the ECS actor state and spatial membership for a spawn handoff.
        /// </summary>
        /// <param name="actor">The actor being registered.</param>
        /// <param name="spawnPosition">The resolved absolute position for the new actor.</param>
        private void OnActorSpawned(Actor actor, WorldPosition spawnPosition)
        {
            if (actor == null)
                return;

            if (_actors.TryGetValue(actor.ID, out var existingRegistration))
            {
                if (ReferenceEquals(existingRegistration.Actor, actor))
                    return;

                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' is already registered with the server actor manager.");
            }

            var identity = actor.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' has no NetworkIdentity for server actor registration.");
            }

            bool spatialMembershipInserted = false;
            bool entityCreated = false;
            bool registrationPublished = false;
            bool playerValidationStateTouched = false;
            bool npcActivityCountRegistered = false;
            Entity entity = default;
            try
            {
                ServerECSManager.ClearActorSimulationEventCancellation(actor.ID);
                _spatialIndex.Insert(actor.ID, actor.SpatialContext, spawnPosition);
                spatialMembershipInserted = true;

                entity = _entityManager.CreateEntity();
                entityCreated = true;
                _entityManager.AddComponentData(entity, new Systems.ActorComponent
                {
                    ActorID = actor.ID,
                    ActorName = actor.Name,
                    Owner = actor.Owner ?? default,
                    CurrentScene = default,
                    WorldPositionD = new Unity.Mathematics.double3(
                        spawnPosition.x,
                        spawnPosition.y,
                        spawnPosition.z)
                });
                _entityManager.AddComponentData(entity, new Systems.AcceptedPositionDirtyComponent());
                _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity, false);

                if (actor is NPCActor)
                {
                    _entityManager.AddComponentData(entity, new Systems.NPCActivityComponent
                    {
                        State = Systems.NPCActivityState.Full,
                    });
                    IncrementNPCActivityCount(Systems.NPCActivityState.Full);
                    npcActivityCountRegistered = true;
                }

                if (actor.Owner == null)
                    EnsureNPCMovementComponents(entity, actor);

                if (actor.Owner != null)
                {
                    var owner = actor.Owner.Value;
                    playerValidationStateTouched = true;
                    InitializePlayerValidationState(owner, actor, spawnPosition);
                }

                // Add the spellcasting component on the actor.
                _entityManager.AddComponentData(entity, new Systems.SpellCastComponent());
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, false);

                // Add cooldown buffer for per-spell cooldown tracking.
                _entityManager.AddBuffer<Systems.CooldownElement>(entity);
                _entityManager.SetComponentEnabled<Systems.CooldownElement>(
                    entity,
                    false);

                // Add aura buffer for active aura tracking.
                _entityManager.AddBuffer<Systems.AuraElement>(entity);
                _entityManager.SetComponentEnabled<Systems.AuraElement>(
                    entity,
                    false);

                // Add ticker buffer for generic time-based callbacks.
                _entityManager.AddBuffer<Systems.TickerElement>(entity);
                _entityManager.SetComponentEnabled<Systems.TickerElement>(
                    entity,
                    false);

                _actors.Add(
                    actor.ID,
                    new ActorRegistration(actor, entity, identity));
                registrationPublished = true;

                TrackNPCForAggro(actor);

                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                long snapshotTickID = ServerECSManager.CurrentSimulationTickID;
                actor.SetAcceptedPositionSnapshotFromServer(
                    new WorldPosition(
                        component.WorldPositionD.x,
                        component.WorldPositionD.y,
                        component.WorldPositionD.z),
                    snapshotTickID >= 0 ? snapshotTickID : 0);

                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(SPAWN_EVENT_COUNT_METRIC, 1);
                Debug.Log("Actor spawned on server: " + actor.Name + " (ID: " + actor.ID + ")");
            }
            catch (Exception registrationException)
            {
                var rollbackFailures = new List<Exception>();
                try
                {
                    if (npcActivityCountRegistered)
                    {
                        DecrementNPCActivityCount(Systems.NPCActivityState.Full);
                        npcActivityCountRegistered = false;
                    }
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                try
                {
                    if (spatialMembershipInserted)
                        RemoveSpatialMembership(actor.ID);
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                try
                {
                    if (entityCreated && _entityManager.Exists(entity))
                        _entityManager.DestroyEntity(entity);
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                try
                {
                    if (registrationPublished)
                        _actors.Remove(actor.ID);
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                if (playerValidationStateTouched && actor.Owner.HasValue)
                    RemovePlayerValidationState(actor.Owner.Value);
                _npcAggroStates.Remove(actor.ID);

                if (rollbackFailures.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Actor '{actor.ID}' registration failed and rollback also failed.",
                        new AggregateException(
                            registrationException,
                            new AggregateException(rollbackFailures)));
                }

                throw;
            }

            if (!actor.Owner.HasValue)
            {
                try
                {
                    ServerInterestManager.Instance.ReevaluateActorVisibility(
                        actor,
                        actor.SpatialContext,
                        spawnPosition,
                        actor.SpatialContext,
                        spawnPosition);
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"[Visibility] Post-spawn reevaluation failed for actor '{actor.ID}': {exception.Message}");
                }
            }

            if (actor is PlayerActor player)
            {
                _npcActivityCandidateIDs.Clear();
                AppendNPCActivityCandidates(
                    player.SpatialContext,
                    GetRequiredAcceptedWorldPosition(player.ID),
                    GameConfigurationManager.Config.ActorVisibilityEntryRadius);
                ReevaluateNPCActivityCandidates();
            }
            else if (actor is NPCActor npc && !npc.Owner.HasValue && !npc.isDead)
            {
                ReevaluateNPCActivity(npc);
            }
        }

        /// <summary>Rolls back actor and ECS state created by a rejected spawn attempt.</summary>
        /// <param name="actor">The actor whose spawn was rejected.</param>
        internal void RollbackActorSpawn(Actor actor)
        {
            if (actor == null)
                return;

            if (_actors.TryGetValue(actor.ID, out var registered)
                && !ReferenceEquals(registered.Actor, actor))
            {
                return;
            }

            RemoveSpatialMembership(actor.ID);
            try
            {
                if (_actors.TryGetValue(actor.ID, out var registration)
                    && _entityManager.Exists(registration.Entity))
                {
                    if (actor is NPCActor)
                    {
                        var activity = _entityManager.GetComponentData<Systems.NPCActivityComponent>(
                            registration.Entity);
                        DecrementNPCActivityCount(activity.State);
                    }
                    _entityManager.DestroyEntity(registration.Entity);
                }
            }
            finally
            {
                OnActorDespawned(actor);
            }
        }

        /// <summary>Removes an actor and all player proposal and validation state.</summary>
        /// <param name="actor">The actor being removed.</param>
        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            if (!_actors.TryGetValue(actor.ID, out var registration))
                return;
            if (!ReferenceEquals(registration.Actor, actor))
                return;

            if (actor is PlayerActor player)
            {
                _npcActivityCandidateIDs.Clear();
                AppendNPCActivityCandidates(
                    player.SpatialContext,
                    GetRequiredAcceptedWorldPosition(player.ID),
                    GameConfigurationManager.Config.ActorVisibilityExitRadius);
            }

            RemoveCombatReferencesToActor(actor);

            if (actor.Owner.HasValue)
                ServerInterestManager.Instance.OnPlayerActorRemoved(actor);

            RemoveNPCNavigationRegistration(actor);
            RemoveSpatialMembership(actor.ID);
            ServerECSManager.InvalidateActorSimulationEvents(actor.ID);

            if (_entityManager.Exists(registration.Entity))
            {
                if (actor is NPCActor)
                {
                    var activity = _entityManager.GetComponentData<Systems.NPCActivityComponent>(
                        registration.Entity);
                    DecrementNPCActivityCount(activity.State);
                }
                _entityManager.DestroyEntity(registration.Entity);
            }

            _actors.Remove(actor.ID);

            if (actor.Owner.HasValue)
            {
                var owner = actor.Owner.Value;
                if (_playerActors.TryGetValue(owner, out var playerActor)
                    && ReferenceEquals(playerActor, actor))
                {
                    _pendingSpatialContextTransfers.Remove(owner);
                    RemovePlayerValidationState(owner);
                }
            }
            _npcAggroStates.Remove(actor.ID);

            if (actor is PlayerActor)
                ReevaluateNPCActivityCandidates();
        }

        private void RemoveCombatReferencesToActor(Actor retiredActor)
        {
            if (ReferenceEquals(retiredActor, null))
                return;

            var actors = new List<Actor>(_actors.Count);
            foreach (ActorRegistration registration in _actors.Values)
                actors.Add(registration.Actor);
            foreach (Actor candidate in actors)
            {
                if (candidate == null || ReferenceEquals(candidate, retiredActor))
                    continue;

                if (retiredActor is PlayerActor
                    && _npcAggroStates.TryGetValue(candidate.ID, out var pendingState)
                    && ReferenceEquals(pendingState.PendingTarget, retiredActor))
                {
                    ClearPendingAggro(ref pendingState);
                    _npcAggroStates[candidate.ID] = pendingState;
                }

                if (!ReferenceEquals(candidate.Target, retiredActor))
                    continue;

                if (candidate.Owner == null)
                {
                    if (retiredActor is PlayerActor && candidate is NPCActor)
                    {
                        _npcActivityCandidateIDs.Add(candidate.ID);
                        DropNPCTarget(candidate, reevaluateActivity: false);
                    }
                    else
                    {
                        DropNPCTarget(candidate);
                    }
                }
                else
                {
                    candidate.SetTargetFromServer(null);
                    ActorEndpoint.onServerStopAutoAttack?.Invoke(candidate);
                }
            }

            if (retiredActor is not PlayerActor retiredPlayer)
                return;

            foreach (Actor candidate in actors)
            {
                if (candidate == null || ReferenceEquals(candidate, retiredActor))
                    continue;

                if (!candidate.TryGetComponent<NPCBehavior>(out var npcBehavior))
                    continue;

                bool threatRemoved = npcBehavior.RemoveThreatSource(retiredPlayer);
                if (threatRemoved && candidate is NPCActor && candidate.Owner == null)
                    _npcActivityCandidateIDs.Add(candidate.ID);
            }
        }

        private void RemoveSpatialMembership(Guid actorID)
        {
            try
            {
                _spatialIndex.Remove(actorID);
            }
            catch (KeyNotFoundException)
            {
                // Rollback and repeated despawn notifications are intentionally idempotent.
            }
        }

        private void OnActorDeath(Actor actor)
        {
            if (actor == null)
                return;

            if (!actor.isDead)
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' death callback ran before its dead state was committed.");
            }

            RetireNPCNavigationOnDeath(actor);
            if (_npcAggroStates.TryGetValue(actor.ID, out var state))
            {
                ClearPendingAggro(ref state);
                _npcAggroStates[actor.ID] = state;
            }

            var keys = new List<Guid>(_npcAggroStates.Keys);
            foreach (var id in keys)
            {
                if (!_npcAggroStates.TryGetValue(id, out state)
                    || state.PendingTarget != actor)
                    continue;

                ClearPendingAggro(ref state);
                _npcAggroStates[id] = state;
            }
        }

        private void TrackNPCForAggro(Actor actor)
        {
            if (actor == null)
                return;

            if (actor.Owner != null || actor.isDead)
            {
                _npcAggroStates.Remove(actor.ID);
                return;
            }

            var behavior = actor.GetComponent<NPCBehavior>();
            if (behavior == null)
            {
                _npcAggroStates.Remove(actor.ID);
                return;
            }

            if (float.IsNaN(behavior.AggroRange)
                || float.IsInfinity(behavior.AggroRange))
            {
                throw new InvalidOperationException(
                    $"NPC '{actor.ID}' must have a finite aggro range.");
            }
            if (float.IsNaN(behavior.AggroDelay)
                || float.IsInfinity(behavior.AggroDelay)
                || behavior.AggroDelay < 0f)
            {
                throw new InvalidOperationException(
                    $"NPC '{actor.ID}' must have a finite, non-negative aggro delay.");
            }
            if (behavior.AggroRange <= 0f)
            {
                _npcAggroStates.Remove(actor.ID);
                return;
            }

            if (behavior.AggroRange > GameConfigurationManager.Config.NPCFullEntryRadius)
            {
                throw new InvalidOperationException(
                    $"NPC '{actor.ID}' has aggro range {behavior.AggroRange}, exceeding the configured Full-entry radius "
                    + $"{GameConfigurationManager.Config.NPCFullEntryRadius}.");
            }

            if (_npcAggroStates.ContainsKey(actor.ID))
                return;

            _npcAggroStates[actor.ID] = new NPCAggroState
            {
                NPC = actor,
                Behavior = behavior,
                PendingTarget = null,
                PendingAcquisitionTickID = 0
            };
        }

        private static void ClearPendingAggro(ref NPCAggroState state)
        {
            state.PendingTarget = null;
            state.PendingAcquisitionTickID = 0;
        }

        private WorldPosition GetRequiredAcceptedWorldPosition(Guid actorID)
        {
            if (TryGetAcceptedWorldPosition(actorID, out WorldPosition position))
                return position;

            throw new InvalidOperationException(
                $"Actor {actorID} has no live accepted world position.");
        }

        #endregion

        #region Actor ownership and interest

        /// <summary>
        /// Updates ECS ownership and reinitializes the periodic validation baseline after an
        /// authoritative ownership transition.
        /// </summary>
        /// <param name="actor">The actor whose owner changed.</param>
        private void OnActorOwnerChanged(Actor actor)
        {
            if (_actors.TryGetValue(actor.ID, out var registration))
            {
                var entity = registration.Entity;
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                var previousOwner = component.Owner;
                component.Owner = actor.Owner ?? default;
                _entityManager.SetComponentData(entity, component);

                if (actor is NPCActor && component.Owner != default)
                    SetNPCActivityState(actor.ID, Systems.NPCActivityState.Full);

                // An unowned actor must have the same movement state as a newly registered NPC
                // before the next authoritative movement bridge runs.
                if (component.Owner == default)
                {
                    EnsureNPCMovementComponents(entity, actor);
                }
                else
                {
                    RemoveNPCNavigationRegistration(actor);
                    if (_entityManager.HasComponent<Systems.MotionStateComponent>(entity))
                    {
                        _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(
                            entity,
                            false);
                    }
                }

                if (previousOwner != default
                    && previousOwner != component.Owner
                    && _playerActors.TryGetValue(previousOwner, out var previousActor)
                    && previousActor == actor)
                {
                    _pendingSpatialContextTransfers.Remove(previousOwner);
                    RemovePlayerValidationState(previousOwner);
                }

                if (actor.Owner != null && component.Owner != default)
                {
                    InitializePlayerValidationState(
                        component.Owner,
                        actor,
                        ToWorldPosition(component.WorldPositionD));
                }

                Debug.Log("Actor owner changed on server: " + actor.Name + " (ID: " + actor.ID + ")");
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    OWNER_CHANGE_ENTITY_SCAN_COUNT_METRIC,
                    0,
                    Time.frameCount);

            TrackNPCForAggro(actor);

            if (actor is NPCActor && !actor.Owner.HasValue && !actor.isDead)
                ReevaluateNPCActivity(actor);
        }

        private void SetNPCActivityState(Guid actorID, Systems.NPCActivityState state)
        {
            if (!_actors.TryGetValue(actorID, out var registration))
            {
                throw new InvalidOperationException(
                    $"Cannot set NPC activity for actor '{actorID}' because it is not registered.");
            }

            var component = _entityManager.GetComponentData<Systems.NPCActivityComponent>(registration.Entity);
            if (component.State == state)
                return;

            Systems.NPCActivityState previousState = component.State;
            bool leavingFull = previousState == Systems.NPCActivityState.Full
                && state != Systems.NPCActivityState.Full;
            component.State = state;
            _entityManager.SetComponentData(registration.Entity, component);
            DecrementNPCActivityCount(previousState);
            IncrementNPCActivityCount(state);

            if (leavingFull && _npcAggroStates.TryGetValue(actorID, out var aggroState))
            {
                ClearPendingAggro(ref aggroState);
                _npcAggroStates[actorID] = aggroState;
            }

            ApplyNPCNavigationActivityState(registration.Actor, state);
        }

        private void OnNPCDamaged(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (actor is not NPCActor)
            {
                throw new InvalidOperationException(
                    $"NPC damage notification received for non-NPC actor '{actor.ID}'.");
            }
            if (!_actors.TryGetValue(actor.ID, out var registration)
                || !ReferenceEquals(registration.Actor, actor))
            {
                throw new InvalidOperationException(
                    $"NPC damage notification received for unregistered or replaced actor '{actor.ID}'.");
            }

            if (actor.isDead)
                return;

            SetNPCActivityState(actor.ID, Systems.NPCActivityState.Full);
        }

        private void ReevaluateNPCActivity(Actor npc)
        {
            if (npc == null)
                throw new InvalidOperationException("Cannot reevaluate activity for a null actor.");
            if (npc is not NPCActor)
                throw new InvalidOperationException(
                    $"Cannot reevaluate activity for non-NPC actor '{npc.ID}'.");
            if (!_actors.TryGetValue(npc.ID, out var registration)
                || !ReferenceEquals(registration.Actor, npc))
            {
                throw new InvalidOperationException(
                    $"Cannot reevaluate activity for NPC '{npc.ID}' because it is not the registered actor instance.");
            }

            if (npc.isDead)
                return;

            if (npc.Owner.HasValue)
            {
                SetNPCActivityState(npc.ID, Systems.NPCActivityState.Full);
                return;
            }

            if (npc.Target != null)
            {
                SetNPCActivityState(npc.ID, Systems.NPCActivityState.Full);
                return;
            }

            if (npc.TryGetComponent<NPCBehavior>(out var npcBehavior))
            {
                foreach (var threatEntry in npcBehavior.ThreatTable)
                {
                    PlayerActor player = threatEntry.Key;
                    if (player == null
                        || !_actors.TryGetValue(player.ID, out var playerRegistration)
                        || !ReferenceEquals(playerRegistration.Actor, player)
                        || !player.SpatialContext.IsCompatibleWith(npc.SpatialContext))
                    {
                        continue;
                    }

                    SetNPCActivityState(npc.ID, Systems.NPCActivityState.Full);
                    return;
                }
            }

            WorldPosition npcPosition = GetRequiredAcceptedWorldPosition(npc.ID);
            double nearestPlayerDistanceSquared = double.MaxValue;
            try
            {
                _npcActivityPlayerQueryBuffer.Clear();
                QuerySpatialInRange(
                    npc.SpatialContext,
                    npcPosition,
                    GameConfigurationManager.Config.ActorVisibilityExitRadius,
                    _npcActivityPlayerQueryBuffer,
                    GetRequiredAcceptedWorldPosition,
                    out _,
                    includeVerticalDistance: true);

                foreach (Guid candidateID in _npcActivityPlayerQueryBuffer)
                {
                    if (!TryGetActor(candidateID, out Actor candidateActor))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}', but no live actor mapping exists.");
                    }

                    if (candidateActor is not PlayerActor player
                        || !player.SpatialContext.IsCompatibleWith(npc.SpatialContext))
                    {
                        continue;
                    }

                    WorldPosition playerPosition = GetRequiredAcceptedWorldPosition(player.ID);
                    double deltaX = npcPosition.x - playerPosition.x;
                    double deltaY = npcPosition.y - playerPosition.y;
                    double deltaZ = npcPosition.z - playerPosition.z;
                    double distanceSquared =
                        deltaX * deltaX
                        + deltaY * deltaY
                        + deltaZ * deltaZ;
                    if (distanceSquared < nearestPlayerDistanceSquared)
                        nearestPlayerDistanceSquared = distanceSquared;
                }
            }
            finally
            {
                _npcActivityPlayerQueryBuffer.Clear();
            }

            double fullEntryRadiusSquared =
                GameConfigurationManager.Config.NPCFullEntryRadius
                * GameConfigurationManager.Config.NPCFullEntryRadius;
            double fullExitRadiusSquared =
                GameConfigurationManager.Config.NPCFullExitRadius
                * GameConfigurationManager.Config.NPCFullExitRadius;
            double visibilityEntryRadiusSquared =
                GameConfigurationManager.Config.ActorVisibilityEntryRadius
                * GameConfigurationManager.Config.ActorVisibilityEntryRadius;
            double visibilityExitRadiusSquared =
                GameConfigurationManager.Config.ActorVisibilityExitRadius
                * GameConfigurationManager.Config.ActorVisibilityExitRadius;

            Systems.NPCActivityState currentState =
                _entityManager.GetComponentData<Systems.NPCActivityComponent>(registration.Entity).State;
            Systems.NPCActivityState nextState;
            switch (currentState)
            {
                case Systems.NPCActivityState.Full:
                    nextState = nearestPlayerDistanceSquared <= fullExitRadiusSquared
                        ? Systems.NPCActivityState.Full
                        : nearestPlayerDistanceSquared <= visibilityExitRadiusSquared
                            ? Systems.NPCActivityState.Nearby
                            : Systems.NPCActivityState.Dormant;
                    break;
                case Systems.NPCActivityState.Nearby:
                    nextState = nearestPlayerDistanceSquared <= fullEntryRadiusSquared
                        ? Systems.NPCActivityState.Full
                        : nearestPlayerDistanceSquared <= visibilityExitRadiusSquared
                            ? Systems.NPCActivityState.Nearby
                            : Systems.NPCActivityState.Dormant;
                    break;
                case Systems.NPCActivityState.Dormant:
                    nextState = nearestPlayerDistanceSquared <= fullEntryRadiusSquared
                        ? Systems.NPCActivityState.Full
                        : nearestPlayerDistanceSquared <= visibilityEntryRadiusSquared
                            ? Systems.NPCActivityState.Nearby
                            : Systems.NPCActivityState.Dormant;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"NPC '{npc.ID}' has an unsupported activity state '{currentState}'.");
            }

            SetNPCActivityState(npc.ID, nextState);
        }

        private void AppendNPCActivityCandidates(
            ActorSpatialContext context,
            WorldPosition center,
            double radius)
        {
            _npcActivityCandidateQueryBuffer.Clear();
            try
            {
                QuerySpatialInRange(
                    context,
                    center,
                    radius,
                    _npcActivityCandidateQueryBuffer,
                    GetRequiredAcceptedWorldPosition,
                    out _,
                    includeVerticalDistance: true);

                foreach (Guid candidateID in _npcActivityCandidateQueryBuffer)
                {
                    if (!TryGetActor(candidateID, out Actor candidateActor))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}', but no live actor mapping exists.");
                    }

                    if (candidateActor is NPCActor)
                        _npcActivityCandidateIDs.Add(candidateID);
                }
            }
            finally
            {
                _npcActivityCandidateQueryBuffer.Clear();
            }
        }

        private void ReevaluateNPCActivityCandidates()
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int transitionCount = 0;
            using var markerScope = NPC_ACTIVITY_REEVALUATION_MARKER.Begin();
            try
            {
                foreach (Guid candidateID in _npcActivityCandidateIDs)
                {
                    if (!TryGetActor(candidateID, out Actor candidateActor))
                        continue;
                    if (candidateActor is not NPCActor npc)
                    {
                        throw new InvalidOperationException(
                            $"NPC activity candidate '{candidateID}' is not an NPC actor.");
                    }

                    Systems.NPCActivityState previousState = default;
                    if (telemetryEnabled)
                    {
                        previousState = _entityManager.GetComponentData<Systems.NPCActivityComponent>(
                            _actors[candidateID].Entity).State;
                    }

                    ReevaluateNPCActivity(npc);

                    if (telemetryEnabled
                        && _actors.TryGetValue(candidateID, out var registration)
                        && ReferenceEquals(registration.Actor, npc)
                        && _entityManager.Exists(registration.Entity))
                    {
                        Systems.NPCActivityState currentState = _entityManager.GetComponentData<Systems.NPCActivityComponent>(
                            registration.Entity).State;
                        if (previousState != currentState)
                            transitionCount++;
                    }
                }

                if (telemetryEnabled)
                    PerformanceTelemetry.TryRecord(
                        NPC_ACTIVITY_TRANSITION_COUNT_METRIC,
                        transitionCount);
            }
            finally
            {
                _npcActivityCandidateIDs.Clear();
            }
        }

        private void IncrementNPCActivityCount(Systems.NPCActivityState state)
        {
            switch (state)
            {
                case Systems.NPCActivityState.Full:
                    _fullNPCCount++;
                    break;
                case Systems.NPCActivityState.Nearby:
                    _nearbyNPCCount++;
                    break;
                case Systems.NPCActivityState.Dormant:
                    _dormantNPCCount++;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Cannot increment NPC activity count for unsupported state '{state}'.");
            }
        }

        private void DecrementNPCActivityCount(Systems.NPCActivityState state)
        {
            switch (state)
            {
                case Systems.NPCActivityState.Full:
                    if (_fullNPCCount == 0)
                        throw new InvalidOperationException("Cannot decrement the zero Full NPC activity count.");
                    _fullNPCCount--;
                    break;
                case Systems.NPCActivityState.Nearby:
                    if (_nearbyNPCCount == 0)
                        throw new InvalidOperationException("Cannot decrement the zero Nearby NPC activity count.");
                    _nearbyNPCCount--;
                    break;
                case Systems.NPCActivityState.Dormant:
                    if (_dormantNPCCount == 0)
                        throw new InvalidOperationException("Cannot decrement the zero Dormant NPC activity count.");
                    _dormantNPCCount--;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Cannot decrement NPC activity count for unsupported state '{state}'.");
            }
        }

        private void OnActorInterestChanged(Actor actor, SceneID sceneID)
        {
            if (_actors.TryGetValue(actor.ID, out var registration))
            {
                var entity = registration.Entity;
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                component.CurrentScene = sceneID;
                _entityManager.SetComponentData(entity, component);

                string sceneName = "unknown";
                if (NetworkManager.main.sceneModule.TryGetSceneState(sceneID, out var state))
                    sceneName = state.scene.name;

                Debug.Log("Actor interest changed on server: " + actor.Name + " (ID: " + actor.ID + ") to logical scene " + sceneName);
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    INTEREST_CHANGE_ENTITY_SCAN_COUNT_METRIC,
                    0,
                    Time.frameCount);
        }

        #endregion

        #region NPC aggro processing

        /// <summary>Runs the lower-rate authoritative NPC proximity and aggro decision pass.</summary>
        private void UpdateNPCAggro(ServerSimulationTick tick)
        {
            using var markerScope = NPC_AGGRO_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int playerCenterCount = 0;
            int uniqueCellCount = 0;
            int uniqueNPCCandidateCount = 0;
            int localPairCount = 0;
            int decisionCount = 0;
            int spatialCandidateCount = 0;
            double fullExitRadiusSquared =
                (double)GameConfigurationManager.Config.NPCFullExitRadius
                * GameConfigurationManager.Config.NPCFullExitRadius;

            _npcPerceptionContexts.Clear();
            _npcPerceptionContextPlayers.Clear();
            _npcPerceptionContextPlayerPositions.Clear();
            _npcPerceptionCandidateIDs.Clear();
            try
            {
                foreach (Actor actor in _playerActors.Values)
                {
                    if (actor is not PlayerActor player || player.isDead)
                        continue;
                    if (!player.SpatialContext.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Living player '{player.ID}' has an invalid spatial context during NPC perception.");
                    }

                    _npcPerceptionContexts.Add(player.SpatialContext);
                    playerCenterCount++;
                }

                foreach (ActorSpatialContext context in _npcPerceptionContexts)
                {
                    _npcPerceptionContextPlayers.Clear();
                    _npcPerceptionContextPlayerPositions.Clear();
                    _npcPerceptionCandidateIDs.Clear();

                    foreach (Actor actor in _playerActors.Values)
                    {
                        if (actor is not PlayerActor player
                            || player.isDead
                            || !context.IsCompatibleWith(player.SpatialContext))
                        {
                            continue;
                        }

                        _npcPerceptionContextPlayers.Add(player);
                        _npcPerceptionContextPlayerPositions.Add(
                            GetRequiredAcceptedWorldPosition(player.ID));
                    }

                    _spatialIndex.QueryCandidateUnion(
                        context,
                        _npcPerceptionContextPlayerPositions,
                        GameConfigurationManager.Config.NPCFullExitRadius,
                        _npcPerceptionCandidateIDs,
                        out int examinedCellCount,
                        out int examinedCandidateCount);
                    uniqueCellCount += examinedCellCount;
                    spatialCandidateCount += examinedCandidateCount;

                    foreach (Guid candidateID in _npcPerceptionCandidateIDs)
                    {
                        if (!TryGetActor(candidateID, out _))
                        {
                            throw new InvalidOperationException(
                                $"Spatial index returned actor '{candidateID}', but no live actor mapping exists.");
                        }
                    }

                    foreach (Guid candidateID in _npcPerceptionCandidateIDs)
                    {
                        if (!TryGetActor(candidateID, out Actor candidateActor))
                            continue;

                        if (candidateActor is not NPCActor)
                            continue;

                        uniqueNPCCandidateCount++;
                        if (!_npcAggroStates.TryGetValue(candidateID, out var state))
                            continue;

                        if (state.NPC == null || state.Behavior == null)
                        {
                            _npcAggroStates.Remove(candidateID);
                            continue;
                        }

                        if (state.NPC.isDead)
                        {
                            ClearPendingAggro(ref state);
                            _npcAggroStates[candidateID] = state;
                            continue;
                        }

                        if (state.NPC.Target != null)
                        {
                            ClearPendingAggro(ref state);
                            _npcAggroStates[candidateID] = state;
                            continue;
                        }

                        var activityComponent = _entityManager.GetComponentData<Systems.NPCActivityComponent>(
                            _actors[candidateID].Entity);
                        if (activityComponent.State != Systems.NPCActivityState.Full)
                        {
                            ClearPendingAggro(ref state);
                            _npcAggroStates[candidateID] = state;
                            continue;
                        }

                        decisionCount++;
                        WorldPosition npcPosition = GetRequiredAcceptedWorldPosition(state.NPC.ID);
                        double aggroRangeSquared =
                            (double)state.Behavior.AggroRange * state.Behavior.AggroRange;
                        PlayerActor closest = null;
                        double closestDistanceSquared = double.MaxValue;

                        for (int playerIndex = 0;
                             playerIndex < _npcPerceptionContextPlayers.Count;
                             playerIndex++)
                        {
                            PlayerActor player = _npcPerceptionContextPlayers[playerIndex];
                            if (!_actors.TryGetValue(player.ID, out var playerRegistration)
                                || !ReferenceEquals(playerRegistration.Actor, player)
                                || player.isDead
                                || !player.SpatialContext.IsCompatibleWith(state.NPC.SpatialContext))
                            {
                                continue;
                            }

                            WorldPosition playerPosition =
                                _npcPerceptionContextPlayerPositions[playerIndex];
                            double deltaX = npcPosition.x - playerPosition.x;
                            double deltaY = npcPosition.y - playerPosition.y;
                            double deltaZ = npcPosition.z - playerPosition.z;
                            double distanceSquared =
                                deltaX * deltaX
                                + deltaY * deltaY
                                + deltaZ * deltaZ;
                            if (distanceSquared > fullExitRadiusSquared)
                                continue;

                            localPairCount++;
                            if (distanceSquared > aggroRangeSquared)
                                continue;

                            if (closest == null
                                || distanceSquared < closestDistanceSquared
                                || (distanceSquared == closestDistanceSquared
                                    && player.ID.CompareTo(closest.ID) < 0))
                            {
                                closest = player;
                                closestDistanceSquared = distanceSquared;
                            }
                        }

                        if (closest == null)
                        {
                            ClearPendingAggro(ref state);
                            _npcAggroStates[candidateID] = state;
                            continue;
                        }

                        if (state.PendingTarget != closest)
                        {
                            state.PendingTarget = closest;
                            long delayTicks = GetNonShorteningAggroDelayTicks(state.Behavior.AggroDelay);
                            state.PendingAcquisitionTickID = delayTicks == 0
                                ? tick.TickID
                                : checked(tick.TickID + delayTicks);
                        }

                        if (tick.TickID >= state.PendingAcquisitionTickID)
                        {
                            SetNPCActivityState(state.NPC.ID, Systems.NPCActivityState.Full);
                            Debug.Log($"NPC '{state.NPC.Name}' aggroed player '{closest.Name}'.");
                            state.NPC.SetTargetFromServer(closest);
                            ActorEndpoint.onServerStartAutoAttack?.Invoke(state.NPC);

                            if (!_actors.TryGetValue(candidateID, out var currentRegistration)
                                || !ReferenceEquals(currentRegistration.Actor, state.NPC))
                            {
                                _npcAggroStates.Remove(candidateID);
                                continue;
                            }

                            ClearPendingAggro(ref state);
                        }

                        _npcAggroStates[candidateID] = state;
                    }
                }

                if (telemetryEnabled)
                {
                    PerformanceTelemetry.TryRecord(
                        NPC_PERCEPTION_PLAYER_CENTER_COUNT_METRIC,
                        playerCenterCount);
                    PerformanceTelemetry.TryRecord(
                        NPC_PERCEPTION_UNIQUE_CELL_COUNT_METRIC,
                        uniqueCellCount);
                    PerformanceTelemetry.TryRecord(
                        NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT_METRIC,
                        uniqueNPCCandidateCount);
                    PerformanceTelemetry.TryRecord(
                        NPC_PERCEPTION_LOCAL_PAIR_COUNT_METRIC,
                        localPairCount);
                    PerformanceTelemetry.TryRecord(
                        NPC_PERCEPTION_DECISION_COUNT_METRIC,
                        decisionCount);
                    PerformanceTelemetry.TryRecord(OVERLAP_SEARCH_COUNT_METRIC, 0);
                    PerformanceTelemetry.TryRecord(COLLIDER_CANDIDATE_COUNT_METRIC, 0);
                    PerformanceTelemetry.TryRecord(SPATIAL_CANDIDATE_COUNT_METRIC, spatialCandidateCount);
                    PerformanceTelemetry.TryRecord(ALLOC_HEAVY_OPERATION_COUNT_METRIC, 0);
                }
            }
            finally
            {
                _npcPerceptionContexts.Clear();
                _npcPerceptionContextPlayers.Clear();
                _npcPerceptionContextPlayerPositions.Clear();
                _npcPerceptionCandidateIDs.Clear();
            }
        }

        private static long GetNonShorteningAggroDelayTicks(float delaySeconds)
        {
            if (delaySeconds == 0f)
                return 0;

            float fixedDeltaTime = GameConfigurationManager.Config.ServerSimulationTickDuration;
            return ServerSimulationClock.GetNonShorteningTickCount(delaySeconds, fixedDeltaTime);
        }

        #endregion

        #region Authoritative ECS tick bridge

        /// <summary>
        /// Commits the newest authenticated player proposal before every authoritative ECS tick,
        /// while applying the periodic speed check against its fixed validation baseline.
        /// </summary>
        internal void ProcessAuthoritativeTick(ServerSimulationTick tick)
        {
            bool playerPhysicalTransformsChanged =
                ValidatePlayerPositions(tick);
            if (playerPhysicalTransformsChanged)
                Physics.SyncTransforms();
            SynchronizeNPCPositionsToECS();
        }

        /// <summary>
        /// Publishes changed canonical actor positions after the authoritative ECS simulation step.
        /// </summary>
        internal void ProcessAuthoritativeTickCompleted()
        {
            PublishDirtyAcceptedActorPositions(
                ServerECSManager.CurrentSimulationTickID);
            FlushPendingTickSyncContribution();
        }

        private void PublishDirtyAcceptedActorPositions(long tickID)
        {
            if (tickID < 0)
                throw new InvalidOperationException($"Cannot publish accepted actor positions for tick {tickID}.");

            _acceptedVisibilityMovements.Clear();
            int gameObjectSyncCount = 0;
            var entities = _acceptedPositionDirtyQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (!_actors.TryGetValue(component.ActorID, out ActorRegistration registration))
                    {
                        throw new InvalidOperationException(
                            $"Dirty actor entity has no registered actor for '{component.ActorID}'.");
                    }

                    var actor = registration.Actor;
                    if (!actor.TryGetAcceptedPositionSnapshot(out ActorPositionSnapshot previousSnapshot))
                    {
                        throw new InvalidOperationException(
                            $"Actor '{actor.ID}' has no accepted position snapshot for dirty publication.");
                    }

                    var position = new WorldPosition(
                        component.WorldPositionD.x,
                        component.WorldPositionD.y,
                        component.WorldPositionD.z);
                    var actorContext = actor.SpatialContext;

                    if (!actor.isDead
                        && _entityManager.HasComponent<Systems.MotionStateComponent>(entity)
                        && _entityManager.IsComponentEnabled<Systems.MotionStateComponent>(entity))
                    {
                        actor.transform.position = ServerPhysicsManager.Instance.ToPhysicalPosition(position);
                        gameObjectSyncCount++;
                    }

                    _spatialIndex.Move(
                        actor.ID,
                        actorContext,
                        position);
                    UpdateAcceptedActorChunkMembership(actor, position, registration.Identity);
                    actor.SetAcceptedPositionSnapshotFromServer(position, tickID);
                    NetworkingPerformanceMetrics.RecordMovementPresentationObserverPublication(registration.Identity);
                    _acceptedVisibilityMovements.Add(new AcceptedVisibilityMovement(
                        actor,
                        previousSnapshot.Position,
                        position,
                        actorContext));
                    _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity, false);
                }
            }
            finally
            {
                entities.Dispose();
            }

            if (_acceptedVisibilityMovements.Count == 0)
                return;

            _npcActivityCandidateIDs.Clear();
            foreach (AcceptedVisibilityMovement movement in _acceptedVisibilityMovements)
            {
                if (movement.Actor is PlayerActor)
                {
                    AppendNPCActivityCandidates(
                        movement.Context,
                        movement.PreviousPosition,
                        GameConfigurationManager.Config.ActorVisibilityExitRadius);
                    AppendNPCActivityCandidates(
                        movement.Context,
                        movement.CurrentPosition,
                        GameConfigurationManager.Config.ActorVisibilityEntryRadius);
                }
                else if (movement.Actor is NPCActor)
                {
                    _npcActivityCandidateIDs.Add(movement.Actor.ID);
                }
            }

            ReevaluateNPCActivityCandidates();
            ServerInterestManager.Instance.ReevaluateActorVisibilityBatch(_acceptedVisibilityMovements);

            if (PerformanceTelemetry.IsEnabled && gameObjectSyncCount > 0)
                RecordTickSyncContribution(0, gameObjectSyncCount, tickID);
        }

        private static void UpdateAcceptedActorChunkMembership(
            Actor actor,
            WorldPosition position,
            NetworkIdentity identity)
        {
            Vector2Int chunk = position.Chunk;
            string destinationSceneName = $"World_{chunk.x}_{chunk.y}";
            string sourceSceneName = actor.CurrentChunk;
            if (StringComparer.Ordinal.Equals(sourceSceneName, destinationSceneName))
                return;

            if (string.IsNullOrEmpty(sourceSceneName))
            {
                ChunkSubscriptions.RegisterIdentity(
                    actor.SpatialContext,
                    destinationSceneName,
                    identity);
            }
            else
            {
                ChunkSubscriptions.MoveIdentity(
                    actor.SpatialContext,
                    sourceSceneName,
                    destinationSceneName,
                    identity);
            }

            actor.CurrentChunk = destinationSceneName;
        }

        #endregion

        #region Instance validation and context transfer

        private void OnInstanceValidationToggleRequested(PlayerID playerID)
        {
            if (_pendingSpatialContextTransfers.Contains(playerID))
            {
                Debug.Warning(
                    $"[InstanceValidation] Ignoring duplicate toggle request from player {playerID} while pending.");
                return;
            }

            if (!_playerActors.TryGetValue(playerID, out var actor)
                || actor == null
                || !actor.Owner.HasValue
                || actor.Owner.Value != playerID)
            {
                NotifyInstanceValidationToggleFailure(
                    playerID,
                    null,
                    "the requesting player has no live owned actor");
                return;
            }

            ActorSpatialContext sourceContext = actor.SpatialContext;
            ActorSpatialContext instanceA = ServerWorldManager.Instance.ValidationInstanceA;
            ActorSpatialContext instanceB = ServerWorldManager.Instance.ValidationInstanceB;

            ActorSpatialContext destinationContext;
            string destinationLabel;
            string sourceLabel;
            if (sourceContext.Equals(instanceA))
            {
                destinationContext = instanceB;
                destinationLabel = "B";
                sourceLabel = "A";
            }
            else if (sourceContext.Equals(instanceB))
            {
                destinationContext = instanceA;
                destinationLabel = "A";
                sourceLabel = "B";
            }
            else
            {
                destinationContext = instanceA;
                destinationLabel = "A";
                sourceLabel = string.Empty;
            }

            bool started = TryBeginPlayerSpatialContextTransfer(
                actor,
                destinationContext,
                (succeeded, reason) =>
            {
                if (!succeeded)
                {
                    NotifyInstanceValidationToggleFailure(
                        playerID,
                        actor,
                        reason);
                    return;
                }

                string transition = string.IsNullOrEmpty(sourceLabel)
                    ? $"entered {destinationLabel}"
                    : $"switched {sourceLabel} -> {destinationLabel}";
                if (TryGetAcceptedWorldPosition(actor.ID, out WorldPosition position))
                {
                    Debug.Log(
                        $"[InstanceValidation] Player {actor.ID} {transition} "
                        + $"instance={destinationContext.InstanceID} "
                        + $"position=({position.x:F3},{position.y:F3},{position.z:F3})");
                }
                ActorEndpoint.Client_InstanceValidationToggleResult(
                    playerID,
                    true,
                    destinationLabel,
                    destinationContext.InstanceID);
            });

            if (!started)
                Debug.Warning(
                    $"[InstanceValidation] Ignoring duplicate toggle request from player {playerID} while pending.");
        }

        private bool TryBeginPlayerSpatialContextTransfer(
            Actor actor,
            ActorSpatialContext destinationContext,
            Action<bool, string> onCompleted)
        {
            return TryBeginPlayerSpatialContextTransfer(
                actor,
                destinationContext,
                null,
                onCompleted);
        }

        private bool TryBeginPlayerSpatialContextTransfer(
            Actor actor,
            ActorSpatialContext destinationContext,
            Action<Action<bool>> prepareDestination,
            Action<bool, string> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            PlayerID owner = default;
            bool hasOwner = actor != null && actor.Owner.HasValue;
            if (hasOwner)
            {
                owner = actor.Owner.Value;
                if (_pendingSpatialContextTransfers.Contains(owner))
                    return false;
            }

            string validationFailure = null;
            ActorSpatialContext sourceContext = default;
            WorldPosition sourcePosition = default;

            if (actor == null || !hasOwner)
            {
                validationFailure = "a live owned player actor is required";
            }
            else if (!_playerActors.TryGetValue(owner, out Actor playerActor)
                || !ReferenceEquals(playerActor, actor))
            {
                validationFailure = "the player actor is not the live exact registered actor";
            }
            else
            {
                sourceContext = actor.SpatialContext;
                ActorSpatialContext runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
                if (!runtimeContext.IsValid || !sourceContext.IsValid || !destinationContext.IsValid)
                    validationFailure = "source and destination contexts must be valid";
                else if (sourceContext == destinationContext)
                    validationFailure = "source and destination contexts are identical";
                else if (sourceContext.WorldID != destinationContext.WorldID
                    || !StringComparer.Ordinal.Equals(sourceContext.MapID, destinationContext.MapID))
                    validationFailure = "source and destination must belong to the same world and map";
                else if (destinationContext.InstanceID == Guid.Empty
                    && destinationContext != runtimeContext)
                    validationFailure = "a public destination must be the current runtime context";
                else if (destinationContext.InstanceID != Guid.Empty
                    && (destinationContext.WorldID != runtimeContext.WorldID
                        || !StringComparer.Ordinal.Equals(destinationContext.MapID, runtimeContext.MapID)))
                    validationFailure = "a private destination must belong to the current server runtime";
                else
                {
                    try
                    {
                        if (!TryGetAcceptedWorldPosition(actor.ID, out sourcePosition))
                            validationFailure = "the player actor has no canonical accepted position";
                    }
                    catch (Exception exception)
                    {
                        validationFailure = $"the canonical accepted position could not be read: {exception.Message}";
                    }
                }
            }

            if (validationFailure != null)
            {
                onCompleted(false, validationFailure);
                return true;
            }

            _pendingSpatialContextTransfers.Add(owner);
            RemovePendingPositionProposal(owner);

            bool completionDelivered = false;
            void Complete(bool succeeded, string reason)
            {
                if (completionDelivered)
                    return;

                completionDelivered = true;
                if (_playerActors.TryGetValue(owner, out Actor currentActor)
                    && ReferenceEquals(currentActor, actor))
                {
                    _pendingSpatialContextTransfers.Remove(owner);
                }

                onCompleted(succeeded, reason);
            }

            void OnDestinationReady(bool destinationReady)
            {
                if (completionDelivered)
                    return;
                if (!destinationReady)
                {
                    Complete(false, "destination environment or population readiness failed");
                    return;
                }

                string validationFailure = null;
                try
                {
                    if (!_pendingSpatialContextTransfers.Contains(owner))
                    {
                        validationFailure = "the pending context transfer was cancelled";
                    }
                    else if (!_playerActors.TryGetValue(owner, out Actor currentActor)
                        || !ReferenceEquals(currentActor, actor))
                    {
                        validationFailure = "the requesting player actor changed during destination readiness";
                    }
                    else if (!actor.Owner.HasValue
                        || actor.Owner.Value != owner)
                    {
                        validationFailure = "the player actor is no longer live before transfer";
                    }
                    else if (actor.SpatialContext != sourceContext)
                    {
                        validationFailure = "the source spatial context changed during destination readiness";
                    }
                    else if (!TryGetAcceptedWorldPosition(actor.ID, out WorldPosition currentPosition)
                        || !AreWorldPositionsEqual(currentPosition, sourcePosition))
                    {
                        validationFailure = "the canonical accepted position changed during destination readiness";
                    }
                }
                catch (Exception exception)
                {
                    validationFailure = $"transfer revalidation failed: {exception.Message}";
                }

                if (validationFailure != null)
                {
                    Complete(false, validationFailure);
                    return;
                }

                string transferFailure = null;
                try
                {
                    TransferActorSpatialContext(actor, destinationContext, sourcePosition);
                }
                catch (Exception exception)
                {
                    transferFailure = $"context transfer failed: {exception.Message}";
                }

                Complete(transferFailure == null, transferFailure);
            }

            try
            {
                if (prepareDestination != null)
                {
                    prepareDestination(OnDestinationReady);
                }
                else if (destinationContext.InstanceID == Guid.Empty)
                {
                    ServerInterestManager.Instance.EnsureInterestGridLoaded(
                        destinationContext,
                        sourcePosition,
                        OnDestinationReady);
                }
                else
                {
                    ServerInterestManager.Instance.EnsureDestinationReadyForAdmission(
                        destinationContext,
                        sourcePosition,
                        OnDestinationReady);
                }
            }
            catch (Exception exception)
            {
                Complete(false, $"destination readiness could not start: {exception.Message}");
            }

            return true;
        }

        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            _pendingSpatialContextTransfers.Remove(playerID);
        }

        private static void NotifyInstanceValidationToggleFailure(
            PlayerID playerID,
            Actor actor,
            string reason)
        {
            string actorID = actor == null ? "<none>" : actor.ID.ToString();
            Debug.Error(
                $"[InstanceValidation] Toggle failed for player {playerID} actor={actorID}: {reason}");
            ActorEndpoint.Client_InstanceValidationToggleResult(
                playerID,
                false,
                string.Empty,
                Guid.Empty);
        }

        private static bool AreWorldPositionsEqual(WorldPosition left, WorldPosition right)
        {
            return left.x == right.x
                && left.y == right.y
                && left.z == right.z;
        }

        /// <summary>
        /// Transfers one live actor between authoritative logical contexts and projects all owned
        /// position, spatial, visibility and observer state as one server transaction.
        /// </summary>
        /// <param name="actor">The registered live actor to transfer.</param>
        /// <param name="destinationContext">The already-authorised destination logical context.</param>
        /// <param name="destinationPosition">The already-authorised destination absolute world position.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="actor"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when destination input is invalid or unchanged.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when authoritative actor state or destination environment is unavailable, inconsistent,
        /// or a transfer/rollback operation fails.
        /// </exception>
        internal void TransferActorSpatialContext(
            Actor actor,
            ActorSpatialContext destinationContext,
            WorldPosition destinationPosition)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            ActorSpatialContext sourceContext = actor.SpatialContext;
            if (destinationContext.Equals(sourceContext))
                throw new ArgumentException(
                    "A logical-context transfer requires a different destination context.",
                    nameof(destinationContext));

            if (!_actors.TryGetValue(actor.ID, out var registration)
                || !ReferenceEquals(registration.Actor, actor))
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' is not registered as the exact server actor instance.");
            }
            var entity = registration.Entity;
            var originalComponent = _entityManager.GetComponentData<Systems.ActorComponent>(entity);

            var sourcePosition = new WorldPosition(
                originalComponent.WorldPositionD.x,
                originalComponent.WorldPositionD.y,
                originalComponent.WorldPositionD.z);
            if (!sourcePosition.IsFinite)
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' has a non-finite canonical source position.");

            var destinationChunk = destinationPosition.Chunk;
            var transferRuntime = ServerWorldManager.Instance.IsTransferRuntime(destinationContext);
            if (!transferRuntime
                && !ServerWorldManager.Instance.IsChunkLoaded(destinationContext, destinationChunk))
            {
                throw new InvalidOperationException(
                    $"Destination chunk '{destinationChunk}' is not loaded for actor context transfer.");
            }

            PlayerID? owner = actor.Owner;
            if (owner.HasValue)
            {
                if (!ServerInterestManager.Instance.IsPlayerDestinationEnvironmentReady(
                        owner.Value,
                        destinationContext,
                        destinationPosition))
                {
                    throw new InvalidOperationException(
                        $"Destination interest grid is not fully loaded for actor '{actor.ID}'.");
                }
            }

            var destinationPhysicalPosition =
                ServerPhysicsManager.Instance.ToPhysicalPosition(destinationPosition);
            long transferTickID = ServerECSManager.CurrentSimulationTickID;
            if (transferTickID < 0)
                throw new InvalidOperationException(
                    $"Actor context transfer requires a nonnegative simulation tick, received {transferTickID}.");

            var sourcePhysicalPosition = actor.transform.position;
            var sourceSceneName = actor.CurrentChunk;
            var destinationSceneName = $"World_{destinationChunk.x}_{destinationChunk.y}";
            var identity = registration.Identity;

            // Targets are live relationships, not part of spatial membership. Retire any
            // relationship that cannot survive the destination context before publishing it.
            if (actor is PlayerActor)
                _npcActivityCandidateIDs.Clear();
            try
            {
                ClearIncompatibleTargetsForTransfer(actor, destinationContext);
            }
            catch
            {
                if (actor is PlayerActor)
                    _npcActivityCandidateIDs.Clear();
                throw;
            }

            bool spatialMembershipTransferred = false;
            bool actorContextTransferred = false;
            bool componentPositionCommitted = false;
            bool identityTransferred = false;
            bool currentChunkChanged = false;
            bool physicalPositionChanged = false;
            bool playerValidationStateInitialized = false;
            bool destinationInterestCallbackStarted = false;
            bool acceptedPositionDirtyBeforeTransfer =
                _entityManager.IsComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity);

            try
            {
                GetSpatialIndex().Transfer(
                    actor.ID,
                    destinationContext,
                    destinationPosition);
                spatialMembershipTransferred = true;

                actor.TransferAuthoritativeSpatialContext(
                    sourceContext,
                    destinationContext);
                actorContextTransferred = true;

                var destinationComponent = originalComponent;
                destinationComponent.WorldPositionD = new Unity.Mathematics.double3(
                    destinationPosition.x,
                    destinationPosition.y,
                    destinationPosition.z);
                _entityManager.SetComponentData(entity, destinationComponent);
                componentPositionCommitted = true;

                ChunkSubscriptions.TransferIdentity(
                    sourceContext,
                    sourceSceneName,
                    destinationContext,
                    destinationSceneName,
                    identity);
                identityTransferred = true;

                actor.CurrentChunk = destinationSceneName;
                currentChunkChanged = true;

                actor.transform.position = destinationPhysicalPosition;
                physicalPositionChanged = true;

                if (owner.HasValue)
                {
                    InitializePlayerValidationState(owner.Value, actor, destinationPosition);
                    playerValidationStateInitialized = true;

                    if (ActorEndpoint.onServerAcceptedPlayerPositionChanged != null)
                    {
                        destinationInterestCallbackStarted = true;
                        ActorEndpoint.onServerAcceptedPlayerPositionChanged.Invoke(
                            actor,
                            destinationPosition);
                    }
                }

                actor.SetAcceptedPositionSnapshotFromServer(
                    destinationPosition,
                    transferTickID);
                _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity, false);
            }
            catch (Exception transferException)
            {
                var rollbackFailures = new List<Exception>();
                bool actorContextRestoredForInterest = false;

                if (destinationInterestCallbackStarted && actorContextTransferred)
                {
                    // ServerInterestManager derives the callback context from Actor.SpatialContext.
                    // Restore the actor context before projecting interest back to the source context;
                    // this is the same context rollback otherwise performed below.
                    try
                    {
                        actor.TransferAuthoritativeSpatialContext(
                            destinationContext,
                            sourceContext);
                        actorContextTransferred = false;
                        actorContextRestoredForInterest = true;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (destinationInterestCallbackStarted && actorContextRestoredForInterest)
                {
                    try
                    {
                        ActorEndpoint.onServerAcceptedPlayerPositionChanged?.Invoke(
                            actor,
                            sourcePosition);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (playerValidationStateInitialized && owner.HasValue)
                {
                    try
                    {
                        InitializePlayerValidationState(owner.Value, actor, sourcePosition);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (physicalPositionChanged)
                {
                    try
                    {
                        actor.transform.position = sourcePhysicalPosition;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (currentChunkChanged)
                {
                    try
                    {
                        actor.CurrentChunk = sourceSceneName;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (identityTransferred)
                {
                    try
                    {
                        ChunkSubscriptions.TransferIdentity(
                            destinationContext,
                            destinationSceneName,
                            sourceContext,
                            sourceSceneName,
                            identity);
                        identityTransferred = false;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (componentPositionCommitted)
                {
                    try
                    {
                        _entityManager.SetComponentData(entity, originalComponent);
                        componentPositionCommitted = false;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                try
                {
                    _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(
                        entity,
                        acceptedPositionDirtyBeforeTransfer);
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                if (actorContextTransferred)
                {
                    try
                    {
                        actor.TransferAuthoritativeSpatialContext(
                            destinationContext,
                            sourceContext);
                        actorContextTransferred = false;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (spatialMembershipTransferred)
                {
                    try
                    {
                        GetSpatialIndex().Transfer(
                            actor.ID,
                            sourceContext,
                            sourcePosition);
                        spatialMembershipTransferred = false;
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                _npcActivityCandidateIDs.Clear();
                if (rollbackFailures.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Actor '{actor.ID}' context transfer failed and rollback also failed.",
                        new AggregateException(transferException, new AggregateException(rollbackFailures)));
                }

                throw;
            }

            if (actor is PlayerActor transferredPlayer)
            {
                foreach (Guid candidateID in _npcActivityCandidateIDs)
                {
                    if (!TryGetActor(candidateID, out Actor candidateActor)
                        || candidateActor is not NPCActor other
                        || other.Owner.HasValue)
                    {
                        continue;
                    }

                    if (other.TryGetComponent<NPCBehavior>(out var npcBehavior))
                        npcBehavior.RemoveThreatSource(transferredPlayer);
                }

                AppendNPCActivityCandidates(
                    sourceContext,
                    sourcePosition,
                    GameConfigurationManager.Config.ActorVisibilityExitRadius);
                AppendNPCActivityCandidates(
                    destinationContext,
                    destinationPosition,
                    GameConfigurationManager.Config.ActorVisibilityEntryRadius);
                ReevaluateNPCActivityCandidates();
            }
            else if (actor is NPCActor)
            {
                _npcActivityCandidateIDs.Clear();
                _npcActivityCandidateIDs.Add(actor.ID);
                ReevaluateNPCActivityCandidates();
            }

            try
            {
                ServerInterestManager.Instance.ReevaluateActorVisibility(
                    actor,
                    sourceContext,
                    sourcePosition,
                    destinationContext,
                    destinationPosition);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"[Visibility] Post-transfer reevaluation failed for actor '{actor.ID}': {exception.Message}");
            }
        }

        private void ClearIncompatibleTargetsForTransfer(
            Actor actor,
            ActorSpatialContext destinationContext)
        {
            var actors = new List<Actor>(_actors.Count);
            foreach (ActorRegistration registration in _actors.Values)
                actors.Add(registration.Actor);

            foreach (Actor other in actors)
            {
                if (other == null)
                    continue;

                bool losesTarget = ReferenceEquals(other, actor)
                    ? other.Target != null
                      && !destinationContext.IsCompatibleWith(other.Target.SpatialContext)
                    : ReferenceEquals(other.Target, actor)
                      && !other.SpatialContext.IsCompatibleWith(destinationContext);
                if (_npcAggroStates.TryGetValue(other.ID, out var state)
                    && (losesTarget
                        || ReferenceEquals(other, actor)
                           && state.PendingTarget != null
                           && !destinationContext.IsCompatibleWith(state.PendingTarget.SpatialContext)
                        || ReferenceEquals(state.PendingTarget, actor)
                           && !other.SpatialContext.IsCompatibleWith(destinationContext)))
                {
                    ClearPendingAggro(ref state);
                    _npcAggroStates[other.ID] = state;
                }

                if (actor is PlayerActor transferringPlayer
                    && other is NPCActor
                    && other.Owner == null
                    && !destinationContext.IsCompatibleWith(other.SpatialContext)
                    && other.TryGetComponent<NPCBehavior>(out var npcBehavior)
                    && npcBehavior.ThreatTable.ContainsKey(transferringPlayer))
                {
                    _npcActivityCandidateIDs.Add(other.ID);
                }

                if (!losesTarget)
                    continue;

                if (other.Owner == null)
                {
                    if (actor is PlayerActor && other is NPCActor)
                    {
                        _npcActivityCandidateIDs.Add(other.ID);
                        DropNPCTarget(other, reevaluateActivity: false);
                    }
                    else
                    {
                        DropNPCTarget(other);
                    }
                }
                else
                {
                    other.SetTargetFromServer(null);
                    ActorEndpoint.onServerStopAutoAttack?.Invoke(other);
                }
            }
        }

        #endregion

    }
}
