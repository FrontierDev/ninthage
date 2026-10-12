using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime;
using System.Collections.Generic;
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

        [FormerlySerializedAs("sfxSource")]
        [SerializeField] private AudioSource _sfxSource;

        private readonly Dictionary<string, AudioClip> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _pendingLoads = new();
        private readonly List<string> _cacheAccessOrder = new(); // Track LRU for eviction

        [ToDo("Implement a more robust caching strategy if needed, e.g. separate pools for UI vs world SFX, or reference counting for shared clips.")]
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
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            ActorEndpoint.onClientPlaySFX -= PlaySFX;
            ActorEndpoint.onClientPlaySFXOnAudioSource -= PlaySFXOnAudioSource;

            // Release pending async operations
            foreach (var handle in _pendingLoads.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _pendingLoads.Clear();

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

            handle.Completed += loadHandle =>
            {
                _pendingLoads.Remove(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to load audio clip: {addressablePath}");
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
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

            handle.Completed += loadHandle =>
            {
                _pendingLoads.Remove(addressablePath);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.Error($"Failed to preload audio clip: {addressablePath}");
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
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

            handle.Completed += loadHandle =>
            {
                _pendingLoads.Remove(addressablePath);

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
                    return;
                }

                EvictCacheIfNeeded();
                _cache[addressablePath] = loadHandle.Result;
                UpdateCacheAccessOrder(addressablePath);
                audioSource.PlayOneShot(loadHandle.Result);
            };
        }
    }
}
