using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.AI.Navigation;
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
        private readonly Dictionary<WorldChunkKey, ServerPhysicsFrame> _pendingFrames = new();
        private readonly Dictionary<SceneID, ServerPhysicsFrame> _sceneFrames = new();
        private readonly Dictionary<WorldChunkKey, Action> _pendingLoadCleanup = new();
        private readonly HashSet<SceneID> _scenesAwaitingUnload = new();
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
            _networkManager.sceneModule.onPreSceneLoaded += OnChunkScenePreLoaded;
            _networkManager.sceneModule.onSceneUnloaded += OnChunkSceneUnloaded;

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _networkManager.sceneModule != null)
            {
                _networkManager.sceneModule.onSceneLoaded -= OnChunkSceneLoaded;
                _networkManager.sceneModule.onPreSceneLoaded -= OnChunkScenePreLoaded;
                _networkManager.sceneModule.onSceneUnloaded -= OnChunkSceneUnloaded;
            }

            foreach (var sceneFrame in _sceneFrames)
            {
                if (_networkManager != null
                    && _networkManager.sceneModule != null
                    && _networkManager.sceneModule.TryGetSceneState(sceneFrame.Key, out var state))
                    DetachSceneFromFrame(state.scene);
                RetireFrame(sceneFrame.Value.Context, sceneFrame.Value.TileCoordinate, sceneFrame.Value);
            }
            _sceneFrames.Clear();

            foreach (var pendingFrame in _pendingFrames)
                RetireFrame(pendingFrame.Value.Context, pendingFrame.Value.TileCoordinate, pendingFrame.Value);
            _pendingFrames.Clear();

            foreach (var cleanup in _pendingLoadCleanup.Values)
                cleanup?.Invoke();
            _pendingLoadCleanup.Clear();
            _scenesAwaitingUnload.Clear();

            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
                ClearRuntimeContext();
            }
        }

        private void LateUpdate()
        {
            // PurrNet registers completed Addressables scene handles after its scene callbacks.
            // Defer failure cleanup until LateUpdate so UnloadSceneAsync uses that owned handle.
            if (_scenesAwaitingUnload.Count == 0
                || _networkManager == null
                || _networkManager.sceneModule == null)
                return;

            foreach (SceneID sceneID in _scenesAwaitingUnload)
            {
                if (_networkManager.sceneModule.TryGetSceneState(sceneID, out _))
                    _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            }

            _scenesAwaitingUnload.Clear();
        }

        private void OnChunkScenePreLoaded(SceneID sceneID, bool asServer)
        {
            // The installed PurrNet callback is post-activation. It is intentionally a
            // fail-closed boundary until an owner-approved pre-activation seam exists.
            if (!asServer || !_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
                return;

            string sceneName = state.scene.name;
            if (sceneName == "global_actors" || !sceneName.StartsWith("World"))
                return;

            if (!_pendingSceneOwners.TryGetValue(sceneID, out var chunkKey)
                || !_pendingLoads.ContainsKey(chunkKey)
                || !_pendingFrames.TryGetValue(chunkKey, out var frame))
            {
                FailUnownedWorldScene(
                    sceneID,
                    state.scene,
                    $"World scene '{sceneName}' loaded with SceneID {sceneID} without a pending physics frame.");
                return;
            }

            FailLoadedChunk(
                sceneID,
                chunkKey,
                frame,
                $"World scene '{sceneName}' cannot be placed safely: the installed PurrNet Addressables path activates the scene before onPreSceneLoaded. "
                + "Phase B requires an owner-approved pre-activation or physics-isolation integration before this scene can become physics-visible.");
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

                if (_pendingSceneOwners.TryGetValue(sceneID, out var chunkKey)
                    && _pendingLoads.ContainsKey(chunkKey)
                    && _pendingFrames.TryGetValue(chunkKey, out var frame))
                {
                    FailLoadedChunk(
                        sceneID,
                        chunkKey,
                        frame,
                        $"World scene '{sceneName}' loaded without verified physics placement. "
                        + "The installed PurrNet Addressables path does not provide a pre-activation placement seam.");
                    return;
                }
                FailUnownedWorldScene(
                    sceneID,
                    state.scene,
                    $"World scene '{sceneName}' loaded with SceneID {sceneID} without a pending context owner.");
            }
        }

        private void OnChunkSceneUnloaded(SceneID sceneID, bool asServer)
        {
            if (!asServer)
                return;

            if (_sceneFrames.TryGetValue(sceneID, out var frame))
            {
                _sceneFrames.Remove(sceneID);
                if (_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
                    DetachSceneFromFrame(state.scene);
                RetireFrame(frame.Context, frame.TileCoordinate, frame);
            }

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
                _loadedChunks.Remove(unloadedKey);

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

            ServerPositionManager positionManager = ServerPositionManager.Instance;
            if (positionManager == null || !ServerPositionManager.IsInitialized)
                throw new InvalidOperationException("ServerPositionManager must be initialized before loading world scenes.");

            ServerPhysicsFrame frame = positionManager.ReserveFrame(context, chunkCoord);
            positionManager.AcquireFrameReference(frame);
            _pendingFrames.Add(chunkKey, frame);

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
                RetirePendingFrame(chunkKey, frame);
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
                RetirePendingFrame(chunkKey, frame);
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
                RetirePendingFrame(chunkKey, frame);
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
                RetirePendingFrame(chunkKey, frame);
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
                RetirePendingFrame(chunkKey, frame);
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

            _pendingLoadCleanup[chunkKey] = UnsubscribeLoadHandlers;
            try
            {
                _networkManager.sceneModule.onSceneLoaded += OnLoaded;
                loadHandle.Completed += OnAddressableLoadCompleted;
            }
            catch (Exception exception)
            {
                RetirePendingFrame(chunkKey, frame);
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
            if (_pendingLoadCleanup.Remove(chunkKey, out var registeredCleanup))
                registeredCleanup?.Invoke();
            else if (unsubscribe != null && _pendingLoads.ContainsKey(chunkKey))
                unsubscribe?.Invoke();

            if (sceneID != default
                && _pendingSceneOwners.TryGetValue(sceneID, out WorldChunkKey owner)
                && owner == chunkKey)
            {
                _pendingSceneOwners.Remove(sceneID);
            }

            if (!succeeded && _pendingFrames.TryGetValue(chunkKey, out var frame))
                RetirePendingFrame(chunkKey, frame);

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
            if (_sceneFrames.TryGetValue(sceneID, out var frame))
            {
                _sceneFrames.Remove(sceneID);
                if (_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
                    DetachSceneFromFrame(state.scene);
                RetireFrame(frame.Context, frame.TileCoordinate, frame);
            }
            if (PerformanceTelemetry.IsEnabled)
            {
                if (_pendingUnloadTimestamps == null)
                    _pendingUnloadTimestamps = new Dictionary<SceneID, long>();
                if (_pendingUnloadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingUnloadTimestamps[sceneID] = Stopwatch.GetTimestamp();
            }
            _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            RecordPendingSceneOperations();
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var sceneState)) return;
            string sceneName = sceneState.scene.name;
            Debug.Log($"Unloaded chunk {chunkCoord} (no remaining subscribers).");
        }

        private void BindSceneToFrame(Scene scene, ServerPhysicsFrame frame)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
                throw new InvalidOperationException("World scene contains no root GameObjects.");

            var surfaces = new List<NavMeshSurface>();
            var colliders = new List<Collider>();
            Bounds authoredBounds = default;
            bool hasBounds = false;

            foreach (var root in roots)
            {
                foreach (var surface in root.GetComponentsInChildren<NavMeshSurface>(true))
                {
                    if (surface.isActiveAndEnabled)
                        surfaces.Add(surface);
                }
                colliders.AddRange(root.GetComponentsInChildren<Collider>(true));
            }

            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;
                if (!hasBounds)
                {
                    authoredBounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    authoredBounds.Encapsulate(collider.bounds);
                }
            }

            foreach (var surface in surfaces)
            {
                if (surface.navMeshData == null)
                {
                    throw new InvalidOperationException(
                        $"Active NavMeshSurface '{surface.name}' has no baked NavMeshData.");
                }

                EncapsulateAuthoredBounds(
                    ref authoredBounds,
                    ref hasBounds,
                    surface.transform,
                    surface.navMeshData.sourceBounds);
            }

            if (!hasBounds)
                throw new InvalidOperationException("World scene contains no enabled collider or NavMeshData geometry.");

            ValidateAuthoredBounds(frame, authoredBounds);

            foreach (var surface in surfaces)
                surface.RemoveData();

            foreach (var root in roots)
                root.transform.position = ToFrameLocalRootPosition(root.transform.position, frame);

            foreach (var surface in surfaces)
                surface.AddData();

            foreach (var surface in surfaces)
            {
                if (!NavMeshSurface.activeSurfaces.Contains(surface))
                {
                    throw new InvalidOperationException(
                        $"Active NavMeshSurface '{surface.name}' was not registered after placement.");
                }
            }
        }

        private void DetachSceneFromFrame(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var surface in root.GetComponentsInChildren<NavMeshSurface>(true))
                    surface.RemoveData();
                root.SetActive(false);
            }
        }

        private void ValidateAuthoredBounds(ServerPhysicsFrame frame, Bounds authoredBounds)
        {
            Vector3 localMin = ToTileLocalPoint(authoredBounds.min, frame);
            Vector3 localMax = ToTileLocalPoint(authoredBounds.max, frame);
            ServerPositionManager.Instance.ValidateEnvironmentTileExtents(localMin, localMax);
        }

        private static void EncapsulateAuthoredBounds(
            ref Bounds bounds,
            ref bool hasBounds,
            Transform surfaceTransform,
            Bounds sourceBounds)
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? sourceBounds.min.x : sourceBounds.max.x,
                            y == 0 ? sourceBounds.min.y : sourceBounds.max.y,
                            z == 0 ? sourceBounds.min.z : sourceBounds.max.z);
                        Vector3 authoredPoint = surfaceTransform.TransformPoint(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(authoredPoint, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(authoredPoint);
                        }
                    }
                }
            }
        }

        private static Vector3 ToFrameLocalRootPosition(Vector3 authoredPosition, ServerPhysicsFrame frame)
        {
            double localX = (double)frame.SlotAnchor.x + authoredPosition.x - frame.TileOrigin.x;
            double localY = (double)frame.SlotAnchor.y + authoredPosition.y - frame.TileOrigin.y;
            double localZ = (double)frame.SlotAnchor.z + authoredPosition.z - frame.TileOrigin.z;
            if (localX < -float.MaxValue || localX > float.MaxValue
                || localY < -float.MaxValue || localY > float.MaxValue
                || localZ < -float.MaxValue || localZ > float.MaxValue)
            {
                throw new InvalidOperationException(
                    $"World scene root cannot be represented in physics frame {frame.SlotIndex}.");
            }

            return new Vector3((float)localX, (float)localY, (float)localZ);
        }

        private static Vector3 ToTileLocalPoint(Vector3 authoredPosition, ServerPhysicsFrame frame)
        {
            double localX = (double)authoredPosition.x - frame.TileOrigin.x;
            double localY = (double)authoredPosition.y - frame.TileOrigin.y;
            double localZ = (double)authoredPosition.z - frame.TileOrigin.z;
            if (double.IsNaN(localX) || double.IsInfinity(localX)
                || double.IsNaN(localY) || double.IsInfinity(localY)
                || double.IsNaN(localZ) || double.IsInfinity(localZ)
                || localX < -float.MaxValue || localX > float.MaxValue
                || localY < -float.MaxValue || localY > float.MaxValue
                || localZ < -float.MaxValue || localZ > float.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Authored bounds cannot be represented in tile {frame.TileCoordinate}.");
            }

            return new Vector3((float)localX, (float)localY, (float)localZ);
        }

        private void FailLoadedChunk(
            SceneID sceneID,
            WorldChunkKey chunkKey,
            ServerPhysicsFrame frame,
            string failureMessage)
        {
            if (_scenesAwaitingUnload.Contains(sceneID))
                return;

            _sceneFrames.Remove(sceneID);
            if (_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
                DetachSceneFromFrame(state.scene);
            _pendingFrames.Remove(chunkKey);
            RetireFrame(frame.Context, frame.TileCoordinate, frame);
            CompletePendingChunkLoad(chunkKey, sceneID, false, failureMessage, null);
            QueueSceneUnload(sceneID);
        }

        private void FailUnownedWorldScene(SceneID sceneID, Scene scene, string failureMessage)
        {
            if (_scenesAwaitingUnload.Contains(sceneID))
                return;

            Debug.Error(failureMessage);
            DetachSceneFromFrame(scene);
            QueueSceneUnload(sceneID);
        }

        private void QueueSceneUnload(SceneID sceneID)
        {
            _scenesAwaitingUnload.Add(sceneID);
        }

        private void RetirePendingFrame(WorldChunkKey chunkKey, ServerPhysicsFrame frame)
        {
            if (!_pendingFrames.Remove(chunkKey))
                return;

            RetireFrame(frame.Context, frame.TileCoordinate, frame);
        }

        private void RetireFrame(ActorSpatialContext context, Vector2Int chunkCoord, ServerPhysicsFrame frame)
        {
            ServerPositionManager positionManager = ServerPositionManager.Instance;
            if (positionManager == null || !ServerPositionManager.IsInitialized)
                return;

            try
            {
                positionManager.BeginFrameRetirement(frame);
                if (positionManager.GetFrameReferenceCount(frame) > 0)
                    positionManager.ReleaseFrameReference(frame);
                positionManager.ReleaseFrame(frame);
            }
            catch (InvalidOperationException exception)
            {
                Debug.Error(
                    $"Could not retire physics frame for context '{context.MapID}' tile {chunkCoord}: {exception.Message}");
            }
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
