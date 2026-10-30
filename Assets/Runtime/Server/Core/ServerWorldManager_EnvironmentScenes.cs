using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
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
        private readonly Dictionary<WorldChunkKey, List<Action<bool, SceneID>>> _loadAfterUnloadCallbacks = new();
        private readonly Dictionary<SceneID, WorldChunkKey> _pendingSceneOwners = new();
        private readonly Dictionary<SceneID, WorldChunkKey> _pendingUnloadOwners = new();
        private readonly Dictionary<WorldChunkKey, Action> _pendingLoadCleanup = new();
        private readonly HashSet<SceneID> _scenesAwaitingUnload = new();
        private readonly List<SceneID> _sceneUnloadRetryBuffer = new();
        private Dictionary<WorldChunkKey, long> _pendingLoadTimestamps;
        private Dictionary<SceneID, long> _pendingUnloadTimestamps;

        private void LateUpdate()
        {
            // PurrNet registers completed Addressables scene handles after its scene callbacks.
            // Defer failure cleanup until LateUpdate so UnloadSceneAsync uses that owned handle.
            if (_scenesAwaitingUnload.Count == 0)
                return;

            _sceneUnloadRetryBuffer.Clear();
            try
            {
                _sceneUnloadRetryBuffer.AddRange(_scenesAwaitingUnload);
                foreach (SceneID sceneID in _sceneUnloadRetryBuffer)
                {
                    if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out _))
                        continue;

                    try
                    {
                        _networkManager.sceneModule.UnloadSceneAsync(sceneID);
                        _scenesAwaitingUnload.Remove(sceneID);
                    }
                    catch (Exception exception)
                    {
                        Debug.Error(
                            $"Failed to unload cleanup scene {sceneID}: {exception.Message}\n{exception.StackTrace}");
                    }
                }
            }
            finally
            {
                _sceneUnloadRetryBuffer.Clear();
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

        internal bool IsChunkSceneAddressable(Vector2Int chunkCoord)
        {
            string address = $"World_{chunkCoord.x}_{chunkCoord.y}";
            foreach (IResourceLocator locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(
                        address,
                        typeof(SceneInstance),
                        out IList<IResourceLocation> locations)
                    && locations != null
                    && locations.Count > 0)
                {
                    return true;
                }
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
            var chunkKey = new WorldChunkKey(context, chunkCoord);
            if (IsChunkUnloading(chunkKey))
            {
                if (!_loadAfterUnloadCallbacks.TryGetValue(chunkKey, out var deferredLoadCallbacks))
                {
                    deferredLoadCallbacks = new List<Action<bool, SceneID>>();
                    _loadAfterUnloadCallbacks.Add(chunkKey, deferredLoadCallbacks);
                }

                deferredLoadCallbacks.Add(onCompleted);
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

            if (!IsChunkSceneAddressable(chunkCoord))
            {
                onCompleted?.Invoke(false, default);
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
                    $"Chunk load threw before PurrNet could assign a scene ID for {chunkKey}: {exception.Message}");
                throw;
            }

            if (!loadHandle.IsValid())
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    default,
                    false,
                    $"Chunk load returned an invalid Addressables handle for {chunkKey}.");
                throw new InvalidOperationException($"PurrNet returned an invalid load handle for world scene '{address}'.");
            }

            SceneID requestedSceneID = _networkManager.sceneModule.lastSceneId;
            if (requestedSceneID == default)
            {
                CompletePendingChunkLoad(
                    chunkKey,
                    default,
                    false,
                    $"PurrNet did not assign a scene ID while loading chunk {chunkKey}.");
                throw new InvalidOperationException($"PurrNet did not assign a SceneID for world scene '{address}'.");
            }
            if (!_pendingSceneOwners.TryAdd(requestedSceneID, chunkKey))
            {
                string errorMessage = $"PurrNet reused SceneID {requestedSceneID} for world scene '{address}'.";
                CompletePendingChunkLoad(chunkKey, requestedSceneID, false, errorMessage);
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
                    $"Addressables failed to load chunk {chunkKey}: {exceptionMessage}");
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
                    $"Chunk load event registration failed for {chunkKey}: {exception.Message}");
                throw;
            }
        }

        private void CompletePendingChunkLoad(
            WorldChunkKey chunkKey,
            SceneID sceneID,
            bool succeeded,
            string failureMessage)
        {
            if (_pendingLoadCleanup.Remove(chunkKey, out var registeredCleanup))
                registeredCleanup?.Invoke();

            if (sceneID != default
                && _pendingSceneOwners.TryGetValue(sceneID, out WorldChunkKey owner)
                && owner == chunkKey)
            {
                _pendingSceneOwners.Remove(sceneID);
            }

            if (!succeeded)
                Debug.Error(failureMessage);

            if (!_pendingLoads.TryGetValue(chunkKey, out var callbacks))
            {
                if (_pendingLoadTimestamps?.Remove(chunkKey) == true)
                    RecordPendingSceneOperations();
                return;
            }

            _pendingLoads.Remove(chunkKey);
            _pendingLoadTimestamps?.Remove(chunkKey);
            RecordPendingSceneOperations();

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
            if (_pendingUnloadOwners.ContainsKey(sceneID))
            {
                throw new InvalidOperationException(
                    $"World chunk {chunkKey} already has a pending unload for SceneID {sceneID}.");
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
                if (_loadAfterUnloadCallbacks.Remove(chunkKey, out var deferredCallbacks))
                {
                    foreach (var callback in deferredCallbacks)
                    {
                        try
                        {
                            callback?.Invoke(true, sceneID);
                        }
                        catch (Exception callbackException)
                        {
                            Debug.Error(
                                $"Deferred chunk-load callback failed during unload rollback for {chunkKey}: "
                                + $"{callbackException.Message}\n{callbackException.StackTrace}");
                        }
                    }
                }

                throw;
            }
            RecordPendingSceneOperations();
        }

        private bool IsChunkUnloading(WorldChunkKey chunkKey)
        {
            foreach (WorldChunkKey pendingUnloadKey in _pendingUnloadOwners.Values)
            {
                if (pendingUnloadKey == chunkKey)
                    return true;
            }

            return false;
        }

        private void StartDeferredChunkLoadAfterUnload(WorldChunkKey chunkKey)
        {
            if (!_loadAfterUnloadCallbacks.Remove(chunkKey, out var callbacks)
                || callbacks.Count == 0)
                return;

            bool callbacksDispatched = false;
            void DispatchCallbacks(bool succeeded, SceneID sceneID)
            {
                if (callbacksDispatched)
                    return;

                callbacksDispatched = true;
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(succeeded, sceneID);
                    }
                    catch (Exception callbackException)
                    {
                        Debug.Error(
                            $"Deferred chunk-load callback failed for {chunkKey}: "
                            + $"{callbackException.Message}\n{callbackException.StackTrace}");
                    }
                }
            }

            try
            {
                EnsureChunkLoaded(
                    chunkKey.Context,
                    chunkKey.ChunkCoord,
                    DispatchCallbacks);
            }
            catch (Exception startException)
            {
                DispatchCallbacks(false, default);
                Debug.Error(
                    $"Deferred chunk load could not start for {chunkKey}: "
                    + $"{startException.Message}\n{startException.StackTrace}");
            }
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
            _scenesAwaitingUnload.Add(sceneID);
        }
    }
}
