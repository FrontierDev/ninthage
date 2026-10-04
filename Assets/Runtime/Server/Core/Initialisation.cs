using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections;
using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using PurrNet.Transports;
using Game.Server.Persistence;

namespace Game.Server
{
    public static class ServerInitialization
    {
        private static MonoBehaviour host;
        private static bool forceCleanServer;

        public static void Begin(MonoBehaviour _host)
        {
            host = _host;
            host.StartCoroutine(InitializationProcess());
        }

        private static IEnumerator InitializationProcess()
        {
            Debug.Log("Server Initialization started.");
            yield return null;

            // Step 1: Initialize addressables.
            yield return InitializeAddressables();

            // Step 2: Load game configuration.
            yield return LoadGameConfiguration();

            // Step 3: Initialize networking.
            yield return ServerInitializeNetworking();

            // Step 4: ECS Worlds after network initialized.
            yield return ServerInitializeECSWorlds();

            // Step 5: Start server simulation loop.
            yield return ServerStartSimulation();

            // Step 6: Initialise server-side hooks for RPC handlers.
            yield return ServerInitializeRPCHooks();

            // Step 7: Load player database.
            yield return ServerInitializePlayerDatabase();

            // Step 8: Load world scene through PurrNet so that it is tracked and ready for networked spawning.
            yield return ServerLoadWorldScene();

            // Step 9: Unload initialization scene to free up resources, leaving only the networked world scene active.            
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

            // Check to see if the server should clear player data on start,
            // which is useful for testing character creation, etc.
            forceCleanServer = Game.Shared.GameConfigurationManager.Config.ClearPlayerDataOnStart;

            if (forceCleanServer)
                Debug.Warning(" - Server is configured to clear player data on start. All existing player data will be deleted.");

            Debug.Log(" ✓ Step 2: Game configuration loaded successfully.");
        }

        private static IEnumerator ServerInitializeNetworking()
        {
            Debug.Log(" - Step 3: Initializing networking as SERVER...");

            // Load NetworkManager prefab from Resources
            var networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            if (networkManagerPrefab == null)
            {
                Debug.Error(" ✗ Failed to load NetworkManager prefab from Resources/NetworkManager");
                yield break;
            }

            // Instantiate NetworkManager
            var networkManagerObj = GameObject.Instantiate(networkManagerPrefab);
            networkManagerObj.name = "[Server] NetworkManager";
            GameObject.DontDestroyOnLoad(networkManagerObj);
            var networkManager = networkManagerObj.GetComponent<NetworkManager>();

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

            // Give NetworkManager time to initialize
            yield return new WaitForSeconds(0.5f);

            // Explicitly start server.
            networkManager.StartServer();
            Debug.Log(" - Server started (listening on port 8888).");
            networkManagerObj.AddComponent<ServerConnectionManager>();
            Debug.Log(" - Initialised the server connection manager.");

            // Monitor server connection state
            networkManager.onServerConnectionState += (state) => Debug.Log($" - Server connection state: {state}.");

            yield return null;
            Debug.Log(" ✓ Step 3: Networking initialized successfully.");
        }

        private static IEnumerator ServerInitializeECSWorlds()
        {
            Debug.Log(" - Step 4: Initializing server ECS worlds and systems...");

            ECS.ServerECSManager.ServerInitializeECSWorlds();

            yield return new WaitUntil(() => ECS.ServerECSManager.Initialized == true);
            Debug.Log(" ✓ Step 4: Server ECS worlds and systems initialized successfully.");
        }

        private static IEnumerator ServerStartSimulation()
        {
            Debug.Log(" - Step 5: Starting server simulation loop...");

            // Start the main server simulation loop here, e.g. by enabling a ServerSimulationManager component that runs the ECS systems.

            yield return null;
            Debug.Warning(" ✓ Step 5: Server simulation loop not yet implemented.");
        }

        private static IEnumerator ServerInitializeRPCHooks()
        {
            Debug.Log(" - Step 6: Initializing server RPC hooks...");

            ServerHooksManager.ServerInitializeRPCHooks();

            yield return new WaitUntil(() => ServerHooksManager.Initialized == true);
            Debug.Log(" ✓ Step 6: Server RPC hooks initialized successfully.");
        }

        private static IEnumerator ServerInitializePlayerDatabase()
        {
            Debug.Log(" - Step 7: Initializing player database...");

            if (forceCleanServer)
            {
                PlayerDatabase.CleanDatabase();
            }

            var authService = new PlayerAuthService();
            Game.Shared.Authentication.LoginAuthenticator.SetAuthService(authService);

            yield return null;
            Debug.Log(" ✓ Step 7: Player database initialized successfully.");
        }

        private static IEnumerator ServerLoadWorldScene()
        {
            Debug.Log(" - Step 8: Loading world scene...");

            // For demonstration purposes, we'll just load a single scene here. In a real implementation, you would likely want to load/unload scenes dynamically based on player location, etc.
            NetworkManager.main.gameObject.AddComponent<ServerWorldManager>();
            yield return new WaitUntil(() => ServerWorldManager.IsInitialized == true);
            Debug.Log(" - Step 8: World scene manager initialized.");

            ServerWorldManager.Instance.LoadGlobalActorsScene();
            yield return new WaitUntil(() => ServerWorldManager.Instance.IsGlobalActorsSceneLoaded);
            Debug.Log(" - Step 8: Global actors scene loaded.");

            NetworkManager.main.gameObject.AddComponent<ServerInterestManager>();
            yield return new WaitUntil(() => ServerInterestManager.IsInitialized == true);
            Debug.Log(" - Step 8: Interest manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerPositionManager>();
            yield return new WaitUntil(() => ServerPositionManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server position manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerSpawnManager>();
            yield return new WaitUntil(() => ServerSpawnManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server spawn manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerActorManager>();
            yield return new WaitUntil(() => ServerActorManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server actor manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerStatManager>();
            yield return new WaitUntil(() => ServerStatManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server stat manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerSpellCastManager>();
            yield return new WaitUntil(() => ServerSpellCastManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server spell cast manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerAuraManager>();
            yield return new WaitUntil(() => ServerAuraManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server aura manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerTickerManager>();
            yield return new WaitUntil(() => ServerTickerManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server ticker manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerCooldownManager>();
            yield return new WaitUntil(() => ServerCooldownManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server cooldown manager initialized.");

            NetworkManager.main.gameObject.AddComponent<ServerAutoAttackManager>();
            yield return new WaitUntil(() => ServerAutoAttackManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server auto-attack manager initialized.");

            Debug.Log(" ✓ Step 8: World scene loaded successfully.");
        }
    }
}