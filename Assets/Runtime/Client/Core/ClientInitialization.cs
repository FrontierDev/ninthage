using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Utility;
using LoadingScreen = Game.Client.UI.LoadingScreenController;
using PurrNet;
using PurrNet.Transports;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientInitialization</c> implementation.
    /// </summary>
    public static class ClientInitialization
    {
        private const string PROFILING_ARGUMENT = "--profiling";
        private const string PROFILE_ID_ARGUMENT = "--profile-id";
        private const string DEFAULT_PROFILE_ID = "live-client";

        private readonly struct ClientLaunchOptions
        {
            public ClientLaunchOptions(bool profilingEnabled, string profileID)
            {
                ProfilingEnabled = profilingEnabled;
                ProfileID = profileID;
            }

            public bool ProfilingEnabled { get; }
            public string ProfileID { get; }
        }

        /// <summary>
        /// Registers project-owned client metrics for an already active telemetry session.
        /// ClientPerformanceProfiler owns session startup and invokes this entrypoint afterwards.
        /// </summary>
        public static void RegisterPerformanceMetrics()
        {
            if (!PerformanceTelemetry.IsEnabled)
                throw new InvalidOperationException("Start a performance telemetry session before registering client metrics.");

            ClientPerformanceMetrics.RegisterMetrics();
            NetworkingPerformanceMetrics.RegisterClientMetrics();
        }

        /// <summary>
        /// Executes the client-side <c>Begin</c> operation.
        /// </summary>
        public static void Begin(MonoBehaviour host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            ClientLaunchOptions launchOptions = ResolveLaunchOptions();
            host.StartCoroutine(InitializationProcess(launchOptions));
        }

        private static IEnumerator InitializationProcess(ClientLaunchOptions launchOptions)
        {
            bool profilingStarted = false;
            bool initializationCompleted = false;
            try
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

                if (launchOptions.ProfilingEnabled)
                {
                    ClientPerformanceProfiler.Start(launchOptions.ProfileID);
                    profilingStarted = true;
                }

                // Step 4: ECS Worlds after network initialized.
                yield return ClientInitializeECSWorlds();
                LoadingScreen.Instance.Tick();

                yield return ClientInitializeWorldManager();
                LoadingScreen.Instance.Tick();

                // Step 5: Unload the initialisation scene.
                yield return UnloadInitializationScene();
                initializationCompleted = true;
            }
            finally
            {
                if (profilingStarted && !initializationCompleted)
                    ClientPerformanceProfiler.Stop(
                        PerformanceRunStatus.FailInstrumentation,
                        "CLIENT_INITIALIZATION_ABORTED",
                        "Client initialization aborted after profiling started.");
            }
        }

        private static ClientLaunchOptions ResolveLaunchOptions()
        {
            bool profilingEnabled = false;
            string profileID = DEFAULT_PROFILE_ID;
            bool profileIDSpecified = false;
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                string argument = commandLineArguments[i];
                if (string.Equals(argument, PROFILING_ARGUMENT, StringComparison.Ordinal))
                {
                    if (profilingEnabled)
                    {
                        throw new InvalidOperationException(
                            $"Client launch argument '{PROFILING_ARGUMENT}' was supplied more than once.");
                    }

                    profilingEnabled = true;
                    continue;
                }

                if (!string.Equals(argument, PROFILE_ID_ARGUMENT, StringComparison.Ordinal))
                    continue;

                if (profileIDSpecified)
                {
                    throw new InvalidOperationException(
                        $"Client launch argument '{PROFILE_ID_ARGUMENT}' was supplied more than once.");
                }

                if (i + 1 >= commandLineArguments.Length
                    || string.IsNullOrWhiteSpace(commandLineArguments[i + 1])
                    || commandLineArguments[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Client launch argument '{PROFILE_ID_ARGUMENT}' requires a nonempty profile ID.");
                }

                profileID = commandLineArguments[++i];
                if (!IsValidProfileID(profileID))
                {
                    throw new InvalidOperationException(
                        $"Client launch argument '{PROFILE_ID_ARGUMENT}' must contain at most 128 ASCII letters, digits, periods, underscores, or hyphens.");
                }

                profileIDSpecified = true;
            }

            if (profileIDSpecified && !profilingEnabled)
            {
                throw new InvalidOperationException(
                    $"Client launch argument '{PROFILE_ID_ARGUMENT}' requires '{PROFILING_ARGUMENT}'.");
            }

            return new ClientLaunchOptions(profilingEnabled, profileID);
        }

        private static bool IsValidProfileID(string profileID)
        {
            if (string.IsNullOrWhiteSpace(profileID) || profileID.Length > 128)
                return false;

            foreach (char character in profileID)
            {
                bool isAsciiLetter = character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z';
                bool isAsciiDigit = character >= '0' && character <= '9';
                if (!isAsciiLetter && !isAsciiDigit && character != '.' && character != '_' && character != '-')
                    return false;
            }

            return true;
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
            networkManager.ResetOriginalScene(networkManagerObj.scene);

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
