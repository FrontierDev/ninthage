using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Game.Server.Core
{
    public sealed partial class ServerWorldManager
    {
        private readonly struct InstanceSceneKey : IEquatable<InstanceSceneKey>
        {
            internal ActorSpatialContext Context { get; }
            internal string Address { get; }

            internal InstanceSceneKey(ActorSpatialContext context, string address)
            {
                Context = context;
                Address = address;
            }

            public bool Equals(InstanceSceneKey other)
            {
                return Context == other.Context
                    && StringComparer.Ordinal.Equals(Address, other.Address);
            }

            public override bool Equals(object obj)
            {
                return obj is InstanceSceneKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Context.GetHashCode() * 397)
                        ^ (Address == null ? 0 : StringComparer.Ordinal.GetHashCode(Address));
                }
            }
        }

        private readonly Dictionary<InstanceSceneKey, SceneID> _loadedInstanceScenes = new();
        private readonly Dictionary<InstanceSceneKey, List<Action<bool, SceneID>>> _pendingInstanceSceneLoads = new();
        private readonly Dictionary<SceneID, InstanceSceneKey> _pendingInstanceSceneOwners = new();
        private readonly Dictionary<InstanceSceneKey, Action> _pendingInstanceSceneLoadCleanup = new();
        private readonly Dictionary<SceneID, InstanceSceneKey> _unloadingInstanceScenes = new();

        internal void EnsureInstanceSceneLoaded(
            ActorSpatialContext context,
            string address,
            Action<bool, SceneID> onCompleted)
        {
            if (!context.IsValid || !_transferRuntimes.ContainsKey(context))
                throw new ArgumentException("A registered transfer runtime context is required.", nameof(context));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("A non-empty instance scene address is required.", nameof(address));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            var key = new InstanceSceneKey(context, address);
            if (_loadedInstanceScenes.TryGetValue(key, out SceneID loadedSceneID))
            {
                onCompleted(true, loadedSceneID);
                return;
            }

            if (_pendingInstanceSceneLoads.TryGetValue(key, out List<Action<bool, SceneID>> callbacks))
            {
                callbacks.Add(onCompleted);
                return;
            }

            foreach (KeyValuePair<SceneID, InstanceSceneKey> unloading in _unloadingInstanceScenes)
            {
                if (!unloading.Value.Equals(key))
                    continue;

                onCompleted(false, default);
                Debug.Error($"[TransferInstanceScene] Cannot load '{address}' for {context} while its prior scene is unloading.");
                return;
            }

            _pendingInstanceSceneLoads.Add(key, new List<Action<bool, SceneID>> { onCompleted });
            AsyncOperationHandle<SceneInstance> loadHandle;
            try
            {
                loadHandle = _networkManager.sceneModule.LoadAddressableSceneAsync(address, _worldSceneSettings);
            }
            catch (Exception exception)
            {
                CompleteInstanceSceneLoad(key, default, false);
                Debug.Error($"[TransferInstanceScene] Load request for '{address}' in {context} threw: {exception.Message}");
                throw;
            }

            if (!loadHandle.IsValid())
            {
                CompleteInstanceSceneLoad(key, default, false);
                throw new InvalidOperationException(
                    $"PurrNet returned an invalid Addressables handle for transfer scene '{address}' in {context}.");
            }

            SceneID requestedSceneID = _networkManager.sceneModule.lastSceneId;
            if (requestedSceneID == default)
            {
                CompleteInstanceSceneLoad(key, default, false);
                throw new InvalidOperationException(
                    $"PurrNet did not assign a SceneID for transfer scene '{address}' in {context}.");
            }
            if (!_pendingInstanceSceneOwners.TryAdd(requestedSceneID, key))
            {
                CompleteInstanceSceneLoad(key, requestedSceneID, false);
                throw new InvalidOperationException(
                    $"PurrNet reused SceneID {requestedSceneID} for transfer scene '{address}' in {context}.");
            }

            void UnsubscribeLoadHandlers()
            {
                loadHandle.Completed -= OnAddressableLoadCompleted;
            }

            void OnAddressableLoadCompleted(AsyncOperationHandle<SceneInstance> completedHandle)
            {
                if (completedHandle.Status == AsyncOperationStatus.Succeeded)
                    return;

                CompleteInstanceSceneLoad(key, requestedSceneID, false);
                string reason = completedHandle.OperationException?.Message ?? "no exception details were provided";
                Debug.Error($"[TransferInstanceScene] Addressables failed to load '{address}' for {context}: {reason}");
                UnsubscribeLoadHandlers();
            }

            _pendingInstanceSceneLoadCleanup[key] = UnsubscribeLoadHandlers;
            try
            {
                loadHandle.Completed += OnAddressableLoadCompleted;
            }
            catch (Exception exception)
            {
                UnsubscribeLoadHandlers();
                CompleteInstanceSceneLoad(key, requestedSceneID, false);
                Debug.Error($"[TransferInstanceScene] Load event registration for '{address}' in {context} failed: {exception.Message}");
                throw;
            }
        }

        internal bool TryGetInstanceSceneID(
            ActorSpatialContext context,
            string address,
            out SceneID sceneID)
        {
            return _loadedInstanceScenes.TryGetValue(new InstanceSceneKey(context, address), out sceneID);
        }

        internal void UnloadInstanceScene(ActorSpatialContext context, string address)
        {
            var key = new InstanceSceneKey(context, address);
            if (!_loadedInstanceScenes.TryGetValue(key, out SceneID sceneID))
                return;
            if (_unloadingInstanceScenes.ContainsKey(sceneID))
                return;

            _loadedInstanceScenes.Remove(key);
            _unloadingInstanceScenes.Add(sceneID, key);
            try
            {
                _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            }
            catch (Exception exception)
            {
                _unloadingInstanceScenes.Remove(sceneID);
                _loadedInstanceScenes.Add(key, sceneID);
                throw new InvalidOperationException(
                    $"Unloading transfer scene '{address}' for {context} failed.",
                    exception);
            }
        }

        internal bool IsTransferInstanceScene(SceneID sceneID)
        {
            foreach (SceneID loadedScene in _loadedInstanceScenes.Values)
            {
                if (loadedScene == sceneID)
                    return true;
            }

            return _pendingInstanceSceneOwners.ContainsKey(sceneID)
                || _unloadingInstanceScenes.ContainsKey(sceneID);
        }

        internal void ConfirmTransferInstanceSceneReady(SceneID sceneID)
        {
            if (!_pendingInstanceSceneOwners.TryGetValue(sceneID, out InstanceSceneKey key)
                || !_loadedInstanceScenes.TryGetValue(key, out SceneID loadedSceneID)
                || loadedSceneID != sceneID)
                throw new InvalidOperationException(
                    $"Transfer-instance scene {sceneID} cannot be confirmed without a matching pending load.");

            CompleteInstanceSceneLoad(key, sceneID, true);
        }

        internal void FailTransferInstanceScene(SceneID sceneID, string reason)
        {
            if (!_pendingInstanceSceneOwners.TryGetValue(sceneID, out InstanceSceneKey key))
                throw new InvalidOperationException(
                    $"Transfer-instance scene {sceneID} cannot fail because it has no pending owner.");

            _loadedInstanceScenes.Remove(key);
            CompleteInstanceSceneLoad(key, sceneID, false);
            Debug.Error(
                $"[TransferInstanceScene] Rejected '{key.Address}' for {key.Context}: {reason}");
            _scenesAwaitingUnload.Add(sceneID);
        }

        private bool HandleTransferInstanceSceneLoaded(SceneID sceneID)
        {
            if (!_pendingInstanceSceneOwners.TryGetValue(sceneID, out InstanceSceneKey key))
                return false;

            if (!_pendingInstanceSceneLoads.ContainsKey(key))
            {
                _pendingInstanceSceneOwners.Remove(sceneID);
                Debug.Error($"[TransferInstanceScene] Loaded SceneID {sceneID} had no pending owner for {key.Address}.");
                _scenesAwaitingUnload.Add(sceneID);
                return true;
            }

            if (!_loadedInstanceScenes.TryAdd(key, sceneID))
            {
                _pendingInstanceSceneOwners.Remove(sceneID);
                CompleteInstanceSceneLoad(key, sceneID, false);
                Debug.Error(
                    $"[TransferInstanceScene] Duplicate loaded scene registration for '{key.Address}' in {key.Context}.");
                _scenesAwaitingUnload.Add(sceneID);
            }
            return true;
        }

        private bool HandleTransferInstanceSceneUnloaded(SceneID sceneID)
        {
            if (_unloadingInstanceScenes.Remove(sceneID))
                return true;

            InstanceSceneKey loadedKey = default;
            bool foundLoadedScene = false;
            foreach (KeyValuePair<InstanceSceneKey, SceneID> loaded in _loadedInstanceScenes)
            {
                if (loaded.Value != sceneID)
                    continue;
                loadedKey = loaded.Key;
                foundLoadedScene = true;
                break;
            }
            if (foundLoadedScene)
            {
                _loadedInstanceScenes.Remove(loadedKey);
                Debug.Error(
                    $"[TransferInstanceScene] Tracked scene '{loadedKey.Address}' for {loadedKey.Context} "
                    + $"reached terminal unload outside the owned unload path.");
                return true;
            }

            if (_pendingInstanceSceneOwners.Remove(sceneID, out InstanceSceneKey pendingKey))
            {
                CompleteInstanceSceneLoad(pendingKey, sceneID, false);
                Debug.Error(
                    $"[TransferInstanceScene] Pending scene '{pendingKey.Address}' for {pendingKey.Context} "
                    + "unloaded before its load completed.");
                return true;
            }

            return false;
        }

        private void CompleteInstanceSceneLoad(InstanceSceneKey key, SceneID sceneID, bool succeeded)
        {
            if (_pendingInstanceSceneLoadCleanup.Remove(key, out Action cleanup))
                cleanup?.Invoke();
            if (sceneID != default
                && _pendingInstanceSceneOwners.TryGetValue(sceneID, out InstanceSceneKey owner)
                && owner.Equals(key))
                _pendingInstanceSceneOwners.Remove(sceneID);

            if (!succeeded
                && sceneID != default
                && _loadedInstanceScenes.TryGetValue(key, out SceneID loadedSceneID)
                && loadedSceneID == sceneID)
            {
                _loadedInstanceScenes.Remove(key);
                _scenesAwaitingUnload.Add(sceneID);
            }

            if (!_pendingInstanceSceneLoads.Remove(key, out List<Action<bool, SceneID>> callbacks))
                return;

            foreach (Action<bool, SceneID> callback in callbacks)
            {
                try
                {
                    callback?.Invoke(succeeded, succeeded ? sceneID : default);
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"[TransferInstanceScene] Completion callback for '{key.Address}' in {key.Context} threw: {exception.Message}");
                }
            }
        }

        private void ClearInstanceSceneState()
        {
            foreach (KeyValuePair<InstanceSceneKey, List<Action<bool, SceneID>>> pending in _pendingInstanceSceneLoads)
            {
                if (_pendingInstanceSceneLoadCleanup.Remove(pending.Key, out Action cleanup))
                {
                    try
                    {
                        cleanup?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Debug.Error(
                            $"[TransferInstanceScene] Teardown cleanup for '{pending.Key.Address}' in {pending.Key.Context} threw: {exception.Message}");
                    }
                }
                foreach (Action<bool, SceneID> callback in pending.Value)
                {
                    try
                    {
                        callback?.Invoke(false, default);
                    }
                    catch (Exception exception)
                    {
                        Debug.Error(
                            $"[TransferInstanceScene] Teardown callback for '{pending.Key.Address}' in {pending.Key.Context} threw: {exception.Message}");
                    }
                }
            }

            _pendingInstanceSceneLoads.Clear();
            _pendingInstanceSceneOwners.Clear();
            _pendingInstanceSceneLoadCleanup.Clear();
            _loadedInstanceScenes.Clear();
            _unloadingInstanceScenes.Clear();
        }
    }
}
