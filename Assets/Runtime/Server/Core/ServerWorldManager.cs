using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    /// <summary>Identifies an environment tile within an authoritative runtime context.</summary>
    public readonly struct WorldChunkKey : IEquatable<WorldChunkKey>
    {
        /// <summary>Gets the authoritative world, map and runtime instance context.</summary>
        public ActorSpatialContext Context { get; }
        /// <summary>Gets the environment tile coordinate.</summary>
        public Vector2Int ChunkCoord { get; }

        /// <summary>Creates a context-qualified environment tile key.</summary>
        /// <param name="context">The authoritative runtime context.</param>
        /// <param name="chunkCoord">The environment tile coordinate.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        public WorldChunkKey(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));

            Context = context;
            ChunkCoord = chunkCoord;
        }

        /// <inheritdoc />
        public bool Equals(WorldChunkKey other)
        {
            return Context == other.Context && ChunkCoord == other.ChunkCoord;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is WorldChunkKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Context.GetHashCode() * 397) ^ ChunkCoord.GetHashCode();
            }
        }

        /// <summary>Determines whether two keys identify the same context and tile.</summary>
        public static bool operator ==(WorldChunkKey left, WorldChunkKey right)
        {
            return left.Equals(right);
        }

        /// <summary>Determines whether two keys identify different contexts or tiles.</summary>
        public static bool operator !=(WorldChunkKey left, WorldChunkKey right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Server authority for world chunk scene loading and unloading.
    /// Does not manage player subscriptions — see ServerInterestManager.
    /// </summary>
    public sealed class ServerWorldManager : MonoBehaviour
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

        /// <summary>Initializes the authoritative public context for the current server lifetime.</summary>
        internal static void InitializeRuntimeContext(Guid worldID)
        {
            if (_runtimeContext.IsValid)
                throw new InvalidOperationException("The server world runtime context is already initialized.");

            _runtimeContext = ActorSpatialContext.CreatePublic(worldID, MAIN_MAP_ID);
        }

        /// <summary>Clears the authoritative public context during server teardown.</summary>
        internal static void ClearRuntimeContext()
        {
            _runtimeContext = default;
        }

        private NetworkManager _networkManager;
        private PurrSceneSettings _worldSceneSettings;

        // Context-qualified chunk key → PurrNet SceneID.
        private readonly Dictionary<WorldChunkKey, SceneID> _loadedChunks = new();
        /// <summary>Gets the currently loaded world chunks.</summary>
        public IReadOnlyDictionary<WorldChunkKey, SceneID> LoadedChunks => _loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<WorldChunkKey, List<Action<bool, SceneID>>> _pendingLoads = new();
        private readonly Dictionary<SceneID, WorldChunkKey> _pendingSceneOwners = new();
        private Dictionary<WorldChunkKey, long> _pendingLoadTimestamps;
        private Dictionary<SceneID, long> _pendingUnloadTimestamps;
        private long _globalActorsLoadTimestamp;

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneID;
        /// <summary>Gets the networked scene containing global actors.</summary>
        public SceneID GlobalActorsSceneID => _globalActorsSceneID;
        /// <summary>Gets whether the global actor scene has loaded.</summary>
        public bool IsGlobalActorsSceneLoaded => _globalActorsSceneID != default;
        /// <summary>Gets the authoritative public context for the current server lifetime.</summary>
        public ActorSpatialContext CurrentRuntimeContext => _runtimeContext;

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
                if (!_pendingSceneOwners.TryGetValue(sceneID, out var chunkKey))
                {
                    Debug.Error($"World scene '{sceneName}' loaded with SceneID {sceneID} but has no pending context owner.");
                    return;
                }
                if (!_pendingLoads.ContainsKey(chunkKey))
                {
                    Debug.Error($"World scene '{sceneName}' loaded for context key {chunkKey.ChunkCoord}, but its load is no longer pending.");
                    return;
                }

                Vector2Int chunkCoord;
                try
                {
                    chunkCoord = ParseChunkCoordinate(sceneName);
                }
                catch (Exception exception)
                {
                    Debug.Error($"World scene '{sceneName}' could not be parsed for pending SceneID {sceneID}: {exception.Message}");
                    return;
                }
                if (chunkCoord != chunkKey.ChunkCoord)
                {
                    Debug.Error($"World scene '{sceneName}' does not match requested tile {chunkKey.ChunkCoord}.");
                    return;
                }
                if (_pendingLoadTimestamps != null
                    && _pendingLoadTimestamps.TryGetValue(chunkKey, out long startedTimestamp))
                {
                    _pendingLoadTimestamps.Remove(chunkKey);
                    RecordSceneDuration(SCENE_LOAD_DURATION_METRIC, startedTimestamp);
                }
                if (_loadedChunks.ContainsKey(chunkKey))
                {
                    Debug.Error($"World scene '{sceneName}' loaded twice for tile {chunkCoord} in the same context.");
                    return;
                }

                _loadedChunks.Add(chunkKey, sceneID);
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

        /// <summary>Determines whether a context-qualified chunk scene is loaded.</summary>
        /// <param name="context">The authoritative runtime context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool IsChunkLoaded(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            return _loadedChunks.ContainsKey(new WorldChunkKey(context, chunkCoord));
        }

        /// <summary>Determines whether a context-qualified chunk scene is currently loading.</summary>
        /// <param name="context">The authoritative runtime context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when a load is pending.</returns>
        public bool IsChunkLoadPending(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            return _pendingLoads.ContainsKey(new WorldChunkKey(context, chunkCoord));
        }

        /// <summary>Attempts to get the networked scene identifier for a loaded chunk.</summary>
        /// <param name="context">The authoritative runtime context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <param name="sceneID">Receives the scene identifier when found.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool TryGetChunkSceneID(ActorSpatialContext context, Vector2Int chunkCoord, out SceneID sceneID)
        {
            return _loadedChunks.TryGetValue(new WorldChunkKey(context, chunkCoord), out sceneID);
        }

        /// <summary>
        /// Ensures a chunk scene is loaded. If already loaded, invokes the callback immediately
        /// with the existing SceneID. Otherwise triggers an async load and invokes the callback
        /// once the scene succeeds or reaches a terminal failure.
        /// </summary>
        /// <param name="context">The authoritative runtime context that owns the chunk.</param>
        /// <param name="chunkCoord">The environment tile coordinate.</param>
        /// <param name="onCompleted">Receives the terminal result and verified scene identifier on success.</param>
        public void EnsureChunkLoaded(ActorSpatialContext context, Vector2Int chunkCoord, Action<bool, SceneID> onCompleted)
        {
            var chunkKey = new WorldChunkKey(context, chunkCoord);
            if (_loadedChunks.TryGetValue(chunkKey, out SceneID sceneID))
            {
                onCompleted?.Invoke(true, sceneID);
                return;
            }

            // If a load is already in-flight, just queue the callback.
            if (_pendingLoads.TryGetValue(chunkKey, out var callbacks))
            {
                callbacks.Add(onCompleted);
                return;
            }

            _pendingLoads[chunkKey] = new() { onCompleted };
            if (PerformanceTelemetry.IsEnabled)
            {
                if (_pendingLoadTimestamps == null)
                    _pendingLoadTimestamps = new Dictionary<WorldChunkKey, long>();
                if (_pendingLoadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingLoadTimestamps[chunkKey] = Stopwatch.GetTimestamp();
                RecordPendingSceneOperations();
            }

            var address = $"World_{chunkCoord.x}_{chunkCoord.y}";
            AsyncOperationHandle<SceneInstance> loadHandle;
            try
            {
                loadHandle = _networkManager.sceneModule.LoadAddressableSceneAsync(address, _worldSceneSettings);
            }
            catch (Exception exception)
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    default,
                    false,
                    $"Chunk load threw before PurrNet could assign a scene ID for {chunkKey}: {exception.Message}",
                    null);
                throw;
            }

            if (!loadHandle.IsValid())
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    default,
                    false,
                    $"Chunk load returned an invalid Addressables handle for {chunkKey}.",
                    null);
                throw new InvalidOperationException($"PurrNet returned an invalid load handle for world scene '{address}'.");
            }

            SceneID requestedSceneID = _networkManager.sceneModule.lastSceneId;
            if (requestedSceneID == default)
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    default,
                    false,
                    $"PurrNet did not assign a scene ID while loading chunk {chunkKey}.",
                    null);
                throw new InvalidOperationException($"PurrNet did not assign a SceneID for world scene '{address}'.");
            }
            if (!_pendingSceneOwners.TryAdd(requestedSceneID, chunkKey))
            {
                string errorMessage = $"PurrNet reused SceneID {requestedSceneID} for world scene '{address}'.";
                CompletePendingChunkLoad(chunkKey, requestedSceneID, false, errorMessage, null);
                throw new InvalidOperationException(errorMessage);
            }

            void UnsubscribeLoadHandlers()
            {
                _networkManager.sceneModule.onSceneLoaded -= OnLoaded;
                loadHandle.Completed -= OnAddressableLoadCompleted;
            }

            void OnAddressableLoadCompleted(AsyncOperationHandle<SceneInstance> completedHandle)
            {
                if (completedHandle.Status == AsyncOperationStatus.Succeeded)
                    return;

                string exceptionMessage = completedHandle.OperationException?.Message ?? "no exception details were provided";
                CompletePendingChunkLoad(
                    chunkKey,
                    requestedSceneID,
                    false,
                    $"Addressables failed to load chunk {chunkKey}: {exceptionMessage}",
                    UnsubscribeLoadHandlers);
            }

            void OnLoaded(SceneID loadedSceneID, bool asServer)
            {
                if (!asServer || loadedSceneID != requestedSceneID)
                    return;
                if (!_pendingSceneOwners.TryGetValue(loadedSceneID, out var loadedKey)
                    || loadedKey != chunkKey)
                    return;

                if (!_loadedChunks.TryGetValue(chunkKey, out var chunkSceneID)
                    || chunkSceneID != loadedSceneID)
                {
                    CompletePendingChunkLoad(
                        chunkKey,
                        requestedSceneID,
                        false,
                        $"PurrNet reported scene {loadedSceneID} for chunk {chunkKey}, but the verified loaded-chunk mapping was not created.",
                        UnsubscribeLoadHandlers);
                    return;
                }

                CompletePendingChunkLoad(chunkKey, requestedSceneID, true, null, UnsubscribeLoadHandlers);
            }

            try
            {
                _networkManager.sceneModule.onSceneLoaded += OnLoaded;
                loadHandle.Completed += OnAddressableLoadCompleted;
            }
            catch (Exception exception)
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    requestedSceneID,
                    false,
                    $"Chunk load event registration failed for {chunkKey}: {exception.Message}",
                    UnsubscribeLoadHandlers);
                throw;
            }
        }

        private void CompletePendingChunkLoad(
            WorldChunkKey chunkKey,
            SceneID sceneID,
            bool succeeded,
            string failureMessage,
            Action unsubscribe)
        {
            unsubscribe?.Invoke();

            if (sceneID != default
                && _pendingSceneOwners.TryGetValue(sceneID, out WorldChunkKey owner)
                && owner == chunkKey)
            {
                _pendingSceneOwners.Remove(sceneID);
            }

            if (!_pendingLoads.TryGetValue(chunkKey, out var callbacks))
            {
                if (_pendingLoadTimestamps?.Remove(chunkKey) == true)
                    RecordPendingSceneOperations();
                return;
            }

            _pendingLoads.Remove(chunkKey);
            _pendingLoadTimestamps?.Remove(chunkKey);
            RecordPendingSceneOperations();

            if (!succeeded)
                Debug.Error(failureMessage);

            SceneID callbackSceneID = succeeded ? sceneID : default;
            foreach (var callback in callbacks)
                callback?.Invoke(succeeded, callbackSceneID);
        }

        /// <summary>
        /// Unloads a chunk scene. Should only be called when no players remain subscribed.
        /// </summary>
        /// <param name="context">The authoritative runtime context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        public void UnloadChunk(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            var chunkKey = new WorldChunkKey(context, chunkCoord);
            if (!_loadedChunks.TryGetValue(chunkKey, out SceneID sceneID))
                return;

            _loadedChunks.Remove(chunkKey);
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
