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
using UnityEngine.ResourceManagement.AsyncOperations;
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
        private const float ADDRESSABLES_PROGRESS_END = 0.10f;
        private const float CONTENT_PROGRESS_END = 0.50f;
        private const float NETWORKING_READY_PROGRESS = 0.60f;
        private const float ECS_READY_PROGRESS = 0.70f;
        private const float WORLD_MANAGER_READY_PROGRESS = 0.75f;
        private const float MAIN_MENU_LOAD_PROGRESS_END = 0.95f;
        private const float UNITY_SCENE_LOAD_READY_PROGRESS = 0.90f;

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
                LoadingScreen.Instance.Show();

                // Step 1: Initialize addressables.
                Exception addressablesFailure = null;
                yield return InitializeAddressables(exception => addressablesFailure = exception);
                if (addressablesFailure != null)
                {
                    Debug.Error($"Client Addressables initialization failed: {addressablesFailure}");
                    LoadingScreen.Instance.ShowError(
                        "Required local game content is missing or corrupted. Repair or reinstall Ninth Age.");
                    yield break;
                }

                // Step 2: Load and publish all required local game content.
                Exception contentFailure = null;
                yield return GameContentBootstrap.Initialize(
                    exception => contentFailure = exception,
                    progress =>
                    {
                        float loadingProgress = Mathf.Lerp(
                            ADDRESSABLES_PROGRESS_END,
                            CONTENT_PROGRESS_END,
                            Mathf.Clamp01(progress));

                        LoadingScreen.Instance.SetProgress(loadingProgress);
                    });
                if (contentFailure != null)
                {
                    Debug.Error($"Client game content initialization failed: {contentFailure}");
                    LoadingScreen.Instance.ShowError(
                        "Required local game content is missing or corrupted. Repair or reinstall Ninth Age.");
                    yield break;
                }
                LoadingScreen.Instance.SetProgress(CONTENT_PROGRESS_END);

                // Step 3: Initialize networking as CLIENT.
                yield return ClientInitializeNetworking();
                LoadingScreen.Instance.SetProgress(NETWORKING_READY_PROGRESS);

                if (launchOptions.ProfilingEnabled)
                {
                    ClientPerformanceProfiler.Start(launchOptions.ProfileID);
                    profilingStarted = true;
                }

                // Step 4: ECS Worlds after network initialized.
                yield return ClientInitializeECSWorlds();
                LoadingScreen.Instance.SetProgress(ECS_READY_PROGRESS);

                yield return ClientInitializeWorldManager();
                LoadingScreen.Instance.SetProgress(WORLD_MANAGER_READY_PROGRESS);

                // Step 5: Unload the initialisation scene.
                yield return UnloadInitializationScene();
                LoadingScreen.Instance.Finish();
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
        /// Initializes the local Addressables catalog and reports failures to the client bootstrapper.
        /// </summary>
        private static IEnumerator InitializeAddressables(Action<Exception> onFailure)
        {
            Debug.Log(" - Step 1: Initializing Addressables...");

            AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> initOp =
                Addressables.InitializeAsync(false);

            float reportedProgress = 0f;
            while (!initOp.IsDone)
            {
                float normalizedProgress = Mathf.Clamp01(initOp.PercentComplete);
                float loadingProgress = normalizedProgress * ADDRESSABLES_PROGRESS_END;
                if (loadingProgress > reportedProgress)
                {
                    LoadingScreen.Instance.SetProgress(loadingProgress);
                    reportedProgress = loadingProgress;
                }

                yield return null;
            }

            AsyncOperationStatus status = initOp.Status;
            Exception operationException = initOp.OperationException;
            if (status != AsyncOperationStatus.Succeeded)
            {
                if (initOp.IsValid())
                    Addressables.Release(initOp);

                onFailure(new InvalidOperationException("Local Addressables initialization failed.", operationException));
                yield break;
            }

            LoadingScreen.Instance.SetProgress(ADDRESSABLES_PROGRESS_END);
            if (initOp.IsValid())
                Addressables.Release(initOp);

            Debug.Log(" ✓ Step 1: Addressables initialized successfully.");
        }

        private static IEnumerator ClientInitializeNetworking()
        {
            Debug.Log(" - Step 3: Initializing networking as CLIENT...");

            // Use the validated prefab published by the shared content bootstrap.
            var networkManagerPrefab = GameContentBootstrap.NetworkManagerPrefab;
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
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            if (loadOperation == null)
                throw new InvalidOperationException("Failed to start the MainMenu scene load.");

            while (!loadOperation.isDone)
            {
                float normalizedProgress = Mathf.Clamp01(
                    loadOperation.progress / UNITY_SCENE_LOAD_READY_PROGRESS);
                float loadingProgress = Mathf.Lerp(
                    WORLD_MANAGER_READY_PROGRESS,
                    MAIN_MENU_LOAD_PROGRESS_END,
                    normalizedProgress);
                LoadingScreen.Instance.SetProgress(loadingProgress);
                yield return null;
            }

            LoadingScreen.Instance.SetProgress(MAIN_MENU_LOAD_PROGRESS_END);

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync("Bootstrapper");
            if (unloadOperation == null)
                throw new InvalidOperationException("Failed to start the Bootstrapper scene unload.");

            while (!unloadOperation.isDone)
            {
                float normalizedProgress = Mathf.Clamp01(
                    unloadOperation.progress / UNITY_SCENE_LOAD_READY_PROGRESS);
                float loadingProgress = Mathf.Lerp(
                    MAIN_MENU_LOAD_PROGRESS_END,
                    1f,
                    normalizedProgress);
                LoadingScreen.Instance.SetProgress(loadingProgress);
                yield return null;
            }

            Debug.Log(" ✓ Step 7: Initialization scene unloaded successfully.");
        }
    }
}
