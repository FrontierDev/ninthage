using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.World;
using Game.Shared.Utility;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    /// <summary>
    /// Server authority for world chunk scene loading and unloading.
    /// Does not manage player subscriptions — see ServerInterestManager.
    /// </summary>
    public sealed partial class ServerWorldManager : MonoBehaviour
    {
        private const string MAIN_MAP_ID = "main-world";
        private const int MAX_PENDING_TELEMETRY_OPERATIONS = 256;
        private static readonly PerformanceMetricDefinition SCENE_LOAD_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_WORLD_SCENE_LOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "world_manager",
            "scene_operation");
        private static readonly PerformanceMetricDefinition SCENE_UNLOAD_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_WORLD_SCENE_UNLOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "world_manager",
            "scene_operation");
        private static readonly PerformanceMetricDefinition PENDING_SCENE_OPERATION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_WORLD_PENDING_SCENE_OPERATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "world_manager",
            "scene_operation");
        private static ServerWorldManager _instance;
        /// <summary>Gets the active server world manager.</summary>
        public static ServerWorldManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the world manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private static ActorSpatialContext _runtimeContext;
        private static ActorSpatialContext _validationInstanceA;
        private static ActorSpatialContext _validationInstanceB;

        private readonly Dictionary<WorldChunkKey, List<TransferCollider>> _transferCollidersByChunk = new();
        private readonly Dictionary<string, ActorSpatialContext> _transferInstancesByKey = new(StringComparer.Ordinal);

        /// <summary>Initializes the authoritative public context for the current server lifetime.</summary>
        internal static void InitializeRuntimeContext(Guid worldID)
        {
            if (_runtimeContext.IsValid)
                throw new InvalidOperationException("The server world runtime context is already initialized.");

            _runtimeContext = ActorSpatialContext.CreatePublic(worldID, MAIN_MAP_ID);

            Guid instanceAID;
            do
            {
                instanceAID = Guid.NewGuid();
            }
            while (instanceAID == Guid.Empty);

            Guid instanceBID;
            do
            {
                instanceBID = Guid.NewGuid();
            }
            while (instanceBID == Guid.Empty || instanceBID == instanceAID);

            _validationInstanceA = ActorSpatialContext.CreateInstanced(
                _runtimeContext.WorldID,
                _runtimeContext.MapID,
                instanceAID);
            _validationInstanceB = ActorSpatialContext.CreateInstanced(
                _runtimeContext.WorldID,
                _runtimeContext.MapID,
                instanceBID);

            Debug.Log(
                $"[InstanceValidation] Created runtime instances A={instanceAID} B={instanceBID}");
        }

        /// <summary>Clears the authoritative public context during server teardown.</summary>
        internal static void ClearRuntimeContext()
        {
            _runtimeContext = default;
            _validationInstanceA = default;
            _validationInstanceB = default;
        }

        private NetworkManager _networkManager;
        private PurrSceneSettings _worldSceneSettings;

        private long _globalActorsLoadTimestamp;

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneID;
        /// <summary>Gets the networked scene containing global actors.</summary>
        public SceneID GlobalActorsSceneID => _globalActorsSceneID;
        /// <summary>Gets whether the global actor scene has loaded.</summary>
        public bool IsGlobalActorsSceneLoaded => _globalActorsSceneID != default;
        /// <summary>Gets the authoritative public context for the current server lifetime.</summary>
        public ActorSpatialContext CurrentRuntimeContext => _runtimeContext;
        /// <summary>Gets the first server-lifetime validation instance.</summary>
        internal ActorSpatialContext ValidationInstanceA => _validationInstanceA;
        /// <summary>Gets the second server-lifetime validation instance.</summary>
        internal ActorSpatialContext ValidationInstanceB => _validationInstanceB;

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(SCENE_LOAD_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SCENE_UNLOAD_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(PENDING_SCENE_OPERATION_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerWorldManager is already initialized!");
                return;
            }
            if (!_runtimeContext.IsValid)
                throw new InvalidOperationException("The server world runtime context must be initialized before the world manager.");
            _instance = this;

            _networkManager = NetworkManager.main;

            _worldSceneSettings = new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = false,
            };

            _networkManager.sceneModule.onSceneLoaded += OnChunkSceneLoaded;
            _networkManager.sceneModule.onSceneUnloaded += OnChunkSceneUnloaded;

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _networkManager.sceneModule != null)
            {
                _networkManager.sceneModule.onSceneLoaded -= OnChunkSceneLoaded;
                _networkManager.sceneModule.onSceneUnloaded -= OnChunkSceneUnloaded;
            }

            _transferCollidersByChunk.Clear();
            _transferInstancesByKey.Clear();

            foreach (var cleanup in _pendingLoadCleanup.Values)
                cleanup?.Invoke();
            _pendingLoadCleanup.Clear();
            _pendingUnloadOwners.Clear();
            _scenesAwaitingUnload.Clear();

            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
                ClearRuntimeContext();
            }
        }

        private void OnChunkSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!asServer) return;
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;

            string sceneName = state.scene.name;

            if (sceneName == "global_actors")
            {
                if (_globalActorsLoadTimestamp > 0)
                {
                    RecordSceneDuration(SCENE_LOAD_DURATION_METRIC, _globalActorsLoadTimestamp);
                    _globalActorsLoadTimestamp = 0;
                }
                _globalActorsSceneID = sceneID;
                Debug.Log("Global actors scene loaded.");
                RecordPendingSceneOperations();
                return;
            }

            if (sceneName.StartsWith("World"))
            {
                if (_scenesAwaitingUnload.Contains(sceneID))
                    return;

                if (!_pendingSceneOwners.TryGetValue(sceneID, out var chunkKey))
                {
                    FailUnownedWorldScene(
                        sceneID,
                        $"World scene '{sceneName}' loaded with SceneID {sceneID} without a pending context owner.");
                    return;
                }
                if (!_pendingLoads.ContainsKey(chunkKey))
                {
                    CompletePendingChunkLoad(
                        chunkKey,
                        sceneID,
                        false,
                        $"World scene '{sceneName}' loaded for context key {chunkKey.ChunkCoord}, but its load is no longer pending.",
                        null);
                    QueueSceneUnload(sceneID);
                    return;
                }
                if (_loadedChunks.ContainsKey(chunkKey))
                {
                    CompletePendingChunkLoad(
                        chunkKey,
                        sceneID,
                        false,
                        $"World scene '{sceneName}' loaded with a conflicting existing scene mapping for tile {chunkKey.ChunkCoord}.",
                        null);
                    QueueSceneUnload(sceneID);
                    return;
                }

                bool physicalBindingEstablished = false;
                try
                {
                    ServerPhysicsManager.Instance.BindEnvironmentScene(chunkKey, state.scene);
                    physicalBindingEstablished = true;
                    RegisterTransferColliders(chunkKey, state.scene);
                    _loadedChunks.Add(chunkKey, sceneID);
                }
                catch (Exception exception)
                {
                    _transferCollidersByChunk.Remove(chunkKey);
                    if (_loadedChunks.TryGetValue(chunkKey, out SceneID registeredSceneID)
                        && registeredSceneID == sceneID)
                    {
                        _loadedChunks.Remove(chunkKey);
                    }

                    string failure = exception.Message;
                    if (physicalBindingEstablished)
                    {
                        try
                        {
                            ServerPhysicsManager.Instance.UnbindEnvironmentScene(chunkKey);
                        }
                        catch (Exception unbindException)
                        {
                            failure += $"; physical binding rollback failed: {unbindException.Message}";
                        }
                    }

                    try
                    {
                        CompletePendingChunkLoad(
                            chunkKey,
                            sceneID,
                            false,
                            $"World scene '{sceneName}' failed registration for chunk {chunkKey} with SceneID {sceneID}: {failure}",
                            null);
                    }
                    finally
                    {
                        QueueSceneUnload(sceneID);
                    }

                    return;
                }

                if (_pendingLoadTimestamps != null
                    && _pendingLoadTimestamps.TryGetValue(chunkKey, out long startedTimestamp))
                {
                    _pendingLoadTimestamps.Remove(chunkKey);
                    RecordSceneDuration(SCENE_LOAD_DURATION_METRIC, startedTimestamp);
                }
                CompletePendingChunkLoad(chunkKey, sceneID, true, null, null);
                Debug.Log($"Scene '{sceneName}' loaded.");
            }
        }

        private void OnChunkSceneUnloaded(SceneID sceneID, bool asServer)
        {
            if (!asServer)
                return;

            if (_pendingUnloadOwners.TryGetValue(sceneID, out WorldChunkKey pendingUnloadKey))
            {
                try
                {
                    ServerPhysicsManager.Instance.UnbindEnvironmentScene(pendingUnloadKey);
                }
                finally
                {
                    _transferCollidersByChunk.Remove(pendingUnloadKey);
                    _pendingUnloadOwners.Remove(sceneID);
                }
            }
            else
            {
                WorldChunkKey unloadedKey = default;
                bool hasLoadedKey = false;
                foreach (var loadedChunk in _loadedChunks)
                {
                    if (loadedChunk.Value != sceneID)
                        continue;

                    unloadedKey = loadedChunk.Key;
                    hasLoadedKey = true;
                    break;
                }
                if (hasLoadedKey)
                {
                    _loadedChunks.Remove(unloadedKey);
                    try
                    {
                        ServerPhysicsManager.Instance.UnbindEnvironmentScene(unloadedKey);
                    }
                    finally
                    {
                        _transferCollidersByChunk.Remove(unloadedKey);
                    }
                    Debug.Error(
                        $"Tracked world chunk {unloadedKey} reached terminal unload outside the owned UnloadChunk path.");
                }
            }

            if (_globalActorsSceneID == sceneID)
                _globalActorsSceneID = default;

            if (_pendingUnloadTimestamps == null)
                return;
            if (!_pendingUnloadTimestamps.TryGetValue(sceneID, out long startedTimestamp))
                return;

            _pendingUnloadTimestamps.Remove(sceneID);
            RecordSceneDuration(SCENE_UNLOAD_DURATION_METRIC, startedTimestamp);
            RecordPendingSceneOperations();
        }

        private void RegisterTransferColliders(WorldChunkKey chunkKey, Scene scene)
        {
            var colliders = new List<TransferCollider>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TransferCollider collider in root.GetComponentsInChildren<TransferCollider>(true))
                {
                    if (collider == null)
                        throw new InvalidOperationException(
                            $"World chunk {chunkKey} contains a missing TransferCollider component.");
                    if (string.IsNullOrWhiteSpace(collider.InstanceKey))
                        throw new InvalidOperationException(
                            $"World chunk {chunkKey} contains a TransferCollider with an empty InstanceKey.");

                    Vector2 size = collider.Size;
                    if (float.IsNaN(size.x) || float.IsInfinity(size.x) || size.x <= 0f
                        || float.IsNaN(size.y) || float.IsInfinity(size.y) || size.y <= 0f)
                    {
                        throw new InvalidOperationException(
                            $"World chunk {chunkKey} contains TransferCollider key='{collider.InstanceKey}' with invalid size {size}.");
                    }

                    colliders.Add(collider);
                }
            }

            if (!_transferCollidersByChunk.TryAdd(chunkKey, colliders))
                throw new InvalidOperationException(
                    $"TransferCollider registrations already exist for world chunk {chunkKey}.");
        }

        internal int AppendTransferColliders(
            ActorSpatialContext context,
            WorldPosition previousPosition,
            WorldPosition currentPosition,
            List<TransferCollider> resultBuffer)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid spatial context is required.", nameof(context));
            if (!previousPosition.IsFinite)
                throw new ArgumentException("Previous position must be finite.", nameof(previousPosition));
            if (!currentPosition.IsFinite)
                throw new ArgumentException("Current position must be finite.", nameof(currentPosition));
            if (resultBuffer == null)
                throw new ArgumentNullException(nameof(resultBuffer));

            int initialCount = resultBuffer.Count;
            AppendTransferColliders(new WorldChunkKey(context, previousPosition.Chunk), resultBuffer);
            if (currentPosition.Chunk != previousPosition.Chunk)
                AppendTransferColliders(new WorldChunkKey(context, currentPosition.Chunk), resultBuffer);

            return resultBuffer.Count - initialCount;
        }

        private void AppendTransferColliders(WorldChunkKey chunkKey, List<TransferCollider> resultBuffer)
        {
            if (!_transferCollidersByChunk.TryGetValue(chunkKey, out List<TransferCollider> colliders))
                return;

            resultBuffer.AddRange(colliders);
        }

        internal ActorSpatialContext ResolveOrCreateTransferInstance(string instanceKey)
        {
            if (!_runtimeContext.IsValid)
                throw new InvalidOperationException("The current server runtime context is not initialized.");
            if (string.IsNullOrWhiteSpace(instanceKey))
                throw new ArgumentException("A nonempty transfer instance key is required.", nameof(instanceKey));
            if (_transferInstancesByKey.TryGetValue(instanceKey, out ActorSpatialContext existing))
                return existing;

            Guid instanceID;
            do
            {
                instanceID = Guid.NewGuid();
            }
            while (instanceID == Guid.Empty);

            ActorSpatialContext context = ActorSpatialContext.CreateInstanced(
                _runtimeContext.WorldID,
                _runtimeContext.MapID,
                instanceID);
            _transferInstancesByKey.Add(instanceKey, context);
            Debug.Log($"[TransferCollider] Created runtime instance key='{instanceKey}' instance={instanceID}");
            return context;
        }

        internal bool TryGetTransferInstance(string instanceKey, out ActorSpatialContext context)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
                throw new ArgumentException("A nonempty transfer instance key is required.", nameof(instanceKey));

            return _transferInstancesByKey.TryGetValue(instanceKey, out context);
        }

        /// <summary>
        /// Loads the persistent global_actors scene where all actor GameObjects reside.
        /// </summary>
        public void LoadGlobalActorsScene()
        {
            var settings = new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = false,
            };
            if (PerformanceTelemetry.IsEnabled)
            {
                _globalActorsLoadTimestamp = Stopwatch.GetTimestamp();
                RecordPendingSceneOperations();
            }
            _networkManager.sceneModule.LoadSceneAsync("global_actors", settings);
        }

        private void RecordPendingSceneOperations()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            int pendingOperationCount = _pendingLoads.Count;
            if (_pendingUnloadTimestamps != null)
                pendingOperationCount += _pendingUnloadTimestamps.Count;
            if (_globalActorsLoadTimestamp > 0)
                pendingOperationCount++;
            PerformanceTelemetry.TryRecord(PENDING_SCENE_OPERATION_COUNT_METRIC, pendingOperationCount);
        }

        private static void RecordSceneDuration(PerformanceMetricDefinition metric, long startedTimestamp)
        {
            if (!PerformanceTelemetry.IsEnabled || startedTimestamp <= 0)
                return;

            long elapsedTimestamp = Stopwatch.GetTimestamp() - startedTimestamp;
            if (elapsedTimestamp < 0)
                return;

            PerformanceTelemetry.TryRecord(
                metric,
                elapsedTimestamp * 1000d / Stopwatch.Frequency);
        }
    }
}
