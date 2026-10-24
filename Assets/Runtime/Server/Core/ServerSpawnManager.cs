using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    /// <summary>Coordinates authoritative actor spawning and scene placement.</summary>
    public sealed class ServerSpawnManager : MonoBehaviour
    {
        private static readonly PerformanceMetricDefinition UPDATE_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition SPAWN_COMMAND_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_COMMAND_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition SPAWN_SUCCESS_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_SUCCESS_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMetricDefinition SPAWN_FAILURE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_FAILURE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMetricDefinition SCENE_LOAD_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_SCENE_LOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "scene_operation");
        private static readonly PerformanceMetricDefinition DESPAWN_EVENT_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_DESPAWN_EVENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMarker UPDATE_MARKER = new(UPDATE_DURATION_METRIC, "Server.SpawnManager.Update");
        private static readonly PerformanceMarker SCENE_LOAD_MARKER = new(SCENE_LOAD_DURATION_METRIC, "Server.SpawnManager.OnPostSceneLoaded");
        private static ServerSpawnManager _instance;
        /// <summary>Gets the active server spawn manager.</summary>
        public static ServerSpawnManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the spawn manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(UPDATE_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_COMMAND_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_SUCCESS_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_FAILURE_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SCENE_LOAD_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(DESPAWN_EVENT_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private NetworkManager _networkManager;

        // Managed prefab list indexed by SpawnPointComponent.PrefabIndex
        private readonly List<GameObject> _prefabRegistry = new();
        // SceneIndex → Scene for PurrNet Instantiate
        private readonly Dictionary<int, Scene> _sceneRegistry = new();
        private int _nextSceneIndex;

        private readonly struct SpawnPointRuntimeKey : IEquatable<SpawnPointRuntimeKey>
        {
            public SpawnPointRuntimeKey(ActorSpatialContext context, Guid spawnPointGUID)
            {
                if (!context.IsValid)
                    throw new ArgumentException(
                        "A valid actor spatial context is required.",
                        nameof(context));
                if (spawnPointGUID == Guid.Empty)
                    throw new ArgumentException(
                        "A nonempty spawn-point GUID is required.",
                        nameof(spawnPointGUID));

                Context = context;
                SpawnPointGUID = spawnPointGUID;
            }

            public ActorSpatialContext Context { get; }
            public Guid SpawnPointGUID { get; }

            public bool Equals(SpawnPointRuntimeKey other)
            {
                return Context.Equals(other.Context)
                    && SpawnPointGUID == other.SpawnPointGUID;
            }

            public override bool Equals(object obj)
            {
                return obj is SpawnPointRuntimeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Context.GetHashCode() * 397) ^ SpawnPointGUID.GetHashCode();
                }
            }
        }

        private readonly Dictionary<SpawnPointRuntimeKey, Actor> _spawnPointActorMap = new();
        private readonly Dictionary<SceneID, List<Entity>> _spawnPointEntitiesByScene = new();

        private enum InstancePopulationReadiness
        {
            Preparing,
            Ready,
            Failed
        }

        private sealed class InstancePopulationPreparation
        {
            public InstancePopulationReadiness Readiness { get; set; }
            public HashSet<Vector2Int> RequiredChunks { get; }
            public HashSet<Vector2Int> DiscoveredChunks { get; } = new();
            public HashSet<SpawnPointRuntimeKey> PendingSpawnPoints { get; } = new();
            public List<Action<bool>> CompletionCallbacks { get; } = new();

            public InstancePopulationPreparation(HashSet<Vector2Int> requiredChunks)
            {
                Readiness = InstancePopulationReadiness.Preparing;
                RequiredChunks = requiredChunks;
            }
        }

        private readonly Dictionary<ActorSpatialContext, InstancePopulationPreparation>
            _instancePopulationPreparations = new();

        private EntityManager _entityManager;
        private EntityQuery _spawnCommandQuery;

        /// <summary>Initializes server spawn manager state and subscriptions.</summary>
        public void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerSpawnManager is already initialized!");
                return;
            }

            _instance = this;

            _networkManager = NetworkManager.main;
            _networkManager.sceneModule.onPostSceneLoaded += OnPostSceneLoaded;
            _networkManager.sceneModule.onSceneUnloaded += OnSceneUnloaded;

            Game.Shared.Networking.ActorEndpoint.onServerActorDespawned += OnServerActorDespawned;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _spawnCommandQuery = _entityManager.CreateEntityQuery(typeof(Systems.SpawnCommand));

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _networkManager.sceneModule != null)
            {
                _networkManager.sceneModule.onPostSceneLoaded -= OnPostSceneLoaded;
                _networkManager.sceneModule.onSceneUnloaded -= OnSceneUnloaded;
            }

            Game.Shared.Networking.ActorEndpoint.onServerActorDespawned -= OnServerActorDespawned;

            _instance = null;
            _initialized = false;
            _instancePopulationPreparations.Clear();
        }

        internal void EnsureInstancePopulationReady(
            ActorSpatialContext context,
            IReadOnlyCollection<Vector2Int> requiredChunks,
            Action<bool> onCompleted)
        {
            if (!_initialized || !ReferenceEquals(_instance, this))
                throw new InvalidOperationException(
                    "Instance population readiness requires the current initialized server spawn manager.");
            if (!context.IsValid)
                throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
            if (context.InstanceID == Guid.Empty)
                throw new ArgumentException("A private instance context is required.", nameof(context));
            if (requiredChunks == null || requiredChunks.Count == 0)
                throw new ArgumentException("At least one required chunk is needed.", nameof(requiredChunks));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                throw new InvalidOperationException(
                    "Instance population readiness requires an initialized server world manager.");

            ActorSpatialContext runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            if (!runtimeContext.IsValid
                || context.WorldID != runtimeContext.WorldID
                || !StringComparer.Ordinal.Equals(context.MapID, runtimeContext.MapID))
            {
                throw new InvalidOperationException(
                    "Instance population context does not belong to the current server runtime.");
            }

            var requestedChunks = new HashSet<Vector2Int>(requiredChunks);
            if (requestedChunks.Count == 0)
                throw new ArgumentException("At least one required chunk is needed.", nameof(requiredChunks));

            if (!_instancePopulationPreparations.TryGetValue(context, out var preparation))
            {
                preparation = new InstancePopulationPreparation(requestedChunks);
                preparation.CompletionCallbacks.Add(onCompleted);
                _instancePopulationPreparations.Add(context, preparation);
                return;
            }

            switch (preparation.Readiness)
            {
                case InstancePopulationReadiness.Preparing:
                    if (!preparation.RequiredChunks.SetEquals(requestedChunks))
                    {
                        throw new InvalidOperationException(
                            $"Required chunks changed while instance population was preparing for {context}.");
                    }
                    preparation.CompletionCallbacks.Add(onCompleted);
                    return;
                case InstancePopulationReadiness.Ready:
                    onCompleted(true);
                    return;
                case InstancePopulationReadiness.Failed:
                    onCompleted(false);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Unknown population readiness state for {context}.");
            }
        }

        internal void FailInstancePopulationPreparation(ActorSpatialContext context, string reason)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A failure reason is required.", nameof(reason));
            if (!_instancePopulationPreparations.TryGetValue(context, out var preparation)
                || preparation.Readiness != InstancePopulationReadiness.Preparing)
            {
                return;
            }

            preparation.Readiness = InstancePopulationReadiness.Failed;
            Debug.Error(
                $"[InstanceReadiness] Failed world={context.WorldID} map={context.MapID} "
                + $"instance={context.InstanceID}: {reason}");
            CompletePopulationCallbacks(preparation, false);
        }

        private void RecordSpawnPointDiscovery(WorldChunkKey sourceChunk, Guid spawnPointGUID)
        {
            if (!_instancePopulationPreparations.TryGetValue(sourceChunk.Context, out var preparation)
                || preparation.Readiness != InstancePopulationReadiness.Preparing
                || !preparation.RequiredChunks.Contains(sourceChunk.ChunkCoord))
            {
                return;
            }

            if (spawnPointGUID == Guid.Empty)
                throw new InvalidOperationException("A discovered authored spawn point has no GUID.");
            preparation.PendingSpawnPoints.Add(
                new SpawnPointRuntimeKey(sourceChunk.Context, spawnPointGUID));
        }

        private void CompletePopulationCallbacks(
            InstancePopulationPreparation preparation,
            bool succeeded)
        {
            Action<bool>[] callbacks = preparation.CompletionCallbacks.ToArray();
            preparation.CompletionCallbacks.Clear();
            foreach (Action<bool> callback in callbacks)
            {
                try
                {
                    callback(succeeded);
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"[InstanceReadiness] Completion callback failed: {exception.Message}\n{exception.StackTrace}");
                }
            }
        }

        private void CompleteSpawnPointPreparation(
            ActorSpatialContext context,
            SpawnPointRuntimeKey spawnPointKey,
            bool succeeded,
            string failureReason)
        {
            if (!_instancePopulationPreparations.TryGetValue(context, out var preparation)
                || preparation.Readiness != InstancePopulationReadiness.Preparing
                || !preparation.PendingSpawnPoints.Contains(spawnPointKey))
            {
                return;
            }

            if (!succeeded)
            {
                FailInstancePopulationPreparation(context, failureReason);
                return;
            }

            preparation.PendingSpawnPoints.Remove(spawnPointKey);
            TryMarkInstancePopulationReady(context, preparation);
        }

        private void TryMarkInstancePopulationReady(
            ActorSpatialContext context,
            InstancePopulationPreparation preparation)
        {
            if (preparation.Readiness != InstancePopulationReadiness.Preparing
                || !preparation.DiscoveredChunks.SetEquals(preparation.RequiredChunks)
                || preparation.PendingSpawnPoints.Count != 0)
            {
                return;
            }

            preparation.Readiness = InstancePopulationReadiness.Ready;
            Debug.Log(
                $"[InstanceReadiness] Ready world={context.WorldID} map={context.MapID} "
                + $"instance={context.InstanceID}");
            CompletePopulationCallbacks(preparation, true);
        }

        private void OnPostSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!asServer) return;
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;
            using var markerScope = SCENE_LOAD_MARKER.Begin();

            var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
            if (sceneID == globalSceneID)
                return;

            if (ServerWorldManager.Instance.IsTransferInstanceScene(sceneID))
            {
                ActorSpawnPoint[] transferSpawnPoints = state.scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<ActorSpawnPoint>(true))
                    .ToArray();
                if (transferSpawnPoints.Length > 0)
                {
                    ServerWorldManager.Instance.FailTransferInstanceScene(
                        sceneID,
                        $"scene contains {transferSpawnPoints.Length} unsupported ActorSpawnPoint component(s)");
                    throw new InvalidOperationException(
                        $"Transfer-instance scene '{state.scene.name}' contains ActorSpawnPoint content, "
                        + "which is not supported for transfer scenes.");
                }

                ServerWorldManager.Instance.ConfirmTransferInstanceSceneReady(sceneID);
                return;
            }

            WorldChunkKey sourceChunk = ResolveLoadedChunkKey(sceneID);
            if (!sourceChunk.Context.IsValid)
                throw new InvalidOperationException(
                    $"Loaded scene {sceneID} resolved to an invalid source context.");

            if (!_networkManager.sceneModule.TryGetSceneState(globalSceneID, out var globalState))
            {
                ServerSpawnManager.Instance.FailInstancePopulationPreparation(
                    sourceChunk.Context,
                    "the global actor scene was unavailable during environment spawn-point registration");
                return;
            }
            int sceneIndex = RegisterScene(globalState.scene);

            _instancePopulationPreparations.TryGetValue(sourceChunk.Context, out var preparation);
            bool tracksRequiredChunk = preparation != null
                && preparation.Readiness == InstancePopulationReadiness.Preparing
                && preparation.RequiredChunks.Contains(sourceChunk.ChunkCoord);

            var createdSpawnPointEntities = new List<Entity>();
            var addedPlaceholderReservations = new List<SpawnPointRuntimeKey>();
            ActorSpawnPoint[] actorSpawnPoints = Array.Empty<ActorSpawnPoint>();
            bool sceneRegistrationAdded = false;
            try
            {
                if (_spawnPointEntitiesByScene.ContainsKey(sceneID))
                {
                    throw new InvalidOperationException(
                        $"Spawn points for scene {sceneID} have already been registered.");
                }

                // Find spawn points ONLY in the specific scene that was loaded.
                actorSpawnPoints = state.scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<ActorSpawnPoint>())
                    .ToArray();

                Debug.Log($"Found {actorSpawnPoints.Length} spawn points in '{state.scene.name}'.");
                if (tracksRequiredChunk)
                    preparation.DiscoveredChunks.Add(sourceChunk.ChunkCoord);

                if (actorSpawnPoints.Length > 0)
                {
                    FixedString128Bytes sourceMapID = ToSpawnContextMapID(sourceChunk.Context);
                    if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                    {
                        throw new InvalidOperationException(
                            "Server spawn-point registration requires an initialized server physics manager.");
                    }
                    foreach (ActorSpawnPoint spawnPoint in actorSpawnPoints)
                    {
                        Guid spawnPointGUID = spawnPoint.GUID;
                        var spawnPointKey = new SpawnPointRuntimeKey(
                            sourceChunk.Context,
                            spawnPointGUID);
                        if (_spawnPointActorMap.TryGetValue(spawnPointKey, out var reservedActor)
                            && reservedActor != null)
                        {
                            Debug.Warning(
                                $"Spawn point '{spawnPoint.name}' with GUID {spawnPointGUID} already has an actor spawned in context {sourceChunk.Context}. Skipping duplicate.");
                            continue;
                        }
                        if (_spawnPointActorMap.ContainsKey(spawnPointKey))
                            _spawnPointActorMap.Remove(spawnPointKey);

                        int prefabIndex = RegisterPrefab(spawnPoint.Actor.gameObject);
                        WorldPosition spawnWorldPosition = ServerPhysicsManager.Instance.ToWorldPosition(
                            spawnPoint.transform.position);

                        var entity = _entityManager.CreateEntity();
                        createdSpawnPointEntities.Add(entity);
                        _entityManager.AddComponentData(entity, new Systems.SpawnPointComponent
                        {
                            SpawnPointGUID = spawnPointGUID,
                            WorldID = sourceChunk.Context.WorldID,
                            MapID = sourceMapID,
                            InstanceID = sourceChunk.Context.InstanceID,
                            SceneName = state.scene.name,
                            WorldPositionD = new double3(
                                spawnWorldPosition.x,
                                spawnWorldPosition.y,
                                spawnWorldPosition.z),
                            ChunkCoord = new int2(sourceChunk.ChunkCoord.x, sourceChunk.ChunkCoord.y),
                            PrefabIndex = prefabIndex,
                            SceneIndex = sceneIndex,
                            Pending = true
                        });

                        // Tell the spawn system that this spawn point has an actor associated with it.
                        _spawnPointActorMap[spawnPointKey] = spawnPoint.Actor;
                        addedPlaceholderReservations.Add(spawnPointKey);
                        RecordSpawnPointDiscovery(sourceChunk, spawnPointGUID);
                    }
                }

                _spawnPointEntitiesByScene.Add(sceneID, createdSpawnPointEntities);
                sceneRegistrationAdded = true;

                if (tracksRequiredChunk)
                    TryMarkInstancePopulationReady(sourceChunk.Context, preparation);
            }
            catch (Exception exception)
            {
                var rollbackFailures = new List<Exception>();
                foreach (Entity entity in createdSpawnPointEntities)
                {
                    try
                    {
                        if (_entityManager.Exists(entity))
                            _entityManager.DestroyEntity(entity);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                foreach (SpawnPointRuntimeKey spawnPointKey in addedPlaceholderReservations)
                {
                    try
                    {
                        if (!_spawnPointActorMap.TryGetValue(spawnPointKey, out Actor currentReservation))
                            continue;

                        ActorSpawnPoint authoredSpawnPoint = actorSpawnPoints.FirstOrDefault(
                            spawnPoint => spawnPoint != null
                                && spawnPoint.GUID == spawnPointKey.SpawnPointGUID);
                        Actor authoredPlaceholder = authoredSpawnPoint == null
                            ? null
                            : authoredSpawnPoint.Actor;
                        if (!ReferenceEquals(currentReservation, authoredPlaceholder))
                        {
                            throw new InvalidOperationException(
                                $"Spawn-point reservation for '{spawnPointKey.SpawnPointGUID}' changed during scene-registration rollback; refusing to remove the replacement.");
                        }

                        _spawnPointActorMap.Remove(spawnPointKey);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                if (sceneRegistrationAdded)
                    _spawnPointEntitiesByScene.Remove(sceneID);

                if (tracksRequiredChunk)
                {
                    try
                    {
                        FailInstancePopulationPreparation(
                            sourceChunk.Context,
                            $"spawn-point discovery failed for chunk {sourceChunk.ChunkCoord}: {exception.Message}");
                    }
                    catch (Exception readinessException)
                    {
                        rollbackFailures.Add(readinessException);
                    }
                }

                if (rollbackFailures.Count > 0)
                {
                    var failures = new List<Exception>(rollbackFailures.Count + 1) { exception };
                    failures.AddRange(rollbackFailures);
                    string rollbackFailureMessages = string.Join(
                        "; ",
                        rollbackFailures.Select(failure => failure.Message));
                    throw new InvalidOperationException(
                        $"Spawn-point discovery for scene {sceneID} failed: {exception.Message}. "
                        + $"Rollback or readiness failure: {rollbackFailureMessages}",
                        new AggregateException(failures));
                }

                throw;
            }
        }

        private void OnSceneUnloaded(SceneID sceneID, bool asServer)
        {
            if (!asServer)
                return;
            if (!_spawnPointEntitiesByScene.ContainsKey(sceneID))
                return;

            DestroySpawnPointEntities(sceneID);
        }

        private void DestroySpawnPointEntities(SceneID sceneID)
        {
            if (!_spawnPointEntitiesByScene.TryGetValue(sceneID, out List<Entity> entities))
                return;

            foreach (Entity entity in entities)
            {
                if (!_entityManager.Exists(entity))
                    continue;
                if (!_entityManager.HasComponent<Systems.SpawnPointComponent>(entity))
                {
                    throw new InvalidOperationException(
                        $"Spawn-point entity ownership for scene {sceneID} references entity {entity} without SpawnPointComponent.");
                }

                _entityManager.DestroyEntity(entity);
            }

            _spawnPointEntitiesByScene.Remove(sceneID);
        }

        private WorldChunkKey ResolveLoadedChunkKey(SceneID sceneID)
        {
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Resolving a loaded source chunk requires an initialized server world manager.");
            }

            WorldChunkKey resolvedChunk = default;
            bool found = false;
            foreach (var loadedChunk in ServerWorldManager.Instance.LoadedChunks)
            {
                if (loadedChunk.Value != sceneID)
                    continue;
                if (found)
                {
                    throw new InvalidOperationException(
                        $"SceneID {sceneID} is mapped to multiple loaded world chunks.");
                }

                resolvedChunk = loadedChunk.Key;
                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"SceneID {sceneID} is not mapped to a loaded world chunk.");
            }

            return resolvedChunk;
        }

        private static FixedString128Bytes ToSpawnContextMapID(ActorSpatialContext context)
        {
            if (!context.IsValid)
                throw new ArgumentException(
                    "A valid actor spatial context is required.",
                    nameof(context));

            string mapID = context.MapID;
            try
            {
                var fixedMapID = new FixedString128Bytes(mapID);
                if (!StringComparer.Ordinal.Equals(fixedMapID.ToString(), mapID))
                {
                    throw new InvalidOperationException(
                        $"Map ID '{mapID}' cannot be represented losslessly in FixedString128Bytes.");
                }

                return fixedMapID;
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Map ID '{mapID}' cannot be represented losslessly in the spawn command.",
                    exception);
            }
        }

        private void OnServerActorDespawned(Actor actor)
        {
            // If this actor was spawned from a spawn point, mark that exact reservation as pending again.
            if (actor != null)
            {
                bool foundReservation = false;
                SpawnPointRuntimeKey reservationKey = default;
                foreach (var reservation in _spawnPointActorMap)
                {
                    if (!ReferenceEquals(reservation.Value, actor))
                        continue;
                    if (foundReservation)
                    {
                        throw new InvalidOperationException(
                            $"Spawn-point actor '{actor.ID}' owns multiple runtime reservations.");
                    }

                    foundReservation = true;
                    reservationKey = reservation.Key;
                }

                if (foundReservation)
                    _spawnPointActorMap.Remove(reservationKey);
            }
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(DESPAWN_EVENT_COUNT_METRIC, 1);

            if (actor == null)
                return;

            NetworkIdentity identity = actor.GetComponent<NetworkIdentity>();
            if (identity != null
                && actor.SpatialContext.IsValid
                && !string.IsNullOrWhiteSpace(actor.CurrentChunk))
            {
                Game.Shared.Networking.ChunkSubscriptions.UnregisterIdentity(
                    actor.SpatialContext,
                    actor.CurrentChunk,
                    identity);
            }

            if (actor.Owner != null
                && Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                    actor.Owner.Value,
                    out var registeredActor)
                && registeredActor == actor)
            {
                Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(actor.Owner.Value);
            }
        }

        /// <summary>Starts spawning the actor associated with a connected player.</summary>
        /// <param name="playerID">The player to spawn.</param>
        public void SpawnPlayer(PlayerID playerID)
        {
            StartCoroutine(SpawnPlayerCoroutine(playerID));
        }

        private IEnumerator SpawnPlayerCoroutine(PlayerID playerID)
        {
            var spawnScene = new Vector2Int(0, 0);
            ServerWorldManager worldManager = ServerWorldManager.Instance;
            ActorSpatialContext runtimeContext = worldManager.CurrentRuntimeContext;
            worldManager.RetainChunk(runtimeContext, spawnScene);

            try
            {
                // Actively request the spawn chunk to load instead of passively waiting.
                bool chunkLoadCompleted = false;
                bool chunkLoadSucceeded = false;
                try
                {
                    worldManager.EnsureChunkLoaded(
                        runtimeContext,
                        spawnScene,
                        (succeeded, _) =>
                        {
                            chunkLoadSucceeded = succeeded;
                            chunkLoadCompleted = true;
                        });
                }
                catch (Exception exception)
                {
                    Debug.Error($"Failed to start loading the player spawn chunk: {exception.Message}");
                    yield break;
                }

                yield return new WaitUntil(() => chunkLoadCompleted);
                if (!chunkLoadSucceeded)
                {
                    Debug.Error($"Failed to load the player spawn chunk for player {playerID}.");
                    yield break;
                }

                // Load the player prefab from Addressables
                var handle = Addressables.LoadAssetAsync<GameObject>("PlayerPrefab"); // your addressable key
                yield return handle;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error("Failed to load player prefab addressable.");
                    yield break;
                }

                var sourceChunk = new WorldChunkKey(runtimeContext, spawnScene);
                var spawnWorldPosition = WorldPosition.FromChunkLocal(
                    spawnScene,
                    new Vector3(0f, 5f, 0f));
                GameObject prefab = handle.Result;

                // Spawn the actor into the persistent global_actors scene, not the chunk scene.
                var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
                SpawnAt(
                    sourceChunk,
                    spawnWorldPosition,
                    prefab,
                    globalSceneID,
                    playerID);

                // Wait for the actor to be spawned and linked to the session (happens on next Update tick).
                var session = ServerConnectionManager.Instance.GetSessionData(playerID);
                if (session == null) yield break;
                yield return new WaitUntil(() => session.PlayerActor != null);

                // Look up the character data from the account and assign it to the PlayerActor.
                var accountData = ServerConnectionManager.Instance.GetAccountData(playerID);
                var characterData = accountData?.Characters.Find(c => c.GUID == session.CharacterID);
                if (characterData == null)
                {
                    Debug.Error($"Character data not found for player {playerID}, character GUID {session.CharacterID}.");
                    yield break;
                }

                var playerActor = session.PlayerActor as Game.Shared.Runtime.Actors.PlayerActor;
                if (playerActor == null)
                {
                    Debug.Error($"PlayerPrefab is missing a PlayerActor component. " +
                                $"Ensure the prefab has PlayerActor (not just Actor).");
                    yield break;
                }

                // Check that the player has all known spells for their class.
                var classDef = Shared.Data.ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID);
                if (classDef == null)
                {
                    Debug.Error($"Class definition '{characterData.ClassID}' not found for player {playerID}.");
                    yield break;
                }
                else
                {
                    foreach (var classSpell in classDef.ClassSpellList)
                    {
                        if (!characterData.KnownSpellIDs.Contains(classSpell.spell.DefinitionID))
                        {
                            Debug.Log($"Adding missing spell '{classSpell.spell.DisplayName}' to player {playerID} character data.");
                            characterData.KnownSpellIDs.Add(classSpell.spell.DefinitionID);
                        }
                    }
                }

                characterData.PVPFactionID = RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID)?.DefaultPVPFaction?.DefinitionID;

                for (int i = characterData.SavedInventory.Count - 1; i >= 0; i--)
                {
                    var entry = characterData.SavedInventory[i];
                    if (Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemID) == null)
                    {
                        Debug.Warning($"Removing invalid inventory entry at slot {entry.SlotIndex} with unknown item '{entry.ItemID}' for player {playerID}.");
                        characterData.SavedInventory.RemoveAt(i);
                    }
                    else if (string.IsNullOrEmpty(entry.ItemGUID))
                    {
                        entry.ItemGUID = System.Guid.NewGuid().ToString("N");
                        characterData.SavedInventory[i] = entry;
                        Debug.Log($"Assigned GUID to inventory entry at slot {entry.SlotIndex} for player {playerID}.");
                    }
                }

                for (int i = characterData.SavedEquipment.Count - 1; i >= 0; i--)
                {
                    var entry = characterData.SavedEquipment[i];
                    var def = Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemID);
                    if (def == null || (def.ItemType != Shared.Data.ItemType.Weapon && def.ItemType != Shared.Data.ItemType.Armor))
                    {
                        Debug.Warning($"Removing invalid equipment entry at slot {entry.SlotIndex} with unknown item '{entry.ItemID}' for player {playerID}.");
                        characterData.SavedEquipment.RemoveAt(i);
                    }
                    else if (string.IsNullOrEmpty(entry.ItemGUID))
                    {
                        entry.ItemGUID = System.Guid.NewGuid().ToString("N");
                        characterData.SavedEquipment[i] = entry;
                        Debug.Log($"Assigned GUID to equipment entry at slot {entry.SlotIndex} for player {playerID}.");
                    }
                }

                // Ensure player has all required weapon skills for their class
                foreach (var weaponType in classDef.WeaponTypes)
                {
                    if (!characterData.SavedWeaponSkills.Any(ws => ws.WeaponType == (int)weaponType))
                    {
                        characterData.SavedWeaponSkills.Add(new Shared.Persistence.SavedWeaponSkill
                        {
                            WeaponType = (int)weaponType,
                            Level = 1,
                            Experience = 0
                        });
                        Debug.Log($"Added weapon skill for {weaponType} to player {playerID}.");
                    }
                }

                // Remove weapon skills no longer valid for this class
                characterData.SavedWeaponSkills.RemoveAll(ws =>
                {
                    var weaponType = (Shared.Data.ItemWeaponType)ws.WeaponType;
                    bool isInvalid = !classDef.WeaponTypes.Contains(weaponType);
                    if (isInvalid)
                        Debug.Warning($"Removing invalid weapon skill {weaponType} from player {playerID}.");
                    return isInvalid;
                });

                foreach (var startingRep in RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID).StartingReputations)
                {
                    if (characterData.SavedReputations.Any(rep => rep.FactionID == startingRep.Faction.DefinitionID))
                        continue;

                    characterData.SavedReputations.Add(new Shared.Persistence.SavedReputationEntry
                    {
                        FactionID = startingRep.Faction.DefinitionID,
                        Reputation = startingRep.Reputation
                    });
                    Debug.Log($"Set initial reputation with faction '{startingRep.Faction.DisplayName}' to {startingRep.Reputation} for player {playerID}.");
                }

                try
                {
                    playerActor.SetCharacterData(characterData);
                }
                catch (Exception e)
                {
                    Debug.Error($"Error setting character data for player {playerID}: {e}");
                }

                var json = JsonUtility.ToJson(characterData);
                Debug.Log($"Character JSON length: {json.Length}");

                ServerStatManager.Instance.InitializePlayerStats(playerActor);
                ServerStatManager.Instance.RegisterActor(playerActor);

                // Stat initialization sets all stats to 0 before filling to max,
                // which transiently triggers the death check. Reset the flag.
                playerActor.isDead = false;

                // Trigger initial 3x3 chunk interest around the accepted spawn position.
                if (!ServerInterestManager.IsInitialized
                    || ServerInterestManager.Instance == null)
                {
                    throw new InvalidOperationException(
                        "Initial player interest requires initialized server interest manager.");
                }

                ServerInterestManager.Instance.UpdatePlayerInterestFromAcceptedPosition(
                    playerActor,
                    spawnWorldPosition);

                // Send the finalized character data to the client now, after all server-side
                // modifications are complete. This must happen before AddPlayerToScene so
                // the client has _activeCharacter set before actor ownership is replicated.
                Game.Shared.Networking.AccountEndpoint.Client_EnterWorld(playerID, characterData, $"World_{spawnScene.x}_{spawnScene.y}");

                // Add the player to global_actors AFTER ChunkSubscriptions is populated.
                // This avoids a race where PurrNet's Spawn() and OnPlayerLoadedScene both
                // send spawn packets before the visibility rule has data to work with.
                ScenePlayersModule scenePlayers = _networkManager.scenePlayersModule;
                bool playerLoadedInGlobalScene = scenePlayers.IsPlayerLoadedInScene(playerID, globalSceneID);
                void OnPlayerLoadedScene(PlayerID loadedPlayerID, SceneID loadedSceneID, bool asServer)
                {
                    if (asServer && loadedPlayerID == playerID && loadedSceneID == globalSceneID)
                        playerLoadedInGlobalScene = true;
                }

                scenePlayers.onPlayerLoadedScene += OnPlayerLoadedScene;
                try
                {
                    scenePlayers.AddPlayerToScene(playerID, globalSceneID);
                    if (!scenePlayers.IsPlayerInScene(playerID, globalSceneID))
                    {
                        throw new InvalidOperationException(
                            $"PurrNet did not add player {playerID} to the global actors scene {globalSceneID}.");
                    }

                    if (!playerLoadedInGlobalScene)
                        yield return new WaitUntil(() => playerLoadedInGlobalScene);
                }
                finally
                {
                    scenePlayers.onPlayerLoadedScene -= OnPlayerLoadedScene;
                }

                if (!scenePlayers.IsPlayerLoadedInScene(playerID, globalSceneID))
                {
                    throw new InvalidOperationException(
                        $"Player {playerID} has not finished loading the global actors scene {globalSceneID}.");
                }

                ServerInterestManager.Instance.ReevaluateActorVisibility(
                    playerActor,
                    playerActor.SpatialContext,
                    spawnWorldPosition,
                    playerActor.SpatialContext,
                    spawnWorldPosition);
                }
            finally
            {
                worldManager.ReleaseChunk(runtimeContext, spawnScene);
            }
        }

        /// <summary>Queues an actor prefab at an absolute world position in a networked scene.</summary>
        /// <param name="sourceChunk">The verified environment tile supplying the spawn position and context.</param>
        /// <param name="worldPosition">The finite absolute world position for the spawned actor.</param>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="sceneID">The networked scene identifier.</param>
        /// <param name="owner">The player that owns the spawned actor.</param>
        public void SpawnAt(
            WorldChunkKey sourceChunk,
            WorldPosition worldPosition,
            GameObject prefab,
            SceneID sceneID,
            PlayerID owner)
        {
            if (!sourceChunk.Context.IsValid)
                throw new ArgumentException("The source chunk must contain a valid context.", nameof(sourceChunk));
            if (!worldPosition.IsFinite)
                throw new ArgumentException("The spawn world position must be finite.", nameof(worldPosition));
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Queueing a spawn requires an initialized server world manager.");
            }
            if (!ServerWorldManager.Instance.TryGetChunkSceneID(
                    sourceChunk.Context,
                    sourceChunk.ChunkCoord,
                    out _))
            {
                throw new InvalidOperationException(
                    $"The exact source chunk '{sourceChunk.ChunkCoord}' is not loaded for context "
                    + $"({sourceChunk.Context.WorldID}, {sourceChunk.Context.MapID}, {sourceChunk.Context.InstanceID}).");
            }
            FixedString128Bytes sourceMapID = ToSpawnContextMapID(sourceChunk.Context);

            string sceneName = $"World_{sourceChunk.ChunkCoord.x}_{sourceChunk.ChunkCoord.y}";
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because network scene '{sceneID}' is not available.");
                return;
            }
            if (!state.scene.IsValid() || !state.scene.isLoaded)
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because scene '{state.scene.name}' is not loaded.");
                return;
            }
            if (prefab == null)
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because the prefab is null.");
                return;
            }

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Queueing a spawn requires an initialized server physics manager.");
            }
            ServerPhysicsManager.Instance.ToPhysicalPosition(worldPosition);

            int prefabIndex = RegisterPrefab(prefab);
            int sceneIndex = RegisterScene(state.scene);

            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new Systems.SpawnCommand
            {
                WorldID = sourceChunk.Context.WorldID,
                MapID = sourceMapID,
                InstanceID = sourceChunk.Context.InstanceID,
                WorldPositionD = new double3(worldPosition.x, worldPosition.y, worldPosition.z),
                PrefabIndex = prefabIndex,
                SceneIndex = sceneIndex,
                SceneName = sceneName,
                Owner = owner,
                ChunkCoord = new int2(sourceChunk.ChunkCoord.x, sourceChunk.ChunkCoord.y)
            });
        }

        /// <summary>Commits the ECS spawn-command snapshot at the authoritative tick boundary.</summary>
        internal void CommitSpawnCommands()
        {
            // The authoritative simulation owns this snapshot and invokes it after all ECS systems finish.
            using var markerScope = UPDATE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            if (_spawnCommandQuery.IsEmpty)
            {
                if (telemetryEnabled)
                    PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, 0);
                return;
            }

            NativeArray<Entity> entities = _spawnCommandQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                NativeArray<Systems.SpawnCommand> commands = default;
                try
                {
                    commands = _spawnCommandQuery.ToComponentDataArray<Systems.SpawnCommand>(Unity.Collections.Allocator.Temp);

                    if (telemetryEnabled)
                        PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, commands.Length);

                    for (int i = 0; i < commands.Length; i++)
                    {
                        var cmd = commands[i];
                        string prefabName = $"registry index {cmd.PrefabIndex}";
                        string sceneName = cmd.SceneName.ToString();
                        string sceneNameForLog = $"registry index {cmd.SceneIndex}";
                        GameObject prefab = null;
                        GameObject spawnedObject = null;
                        Actor spawnedActor = null;
                        NetworkIdentity registeredIdentity = null;
                        Actor spawnPointReservation = null;
                        SpawnPointRuntimeKey spawnPointKey = default;
                        ActorSpatialContext spawnContext = default;
                        bool hasSpawnContext = false;
                        string spawnFailureReason = "spawn command failed";
                        bool hasSpawnPointReservation = false;
                        bool spawnPointReservationUpdated = false;
                        bool playerActorRegistered = false;
                        bool hadPreviousPlayerActor = false;
                        Actor previousPlayerActor = null;
                        PlayerSessionData session = null;
                        Actor previousSessionActor = null;
                        bool sessionActorRegistered = false;
                        bool npcStatsRegistered = false;
                        bool actorSpawnEventStarted = false;
                        bool spawnSucceeded = false;

                        try
                        {
                            string mapID = cmd.MapID.ToString();
                            if (cmd.WorldID == Guid.Empty)
                                throw new InvalidOperationException("The spawn command world ID is empty.");
                            if (string.IsNullOrWhiteSpace(mapID))
                                throw new InvalidOperationException("The spawn command map ID is empty.");

                            spawnContext = cmd.InstanceID == Guid.Empty
                                ? ActorSpatialContext.CreatePublic(cmd.WorldID, mapID)
                                : ActorSpatialContext.CreateInstanced(cmd.WorldID, mapID, cmd.InstanceID);
                            hasSpawnContext = true;

                            if (cmd.SpawnPointGUID != Guid.Empty)
                            {
                                spawnPointKey = new SpawnPointRuntimeKey(
                                    spawnContext,
                                    cmd.SpawnPointGUID);
                            }

                            if (cmd.PrefabIndex < 0 || cmd.PrefabIndex >= _prefabRegistry.Count)
                                throw new InvalidOperationException($"Prefab registry index {cmd.PrefabIndex} is invalid.");

                            prefab = _prefabRegistry[cmd.PrefabIndex];
                            if (prefab == null)
                                throw new InvalidOperationException($"Prefab registry entry {cmd.PrefabIndex} is null or has been destroyed.");
                            prefabName = prefab.name;

                            if (!_sceneRegistry.TryGetValue(cmd.SceneIndex, out var scene))
                                throw new InvalidOperationException($"Scene registry index {cmd.SceneIndex} is invalid.");
                            if (!scene.IsValid() || !scene.isLoaded)
                                throw new InvalidOperationException($"Scene registry entry {cmd.SceneIndex} is no longer loaded.");
                            sceneNameForLog = scene.name;

                            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                                throw new InvalidOperationException("The server world manager is not initialized.");

                            var sourceChunk = new WorldChunkKey(
                                spawnContext,
                                new Vector2Int(cmd.ChunkCoord.x, cmd.ChunkCoord.y));
                            if (cmd.SpawnPointGUID != Guid.Empty)
                            {
                                hasSpawnPointReservation = _spawnPointActorMap.TryGetValue(
                                    spawnPointKey,
                                    out spawnPointReservation);
                                if (!hasSpawnPointReservation || spawnPointReservation == null)
                                {
                                    throw new InvalidOperationException(
                                        $"Spawn-point reservation for '{cmd.SpawnPointGUID}' is missing in context {spawnContext}.");
                                }
                            }

                            if (!ServerWorldManager.Instance.TryGetChunkSceneID(
                                    sourceChunk.Context,
                                    sourceChunk.ChunkCoord,
                                    out _))
                            {
                                throw new InvalidOperationException(
                                    $"The queued source chunk '{sourceChunk.ChunkCoord}' is no longer loaded for context "
                                    + $"({spawnContext.WorldID}, {spawnContext.MapID}, {spawnContext.InstanceID}).");
                            }

                            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                                throw new InvalidOperationException("The server physics manager is not initialized.");
                            if (!ServerActorManager.IsInitialized || ServerActorManager.Instance == null)
                                throw new InvalidOperationException("Consuming a spawn command requires an initialized server actor manager.");

                            var spawnWorldPosition = new WorldPosition(
                                cmd.WorldPositionD.x,
                                cmd.WorldPositionD.y,
                                cmd.WorldPositionD.z);
                            if (!spawnWorldPosition.IsFinite)
                                throw new InvalidOperationException("The spawn command world position must be finite.");

                            var physicalPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(spawnWorldPosition);

                            // Use PurrNet's Instantiate proxy — handles scene placement,
                            // hierarchy registration, spawn, and replication in one call.
                            spawnedObject = PurrNet.UnityProxy.Instantiate(prefab, physicalPosition, Quaternion.identity, scene);
                            if (spawnedObject == null)
                                throw new InvalidOperationException("PurrNet returned a null spawned object.");
                            var actor = spawnedObject.GetComponent<Game.Shared.Runtime.Actors.Actor>();
                            if (actor == null)
                                throw new InvalidOperationException($"Prefab '{prefabName}' is missing an Actor component. Add it to the prefab in the editor.");
                            spawnedActor = actor;

                            actor.CurrentChunk = sceneName;
                            actor.SetAuthoritativeSpatialContext(spawnContext);
                            actor.Initialize(actor.ID, cmd.Owner != default ? $"Player_{cmd.Owner}" : prefabName);
                            actor.SetSpawnPointGUID(cmd.SpawnPointGUID);

                            if (hasSpawnPointReservation)
                            {
                                _spawnPointActorMap[spawnPointKey] = spawnedActor;
                                spawnPointReservationUpdated = true;
                            }

                            Debug.Log(
                                $"[Spawn] Spawned '{prefabName}' chunk=({sourceChunk.ChunkCoord.x},{sourceChunk.ChunkCoord.y}) "
                                + $"world={spawnContext.WorldID} map={spawnContext.MapID} "
                                + $"instance={spawnContext.InstanceID} position={physicalPosition} "
                                + $"scene='{sceneNameForLog}'.");

                            // Register the root identity for targeted visibility re-evaluation.
                            NetworkIdentity identity = spawnedObject.GetComponent<NetworkIdentity>();
                            if (identity != null)
                            {
                                Game.Shared.Networking.ChunkSubscriptions.RegisterIdentity(
                                    actor.SpatialContext,
                                    sceneName,
                                    identity);
                                registeredIdentity = identity;
                            }

                            // Set ownership and register player actor cache.
                            if (identity != null && cmd.Owner != default)
                            {
                                identity.GiveOwnership(cmd.Owner);
                                actor.SetOwner(cmd.Owner);
                                hadPreviousPlayerActor = Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                                    cmd.Owner,
                                    out previousPlayerActor);
                                Game.Shared.Networking.ChunkSubscriptions.RegisterPlayerActor(cmd.Owner, actor);
                                playerActorRegistered = true;
                                spawnedObject.name = $"Player{cmd.Owner}";

                                // Add a disabled NavMeshAgent for server-driven movement (charge spells, etc.).
                                if (spawnedObject.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                                {
                                    var playerAgent = spawnedObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
                                    playerAgent.radius = 0.25f;
                                    playerAgent.height = 2f;
                                    playerAgent.enabled = false;
                                }
                            }

                            // Initialize NPC stats (NPCs have no owner, so this must be outside the owner block).
                            if (!actor.IsPlayer)
                            {
                                ServerStatManager.Instance.InitializeNPCStats(actor);
                                ServerStatManager.Instance.RegisterActor(actor);
                                npcStatsRegistered = true;

                                // Teach NPCs the auto-attack spell so they can swing.
                                var autoAttack = GameConfigurationManager.Config.AutoAttackSpell;
                                if (autoAttack != null)
                                    spawnedObject.GetComponent<ActorSpellcaster>()?.LearnSpell(autoAttack.DefinitionID);
                            }

                            // Fire spawn event after Initialize + GiveOwnership so ECS entity is created with accurate data.
                            actorSpawnEventStarted = true;
                            ServerActorManager.Instance.RegisterSpawnedActor(actor, spawnWorldPosition);
                            Game.Shared.Networking.ActorEndpoint.onServerActorSpawned?.Invoke(actor);

                            // Link the actor to the player's session data for disconnect cleanup.
                            if (cmd.Owner != default)
                            {
                                session = ServerConnectionManager.Instance.GetSessionData(cmd.Owner);
                                if (session != null)
                                {
                                    previousSessionActor = session.PlayerActor;
                                    session.SetPlayerActor(actor);
                                    sessionActorRegistered = true;
                                }
                            }

                            spawnSucceeded = true;
                        }
                        catch (Exception ex)
                        {
                            spawnFailureReason = ex.Message;
                            Debug.Error($"Failed to consume spawn command for prefab '{prefabName}' in scene '{sceneNameForLog}' (spawn point {cmd.SpawnPointGUID}): {ex.Message}\n{ex.StackTrace}");
                        }
                        finally
                        {
                            if (!spawnSucceeded)
                            {
                                try
                                {
                                    if (sessionActorRegistered && session != null && session.PlayerActor == spawnedActor)
                                        session.SetPlayerActor(previousSessionActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to restore the player session after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (playerActorRegistered
                                        && Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(cmd.Owner, out var currentPlayerActor)
                                        && currentPlayerActor == spawnedActor)
                                    {
                                        if (hadPreviousPlayerActor)
                                            Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS[cmd.Owner] = previousPlayerActor;
                                        else
                                            Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(cmd.Owner);
                                    }
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to restore player actor subscriptions after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (npcStatsRegistered && ServerStatManager.IsInitialized)
                                        ServerStatManager.Instance.UnregisterActor(spawnedActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to unregister NPC stats after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (actorSpawnEventStarted && spawnedActor != null && ServerActorManager.IsInitialized)
                                        ServerActorManager.Instance.RollbackActorSpawn(spawnedActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to roll back ECS actor state after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (hasSpawnPointReservation)
                                    {
                                        if (!_spawnPointActorMap.TryGetValue(
                                                spawnPointKey,
                                                out var currentReservation))
                                        {
                                            throw new InvalidOperationException(
                                                $"Spawn-point reservation for '{cmd.SpawnPointGUID}' disappeared during rollback.");
                                        }
                                        if (!ReferenceEquals(currentReservation, spawnPointReservation)
                                            && !(spawnPointReservationUpdated
                                                && ReferenceEquals(currentReservation, spawnedActor)))
                                        {
                                            throw new InvalidOperationException(
                                                $"Spawn-point reservation for '{cmd.SpawnPointGUID}' changed during rollback.");
                                        }

                                        _spawnPointActorMap.Remove(spawnPointKey);
                                    }
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to release the spawn-point reservation for {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (registeredIdentity != null && spawnedActor != null)
                                    {
                                        Game.Shared.Networking.ChunkSubscriptions.UnregisterIdentity(
                                            spawnedActor.SpatialContext,
                                            sceneName,
                                            registeredIdentity);
                                    }
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to unregister the rejected spawn identity for {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                if (spawnedObject != null)
                                {
                                    try
                                    {
                                        PurrNet.UnityProxy.Destroy(spawnedObject);
                                    }
                                    catch (Exception cleanupException)
                                    {
                                        Debug.Error($"Failed to clean up rejected spawn object for prefab '{prefabName}' in scene '{sceneNameForLog}': {cleanupException.Message}\n{cleanupException.StackTrace}");
                                    }
                                }
                            }

                            // Always destroy the entity so it doesn't retry.
                            if (_entityManager.Exists(entities[i]))
                                _entityManager.DestroyEntity(entities[i]);

                            if (telemetryEnabled)
                            {
                                PerformanceTelemetry.TryRecord(
                                    spawnSucceeded ? SPAWN_SUCCESS_COUNT_METRIC : SPAWN_FAILURE_COUNT_METRIC,
                                    1);
                            }
                        }

                        if (hasSpawnContext && cmd.SpawnPointGUID != Guid.Empty)
                        {
                            CompleteSpawnPointPreparation(
                                spawnContext,
                                spawnPointKey,
                                spawnSucceeded,
                                spawnFailureReason);
                        }
                    }

                }
                finally
                {
                    if (commands.IsCreated)
                        commands.Dispose();
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }
        private int RegisterPrefab(GameObject scenePrefab)
        {
            // Resolve to PurrNet's own reference so Spawn() identity check passes.
            // Addressable scenes deserialize a different object instance than what
            // NetworkPrefabs holds, so we match by name.
            foreach (var data in _networkManager.prefabProvider.allPrefabs)
            {
                if (data.prefab != null && data.prefab.name == scenePrefab.name)
                {
                    int idx = _prefabRegistry.IndexOf(data.prefab);
                    if (idx >= 0) return idx;
                    _prefabRegistry.Add(data.prefab);
                    return _prefabRegistry.Count - 1;
                }
            }

            Debug.Error($"Prefab '{scenePrefab.name}' not found in PurrNet's NetworkPrefabs!");
            _prefabRegistry.Add(scenePrefab);
            return _prefabRegistry.Count - 1;
        }

        private int RegisterScene(Scene scene)
        {
            foreach (var kvp in _sceneRegistry)
                if (kvp.Value == scene) return kvp.Key;
            int idx = _nextSceneIndex++;
            _sceneRegistry[idx] = scene;
            return idx;
        }
    }
}
