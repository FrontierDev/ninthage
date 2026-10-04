using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections;
using Debug = Game.Shared.FormattedDebug;
using LoadingScreen = Game.Client.UI.LoadingScreenController;
using PurrNet;
using PurrNet.Transports;
using UnityEngine.SceneManagement;
using Game.Shared.Utility;
using Game.Shared.Data;
using Game.Shared;

namespace Game.Client
{
    public static class ClientInitialization
    {
        public static void Begin(MonoBehaviour host)
        {
            host.StartCoroutine(InitializationProcess());
        }

        private static IEnumerator InitializationProcess()
        {
            Debug.Log("Client Initialization started.");

            yield return new WaitUntil(() => LoadingScreen.Instance != null);
            LoadingScreen.Instance.Show(5);

            // Step 1: Initialize addressables.
            yield return InitializeAddressables();
            LoadingScreen.Instance.Tick();

            // Step 2: Load game configuration.
            yield return LoadGameConfiguration();
            LoadingScreen.Instance.Tick();

            // Step 3: Initialize networking as CLIENT.
            yield return ClientInitializeNetworking();
            LoadingScreen.Instance.Tick();

            // Step 4: ECS Worlds after network initialized.
            yield return ClientInitializeECSWorlds();
            LoadingScreen.Instance.Tick();

            yield return ClientInitializeWorldManager();
            LoadingScreen.Instance.Tick();

            // Step 5: Unload the initialisation scene.
            yield return UnloadInitializationScene();
        }

        /// <summary>
        /// Initializes the Addressables system, ensuring all assets are ready for use before proceeding with further initialization steps.
        /// </summary>
        /// <returns></returns>
        private static IEnumerator InitializeAddressables()
        {
            Debug.Log(" - Step 1: Initializing Addressables...");

            // Initiate async initialization
            var initOp = Addressables.InitializeAsync();

            // Wait for operation to complete
            while (!initOp.IsDone) { yield return null; }

            Debug.Log(" ✓ Step 1: Addressables initialized successfully.");
        }

        private static IEnumerator LoadGameConfiguration()
        {
            Debug.Log(" - Step 2: Loading game configuration...");

            // Tell the game configuration manager to load.
            Game.Shared.GameConfigurationManager.Initialize();
            yield return new WaitUntil(() => Game.Shared.GameConfigurationManager.IsInitialized);

            Debug.Log(" ✓ Step 2: Game configuration loaded successfully.");
        }

        private static IEnumerator ClientInitializeNetworking()
        {
            Debug.Log(" - Step 3: Initializing networking as CLIENT...");

            // Load NetworkManager prefab from Resources
            var networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            if (networkManagerPrefab == null)
            {
                Debug.Error(" ✗ Failed to load NetworkManager prefab from Resources/NetworkManager");
                yield break;
            }

            // Instantiate NetworkManager
            var networkManagerObj = GameObject.Instantiate(networkManagerPrefab);
            networkManagerObj.name = "[Client] NetworkManager";
            GameObject.DontDestroyOnLoad(networkManagerObj);
            var networkManager = networkManagerObj.GetComponent<NetworkManager>();

            // Add the client connection manage to the network manager object.
            networkManagerObj.AddComponent<ClientConnectionManager>();

            // Disable auto-start; we'll manage it explicitly
            networkManager.startServerFlags = StartFlags.None;
            networkManager.startClientFlags = StartFlags.None;

            // Get transport for explicit control
            var transport = networkManagerObj.GetComponentInChildren<UDPTransport>();
            if (transport == null)
            {
                Debug.Error(" ✗ Failed to find UDPTransport.");
                yield break;
            }

            // On the client, do nothing else. Server will start itself when ready.

            yield return null;
            Debug.Log(" ✓ Step 3: Networking initialized successfully.");
        }

        private static IEnumerator ClientInitializeECSWorlds()
        {
            Debug.Log(" - Step 4: Initializing client ECS worlds and systems...");

            ClientECSManager.ClientInitializeECSWorlds();

            yield return new WaitUntil(() => ClientECSManager.Initialized == true);
            Debug.Log(" ✓ Step 4: Client ECS worlds and systems initialized successfully.");
        }

        private static IEnumerator ClientInitializeWorldManager()
        {
            Debug.Log(" - Step 5: Initializing ClientWorldManager...");

            // Create a new GameObject for the ClientWorldManager and add the component.
            var worldManagerObj = new GameObject("[Client] WorldManager");
            GameObject.DontDestroyOnLoad(worldManagerObj);
            var worldManager = worldManagerObj.AddComponent<ClientWorldManager>();
            var positionManager = worldManagerObj.AddComponent<ClientPositionManager>();
            var cameraManager = worldManagerObj.AddComponent<CameraManager>();
            var inputController = worldManagerObj.AddComponent<ClientInputController>();
            var combatManager = worldManagerObj.AddComponent<ClientCombatManager>();
            var vfxManager = worldManagerObj.AddComponent<ClientVFXManager>();
            var audioManager = worldManagerObj.AddComponent<ClientAudioManager>();

            yield return new WaitUntil(() => (ClientWorldManager.Initialized && ClientPositionManager.Initialized) == true);

            Debug.Log(" ✓ Step 5: ClientWorldManager initialized successfully.");
        }

        private static IEnumerator UnloadInitializationScene()
        {
            Debug.Log(" - Step 7: Unloading initialization scene...");

            // Don't destroy the main camera.
            GameObject.DontDestroyOnLoad(GameObject.FindGameObjectWithTag("MainCamera"));
            GameObject.DontDestroyOnLoad(GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>());
            GameObject.DontDestroyOnLoad(GameObject.FindGameObjectWithTag("CinemachineCamera"));

            // Unload the initialization scene to free up resources.
            var loadOp = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            while (!loadOp.isDone) { yield return null; }

            var unloadOp = SceneManager.UnloadSceneAsync("Bootstrapper");
            while (!unloadOp.isDone) { yield return null; }

            Debug.Log(" ✓ Step 7: Initialization scene unloaded successfully.");
        }
    }
}