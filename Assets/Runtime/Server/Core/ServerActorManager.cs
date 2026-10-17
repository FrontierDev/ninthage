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
    /// Player movement is client-authoritative (via NetworkTransform owner-auth).
    /// The server periodically validates player positions for cheat detection.
    /// </summary>
    public sealed class ServerActorManager : MonoBehaviour
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
            "server_update");
        private static readonly PerformanceMetricDefinition POSITION_VALIDATION_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_POSITION_VALIDATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition PLAYER_ECS_SYNC_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_PLAYER_ECS_SYNC_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
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
            "server_update");
        private static readonly PerformanceMetricDefinition COLLIDER_CANDIDATE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_COLLIDER_CANDIDATE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager.npc_aggro",
            "server_update");
        private static readonly PerformanceMetricDefinition FULL_ENTITY_SCAN_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_FULL_ENTITY_SCAN_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_frame");
        private static readonly PerformanceMetricDefinition GAMEOBJECT_SYNC_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_ECS_GAMEOBJECT_SYNC_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_frame");
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
            "server_update");
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
        private static readonly PerformanceMarker NPC_AGGRO_MARKER = new(NPC_AGGRO_DURATION_METRIC, "Server.ActorManager.UpdateNPCAggro");
        private static readonly PerformanceMarker POSITION_VALIDATION_MARKER = new(POSITION_VALIDATION_DURATION_METRIC, "Server.ActorManager.ValidatePlayerPositions");
        private static readonly PerformanceMarker PLAYER_ECS_SYNC_MARKER = new(PLAYER_ECS_SYNC_DURATION_METRIC, "Server.ActorManager.SyncPlayerECS");
        private const int FRAME_ENTITY_SCAN_SAMPLE_CAPACITY = PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC;
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
            PerformanceTelemetry.RegisterMetric(FULL_ENTITY_SCAN_COUNT_METRIC, FRAME_ENTITY_SCAN_SAMPLE_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GAMEOBJECT_SYNC_COUNT_METRIC, FRAME_ENTITY_SCAN_SAMPLE_CAPACITY);
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

        // Authoritative movement bridges can run more than once per render frame. They are
        // aggregated into one per-frame observation so all real contributions remain represented.
        private int _pendingFrameOrTick = -1;
        private int _pendingFrameEntityScanCount;
        private int _pendingFrameGameObjectSyncCount;
        private bool _hasPendingFrameSyncContribution;

        /// <summary>Finds the server actor associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="actor">Receives the actor when found.</param>
        /// <returns><see langword="true"/> when an actor was found.</returns>
        public bool TryGetActor(Guid actorID, out Actor actor) => _actorObjects.TryGetValue(actorID, out actor);

        /// <summary>Finds the authoritative ECS entity associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="entity">Receives the authoritative ECS entity when found.</param>
        /// <returns><see langword="true"/> when the actor has a live authoritative entity.</returns>
        internal bool TryGetActorEntity(Guid actorID, out Entity entity) => _actorEntities.TryGetValue(actorID, out entity);

        // Player tracking for validation.
        private readonly Dictionary<PlayerID, Actor> _playerActors = new();
        private readonly Dictionary<PlayerID, Vector3> _lastValidatedPositions = new();
        private readonly Dictionary<PlayerID, long> _lastValidationTickIDs = new();
        private readonly Dictionary<PlayerID, long> _nextValidationTickIDs = new();
        private readonly Dictionary<PlayerID, int> _violationCounters = new();

        // NPC aggro tracking.
        private struct NPCAggroState
        {
            public Actor NPC;
            public NPCBehavior Behavior;
            public NavMeshAgent Agent;
            public Actor PendingTarget;
            public float AggroTimer;
        }
        private readonly Dictionary<Guid, NPCAggroState> _npcAggroStates = new();

        // Generous speed cap: moveSpeed(5) * sqrt(2) diagonal * 2 headroom for latency bursts.
        private const float MAX_ALLOWED_SPEED = 15f;
        // Require sustained violations before correcting to tolerate network jitter.
        private const int VIOLATION_THRESHOLD = 3;
        private const float VALIDATION_INTERVAL = 0.5f;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerActorManager is already initialized!");
                return;
            }
            _instance = this;

            ActorEndpoint.onServerActorSpawned += OnActorSpawned;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged += OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged += OnActorInterestChanged;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerActorSpawned -= OnActorSpawned;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            ActorEndpoint.onServerActorOwnerChanged -= OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged -= OnActorInterestChanged;

            _actorObjects.Clear();
            _actorEntities.Clear();
            _playerActors.Clear();
            _lastValidatedPositions.Clear();
            _lastValidationTickIDs.Clear();
            _nextValidationTickIDs.Clear();
            _violationCounters.Clear();
            _npcAggroStates.Clear();
            _pendingFrameOrTick = -1;
            _pendingFrameEntityScanCount = 0;
            _pendingFrameGameObjectSyncCount = 0;
            _hasPendingFrameSyncContribution = false;
            _instance = null;
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized) return;
            using var markerScope = UPDATE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            FlushPendingFrameSyncContributionIfFrameChanged(telemetryEnabled);
            UpdateNPCAggro();
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

        private void LateUpdate()
        {
            if (!_initialized || !PerformanceTelemetry.IsEnabled) return;
            using var markerScope = LATE_UPDATE_MARKER.Begin();
            if (_hasPendingFrameSyncContribution)
                FlushPendingFrameSyncContribution();
        }

        private void OnActorSpawned(Actor actor)
        {
            ServerECSManager.ClearActorSimulationEventCancellation(actor.ID);
            var wp = actor.WorldPos;
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new Systems.ActorComponent
            {
                ActorID = actor.ID,
                ActorName = actor.Name,
                Owner = actor.Owner ?? default,
                CurrentScene = default,
                WorldPositionD = new Unity.Mathematics.double3(wp.x, wp.y, wp.z)
            });

            if (actor.Owner == null)
                EnsureNPCMovementComponents(entity, actor);

            _actorObjects[actor.ID] = actor;
            _actorEntities[actor.ID] = entity;

            if (actor.Owner != null)
            {
                var owner = actor.Owner.Value;
                InitializePlayerValidationState(owner, actor);
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

            // Track NPCs with aggro behavior for proximity-based targeting.
            if (actor.Owner == null && actor.isDead == false)
            {
                var behavior = actor.GetComponent<NPCBehavior>();
                if (behavior != null)
                {
                    if (behavior.AggroRange > 0)
                    {
                        _npcAggroStates[actor.ID] = new NPCAggroState
                        {
                            NPC = actor,
                            Behavior = behavior,
                            Agent = actor.GetComponent<NavMeshAgent>(),
                            PendingTarget = null,
                            AggroTimer = 0f
                        };
                    }
                }
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(SPAWN_EVENT_COUNT_METRIC, 1);
            Debug.Log("Actor spawned on server: " + actor.Name + " (ID: " + actor.ID + ")");
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            _actorObjects.Remove(actor.ID);
            _actorEntities.Remove(actor.ID);
            if (actor.Owner.HasValue)
            {
                var owner = actor.Owner.Value;
                if (_playerActors.TryGetValue(owner, out var playerActor) && playerActor == actor)
                {
                    _playerActors.Remove(owner);
                    _lastValidatedPositions.Remove(owner);
                    _lastValidationTickIDs.Remove(owner);
                    _nextValidationTickIDs.Remove(owner);
                    _violationCounters.Remove(owner);
                }
            }
            _npcAggroStates.Remove(actor.ID);
            ServerECSManager.InvalidateActorSimulationEvents(actor.ID);
        }

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
                        _playerActors.Remove(previousOwner);
                        _lastValidatedPositions.Remove(previousOwner);
                        _lastValidationTickIDs.Remove(previousOwner);
                        _nextValidationTickIDs.Remove(previousOwner);
                        _violationCounters.Remove(previousOwner);
                    }

                    if (actor.Owner != null && component.Owner != default)
                        InitializePlayerValidationState(component.Owner, actor);

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
        }

        private void EnsureNPCMovementComponents(Entity entity, Actor actor)
        {
            // Components remain attached when ownership returns to a player; the owner checks in
            // ActorUpdateSystem and both movement bridges keep player movement out of this path.
            var agent = actor.GetComponent<NavMeshAgent>();
            bool hasEnabledAgent = agent != null && agent.enabled;

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

        /// <summary>
        /// Scans for nearby players and starts aggro timers on NPCs.
        /// Once the delay expires the NPC acquires a target and begins auto-attacking.
        /// </summary>
        private void UpdateNPCAggro()
        {
            using var markerScope = NPC_AGGRO_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int overlapSearchCount = 0;
            int colliderCandidateCount = 0;
            int allocHeavyOperationCount = telemetryEnabled ? 1 : 0;
            float dt = Time.deltaTime;
            var keys = new List<Guid>(_npcAggroStates.Keys);

            foreach (var id in keys)
            {
                var state = _npcAggroStates[id];
                if (state.NPC == null || state.NPC.isDead) continue;

                // Already locked on a target — nothing to do.
                if (state.NPC.Target != null) continue;

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
                    if (state.PendingTarget != null)
                    {
                        state.PendingTarget = null;
                        state.AggroTimer = 0f;
                        _npcAggroStates[id] = state;
                    }
                    continue;
                }

                // Player detected — start or continue aggro timer.
                if (state.PendingTarget != closest)
                {
                    state.PendingTarget = closest;
                    state.AggroTimer = 0f;
                }

                state.AggroTimer += dt;

                if (state.AggroTimer >= state.Behavior.AggroDelay)
                {
                    Debug.Log($"NPC '{state.NPC.Name}' aggroed player '{closest.Name}'.");
                    state.NPC.SetTargetFromServer(closest);
                    ActorEndpoint.onServerStartAutoAttack?.Invoke(state.NPC);
                    state.PendingTarget = null;
                    state.AggroTimer = 0f;
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

        /// <summary>
        /// Samples and validates player movement for one authoritative simulation tick.
        /// </summary>
        internal void ProcessAuthoritativeTick(ServerSimulationTick tick)
        {
            if (!_initialized)
                return;

            ValidatePlayerPositions(tick);
            SyncPlayerECS();
            SynchronizeNPCPositionsToECS();
        }

        /// <summary>
        /// Samples enabled NavMesh-driven NPC transforms before the ECS simulation step.
        /// The resolved movement owner is stored in MotionStateComponent so ActorUpdateSystem
        /// either owns gravity for this tick or leaves movement to the NavMeshAgent.
        /// </summary>
        internal void ProcessAuthoritativeTickCompleted()
        {
            if (!_initialized)
                return;

            SynchronizeNPCTransformsFromECS();
        }

        private void SynchronizeNPCPositionsToECS()
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            int fullScanCount = entities.Length;
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                if (component.Owner != default)
                    continue;

                if (!_actorObjects.TryGetValue(component.ActorID, out var actorObj) || actorObj == null)
                    continue;

                var motion = _entityManager.GetComponentData<Systems.MotionStateComponent>(entity);
                var agent = actorObj.GetComponent<NavMeshAgent>();
                bool hasEnabledAgent = agent != null && agent.enabled;
                if (motion.UsesNavMeshAgent != hasEnabledAgent)
                {
                    motion.UsesNavMeshAgent = hasEnabledAgent;
                    _entityManager.SetComponentData(entity, motion);
                }

                if (!hasEnabledAgent)
                    continue;

                var pos = actorObj.transform.position;
                component.WorldPositionD = new Unity.Mathematics.double3(pos.x, pos.y, pos.z);
                _entityManager.SetComponentData(entity, component);
                if (telemetryEnabled)
                    syncCount++;
            }
            entities.Dispose();
            if (telemetryEnabled)
                RecordFrameSyncContribution(fullScanCount, syncCount, Time.frameCount);
        }

        private void SynchronizeNPCTransformsFromECS()
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            int fullScanCount = entities.Length;
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                if (component.Owner != default)
                    continue;

                if (!_actorObjects.TryGetValue(component.ActorID, out var actorObj) || actorObj == null)
                    continue;

                // The pre-tick bridge resolved ownership for this tick. A NavMesh agent remains
                // the sole transform writer; ECS-owned NPCs receive the completed simulation state.
                if (_entityManager.GetComponentData<Systems.MotionStateComponent>(entity).UsesNavMeshAgent)
                    continue;

                actorObj.transform.position = new Vector3(
                    (float)component.WorldPositionD.x,
                    (float)component.WorldPositionD.y,
                    (float)component.WorldPositionD.z
                );
                if (telemetryEnabled)
                    syncCount++;
            }
            entities.Dispose();
            if (telemetryEnabled)
                RecordFrameSyncContribution(fullScanCount, syncCount, Time.frameCount);
        }

        /// <summary>
        /// Periodically checks that player positions are legal.
        /// NetworkTransform (owner-auth) pushes the client position to
        /// the server actor's transform automatically; we just read it.
        /// </summary>
        private void ValidatePlayerPositions(ServerSimulationTick tick)
        {
            using var markerScope = POSITION_VALIDATION_MARKER.Begin();
            foreach (var kvp in _playerActors)
            {
                var playerID = kvp.Key;
                var actor = kvp.Value;
                if (actor == null) continue;

                if (!_nextValidationTickIDs.TryGetValue(playerID, out var nextValidationTickID)) continue;
                if (!_lastValidationTickIDs.TryGetValue(playerID, out var lastValidationTickID)) continue;
                if (!_lastValidatedPositions.TryGetValue(playerID, out var lastValidatedPosition)) continue;

                var proposedPosition = actor.transform.position;
                if (tick.TickID < nextValidationTickID) continue;

                long validationIntervalTicks = checked(nextValidationTickID - lastValidationTickID);
                long elapsedTickCount = checked(tick.TickID - lastValidationTickID);
                if (validationIntervalTicks <= 0 || elapsedTickCount <= 0)
                    throw new InvalidOperationException("Player position validation deadline state is invalid.");

                float elapsed = (float)(elapsedTickCount * (double)tick.DeltaTime);

                // Speed check: horizontal distance / elapsed time.
                var horizontalDelta = new Vector3(
                    proposedPosition.x - lastValidatedPosition.x,
                    0f,
                    proposedPosition.z - lastValidatedPosition.z);
                float speed = horizontalDelta.magnitude / elapsed;

                // The existing three-strike policy provisionally admits proposals between validation
                // deadlines to tolerate network jitter. At the threshold, the proposal is rejected
                // by restoring both the accepted position and the server-side transform before ECS sync.
                var acceptedPosition = proposedPosition;

                if (speed > MAX_ALLOWED_SPEED)
                {
                    _violationCounters[playerID] = _violationCounters.GetValueOrDefault(playerID) + 1;

                    if (_violationCounters[playerID] >= VIOLATION_THRESHOLD)
                    {
                        Debug.Warning("Player " + playerID + " exceeded max speed (" + speed.ToString("F1") + " m/s) " + _violationCounters[playerID] + " times. Correcting.");
                        acceptedPosition = lastValidatedPosition;
                        var worldPos = new WorldPosition(acceptedPosition.x, acceptedPosition.y, acceptedPosition.z);
                        var (chunk, localPos) = ServerPositionManager.Instance.ToChunkLocal(worldPos);
                        MovementEndpoint.Client_ServerCorrection(playerID, chunk, localPos);

                        // Keep the server-side bridge on the accepted position until the owner receives the snap.
                        actor.transform.position = acceptedPosition;
                        _violationCounters[playerID] = 0;
                    }
                }
                else
                {
                    _violationCounters[playerID] = 0;
                }

                _lastValidatedPositions[playerID] = acceptedPosition;
                _lastValidationTickIDs[playerID] = tick.TickID;
                _nextValidationTickIDs[playerID] = checked(tick.TickID + validationIntervalTicks);
            }
        }

        /// <summary>
        /// Pushes the accepted or provisionally admitted client-authoritative transform position
        /// (received via NetworkTransform) into ECS so game systems have up-to-date data.
        /// </summary>
        private void SyncPlayerECS()
        {
            using var markerScope = PLAYER_ECS_SYNC_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            int fullScanCount = entities.Length;
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                if (component.Owner == default) continue;

                if (_playerActors.TryGetValue(component.Owner, out var actor) && actor != null)
                {
                    var pos = actor.transform.position;
                    component.WorldPositionD = new Unity.Mathematics.double3(pos.x, pos.y, pos.z);
                    _entityManager.SetComponentData(entity, component);
                    if (telemetryEnabled)
                        syncCount++;
                }
            }
            entities.Dispose();
            if (telemetryEnabled)
            {
                RecordFrameSyncContribution(fullScanCount, syncCount, Time.frameCount);
            }
        }

        private void InitializePlayerValidationState(PlayerID owner, Actor actor)
        {
            long currentTickID = ServerSimulationClock.CurrentTickID;
            long validationIntervalTicks = GetValidationIntervalTicks();
            _playerActors[owner] = actor;
            _lastValidatedPositions[owner] = actor.transform.position;
            _lastValidationTickIDs[owner] = currentTickID;
            _nextValidationTickIDs[owner] = checked(currentTickID + validationIntervalTicks);
            _violationCounters[owner] = 0;
        }

        private static long GetValidationIntervalTicks()
        {
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("Player position validation requires initialized server simulation configuration.");

            ServerSimulationClock.ValidateConfiguration(GameConfigurationManager.Config);
            float fixedDeltaTime = GameConfigurationManager.Config.ServerSimulationTickDuration;
            double tickCountEstimate = VALIDATION_INTERVAL / (double)fixedDeltaTime;
            if (double.IsInfinity(tickCountEstimate) || tickCountEstimate > long.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(VALIDATION_INTERVAL),
                    "The player position validation interval cannot be represented by an authoritative tick deadline.");

            return ServerSimulationClock.GetNonShorteningTickCount(VALIDATION_INTERVAL, fixedDeltaTime);
        }

        private void RecordFrameSyncContribution(int fullScanCount, int gameObjectSyncCount, int frameOrTick)
        {
            if (_hasPendingFrameSyncContribution && _pendingFrameOrTick != frameOrTick)
                FlushPendingFrameSyncContribution();

            if (!_hasPendingFrameSyncContribution)
            {
                _pendingFrameOrTick = frameOrTick;
                _pendingFrameEntityScanCount = 0;
                _pendingFrameGameObjectSyncCount = 0;
                _hasPendingFrameSyncContribution = true;
            }

            _pendingFrameEntityScanCount += fullScanCount;
            _pendingFrameGameObjectSyncCount += gameObjectSyncCount;
        }

        private void FlushPendingFrameSyncContributionIfFrameChanged(bool telemetryEnabled)
        {
            if (telemetryEnabled
                && _hasPendingFrameSyncContribution
                && _pendingFrameOrTick != Time.frameCount)
            {
                FlushPendingFrameSyncContribution();
            }
        }

        private void FlushPendingFrameSyncContribution()
        {
            PerformanceTelemetry.TryRecord(
                FULL_ENTITY_SCAN_COUNT_METRIC,
                _pendingFrameEntityScanCount,
                _pendingFrameOrTick);
            PerformanceTelemetry.TryRecord(
                GAMEOBJECT_SYNC_COUNT_METRIC,
                _pendingFrameGameObjectSyncCount,
                _pendingFrameOrTick);
            _pendingFrameOrTick = -1;
            _pendingFrameEntityScanCount = 0;
            _pendingFrameGameObjectSyncCount = 0;
            _hasPendingFrameSyncContribution = false;
        }
    }
}
