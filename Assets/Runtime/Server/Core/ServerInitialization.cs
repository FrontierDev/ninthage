using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Server.Services;
using Game.Server.Systems;
using Game.Shared.Networking;
using Game.Shared.Performance;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Server.Core
{
    /// <summary>Coordinates startup of the dedicated server runtime.</summary>
    public static class ServerInitialization
    {
        private const string WORLD_ARGUMENT = "--world";
        private const string PROFILING_ARGUMENT = "--profiling";
        private const string PROFILE_ID_ARGUMENT = "--profile-id";
        private const string CONTROL_PORT_ARGUMENT = "--control-port";
        private const string DEFAULT_PROFILE_ID = "live-server";
        private const int DEFAULT_CONTROL_PORT = 8890;
        private const int SERVER_PLAYER_LOOP_TARGET_FRAME_RATE = 60;

        private static readonly Guid WORLD_SERVER_RUN_ID = CreateWorldServerRunID();

        private readonly struct ServerLaunchOptions
        {
            public ServerLaunchOptions(string worldName, bool profilingEnabled, string profileId, int controlPort)
            {
                WorldName = worldName;
                ProfilingEnabled = profilingEnabled;
                ProfileId = profileId;
                ControlPort = controlPort;
            }

            public string WorldName { get; }
            public bool ProfilingEnabled { get; }
            public string ProfileId { get; }
            public int ControlPort { get; }
        }

        private static MonoBehaviour _host;
        private static GameObject _networkManagerObject;
        private static bool _forceCleanServer;
        private static string _launchWorldName;
        private static bool _profilingRequested;
        private static Coroutine _initializationCoroutine;
        private static Coroutine _fatalShutdownCoroutine;
        private static bool _fatalShutdownRequested;
        private static string _fatalShutdownMessage;
        private static bool _gracefulShutdownRunning;
        private static bool _networkShutdownTimedOut;
        private static bool _initializationSucceeded;
        private static ServerTimeAuthority _timeAuthority;
        private static MasterWorldClient _masterWorldClient;
        private static MasterPersistenceClient _masterPersistenceClient;
        private static WorldPersistenceCoordinator _worldPersistenceCoordinator;
        private static CancellationTokenSource _persistenceCheckpointCancellation;
        private static Task _persistenceCheckpointTask;
        private static CancellationTokenSource _masterHeartbeatCancellation;
        private static Task _masterHeartbeatTask;
        private static Coroutine _masterHeartbeatMonitorCoroutine;

        internal static bool IsFatalShutdownRequested => _fatalShutdownRequested;
        internal static int DedicatedServerPlayerLoopTargetFrameRate => SERVER_PLAYER_LOOP_TARGET_FRAME_RATE;

        /// <summary>Gets the current server-lifetime UTC authority, or <c>null</c> when the server is stopped.</summary>
        public static ServerTimeAuthority TimeAuthority => _timeAuthority;

        /// <summary>Gets the bootstrap-owned persistence coordinator for the active World Server run.</summary>
        internal static WorldPersistenceCoordinator PersistenceCoordinator
        {
            get
            {
                if (_worldPersistenceCoordinator == null)
                    throw new InvalidOperationException(
                        "World persistence has not been initialized or has already been torn down.");

                return _worldPersistenceCoordinator;
            }
        }

        /// <summary>
        /// Registers project-owned server metrics for an already active telemetry session.
        /// Live server profiling owns session startup and invokes this entrypoint after networking.
        /// </summary>
        public static void RegisterPerformanceMetrics()
        {
            if (!PerformanceTelemetry.IsEnabled)
                throw new InvalidOperationException("Start a performance telemetry session before registering server metrics.");

            NetworkManager networkManager = NetworkManager.main;
            if (networkManager == null)
                throw new InvalidOperationException("Cannot register server metrics without an active NetworkManager.");

            ITransport rawTransport = networkManager.rawTransport;
            if (rawTransport == null)
                throw new InvalidOperationException("Cannot register server metrics without an active NetworkManager raw transport.");

            NetworkingPerformanceMetrics.RegisterServerMetrics(
                rawTransport.measuresRoundTripTime,
                rawTransport is UDPTransport);
            ServerECSManager.RegisterPerformanceMetrics();
            WorldSpawnSystem.RegisterPerformanceMetrics();
            ActorUpdateSystem.RegisterPerformanceMetrics();
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
            ConfigureDedicatedServerPlayerLoop();
            ServerLaunchOptions launchOptions = ResolveLaunchOptions();
            BeginInternal(
                host,
                launchOptions.WorldName,
                launchOptions.ProfilingEnabled,
                launchOptions.ProfileId,
                launchOptions.ControlPort);
        }

        /// <summary>Begins the Editor/test server initialization with an explicitly supplied world name.</summary>
        /// <param name="host">The component that owns the initialization coroutine.</param>
        /// <param name="worldName">The explicit world identity supplied by the test bootstrap.</param>
        public static void Begin(MonoBehaviour host, string worldName)
        {
            BeginInternal(host, ValidateExplicitWorldName(worldName), false, null, null);
        }

        private static void BeginInternal(
            MonoBehaviour host,
            string launchWorldName,
            bool profilingEnabled,
            string profilingProfileId,
            int? controlPort)
        {
            ValidateHost(host);
            if (_fatalShutdownRequested)
                throw new InvalidOperationException("The server cannot restart after a fatal authoritative simulation failure.");
            if (_initializationCoroutine != null || ServerECSManager.Initialized || ServerSimulationClock.IsRunning)
                throw new InvalidOperationException("The server initialization sequence is already active.");

            _timeAuthority = new ServerTimeAuthority();
            _host = host;
            _launchWorldName = launchWorldName;
            _profilingRequested = profilingEnabled;
            _initializationSucceeded = false;
            _networkShutdownTimedOut = false;
            _fatalShutdownMessage = null;
            ShutdownExitCode = 0;
            try
            {
                ServerShutdownController.Attach(_host);
                if (controlPort.HasValue)
                    ServerControlEndpoint.Attach(_host, controlPort.Value);
                _initializationCoroutine = _host.StartCoroutine(InitializationProcess(profilingProfileId));
            }
            catch
            {
                CancelAndDisposeMasterStateSynchronously();
                _timeAuthority = null;
                _launchWorldName = null;
                _profilingRequested = false;
                _host = null;
                ServerControlEndpoint.Detach();
                ServerShutdownController.Detach();
                throw;
            }
        }

        private static void ValidateHost(MonoBehaviour host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
        }

        private static void ConfigureDedicatedServerPlayerLoop()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = SERVER_PLAYER_LOOP_TARGET_FRAME_RATE;
            Debug.Log($"Dedicated server player loop target: {SERVER_PLAYER_LOOP_TARGET_FRAME_RATE} Hz.");
        }

        private static Guid CreateWorldServerRunID()
        {
            Guid worldServerRunID;
            do
            {
                worldServerRunID = Guid.NewGuid();
            }
            while (worldServerRunID == Guid.Empty);

            return worldServerRunID;
        }

        private static string ResolveBuildIdentity()
        {
            if (!string.IsNullOrWhiteSpace(Application.buildGUID))
                return Application.buildGUID;

            if (!string.IsNullOrWhiteSpace(Application.version))
                return Application.version;

            throw new InvalidOperationException("The server requires a nonblank build identity.");
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

        private static ServerLaunchOptions ResolveLaunchOptions()
        {
            string launchWorldName = null;
            bool profilingEnabled = false;
            string profileId = DEFAULT_PROFILE_ID;
            bool profileIdSpecified = false;
            int controlPort = DEFAULT_CONTROL_PORT;
            bool controlPortSpecified = false;
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                string argument = commandLineArguments[i];
                if (string.Equals(argument, PROFILING_ARGUMENT, StringComparison.Ordinal))
                {
                    if (profilingEnabled)
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{PROFILING_ARGUMENT}' was supplied more than once.");
                    }

                    profilingEnabled = true;
                    continue;
                }

                if (string.Equals(argument, PROFILE_ID_ARGUMENT, StringComparison.Ordinal))
                {
                    if (profileIdSpecified)
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{PROFILE_ID_ARGUMENT}' was supplied more than once.");
                    }

                    if (i + 1 >= commandLineArguments.Length
                        || string.IsNullOrWhiteSpace(commandLineArguments[i + 1])
                        || commandLineArguments[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{PROFILE_ID_ARGUMENT}' requires a nonempty profile ID.");
                    }

                    profileId = commandLineArguments[++i];
                    if (!IsValidProfileId(profileId))
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{PROFILE_ID_ARGUMENT}' must contain at most 128 ASCII letters, digits, periods, underscores, or hyphens.");
                    }

                    profileIdSpecified = true;
                    continue;
                }

                if (string.Equals(argument, CONTROL_PORT_ARGUMENT, StringComparison.Ordinal))
                {
                    if (controlPortSpecified)
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{CONTROL_PORT_ARGUMENT}' was supplied more than once.");
                    }

                    if (i + 1 >= commandLineArguments.Length
                        || string.IsNullOrWhiteSpace(commandLineArguments[i + 1])
                        || commandLineArguments[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{CONTROL_PORT_ARGUMENT}' requires an integer port in the range 1..65535.");
                    }

                    string controlPortValue = commandLineArguments[++i];
                    if (!int.TryParse(
                            controlPortValue,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out controlPort)
                        || controlPort < 1
                        || controlPort > 65535)
                    {
                        throw new InvalidOperationException(
                            $"Server launch argument '{CONTROL_PORT_ARGUMENT}' must be an integer in the range 1..65535.");
                    }

                    controlPortSpecified = true;
                    continue;
                }

                if (!string.Equals(argument, WORLD_ARGUMENT, StringComparison.Ordinal))
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

            if (profileIdSpecified && !profilingEnabled)
            {
                throw new InvalidOperationException(
                    $"Server launch argument '{PROFILE_ID_ARGUMENT}' requires '{PROFILING_ARGUMENT}'.");
            }

            return new ServerLaunchOptions(
                ValidateExplicitWorldName(launchWorldName),
                profilingEnabled,
                profileId,
                controlPort);
        }

        private static bool IsValidProfileId(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || profileId.Length > 128)
                return false;

            foreach (char character in profileId)
            {
                bool isAsciiLetter = character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z';
                bool isAsciiDigit = character >= '0' && character <= '9';
                if (!isAsciiLetter && !isAsciiDigit && character != '.' && character != '_' && character != '-')
                    return false;
            }

            return true;
        }

        private static IEnumerator InitializationProcess(string profilingProfileId)
        {
            try
            {
                Debug.Log("Server Initialization started.");
                yield return ServerRegisterWithMaster();
                yield return null;

                // Step 1: Initialize addressables.
                yield return InitializeAddressables();

                // Step 2: Load game configuration.
                yield return LoadGameConfiguration();

                // Step 3: Initialize networking.
                yield return ServerInitializeNetworking();

                // Step 3A: Start live profiling after the real transport exists and before ECS initialization.
                if (_profilingRequested)
                    ServerPerformanceProfiler.Start(_launchWorldName, profilingProfileId);

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
                if (!_initializationSucceeded && !_fatalShutdownRequested)
                    AbortInitialization();
            }
        }

        private static IEnumerator ServerRegisterWithMaster()
        {
            Debug.Log(" - Step 0: Registering with the Master World Server...");
            if (_masterWorldClient != null)
                throw new InvalidOperationException("The Master World Server client is already initialized.");

            _masterWorldClient = new MasterWorldClient(
                _launchWorldName,
                WORLD_SERVER_RUN_ID,
                ResolveBuildIdentity());

            Task<MasterWorldProtocol.RegistrationResponse> registrationTask =
                _masterWorldClient.RegisterAsync(CancellationToken.None);
            while (!registrationTask.IsCompleted)
                yield return null;

            MasterWorldProtocol.RegistrationResponse registrationResponse =
                registrationTask.GetAwaiter().GetResult();
            if (registrationResponse.Result != MasterWorldProtocol.RegistrationResult.Accepted)
            {
                throw new InvalidOperationException(
                    $"Master registration for world '{_launchWorldName}' was rejected with result {registrationResponse.Result}.");
            }

            ServerWorldManager.InitializeRuntimeContext(registrationResponse.WorldID.Value);
            Debug.Log(
                $"Server world '{_launchWorldName}' registered with durable world ID '{registrationResponse.WorldID.Value}'.");

            Task<MasterWorldProtocol.LeaseResponse> initialHeartbeatTask =
                _masterWorldClient.HeartbeatAsync(registrationResponse.WorldID.Value, CancellationToken.None);
            while (!initialHeartbeatTask.IsCompleted)
                yield return null;

            MasterWorldProtocol.LeaseResponse initialHeartbeatResponse =
                initialHeartbeatTask.GetAwaiter().GetResult();
            if (initialHeartbeatResponse.Result != MasterWorldProtocol.LeaseResult.Accepted)
            {
                throw new InvalidOperationException(
                    $"Master heartbeat for world '{_launchWorldName}' was rejected with result {initialHeartbeatResponse.Result}.");
            }

            _masterPersistenceClient = new MasterPersistenceClient(_masterWorldClient);
            _worldPersistenceCoordinator = new WorldPersistenceCoordinator(
                _masterPersistenceClient,
                registrationResponse.WorldID.Value,
                WORLD_SERVER_RUN_ID);
            _persistenceCheckpointCancellation = new CancellationTokenSource();
            _persistenceCheckpointTask = _worldPersistenceCoordinator.RunCheckpointLoopAsync(
                _persistenceCheckpointCancellation.Token);

            _masterHeartbeatCancellation = new CancellationTokenSource();
            _masterHeartbeatTask = _masterWorldClient.RunHeartbeatLoopAsync(
                registrationResponse.WorldID.Value,
                registrationResponse.HeartbeatIntervalSeconds.Value,
                _masterHeartbeatCancellation.Token);
            _masterHeartbeatMonitorCoroutine = _host.StartCoroutine(MonitorMasterHeartbeat());
        }

        private static IEnumerator MonitorMasterHeartbeat()
        {
            Task heartbeatTask = _masterHeartbeatTask;
            while (heartbeatTask != null && !heartbeatTask.IsCompleted)
                yield return null;

            _masterHeartbeatMonitorCoroutine = null;
            if (heartbeatTask == null)
                yield break;

            bool cancellationRequested = IsMasterHeartbeatCancellationRequested();
            try
            {
                heartbeatTask.GetAwaiter().GetResult();
                if (!cancellationRequested)
                {
                    BeginFatalShutdown(
                        new InvalidOperationException("The Master heartbeat loop completed unexpectedly."));
                }
            }
            catch (OperationCanceledException) when (cancellationRequested)
            {
            }
            catch (Exception exception)
            {
                if (!cancellationRequested)
                    BeginFatalShutdown(exception);
            }
        }

        private static bool IsMasterHeartbeatCancellationRequested()
        {
            return _masterHeartbeatCancellation != null
                && _masterHeartbeatCancellation.IsCancellationRequested;
        }

        private static void RequestMasterHeartbeatCancellation()
        {
            if (_masterHeartbeatCancellation == null
                || _masterHeartbeatCancellation.IsCancellationRequested)
            {
                return;
            }

            _masterHeartbeatCancellation.Cancel();
        }

        private static void StopMasterHeartbeatMonitor()
        {
            if (_masterHeartbeatMonitorCoroutine != null && _host != null)
                _host.StopCoroutine(_masterHeartbeatMonitorCoroutine);

            _masterHeartbeatMonitorCoroutine = null;
        }

        private static IEnumerator StopAndSettleMasterHeartbeat()
        {
            RequestMasterHeartbeatCancellation();
            StopMasterHeartbeatMonitor();

            while (_masterHeartbeatTask != null && !_masterHeartbeatTask.IsCompleted)
                yield return null;

            ObserveMasterHeartbeatTask();
            _masterHeartbeatTask = null;
            DisposeMasterHeartbeatCancellation();
        }

        private static IEnumerator StopAndSettlePersistenceGracefully()
        {
            WorldPersistenceCoordinator persistenceCoordinator = _worldPersistenceCoordinator;
            if (persistenceCoordinator == null)
            {
                ClearPersistenceState();
                yield break;
            }

            try
            {
                persistenceCoordinator.StopAcceptingWork();
                RequestPersistenceCheckpointCancellation();

                while (_persistenceCheckpointTask != null && !_persistenceCheckpointTask.IsCompleted)
                    yield return null;

                ObservePersistenceCheckpointTask();

                while (!persistenceCoordinator.IsIdle)
                    yield return null;

                if (_fatalShutdownRequested)
                    yield break;

                Task finalCheckpointTask = null;
                try
                {
                    finalCheckpointTask = persistenceCoordinator.FlushPendingCheckpointsForShutdownAsync(
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    LogPersistenceTeardownFailure("final checkpoint flush", exception);
                }

                if (finalCheckpointTask != null)
                    yield return ObservePersistenceTask(finalCheckpointTask, "final checkpoint flush");

                if (_fatalShutdownRequested)
                    yield break;

                Task releaseCharactersTask = null;
                try
                {
                    releaseCharactersTask = persistenceCoordinator.ReleaseAllCharactersAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    LogPersistenceTeardownFailure("character lease release", exception);
                }

                if (releaseCharactersTask != null)
                    yield return ObservePersistenceTask(releaseCharactersTask, "character lease release");
            }
            finally
            {
                ClearPersistenceState();
            }
        }

        private static IEnumerator ObservePersistenceTask(Task task, string operationDescription)
        {
            while (!task.IsCompleted)
                yield return null;

            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException exception)
            {
                LogPersistenceTeardownFailure(operationDescription, exception);
            }
            catch (Exception exception)
            {
                LogPersistenceTeardownFailure(operationDescription, exception);
            }
        }

        private static IEnumerator UnregisterMasterWorld()
        {
            if (_masterWorldClient == null)
                yield break;

            ServerWorldManager worldManager = ServerWorldManager.Instance;
            if (worldManager == null || !worldManager.CurrentRuntimeContext.IsValid)
            {
                DisposeMasterWorldClient();
                yield break;
            }

            Guid worldID = worldManager.CurrentRuntimeContext.WorldID;
            Task<MasterWorldProtocol.LeaseResponse> unregisterTask = null;
            try
            {
                unregisterTask = _masterWorldClient.UnregisterAsync(worldID, CancellationToken.None);
            }
            catch (Exception exception)
            {
                LogMasterUnregisterFailure(exception);
            }

            if (unregisterTask != null)
            {
                while (!unregisterTask.IsCompleted)
                    yield return null;

                try
                {
                    MasterWorldProtocol.LeaseResponse response = unregisterTask.GetAwaiter().GetResult();
                    if (response.Result != MasterWorldProtocol.LeaseResult.Accepted)
                    {
                        Debug.Warning(
                            $"Master unregister was rejected with result {response.Result}.");
                    }
                }
                catch (Exception exception)
                {
                    LogMasterUnregisterFailure(exception);
                }
            }

            DisposeMasterWorldClient();
        }

        private static void LogMasterUnregisterFailure(Exception exception)
        {
            Debug.Warning(
                "Master unregister failed: "
                + exception.GetType().Name
                + ": "
                + exception.Message);
        }

        private static void CancelAndDisposeMasterStateSynchronously()
        {
            CancelAndDisposePersistenceStateSynchronously();
            RequestMasterHeartbeatCancellation();
            StopMasterHeartbeatMonitor();
            ObserveMasterHeartbeatTask();
            _masterHeartbeatTask = null;
            DisposeMasterHeartbeatCancellation();
            DisposeMasterWorldClient();
        }

        private static void CancelAndDisposePersistenceStateSynchronously()
        {
            if (_worldPersistenceCoordinator != null)
                _worldPersistenceCoordinator.StopAcceptingWork();

            RequestPersistenceCheckpointCancellation();
            ObservePersistenceCheckpointTask();
            ClearPersistenceState();
        }

        private static void RequestPersistenceCheckpointCancellation()
        {
            if (_persistenceCheckpointCancellation == null
                || _persistenceCheckpointCancellation.IsCancellationRequested)
            {
                return;
            }

            _persistenceCheckpointCancellation.Cancel();
        }

        private static void ObservePersistenceCheckpointTask()
        {
            Task persistenceTask = _persistenceCheckpointTask;
            if (persistenceTask == null)
                return;

            if (!persistenceTask.IsCompleted)
            {
                persistenceTask.ContinueWith(
                    completedTask => ObserveCompletedPersistenceTask(completedTask),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                return;
            }

            ObserveCompletedPersistenceTask(persistenceTask);
        }

        private static void ObserveCompletedPersistenceTask(Task persistenceTask)
        {
            try
            {
                persistenceTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                LogPersistenceTeardownFailure("checkpoint loop", exception);
            }
        }

        private static void ClearPersistenceState()
        {
            _persistenceCheckpointTask = null;
            _persistenceCheckpointCancellation?.Dispose();
            _persistenceCheckpointCancellation = null;
            _worldPersistenceCoordinator = null;
            _masterPersistenceClient = null;
        }

        private static void LogPersistenceTeardownFailure(string operationDescription, Exception exception)
        {
            Debug.Warning(
                "Persistence "
                + operationDescription
                + " failed during server teardown: "
                + exception.GetType().Name
                + ": "
                + exception.Message);
        }

        private static void ObserveMasterHeartbeatTask()
        {
            if (_masterHeartbeatTask == null)
                return;

            try
            {
                _masterHeartbeatTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (IsMasterHeartbeatCancellationRequested())
            {
            }
            catch (Exception exception)
            {
                Debug.Warning(
                    "Master heartbeat task ended during teardown: "
                    + exception.GetType().Name
                    + ": "
                    + exception.Message);
            }
        }

        private static void DisposeMasterHeartbeatCancellation()
        {
            _masterHeartbeatCancellation?.Dispose();
            _masterHeartbeatCancellation = null;
        }

        private static void DisposeMasterWorldClient()
        {
            _masterWorldClient?.Dispose();
            _masterWorldClient = null;
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

        /// <summary>Requests graceful shutdown of the running dedicated server.</summary>
        public static void Stop()
        {
            ServerShutdownController.RequestShutdown(
                "SERVER_STOPPED",
                "The dedicated server was requested to stop.");
        }

        internal static int ShutdownExitCode { get; private set; }

        /// <summary>Runs the ordered main-thread teardown for an operator/admin shutdown request.</summary>
        internal static IEnumerator BeginGracefulShutdown(string reasonCode, string message)
        {
            if (_gracefulShutdownRunning)
                yield break;
            if (_fatalShutdownRequested)
            {
                ShutdownExitCode = 1;
                yield break;
            }

            _gracefulShutdownRunning = true;
            _networkShutdownTimedOut = false;
            ShutdownExitCode = 0;
            try
            {
                ServerControlEndpoint.Detach();
                _timeAuthority = null;
                StopInitializationCoroutine();
                ServerSimulationClock.Stop();

                NetworkManager networkManager = GetNetworkManager();
                yield return StopNetworkServerAndWait(networkManager);

                yield return StopAndSettlePersistenceGracefully();
                yield return StopAndSettleMasterHeartbeat();
                if (_fatalShutdownRequested)
                    DisposeMasterWorldClient();
                else
                    yield return UnregisterMasterWorld();

                DestroyNetworkManager();
                yield return null;

                ServerECSManager.Shutdown();
                ServerWorldManager.ClearRuntimeContext();

                bool fatalShutdown = _fatalShutdownRequested;
                string finalReasonCode = fatalShutdown
                    ? "FATAL_SERVER_SHUTDOWN"
                    : _networkShutdownTimedOut
                        ? "NETWORK_SHUTDOWN_TIMEOUT"
                        : reasonCode;
                string finalMessage = fatalShutdown
                    ? _fatalShutdownMessage
                    : _networkShutdownTimedOut
                        ? message + " Network shutdown exceeded the 10-second timeout."
                        : message;
                PerformanceRunStatus finalStatus = fatalShutdown || _networkShutdownTimedOut
                    ? PerformanceRunStatus.FailInstrumentation
                    : PerformanceRunStatus.CompleteMeasured;

                ServerPerformanceProfiler.Stop(finalStatus, finalReasonCode, finalMessage);
                ClearLifetimeState();
                if (fatalShutdown)
                {
                    ShutdownExitCode = 1;
                    _fatalShutdownRequested = false;
                    _fatalShutdownMessage = null;
                }
            }
            finally
            {
                _gracefulShutdownRunning = false;
            }
        }

        /// <summary>Aborts a still-running initialization coroutine when its server host is destroyed.</summary>
        internal static void AbortInitialization()
        {
            if (_fatalShutdownRequested || _gracefulShutdownRunning)
                return;

            ServerControlEndpoint.Detach();
            _timeAuthority = null;
            StopInitializationCoroutine();
            ServerSimulationClock.Stop();

            NetworkManager networkManager = GetNetworkManager();
            TryStopNetworkServer(networkManager);
            CancelAndDisposeMasterStateSynchronously();
            ServerECSManager.Shutdown();
            ServerWorldManager.ClearRuntimeContext();
            DestroyNetworkManager();
            ServerPerformanceProfiler.Stop(
                PerformanceRunStatus.FailInstrumentation,
                "SERVER_INITIALIZATION_ABORTED",
                "Server initialization was aborted before the runtime became ready.");
            _initializationSucceeded = false;
            _launchWorldName = null;
            _profilingRequested = false;
            _host = null;
            ServerShutdownController.Detach();
        }

        private static void StopInitializationCoroutine()
        {
            if (_initializationCoroutine != null && _host != null)
                _host.StopCoroutine(_initializationCoroutine);

            _initializationCoroutine = null;
        }

        private static NetworkManager GetNetworkManager()
        {
            return _networkManagerObject != null
                ? _networkManagerObject.GetComponent<NetworkManager>()
                : null;
        }

        private static void TryStopNetworkServer(NetworkManager networkManager)
        {
            if (networkManager == null || !networkManager.isServer)
                return;

            try
            {
                networkManager.StopServer();
            }
            catch (Exception exception)
            {
                Debug.Error("Failed to request PurrNet server shutdown during abort: " + exception.Message);
            }
        }

        private static IEnumerator StopNetworkServerAndWait(NetworkManager networkManager)
        {
            if (networkManager == null || !networkManager.isServer)
                yield break;

            TryStopNetworkServer(networkManager);
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (networkManager != null
                && networkManager.serverState != ConnectionState.Disconnected)
            {
                if (stopwatch.Elapsed.TotalSeconds >= ServerShutdownController.NETWORK_SHUTDOWN_TIMEOUT_SECONDS)
                {
                    _networkShutdownTimedOut = true;
                    Debug.Warning(
                        "PurrNet server shutdown exceeded the 10-second timeout; continuing teardown.");
                    break;
                }

                yield return null;
            }

            if (!_networkShutdownTimedOut)
                yield return null;
        }

        private static void ClearLifetimeState()
        {
            StopMasterHeartbeatMonitor();
            _masterHeartbeatTask = null;
            DisposeMasterHeartbeatCancellation();
            ClearPersistenceState();
            DisposeMasterWorldClient();
            _timeAuthority = null;
            _initializationCoroutine = null;
            _initializationSucceeded = false;
            _launchWorldName = null;
            _profilingRequested = false;
            _host = null;
        }

        private static void DestroyNetworkManager()
        {
            if (_networkManagerObject == null)
                return;

            GameObject.Destroy(_networkManagerObject);
            _networkManagerObject = null;
        }

        internal static void BeginFatalShutdown(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            if (_fatalShutdownRequested)
                return;

            _fatalShutdownRequested = true;
            _fatalShutdownMessage = exception.Message;
            ServerShutdownController.ClearPendingRequest();
            ServerControlEndpoint.Detach();
            Debug.Error($"Fatal authoritative server simulation failure; shutting down the server runtime.\n{exception}");
            ServerSimulationClock.Stop();

            if (_gracefulShutdownRunning)
                return;

            NetworkManager networkManager = GetNetworkManager();
            StopInitializationCoroutine();

            if (_host == null)
            {
                Debug.Error(
                    "Fatal server shutdown cannot complete because the initialization host is missing. "
                    + "The ECS world remains alive to protect outstanding server callbacks.");
                TryStopNetworkServer(networkManager);
                CancelAndDisposeMasterStateSynchronously();
                DestroyNetworkManager();
                ServerECSManager.Shutdown();
                ServerWorldManager.ClearRuntimeContext();
                ServerPerformanceProfiler.Stop(
                    PerformanceRunStatus.FailInstrumentation,
                    "FATAL_SERVER_SHUTDOWN",
                    exception.Message);
                ClearLifetimeState();
                _fatalShutdownRequested = false;
                _fatalShutdownMessage = null;
                ServerShutdownController.CompleteShutdown(1);
                return;
            }

            _fatalShutdownCoroutine = _host.StartCoroutine(FatalShutdownProcess(networkManager, exception.Message));
        }

        private static IEnumerator FatalShutdownProcess(NetworkManager networkManager, string fatalMessage)
        {
            yield return StopNetworkServerAndWait(networkManager);

            CancelAndDisposePersistenceStateSynchronously();
            yield return StopAndSettleMasterHeartbeat();
            DisposeMasterWorldClient();

            DestroyNetworkManager();
            yield return null;

            ServerECSManager.Shutdown();
            ServerWorldManager.ClearRuntimeContext();
            ServerPerformanceProfiler.Stop(
                PerformanceRunStatus.FailInstrumentation,
                "FATAL_SERVER_SHUTDOWN",
                _networkShutdownTimedOut
                    ? fatalMessage + " Network shutdown exceeded the 10-second timeout."
                    : fatalMessage);
            ClearLifetimeState();
            _fatalShutdownCoroutine = null;
            _fatalShutdownRequested = false;
            _fatalShutdownMessage = null;
            ServerShutdownController.CompleteShutdown(1);
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
