using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Modules;
using System;
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

                try
                {
                    ServerPhysicsManager.Instance.BindEnvironmentScene(chunkKey, state.scene);
                }
                catch (Exception exception)
                {
                    try
                    {
                        CompletePendingChunkLoad(
                            chunkKey,
                            sceneID,
                            false,
                            $"World scene '{sceneName}' failed physical binding for chunk {chunkKey} with SceneID {sceneID}: {exception.Message}",
                            null);
                    }
                    finally
                    {
                        QueueSceneUnload(sceneID);
                    }

                    return;
                }

                _loadedChunks.Add(chunkKey, sceneID);
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
                ServerPhysicsManager.Instance.UnbindEnvironmentScene(pendingUnloadKey);
                _pendingUnloadOwners.Remove(sceneID);
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
                    ServerPhysicsManager.Instance.UnbindEnvironmentScene(unloadedKey);
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
