using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
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

        private readonly Dictionary<string, AudioClip> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _pendingLoads = new();
        private Dictionary<string, long> _pendingLoadTimestamps;
        private readonly List<string> _cacheAccessOrder = new(); // Track LRU for eviction

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
            // Unsubscribe from events
            ActorEndpoint.onClientPlaySFX -= PlaySFX;
            ActorEndpoint.onClientPlaySFXOnAudioSource -= PlaySFXOnAudioSource;
            PerformanceTelemetry.SessionEnded -= OnTelemetrySessionEnded;

            // Release pending async operations
            foreach (var handle in _pendingLoads.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                    if (PerformanceTelemetry.IsEnabled)
                        PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                }
            }
            _pendingLoads.Clear();
            _pendingLoadTimestamps?.Clear();

            // Clear instance
            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }

        private void EvictCacheIfNeeded()
        {
            if (_cache.Count >= MAX_CACHE_SIZE)
            {
                // Remove least recently used entry
                if (_cacheAccessOrder.Count > 0)
                {
                    string lruKey = _cacheAccessOrder[0];
                    _cache.Remove(lruKey);
                    _cacheAccessOrder.RemoveAt(0);
                    RecordCacheCount();
                }
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
            if (_cache.TryGetValue(addressablePath, out AudioClip clip))
            {
                UpdateCacheAccessOrder(addressablePath);
                _sfxSource.PlayOneShot(clip);
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
                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load audio clip: {addressablePath}");
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
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
                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to preload audio clip: {addressablePath}");
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
            };
        }

        /// <summary>
        /// Get a cached audio clip synchronously, or null if not cached.
        /// </summary>
        public AudioClip GetCachedClip(string addressablePath)
        {
            if (_cache.TryGetValue(addressablePath, out AudioClip clip))
            {
                UpdateCacheAccessOrder(addressablePath);
                return clip;
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
                _pendingLoads.Remove(addressablePath);
                TrackAddressableLoadCompleted(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load audio clip: {addressablePath}");
                    return;
                }

                // Check if audioSource still exists
                if (audioSource == null)
                {
                    Debug.Warning($"AudioSource was destroyed before clip loaded: {addressablePath}");
                    Addressables.Release(loadHandle);
                    if (PerformanceTelemetry.IsEnabled)
                        PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_RELEASE_COUNT, 1);
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
                RecordCacheCount();
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
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT, _pendingLoads.Count);
        }

        private void TrackAddressableLoadCompleted(string addressablePath)
        {
            bool trackedForTelemetry = false;
            if (_pendingLoadTimestamps != null
                && _pendingLoadTimestamps.TryGetValue(addressablePath, out long startedTimestamp))
            {
                trackedForTelemetry = true;
                _pendingLoadTimestamps.Remove(addressablePath);
                ClientPerformanceMetrics.TryRecordDuration(ClientPerformanceMetrics.ADDRESSABLE_LOAD_DURATION, startedTimestamp);
            }

            if (trackedForTelemetry && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(
                    ClientPerformanceMetrics.ADDRESSABLE_PENDING_COUNT,
                    _pendingLoads.Count);
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
    }
}
