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

        private NetworkManager _networkManager;
        private PurrSceneSettings _worldSceneSettings;

        // Chunk coord → PurrNet SceneID
        private readonly Dictionary<Vector2Int, SceneID> _loadedChunks = new();
        /// <summary>Gets the currently loaded world chunks.</summary>
        public IReadOnlyDictionary<Vector2Int, SceneID> LoadedChunks => _loadedChunks;

        // Chunks currently being loaded asynchronously.
        private readonly Dictionary<Vector2Int, List<Action<SceneID>>> _pendingLoads = new();

        // Persistent scene where all actor GameObjects live.
        private SceneID _globalActorsSceneID;
        /// <summary>Gets the networked scene containing global actors.</summary>
        public SceneID GlobalActorsSceneID => _globalActorsSceneID;
        /// <summary>Gets whether the global actor scene has loaded.</summary>
        public bool IsGlobalActorsSceneLoaded => _globalActorsSceneID != default;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerWorldManager is already initialized!");
                return;
            }
            _instance = this;

            _networkManager = NetworkManager.main;

            _worldSceneSettings = new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = false,
            };

            _networkManager.sceneModule.onSceneLoaded += OnChunkSceneLoaded;

            _initialized = true;
        }

        private void OnChunkSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!asServer) return;
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;

            string sceneName = state.scene.name;

            if (sceneName == "global_actors")
            {
                _globalActorsSceneID = sceneID;
                Debug.Log("Global actors scene loaded.");
                return;
            }

            if (sceneName.StartsWith("World"))
            {
                Vector2Int chunkCoord = ParseChunkCoordinate(sceneName);
                _loadedChunks[chunkCoord] = sceneID;
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
            _networkManager.sceneModule.LoadSceneAsync("global_actors", settings);
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
            return _loadedChunks.ContainsKey(chunkCoord);
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
        /// <param name="sceneID">Receives the scene identifier when found.</param>
        /// <returns><see langword="true"/> when the chunk is loaded.</returns>
        public bool TryGetChunkSceneID(Vector2Int chunkCoord, out SceneID sceneID)
        {
            return _loadedChunks.TryGetValue(chunkCoord, out sceneID);
        }

        /// <summary>
        /// Ensures a chunk scene is loaded. If already loaded, invokes the callback immediately
        /// with the existing SceneID. Otherwise triggers an async load and invokes the callback
        /// once the scene is ready.
        /// </summary>
        public void EnsureChunkLoaded(Actor actor, Vector2Int chunkCoord, Action<SceneID> onReady)
        {
            if (_loadedChunks.TryGetValue(chunkCoord, out SceneID sceneID))
            {
                onReady?.Invoke(sceneID);
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
            _networkManager.sceneModule.LoadAddressableSceneAsync(address, _worldSceneSettings);

            void OnLoaded(SceneID loadedSceneID, bool asServer)
            {
                if (!asServer) return;
                if (!_loadedChunks.TryGetValue(chunkCoord, out var chunkSceneID)) return;
                if (chunkSceneID != loadedSceneID) return;

                _networkManager.sceneModule.onSceneLoaded -= OnLoaded;

                if (_pendingLoads.TryGetValue(chunkCoord, out var pending))
                {
                    _pendingLoads.Remove(chunkCoord);
                    foreach (var cb in pending)
                        cb?.Invoke(loadedSceneID);
                }
            }

            _networkManager.sceneModule.onSceneLoaded += OnLoaded;
        }

        /// <summary>
        /// Unloads a chunk scene. Should only be called when no players remain subscribed.
        /// </summary>
        public void UnloadChunk(Vector2Int chunkCoord)
        {
            if (!_loadedChunks.TryGetValue(chunkCoord, out SceneID sceneID))
                return;

            _loadedChunks.Remove(chunkCoord);
            _networkManager.sceneModule.UnloadSceneAsync(sceneID);
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;
            string sceneName = state.scene.name;
            Debug.Log($"Unloaded chunk {chunkCoord} (no remaining subscribers).");
        }
    }
}
