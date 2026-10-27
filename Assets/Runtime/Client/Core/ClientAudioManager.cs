using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientAudioManager</c> implementation.
    /// </summary>
    public sealed class ClientAudioManager : MonoBehaviour
    {
        private static ClientAudioManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientAudioManager Instance => _instance;

        private static bool _initialized;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        private const int MAX_CACHE_SIZE = 50; // Prevent unbounded memory growth
        private const int MAX_TRACKED_ADDRESSABLE_OPERATIONS = 256;

        [FormerlySerializedAs("sfxSource")]
        [SerializeField] private AudioSource _sfxSource;

        private readonly Dictionary<string, (AudioClip Clip, AsyncOperationHandle<AudioClip> Handle)> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _pendingLoads = new();
        private Dictionary<string, long> _pendingLoadTimestamps;
        private readonly List<string> _cacheAccessOrder = new(); // Track LRU for eviction
        private bool _isDestroying;

        private void Awake()
        {
            // Prevent duplicate instances
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _initialized = true;

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f;
            }

            // Preload commonly used UI SFX to reduce latency on first play.
            PreloadSFX("sfx_click_generic");

            // Set up callbacks
            ActorEndpoint.onClientPlaySFX += PlaySFX;
            ActorEndpoint.onClientPlaySFXOnAudioSource += PlaySFXOnAudioSource;
            PerformanceTelemetry.SessionEnded += OnTelemetrySessionEnded;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            _isDestroying = true;

            // Unsubscribe from events
            ActorEndpoint.onClientPlaySFX -= PlaySFX;
            ActorEndpoint.onClientPlaySFXOnAudioSource -= PlaySFXOnAudioSource;
            PerformanceTelemetry.SessionEnded -= OnTelemetrySessionEnded;

            // Release cached async operations. Pending operations remain owned by their completion callbacks.
            var cachedKeys = new List<string>(_cache.Keys);
            foreach (string key in cachedKeys)
            {
                var cached = _cache[key];
                if (cached.Handle.IsValid())
                {
                    Addressables.Release(cached.Handle);
                    if (PerformanceTelemetry.IsEnabled)
                        PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                }

                _cache.Remove(key);
                _cacheAccessOrder.Remove(key);
                RecordAddressableOwnershipCounts();
            }
            _cache.Clear();
            _cacheAccessOrder.Clear();
            RecordCacheCount();
            RecordAddressableOwnershipCounts();
            _pendingLoadTimestamps?.Clear();

            // Clear instance
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
                _initialized = false;
            }
        }

        private void EvictCacheIfNeeded()
        {
            if (_cache.Count >= MAX_CACHE_SIZE)
            {
                if (_cacheAccessOrder.Count == 0)
                    throw new InvalidOperationException("Audio cache and LRU state are inconsistent.");

                string lruKey = _cacheAccessOrder[0];
                if (!_cache.TryGetValue(lruKey, out var cached))
                    throw new InvalidOperationException(
                        $"Audio cache and LRU state are inconsistent for key '{lruKey}'.");

                _cache.Remove(lruKey);
                _cacheAccessOrder.RemoveAt(0);
                if (cached.Handle.IsValid())
                {
                    Addressables.Release(cached.Handle);
                    if (PerformanceTelemetry.IsEnabled)
                        PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                }

                RecordCacheCount();
                RecordAddressableOwnershipCounts();
            }
        }

        private void UpdateCacheAccessOrder(string key)
        {
            // Move key to end (most recently used)
            _cacheAccessOrder.Remove(key);
            _cacheAccessOrder.Add(key);
        }

        /// <summary>
        /// Executes the client-side <c>PlaySFX</c> operation.
        /// </summary>
        public void PlaySFX(string addressablePath)
        {
            // Check cache first
            if (_cache.TryGetValue(addressablePath, out var cached))
            {
                UpdateCacheAccessOrder(addressablePath);
                _sfxSource.PlayOneShot(cached.Clip);
                return;
            }

            // Prevent duplicate loads
            if (_pendingLoads.ContainsKey(addressablePath))
                return;

            var handle = Addressables.LoadAssetAsync<AudioClip>(addressablePath);
            _pendingLoads[addressablePath] = handle;
            TrackAddressableLoadStarted(addressablePath);

            handle.Completed += loadHandle =>
            {
                if (!_pendingLoads.TryGetValue(addressablePath, out var ownedHandle)
                    || !ownedHandle.Equals(loadHandle))
                {
                    Debug.Error(
                        $"[ClientAudioManager] Addressable ownership mismatch for audio clip: {addressablePath}");
                    return;
                }

                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load audio clip: {addressablePath}");
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                if (_isDestroying)
                {
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = (loadHandle.Result, loadHandle);
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
                RecordAddressableOwnershipCounts();
                _sfxSource.PlayOneShot(loadHandle.Result);
            };
        }

        /// <summary>
        /// Executes the client-side <c>PreloadSFX</c> operation.
        /// </summary>
        public void PreloadSFX(string addressablePath)
        {
            // Already cached or pending
            if (_cache.ContainsKey(addressablePath) || _pendingLoads.ContainsKey(addressablePath))
                return;

            var handle = Addressables.LoadAssetAsync<AudioClip>(addressablePath);
            _pendingLoads[addressablePath] = handle;
            TrackAddressableLoadStarted(addressablePath);

            handle.Completed += loadHandle =>
            {
                if (!_pendingLoads.TryGetValue(addressablePath, out var ownedHandle)
                    || !ownedHandle.Equals(loadHandle))
                {
                    Debug.Error(
                        $"[ClientAudioManager] Addressable ownership mismatch for audio clip: {addressablePath}");
                    return;
                }

                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to preload audio clip: {addressablePath}");
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                if (_isDestroying)
                {
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = (loadHandle.Result, loadHandle);
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
                RecordAddressableOwnershipCounts();
            };
        }

        /// <summary>
        /// Get a cached audio clip synchronously, or null if not cached.
        /// </summary>
        public AudioClip GetCachedClip(string addressablePath)
        {
            if (_cache.TryGetValue(addressablePath, out var cached))
            {
                UpdateCacheAccessOrder(addressablePath);
                return cached.Clip;
            }
            return null;
        }

        /// <summary>
        /// Play a sound effect on the given AudioSource using cached clips when available.
        /// </summary>
        public void PlaySFXOnAudioSource(AudioSource audioSource, string addressablePath)
        {
            if (audioSource == null)
            {
                Debug.Warning("[ClientAudioManager] AudioSource is null!");
                return;
            }

            // Try to get cached clip first
            var clip = GetCachedClip(addressablePath);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            // Prevent duplicate loads
            if (_pendingLoads.ContainsKey(addressablePath))
            {
                return;
            }

            var handle = Addressables.LoadAssetAsync<AudioClip>(addressablePath);
            _pendingLoads[addressablePath] = handle;
            TrackAddressableLoadStarted(addressablePath);

            handle.Completed += loadHandle =>
            {
                if (!_pendingLoads.TryGetValue(addressablePath, out var ownedHandle)
                    || !ownedHandle.Equals(loadHandle))
                {
                    Debug.Error(
                        $"[ClientAudioManager] Addressable ownership mismatch for audio clip: {addressablePath}");
                    return;
                }

                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load audio clip: {addressablePath}");
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                if (_isDestroying)
                {
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                // Check if audioSource still exists
                if (audioSource == null)
                {
                    Debug.Warning($"AudioSource was destroyed before clip loaded: {addressablePath}");
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                        if (PerformanceTelemetry.IsEnabled)
                            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    }
                    RecordAddressableOwnershipCounts();
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = (loadHandle.Result, loadHandle);
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
                RecordAddressableOwnershipCounts();
                audioSource.PlayOneShot(loadHandle.Result);
            };
        }

        private void TrackAddressableLoadStarted(string addressablePath)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            _pendingLoadTimestamps ??= new Dictionary<string, long>();
            if (_pendingLoadTimestamps.Count < MAX_TRACKED_ADDRESSABLE_OPERATIONS)
                _pendingLoadTimestamps[addressablePath] = Stopwatch.GetTimestamp();
            RecordAddressableOwnershipCounts();
        }

        private void TrackAddressableLoadCompleted(string addressablePath)
        {
            if (_pendingLoadTimestamps != null
                && _pendingLoadTimestamps.TryGetValue(addressablePath, out long startedTimestamp))
            {
                _pendingLoadTimestamps.Remove(addressablePath);
                ClientPerformanceMetrics.TryRecordDuration(ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION, startedTimestamp);
            }

            RecordAddressableOwnershipCounts();
        }

        private void OnTelemetrySessionEnded()
        {
            _pendingLoadTimestamps?.Clear();
        }

        private void RecordCacheCount()
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.AUDIO_CACHE_COUNT, _cache.Count);
        }

        private void RecordAddressableOwnershipCounts()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingLoads.Count);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.AUDIO_LIVE_ADDRESSABLE_HANDLE_COUNT,
                _pendingLoads.Count + _cache.Count);
        }
    }
}
