using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Actor = Game.Shared.Actor;
using PurrNet.Modules;

namespace Game.Server
{
    /// <summary>
    /// Server authority for world chunk scene loading and unloading.
    /// Does not manage player subscriptions — see ServerInterestManager.
    /// </summary>
    public sealed class ServerWorldManager : MonoBehaviour
    {
        private static ServerWorldManager _instance;
        public static ServerWorldManager Instance => _instance;
        private static bool _initialized = false;
        public static bool IsInitialized => _initialized;

        private NetworkManager networkManager;
        private PurrSceneSettings worldSceneSettings;

        // Chunk coord → PurrNet SceneID
        private readonly Dictionary<Vector2Int, SceneID> loadedChunks = new Dictionary<Vector2Int, SceneID>();
        public IReadOnlyDictionary<Vector2Int, SceneID> LoadedChunks => loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<Vector2Int, List<Action<SceneID>>> _pendingLoads = new();

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneId;
        public SceneID GlobalActorsSceneId => _globalActorsSceneId;
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

        public Vector2Int ParseChunkCoordinate(string sceneName)
        {
            string[] parts = sceneName.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);
            return new Vector2Int(x, y);
        }

        public bool IsChunkLoaded(Vector2Int chunkCoord)
        {
            return loadedChunks.ContainsKey(chunkCoord);
        }

        public bool IsChunkLoadPending(Vector2Int chunkCoord)
        {
            return _pendingLoads.ContainsKey(chunkCoord);
        }

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