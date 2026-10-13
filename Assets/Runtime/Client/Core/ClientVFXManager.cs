using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using Game.Shared.Runtime.VFX;
using PurrNet;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientVFXManager</c> implementation.
    /// </summary>
    public sealed class ClientVFXManager : MonoBehaviour
    {
        private sealed class LifetimeTracker : MonoBehaviour
        {
            internal Action Released;

            private void OnDestroy()
            {
                Action released = Released;
                Released = null;
                released?.Invoke();
            }
        }

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

        private int _activeVFXCount;
        private int _pendingAddressableLoads;

        private void Awake()
        {
            _instance = this;

            ActorEndpoint.onClientSpawnVFXAtPosition += OnClientSpawnVFXAtPosition;
            ActorEndpoint.onClientSpawnProjectile += OnClientSpawnProjectile;
            ActorEndpoint.onClientTriggerEffect += OnClientTriggerEffect;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onClientSpawnVFXAtPosition -= OnClientSpawnVFXAtPosition;
            ActorEndpoint.onClientSpawnProjectile -= OnClientSpawnProjectile;
            ActorEndpoint.onClientTriggerEffect -= OnClientTriggerEffect;

            _instance = null;
            _initialized = false;
        }

        private void OnClientSpawnVFXAtPosition(ActorVFXController actor, int vfxID, string addressablePath, Vector3 position, float lifetime)
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            long startedTimestamp = telemetryEnabled ? Stopwatch.GetTimestamp() : 0;
            if (telemetryEnabled)
            {
                _pendingAddressableLoads++;
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingAddressableLoads);
            }

            Addressables.LoadAssetAsync<GameObject>(addressablePath).Completed += handle =>
            {
                if (_pendingAddressableLoads > 0)
                    _pendingAddressableLoads--;
                if (PerformanceTelemetry.IsEnabled)
                {
                    ClientPerformanceMetrics.TryRecordDuration(ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION, startedTimestamp);
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingAddressableLoads);
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load effect prefab: {addressablePath}");
                    return;
                }

                var go = UnityProxy.InstantiateDirectly(handle.Result, position, Quaternion.identity);
                TrackVFX(go);
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
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            long startedTimestamp = telemetryEnabled ? Stopwatch.GetTimestamp() : 0;
            if (telemetryEnabled)
            {
                _pendingAddressableLoads++;
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingAddressableLoads);
            }

            Addressables.LoadAssetAsync<GameObject>(addressablePath).Completed += handle =>
            {
                if (_pendingAddressableLoads > 0)
                    _pendingAddressableLoads--;
                if (PerformanceTelemetry.IsEnabled)
                {
                    ClientPerformanceMetrics.TryRecordDuration(ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION, startedTimestamp);
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingAddressableLoads);
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load projectile prefab: {addressablePath}");
                    return;
                }

                if (target == null || target.ProjectileTarget == null)
                {
                    Debug.Error("Target or target ProjectileTarget is null when spawning projectile");
                    return;
                }

                var go = UnityProxy.InstantiateDirectly(handle.Result, actor.SpellEffectSpawnPoint.position, Quaternion.identity);
                TrackVFX(go);
                var projectile = go.GetComponent<SpellProjectile>();
                if (projectile == null)
                {
                    Debug.Error($"Projectile prefab is missing SpellProjectile component: {addressablePath}");
                    Destroy(go);
                    return;
                }

                projectile.Initialize(target.ProjectileTarget, speed);
            };
        }

        private void OnClientTriggerEffect(ActorVFXController actor, int vfxID)
        {
            if (!actor.ActiveEffects.TryGetValue(vfxID, out var go) || go == null) return;
            var trigger = go.GetComponentInChildren<IVFXTrigger>(true);
            trigger?.Trigger();
        }

        private void TrackVFX(GameObject effect)
        {
            if (!PerformanceTelemetry.IsEnabled || effect == null)
                return;

            var tracker = effect.GetComponent<LifetimeTracker>()
                ?? effect.AddComponent<LifetimeTracker>();
            tracker.Released += OnVFXReleased;
            _activeVFXCount++;
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_SPAWN_COUNT, 1);
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_ACTIVE_COUNT, _activeVFXCount);
        }

        private void OnVFXReleased()
        {
            if (_activeVFXCount > 0)
                _activeVFXCount--;

            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_RELEASE_COUNT, 1);
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.VFX_ACTIVE_COUNT, _activeVFXCount);
        }
    }
}
