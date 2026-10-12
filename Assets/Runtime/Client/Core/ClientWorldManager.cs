using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using Game.Client.UI;

namespace Game.Client
{
    public sealed class ClientWorldManager : MonoBehaviour
    {
        private static ClientWorldManager _instance;
        public static ClientWorldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientWorldManager();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        private List<Scene> loadedScenes = new List<Scene>();

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            Game.Shared.Networking.AccountEndpoint.onEnteringWorld += OnEnteringWorldRequested;
            _initialized = true;
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld -= OnEnteringWorldRequested;

            if (NetworkManager.main != null)
            {
                NetworkManager.main.sceneModule.onSceneLoaded -= OnSceneLoaded;
                NetworkManager.main.sceneModule.onSceneUnloaded -= OnSceneUnloaded;
            }

            Game.Shared.Networking.MovementEndpoint.onClientInterestRequest -= OnClientInterestRequest;

            _instance = null;
            _initialized = false;
        }

        private void OnEnteringWorldRequested(Game.Shared.Persistence.CharacterData characterData, string sceneName)
        {
            StartCoroutine(OnEnteringWorld(sceneName));
        }

        private void OnClientInterestRequest(Vector2Int chunk)
        {
            ClientPositionManager.Instance.SetOriginChunk(chunk);
        }

        public void OnSceneLoaded(SceneID sceneId, bool asServer)
        {
            if (!NetworkManager.main.sceneModule.TryGetSceneState(sceneId, out var state)) return;
            if (state.scene.name.StartsWith("World"))
            {
                if (loadedScenes.Contains(state.scene)) return;

                loadedScenes.Add(state.scene);

                // Offset the new scene to match the current floating origin.
                ClientPositionManager.Instance.ApplyOriginToScene(state.scene);
            }
        }

        private void OnSceneUnloaded(SceneID sceneId, bool asServer)
        {
            loadedScenes.RemoveAll(s => !s.isLoaded);
        }

        private IEnumerator OnEnteringWorld(string sceneName)
        {
            LoadingScreenController.Instance.Show(2);

            var spawnChunk = ParseChunkCoordinate(sceneName);
            ClientPositionManager.Instance.SetOriginChunk(spawnChunk);

            // Subscribe BEFORE loading global_world to avoid missing scene callbacks
            NetworkManager.main.sceneModule.onSceneLoaded += OnSceneLoaded;
            NetworkManager.main.sceneModule.onSceneUnloaded += OnSceneUnloaded;
            Game.Shared.Networking.MovementEndpoint.onClientInterestRequest += OnClientInterestRequest;

            var loadOp = SceneManager.LoadSceneAsync("global_world", LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadOp.isDone);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("global_world"));

            yield return new WaitUntil(() => loadedScenes.Count >= 9);

            Game.Shared.Networking.AccountEndpoint.onEnteredWorld?.Invoke();
            LoadingScreenController.Instance.Finish();
        }
        #endregion

        public void GetWorldScenes(List<Scene> buffer)
        {
            buffer.Clear();
            buffer.AddRange(loadedScenes);
        }

        private static Vector2Int ParseChunkCoordinate(string sceneName)
        {
            // Expected format: "World_X_Y"
            string[] parts = sceneName.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);
            return new Vector2Int(x, y);
        }
    }
}
