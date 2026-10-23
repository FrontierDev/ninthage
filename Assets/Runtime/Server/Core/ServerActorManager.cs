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
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server.Core
{
    /// <summary>
    /// The server actor manager handles all server-side actors, whether they are NPCs or PCs.
    /// Player movement proposals are owner-authored and validated at the authoritative
    /// simulation boundary before they reach ECS.
    /// </summary>
    public sealed partial class ServerActorManager : MonoBehaviour
    {
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
        private static readonly PerformanceMetricDefinition DORMANT_ACTOR_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTORS_DORMANT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition SPATIAL_CANDIDATE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPATIAL_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_update");
        private static readonly PerformanceMarker UPDATE_MARKER = new(UPDATE_DURATION_METRIC, "Server.ActorManager.Update");
        private static readonly PerformanceMarker LATE_UPDATE_MARKER = new(LATE_UPDATE_DURATION_METRIC, "Server.ActorManager.LateUpdate");
        private static readonly PerformanceMarker NPC_AGGRO_MARKER = new(NPC_AGGRO_DURATION_METRIC, "Server.ActorManager.ScheduledNPCAggro");
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
            PerformanceTelemetry.RegisterMetric(POSITION_VALIDATION_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(PLAYER_ECS_SYNC_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
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
            PerformanceTelemetry.RegisterMetric(
                DORMANT_ACTOR_COUNT_METRIC,
                0,
                PerformanceMetricObservation.Unavailable,
                "The server has no project-owned dormant actor state probe.");
            PerformanceTelemetry.RegisterMetric(
                SPATIAL_CANDIDATE_COUNT_METRIC,
                0,
                PerformanceMetricObservation.Unavailable,
                "The server has no project-owned spatial candidate probe; NPC overlap candidates are recorded separately.");
        }

        // ECS Management.
        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        // Maps actor ID -> Actor MonoBehaviour for authoritative movement bridges.
        private readonly Dictionary<Guid, Actor> _actorObjects = new();
        private readonly Dictionary<Guid, Entity> _actorEntities = new();
        private ServerSpatialIndex _spatialIndex;

        /// <summary>Finds the server actor associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="actor">Receives the actor when found.</param>
        /// <returns><see langword="true"/> when an actor was found.</returns>
        public bool TryGetActor(Guid actorID, out Actor actor) => _actorObjects.TryGetValue(actorID, out actor);

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

        /// <summary>Finds the authoritative ECS entity associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="entity">Receives the authoritative ECS entity when found.</param>
        /// <returns><see langword="true"/> when the actor has a live authoritative entity.</returns>
        internal bool TryGetActorEntity(Guid actorID, out Entity entity) => _actorEntities.TryGetValue(actorID, out entity);

        // NPC aggro tracking.
        private struct NPCAggroState
        {
            public Actor NPC;
            public NPCBehavior Behavior;
            public NavMeshAgent Agent;
            public Actor PendingTarget;
            public long PendingAcquisitionTickID;
        }
        private readonly Dictionary<Guid, NPCAggroState> _npcAggroStates = new();
        private ServerSimulationRegistrationHandle _npcAggroSchedule;
        private int _npcAggroDecisionIntervalTicks;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerActorManager is already initialized!");
                return;
            }
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("Server spatial indexing requires initialized game configuration.");

            _npcAggroDecisionIntervalTicks = GetNPCAggroDecisionIntervalTicks();
            _spatialIndex = new ServerSpatialIndex(GameConfigurationManager.Config.SpatialIndexCellWidth);
            _instance = this;

            ActorEndpoint.onServerActorSpawned += OnActorSpawned;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged += OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged += OnActorInterestChanged;
            ActorEndpoint.onActorDeath += OnActorDeath;
            MovementEndpoint.onServerPositionProposal += OnServerPositionProposal;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            _initialized = true;
            EnsureNPCAggroScheduleRegistered();
        }

        private void OnDestroy()
        {
            _initialized = false;

            _npcAggroSchedule.Cancel();
            _npcAggroSchedule = default;

            ActorEndpoint.onServerActorSpawned -= OnActorSpawned;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged -= OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged -= OnActorInterestChanged;
            ActorEndpoint.onActorDeath -= OnActorDeath;
            MovementEndpoint.onServerPositionProposal -= OnServerPositionProposal;

            foreach (var actorID in _actorObjects.Keys)
                RemoveSpatialMembership(actorID);

            foreach (var actorID in _actorEntities.Keys)
            {
                if (!_actorObjects.ContainsKey(actorID))
                    RemoveSpatialMembership(actorID);
            }

            _actorObjects.Clear();
            _actorEntities.Clear();
            _playerActors.Clear();
            _periodicValidationBaselines.Clear();
            _lastValidationTickIDs.Clear();
            _nextValidationTickIDs.Clear();
            _violationCounters.Clear();
            _pendingPositionProposals.Clear();
            _lastProposalOrder.Clear();
            _npcAggroStates.Clear();
            _pendingTickID = -1;
            _pendingTickEntityScanCount = 0;
            _pendingTickGameObjectSyncCount = 0;
            _hasPendingTickSyncContribution = false;
            _spatialIndex = null;
            _instance = null;
        }

        private ServerSpatialIndex GetSpatialIndex()
        {
            if (_spatialIndex == null)
                throw new InvalidOperationException("The server spatial index is not initialized.");

            return _spatialIndex;
        }

        private void Update()
        {
            if (!_initialized) return;
            EnsureNPCAggroScheduleRegistered();
            using var markerScope = UPDATE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            if (telemetryEnabled)
            {
                PerformanceTelemetry.TryRecord(ECS_ACTOR_COUNT_METRIC, _actorComponentQuery.CalculateEntityCount());
                PerformanceTelemetry.TryRecord(NETWORK_GAMEOBJECT_COUNT_METRIC, _actorObjects.Count);
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

        private void EnsureNPCAggroScheduleRegistered()
        {
            if (!ServerSimulationClock.IsRunning)
            {
                if (_npcAggroSchedule.IsValid)
                {
                    _npcAggroSchedule.Cancel();
                    _npcAggroSchedule = default;
                }

                return;
            }

            if (!_npcAggroSchedule.IsValid)
            {
                _npcAggroSchedule = ServerSimulationClock.RegisterLowerRate(
                    _npcAggroDecisionIntervalTicks,
                    UpdateNPCAggro);
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
            if (!_initialized || !PerformanceTelemetry.IsEnabled) return;
            using var markerScope = LATE_UPDATE_MARKER.Begin();
            // Tick-scoped bridge telemetry is flushed by ProcessAuthoritativeTickCompleted.
        }

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
            if (actor == null || _actorObjects.ContainsKey(actor.ID) || _actorEntities.ContainsKey(actor.ID))
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
            if (actor == null || !_initialized || _spatialIndex == null)
                return;

            bool hasExistingObject = _actorObjects.TryGetValue(actor.ID, out var existingActor);
            bool hasExistingEntity = _actorEntities.ContainsKey(actor.ID);
            if (hasExistingObject || hasExistingEntity)
            {
                if (hasExistingObject && hasExistingEntity && ReferenceEquals(existingActor, actor))
                    return;

                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' is already registered with the server actor manager.");
            }

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Server actor registration requires an initialized server physics manager.");
            }

            ServerPhysicsManager.Instance.BindActor(
                actor.ID,
                actor.SpatialContext,
                spawnPosition);
            bool physicalRegionBound = true;
            bool spatialMembershipInserted = false;
            try
            {
                ServerECSManager.ClearActorSimulationEventCancellation(actor.ID);
                _spatialIndex.Insert(actor.ID, actor.SpatialContext, spawnPosition);
                spatialMembershipInserted = true;

                var entity = _entityManager.CreateEntity();
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

                if (actor.Owner == null)
                    EnsureNPCMovementComponents(entity, actor);

                _actorObjects[actor.ID] = actor;
                _actorEntities[actor.ID] = entity;

                if (actor.Owner != null)
                {
                    var owner = actor.Owner.Value;
                    InitializePlayerValidationState(owner, actor, spawnPosition);
                }

                // Add the spellcasting component on the actor.
                _entityManager.AddComponentData(entity, new Systems.SpellCastComponent());
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, false);

                // Add cooldown buffer for per-spell cooldown tracking.
                _entityManager.AddBuffer<Systems.CooldownElement>(entity);

                // Add aura buffer for active aura tracking.
                _entityManager.AddBuffer<Systems.AuraElement>(entity);

                // Add ticker buffer for generic time-based callbacks.
                _entityManager.AddBuffer<Systems.TickerElement>(entity);

                TrackNPCForAggro(actor);

                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(SPAWN_EVENT_COUNT_METRIC, 1);
                Debug.Log("Actor spawned on server: " + actor.Name + " (ID: " + actor.ID + ")");
            }
            catch
            {
                if (spatialMembershipInserted)
                    RemoveSpatialMembership(actor.ID);

                if (physicalRegionBound
                    && ServerPhysicsManager.IsInitialized
                    && ServerPhysicsManager.Instance != null
                    && ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(actor.ID, out _))
                {
                    ServerPhysicsManager.Instance.UnbindActor(actor.ID);
                }

                throw;
            }
        }

        /// <summary>Rolls back actor and ECS state created by a rejected spawn attempt.</summary>
        /// <param name="actor">The actor whose spawn was rejected.</param>
        internal void RollbackActorSpawn(Actor actor)
        {
            if (actor == null)
                return;

            if (_actorObjects.TryGetValue(actor.ID, out var registeredActor)
                && !ReferenceEquals(registeredActor, actor))
            {
                return;
            }

            RemoveSpatialMembership(actor.ID);
            try
            {
                if (TryGetActorEntity(actor.ID, out var entity) && _entityManager.Exists(entity))
                    _entityManager.DestroyEntity(entity);
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
            if (actor == null || !_initialized)
                return;

            if (_actorObjects.TryGetValue(actor.ID, out var registeredActor)
                && !ReferenceEquals(registeredActor, actor))
                return;

            if (ServerPhysicsManager.IsInitialized
                && ServerPhysicsManager.Instance != null
                && ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(actor.ID, out _))
            {
                ServerPhysicsManager.Instance.UnbindActor(actor.ID);
            }

            RemoveSpatialMembership(actor.ID);
            _actorObjects.Remove(actor.ID);
            _actorEntities.Remove(actor.ID);
            if (actor.Owner.HasValue)
            {
                var owner = actor.Owner.Value;
                if (_playerActors.TryGetValue(owner, out var playerActor) && playerActor == actor)
                {
                    RemovePlayerValidationState(owner);
                }
            }
            _npcAggroStates.Remove(actor.ID);
            ServerECSManager.InvalidateActorSimulationEvents(actor.ID);
        }

        private void RemoveSpatialMembership(Guid actorID)
        {
            if (_spatialIndex == null || actorID == Guid.Empty)
                return;

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
            if (behavior == null || behavior.AggroRange <= 0f)
            {
                _npcAggroStates.Remove(actor.ID);
                return;
            }

            if (_npcAggroStates.ContainsKey(actor.ID))
                return;

            _npcAggroStates[actor.ID] = new NPCAggroState
            {
                NPC = actor,
                Behavior = behavior,
                Agent = actor.GetComponent<NavMeshAgent>(),
                PendingTarget = null,
                PendingAcquisitionTickID = 0
            };
        }

        private static void ClearPendingAggro(ref NPCAggroState state)
        {
            state.PendingTarget = null;
            state.PendingAcquisitionTickID = 0;
        }

        /// <summary>
        /// Updates ECS ownership and reinitializes the periodic validation baseline after an
        /// authoritative ownership transition.
        /// </summary>
        /// <param name="actor">The actor whose owner changed.</param>
        private void OnActorOwnerChanged(Actor actor)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                if (component.ActorID == actor.ID)
                {
                    var previousOwner = component.Owner;
                    component.Owner = actor.Owner ?? default;
                    _entityManager.SetComponentData(entity, component);

                    // An unowned actor must have the same movement state as a newly registered NPC
                    // before the next authoritative movement bridge runs.
                    if (component.Owner == default)
                        EnsureNPCMovementComponents(entity, actor);

                    if (previousOwner != default
                        && previousOwner != component.Owner
                        && _playerActors.TryGetValue(previousOwner, out var previousActor)
                        && previousActor == actor)
                    {
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
                    break;
                }
            }
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    OWNER_CHANGE_ENTITY_SCAN_COUNT_METRIC,
                    entities.Length,
                    Time.frameCount);
            entities.Dispose();

            TrackNPCForAggro(actor);
        }

        private void OnActorInterestChanged(Actor actor, SceneID sceneID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                if (component.ActorID == actor.ID)
                {
                    component.CurrentScene = sceneID;
                    _entityManager.SetComponentData(entity, component);

                    string sceneName = "unknown";
                    if (NetworkManager.main.sceneModule.TryGetSceneState(sceneID, out var state))
                        sceneName = state.scene.name;

                    Debug.Log("Actor interest changed on server: " + actor.Name + " (ID: " + actor.ID + ") to logical scene " + sceneName);
                    break;
                }
            }
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    INTEREST_CHANGE_ENTITY_SCAN_COUNT_METRIC,
                    entities.Length,
                    Time.frameCount);
            entities.Dispose();
        }

        /// <summary>Runs the lower-rate authoritative NPC proximity and aggro decision pass.</summary>
        private void UpdateNPCAggro(ServerSimulationTick tick)
        {
            using var markerScope = NPC_AGGRO_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int overlapSearchCount = 0;
            int colliderCandidateCount = 0;
            int allocHeavyOperationCount = telemetryEnabled ? 1 : 0;
            var keys = new List<Guid>(_npcAggroStates.Keys);

            foreach (var id in keys)
            {
                if (!_npcAggroStates.TryGetValue(id, out var state))
                    continue;

                if (state.NPC == null || state.Behavior == null)
                {
                    _npcAggroStates.Remove(id);
                    continue;
                }

                if (state.NPC.isDead)
                {
                    ClearPendingAggro(ref state);
                    _npcAggroStates[id] = state;
                    continue;
                }

                // Already locked on a target — nothing to do.
                if (state.NPC.Target != null)
                {
                    ClearPendingAggro(ref state);
                    _npcAggroStates[id] = state;
                    continue;
                }

                // Find the nearest player within aggro range via physics query.
                float aggroRange = state.Behavior.AggroRange;
                Vector3 npcPos = state.NPC.transform.position;

                Actor closest = null;
                float closestDistSqr = float.MaxValue;

                var colliders = Physics.OverlapSphere(npcPos, aggroRange);
                if (telemetryEnabled)
                {
                    overlapSearchCount++;
                    colliderCandidateCount += colliders.Length;
                    allocHeavyOperationCount++;
                }
                foreach (var col in colliders)
                {
                    var player = col.GetComponentInParent<PlayerActor>();
                    if (player == null || player.isDead) continue;

                    float distSqr = (player.transform.position - npcPos).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closest = player;
                        closestDistSqr = distSqr;
                    }
                }

                if (closest == null)
                {
                    // No player in range — reset any pending aggro.
                    ClearPendingAggro(ref state);
                    _npcAggroStates[id] = state;
                    continue;
                }

                // Player detected — start or continue the authoritative pending deadline.
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
                    Debug.Log($"NPC '{state.NPC.Name}' aggroed player '{closest.Name}'.");
                    state.NPC.SetTargetFromServer(closest);
                    ActorEndpoint.onServerStartAutoAttack?.Invoke(state.NPC);

                    if (!_actorObjects.TryGetValue(id, out var currentActor)
                        || currentActor != state.NPC)
                    {
                        _npcAggroStates.Remove(id);
                        continue;
                    }

                    ClearPendingAggro(ref state);
                }

                _npcAggroStates[id] = state;
            }
            if (telemetryEnabled)
            {
                PerformanceTelemetry.TryRecord(OVERLAP_SEARCH_COUNT_METRIC, overlapSearchCount);
                PerformanceTelemetry.TryRecord(COLLIDER_CANDIDATE_COUNT_METRIC, colliderCandidateCount);
                PerformanceTelemetry.TryRecord(ALLOC_HEAVY_OPERATION_COUNT_METRIC, allocHeavyOperationCount);
            }
        }

        private static long GetNonShorteningAggroDelayTicks(float delaySeconds)
        {
            if (float.IsNaN(delaySeconds)
                || float.IsInfinity(delaySeconds)
                || delaySeconds < 0f)
            {
                throw new InvalidOperationException(
                    "NPCBehavior.AggroDelay must be finite and non-negative.");
            }

            if (delaySeconds == 0f)
                return 0;

            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("NPC aggro timing requires initialized server simulation configuration.");

            float fixedDeltaTime = GameConfigurationManager.Config.ServerSimulationTickDuration;
            ServerSimulationClock.ValidateConfiguration(GameConfigurationManager.Config);
            double tickCountEstimate = delaySeconds / (double)fixedDeltaTime;
            if (double.IsInfinity(tickCountEstimate) || tickCountEstimate > long.MaxValue)
            {
                throw new InvalidOperationException(
                    "NPCBehavior.AggroDelay cannot be represented by an authoritative tick deadline.");
            }

            return ServerSimulationClock.GetNonShorteningTickCount(delaySeconds, fixedDeltaTime);
        }

        /// <summary>
        /// Commits the newest authenticated player proposal before every authoritative ECS tick,
        /// while applying the periodic speed check against its fixed validation baseline.
        /// </summary>
        internal void ProcessAuthoritativeTick(ServerSimulationTick tick)
        {
            if (!_initialized)
                return;

            ValidatePlayerPositions(tick);
            SynchronizeNPCPositionsToECS();
        }

        /// <summary>
        /// Projects ECS-owned NPC positions to their GameObject transforms after the authoritative ECS simulation step.
        /// </summary>
        internal void ProcessAuthoritativeTickCompleted()
        {
            if (!_initialized)
                return;

            SynchronizeNPCTransformsFromECS();
            FlushPendingTickSyncContribution();
        }

    }
}
