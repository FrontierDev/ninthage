using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Persistence;
using Game.Server.Services;
using Game.Server.Systems;
using Game.Shared.Networking;
using Game.Shared.Performance;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Server.Core
{
    /// <summary>Coordinates startup of the dedicated server runtime.</summary>
    public static class ServerInitialization
    {
        private const string WORLD_ARGUMENT = "--world";
        private static MonoBehaviour _host;
        private static GameObject _networkManagerObject;
        private static bool _forceCleanServer;
        private static Coroutine _initializationCoroutine;
        private static bool _initializationSucceeded;
        private static ServerTimeAuthority _timeAuthority;

        /// <summary>Gets the current server-lifetime UTC authority, or <c>null</c> when the server is stopped.</summary>
        public static ServerTimeAuthority TimeAuthority => _timeAuthority;

        /// <summary>
        /// Registers project-owned server metrics for an already active telemetry session.
        /// The benchmark runner owns session startup and invokes this entrypoint afterwards.
        /// </summary>
        public static void RegisterPerformanceMetrics()
        {
            if (!PerformanceTelemetry.IsEnabled)
                throw new InvalidOperationException("Start a performance telemetry session before registering server metrics.");

            NetworkingPerformanceMetrics.RegisterServerMetrics();
            ServerECSManager.RegisterPerformanceMetrics();
            WorldSpawnSystem.RegisterPerformanceMetrics();
            ActorUpdateSystem.RegisterPerformanceMetrics();
            ResourceRegenSystem.RegisterPerformanceMetrics();
            SpellCastSystem.RegisterPerformanceMetrics();
            CooldownTickSystem.RegisterPerformanceMetrics();
            AuraTickSystem.RegisterPerformanceMetrics();
            ActorTickerSystem.RegisterPerformanceMetrics();
            ServerActorManager.RegisterPerformanceMetrics();
            ServerAutoAttackManager.RegisterPerformanceMetrics();
            ServerSpawnManager.RegisterPerformanceMetrics();
            ServerWorldManager.RegisterPerformanceMetrics();
        }

        /// <summary>Begins the server initialization coroutine on the supplied host.</summary>
        /// <param name="host">The component that owns the initialization coroutine.</param>
        public static void Begin(MonoBehaviour host)
        {
            ValidateHost(host);
            BeginInternal(host, ResolveLaunchWorldName());
        }

        /// <summary>Begins the Editor/test server initialization with an explicitly supplied world name.</summary>
        /// <param name="host">The component that owns the initialization coroutine.</param>
        /// <param name="worldName">The explicit world identity supplied by the test bootstrap.</param>
        public static void Begin(MonoBehaviour host, string worldName)
        {
            BeginInternal(host, ValidateExplicitWorldName(worldName));
        }

        private static void BeginInternal(MonoBehaviour host, string launchWorldName)
        {
            ValidateHost(host);
            if (_initializationCoroutine != null || ServerECSManager.Initialized || ServerSimulationClock.IsRunning)
                throw new InvalidOperationException("The server initialization sequence is already active.");

            _timeAuthority = new ServerTimeAuthority();
            _host = host;
            _initializationSucceeded = false;
            bool runtimeContextInitialized = false;
            try
            {
                InitializeWorldRuntimeContext(launchWorldName);
                runtimeContextInitialized = true;
                _initializationCoroutine = _host.StartCoroutine(InitializationProcess());
            }
            catch
            {
                if (runtimeContextInitialized)
                    ServerWorldManager.ClearRuntimeContext();
                _timeAuthority = null;
                _host = null;
                throw;
            }
        }

        private static void ValidateHost(MonoBehaviour host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
        }

        private static void InitializeWorldRuntimeContext(string launchWorldName)
        {
            Guid worldID = Guid.NewGuid();
            ServerWorldManager.InitializeRuntimeContext(worldID);
            Debug.Log($"Server world '{launchWorldName}' initialized with world ID '{worldID}'.");
        }

        private static string ValidateExplicitWorldName(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                throw new ArgumentException(
                    "The explicit Editor/test server world name must be nonempty.",
                    nameof(worldName));
            }

            return worldName;
        }

        private static string ResolveLaunchWorldName()
        {
            string launchWorldName = null;
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                if (!string.Equals(commandLineArguments[i], WORLD_ARGUMENT, StringComparison.Ordinal))
                    continue;

                if (launchWorldName != null)
                {
                    throw new InvalidOperationException(
                        $"Server launch argument '{WORLD_ARGUMENT}' was supplied more than once.");
                }

                if (i + 1 >= commandLineArguments.Length
                    || string.IsNullOrWhiteSpace(commandLineArguments[i + 1])
                    || commandLineArguments[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Server launch argument '{WORLD_ARGUMENT}' requires a nonempty world name.");
                }

                launchWorldName = commandLineArguments[++i];
            }

            if (launchWorldName == null)
            {
                throw new InvalidOperationException(
                    $"Server startup requires an explicit '{WORLD_ARGUMENT} <world-name>' launch argument.");
            }

            return ValidateExplicitWorldName(launchWorldName);
        }

        private static IEnumerator InitializationProcess()
        {
            try
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

                // Step 6: Initialise server-side bindings for RPC handlers.
                yield return ServerInitializeBindings();

                // Step 7: Load player database.
                yield return ServerInitializePlayerDatabase();

                // Step 8: Load world scene through PurrNet so that it is tracked and ready for networked spawning.
                yield return ServerLoadWorldScene();

                _initializationSucceeded = true;
            }
            finally
            {
                _initializationCoroutine = null;
                if (!_initializationSucceeded)
                    Stop();
            }
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
            if (!Game.Shared.GameConfigurationManager.IsInitialized
                || Game.Shared.GameConfigurationManager.Config == null)
            {
                throw new InvalidOperationException("Server initialization could not load GameConfiguration.");
            }

            // Perform validation on game configuration to ensure all required settings are correctly set.
            Game.Shared.GameConfigurationManager.Config.ValidateSpatialConfiguration();

            yield return null;

            // Check to see if the server should clear player data on start,
            // which is useful for testing character creation, etc.
            _forceCleanServer = Game.Shared.GameConfigurationManager.Config.ClearPlayerDataOnStart;

            if (_forceCleanServer)
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
                throw new InvalidOperationException("Failed to load NetworkManager prefab from Resources/NetworkManager.");
            }

            // Instantiate NetworkManager
            var networkManagerObj = GameObject.Instantiate(networkManagerPrefab);
            _networkManagerObject = networkManagerObj;
            networkManagerObj.name = "[Server] NetworkManager";
            GameObject.DontDestroyOnLoad(networkManagerObj);
            var networkManager = networkManagerObj.GetComponent<NetworkManager>();
            if (networkManager == null)
                throw new InvalidOperationException("The NetworkManager prefab has no NetworkManager component.");

            // Disable auto-start; we'll manage it explicitly
            networkManager.startServerFlags = StartFlags.None;
            networkManager.startClientFlags = StartFlags.None;

            // Get transport for explicit control
            var transport = networkManagerObj.GetComponentInChildren<UDPTransport>();
            if (transport == null)
            {
                Debug.Error(" ✗ Failed to find UDPTransport.");
                throw new InvalidOperationException("The NetworkManager prefab has no UDPTransport component.");
            }

            // Give NetworkManager time to initialize
            yield return new WaitForSeconds(0.5f);

            // Explicitly start server.
            networkManager.StartServer();
            Debug.Log(" - Server started (listening on port 8888).");
            networkManagerObj.AddComponent<ServerConnectionManager>();
            Debug.Log(" - Initialised the server connection manager.");

            yield return null;
            Debug.Log(" ✓ Step 3: Networking initialized successfully.");
        }

        private static IEnumerator ServerInitializeECSWorlds()
        {
            Debug.Log(" - Step 4: Initializing server ECS worlds and systems...");

            ServerECSManager.ServerInitializeECSWorlds();

            yield return new WaitUntil(() => ServerECSManager.Initialized == true);
            Debug.Log(" ✓ Step 4: Server ECS worlds and systems initialized successfully.");
        }

        private static IEnumerator ServerStartSimulation()
        {
            Debug.Log(" - Step 5: Starting server simulation loop...");

            if (!ServerECSManager.Initialized)
                throw new InvalidOperationException("The server ECS world must be ready before starting simulation.");

            ServerSimulationClock.Start(_host);
            yield return null;
            if (!ServerSimulationClock.IsRunning)
                throw new InvalidOperationException("The server simulation clock stopped during startup.");

            Debug.Log(" ✓ Step 5: Server simulation loop started successfully.");
        }

        /// <summary>Stops server initialization, the simulation clock and the owned ECS world.</summary>
        public static void Stop()
        {
            _timeAuthority = null;
            if (_initializationCoroutine != null && _host != null)
                _host.StopCoroutine(_initializationCoroutine);

            _initializationCoroutine = null;
            ServerSimulationClock.Stop();
            ServerECSManager.Shutdown();
            ServerWorldManager.ClearRuntimeContext();
            DestroyNetworkManager();
            _initializationSucceeded = false;
            _host = null;
        }

        /// <summary>Aborts a still-running initialization coroutine when its server host is destroyed.</summary>
        internal static void AbortInitialization()
        {
            _timeAuthority = null;
            if (_initializationCoroutine != null && _host != null)
                _host.StopCoroutine(_initializationCoroutine);

            _initializationCoroutine = null;
            ServerSimulationClock.Stop();
            ServerECSManager.Shutdown();
            ServerWorldManager.ClearRuntimeContext();
            DestroyNetworkManager();
            _initializationSucceeded = false;
            _host = null;
        }

        private static void DestroyNetworkManager()
        {
            if (_networkManagerObject == null)
                return;

            GameObject.Destroy(_networkManagerObject);
            _networkManagerObject = null;
        }

        private static IEnumerator ServerInitializeBindings()
        {
            Debug.Log(" - Step 6: Initializing server bindings...");

            ServerBindings.Initialize();

            yield return new WaitUntil(() => ServerBindings.Initialized == true);
            Debug.Log(" ✓ Step 6: Server bindings initialized successfully.");
        }

        private static IEnumerator ServerInitializePlayerDatabase()
        {
            Debug.Log(" - Step 7: Initializing player database...");

            if (_forceCleanServer)
            {
                PlayerDatabase.CleanDatabase();
            }

            var authService = new PlayerAuthService();
            Game.Shared.Networking.LoginAuthenticator.SetAuthService(authService);

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

            NetworkManager.main.gameObject.AddComponent<ServerPhysicsManager>();
            yield return new WaitUntil(() => ServerPhysicsManager.IsInitialized == true);
            Debug.Log(" - Step 8: Server physics manager initialized.");

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
