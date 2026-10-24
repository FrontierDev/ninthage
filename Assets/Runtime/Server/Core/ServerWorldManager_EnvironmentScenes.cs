using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Game.Server.Core
{
    public sealed partial class ServerWorldManager
    {
        // Context-qualified chunk key → PurrNet SceneID.
        private readonly Dictionary<WorldChunkKey, SceneID> _loadedChunks = new();
        /// <summary>Gets the currently loaded world chunks.</summary>
        public IReadOnlyDictionary<WorldChunkKey, SceneID> LoadedChunks => _loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<WorldChunkKey, List<Action<bool, SceneID>>> _pendingLoads = new();
        private readonly Dictionary<WorldChunkKey, int> _chunkRetainCounts = new();
        private readonly Dictionary<WorldChunkKey, List<Action<bool, SceneID>>> _reloadAfterUnloadCallbacks = new();
        private readonly Dictionary<SceneID, WorldChunkKey> _pendingSceneOwners = new();
        private readonly Dictionary<SceneID, WorldChunkKey> _pendingUnloadOwners = new();
        private readonly Dictionary<WorldChunkKey, Action> _pendingLoadCleanup = new();
        private readonly HashSet<SceneID> _scenesAwaitingUnload = new();
        // Cleanup-only unload requests accepted by PurrNet and awaiting terminal unload notification.
        private readonly HashSet<SceneID> _cleanupOnlyUnloads = new();
        private Dictionary<WorldChunkKey, long> _pendingLoadTimestamps;
        private Dictionary<SceneID, long> _pendingUnloadTimestamps;

        private void LateUpdate()
        {
            if (_tearingDown)
                return;

            // PurrNet registers completed Addressables scene handles after its scene callbacks.
            // Defer failure cleanup until LateUpdate so UnloadSceneAsync uses that owned handle.
            if (_scenesAwaitingUnload.Count == 0
                || _networkManager == null
                || _networkManager.sceneModule == null)
                return;

            var scenesToUnload = new List<SceneID>(_scenesAwaitingUnload);
            foreach (SceneID sceneID in scenesToUnload)
            {
                if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out _))
                    continue;

                _cleanupOnlyUnloads.Add(sceneID);
                try
                {
                    _networkManager.sceneModule.UnloadSceneAsync(sceneID);
                }
                catch (Exception exception)
                {
                    _cleanupOnlyUnloads.Remove(sceneID);
                    RecordPendingSceneOperations();
                    Debug.Error($"Failed to unload rejected world scene {sceneID}: {exception}");
                    continue;
                }

                if (!_scenesAwaitingUnload.Contains(sceneID))
                    continue;

                if (_pendingUnloadTimestamps == null && PerformanceTelemetry.IsEnabled)
                    _pendingUnloadTimestamps = new Dictionary<SceneID, long>();
                if (PerformanceTelemetry.IsEnabled
                    && _pendingUnloadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingUnloadTimestamps[sceneID] = Stopwatch.GetTimestamp();
                _scenesAwaitingUnload.Remove(sceneID);
                RecordPendingSceneOperations();
            }
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

        internal void RetainChunk(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            if (_tearingDown)
                throw new InvalidOperationException("Cannot retain a world chunk while the world manager is tearing down.");

            var chunkKey = new WorldChunkKey(context, chunkCoord);
            int retainCount = _chunkRetainCounts.TryGetValue(chunkKey, out int currentCount)
                ? checked(currentCount + 1)
                : 1;
            _chunkRetainCounts[chunkKey] = retainCount;
        }

        internal void ReleaseChunk(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            if (_tearingDown)
                return;

            var chunkKey = new WorldChunkKey(context, chunkCoord);
            if (!_chunkRetainCounts.TryGetValue(chunkKey, out int retainCount) || retainCount <= 0)
                throw new InvalidOperationException($"World chunk {chunkKey} has no retain to release.");

            if (retainCount > 1)
            {
                _chunkRetainCounts[chunkKey] = retainCount - 1;
                return;
            }

            _chunkRetainCounts.Remove(chunkKey);
            if (_pendingLoads.ContainsKey(chunkKey) || IsChunkUnloading(chunkKey))
                return;

            if (_loadedChunks.ContainsKey(chunkKey))
                UnloadChunk(chunkKey);
        }

        private bool HasPositiveChunkRetain(WorldChunkKey chunkKey)
        {
            return _chunkRetainCounts.TryGetValue(chunkKey, out int retainCount)
                && retainCount > 0;
        }

        private bool IsChunkUnloading(WorldChunkKey chunkKey)
        {
            foreach (KeyValuePair<SceneID, WorldChunkKey> unloading in _pendingUnloadOwners)
            {
                if (unloading.Value == chunkKey)
                    return true;
            }

            return false;
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
            if (_tearingDown)
                throw new InvalidOperationException("Cannot load a world chunk while the world manager is tearing down.");

            var chunkKey = new WorldChunkKey(context, chunkCoord);
            if (!HasPositiveChunkRetain(chunkKey))
                throw new InvalidOperationException($"World chunk {chunkKey} cannot be loaded without a positive retain.");

            if (IsChunkUnloading(chunkKey))
            {
                if (!_reloadAfterUnloadCallbacks.TryGetValue(
                        chunkKey,
                        out List<Action<bool, SceneID>> reloadCallbacks))
                {
                    reloadCallbacks = new List<Action<bool, SceneID>>();
                    _reloadAfterUnloadCallbacks.Add(chunkKey, reloadCallbacks);
                }
                reloadCallbacks.Add(onCompleted);
                return;
            }

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

            _pendingLoadCleanup[chunkKey] = UnsubscribeLoadHandlers;
            try
            {
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
            if (_pendingLoadCleanup.Remove(chunkKey, out var registeredCleanup))
            {
                try
                {
                    registeredCleanup?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.Error($"Load-handler cleanup for world chunk {chunkKey} threw: {exception}");
                }
            }
            else if (unsubscribe != null && _pendingLoads.ContainsKey(chunkKey))
            {
                try
                {
                    unsubscribe?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.Error($"Load-handler cleanup for world chunk {chunkKey} threw: {exception}");
                }
            }

            if (sceneID != default
                && _pendingSceneOwners.TryGetValue(sceneID, out WorldChunkKey owner)
                && owner == chunkKey)
            {
                _pendingSceneOwners.Remove(sceneID);
            }

            if (!_pendingLoads.Remove(chunkKey, out List<Action<bool, SceneID>> callbacks))
            {
                if (_pendingLoadTimestamps?.Remove(chunkKey) == true)
                    RecordPendingSceneOperations();
                return;
            }

            _pendingLoadTimestamps?.Remove(chunkKey);
            RecordPendingSceneOperations();

            if (!succeeded)
                Debug.Error(failureMessage);

            SceneID callbackSceneID = succeeded ? sceneID : default;
            foreach (var callback in callbacks)
            {
                try
                {
                    callback?.Invoke(succeeded, callbackSceneID);
                }
                catch (Exception exception)
                {
                    Debug.Error($"Completion callback for world chunk {chunkKey} threw: {exception}");
                }
            }

            if (succeeded && !HasPositiveChunkRetain(chunkKey) && _loadedChunks.ContainsKey(chunkKey))
                UnloadChunk(chunkKey);
        }

        private void HandleChunkUnloadCompleted(WorldChunkKey chunkKey)
        {
            _reloadAfterUnloadCallbacks.Remove(
                chunkKey,
                out List<Action<bool, SceneID>> reloadCallbacks);

            bool completionDelivered = false;
            void CompleteReload(bool succeeded, SceneID sceneID)
            {
                if (completionDelivered)
                    return;

                completionDelivered = true;
                if (reloadCallbacks == null)
                    return;

                foreach (Action<bool, SceneID> callback in reloadCallbacks)
                {
                    try
                    {
                        callback?.Invoke(succeeded, succeeded ? sceneID : default);
                    }
                    catch (Exception exception)
                    {
                        Debug.Error($"Deferred reload callback for world chunk {chunkKey} threw: {exception}");
                    }
                }
            }

            if (!HasPositiveChunkRetain(chunkKey))
            {
                CompleteReload(false, default);
                return;
            }

            try
            {
                EnsureChunkLoaded(chunkKey.Context, chunkKey.ChunkCoord, CompleteReload);
            }
            catch (Exception exception)
            {
                CompleteReload(false, default);
                Debug.Error($"Failed to reload retained world chunk {chunkKey} after unload: {exception}");
            }
        }

        private void ClearEnvironmentSceneState()
        {
            var loadCleanups = new List<Action>(_pendingLoadCleanup.Values);
            _pendingLoadCleanup.Clear();
            foreach (Action cleanup in loadCleanups)
            {
                try
                {
                    cleanup?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.Error($"Environment load-handler cleanup during teardown threw: {exception}");
                }
            }

            var pendingCallbacks = new List<KeyValuePair<WorldChunkKey, Action<bool, SceneID>>>();
            foreach (KeyValuePair<WorldChunkKey, List<Action<bool, SceneID>>> pending in _pendingLoads)
            {
                foreach (Action<bool, SceneID> callback in pending.Value)
                    pendingCallbacks.Add(new KeyValuePair<WorldChunkKey, Action<bool, SceneID>>(pending.Key, callback));
            }

            _pendingLoads.Clear();
            _reloadAfterUnloadCallbacks.Clear();
            _chunkRetainCounts.Clear();
            _pendingSceneOwners.Clear();
            _pendingLoadCleanup.Clear();
            _pendingUnloadOwners.Clear();
            _scenesAwaitingUnload.Clear();
            _cleanupOnlyUnloads.Clear();
            _loadedChunks.Clear();
            _transferCollidersByChunk.Clear();
            _pendingLoadTimestamps?.Clear();
            _pendingUnloadTimestamps?.Clear();
            _globalActorsLoadTimestamp = 0;
            _globalActorsSceneID = default;

            foreach (KeyValuePair<WorldChunkKey, Action<bool, SceneID>> pending in pendingCallbacks)
            {
                try
                {
                    pending.Value?.Invoke(false, default);
                }
                catch (Exception exception)
                {
                    Debug.Error($"Teardown callback for world chunk {pending.Key} threw: {exception}");
                }
            }

            RecordPendingSceneOperations();
        }

        /// <summary>
        /// Unloads a chunk scene after its final environment retain is released.
        /// </summary>
        /// <param name="chunkKey">The context-qualified key of the chunk to unload.</param>
        private void UnloadChunk(WorldChunkKey chunkKey)
        {
            if (_tearingDown)
                throw new InvalidOperationException("Cannot unload a world chunk while the world manager is tearing down.");

            if (!_loadedChunks.TryGetValue(chunkKey, out SceneID sceneID))
                return;
            if (_pendingUnloadOwners.ContainsKey(sceneID))
            {
                throw new InvalidOperationException(
                    $"World chunk {chunkKey} already has a pending unload for SceneID {sceneID}.");
            }

            if (!ServerPhysicsManager.IsInitialized
                || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "World chunk unload requires an initialized server physics manager.");
            }

            ServerPhysicsManager.Instance.PrepareEnvironmentNavigationUnload(chunkKey);

            _loadedChunks.Remove(chunkKey);
            _pendingUnloadOwners.Add(sceneID, chunkKey);
            if (PerformanceTelemetry.IsEnabled)
            {
                if (_pendingUnloadTimestamps == null)
                    _pendingUnloadTimestamps = new Dictionary<SceneID, long>();
                if (_pendingUnloadTimestamps.Count < MAX_PENDING_TELEMETRY_OPERATIONS)
                    _pendingUnloadTimestamps[sceneID] = Stopwatch.GetTimestamp();
            }
            RecordPendingSceneOperations();
            try
            {
                _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            }
            catch (Exception unloadException)
            {
                _pendingUnloadOwners.Remove(sceneID);
                _loadedChunks.Add(chunkKey, sceneID);
                _pendingUnloadTimestamps?.Remove(sceneID);

                try
                {
                    ServerPhysicsManager.Instance.CancelEnvironmentNavigationUnload(chunkKey);
                }
                catch (Exception navigationRollbackException)
                {
                    RecordPendingSceneOperations();
                    throw new InvalidOperationException(
                        $"World chunk unload request for {chunkKey} failed and navigation rollback also failed.",
                        new AggregateException(unloadException, navigationRollbackException));
                }

                RecordPendingSceneOperations();
                throw;
            }
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var sceneState)) return;
            string sceneName = sceneState.scene.name;
            Debug.Log($"Unloaded chunk {chunkKey.ChunkCoord} (no remaining retains).");
        }

        private void FailUnownedWorldScene(SceneID sceneID, string failureMessage)
        {
            if (_scenesAwaitingUnload.Contains(sceneID))
                return;

            Debug.Error(failureMessage);
            QueueSceneUnload(sceneID);
        }

        private void QueueSceneUnload(SceneID sceneID)
        {
            if (_scenesAwaitingUnload.Add(sceneID))
                RecordPendingSceneOperations();
        }
    }
}
