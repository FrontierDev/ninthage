using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Server
{
    /// <summary>
    /// Server authority for world chunk scene loading and unloading.
    /// Does not manage player subscriptions — see ServerInterestManager.
    /// </summary>
    public sealed class ServerWorldManager : MonoBehaviour
    {
        private static ServerWorldManager _instance;
        /// <summary>Gets the active server world manager.</summary>
        public static ServerWorldManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the world manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private NetworkManager networkManager;
        private PurrSceneSettings worldSceneSettings;

        // Chunk coord → PurrNet SceneID
        private readonly Dictionary<Vector2Int, SceneID> loadedChunks = new();
        /// <summary>Gets the currently loaded world chunks.</summary>
        public IReadOnlyDictionary<Vector2Int, SceneID> LoadedChunks => loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<Vector2Int, List<Action<SceneID>>> _pendingLoads = new();

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneId;
        /// <summary>Gets the networked scene containing global actors.</summary>
        public SceneID GlobalActorsSceneId => _globalActorsSceneId;
        /// <summary>Gets whether the global actor scene has loaded.</summary>
        public bool IsGlobalActorsSceneLoaded => _globalActorsSceneId != default;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerWorldManager is already initialized!");
                return;
            }
            _instance = this;

            networkManager = NetworkManager.main;

            worldSceneSettings = new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = false,
            };

            networkManager.sceneModule.onSceneLoaded += OnChunkSceneLoaded;

            _initialized = true;
        }

        private void OnChunkSceneLoaded(SceneID sceneId, bool asServer)
        {
            if (!asServer) return;
            if (!networkManager.sceneModule.TryGetSceneState(sceneId, out var state)) return;

            string sceneName = state.scene.name;

            if (sceneName == "global_actors")
            {
                _globalActorsSceneId = sceneId;
                Debug.Log("Global actors scene loaded.");
                return;
            }

            if (sceneName.StartsWith("World"))
            {
                Vector2Int chunkCoord = ParseChunkCoordinate(sceneName);
                loadedChunks[chunkCoord] = sceneId;
                Debug.Log($"Scene '{sceneName}' loaded.");
            }
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
            networkManager.sceneModule.LoadSceneAsync("global_actors", settings);
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

        /// <summary>Determines whether a chunk scene is loaded.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool IsChunkLoaded(Vector2Int chunkCoord)
        {
            return loadedChunks.ContainsKey(chunkCoord);
        }

        /// <summary>Determines whether a chunk scene is currently loading.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <returns><see langword="true"/> when a load is pending.</returns>
        public bool IsChunkLoadPending(Vector2Int chunkCoord)
        {
            return _pendingLoads.ContainsKey(chunkCoord);
        }

        /// <summary>Attempts to get the networked scene identifier for a loaded chunk.</summary>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <param name="sceneId">Receives the scene identifier when found.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool TryGetChunkSceneId(Vector2Int chunkCoord, out SceneID sceneId)
        {
            return loadedChunks.TryGetValue(chunkCoord, out sceneId);
        }

        /// <summary>
        /// Ensures a chunk scene is loaded. If already loaded, invokes the callback immediately
        /// with the existing SceneID. Otherwise triggers an async load and invokes the callback
        /// once the scene is ready.
        /// </summary>
        public void EnsureChunkLoaded(Actor actor, Vector2Int chunkCoord, Action<SceneID> onReady)
        {
            if (loadedChunks.TryGetValue(chunkCoord, out SceneID sceneId))
            {
                onReady?.Invoke(sceneId);
                return;
            }

            // If a load is already in-flight, just queue the callback.
            if (_pendingLoads.TryGetValue(chunkCoord, out var callbacks))
            {
                callbacks.Add(onReady);
                return;
            }

            _pendingLoads[chunkCoord] = new List<Action<SceneID>> { onReady };

            var address = $"World_{chunkCoord.x}_{chunkCoord.y}";
            networkManager.sceneModule.LoadAddressableSceneAsync(address, worldSceneSettings);

            void OnLoaded(SceneID loadedSceneId, bool asServer)
            {
                if (!asServer) return;
                if (!loadedChunks.TryGetValue(chunkCoord, out var chunkSceneId)) return;
                if (chunkSceneId != loadedSceneId) return;

                networkManager.sceneModule.onSceneLoaded -= OnLoaded;

                if (_pendingLoads.TryGetValue(chunkCoord, out var pending))
                {
                    _pendingLoads.Remove(chunkCoord);
                    foreach (var cb in pending)
                        cb?.Invoke(loadedSceneId);
                }
            }

            networkManager.sceneModule.onSceneLoaded += OnLoaded;
        }

        /// <summary>
        /// Unloads a chunk scene. Should only be called when no players remain subscribed.
        /// </summary>
        public void UnloadChunk(Vector2Int chunkCoord)
        {
            if (!loadedChunks.TryGetValue(chunkCoord, out SceneID sceneId))
                return;

            loadedChunks.Remove(chunkCoord);
            networkManager.sceneModule.UnloadSceneAsync(sceneId);
            if (!networkManager.sceneModule.TryGetSceneState(sceneId, out var state)) return;
            string sceneName = state.scene.name;
            Debug.Log($"Unloaded chunk {chunkCoord} (no remaining subscribers).");
        }
    }
}
