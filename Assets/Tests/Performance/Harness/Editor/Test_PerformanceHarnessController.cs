using Game.Server.Core;
using Game.Shared.Performance;
using Game.Shared.Runtime;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    [Serializable]
    internal sealed class Test_PerformancePendingRunState
    {
        public string runID;
        public string resultsDirectory;
        public string fixturePath;
        public string runPath;
        public string summaryPath;
        public string reportPath;
        public string fixtureSha256;
        public long startedUtcTicks;
        public Test_PerformancePendingRunOptions options;
        public Test_PerformanceRunMetadata metadata;
        public Test_PerformanceSceneSetupState[] previousSceneSetup;
        public string lifecycle;
        public bool stopRequested;
        public bool telemetryStarted;
        public bool finalized;
        public string finalizedStatus;
        public string finalizedReasonCode;
        public string finalizedMessage;
        public string stage14RuntimeWorldID;
        public string stage14RuntimeMapID;
        public string stage14RuntimeInstanceID;
        public int[] stage14RuntimeChunkX;
        public int[] stage14RuntimeChunkZ;
        public bool worldFixtureLoaded;
        public bool preflightCompleted;
        public bool workloadApplied;
        public bool measurementStarted;
        public bool rampCompleted;
        public bool warmupCompleted;
        public bool measurementCompleted;
        public bool recoveryCompleted;
        public bool cleanupComplete;
        public int currentPhase;
        public int rampCompletedEntityCount;
        public int measurementStartEntityCount;
        public int measurementEndEntityCount;
        public int peakEntityCount;
        public bool postCleanupObserved;
        public int postCleanupFixtureEntityCount;
        public int postCleanupActorEntityCount;
        public bool activitySnapshotCaptured;
        public int activityIdleCount;
        public int activityPatrollingCount;
        public int activityCombatCount;
        public int activityWakeSurgeCount;
        public int activitySpawnEventCount;
        public int activityDespawnEventCount;
        public int activityTotalSpawnEventCount;
        public int activityTotalDespawnEventCount;
        public Test_NPCActivityScenarioEvidence stage14NPCActivityEvidence;
        public Test_PerformanceObservabilityState observability;

        public SceneSetup[] RestoreSceneSetup()
        {
            if (previousSceneSetup == null)
                return Array.Empty<SceneSetup>();

            var sceneSetup = new SceneSetup[previousSceneSetup.Length];
            for (var index = 0; index < previousSceneSetup.Length; index++)
                sceneSetup[index] = previousSceneSetup[index].ToSceneSetup();
            return sceneSetup;
        }
    }

    [Serializable]
    internal sealed class Test_PerformancePendingRunOptions
    {
        public string profileID;
        public int worldFixture;
        public int densityTier;
        public int workloadMode;
        public int actorCount;
        public int npcActivityScenario;
        public int seed;
        public int graphicalScenario;
        public int actorDensity;
        public float warmupSeconds;
        public float measurementSeconds;
        public static Test_PerformancePendingRunOptions Capture(Test_PerformanceHarnessOptions options)
        {
            return new Test_PerformancePendingRunOptions
            {
                profileID = options.ProfileID,
                worldFixture = (int)options.WorldFixture,
                densityTier = (int)options.DensityTier,
                workloadMode = (int)options.WorkloadMode,
                actorCount = options.ActorCount,
                npcActivityScenario = (int)options.NPCActivityScenario,
                seed = options.Seed,
                graphicalScenario = (int)options.GraphicalScenario,
                actorDensity = (int)options.ActorDensity,
                warmupSeconds = options.WarmupSeconds,
                measurementSeconds = options.MeasurementSeconds,
            };
        }

        public Test_PerformanceHarnessOptions ToOptions()
        {
            return new Test_PerformanceHarnessOptions
            {
                ProfileID = profileID,
                WorldFixture = (Test_WorldFixtureVariant)worldFixture,
                DensityTier = (Test_WorldFixtureDensityTier)densityTier,
                WorkloadMode = (Test_PerformanceWorkloadMode)workloadMode,
                ActorCount = actorCount,
                NPCActivityScenario = (Test_NPCActivityScenario)npcActivityScenario,
                Seed = seed,
                GraphicalScenario = (Test_GraphicalScenario)graphicalScenario,
                ActorDensity = (Test_GraphicalActorDensity)actorDensity,
                WarmupSeconds = warmupSeconds,
                MeasurementSeconds = measurementSeconds,
            };
        }
    }

    [Serializable]
    internal sealed class Test_PerformanceSceneSetupState
    {
        public string path;
        public bool isLoaded;
        public bool isActive;
        public bool isSubScene;

        public static Test_PerformanceSceneSetupState[] Capture(SceneSetup[] sceneSetup)
        {
            if (sceneSetup == null)
                return Array.Empty<Test_PerformanceSceneSetupState>();

            var result = new Test_PerformanceSceneSetupState[sceneSetup.Length];
            for (var index = 0; index < sceneSetup.Length; index++)
            {
                result[index] = new Test_PerformanceSceneSetupState
                {
                    path = sceneSetup[index].path,
                    isLoaded = sceneSetup[index].isLoaded,
                    isActive = sceneSetup[index].isActive,
                    isSubScene = sceneSetup[index].isSubScene,
                };
            }
            return result;
        }

        public SceneSetup ToSceneSetup()
        {
            return new SceneSetup
            {
                path = path,
                isLoaded = isLoaded,
                isActive = isActive,
                isSubScene = isSubScene,
            };
        }
    }

    /// <summary>Coordinates the Editor-only scene and Play Mode lifecycle for one run.</summary>
    [InitializeOnLoad]
    internal static class Test_PerformanceHarnessController
    {
        private const string PENDING_RUN_SESSION_KEY = "Game.Tests.Performance.Harness.PendingRun";
        private const string STARTING_LIFECYCLE = "starting";
        private const string RUNNING_LIFECYCLE = "running";
        private const string FINALIZED_LIFECYCLE = "finalized";

        private static SceneSetup[] _previousSceneSetup;
        private static Test_PerformanceRun _activeRun;

        static Test_PerformanceHarnessController()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            RestorePendingRun();
            if (EditorApplication.isPlaying && IsRunning)
                EditorApplication.delayCall += ResumeRuntimeAfterReload;
        }

        public static event Action StateChanged;

        internal static event Action LifecycleSettled;

        public static bool IsRunning => _activeRun != null && !_activeRun.Finalized;

        public static string ActiveRunID => IsRunning ? _activeRun.RunID : null;

        public static string ActiveResultDirectory => IsRunning ? _activeRun.ResultsDirectory : null;

        public static string LastCompletedRunID { get; private set; }

        public static string LastCompletedResultDirectory { get; private set; }

        public static string Status { get; private set; } = "Idle";

        public static string LastReportPath { get; private set; }

        internal static bool TryValidateLaunchPrerequisites(Test_PerformanceHarnessOptions options, out string diagnostic)
        {
            diagnostic = null;
            try
            {
                if (options == null)
                    return FailLaunchValidation("A performance workload configuration is required.", out diagnostic);
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return FailLaunchValidation("Stop the current Play Mode session before launching a performance workload.", out diagnostic);
                if (!options.TryValidate(out diagnostic, out _, out _))
                    return false;
                if (!CanChangeScenes(out diagnostic))
                    return false;

                var harnessScenePath = options.IsGraphicalClient
                    ? Test_GraphicalFixturePreflight.CLIENT_BOOTSTRAP_SCENE_PATH
                    : Test_PerformanceRun.HARNESS_SCENE_PATH;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(harnessScenePath) == null)
                    return FailLaunchValidation("Required Play Mode entry scene is missing or not imported: " + harnessScenePath, out diagnostic);
                return true;
            }
            catch (Exception exception)
            {
                return FailLaunchValidation("Launch preflight failed: " + exception.Message, out diagnostic);
            }
        }

        internal static void PersistActiveRun(string lifecycle)
        {
            if (_activeRun == null)
                return;

            var state = _activeRun.CreatePendingRunState(_previousSceneSetup, lifecycle);
            SessionState.SetString(PENDING_RUN_SESSION_KEY, JsonUtility.ToJson(state));
        }

        public static void Start(Test_PerformanceHarnessOptions options)
        {
            StartInternal(options, 0, null);
        }

        internal static void StartQueued(
            Test_PerformanceHarnessOptions options,
            long queueDispatchTimestamp,
            DateTime queueDispatchUtc)
        {
            StartInternal(options, queueDispatchTimestamp, queueDispatchUtc);
        }

        private static void StartInternal(
            Test_PerformanceHarnessOptions options,
            long queueDispatchTimestamp,
            DateTime? queueDispatchUtc)
        {
            RestorePendingRun();
            if (IsRunning)
            {
                Status = "A performance run is already active.";
                StateChanged?.Invoke();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Status = "Stop the current Play Mode session before starting a workload run.";
                StateChanged?.Invoke();
                return;
            }

            Test_PerformanceRun run = null;
            try
            {
                run = Test_PerformanceRun.Create(options);
                if (queueDispatchTimestamp > 0 && queueDispatchUtc.HasValue)
                    run.MarkQueueDispatch(queueDispatchTimestamp, queueDispatchUtc.Value);
                else
                    run.MarkQueueDispatch(System.Diagnostics.Stopwatch.GetTimestamp(), DateTime.UtcNow);
                run.Validate();
                if (!string.IsNullOrEmpty(run.ValidationDiagnostic))
                {
                    run.Block(run.ValidationDiagnostic);
                    RememberCompletedRun(run);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: " + run.ValidationDiagnostic;
                    StateChanged?.Invoke();
                    return;
                }

                if (!CanChangeScenes(out var dirtySceneDiagnostic))
                {
                    run.Block(dirtySceneDiagnostic);
                    RememberCompletedRun(run);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: " + dirtySceneDiagnostic;
                    StateChanged?.Invoke();
                    return;
                }

                var harnessScenePath = run.IsGraphicalClient
                    ? Test_GraphicalFixturePreflight.CLIENT_BOOTSTRAP_SCENE_PATH
                    : Test_PerformanceRun.HARNESS_SCENE_PATH;
                var harnessScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(harnessScenePath);
                if (harnessScene == null)
                {
                    run.Block("Required Play Mode entry scene is missing or not imported: " + harnessScenePath);
                    RememberCompletedRun(run);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: Play Mode entry scene unavailable";
                    StateChanged?.Invoke();
                    return;
                }

                _previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
                _activeRun = run;
                PersistActiveRun(STARTING_LIFECYCLE);
                var opened = EditorSceneManager.OpenScene(harnessScenePath, OpenSceneMode.Single);
                if (!opened.IsValid())
                    throw new InvalidOperationException("Unity could not open the test-owned performance harness scene.");

                Status = run.IsGraphicalClient
                    ? "Starting ordinary graphical client Play Mode..."
                    : "Starting ordinary server Play Mode...";
                LastReportPath = run.ReportPath;
                StateChanged?.Invoke();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                if (run != null && !run.Finalized)
                {
                    try
                    {
                        run.Fail(exception, "RUNNER_START_FAILED", PerformanceRunStatus.FailProvision);
                    }
                    catch (Exception finalizationException)
                    {
                        UnityEngine.Debug.LogException(finalizationException);
                    }
                }
                if (run != null && run.Finalized)
                    RememberCompletedRun(run);
                RestorePreviousSceneSetup(out _);
                _activeRun = null;
                _previousSceneSetup = null;
                ClearPendingRun();
                if (run != null)
                    LastReportPath = run.ReportPath;
                Status = "Could not start: " + exception.Message;
                StateChanged?.Invoke();
                UnityEngine.Debug.LogException(exception);
            }
        }

        public static void RequestStop()
        {
            if (_activeRun == null)
                return;
            _activeRun.StopRequested = true;
            PersistActiveRun(RUNNING_LIFECYCLE);
            Status = "Stop requested; waiting for fixture cleanup...";
            StateChanged?.Invoke();
        }

        internal static void Complete(Test_PerformanceRun run)
        {
            if (!ReferenceEquals(run, _activeRun))
                return;
            run.MarkWorkloadFinalized();
            LastReportPath = run.ReportPath;
            RememberCompletedRun(run);
            Status = "Finished: " + run.ReportPath;
            PersistActiveRun(FINALIZED_LIFECYCLE);
            StateChanged?.Invoke();
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RestorePendingRun();
                if (_activeRun == null || _activeRun.Finalized)
                    return;

                _activeRun.MarkPlayModeEntered();
                PersistActiveRun(RUNNING_LIFECYCLE);
                StartRuntimeRunner();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_activeRun != null && !_activeRun.Finalized)
                    _activeRun.AbortFromPlayModeExit();

                if (_activeRun != null && _activeRun.Finalized)
                    _activeRun.MarkPlayModeExited();

                var sceneRestored = RestorePreviousSceneSetup(out var sceneRestoreDiagnostic);
                if (_activeRun != null && _activeRun.Finalized)
                {
                    if (sceneRestored)
                        _activeRun.MarkSceneRestored();
                    else
                        _activeRun.MarkSceneRestoreFailed(sceneRestoreDiagnostic);
                    _activeRun.CompleteEditorLifecycle();
                }
                ClearPendingRun();
                _previousSceneSetup = null;
                _activeRun = null;
                StateChanged?.Invoke();
                LifecycleSettled?.Invoke();
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            if (_activeRun == null)
                return;

            try
            {
                PersistActiveRun(_activeRun.Finalized ? FINALIZED_LIFECYCLE : RUNNING_LIFECYCLE);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }

        private static void RestorePendingRun()
        {
            var stateJson = SessionState.GetString(PENDING_RUN_SESSION_KEY, null);
            if (string.IsNullOrEmpty(stateJson))
                return;

            Test_PerformancePendingRunState state;
            try
            {
                state = JsonUtility.FromJson<Test_PerformancePendingRunState>(stateJson);
                if (state == null || string.IsNullOrEmpty(state.runID) || state.options == null || state.metadata == null)
                    throw new InvalidOperationException("The pending performance run state is incomplete.");

                _previousSceneSetup = state.RestoreSceneSetup();
                LastReportPath = state.reportPath;
                if (state.finalized || state.lifecycle == FINALIZED_LIFECYCLE)
                {
                    state.finalized = true;
                    _activeRun = Test_PerformanceRun.Restore(state);
                    Status = "Finished: " + state.reportPath;
                    LastCompletedRunID = state.runID;
                    LastCompletedResultDirectory = state.resultsDirectory;
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        CompleteFinalizedRunAfterReload();
                        _previousSceneSetup = null;
                        ClearPendingRun();
                    }
                    return;
                }

                var shouldResumePlayTransition = EditorApplication.isPlayingOrWillChangePlaymode
                    && state.lifecycle != FINALIZED_LIFECYCLE;
                if (!shouldResumePlayTransition)
                {
                    var staleRun = Test_PerformanceRun.Restore(state);
                    staleRun.Fail(
                        new InvalidOperationException("The Editor domain reloaded after the performance run had started, so the run was finalized as incomplete."),
                        "EDITOR_RELOAD_INTERRUPTED",
                        PerformanceRunStatus.FailInstrumentation);
                    RememberCompletedRun(staleRun);
                    _activeRun = staleRun;
                    RestorePreviousSceneSetup(out _);
                    _previousSceneSetup = null;
                    ClearPendingRun();
                    Status = "Run interrupted by Editor reload: " + staleRun.ReportPath;
                    return;
                }

                _activeRun = Test_PerformanceRun.Restore(state);
                Status = "Restored pending performance run for Play Mode.";
            }
            catch (Exception exception)
            {
                RestorePreviousSceneSetup(out _);
                ClearPendingRun();
                _previousSceneSetup = null;
                _activeRun = null;
                Status = "Pending performance run could not be restored: " + exception.Message;
                UnityEngine.Debug.LogException(exception);
            }
            StateChanged?.Invoke();
        }

        private static void CompleteFinalizedRunAfterReload()
        {
            if (_activeRun == null || !_activeRun.Finalized)
                return;

            _activeRun.MarkPlayModeExited();
            var sceneRestored = RestorePreviousSceneSetup(out var diagnostic);
            if (sceneRestored)
                _activeRun.MarkSceneRestored();
            else
                _activeRun.MarkSceneRestoreFailed(diagnostic);
            _activeRun.CompleteEditorLifecycle();
            RememberCompletedRun(_activeRun);
            LastReportPath = _activeRun.ReportPath;
            _activeRun = null;
        }

        private static bool RestorePreviousSceneSetup(out string diagnostic)
        {
            diagnostic = null;
            if (_previousSceneSetup == null || _previousSceneSetup.Length == 0)
                return true;

            try
            {
                EditorSceneManager.RestoreSceneManagerSetup(_previousSceneSetup);
                return true;
            }
            catch (Exception exception)
            {
                diagnostic = "Previous Editor scene setup could not be restored: " + exception.Message;
                Status = "Run finished, but the previous Editor scene setup could not be restored: " + exception.Message;
                UnityEngine.Debug.LogException(exception);
                return false;
            }
        }

        private static void ClearPendingRun()
        {
            SessionState.EraseString(PENDING_RUN_SESSION_KEY);
        }

        private static bool FailLaunchValidation(string message, out string diagnostic)
        {
            diagnostic = message;
            return false;
        }

        private static void RememberCompletedRun(Test_PerformanceRun run)
        {
            if (run == null)
                return;
            LastCompletedRunID = run.RunID;
            LastCompletedResultDirectory = run.ResultsDirectory;
        }

        private static void ResumeRuntimeAfterReload()
        {
            EditorApplication.delayCall -= ResumeRuntimeAfterReload;
            if (!EditorApplication.isPlaying || !IsRunning)
                return;

            StartRuntimeRunner();
        }

        private static void StartRuntimeRunner()
        {
            var existingRunner = UnityEngine.Object.FindAnyObjectByType<Test_PerformanceHarnessBehaviour>();
            if (existingRunner != null)
            {
                if (existingRunner.IsInitialized)
                    return;

                UnityEngine.Object.Destroy(existingRunner.gameObject);
            }

            var runnerObject = CreateRuntimeRunnerObject();
            runnerObject.AddComponent<Test_PerformanceHarnessBehaviour>().Initialize(_activeRun);
        }

        internal static GameObject CreateRuntimeRunnerObject()
        {
            var runnerObject = new GameObject("Test_PerformanceHarnessRuntime");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            return runnerObject;
        }

        private static bool CanChangeScenes(out string diagnostic)
        {
            foreach (var setup in EditorSceneManager.GetSceneManagerSetup())
            {
                if (!setup.isLoaded)
                    continue;
                var scene = SceneManager.GetSceneByPath(setup.path);
                if (scene.IsValid() && scene.isDirty)
                {
                    diagnostic = "The active Editor scene setup contains unsaved changes. Save or close those scenes before running the test-owned harness.";
                    return false;
                }
            }

            diagnostic = null;
            return true;
        }
    }

    /// <summary>Runs one real server-only fixture execution inside ordinary Play Mode.</summary>
    internal sealed class Test_PerformanceHarnessBehaviour : MonoBehaviour
    {
        private const float STAGE14_ENGAGEMENT_SENTINEL_TIMEOUT_SECONDS = 10f;

        private Test_PerformanceRun _run;
        private Test_ActorFixtureRuntime _fixture;
        private Test_NPCActivityScenarioController _npcActivityController;
        private Test_NPCActivityPlayerCapture _npcActivityPlayerCapture;
        private Test_GraphicalFixtureRuntime _graphicalFixture;
        private Scene _selectedWorldScene;
        private bool _cleanupCompleted;
        private bool _sceneUnloadAttempted;
        private Exception _pendingFailure;
        private string _pendingFailureReason;
        private PerformanceRunStatus _pendingFailureStatus;
        private PerformanceRunStatus? _requestedStatus;
        private string _requestedReason;
        private string _requestedMessage;

        public bool IsInitialized => _run != null;

        private void OnApplicationFocus(bool hasFocus)
        {
            _run?.RecordFocusState(hasFocus, true);
        }

        public void Initialize(Test_PerformanceRun run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            StartCoroutine(Execute());
        }

        private IEnumerator Execute()
        {
            try
            {
                yield return DriveNested(ExecuteCore(), RecordExecutionFailure);
                if ((_fixture != null || _graphicalFixture != null) && !_cleanupCompleted)
                    yield return DriveNested(CleanupFixture(), RecordCleanupFailure);
                if (_selectedWorldScene.IsValid() && _selectedWorldScene.isLoaded && !_sceneUnloadAttempted)
                    yield return DriveNested(UnloadSelectedWorldFixture(), RecordSceneCleanupFailure);

                RestoreStage14PlayerHealth();
                FinalizeRun();

                Test_PerformanceHarnessController.Complete(_run);
                Destroy(gameObject);
            }
            finally
            {
                RestoreStage14PlayerHealth();
            }
        }

        private void FinalizeRun()
        {
            if (_run.Finalized)
                return;

            try
            {
                if (_pendingFailure != null)
                    _run.Fail(_pendingFailure, _pendingFailureReason, _pendingFailureStatus);
                else if (_requestedStatus.HasValue)
                    _run.Complete(_requestedStatus.Value, _requestedReason, _requestedMessage);
                else
                    _run.Fail(new InvalidOperationException("The runner exited before finalizing a result."), "RUNNER_INCOMPLETE", PerformanceRunStatus.FailInstrumentation);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                if (_run.Finalized)
                    return;

                try
                {
                    _run.Fail(exception, "FINALIZATION_FAILED", PerformanceRunStatus.FailInstrumentation);
                }
                catch (Exception finalizationException)
                {
                    UnityEngine.Debug.LogException(finalizationException);
                }
            }
        }

        private IEnumerator ExecuteCore()
        {
            _run.StartTelemetry();
            Test_PerformanceHarnessController.PersistActiveRun("running");
            if (_run.IsGraphicalClient)
            {
                yield return ExecuteGraphicalCore();
                yield break;
            }

            yield return WaitForServerReady();
            if (_run.IsStage14NPCActivity)
            {
                yield return ExecuteStage14NPCActivityCore();
                yield break;
            }
            _run.RegisterServerMetrics();
            yield return LoadSelectedWorldFixture();
            _run.CreateDataOnlyRuntime(_run.Options.WorkloadMode);
            _fixture = _run.FixtureRuntime;
            Test_PerformanceHarnessController.PersistActiveRun("running");

            _run.BeginPhase(PerformanceMeasurementPhase.Ramp);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunRamp();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the run during the population ramp; this is not a timeout.");
                yield break;
            }

            _run.ActivityController.PrepareTimedStateStress();
            _run.MarkWorkloadApplied();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Ramp);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Warmup);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunWarmup();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the run during warmup; this is not a timeout.");
                yield break;
            }

            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Warmup);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Measurement);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunMeasurement();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the run during measurement; this is not a timeout.");
                yield break;
            }

            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Measurement);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Recovery);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunRecovery();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Recovery);
            yield return CleanupFixture();
            SetRequestedResult(
                PerformanceRunStatus.CompleteMeasured,
                "MEASUREMENT_COMPLETE",
                "The requested data-only population was measured and cleaned up successfully.");
        }

        private IEnumerator ExecuteStage14NPCActivityCore()
        {
            yield return WaitForStage14Players();
            _npcActivityPlayerCapture.ProtectHealth();
            ValidateStage14RuntimeAuthority();
            _run.RegisterServerMetrics();

            var prefab = Test_ActorFixturePreflight.ValidateFull(
                _run.ActorProfile,
                _npcActivityPlayerCapture.Players.Count,
                ServerWorldManager.Instance.GlobalActorsSceneID);
            var passiveAggroRange = Test_ActorFixturePreflight.GetNPCPassiveAggroRange(prefab);
            var placements = Test_ActorFixtureLayout.BuildStage14NPCActivityPlacements(
                _run.ActorProfile,
                _run.Options.NPCActivityScenario,
                _npcActivityPlayerCapture.AnchorPosition,
                passiveAggroRange);
            var requiredChunks = BuildStage14RequiredChunks(placements);
            var requiredChunkCoordinates = new List<Vector2Int>(requiredChunks.Count);
            foreach (var requiredChunk in requiredChunks)
                requiredChunkCoordinates.Add(requiredChunk.ChunkCoord);
            _run.SetStage14RuntimeEnvironment(
                ServerWorldManager.Instance.CurrentRuntimeContext,
                requiredChunkCoordinates);
            yield return EnsureStage14PlacementChunks(requiredChunks);
            var resolvedPlacements = Test_ActorFixturePreflight.ResolveAuthoritativeGround(
                placements,
                _run.Options.NPCActivityScenario);
            if (ServerWorldManager.Instance.CurrentRuntimeContext != _run.Stage14RuntimeContext)
                throw new Test_ActorFixturePreconditionException("Stage 1.4 ordinary authoritative runtime context changed before NPC spawn.");
            _fixture = new Test_ActorFixtureRuntime(_run.ActorProfile);
            _fixture.StartFullActors(
                ServerWorldManager.Instance.GlobalActorsSceneID,
                _npcActivityPlayerCapture.Anchor,
                resolvedPlacements);
            _run.SetFullNetworkedRuntime(_fixture);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return _fixture.WaitForFullSpawn(_run.EffectivePreflightTimeoutSeconds);
            _npcActivityController = new Test_NPCActivityScenarioController(
                _run.Options.NPCActivityScenario,
                _npcActivityPlayerCapture,
                _run.ActorProfile,
                resolvedPlacements,
                passiveAggroRange);
            _run.AttachStage14NPCActivityEvidence(_npcActivityController, _fixture.SpawnedFullActorCount);
            var sentinelActors = _fixture.StartEngagementForGroup("npc-engaged", 1);
            yield return RunStage14EngagementSentinel(sentinelActors);

            _run.BeginPhase(PerformanceMeasurementPhase.Ramp);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunStage14Ramp();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the Stage 1.4 run during ramp.");
                yield break;
            }
            _run.MarkWorkloadApplied();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Ramp);

            _run.BeginPhase(PerformanceMeasurementPhase.Warmup);
            yield return RunStage14Warmup();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the Stage 1.4 run during warmup.");
                yield break;
            }
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Warmup);

            _run.BeginPhase(PerformanceMeasurementPhase.Measurement);
            yield return RunStage14Measurement();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the Stage 1.4 run during measurement.");
                yield break;
            }
            _npcActivityController.ValidateMeasurementComplete();
            _run.RefreshStage14NPCActivityEvidence(_npcActivityController, _fixture.SpawnedFullActorCount);
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Measurement);

            _run.BeginPhase(PerformanceMeasurementPhase.Recovery);
            yield return RunStage14Recovery();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Recovery);
            yield return CleanupFixture();
            SetRequestedResult(
                PerformanceRunStatus.CompleteMeasured,
                "MEASUREMENT_COMPLETE",
                "The Stage 1.4 full networked NPC activity workload was measured and cleaned up successfully.");
        }

        private IEnumerator RunStage14EngagementSentinel(IReadOnlyList<Actor> sentinelActors)
        {
            var deadline = Time.realtimeSinceStartup + STAGE14_ENGAGEMENT_SENTINEL_TIMEOUT_SECONDS;
            try
            {
                while (!_fixture.IsEngagementResponsive(sentinelActors))
                {
                    ThrowIfStopRequested();
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        throw new Test_NPCActivityWorkloadNotAchievedException(
                            "WORKLOAD_NOT_ACHIEVED: The nearby Stage 1.4 engagement sentinel did not enter the ordinary target/auto-attack path within the bounded sentinel timeout.");
                    }

                    yield return null;
                }
            }
            finally
            {
                _fixture.StopEngagementForGroup("npc-engaged");
            }

            var observationSeconds = GetSentinelReacquisitionObservationSeconds(sentinelActors);
            var observationDeadline = Time.realtimeSinceStartup + observationSeconds;
            while (Time.realtimeSinceStartup < observationDeadline)
            {
                ThrowIfStopRequested();
                _npcActivityController.ValidatePlayerReferencesForSentinel();
                yield return null;
            }

            _fixture.ValidateEngagementStopped(sentinelActors);
            _npcActivityController.ValidatePlayerReferencesForSentinel();
        }

        private static float GetSentinelReacquisitionObservationSeconds(IReadOnlyList<Actor> sentinelActors)
        {
            if (sentinelActors == null || sentinelActors.Count != 1 || sentinelActors[0] == null)
                throw new InvalidOperationException("Stage 1.4 requires exactly one live engagement sentinel.");

            var behavior = sentinelActors[0].GetComponent<NPCBehavior>();
            if (behavior == null)
                throw new Test_ActorFixturePreconditionException("The Stage 1.4 engagement sentinel has no NPCBehavior component.");
            if (!Game.Shared.GameConfigurationManager.IsInitialized
                || Game.Shared.GameConfigurationManager.Config == null)
            {
                throw new Test_ActorFixturePreconditionException(
                    "Stage 1.4 cannot observe sentinel reacquisition without initialized game configuration.");
            }

            var configuration = Game.Shared.GameConfigurationManager.Config;
            var observationSeconds = behavior.AggroDelay
                + configuration.NPCAggroDecisionIntervalSeconds
                + configuration.ServerSimulationTickDuration;
            if (float.IsNaN(observationSeconds)
                || float.IsInfinity(observationSeconds)
                || observationSeconds <= 0f)
            {
                throw new Test_ActorFixturePreconditionException(
                    "Stage 1.4 sentinel reacquisition observation window is not finite and positive.");
            }

            return observationSeconds;
        }

        private void ValidateStage14RuntimeAuthority()
        {
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                throw new Test_ActorFixturePreconditionException("Stage 1.4 requires the ordinary authoritative ServerWorldManager.");

            var runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            if (!runtimeContext.IsValid)
                throw new Test_ActorFixturePreconditionException("Stage 1.4 requires a valid ordinary authoritative server runtime context.");
            if (_npcActivityPlayerCapture == null || _npcActivityPlayerCapture.Anchor == null)
                throw new Test_ActorFixturePreconditionException("Stage 1.4 did not capture a real authenticated anchor PlayerActor.");
            if (_npcActivityPlayerCapture.Context != runtimeContext
                || _npcActivityPlayerCapture.Anchor.SpatialContext != runtimeContext)
            {
                throw new Test_ActorFixturePreconditionException(
                    $"Stage 1.4 anchor PlayerActor '{_npcActivityPlayerCapture.Anchor.ID}' is not in the ordinary authoritative runtime context '{runtimeContext.WorldID}/{runtimeContext.MapID}/{runtimeContext.InstanceID}'.");
            }
            foreach (var player in _npcActivityPlayerCapture.Players)
            {
                if (player == null
                    || player.SpatialContext != runtimeContext
                    || !player.TryGetAcceptedPositionSnapshot(out var playerSnapshot)
                    || !playerSnapshot.Position.IsFinite)
                {
                    throw new Test_ActorFixturePreconditionException(
                        $"Stage 1.4 real player '{player?.ID.ToString() ?? "<null>"}' is not represented by a finite accepted position in the ordinary authoritative runtime context.");
                }
            }
            if (!_npcActivityPlayerCapture.Anchor.TryGetAcceptedPositionSnapshot(out var snapshot)
                || !snapshot.Position.IsFinite)
            {
                throw new Test_ActorFixturePreconditionException(
                    $"Stage 1.4 anchor PlayerActor '{_npcActivityPlayerCapture.Anchor.ID}' has no finite accepted authoritative position.");
            }

            _npcActivityPlayerCapture.AnchorPosition = snapshot.Position;
        }

        private List<WorldChunkKey> BuildStage14RequiredChunks(IReadOnlyList<Test_ActorFixturePlacement> placements)
        {
            var runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            var requiredChunks = new HashSet<WorldChunkKey>();
            foreach (var placement in placements)
            {
                if (placement == null)
                    throw new Test_ActorFixturePreconditionException("Stage 1.4 placement generation returned a null placement before ordinary-world chunk provisioning.");

                var position = new WorldPosition(placement.world_x, placement.world_y, placement.world_z);
                if (!position.IsFinite)
                {
                    throw new Test_ActorFixturePreconditionException(
                        $"Stage 1.4 placement '{placement.actor_fixture_id}' has a non-finite position before ordinary-world chunk provisioning: ({placement.world_x:R}, {placement.world_y:R}, {placement.world_z:R}).");
                }

                requiredChunks.Add(new WorldChunkKey(runtimeContext, position.Chunk));
            }

            var orderedChunks = new List<WorldChunkKey>(requiredChunks);
            orderedChunks.Sort((left, right) =>
            {
                var xComparison = left.ChunkCoord.x.CompareTo(right.ChunkCoord.x);
                return xComparison != 0 ? xComparison : left.ChunkCoord.y.CompareTo(right.ChunkCoord.y);
            });
            return orderedChunks;
        }

        private IEnumerator EnsureStage14PlacementChunks(IReadOnlyList<WorldChunkKey> requiredChunks)
        {
            var worldManager = ServerWorldManager.Instance;
            var runtimeContext = worldManager.CurrentRuntimeContext;
            foreach (var requiredChunk in requiredChunks)
            {
                if (requiredChunk.Context != runtimeContext)
                    throw new Test_ActorFixturePreconditionException("Stage 1.4 required ordinary-world chunks do not match the current authoritative runtime context.");
            }
            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            foreach (var requiredChunk in requiredChunks)
            {
                var chunk = requiredChunk.ChunkCoord;
                if (worldManager.IsChunkLoaded(runtimeContext, chunk))
                    continue;

                var completed = false;
                var succeeded = false;
                try
                {
                    worldManager.EnsureChunkLoaded(
                        runtimeContext,
                        chunk,
                        (success, _) =>
                        {
                            succeeded = success;
                            completed = true;
                        });
                }
                catch (Exception exception)
                {
                    throw new Test_ActorFixturePreconditionException(
                        $"Stage 1.4 ordinary-world chunk '{chunk}' could not be loaded through ServerWorldManager: {exception.Message}");
                }

                while (!completed)
                {
                    ThrowIfStopRequested();
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        throw new Test_ActorFixturePreconditionException(
                            $"Stage 1.4 ordinary-world chunk '{chunk}' did not load through ServerWorldManager within the bounded preflight timeout.");
                    }
                    yield return null;
                }

                if (!succeeded || !worldManager.IsChunkLoaded(runtimeContext, chunk))
                {
                    throw new Test_ActorFixturePreconditionException(
                        $"Stage 1.4 ordinary-world chunk '{chunk}' failed to load through ServerWorldManager before NPC spawn.");
                }
            }

            if (worldManager.CurrentRuntimeContext != runtimeContext)
                throw new Test_ActorFixturePreconditionException("Stage 1.4 ordinary authoritative runtime context changed during chunk provisioning.");
        }

        private IEnumerator ExecuteGraphicalCore()
        {
            yield return WaitForClientReady();
            _run.RegisterClientMetrics();
            _run.RefreshGraphicalRunEvidence();
            yield return LoadSelectedWorldFixture();
            _graphicalFixture = _run.CreateGraphicalRuntime();
            _graphicalFixture.AttachCamera(Camera.main);
            _graphicalFixture.ValidateLoadedScene(_selectedWorldScene);
            Test_PerformanceHarnessController.PersistActiveRun("running");

            _run.BeginPhase(PerformanceMeasurementPhase.Ramp);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunGraphicalRamp();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the graphical run during ramp; this is not a timeout.");
                yield break;
            }

            _run.MarkWorkloadApplied();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Ramp);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Warmup);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunGraphicalWarmup();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the graphical run during warmup; this is not a timeout.");
                yield break;
            }

            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Warmup);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Measurement);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunGraphicalMeasurement();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailCorrectness, "The user deliberately stopped the graphical run during measurement; this is not a timeout.");
                yield break;
            }

            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Measurement);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            _run.BeginPhase(PerformanceMeasurementPhase.Recovery);
            Test_PerformanceHarnessController.PersistActiveRun("running");
            yield return RunGraphicalRecovery();
            _run.MarkPhaseComplete(PerformanceMeasurementPhase.Recovery);
            yield return CleanupFixture();

            if (!_graphicalFixture.RouteComplete)
            {
                SetRequestedResult(
                    PerformanceRunStatus.FailCorrectness,
                    "WORKLOAD_NOT_ACHIEVED",
                    $"The graphical route reached {_graphicalFixture.RouteWaypointsReached} of {_graphicalFixture.Plan.Waypoints.Length} waypoints.");
            }
            else
            {
                SetRequestedResult(
                    PerformanceRunStatus.CompleteMeasured,
                    "MEASUREMENT_COMPLETE",
                    "The selected graphical fixture was rendered through the ordinary client path and the deterministic camera route completed.");
            }
        }

        private IEnumerator DriveNested(IEnumerator root, Action<Exception> onException)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var currentEnumerator = stack.Peek();
                object current;
                try
                {
                    if (!currentEnumerator.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }
                    current = currentEnumerator.Current;
                }
                catch (Exception exception)
                {
                    onException(exception);
                    yield break;
                }

                if (current is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return current;
            }
        }

        private void RecordExecutionFailure(Exception exception)
        {
            SetPendingFailure(exception, ClassifyFailure(exception));
        }

        private void RecordCleanupFailure(Exception exception)
        {
            SetPendingFailure(exception, new Test_PerformanceFailureClassification("CLEANUP_FAILED", PerformanceRunStatus.FailCorrectness));
        }

        private void RecordSceneCleanupFailure(Exception exception)
        {
            SetPendingFailure(exception, new Test_PerformanceFailureClassification("SCENE_CLEANUP_FAILED", PerformanceRunStatus.FailCorrectness));
        }

        private void RestoreStage14PlayerHealth()
        {
            if (_npcActivityPlayerCapture == null)
                return;

            try
            {
                _npcActivityPlayerCapture.RestoreHealth();
            }
            catch (Exception exception)
            {
                if (_run != null && !_run.Finalized)
                    RecordCleanupFailure(exception);
                else
                    UnityEngine.Debug.LogException(exception);
            }
        }

        private void SetPendingFailure(Exception exception, Test_PerformanceFailureClassification classification)
        {
            if (_pendingFailure == null)
            {
                _pendingFailure = exception;
                _pendingFailureReason = classification.ReasonCode;
                _pendingFailureStatus = classification.Status;
                return;
            }

            _pendingFailure = new AggregateException("The performance run and its teardown both failed.", _pendingFailure, exception);
            _pendingFailureReason = "CLEANUP_FAILED";
        }

        private void SetRequestedResult(PerformanceRunStatus status, string reasonCode, string message)
        {
            _requestedStatus = status;
            _requestedReason = reasonCode;
            _requestedMessage = message;
        }

        private static Test_PerformanceFailureClassification ClassifyFailure(Exception exception)
        {
            if (exception is Test_PerformanceUserStopException)
                return new Test_PerformanceFailureClassification("USER_STOPPED", PerformanceRunStatus.FailCorrectness);
            if (exception is Test_NPCActivityWorkloadNotAchievedException)
                return new Test_PerformanceFailureClassification("WORKLOAD_NOT_ACHIEVED", PerformanceRunStatus.FailCorrectness);
            if (exception is Test_ActorFixturePreconditionException)
                return new Test_PerformanceFailureClassification("FIXTURE_PRECONDITION_FAILED", PerformanceRunStatus.FailProvision);
            if (exception is Test_PerformancePrerequisiteException)
                return new Test_PerformanceFailureClassification("PREREQUISITE_BLOCKED", PerformanceRunStatus.BlockedPrerequisite);
            if (exception is TimeoutException)
                return new Test_PerformanceFailureClassification("TIMEOUT", PerformanceRunStatus.FailTimeout);
            if (exception is Test_PerformanceResourceCeilingException)
                return new Test_PerformanceFailureClassification("RESOURCE_CEILING", PerformanceRunStatus.FailResource);
            return new Test_PerformanceFailureClassification("RUNNER_EXCEPTION", PerformanceRunStatus.FailCorrectness);
        }

        private readonly struct Test_PerformanceFailureClassification
        {
            public Test_PerformanceFailureClassification(string reasonCode, PerformanceRunStatus status)
            {
                ReasonCode = reasonCode;
                Status = status;
            }

            public string ReasonCode { get; }

            public PerformanceRunStatus Status { get; }
        }

        private IEnumerator WaitForServerReady()
        {
            ThrowIfStopRequested();
            ValidateStage14RuntimeRole();
            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            while (!Game.Shared.GameConfigurationManager.IsInitialized
                || !ServerECSManager.Initialized
                || !ServerWorldManager.IsInitialized
                || ServerWorldManager.Instance == null
                || !ServerWorldManager.Instance.IsGlobalActorsSceneLoaded
                || !ServerInterestManager.IsInitialized
                || !ServerPositionManager.IsInitialized
                || !ServerSpawnManager.IsInitialized
                || !ServerActorManager.IsInitialized
                || !ServerStatManager.IsInitialized
                || !ServerSpellCastManager.IsInitialized
                || !ServerAuraManager.IsInitialized
                || !ServerTickerManager.IsInitialized
                || !ServerCooldownManager.IsInitialized
                || !ServerAutoAttackManager.IsInitialized
                || PurrNet.NetworkManager.main == null)
            {
                ThrowIfStopRequested();
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out waiting for ordinary server initialization (configuration, ECS, world manager and network manager).");
                yield return null;
            }
            ThrowIfStopRequested();
        }

        private void ValidateStage14RuntimeRole()
        {
            if (!_run.IsStage14NPCActivity)
                return;
            if (Runtime.Role != RuntimeRole.Server || !Runtime.IsServer())
            {
                throw new Test_ActorFixturePreconditionException(
                    "Stage 1.4 requires the shared runtime role to be Server; the bootstrap selected an inconsistent runtime role.");
            }
        }

        private IEnumerator WaitForStage14Players()
        {
            var requiredCount = Test_ActorFixtureLayout.GetStage14NPCActivityRequiredPlayerCount(_run.Options.NPCActivityScenario);
            var requireDenseCluster = _run.Options.NPCActivityScenario == Test_NPCActivityScenario.DensePlayerCluster;
            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            string diagnostic = null;
            while (!Test_NPCActivityScenarioController.TryCaptureRealPlayers(
                requiredCount,
                requireDenseCluster,
                out _npcActivityPlayerCapture,
                out diagnostic))
            {
                ThrowIfStopRequested();
                if (Time.realtimeSinceStartup >= deadline)
                    throw new Test_PerformancePrerequisiteException("BLOCKED-PREREQUISITE: Stage 1.4 real-player preflight did not complete: " + diagnostic);
                yield return null;
            }
            ThrowIfStopRequested();
        }

        private IEnumerator WaitForClientReady()
        {
            ThrowIfStopRequested();
            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            while (!Game.Shared.GameConfigurationManager.IsInitialized
                || !Game.Client.ClientWorldManager.Initialized
                || !Game.Client.ClientPositionManager.Initialized)
            {
                ThrowIfStopRequested();
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out waiting for ordinary graphical client initialization (configuration, client world manager and client position manager).");
                yield return null;
            }
            ThrowIfStopRequested();
        }

        private IEnumerator LoadSelectedWorldFixture()
        {
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                _run.WorldPlan.AssetPath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            if (operation == null)
                throw new InvalidOperationException("Unity did not create a load operation for the selected world fixture.");

            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            while (!operation.isDone)
            {
                ThrowIfStopRequested();
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out loading the selected world fixture scene: " + _run.WorldPlan.AssetPath);
                yield return null;
            }
            ThrowIfStopRequested();

            _selectedWorldScene = SceneManager.GetSceneByPath(_run.WorldPlan.AssetPath);
            _run.SetLoadedWorld(_selectedWorldScene);
        }

        private IEnumerator RunRamp()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RAMP_SECONDS)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceRamp(elapsed, Test_PerformanceRun.RAMP_SECONDS);
                _run.ObserveEntityCount(_fixture.DataOnlyEntityCount);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES);
                _run.RecordMeasurementFrame();
                yield return null;
            }
            _run.ActivityController.AdvanceRamp(Test_PerformanceRun.RAMP_SECONDS, Test_PerformanceRun.RAMP_SECONDS);
            _run.ObserveEntityCount(_fixture.DataOnlyEntityCount);
        }

        private IEnumerator RunStage14Ramp()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < _run.EffectiveRampSeconds)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ObserveEntityCount(_fixture.SpawnedFullActorCount);
                if (elapsed >= nextSample)
                {
                    RecordStage14Sample();
                    nextSample += _run.EffectiveSampleIntervalSeconds;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }
            _run.ObserveEntityCount(_fixture.SpawnedFullActorCount);
        }

        private IEnumerator RunStage14Warmup()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < _run.EffectiveWarmupSeconds)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ObserveEntityCount(_fixture.SpawnedFullActorCount);
                if (elapsed >= nextSample)
                {
                    RecordStage14Sample();
                    nextSample += _run.EffectiveSampleIntervalSeconds;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }
        }

        private IEnumerator RunStage14Measurement()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < _run.EffectiveMeasurementSeconds)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _npcActivityController.ObserveMeasurement(elapsed);
                _run.ObserveEntityCount(_fixture.SpawnedFullActorCount);
                if (elapsed >= nextSample)
                {
                    RecordStage14Sample();
                    nextSample += _run.EffectiveSampleIntervalSeconds;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }
            _npcActivityController.ObserveMeasurement(_run.EffectiveMeasurementSeconds);
        }

        private IEnumerator RunStage14Recovery()
        {
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < _run.EffectiveRecoverySeconds)
            {
                ThrowIfStopRequested();
                yield return null;
            }
        }

        private void RecordStage14Sample()
        {
            var frame = UnityEngine.Time.frameCount;
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.ECS_ENTITY_COUNT, _fixture.SpawnedFullActorCount, frame);
            PerformanceTelemetry.TryRecord(
                Test_PerformanceHarnessMetrics.TOTAL_ALLOCATED_BYTES,
                UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                frame);
            _run.RecordMeasurementSample();
        }

        private IEnumerator RunGraphicalRamp()
        {
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RAMP_SECONDS)
            {
                ThrowIfStopRequested();
                _graphicalFixture.Advance(0f, Test_PerformanceRun.RAMP_SECONDS);
                _graphicalFixture.RecordMetrics();
                _run.RecordMeasurementSample();
                _run.RecordMeasurementFrame();
                yield return null;
            }
        }

        private IEnumerator RunGraphicalWarmup()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < _run.Options.WarmupSeconds)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _graphicalFixture.Advance(elapsed, _run.Options.WarmupSeconds);
                if (elapsed >= nextSample)
                {
                    _graphicalFixture.RecordMetrics();
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }
        }

        private IEnumerator RunGraphicalMeasurement()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < _run.Options.MeasurementSeconds)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _graphicalFixture.Advance(elapsed, _run.Options.MeasurementSeconds);
                if (elapsed >= nextSample)
                {
                    _graphicalFixture.RecordMetrics();
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }

            _graphicalFixture.Advance(_run.Options.MeasurementSeconds, _run.Options.MeasurementSeconds);
            _graphicalFixture.RecordMetrics();
            _run.RecordMeasurementSample();
            _run.RecordMeasurementFrame();
            _run.CaptureGraphicalSnapshot();
        }

        private IEnumerator RunGraphicalRecovery()
        {
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RECOVERY_SECONDS)
            {
                ThrowIfStopRequested();
                yield return null;
            }
        }

        private IEnumerator RunWarmup()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.WARMUP_SECONDS)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceActivity(
                    elapsed,
                    Test_PerformanceRun.WARMUP_SECONDS,
                    Test_PerformanceActivityPhase.Warmup);
                _run.ObserveEntityCount(_fixture.DataOnlyEntityCount);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES);
                _run.RecordMeasurementFrame();
                yield return null;
            }
        }

        private IEnumerator RunMeasurement()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.MEASUREMENT_SECONDS)
            {
                ThrowIfStopRequested();
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceActivity(
                    elapsed,
                    Test_PerformanceRun.MEASUREMENT_SECONDS,
                    Test_PerformanceActivityPhase.Measurement);
                _run.ObserveEntityCount(_fixture.DataOnlyEntityCount);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES);
                _run.RecordMeasurementFrame();
                yield return null;
            }

            _run.CaptureActivitySnapshot("measurement_end_pre_cleanup");
        }

        private IEnumerator RunRecovery()
        {
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RECOVERY_SECONDS)
            {
                ThrowIfStopRequested();
                yield return null;
            }
        }

        private IEnumerator StopAndClean(string reasonCode, PerformanceRunStatus status, string message)
        {
            yield return CleanupFixture();
            SetRequestedResult(status, reasonCode, message);
        }

        private IEnumerator CleanupFixture()
        {
            if (_cleanupCompleted)
                yield break;
            if (_graphicalFixture != null)
            {
                _graphicalFixture.Dispose();
                _run.MarkCleanupComplete();
                _cleanupCompleted = true;
                Test_PerformanceHarnessController.PersistActiveRun("running");
                yield break;
            }
            if (_fixture == null)
                yield break;

            _run.CaptureActivitySnapshot("measurement_interrupted_pre_cleanup");
            _fixture.Cleanup();
            _run.CaptureCleanupReport(_fixture.LastCleanupReport);
            yield return _fixture.WaitForCleanup(_run.EffectivePreflightTimeoutSeconds);
            _run.MarkCleanupComplete();
            _cleanupCompleted = true;
            Test_PerformanceHarnessController.PersistActiveRun("running");
        }

        private IEnumerator UnloadSelectedWorldFixture()
        {
            _sceneUnloadAttempted = true;
            var operation = SceneManager.UnloadSceneAsync(_selectedWorldScene);
            if (operation == null)
                throw new InvalidOperationException("Unity did not create a scene unload operation for the selected world fixture.");

            var deadline = Time.realtimeSinceStartup + _run.EffectivePreflightTimeoutSeconds;
            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out unloading the selected world fixture scene: " + _run.WorldPlan.AssetPath);
                yield return null;
            }
        }

        private void ThrowIfStopRequested()
        {
            if (_run.StopRequested)
                throw new Test_PerformanceUserStopException("The user requested the performance run to stop.");
        }

        private void OnDestroy()
        {
            if (_run == null)
                return;

            RestoreStage14PlayerHealth();
            if (_run.Finalized)
                return;

            var playModeIsActive = Application.isPlaying && EditorApplication.isPlaying;
            if (playModeIsActive && _pendingFailure == null)
            {
                var activeScene = SceneManager.GetActiveScene();
                var fixturePath = _run.WorldPlan == null ? "<unavailable>" : _run.WorldPlan.AssetPath;
                SetPendingFailure(
                    new InvalidOperationException(
                        $"The performance harness runner was destroyed while Play Mode was still active. Active scene: '{activeScene.path}', selected fixture: '{fixturePath}', fixture unload attempted: {_sceneUnloadAttempted}, cleanup completed: {_cleanupCompleted}."),
                    new Test_PerformanceFailureClassification("RUNNER_DESTROYED_UNEXPECTEDLY", PerformanceRunStatus.FailInstrumentation));
            }

            try
            {
                if (_graphicalFixture != null && !_cleanupCompleted)
                {
                    _graphicalFixture.Dispose();
                    _run.MarkCleanupComplete();
                    _cleanupCompleted = true;
                }
                else if (_fixture != null && !_cleanupCompleted)
                {
                    _run.CaptureActivitySnapshot("measurement_interrupted_pre_cleanup");
                    _fixture.Cleanup();
                    _run.CaptureCleanupReport(_fixture.LastCleanupReport);
                    _fixture.AssertCleanupComplete();
                    _run.MarkCleanupComplete();
                    _cleanupCompleted = true;
                }
            }
            catch (Exception exception)
            {
                RecordCleanupFailure(exception);
            }

            try
            {
                if (_pendingFailure != null)
                    _run.Fail(_pendingFailure, _pendingFailureReason, _pendingFailureStatus);
                else
                    _run.AbortFromPlayModeExit();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
