using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Client.VFX;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using Game.Shared.Runtime.VFX;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientVFXManager</c> implementation.
    /// </summary>
    public sealed class ClientVFXManager : MonoBehaviour
    {
        private static ClientVFXManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientVFXManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        private readonly HashSet<AsyncOperationHandle<GameObject>> _pendingVFXHandles = new();
        private readonly Dictionary<GameObject, ActiveVFXOwnership> _activeVFX = new();
        private bool _isDestroying;

        private readonly struct ActiveVFXOwnership
        {
            public AsyncOperationHandle<GameObject> Handle { get; }
            public ClientVFXLifetimeNotifier Notifier { get; }
            public ActorVFXController Actor { get; }
            public int EffectID { get; }
            public bool IsRegisteredEffect { get; }

            public ActiveVFXOwnership(
                AsyncOperationHandle<GameObject> handle,
                ClientVFXLifetimeNotifier notifier,
                ActorVFXController actor,
                int effectID,
                bool isRegisteredEffect)
            {
                Handle = handle;
                Notifier = notifier;
                Actor = actor;
                EffectID = effectID;
                IsRegisteredEffect = isRegisteredEffect;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            ActorEndpoint.onClientSpawnVFXAtPosition += OnClientSpawnVFXAtPosition;
            ActorEndpoint.onClientSpawnProjectile += OnClientSpawnProjectile;
            ActorEndpoint.onClientTriggerEffect += OnClientTriggerEffect;
            ClientWorldManager.Instance.onWorldPresentationEnding += EndWorldPresentation;

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            _isDestroying = true;
            ActorEndpoint.onClientSpawnVFXAtPosition -= OnClientSpawnVFXAtPosition;
            ActorEndpoint.onClientSpawnProjectile -= OnClientSpawnProjectile;
            ActorEndpoint.onClientTriggerEffect -= OnClientTriggerEffect;
            if (ClientWorldManager.Initialized)
                ClientWorldManager.Instance.onWorldPresentationEnding -= EndWorldPresentation;

            var activeInstances = new List<GameObject>(_activeVFX.Keys);
            foreach (var instance in activeInstances)
                RequestActiveVFXDestruction(instance);

            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
                _initialized = false;
            }
        }

        private void OnClientSpawnVFXAtPosition(ActorVFXController actor, int vfxID, string addressablePath, WorldPosition position, float lifetime)
        {
            if (_isDestroying
                || !ClientWorldManager.Initialized
                || !ClientWorldManager.Instance.IsWorldPresentationActive)
            {
                Debug.Error($"Cannot spawn effect {vfxID} ('{addressablePath}') outside an active world presentation.");
                return;
            }

            if (!position.IsFinite)
            {
                Debug.Error($"Cannot spawn effect {vfxID} ('{addressablePath}') at a non-finite world position.");
                return;
            }

            var generation = ClientWorldManager.Instance.WorldPresentationGeneration;
            long startedTimestamp = PerformanceTelemetry.IsEnabled ? Stopwatch.GetTimestamp() : 0;
            var handle = Addressables.LoadAssetAsync<GameObject>(addressablePath);
            if (!_pendingVFXHandles.Add(handle))
                throw new InvalidOperationException($"Addressable VFX handle was already pending: {addressablePath}");
            RecordVFXOwnershipCounts();

            handle.Completed += completedHandle =>
            {
                if (!_pendingVFXHandles.Contains(handle))
                {
                    Debug.Error($"[ClientVFXManager] Completed VFX handle is not owned: {addressablePath}");
                    return;
                }

                ClientPerformanceMetrics.TryRecordDuration(
                    ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION,
                    startedTimestamp);

                bool staleWorld = _isDestroying
                    || !ClientWorldManager.Initialized
                    || !ClientWorldManager.Instance.IsWorldPresentationActive
                    || ClientWorldManager.Instance.WorldPresentationGeneration != generation;
                if (staleWorld
                    || completedHandle.Status != AsyncOperationStatus.Succeeded
                    || completedHandle.Result == null)
                {
                    if (staleWorld)
                        Debug.Error($"Discarding stale effect completion: {addressablePath}");
                    else
                        Debug.Error($"Failed to load effect prefab: {addressablePath}");

                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                if (actor == null)
                {
                    Debug.Error($"Cannot spawn effect {vfxID} ('{addressablePath}'): source actor is unavailable.");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                if (!ClientPositionManager.Initialized || ClientPositionManager.Instance == null)
                {
                    Debug.Error($"Cannot spawn effect {vfxID} ('{addressablePath}'): ClientPositionManager is not initialized.");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }
                if (!position.IsFinite)
                {
                    Debug.Error($"Cannot spawn effect {vfxID} ('{addressablePath}') at a non-finite world position.");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                Vector3 localPosition = ClientPositionManager.Instance.NetworkToLocal(
                    position.Chunk,
                    position.LocalPosition);
                Scene globalActors = SceneManager.GetSceneByName("global_actors");
                if (!globalActors.IsValid() || !globalActors.isLoaded)
                {
                    Debug.Error($"Cannot place effect {vfxID} ('{addressablePath}'): global_actors scene is unavailable.");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                var go = UnityProxy.InstantiateDirectly(completedHandle.Result, localPosition, Quaternion.identity);
                if (go == null)
                {
                    Debug.Error($"Failed to instantiate effect {vfxID} ('{addressablePath}').");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending VFX handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                SceneManager.MoveGameObjectToScene(go, globalActors);
                RegisterActiveVFX(go, handle, actor, vfxID, true);
                actor.RegisterEffect(vfxID, go);

                if (lifetime > 0f)
                {
                    var fadeable = go.GetComponentInChildren<IVFXFadeOut>(true);
                    fadeable?.VFXFadeOut();
                }
            };
        }

        private void OnClientSpawnProjectile(ActorVFXController actor, Actor target, string addressablePath, float speed)
        {
            if (_isDestroying
                || !ClientWorldManager.Initialized
                || !ClientWorldManager.Instance.IsWorldPresentationActive)
            {
                Debug.Error($"Cannot spawn projectile ('{addressablePath}') outside an active world presentation.");
                return;
            }

            var generation = ClientWorldManager.Instance.WorldPresentationGeneration;
            long startedTimestamp = PerformanceTelemetry.IsEnabled ? Stopwatch.GetTimestamp() : 0;
            var handle = Addressables.LoadAssetAsync<GameObject>(addressablePath);
            if (!_pendingVFXHandles.Add(handle))
                throw new InvalidOperationException($"Addressable projectile handle was already pending: {addressablePath}");
            RecordVFXOwnershipCounts();

            handle.Completed += completedHandle =>
            {
                if (!_pendingVFXHandles.Contains(handle))
                {
                    Debug.Error($"[ClientVFXManager] Completed projectile handle is not owned: {addressablePath}");
                    return;
                }

                ClientPerformanceMetrics.TryRecordDuration(
                    ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION,
                    startedTimestamp);

                bool staleWorld = _isDestroying
                    || !ClientWorldManager.Initialized
                    || !ClientWorldManager.Instance.IsWorldPresentationActive
                    || ClientWorldManager.Instance.WorldPresentationGeneration != generation;
                if (staleWorld
                    || completedHandle.Status != AsyncOperationStatus.Succeeded
                    || completedHandle.Result == null)
                {
                    if (staleWorld)
                        Debug.Error($"Discarding stale projectile completion: {addressablePath}");
                    else
                        Debug.Error($"Failed to load projectile prefab: {addressablePath}");

                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending projectile handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                if (actor == null || actor.SpellEffectSpawnPoint == null)
                {
                    Debug.Error($"Cannot spawn projectile ('{addressablePath}'): source actor or spawn point is unavailable.");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending projectile handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                if (target == null || target.ProjectileTarget == null)
                {
                    Debug.Error("Target or target ProjectileTarget is null when spawning projectile");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending projectile handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                var go = UnityProxy.InstantiateDirectly(
                    completedHandle.Result,
                    actor.SpellEffectSpawnPoint.position,
                    Quaternion.identity);
                if (go == null)
                {
                    Debug.Error($"Failed to instantiate projectile prefab: {addressablePath}");
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending projectile handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                var projectile = go.GetComponent<SpellProjectile>();
                if (projectile == null)
                {
                    Debug.Error($"Projectile prefab is missing SpellProjectile component: {addressablePath}");
                    Destroy(go);
                    if (!_pendingVFXHandles.Remove(handle))
                        throw new InvalidOperationException($"Pending projectile handle was lost: {addressablePath}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordVFXOwnershipCounts();
                    return;
                }

                RegisterActiveVFX(go, handle, null, default, false);
                projectile.Initialize(target.ProjectileTarget, speed);
            };
        }

        private void OnClientTriggerEffect(ActorVFXController actor, int vfxID)
        {
            if (!actor.ActiveEffects.TryGetValue(vfxID, out var go) || go == null) return;
            var trigger = go.GetComponentInChildren<IVFXTrigger>(true);
            trigger?.Trigger();
        }

        private void RegisterActiveVFX(
            GameObject instance,
            AsyncOperationHandle<GameObject> handle,
            ActorVFXController actor,
            int effectID,
            bool isRegisteredEffect)
        {
            if (instance == null)
                throw new InvalidOperationException("Cannot register a null active VFX instance.");
            if (!_pendingVFXHandles.Contains(handle))
                throw new InvalidOperationException("Cannot register an active VFX instance without its pending handle.");
            if (_activeVFX.ContainsKey(instance))
                throw new InvalidOperationException("An active VFX instance was registered more than once.");

            var notifier = instance.GetComponent<ClientVFXLifetimeNotifier>();
            if (notifier == null)
                notifier = instance.AddComponent<ClientVFXLifetimeNotifier>();
            notifier.onDestroyed += OnActiveVFXDestroyed;

            _activeVFX.Add(
                instance,
                new ActiveVFXOwnership(handle, notifier, actor, effectID, isRegisteredEffect));
            if (!_pendingVFXHandles.Remove(handle))
                throw new InvalidOperationException("Active VFX registration lost its pending handle.");

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_SPAWN_COUNT, 1);
            RecordVFXOwnershipCounts();
        }

        private void OnActiveVFXDestroyed(GameObject instance)
        {
            CompleteActiveVFXRelease(instance);
        }

        private void RequestActiveVFXDestruction(GameObject instance)
        {
            if (!_activeVFX.TryGetValue(instance, out var ownership))
                return;

            if (ownership.IsRegisteredEffect && ownership.Actor != null)
                ownership.Actor.UnregisterEffect(ownership.EffectID, instance);
            if (instance != null)
                Destroy(instance);
        }

        private void CompleteActiveVFXRelease(GameObject instance)
        {
            if (!_activeVFX.TryGetValue(instance, out var ownership))
                return;

            _activeVFX.Remove(instance);
            if (ownership.Notifier != null)
                ownership.Notifier.onDestroyed -= OnActiveVFXDestroyed;
            if (ownership.IsRegisteredEffect && ownership.Actor != null)
                ownership.Actor.UnregisterEffect(ownership.EffectID, instance);

            if (ownership.Handle.IsValid())
            {
                Addressables.Release(ownership.Handle);
                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_RELEASE_COUNT, 1);
            RecordVFXOwnershipCounts();
        }

        private void EndWorldPresentation()
        {
            var activeInstances = new List<GameObject>(_activeVFX.Keys);
            foreach (var instance in activeInstances)
                RequestActiveVFXDestruction(instance);
        }

        private void RecordVFXOwnershipCounts()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT,
                _pendingVFXHandles.Count);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.VFX_ACTIVE_COUNT,
                _activeVFX.Count);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.VFX_LIVE_ADDRESSABLE_HANDLE_COUNT,
                _pendingVFXHandles.Count + _activeVFX.Count);
        }
    }
}
