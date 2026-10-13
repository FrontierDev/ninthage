using Game.Server.Core;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Coordinates the Editor-only scene and Play Mode lifecycle for one run.</summary>
    [InitializeOnLoad]
    internal static class Test_PerformanceHarnessController
    {
        private static SceneSetup[] _previousSceneSetup;
        private static Test_PerformanceRun _activeRun;

        static Test_PerformanceHarnessController()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static event Action StateChanged;

        public static bool IsRunning => _activeRun != null && !_activeRun.Finalized;

        public static string Status { get; private set; } = "Idle";

        public static string LastReportPath { get; private set; }

        public static void Start(Test_PerformanceHarnessOptions options)
        {
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

            try
            {
                var run = Test_PerformanceRun.Create(options);
                run.Validate();
                if (!string.IsNullOrEmpty(run.ValidationDiagnostic))
                {
                    run.Block(run.ValidationDiagnostic);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: " + run.ValidationDiagnostic;
                    StateChanged?.Invoke();
                    return;
                }

                if (!CanChangeScenes(out var dirtySceneDiagnostic))
                {
                    run.Block(dirtySceneDiagnostic);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: " + dirtySceneDiagnostic;
                    StateChanged?.Invoke();
                    return;
                }

                var harnessScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Test_PerformanceRun.HARNESS_SCENE_PATH);
                if (harnessScene == null)
                {
                    run.Block("Test-owned harness scene is missing or not imported: " + Test_PerformanceRun.HARNESS_SCENE_PATH);
                    LastReportPath = run.ReportPath;
                    Status = "Blocked: harness scene unavailable";
                    StateChanged?.Invoke();
                    return;
                }

                _previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
                var opened = EditorSceneManager.OpenScene(Test_PerformanceRun.HARNESS_SCENE_PATH, OpenSceneMode.Single);
                if (!opened.IsValid())
                    throw new InvalidOperationException("Unity could not open the test-owned performance harness scene.");

                _activeRun = run;
                Status = "Starting ordinary server Play Mode...";
                LastReportPath = run.ReportPath;
                StateChanged?.Invoke();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
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
            Status = "Stop requested; waiting for fixture cleanup...";
            StateChanged?.Invoke();
        }

        internal static void Complete(Test_PerformanceRun run)
        {
            if (!ReferenceEquals(run, _activeRun))
                return;
            LastReportPath = run.ReportPath;
            Status = "Finished: " + run.ReportPath;
            StateChanged?.Invoke();
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && _activeRun != null)
            {
                var runnerObject = new GameObject("Test_PerformanceHarnessRuntime");
                runnerObject.AddComponent<Test_PerformanceHarnessBehaviour>().Initialize(_activeRun);
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_activeRun != null && !_activeRun.Finalized)
                    _activeRun.AbortFromPlayModeExit();

                if (_previousSceneSetup != null && _previousSceneSetup.Length > 0)
                {
                    try
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(_previousSceneSetup);
                    }
                    catch (Exception exception)
                    {
                        Status = "Run finished, but the previous Editor scene setup could not be restored: " + exception.Message;
                    }
                }

                _previousSceneSetup = null;
                _activeRun = null;
                StateChanged?.Invoke();
            }
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
        private Test_PerformanceRun _run;
        private Test_ActorFixtureRuntime _fixture;
        private Scene _selectedWorldScene;
        private bool _cleanupAttempted;
        private Exception _pendingFailure;
        private string _pendingFailureReason;
        private PerformanceRunStatus _pendingFailureStatus;

        public void Initialize(Test_PerformanceRun run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            StartCoroutine(Execute());
        }

        private IEnumerator Execute()
        {
            var execution = ExecuteCore();
            while (true)
            {
                object current;
                try
                {
                    if (!execution.MoveNext())
                        break;
                    current = execution.Current;
                }
                catch (Test_ActorFixturePreconditionException exception)
                {
                    SetPendingFailure(exception, "FIXTURE_PRECONDITION_FAILED", PerformanceRunStatus.FailProvision);
                    break;
                }
                catch (TimeoutException exception)
                {
                    SetPendingFailure(exception, "RUN_TIMEOUT", PerformanceRunStatus.FailTimeout);
                    break;
                }
                catch (Test_PerformanceResourceCeilingException exception)
                {
                    SetPendingFailure(exception, "RESOURCE_CEILING_EXCEEDED", PerformanceRunStatus.FailResource);
                    break;
                }
                catch (Exception exception)
                {
                    SetPendingFailure(exception, "RUNNER_EXCEPTION", PerformanceRunStatus.FailCorrectness);
                    break;
                }

                yield return current;
            }

            if (!_cleanupAttempted && _fixture != null)
            {
                try
                {
                    _fixture.Cleanup();
                }
                catch (Exception exception)
                {
                    SetPendingFailure(exception, "CLEANUP_FAILED", PerformanceRunStatus.FailCorrectness);
                }
            }

            if (!_run.Finalized)
            {
                if (_pendingFailure != null)
                    _run.Fail(_pendingFailure, _pendingFailureReason, _pendingFailureStatus);
                else
                    _run.Fail(new InvalidOperationException("The runner exited before finalizing a result."), "RUNNER_INCOMPLETE", PerformanceRunStatus.FailInstrumentation);
            }

            if (_selectedWorldScene.IsValid() && _selectedWorldScene.isLoaded)
                SceneManager.UnloadSceneAsync(_selectedWorldScene);

            Test_PerformanceHarnessController.Complete(_run);
            Destroy(gameObject);
        }

        private IEnumerator ExecuteCore()
        {
            _run.StartTelemetry();
            yield return WaitForServerReady();
            _run.RegisterServerMetrics();
            yield return LoadSelectedWorldFixture();
            _run.CreateDataOnlyRuntime(_run.Options.WorkloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyActivity);
            _fixture = _run.FixtureRuntime;

            _run.BeginPhase(PerformanceMeasurementPhase.Ramp);
            yield return RunRamp();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailTimeout, "The user requested the run to stop during the population ramp.");
                yield break;
            }

            _run.MarkWorkloadApplied();
            _run.BeginPhase(PerformanceMeasurementPhase.Warmup);
            yield return RunWarmup();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailTimeout, "The user requested the run to stop during warmup.");
                yield break;
            }

            _run.BeginPhase(PerformanceMeasurementPhase.Measurement);
            yield return RunMeasurement();
            if (_run.StopRequested)
            {
                yield return StopAndClean("USER_STOPPED", PerformanceRunStatus.FailTimeout, "The user requested the run to stop during measurement.");
                yield break;
            }

            _run.BeginPhase(PerformanceMeasurementPhase.Recovery);
            yield return RunRecovery();
            yield return CleanupFixture();
            _run.Complete(PerformanceRunStatus.CompleteMeasured, "MEASUREMENT_COMPLETE", "The requested data-only population was measured and cleaned up successfully.");
        }

        private void SetPendingFailure(Exception exception, string reasonCode, PerformanceRunStatus status)
        {
            if (_pendingFailure == null)
            {
                _pendingFailure = exception;
                _pendingFailureReason = reasonCode;
                _pendingFailureStatus = status;
            }
        }

        private IEnumerator WaitForServerReady()
        {
            var deadline = Time.realtimeSinceStartup + Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS;
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
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out waiting for ordinary server initialization (configuration, ECS, world manager and network manager).");
                yield return null;
            }
        }

        private IEnumerator LoadSelectedWorldFixture()
        {
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                _run.WorldPlan.AssetPath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            if (operation == null)
                throw new InvalidOperationException("Unity did not create a load operation for the selected world fixture.");

            var deadline = Time.realtimeSinceStartup + Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS;
            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException("Timed out loading the selected world fixture scene: " + _run.WorldPlan.AssetPath);
                yield return null;
            }

            _selectedWorldScene = SceneManager.GetSceneByPath(_run.WorldPlan.AssetPath);
            _run.SetLoadedWorld(_selectedWorldScene);
        }

        private IEnumerator RunRamp()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RAMP_SECONDS)
            {
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceRamp(elapsed, Test_PerformanceRun.RAMP_SECONDS);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_TOTAL_ALLOCATED_BYTES);
                yield return null;
            }
            _run.ActivityController.AdvanceRamp(Test_PerformanceRun.RAMP_SECONDS, Test_PerformanceRun.RAMP_SECONDS);
        }

        private IEnumerator RunWarmup()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.WARMUP_SECONDS)
            {
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceActivity(elapsed, Test_PerformanceRun.MEASUREMENT_SECONDS);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_TOTAL_ALLOCATED_BYTES);
                yield return null;
            }
        }

        private IEnumerator RunMeasurement()
        {
            var start = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.MEASUREMENT_SECONDS)
            {
                var elapsed = Time.realtimeSinceStartup - start;
                _run.ActivityController.AdvanceActivity(elapsed, Test_PerformanceRun.MEASUREMENT_SECONDS);
                if (elapsed >= nextSample)
                {
                    _run.ActivityController.RecordSample(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS);
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.ActivityController.ThrowIfResourceCeilingExceeded(Test_PerformanceRun.MAX_TOTAL_ALLOCATED_BYTES);
                yield return null;
            }
        }

        private IEnumerator RunRecovery()
        {
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < Test_PerformanceRun.RECOVERY_SECONDS)
                yield return null;
        }

        private IEnumerator StopAndClean(string reasonCode, PerformanceRunStatus status, string message)
        {
            yield return CleanupFixture();
            _run.Complete(status, reasonCode, message);
        }

        private IEnumerator CleanupFixture()
        {
            if (_cleanupAttempted)
                yield break;
            _cleanupAttempted = true;
            if (_fixture == null)
                yield break;

            _fixture.Cleanup();
            yield return _fixture.WaitForCleanup(Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS);
            _run.MarkCleanupComplete();
        }

        private void OnDestroy()
        {
            if (_run != null && !_run.Finalized)
                _run.AbortFromPlayModeExit();
        }
    }
}
