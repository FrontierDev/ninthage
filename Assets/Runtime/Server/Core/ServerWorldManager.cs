using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
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
    public sealed class ServerWorldManager : MonoBehaviour
    {
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

        private NetworkManager _networkManager;
        private PurrSceneSettings _worldSceneSettings;

        // Chunk coord → PurrNet SceneID
        private readonly Dictionary<Vector2Int, SceneID> _loadedChunks = new();
        /// <summary>Gets the currently loaded world chunks.</summary>
        public IReadOnlyDictionary<Vector2Int, SceneID> LoadedChunks => _loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<Vector2Int, List<Action<SceneID>>> _pendingLoads = new();
        private Dictionary<Vector2Int, long> _pendingLoadTimestamps;
        private Dictionary<SceneID, long> _pendingUnloadTimestamps;
        private long _globalActorsLoadTimestamp;

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneID;
        /// <summary>Gets the networked scene containing global actors.</summary>
        public SceneID GlobalActorsSceneID => _globalActorsSceneID;
        /// <summary>Gets whether the global actor scene has loaded.</summary>
        public bool IsGlobalActorsSceneLoaded => _globalActorsSceneID != default;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerWorldManager is already initialized!");
                return;
            }
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

            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
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
                Vector2Int chunkCoord = ParseChunkCoordinate(sceneName);
                if (_pendingLoadTimestamps != null
                    && _pendingLoadTimestamps.TryGetValue(chunkCoord, out long startedTimestamp))
                {
                    _pendingLoadTimestamps.Remove(chunkCoord);
                    RecordSceneDuration(SCENE_LOAD_DURATION_METRIC, startedTimestamp);
                }
                _loadedChunks[chunkCoord] = sceneID;
                Debug.Log($"Scene '{sceneName}' loaded.");
                RecordPendingSceneOperations();
            }
        }

        private void OnChunkSceneUnloaded(SceneID sceneID, bool asServer)
        {
            if (!asServer)
                return;
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

        /// <summary>Parses a chunk coordinate from a world scene name.</summary>
        /// <param name="sceneName">The world scene name.</param>
        /// <returns>The parsed chunk coordinate.</returns>
        public Vector2Int ParseChunkCoordinate(string sceneName)
        {
            string[] parts = sceneName.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);
            return new Vector2Int(x, y);
        }

        /// <summary>Determines whether a chunk scene is loaded.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool IsChunkLoaded(Vector2Int chunkCoord)
        {
            return _loadedChunks.ContainsKey(chunkCoord);
        }

        /// <summary>Determines whether a chunk scene is currently loading.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when a load is pending.</returns>
        public bool IsChunkLoadPending(Vector2Int chunkCoord)
        {
            return _pendingLoads.ContainsKey(chunkCoord);
        }

        /// <summary>Attempts to get the networked scene identifier for a loaded chunk.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <param name="sceneID">Receives the scene identifier when found.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool TryGetChunkSceneID(Vector2Int chunkCoord, out SceneID sceneID)
        {
            return _loadedChunks.TryGetValue(chunkCoord, out sceneID);
        }

        /// <summary>
        /// Ensures a chunk scene is loaded. If already loaded, invokes the callback immediately
        /// with the existing SceneID. Otherwise triggers an async load and invokes the callback
        /// once the scene is ready.
        /// </summary>
        public void EnsureChunkLoaded(Actor actor, Vector2Int chunkCoord, Action<SceneID> onReady)
        {
            if (_loadedChunks.TryGetValue(chunkCoord, out SceneID sceneID))
            {
                onReady?.Invoke(sceneID);
                return;
            }

            // If a load is already in-flight, just queue the callback.
            if (_pendingLoads.TryGetValue(chunkCoord, out var callbacks))
            {
                callbacks.Add(onReady);
                return;
            }

            _pendingLoads[chunkCoord] = new() { onReady };
            if (PerformanceTelemetry.IsEnabled)
            {
                if (_pendingLoadTimestamps == null)
                    _pendingLoadTimestamps = new Dictionary<Vector2Int, long>();
                if (_pendingLoadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingLoadTimestamps[chunkCoord] = Stopwatch.GetTimestamp();
                RecordPendingSceneOperations();
            }

            var address = $"World_{chunkCoord.x}_{chunkCoord.y}";
            _networkManager.sceneModule.LoadAddressableSceneAsync(address, _worldSceneSettings);

            void OnLoaded(SceneID loadedSceneID, bool asServer)
            {
                if (!asServer) return;
                if (!_loadedChunks.TryGetValue(chunkCoord, out var chunkSceneID)) return;
                if (chunkSceneID != loadedSceneID) return;

                _networkManager.sceneModule.onSceneLoaded -= OnLoaded;

                if (_pendingLoads.TryGetValue(chunkCoord, out var pending))
                {
                    _pendingLoads.Remove(chunkCoord);
                    foreach (var cb in pending)
                        cb?.Invoke(loadedSceneID);
                }
                RecordPendingSceneOperations();
            }

            _networkManager.sceneModule.onSceneLoaded += OnLoaded;
        }

        /// <summary>
        /// Unloads a chunk scene. Should only be called when no players remain subscribed.
        /// </summary>
        public void UnloadChunk(Vector2Int chunkCoord)
        {
            if (!_loadedChunks.TryGetValue(chunkCoord, out SceneID sceneID))
                return;

            _loadedChunks.Remove(chunkCoord);
            if (PerformanceTelemetry.IsEnabled)
            {
                if (_pendingUnloadTimestamps == null)
                    _pendingUnloadTimestamps = new Dictionary<SceneID, long>();
                if (_pendingUnloadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingUnloadTimestamps[sceneID] = Stopwatch.GetTimestamp();
            }
            _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            RecordPendingSceneOperations();
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;
            string sceneName = state.scene.name;
            Debug.Log($"Unloaded chunk {chunkCoord} (no remaining subscribers).");
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
