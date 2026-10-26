using Game.Server.Core;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Captures source and machine identity required by benchmark evidence.</summary>
    [Serializable]
    internal sealed class Test_PerformanceRunMetadata
    {
        public string Repository;
        public string GitBranch;
        public string GitBranchStatus;
        public string GitBranchDiagnostic;
        public string CapturedUtc;
        public string Commit;
        public bool Dirty;
        public string DiffSha256;
        public string PackagesSha256;
        public string PackagesLockSha256;
        public string PurrNetSha256;
        public string HostID;
        public int ProcessID;
        public string Hardware;
        public long MemoryBytes;

        public static Test_PerformanceRunMetadata Capture(string projectRoot)
        {
            var commit = RunGit(projectRoot, "rev-parse HEAD").Trim();
            if (commit.Length != 40 || !IsHex(commit))
                throw new InvalidOperationException("Could not capture the full Git commit SHA for the benchmark run.");

            var status = RunGit(projectRoot, "status --porcelain");
            var diff = RunGit(projectRoot, "diff --binary HEAD --");
            var branch = RunGit(projectRoot, "branch --show-current").Trim();
            var machineIdentity = string.Join("|", SystemInfo.operatingSystem, SystemInfo.processorType, SystemInfo.processorCount.ToString(CultureInfo.InvariantCulture));
            return new Test_PerformanceRunMetadata
            {
                Repository = projectRoot,
                GitBranch = branch,
                GitBranchStatus = string.IsNullOrEmpty(branch) ? "detached" : "named",
                GitBranchDiagnostic = string.IsNullOrEmpty(branch) ? "HEAD is detached; no branch name was available." : null,
                CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Commit = commit,
                Dirty = !string.IsNullOrEmpty(status),
                DiffSha256 = HashString(diff),
                PackagesSha256 = HashFile(Path.Combine(projectRoot, "Packages", "manifest.json")),
                PackagesLockSha256 = HashFile(Path.Combine(projectRoot, "Packages", "packages-lock.json")),
                PurrNetSha256 = HashDirectory(Path.Combine(projectRoot, "Assets", "ThirdParty", "PurrNet"), projectRoot),
                HostID = "editor-host-" + HashString(machineIdentity).Substring(0, 12),
                ProcessID = Process.GetCurrentProcess().Id,
                Hardware = machineIdentity,
                MemoryBytes = (long)SystemInfo.systemMemorySize * 1024L * 1024L,
            };
        }

        private static string RunGit(string projectRoot, string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                if (!process.Start())
                    throw new InvalidOperationException("Git could not be started while capturing benchmark identity.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"Git '{arguments}' failed: {error.Trim()}");
                if (output.Length > 8 * 1024 * 1024)
                    throw new InvalidOperationException($"Git '{arguments}' produced an unexpectedly large identity payload.");
                return output;
            }
        }

        internal static string HashFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Required evidence input is missing.", path);

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return ToHex(sha256.ComputeHash(stream));
        }

        private static string HashDirectory(string directory, string projectRoot)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException("Required PurrNet source directory is missing: " + directory);

            var entries = new StringBuilder();
            var files = new List<string>(Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
            files.Sort(StringComparer.Ordinal);
            foreach (var path in files)
            {
                var relative = path.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                entries.Append(relative).Append(':').Append(HashFile(path)).Append('\n');
            }

            return HashString(entries.ToString());
        }

        internal static string HashString(string value)
        {
            using (var sha256 = SHA256.Create())
                return ToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static bool IsHex(string value)
        {
            foreach (var character in value)
            {
                if (!Uri.IsHexDigit(character))
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    internal sealed class Test_PerformancePhaseTimingState
    {
        public string started_utc;
        public string completed_utc;
        public double configured_seconds = -1d;
        public double actual_elapsed_seconds = -1d;
        public bool timing_known;
    }

    [Serializable]
    internal sealed class Test_PerformanceObservabilityState
    {
        public string timing_domain = "editor_monotonic_current_domain";
        public string queue_dispatch_utc;
        public string play_mode_entered_utc;
        public string workload_finalized_utc;
        public string play_mode_exited_utc;
        public string scene_restored_utc;
        public string scene_restoration_status = "pending";
        public string scene_restoration_diagnostic;
        public double queue_dispatch_to_play_mode_entered_seconds = -1d;
        public double workload_finalized_to_play_mode_exited_seconds = -1d;
        public double play_mode_exit_to_scene_restored_seconds = -1d;
        public double total_attempt_wall_clock_seconds = -1d;
        public Test_PerformancePhaseTimingState ramp = new();
        public Test_PerformancePhaseTimingState warmup = new();
        public Test_PerformancePhaseTimingState measurement = new();
        public Test_PerformancePhaseTimingState recovery = new();
        public int measurement_frame_count;
        public int measurement_sample_count;
        public bool measurement_timing_known;
        public double max_frame_delivery_gap_seconds = -1d;
        public bool pause_gap_detected;
        public bool pause_gap_detectable;
        public int focus_transition_count;
        public int focused_frame_count;
        public int unfocused_frame_count;
        public int unknown_focus_frame_count;
        public double focused_seconds = -1d;
        public double unfocused_seconds = -1d;
        public double unknown_focus_seconds = -1d;
        public bool focus_observation_known;
        public bool run_in_background;
        public string focus_context = "unknown";
        public string rendering_qualification = "not_applicable";
        public string rendering_probe_status = "not_applicable";
        public string rendering_reason;
        public bool rendering_workload_verified;
        public int expected_visible_object_count;
        public double draw_call_p50;
        public double triangle_p50;
        public bool has_draw_call_p50;
        public bool has_triangle_p50;
        public int rendering_frame_sample_count;
    }

    /// <summary>Owns one Editor runner attempt, its telemetry session and evidence files.</summary>
    internal sealed class Test_PerformanceRun
    {
        public const string RESULT_ROOT = "Builds/Benchmarks/Results";
        public const string HARNESS_SCENE_PATH = "Assets/Tests/Performance/Harness/Editor/PerformanceHarnessServer.unity";
        public const float PREFLIGHT_TIMEOUT_SECONDS = 30f;
        public const float RAMP_SECONDS = 2f;
        public const float WARMUP_SECONDS = 2f;
        public const float MEASUREMENT_SECONDS = 5f;
        public const float RECOVERY_SECONDS = 1f;
        public const float SAMPLE_INTERVAL_SECONDS = 0.5f;
        public const long MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES = 4L * 1024L * 1024L * 1024L;

        private readonly StringBuilder _log = new();
        private readonly Test_PerformanceRunMetadata _metadata;
        private readonly DateTime _startedUtc;
        private string _telemetrySummary;
        private bool _telemetryStarted;
        private bool _measurementSamplesAvailable;
        private PerformanceMeasurementPhase _currentPhase = PerformanceMeasurementPhase.Preflight;
        private Test_PerformanceActivitySnapshot _activitySnapshot;
        private string _activitySnapshotPhase;
        private readonly Test_PerformanceObservabilityState _observability = new();
        private long _queueDispatchTimestamp;
        private long _playModeEnteredTimestamp;
        private long _workloadFinalizedTimestamp;
        private long _playModeExitedTimestamp;
        private long _sceneRestoredTimestamp;
        private long _phaseStartTimestamp;
        private long _focusStateTimestamp;
        private long _lastMeasurementFrameTimestamp;
        private bool _focusState;
        private bool _focusStateActive;
        private bool _focusObservationForcedUnknown;
        private PerformanceRunStatus _finalStatus;
        private string _finalReasonCode;
        private string _finalMessage;

        private Test_PerformanceRun(
            Test_PerformanceHarnessOptions options,
            Test_ActorFixtureProfile actorProfile,
            Test_ActorScenarioProfile scenario,
            Test_GraphicalScenarioPlan graphicalPlan,
            Test_WorldFixturePlan worldPlan,
            Test_PerformanceRunMetadata metadata,
            string runID,
            string resultsDirectory,
            DateTime startedUtc)
        {
            Options = options;
            ActorProfile = actorProfile;
            Scenario = scenario;
            GraphicalPlan = graphicalPlan;
            WorldPlan = worldPlan;
            _metadata = metadata;
            RunID = runID;
            ResultsDirectory = resultsDirectory;
            FixturePath = Path.Combine(resultsDirectory, "fixture.json");
            RunPath = Path.Combine(resultsDirectory, "run.json");
            SummaryPath = Path.Combine(resultsDirectory, "summary.json");
            ReportPath = Path.Combine(resultsDirectory, "report.md");
            _startedUtc = startedUtc.ToUniversalTime();
            _observability.run_in_background = Application.runInBackground;
            _observability.measurement.configured_seconds = options.IsGraphicalClient
                ? options.MeasurementSeconds
                : options.IsStage14NPCActivity
                    ? 300f
                : MEASUREMENT_SECONDS;
            _observability.expected_visible_object_count = graphicalPlan == null ? 0 : graphicalPlan.RequestedObjectCount;
        }

        public Test_PerformanceHarnessOptions Options { get; }

        public Test_ActorFixtureProfile ActorProfile { get; }

        public Test_ActorScenarioProfile Scenario { get; }

        public Test_GraphicalScenarioPlan GraphicalPlan { get; }

        public Test_WorldFixturePlan WorldPlan { get; }

        public string RunID { get; }

        public string ResultsDirectory { get; }

        public string FixturePath { get; }

        public string RunPath { get; }

        public string SummaryPath { get; }

        public string ReportPath { get; }

        public string ValidationDiagnostic { get; private set; }

        public bool StopRequested { get; set; }

        public bool Finalized { get; private set; }

        public bool WorldFixtureLoaded { get; private set; }

        public bool PreflightCompleted { get; private set; }

        public bool WorkloadApplied { get; private set; }

        public bool MeasurementStarted { get; private set; }

        public bool RampCompleted { get; private set; }

        public bool WarmupCompleted { get; private set; }

        public bool MeasurementCompleted { get; private set; }

        public bool RecoveryCompleted { get; private set; }

        public bool CleanupComplete { get; private set; }

        public int RampCompletedEntityCount { get; private set; }

        public int MeasurementStartEntityCount { get; private set; }

        public int MeasurementEndEntityCount { get; private set; }

        public int PeakEntityCount { get; private set; }

        public int PostCleanupFixtureEntityCount { get; private set; }

        public int PostCleanupActorEntityCount { get; private set; }

        public bool PostCleanupObserved { get; private set; }

        public bool MeasurementSamplesAvailable => _measurementSamplesAvailable;

        public bool ActivitySnapshotCaptured => _activitySnapshot != null;

        public string RuntimeDiagnostic { get; private set; }

        public Scene LoadedWorldScene { get; private set; }

        public Test_ActorFixtureRuntime FixtureRuntime { get; private set; }

        public Test_PerformanceActivityController ActivityController { get; private set; }

        public Test_NPCActivityScenarioEvidence Stage14NPCActivityEvidence { get; private set; }

        public Test_GraphicalFixtureRuntime GraphicalRuntime { get; private set; }

        public bool IsGraphicalClient => Options.IsGraphicalClient;

        public bool IsStage14NPCActivity => Options.IsStage14NPCActivity;

        public float EffectivePreflightTimeoutSeconds => IsStage14NPCActivity ? 120f : PREFLIGHT_TIMEOUT_SECONDS;

        public float EffectiveRampSeconds => IsStage14NPCActivity ? 120f : RAMP_SECONDS;

        public float EffectiveWarmupSeconds => IsStage14NPCActivity ? 60f : IsGraphicalClient ? Options.WarmupSeconds : WARMUP_SECONDS;

        public float EffectiveMeasurementSeconds => IsStage14NPCActivity ? 300f : IsGraphicalClient ? Options.MeasurementSeconds : MEASUREMENT_SECONDS;

        public float EffectiveRecoverySeconds => IsStage14NPCActivity ? 60f : RECOVERY_SECONDS;

        public float EffectiveSampleIntervalSeconds => IsStage14NPCActivity ? 1f : SAMPLE_INTERVAL_SECONDS;

        public int RequestedActorCount => Options.ActorCount;

        public string FixtureHash => Test_PerformanceRunMetadata.HashFile(FixturePath);

        internal void AttachStage14NPCActivityEvidence(Test_NPCActivityScenarioController controller, int achievedFullNPCCount)
        {
            if (!IsStage14NPCActivity)
                throw new InvalidOperationException("Stage 1.4 evidence cannot be attached to a non-Stage 1.4 run.");
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            Stage14NPCActivityEvidence = controller.BuildEvidence(achievedFullNPCCount);
            AppendLog($"Stage 1.4 scenario captured: {Stage14NPCActivityEvidence.npc_activity_scenario}, players={Stage14NPCActivityEvidence.achieved_real_player_count}/{Stage14NPCActivityEvidence.required_real_player_count}, NPCs={Stage14NPCActivityEvidence.achieved_full_npc_count}/{Stage14NPCActivityEvidence.requested_npc_count}.");
            File.WriteAllText(RunPath, Test_PerformanceEvidence.BuildRunJson(this, _metadata.Repository, _metadata), new UTF8Encoding(false));
        }

        internal void RefreshStage14NPCActivityEvidence(Test_NPCActivityScenarioController controller, int achievedFullNPCCount)
        {
            if (!IsStage14NPCActivity)
                throw new InvalidOperationException("Stage 1.4 evidence cannot be refreshed for a non-Stage 1.4 run.");
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            Stage14NPCActivityEvidence = controller.BuildEvidence(achievedFullNPCCount);
            AppendLog($"Stage 1.4 scenario evidence refreshed: {Stage14NPCActivityEvidence.npc_activity_scenario}, players={Stage14NPCActivityEvidence.achieved_real_player_count}/{Stage14NPCActivityEvidence.required_real_player_count}, NPCs={Stage14NPCActivityEvidence.achieved_full_npc_count}/{Stage14NPCActivityEvidence.requested_npc_count}.");
            File.WriteAllText(RunPath, Test_PerformanceEvidence.BuildRunJson(this, _metadata.Repository, _metadata), new UTF8Encoding(false));
        }

        public string MeasurementSummary => BuildMeasurementDisplay();

        public string OrchestrationSummary => BuildOrchestrationDisplay();

        public string FocusSummary => BuildFocusDisplay();

        public string RenderingSummary => BuildRenderingDisplay();

        internal bool RenderingWorkloadVerified => _observability.rendering_workload_verified;

        internal string RenderingQualification => _observability.rendering_qualification;

        internal string FocusContext => _observability.focus_context;

        internal void MarkQueueDispatch(long timestamp, DateTime utc)
        {
            _queueDispatchTimestamp = timestamp;
            _observability.queue_dispatch_utc = utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }

        internal void MarkPlayModeEntered()
        {
            MarkLifecycleEvent(ref _playModeEnteredTimestamp, value => _observability.play_mode_entered_utc = value);
            _observability.queue_dispatch_to_play_mode_entered_seconds = ElapsedSeconds(_queueDispatchTimestamp, _playModeEnteredTimestamp);
        }

        internal void MarkWorkloadFinalized()
        {
            MarkLifecycleEvent(ref _workloadFinalizedTimestamp, value => _observability.workload_finalized_utc = value);
        }

        internal void MarkPlayModeExited()
        {
            if (!string.IsNullOrEmpty(_observability.play_mode_exited_utc))
                return;
            MarkLifecycleEvent(ref _playModeExitedTimestamp, value => _observability.play_mode_exited_utc = value);
            _observability.workload_finalized_to_play_mode_exited_seconds = ElapsedSeconds(_workloadFinalizedTimestamp, _playModeExitedTimestamp);
        }

        internal void MarkSceneRestored()
        {
            if (!string.IsNullOrEmpty(_observability.scene_restored_utc)
                || string.Equals(_observability.scene_restoration_status, "failed", StringComparison.Ordinal))
                return;
            MarkLifecycleEvent(ref _sceneRestoredTimestamp, value => _observability.scene_restored_utc = value);
            _observability.play_mode_exit_to_scene_restored_seconds = ElapsedSeconds(_playModeExitedTimestamp, _sceneRestoredTimestamp);
            _observability.scene_restoration_status = "restored";
            _observability.scene_restoration_diagnostic = null;
        }

        internal void MarkSceneRestoreFailed(string diagnostic)
        {
            if (!string.IsNullOrEmpty(_observability.scene_restored_utc)
                || string.Equals(_observability.scene_restoration_status, "failed", StringComparison.Ordinal))
                return;
            _observability.scene_restoration_status = "failed";
            _observability.scene_restoration_diagnostic = diagnostic ?? "Scene restoration failed; timing is unavailable.";
            _observability.play_mode_exit_to_scene_restored_seconds = -1d;
            ValidationDiagnostic = AppendDiagnostic(ValidationDiagnostic, _observability.scene_restoration_diagnostic);
        }

        internal void CompleteEditorLifecycle()
        {
            if (!Finalized)
                return;

            EnsureFinalizedEvidenceExists();
            if (_observability.total_attempt_wall_clock_seconds < 0d)
                _observability.total_attempt_wall_clock_seconds = (DateTime.UtcNow - _startedUtc).TotalSeconds;
            UpdateFinalizedSummaryLifecycle();
            UpdateFinalizedReportLifecycle();
            WriteArtifacts();
        }

        public static Test_PerformanceRun Create(Test_PerformanceHarnessOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var metadata = Test_PerformanceRunMetadata.Capture(projectRoot);
            ResolveInputs(options, out var actorProfile, out var scenario, out var graphicalPlan, out var worldPlan);
            var resultRoot = Path.Combine(projectRoot, RESULT_ROOT);
            Directory.CreateDirectory(resultRoot);
            var runID = CreateUniqueRunID(resultRoot);
            var resultsDirectory = Path.Combine(resultRoot, runID);
            Directory.CreateDirectory(resultsDirectory);
            var run = new Test_PerformanceRun(options, actorProfile, scenario, graphicalPlan, worldPlan, metadata, runID, resultsDirectory, DateTime.UtcNow);
            run.WriteFixtureAndRun(projectRoot);
            return run;
        }

        public static Test_PerformanceRun Restore(Test_PerformancePendingRunState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.options == null || state.metadata == null)
                throw new InvalidOperationException("The pending performance run does not contain validated options and metadata.");
            if (state.startedUtcTicks <= 0)
                throw new InvalidOperationException("The pending performance run does not contain a valid start timestamp.");

            var options = state.options.ToOptions();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var expectedResultsDirectory = Path.GetFullPath(Path.Combine(projectRoot, RESULT_ROOT, state.runID));
            var resultsDirectory = Path.GetFullPath(state.resultsDirectory ?? string.Empty);
            if (!string.Equals(resultsDirectory, expectedResultsDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The pending performance run evidence directory does not match its run identity.");
            if (!Directory.Exists(resultsDirectory))
                throw new DirectoryNotFoundException("The pending performance run evidence directory is missing: " + resultsDirectory);

            var fixturePath = Path.GetFullPath(state.fixturePath ?? string.Empty);
            var runPath = Path.GetFullPath(state.runPath ?? string.Empty);
            var summaryPath = Path.GetFullPath(state.summaryPath ?? string.Empty);
            var reportPath = Path.GetFullPath(state.reportPath ?? string.Empty);
            var expectedFixturePath = Path.Combine(resultsDirectory, "fixture.json");
            var expectedRunPath = Path.Combine(resultsDirectory, "run.json");
            var expectedSummaryPath = Path.Combine(resultsDirectory, "summary.json");
            var expectedReportPath = Path.Combine(resultsDirectory, "report.md");
            if (!string.Equals(fixturePath, expectedFixturePath, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(runPath, expectedRunPath, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(summaryPath, expectedSummaryPath, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(reportPath, expectedReportPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The pending performance run evidence paths do not match its result directory.");
            }
            if (!File.Exists(fixturePath) || !File.Exists(runPath))
                throw new FileNotFoundException("The pending performance run evidence files are missing.", fixturePath);
            if (state.finalized
                && (!File.Exists(summaryPath) || !File.Exists(reportPath)))
                throw new FileNotFoundException("The finalized performance run workload evidence files are missing.", !File.Exists(summaryPath) ? summaryPath : reportPath);
            if (!string.Equals(Test_PerformanceRunMetadata.HashFile(fixturePath), state.fixtureSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The pending performance run fixture digest changed during Editor reload.");

            ResolveInputs(options, out var actorProfile, out var scenario, out var graphicalPlan, out var worldPlan);
            var run = new Test_PerformanceRun(
                options,
                actorProfile,
                scenario,
                graphicalPlan,
                worldPlan,
                state.metadata,
                state.runID,
                resultsDirectory,
                new DateTime(state.startedUtcTicks, DateTimeKind.Utc));
            run.RestoreLifecycle(state);
            return run;
        }

        private static void ResolveInputs(
            Test_PerformanceHarnessOptions options,
            out Test_ActorFixtureProfile actorProfile,
            out Test_ActorScenarioProfile scenario,
            out Test_GraphicalScenarioPlan graphicalPlan,
            out Test_WorldFixturePlan worldPlan)
        {
            graphicalPlan = null;
            if (options.IsGraphicalClient)
            {
                graphicalPlan = Test_GraphicalFixtureLayout.BuildPlan(
                    options.GraphicalScenario,
                    options.DensityTier,
                    options.ActorDensity,
                    options.Seed);
                actorProfile = null;
                scenario = null;
                worldPlan = FindWorldPlan(
                    Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
                    {
                        Seed = options.Seed,
                        DensityTier = options.DensityTier,
                        FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
                    }),
                    graphicalPlan.FixtureVariants[0]);
                return;
            }

            if (options.IsTimedStateScale)
            {
                actorProfile = Test_ActorFixtureLayout.BuildTimedStateScaleProfile(options.ActorCount, options.Seed);
                scenario = Test_ActorFixtureLayout.BuildTimedStateScaleScenario(options.ActorCount, options.Seed);
            }
            else if (options.IsStage14NPCActivity)
            {
                actorProfile = Test_ActorFixtureLayout.BuildStage14NPCActivityProfile(options.NPCActivityScenario, options.ActorCount, options.Seed);
                scenario = Test_ActorFixtureLayout.BuildStage14NPCActivityScenario(options.NPCActivityScenario, options.ActorCount, options.Seed);
            }
            else
            {
                var catalog = Test_ActorFixtureLayout.BuildCatalog(options.Seed);
                var scenarioCatalog = Test_ActorFixtureLayout.BuildScenarioCatalog(options.Seed);
                actorProfile = FindActorProfile(catalog, options.ActorVariantID);
                scenario = FindScenario(scenarioCatalog, options.ActorVariantID);
            }
            var worldManifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = options.Seed,
                DensityTier = options.DensityTier,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            worldPlan = FindWorldPlan(worldManifest, options.WorldFixture.ToString());
        }

        public void Validate()
        {
            if (Options.TryValidate(out var diagnostic, out _, out _))
                return;
            ValidationDiagnostic = diagnostic;
        }

        public void StartTelemetry()
        {
            if (!string.IsNullOrEmpty(ValidationDiagnostic))
                throw new InvalidOperationException(ValidationDiagnostic);
            if (_telemetryStarted)
                throw new InvalidOperationException("The benchmark telemetry session has already started.");

            var context = new PerformanceSessionContext(RunID, ResultsDirectory)
            {
                FixtureSha256 = FixtureHash,
                GitRepository = _metadata.Repository,
                GitBranch = _metadata.GitBranch,
                GitCapturedUtc = _metadata.CapturedUtc,
                GitCommitSha = _metadata.Commit,
                GitDirty = _metadata.Dirty,
                BuildKind = IsGraphicalClient ? "editor_playmode_rendered_client" : "editor_playmode_server",
                BuildFlags = IsGraphicalClient
                    ? "test_only;ordinary_client_initialization;no_deep_profiling"
                    : "test_only;ordinary_server_initialization;no_deep_profiling",
                ProfileRevision = IsGraphicalClient
                    ? GraphicalPlan.ProfileRevision
                    : ActorProfile.revision,
                Seed = Options.Seed.ToString(CultureInfo.InvariantCulture),
                Platform = Application.platform.ToString(),
                GraphicsOptions = BuildGraphicsIdentity(),
                Hardware = _metadata.Hardware,
                HostId = _metadata.HostID,
                ProcessId = "unity-editor-process-" + _metadata.ProcessID.ToString(CultureInfo.InvariantCulture),
            };
            PerformanceTelemetry.StartSession(context);
            Test_PerformanceHarnessMetrics.Register();
            _telemetryStarted = true;
            AppendLog("Telemetry session started.");
        }

        public void RegisterServerMetrics()
        {
            ServerInitialization.RegisterPerformanceMetrics();
            AppendLog("Project-owned server metrics registered.");
        }

        public void RegisterClientMetrics()
        {
            if (!IsGraphicalClient)
                throw new InvalidOperationException("Client metrics can only be registered for a graphical client run.");
            Game.Client.ClientInitialization.RegisterPerformanceMetrics();
            Test_PerformanceHarnessMetrics.RegisterGraphical();
            AppendLog("Project-owned client and graphical harness metrics registered.");
        }

        public void RefreshGraphicalRunEvidence()
        {
            if (!IsGraphicalClient)
                throw new InvalidOperationException("Graphical run evidence can only be refreshed for a graphical client run.");

            File.WriteAllText(
                RunPath,
                Test_PerformanceEvidence.BuildRunJson(this, _metadata.Repository, _metadata),
                new UTF8Encoding(false));
            AppendLog("Graphical run evidence captured after ordinary client initialization.");
        }

        public void SetLoadedWorld(Scene scene)
        {
            LoadedWorldScene = scene;
            WorldFixtureLoaded = scene.IsValid() && scene.isLoaded && string.Equals(scene.path, WorldPlan.AssetPath, StringComparison.Ordinal);
            if (!WorldFixtureLoaded)
                throw new InvalidOperationException($"Loaded scene identity did not match the selected fixture: expected {WorldPlan.AssetPath}, found {scene.path}.");
            var metadata = FindMetadata(scene, out var missingScriptDiagnostic);
            if (missingScriptDiagnostic != null)
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Selected fixture scene '{scene.path}' contains an unresolved script reference at {missingScriptDiagnostic}. Its fixture metadata cannot be trusted; check the test runtime assembly and imported script GUIDs.");
            }

            if (metadata == null)
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Selected fixture scene '{scene.path}' is missing its required Test_WorldFixtureMetadata component on a root GameObject.");
            }

            var metadataMismatches = new List<string>();
            AddMetadataMismatch(metadataMismatches, "fixture version", Test_WorldFixtureLayout.FIXTURE_VERSION, metadata.FixtureVersion);
            AddMetadataMismatch(metadataMismatches, "fixture ID", WorldPlan.FixtureID, metadata.FixtureID);
            AddMetadataMismatch(metadataMismatches, "variant", WorldPlan.Variant, metadata.Variant);
            AddMetadataMismatch(metadataMismatches, "status", WorldPlan.Status, metadata.Status);
            AddMetadataMismatch(metadataMismatches, "seed", Options.Seed, metadata.Seed);
            AddMetadataMismatch(metadataMismatches, "origin cell size", Test_WorldFixtureLayout.ORIGIN_CELL_SIZE_METERS, metadata.OriginCellSizeMeters);
            AddMetadataMismatch(metadataMismatches, "world tile size", Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS, metadata.WorldTileSizeMeters);
            AddMetadataMismatch(metadataMismatches, "world tile X", WorldPlan.WorldTileX, metadata.WorldTileX);
            AddMetadataMismatch(metadataMismatches, "world tile Z", WorldPlan.WorldTileZ, metadata.WorldTileZ);
            AddMetadataMismatch(metadataMismatches, "object count", WorldPlan.ObjectCount, metadata.ObjectCount);
            AddMetadataMismatch(metadataMismatches, "navigable flag", WorldPlan.IsNavigable, metadata.IsNavigable);
            if (metadataMismatches.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Selected fixture scene '{scene.path}' metadata identity does not match the committed fixture contract: {string.Join("; ", metadataMismatches)}.");
            }

            AppendLog("Selected world fixture loaded: " + scene.path);
        }

        public void CreateDataOnlyRuntime(Test_PerformanceWorkloadMode workloadMode)
        {
            FixtureRuntime = new Test_ActorFixtureRuntime(ActorProfile);
            FixtureRuntime.StartDataOnly(0);
            ActivityController = new Test_PerformanceActivityController(
                FixtureRuntime,
                workloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
                Options.IsTimedStateStress,
                Options.IsTimedStateScale);
            PreflightCompleted = true;
            AppendLog("Data-only ECS fixture prepared with zero entities for incremental ramp.");
        }

        public void SetFullNetworkedRuntime(Test_ActorFixtureRuntime fixture)
        {
            if (!IsStage14NPCActivity)
                throw new InvalidOperationException("A full networked runtime can only be attached to a Stage 1.4 run.");
            if (fixture == null)
                throw new ArgumentNullException(nameof(fixture));
            FixtureRuntime = fixture;
            PreflightCompleted = true;
            AppendLog("Stage 1.4 full networked NPC fixture prepared with real-player anchoring.");
        }

        public Test_GraphicalFixtureRuntime CreateGraphicalRuntime()
        {
            if (!IsGraphicalClient || GraphicalPlan == null)
                throw new InvalidOperationException("The selected run has no graphical scenario plan.");
            GraphicalRuntime = new Test_GraphicalFixtureRuntime(GraphicalPlan);
            PreflightCompleted = true;
            AppendLog("Graphical fixture prepared: " + GraphicalPlan.ScenarioID);
            return GraphicalRuntime;
        }

        public void CaptureGraphicalSnapshot()
        {
            if (!IsGraphicalClient || GraphicalRuntime == null)
                return;
            ObserveEntityCount(GraphicalRuntime.ActualObjectCount);
            AppendLog(
                $"Graphical measurement snapshot: objects={GraphicalRuntime.ActualObjectCount}, visible={GraphicalRuntime.VisibleObjectCount}, actors={GraphicalRuntime.ActualActorCount}, routeWaypoints={GraphicalRuntime.RouteWaypointsReached}/{GraphicalPlan.Waypoints.Length}, routeDistance={GraphicalRuntime.RouteDistanceTravelled.ToString("R", CultureInfo.InvariantCulture)}.");
        }

        public Test_PerformancePendingRunState CreatePendingRunState(SceneSetup[] previousSceneSetup, string lifecycle)
        {
            return new Test_PerformancePendingRunState
            {
                runID = RunID,
                resultsDirectory = ResultsDirectory,
                fixturePath = FixturePath,
                runPath = RunPath,
                summaryPath = SummaryPath,
                reportPath = ReportPath,
                fixtureSha256 = FixtureHash,
                startedUtcTicks = _startedUtc.Ticks,
                options = Test_PerformancePendingRunOptions.Capture(Options),
                metadata = _metadata,
                previousSceneSetup = Test_PerformanceSceneSetupState.Capture(previousSceneSetup),
                lifecycle = lifecycle,
                stopRequested = StopRequested,
                telemetryStarted = _telemetryStarted,
                finalized = Finalized,
                finalizedStatus = Finalized ? StatusText(_finalStatus) : null,
                finalizedReasonCode = Finalized ? _finalReasonCode : null,
                finalizedMessage = Finalized ? _finalMessage : null,
                worldFixtureLoaded = WorldFixtureLoaded,
                preflightCompleted = PreflightCompleted,
                workloadApplied = WorkloadApplied,
                measurementStarted = MeasurementStarted,
                rampCompleted = RampCompleted,
                warmupCompleted = WarmupCompleted,
                measurementCompleted = MeasurementCompleted,
                recoveryCompleted = RecoveryCompleted,
                cleanupComplete = CleanupComplete,
                currentPhase = (int)_currentPhase,
                rampCompletedEntityCount = RampCompletedEntityCount,
                measurementStartEntityCount = MeasurementStartEntityCount,
                measurementEndEntityCount = MeasurementEndEntityCount,
                peakEntityCount = PeakEntityCount,
                postCleanupObserved = PostCleanupObserved,
                postCleanupFixtureEntityCount = PostCleanupFixtureEntityCount,
                postCleanupActorEntityCount = PostCleanupActorEntityCount,
                activitySnapshotCaptured = ActivitySnapshotCaptured,
                activityIdleCount = _activitySnapshot == null ? 0 : _activitySnapshot.IdleCount,
                activityPatrollingCount = _activitySnapshot == null ? 0 : _activitySnapshot.PatrollingCount,
                activityCombatCount = _activitySnapshot == null ? 0 : _activitySnapshot.CombatCount,
                activityWakeSurgeCount = _activitySnapshot == null ? 0 : _activitySnapshot.WakeSurgeCount,
                activitySpawnEventCount = _activitySnapshot == null ? 0 : _activitySnapshot.SpawnEventCount,
                activityDespawnEventCount = _activitySnapshot == null ? 0 : _activitySnapshot.DespawnEventCount,
                activityTotalSpawnEventCount = _activitySnapshot == null ? 0 : _activitySnapshot.TotalSpawnEventCount,
                activityTotalDespawnEventCount = _activitySnapshot == null ? 0 : _activitySnapshot.TotalDespawnEventCount,
                stage14NPCActivityEvidence = Stage14NPCActivityEvidence,
                observability = _observability,
            };
        }

        private void RestoreLifecycle(Test_PerformancePendingRunState state)
        {
            ValidationDiagnostic = null;
            StopRequested = state.stopRequested;
            var telemetrySessionWasActive = state.telemetryStarted;
            _telemetryStarted = false;
            Finalized = state.finalized;
            if (Finalized)
                RestoreFinalizationFields(state);
            WorldFixtureLoaded = state.worldFixtureLoaded;
            PreflightCompleted = state.preflightCompleted;
            WorkloadApplied = state.workloadApplied;
            MeasurementStarted = state.measurementStarted;
            RampCompleted = state.rampCompleted;
            WarmupCompleted = state.warmupCompleted;
            MeasurementCompleted = state.measurementCompleted;
            RecoveryCompleted = state.recoveryCompleted;
            CleanupComplete = state.cleanupComplete;
            _currentPhase = (PerformanceMeasurementPhase)state.currentPhase;
            RampCompletedEntityCount = state.rampCompletedEntityCount;
            MeasurementStartEntityCount = state.measurementStartEntityCount;
            MeasurementEndEntityCount = state.measurementEndEntityCount;
            PeakEntityCount = state.peakEntityCount;
            PostCleanupObserved = state.postCleanupObserved;
            PostCleanupFixtureEntityCount = state.postCleanupFixtureEntityCount;
            PostCleanupActorEntityCount = state.postCleanupActorEntityCount;
            if (state.activitySnapshotCaptured)
            {
                _activitySnapshot = new Test_PerformanceActivitySnapshot(
                    state.activityIdleCount,
                    state.activityPatrollingCount,
                    state.activityCombatCount,
                    state.activityWakeSurgeCount,
                    state.activitySpawnEventCount,
                    state.activityDespawnEventCount,
                    state.activityTotalSpawnEventCount,
                    state.activityTotalDespawnEventCount);
                _activitySnapshotPhase = "restored_pre_cleanup";
            }
            Stage14NPCActivityEvidence = state.stage14NPCActivityEvidence;
            if (state.observability != null)
            {
                CopyObservability(state.observability, _observability);
                _observability.timing_domain = "mixed_monotonic_domains_after_reload";
                if (!MeasurementCompleted)
                {
                    _observability.measurement.actual_elapsed_seconds = -1d;
                    _observability.measurement.timing_known = false;
                    _observability.measurement_timing_known = false;
                    _observability.focus_observation_known = false;
                    _focusObservationForcedUnknown = true;
                }
            }
            if (telemetrySessionWasActive)
                AppendLog("Telemetry session restarted after restoring the pending Editor run.");
        }

        public void MarkWorkloadApplied()
        {
            if (IsGraphicalClient)
            {
                if (GraphicalRuntime == null || !WorldFixtureLoaded)
                    throw new InvalidOperationException("The graphical fixture scene must be loaded before marking the rendered workload active.");
                if (GraphicalRuntime.ActualObjectCount != GraphicalPlan.RequestedObjectCount
                    || GraphicalRuntime.ActualActorCount != GraphicalPlan.RequestedActorCount)
                {
                    throw new InvalidOperationException(
                        $"Graphical workload density was not achieved: objects {GraphicalRuntime.ActualObjectCount}/{GraphicalPlan.RequestedObjectCount}, actors {GraphicalRuntime.ActualActorCount}/{GraphicalPlan.RequestedActorCount}.");
                }

                PerformanceTelemetry.MarkWorkloadApplied();
                WorkloadApplied = true;
                RampCompletedEntityCount = GraphicalRuntime.ActualObjectCount;
                ObserveEntityCount(RampCompletedEntityCount);
                AppendLog("Requested graphical object workload applied: " + GraphicalRuntime.ActualObjectCount.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (IsStage14NPCActivity)
            {
                if (!WorldFixtureLoaded || FixtureRuntime == null || FixtureRuntime.SpawnedFullActorCount != RequestedActorCount)
                    throw new InvalidOperationException("The Stage 1.4 full networked fixture must be spawned before marking the workload active.");
                RampCompletedEntityCount = FixtureRuntime.SpawnedFullActorCount;
                ObserveEntityCount(RampCompletedEntityCount);
                PerformanceTelemetry.MarkWorkloadApplied();
                WorkloadApplied = true;
                AppendLog("Requested full networked NPC population applied: " + FixtureRuntime.SpawnedFullActorCount.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (!WorldFixtureLoaded || FixtureRuntime == null || ActivityController == null)
                throw new InvalidOperationException("The selected world and workload must be applied before marking the workload active.");
            if (FixtureRuntime.DataOnlyEntityCount != RequestedActorCount)
                throw new InvalidOperationException($"Workload applied with {FixtureRuntime.DataOnlyEntityCount} ECS entities; expected {RequestedActorCount}.");
            RampCompletedEntityCount = FixtureRuntime.DataOnlyEntityCount;
            ObserveEntityCount(RampCompletedEntityCount);
            PerformanceTelemetry.MarkWorkloadApplied();
            WorkloadApplied = true;
            AppendLog("Requested data-only population applied: " + FixtureRuntime.DataOnlyEntityCount.ToString(CultureInfo.InvariantCulture));
        }

        public void BeginPhase(PerformanceMeasurementPhase phase)
        {
            if (IsGraphicalClient && phase == PerformanceMeasurementPhase.Measurement)
            {
                if (GraphicalRuntime == null || !WorldFixtureLoaded)
                    throw new InvalidOperationException("The graphical fixture must exist before measurement begins.");
            }

            if (phase == PerformanceMeasurementPhase.Measurement)
            {
                if (!IsGraphicalClient && !IsStage14NPCActivity && (FixtureRuntime == null || ActivityController == null))
                    throw new InvalidOperationException("The data-only fixture must exist before measurement begins.");
                if (!IsGraphicalClient && !IsStage14NPCActivity && FixtureRuntime.DataOnlyEntityCount != RequestedActorCount)
                {
                    throw new InvalidOperationException(
                        $"Measurement cannot begin with {FixtureRuntime.DataOnlyEntityCount} ECS entities; expected {RequestedActorCount}.");
                }
            }

            if (!PerformanceTelemetry.BeginPhase(phase))
                throw new InvalidOperationException("Telemetry rejected phase transition to " + phase + ".");
            _currentPhase = phase;
            _phaseStartTimestamp = Stopwatch.GetTimestamp();
            var phaseTiming = GetPhaseTiming(phase);
            phaseTiming.configured_seconds = GetConfiguredPhaseSeconds(phase);
            phaseTiming.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            phaseTiming.timing_known = true;
            if (phase == PerformanceMeasurementPhase.Measurement)
            {
                if (!IsGraphicalClient && !IsStage14NPCActivity)
                    ActivityController.BeginMeasurement();
                MeasurementStarted = true;
                MeasurementStartEntityCount = IsGraphicalClient
                    ? GraphicalRuntime.ActualObjectCount
                    : IsStage14NPCActivity ? FixtureRuntime.SpawnedFullActorCount : FixtureRuntime.DataOnlyEntityCount;
                ObserveEntityCount(MeasurementStartEntityCount);
                _observability.measurement_frame_count = 0;
                _observability.measurement_sample_count = 0;
                _observability.measurement_timing_known = true;
                _observability.max_frame_delivery_gap_seconds = 0d;
                _observability.pause_gap_detected = false;
                _observability.pause_gap_detectable = true;
                _lastMeasurementFrameTimestamp = Stopwatch.GetTimestamp();
                _observability.focused_frame_count = 0;
                _observability.unfocused_frame_count = 0;
                _observability.unknown_focus_frame_count = 0;
                _observability.focused_seconds = 0d;
                _observability.unfocused_seconds = 0d;
                _observability.unknown_focus_seconds = 0d;
                _observability.focus_transition_count = 0;
                _focusStateActive = false;
                _focusObservationForcedUnknown = false;
                RecordFocusState(Application.isFocused, true);
            }
        }

        public void CaptureActivitySnapshot(string observationPhase)
        {
            if (_activitySnapshot != null || !MeasurementStarted || ActivityController == null)
                return;
            if (string.IsNullOrEmpty(observationPhase))
                throw new ArgumentException("An activity snapshot observation phase is required.", nameof(observationPhase));

            _activitySnapshot = ActivityController.CaptureSnapshot();
            _activitySnapshotPhase = observationPhase;
            AppendLog(
                $"Activity snapshot captured ({observationPhase}): idle={_activitySnapshot.IdleCount}, patrolling={_activitySnapshot.PatrollingCount}, combat={_activitySnapshot.CombatCount}, wakeSurge={_activitySnapshot.WakeSurgeCount}, measurementSpawnEvents={_activitySnapshot.SpawnEventCount}, measurementDespawnEvents={_activitySnapshot.DespawnEventCount}.");
        }

        public void MarkPhaseComplete(PerformanceMeasurementPhase phase)
        {
            var timing = GetPhaseTiming(phase);
            timing.completed_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            timing.actual_elapsed_seconds = ElapsedSeconds(_phaseStartTimestamp, Stopwatch.GetTimestamp());
            timing.timing_known = timing.actual_elapsed_seconds >= 0d;
            if (phase == PerformanceMeasurementPhase.Measurement)
            {
                CloseFocusInterval();
                _observability.measurement_timing_known = timing.timing_known;
                _observability.measurement_frame_count = Math.Max(0, _observability.measurement_frame_count);
            }
            switch (phase)
            {
                case PerformanceMeasurementPhase.Ramp:
                    RampCompleted = true;
                    break;
                case PerformanceMeasurementPhase.Warmup:
                    WarmupCompleted = true;
                    break;
                case PerformanceMeasurementPhase.Measurement:
                    MeasurementCompleted = true;
                    break;
                case PerformanceMeasurementPhase.Recovery:
                    RecoveryCompleted = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        public void RecordMeasurementFrame()
        {
            if (!MeasurementStarted || MeasurementCompleted)
                return;
            _observability.measurement_frame_count++;
            var now = Stopwatch.GetTimestamp();
            if (_lastMeasurementFrameTimestamp > 0 && now >= _lastMeasurementFrameTimestamp)
            {
                var gapSeconds = (now - _lastMeasurementFrameTimestamp) / (double)Stopwatch.Frequency;
                if (gapSeconds > _observability.max_frame_delivery_gap_seconds)
                    _observability.max_frame_delivery_gap_seconds = gapSeconds;
                if (gapSeconds > 1d)
                    _observability.pause_gap_detected = true;
            }
            _lastMeasurementFrameTimestamp = now;
            if (_focusObservationForcedUnknown)
                RecordFocusState(false, false);
            else
                RecordFocusState(Application.isFocused, true);
            if (!_observability.focus_observation_known)
                _observability.unknown_focus_frame_count++;
            else if (_focusState)
                _observability.focused_frame_count++;
            else
                _observability.unfocused_frame_count++;
        }

        public void RecordMeasurementSample()
        {
            if (MeasurementStarted && !MeasurementCompleted)
                _observability.measurement_sample_count++;
        }

        public void RecordFocusState(bool focused, bool known)
        {
            if (MeasurementCompleted)
                return;
            if (!MeasurementStarted)
            {
                _observability.focus_observation_known = known;
                if (known)
                {
                    _focusState = focused;
                    _focusObservationForcedUnknown = false;
                }
                else
                    _focusObservationForcedUnknown = true;
                return;
            }

            var now = Stopwatch.GetTimestamp();
            if (_focusStateActive)
                AddFocusDuration(now - _focusStateTimestamp, _focusState, _observability.focus_observation_known);
            if (_focusStateActive && _observability.focus_observation_known && known && _focusState != focused)
                _observability.focus_transition_count++;
            _focusState = focused;
            _focusStateTimestamp = now;
            _focusStateActive = true;
            _observability.focus_observation_known = known;
            _focusObservationForcedUnknown = !known;
        }

        public void RecordFocusUnknown()
        {
            RecordFocusState(false, false);
        }

        public void MarkCleanupComplete()
        {
            if (IsGraphicalClient)
            {
                if (GraphicalRuntime == null || !GraphicalRuntime.CleanupComplete)
                    throw new InvalidOperationException("Graphical fixture cleanup did not restore the camera and release its test-owned state.");
                CleanupComplete = true;
                AppendLog("Graphical fixture cleanup completed.");
                return;
            }

            if (FixtureRuntime == null)
                throw new InvalidOperationException("No actor fixture exists to clean up.");
            FixtureRuntime.AssertCleanupComplete();
            var cleanupReport = FixtureRuntime.LastCleanupReport;
            if (cleanupReport == null)
                throw new InvalidOperationException("Actor fixture cleanup did not produce a cleanup report.");
            CaptureCleanupReport(cleanupReport);
            CleanupComplete = true;
            AppendLog("Fixture cleanup returned to its captured baseline.");
        }

        public void CaptureCleanupReport(Test_ActorFixtureCleanupReport cleanupReport)
        {
            if (cleanupReport == null)
                return;
            PostCleanupObserved = true;
            PostCleanupFixtureEntityCount = cleanupReport.remainingFixtureEntities;
            PostCleanupActorEntityCount = cleanupReport.currentActorEntities;
        }

        public void ObserveEntityCount(int entityCount)
        {
            if (entityCount < 0)
                throw new ArgumentOutOfRangeException(nameof(entityCount));
            if (entityCount > PeakEntityCount)
                PeakEntityCount = entityCount;
            if (MeasurementStarted)
                MeasurementEndEntityCount = entityCount;
        }

        public void Complete(PerformanceRunStatus requestedStatus, string reasonCode, string message)
        {
            if (Finalized)
                return;

            var status = requestedStatus;
            var finalReason = reasonCode;
            var finalMessage = message;
            var telemetryFinalized = !_telemetryStarted;
            try
            {
                if (_telemetryStarted && PerformanceTelemetry.IsEnabled)
                {
                    telemetryFinalized = PerformanceTelemetry.StopSession(status, reasonCode, message);
                    _telemetrySummary = File.Exists(SummaryPath) ? File.ReadAllText(SummaryPath) : null;
                    if (status == PerformanceRunStatus.CompleteMeasured
                        && string.Equals(ExtractJsonStringValue(_telemetrySummary, "status"), "FAIL-INSTRUMENTATION", StringComparison.Ordinal))
                    {
                        status = PerformanceRunStatus.FailInstrumentation;
                        var telemetryStop = ExtractJsonValue(_telemetrySummary, "stop");
                        finalReason = ExtractJsonStringValue(telemetryStop, "reason_code") ?? "TELEMETRY_REJECTED";
                        finalMessage = ExtractJsonStringValue(telemetryStop, "message") ?? "Telemetry rejected the requested measured result.";
                    }
                }
            }
            catch (Exception exception)
            {
                status = PerformanceRunStatus.FailInstrumentation;
                finalReason = "TELEMETRY_FINALIZATION_FAILED";
                finalMessage = exception.Message;
                RuntimeDiagnostic = AppendDiagnostic(RuntimeDiagnostic, exception.Message);
            }

            _measurementSamplesAvailable = HasMeasurementSamples();
            if (IsStage14NPCActivity && status != PerformanceRunStatus.BlockedPrerequisite
                && !HasRequiredStage14Telemetry(out var missingTelemetry))
            {
                status = PerformanceRunStatus.FailInstrumentation;
                finalReason = "MISSING_REQUIRED_TELEMETRY";
                finalMessage = "FAIL-INSTRUMENTATION: required Stage 1.4 telemetry was unavailable or unmeasured: " + missingTelemetry;
            }
            if (IsGraphicalClient)
                EvaluateGraphicalRenderingEvidence();
            var graphicalLifecycleIncomplete = IsGraphicalClient
                && (!WorkloadApplied
                    || !RampCompleted
                    || !WarmupCompleted
                    || !MeasurementCompleted
                    || !RecoveryCompleted
                    || GraphicalRuntime == null
                    || RampCompletedEntityCount != GraphicalPlan.RequestedObjectCount
                    || MeasurementStartEntityCount != GraphicalPlan.RequestedObjectCount
                    || MeasurementEndEntityCount != GraphicalPlan.RequestedObjectCount
                    || PeakEntityCount < GraphicalPlan.RequestedObjectCount
                    || !MeasurementStarted
                    || !GraphicalRuntime.RouteComplete
                    || !CleanupComplete);
            var serverLifecycleIncomplete = !IsGraphicalClient
                && (!WorkloadApplied
                    || !RampCompleted
                    || !WarmupCompleted
                    || !MeasurementCompleted
                    || !RecoveryCompleted
                    || RampCompletedEntityCount != RequestedActorCount
                    || MeasurementStartEntityCount != RequestedActorCount
                    || MeasurementEndEntityCount != RequestedActorCount
                    || PeakEntityCount < RequestedActorCount
                    || !MeasurementStarted
                    || (!IsStage14NPCActivity && !ActivitySnapshotCaptured)
                    || (IsStage14NPCActivity && Stage14NPCActivityEvidence == null)
                    || !CleanupComplete);
            if (status == PerformanceRunStatus.CompleteMeasured
                && (graphicalLifecycleIncomplete || serverLifecycleIncomplete))
            {
                status = PerformanceRunStatus.FailCorrectness;
                finalReason = "INCOMPLETE_LIFECYCLE";
                finalMessage = "The run did not complete workload, population, measurement, activity evidence and cleanup invariants.";
            }
            if (status == PerformanceRunStatus.CompleteMeasured && !telemetryFinalized)
            {
                status = PerformanceRunStatus.FailInstrumentation;
                finalReason = "TELEMETRY_FINALIZATION_FAILED";
                finalMessage = "The telemetry session did not produce a finalized result.";
            }
            if (status == PerformanceRunStatus.CompleteMeasured && !_measurementSamplesAvailable)
            {
                status = PerformanceRunStatus.FailInstrumentation;
                finalReason = "NO_MEASUREMENT_SAMPLES";
                finalMessage = "Measurement entered but no bounded telemetry samples were written for the measurement phase.";
            }

            _finalStatus = status;
            _finalReasonCode = finalReason;
            _finalMessage = finalMessage;
            WriteSummary(_finalStatus, _finalReasonCode, _finalMessage);
            var logDirectory = Path.Combine(ResultsDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            File.WriteAllText(Path.Combine(logDirectory, "runner.log"), _log.ToString(), new UTF8Encoding(false));
            WriteReport(_finalStatus, _finalReasonCode, _finalMessage);
            WriteArtifacts();
            Finalized = true;
        }

        public void Block(string diagnostic)
        {
            ValidationDiagnostic = diagnostic;
            AppendLog("Run blocked: " + diagnostic);
            Complete(PerformanceRunStatus.BlockedPrerequisite, "PREREQUISITE_BLOCKED", diagnostic);
        }

        public void Fail(Exception exception, string reasonCode = "RUNNER_EXCEPTION", PerformanceRunStatus status = PerformanceRunStatus.FailCorrectness)
        {
            RuntimeDiagnostic = AppendDiagnostic(RuntimeDiagnostic, exception.ToString());
            AppendLog("Run failed: " + exception.Message);
            Complete(status, reasonCode, exception.Message);
        }

        public void AbortFromPlayModeExit()
        {
            if (Finalized)
                return;
            Complete(PerformanceRunStatus.FailInstrumentation, "EDITOR_PLAYMODE_EXITED", "Editor Play Mode exited before the runner finalized the workload.");
        }

        private bool HasMeasurementSamples()
        {
            var samplePath = Path.Combine(ResultsDirectory, "samples.ndjson");
            if (!File.Exists(samplePath))
                return false;

            foreach (var line in File.ReadLines(samplePath))
            {
                if (line.IndexOf("\"phase\":\"measurement\"", StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        private bool HasRequiredStage14Telemetry(out string missingTelemetry)
        {
            var required = new[]
            {
                PerformanceMetricIds.SERVER_ACTORS_FULL_COUNT,
                PerformanceMetricIds.SERVER_ACTORS_NEARBY_COUNT,
                PerformanceMetricIds.SERVER_ACTORS_DORMANT_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_PLAYER_CENTER_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_CELL_COUNT,
                PerformanceMetricIds.SERVER_SPATIAL_CANDIDATE_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_UNIQUE_NPC_CANDIDATE_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_LOCAL_PAIR_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_PERCEPTION_DECISION_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_MANAGER_NPC_AGGRO_DURATION,
                PerformanceMetricIds.SERVER_ACTOR_MANAGER_NPC_NAVIGATION_DURATION,
                PerformanceMetricIds.SERVER_ACTOR_MANAGER_NPC_NAVIGATION_SYNC_DURATION,
                PerformanceMetricIds.SERVER_ACTOR_NPC_NAVIGATION_ACTIVE_COUNT,
                PerformanceMetricIds.SERVER_ACTOR_NPC_ACTIVITY_REEVALUATION_DURATION,
                PerformanceMetricIds.SERVER_ACTOR_NPC_ACTIVITY_TRANSITION_COUNT,
                PerformanceMetricIds.SERVER_SIMULATION_TICK_DURATION,
                PerformanceMetricIds.SERVER_SIMULATION_TICK_BACKLOG,
                PerformanceMetricIds.SERVER_SIMULATION_DEADLINE_MISS_COUNT,
                PerformanceMetricIds.SERVER_SIMULATION_CATCH_UP_CAP_HIT_COUNT,
                PerformanceMetricIds.SERVER_MEMORY_TOTAL_ALLOCATED_BYTES,
                PerformanceMetricIds.SERVER_MEMORY_MONO_USED_BYTES,
                PerformanceMetricIds.SERVER_MEMORY_MONO_HEAP_BYTES,
                PerformanceMetricIds.SERVER_MEMORY_GC_GEN0_COLLECTION_COUNT,
                PerformanceMetricIds.SERVER_MEMORY_GC_GEN1_COLLECTION_COUNT,
                PerformanceMetricIds.SERVER_MEMORY_GC_GEN2_COLLECTION_COUNT,
            };
            var missing = new List<string>();
            Test_PerformanceTelemetrySummaryDocument telemetry;
            try
            {
                telemetry = JsonUtility.FromJson<Test_PerformanceTelemetrySummaryDocument>(_telemetrySummary);
            }
            catch (Exception exception)
            {
                missingTelemetry = "telemetry summary could not be parsed: " + exception.Message;
                return false;
            }
            foreach (var metricID in required)
            {
                var metric = FindMetric(telemetry, metricID);
                if (metric == null || metric.sample_count <= 0 || string.Equals(metric.observation, "unavailable", StringComparison.OrdinalIgnoreCase))
                    missing.Add(metricID);
            }
            missingTelemetry = string.Join(", ", missing);
            return missing.Count == 0;
        }

        private void EvaluateGraphicalRenderingEvidence()
        {
            _observability.rendering_qualification = "unverified";
            _observability.rendering_workload_verified = false;
            if (string.IsNullOrEmpty(_telemetrySummary))
            {
                _observability.rendering_probe_status = "unknown";
                _observability.rendering_reason = "RENDER_PROBES_UNAVAILABLE";
                return;
            }

            Test_PerformanceTelemetrySummaryDocument telemetry;
            try
            {
                telemetry = JsonUtility.FromJson<Test_PerformanceTelemetrySummaryDocument>(_telemetrySummary);
            }
            catch (Exception exception)
            {
                _observability.rendering_probe_status = "invalid_summary";
                _observability.rendering_reason = "RENDER_PROBES_UNAVAILABLE: " + exception.Message;
                return;
            }
            var drawCalls = FindMetric(telemetry, "client.graphics.draw_call_count");
            var triangles = FindMetric(telemetry, "client.graphics.triangle_count");
            var renderedFrames = FindMetric(telemetry, "client.rendered.frame_time")
                ?? FindMetric(telemetry, "client.rendered.frame_interval");
            _observability.rendering_frame_sample_count = renderedFrames == null ? 0 : renderedFrames.sample_count;
            _observability.has_draw_call_p50 = IsMeasuredMetric(drawCalls);
            _observability.has_triangle_p50 = IsMeasuredMetric(triangles);
            if (_observability.has_draw_call_p50)
                _observability.draw_call_p50 = drawCalls.p50;
            if (_observability.has_triangle_p50)
                _observability.triangle_p50 = triangles.p50;

            if (!IsMeasuredMetric(drawCalls) || !IsMeasuredMetric(triangles))
            {
                _observability.rendering_probe_status = "unsupported_or_unavailable";
                _observability.rendering_reason = "RENDER_PROBES_UNAVAILABLE";
                return;
            }

            if (drawCalls.p50 <= 0d || triangles.p50 <= 0d)
            {
                _observability.rendering_probe_status = "measured_zero";
                _observability.rendering_reason = "RENDER_WORKLOAD_UNVERIFIED";
                return;
            }

            if (renderedFrames == null || renderedFrames.sample_count <= 0)
            {
                _observability.rendering_probe_status = "geometry_measured_frame_stats_unavailable";
                _observability.rendering_reason = "RENDER_FRAME_PROBES_UNAVAILABLE";
                return;
            }

            _observability.rendering_probe_status = "measured_nonzero";
            _observability.rendering_qualification = "verified";
            _observability.rendering_workload_verified = true;
            _observability.rendering_reason = null;
        }

        private static bool IsMeasuredMetric(Test_PerformanceTelemetryMetricDocument metric)
        {
            return metric != null
                && (metric.observation == "measured" || metric.observation == "estimated")
                && metric.sample_count > 0;
        }

        private static Test_PerformanceTelemetryMetricDocument FindMetric(
            Test_PerformanceTelemetrySummaryDocument summary,
            string metricID)
        {
            foreach (var metric in summary?.metrics ?? Array.Empty<Test_PerformanceTelemetryMetricDocument>())
            {
                if (metric != null && string.Equals(metric.metric_id, metricID, StringComparison.Ordinal))
                    return metric;
            }
            return null;
        }

        private Test_PerformancePhaseTimingState GetPhaseTiming(PerformanceMeasurementPhase phase)
        {
            switch (phase)
            {
                case PerformanceMeasurementPhase.Ramp: return _observability.ramp;
                case PerformanceMeasurementPhase.Warmup: return _observability.warmup;
                case PerformanceMeasurementPhase.Measurement: return _observability.measurement;
                case PerformanceMeasurementPhase.Recovery: return _observability.recovery;
                default: throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        private double GetConfiguredPhaseSeconds(PerformanceMeasurementPhase phase)
        {
            switch (phase)
            {
                case PerformanceMeasurementPhase.Ramp:
                    return EffectiveRampSeconds;
                case PerformanceMeasurementPhase.Warmup:
                    return EffectiveWarmupSeconds;
                case PerformanceMeasurementPhase.Measurement:
                    return _observability.measurement.configured_seconds;
                case PerformanceMeasurementPhase.Recovery:
                    return EffectiveRecoverySeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        private void MarkLifecycleEvent(ref long timestamp, Action<string> setUtc)
        {
            timestamp = Stopwatch.GetTimestamp();
            setUtc(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        }

        private static double ElapsedSeconds(long startedTimestamp, long completedTimestamp)
        {
            if (startedTimestamp <= 0 || completedTimestamp < startedTimestamp)
                return -1d;
            return (completedTimestamp - startedTimestamp) / (double)Stopwatch.Frequency;
        }

        private void CloseFocusInterval()
        {
            if (!_focusStateActive)
                return;
            AddFocusDuration(Stopwatch.GetTimestamp() - _focusStateTimestamp, _focusState, _observability.focus_observation_known);
            _focusStateActive = false;
            _observability.focus_context = !_observability.focus_observation_known
                ? "unknown"
                : _observability.unfocused_frame_count > 0 || _observability.unfocused_seconds > 0d
                    ? "known_background"
                    : "known_foreground";
        }

        private void AddFocusDuration(long elapsedTimestamp, bool focused, bool known)
        {
            if (elapsedTimestamp < 0)
                return;
            var seconds = elapsedTimestamp / (double)Stopwatch.Frequency;
            if (!known)
                _observability.unknown_focus_seconds += seconds;
            else if (focused)
                _observability.focused_seconds += seconds;
            else
                _observability.unfocused_seconds += seconds;
        }

        private static void CopyObservability(Test_PerformanceObservabilityState source, Test_PerformanceObservabilityState target)
        {
            if (source == null || target == null)
                return;
            target.timing_domain = source.timing_domain;
            target.queue_dispatch_utc = source.queue_dispatch_utc;
            target.play_mode_entered_utc = source.play_mode_entered_utc;
            target.workload_finalized_utc = source.workload_finalized_utc;
            target.play_mode_exited_utc = source.play_mode_exited_utc;
            target.scene_restored_utc = source.scene_restored_utc;
            target.scene_restoration_status = source.scene_restoration_status;
            target.scene_restoration_diagnostic = source.scene_restoration_diagnostic;
            target.queue_dispatch_to_play_mode_entered_seconds = source.queue_dispatch_to_play_mode_entered_seconds;
            target.workload_finalized_to_play_mode_exited_seconds = source.workload_finalized_to_play_mode_exited_seconds;
            target.play_mode_exit_to_scene_restored_seconds = source.play_mode_exit_to_scene_restored_seconds;
            target.total_attempt_wall_clock_seconds = source.total_attempt_wall_clock_seconds;
            target.ramp = source.ramp ?? new Test_PerformancePhaseTimingState();
            target.warmup = source.warmup ?? new Test_PerformancePhaseTimingState();
            target.measurement = source.measurement ?? new Test_PerformancePhaseTimingState();
            target.recovery = source.recovery ?? new Test_PerformancePhaseTimingState();
            target.measurement_frame_count = source.measurement_frame_count;
            target.measurement_sample_count = source.measurement_sample_count;
            target.measurement_timing_known = source.measurement_timing_known;
            target.max_frame_delivery_gap_seconds = source.max_frame_delivery_gap_seconds;
            target.pause_gap_detected = source.pause_gap_detected;
            target.pause_gap_detectable = source.pause_gap_detectable;
            target.focus_transition_count = source.focus_transition_count;
            target.focused_frame_count = source.focused_frame_count;
            target.unfocused_frame_count = source.unfocused_frame_count;
            target.unknown_focus_frame_count = source.unknown_focus_frame_count;
            target.focused_seconds = source.focused_seconds;
            target.unfocused_seconds = source.unfocused_seconds;
            target.unknown_focus_seconds = source.unknown_focus_seconds;
            target.focus_observation_known = source.focus_observation_known;
            target.run_in_background = source.run_in_background;
            target.focus_context = source.focus_context;
            target.rendering_qualification = source.rendering_qualification;
            target.rendering_probe_status = source.rendering_probe_status;
            target.rendering_reason = source.rendering_reason;
            target.rendering_workload_verified = source.rendering_workload_verified;
            target.expected_visible_object_count = source.expected_visible_object_count;
            target.draw_call_p50 = source.draw_call_p50;
            target.triangle_p50 = source.triangle_p50;
            target.has_draw_call_p50 = source.has_draw_call_p50;
            target.has_triangle_p50 = source.has_triangle_p50;
            target.rendering_frame_sample_count = source.rendering_frame_sample_count;
        }

        public void AppendLog(string message)
        {
            if (_log.Length < 100000)
            {
                _log.Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))
                    .Append(" ").AppendLine(message);
            }
        }

        private void WriteFixtureAndRun(string projectRoot)
        {
            var fixtureJson = IsGraphicalClient
                ? Test_PerformanceEvidence.BuildGraphicalFixtureJson(Options, GraphicalPlan, WorldPlan, _metadata)
                : Test_PerformanceEvidence.BuildFixtureJson(Options, ActorProfile, Scenario, WorldPlan, _metadata);
            File.WriteAllText(FixturePath, fixtureJson, new UTF8Encoding(false));
            var runJson = Test_PerformanceEvidence.BuildRunJson(this, projectRoot, _metadata);
            File.WriteAllText(RunPath, runJson, new UTF8Encoding(false));
            AppendLog("Evidence directory created: " + ResultsDirectory);
        }

        private void UpdateFinalizedSummaryLifecycle()
        {
            if (!File.Exists(SummaryPath))
                throw new FileNotFoundException("The finalized performance summary is missing during lifecycle completion.", SummaryPath);

            var summaryJson = File.ReadAllText(SummaryPath);
            var orchestrationJson = BuildOrchestrationJson();
            var updatedSummary = ReplaceJsonObjectProperty(summaryJson, "orchestration", orchestrationJson);
            if (!string.Equals(summaryJson, updatedSummary, StringComparison.Ordinal))
                File.WriteAllText(SummaryPath, updatedSummary, new UTF8Encoding(false));
        }

        private void UpdateFinalizedReportLifecycle()
        {
            if (!File.Exists(ReportPath))
                throw new FileNotFoundException("The finalized performance report is missing during lifecycle completion.", ReportPath);

            const string orchestrationPrefix = "- Editor orchestration: ";
            var report = File.ReadAllText(ReportPath);
            var lineStart = report.IndexOf(orchestrationPrefix, StringComparison.Ordinal);
            if (lineStart < 0)
                throw new InvalidDataException("The finalized performance report is missing its Editor orchestration field.");

            var lineEnd = report.IndexOf('\n', lineStart);
            var contentEnd = lineEnd < 0 ? report.Length : lineEnd;
            if (contentEnd > lineStart && report[contentEnd - 1] == '\r')
                contentEnd--;
            var replacement = orchestrationPrefix + BuildOrchestrationDisplay();
            var updatedReport = report.Substring(0, lineStart) + replacement + report.Substring(contentEnd);
            if (!string.Equals(report, updatedReport, StringComparison.Ordinal))
                File.WriteAllText(ReportPath, updatedReport, new UTF8Encoding(false));
        }

        private void EnsureFinalizedEvidenceExists()
        {
            var missing = new List<string>();
            if (!File.Exists(FixturePath))
                missing.Add("fixture.json");
            if (!File.Exists(RunPath))
                missing.Add("run.json");
            if (!File.Exists(SummaryPath))
                missing.Add("summary.json");
            if (!File.Exists(ReportPath))
                missing.Add("report.md");
            if (missing.Count > 0)
                throw new FileNotFoundException("The finalized performance run is missing required evidence: " + string.Join(", ", missing));
        }

        private void RestoreFinalizationFields(Test_PerformancePendingRunState state)
        {
            var summaryJson = File.Exists(SummaryPath) ? File.ReadAllText(SummaryPath) : null;
            var statusText = state.finalizedStatus ?? ExtractJsonStringValue(summaryJson, "status");
            if (!TryParseStatus(statusText, out _finalStatus))
                throw new InvalidDataException("The finalized pending performance run has no recognized final status.");

            var stopJson = ExtractJsonValue(summaryJson, "stop");
            _finalReasonCode = state.finalizedReasonCode ?? ExtractJsonStringValue(stopJson, "reason_code");
            _finalMessage = state.finalizedMessage ?? ExtractJsonStringValue(stopJson, "message");
            if (string.IsNullOrEmpty(_finalReasonCode) || _finalMessage == null)
                throw new InvalidDataException("The finalized pending performance run has incomplete stop evidence.");
        }

        private static bool TryParseStatus(string statusText, out PerformanceRunStatus status)
        {
            switch (statusText)
            {
                case "COMPLETE-MEASURED": status = PerformanceRunStatus.CompleteMeasured; return true;
                case "BLOCKED-PREREQUISITE": status = PerformanceRunStatus.BlockedPrerequisite; return true;
                case "FAIL-CORRECTNESS": status = PerformanceRunStatus.FailCorrectness; return true;
                case "FAIL-PROVISION": status = PerformanceRunStatus.FailProvision; return true;
                case "FAIL-RESOURCE": status = PerformanceRunStatus.FailResource; return true;
                case "FAIL-TIMEOUT": status = PerformanceRunStatus.FailTimeout; return true;
                case "FAIL-INSTRUMENTATION": status = PerformanceRunStatus.FailInstrumentation; return true;
                default:
                    status = default;
                    return false;
            }
        }

        private static string ReplaceJsonObjectProperty(string json, string propertyName, string replacement)
        {
            var marker = Quote(propertyName) + ":";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (propertyIndex < 0)
            {
                var end = json.LastIndexOf('}');
                if (end < 0)
                    throw new InvalidDataException("The finalized performance summary is not a JSON object.");
                var prefix = json.Substring(0, end).TrimEnd();
                var separator = prefix.EndsWith("{", StringComparison.Ordinal) ? string.Empty : ",";
                return prefix + separator + marker + replacement + json.Substring(end);
            }

            var valueStart = propertyIndex + marker.Length;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                valueStart++;
            var valueEnd = FindJsonValueEnd(json, valueStart);
            return json.Substring(0, valueStart) + replacement + json.Substring(valueEnd);
        }

        private static int FindJsonValueEnd(string json, int valueStart)
        {
            if (valueStart >= json.Length || (json[valueStart] != '{' && json[valueStart] != '['))
                throw new InvalidDataException("The finalized performance summary orchestration property is not a JSON object.");
            var open = json[valueStart];
            var close = open == '[' ? ']' : '}';
            var depth = 0;
            var escaped = false;
            var inString = false;
            for (var index = valueStart; index < json.Length; index++)
            {
                var character = json[index];
                if (inString)
                {
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        inString = false;
                    continue;
                }
                if (character == '"')
                    inString = true;
                else if (character == open)
                    depth++;
                else if (character == close && --depth == 0)
                    return index + 1;
            }
            throw new InvalidDataException("The finalized performance summary orchestration property is incomplete.");
        }

        private void WriteSummary(PerformanceRunStatus status, string reasonCode, string message)
        {
            var metrics = ExtractJsonValue(_telemetrySummary, "metrics") ?? "[]";
            var rawFiles = BuildRawFilesJson();
            var finishedUtc = DateTime.UtcNow;
            var blockedPreflight = status == PerformanceRunStatus.BlockedPrerequisite;
            var correctnessStatus = status == PerformanceRunStatus.CompleteMeasured && CleanupComplete
                ? "pass"
                : blockedPreflight ? "unverified" : "failed";
            var failedAssertions = blockedPreflight || status == PerformanceRunStatus.CompleteMeasured ? 0 : 1;
            var unverified = IsGraphicalClient
                ? BuildGraphicalUnverifiedJson()
                : "[\"standalone_headless\",\"networked_clients\"]";
            var checkedAssertions = WorkloadApplied ? 4 : 0;
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Property(builder, "schema_version", "benchmark-summary/v1", true);
            Property(builder, "run_id", RunID, true);
            Property(builder, "fixture_sha256", FixtureHash, true);
            Property(builder, "status", StatusText(status), true);
            RawProperty(builder, "started_utc", blockedPreflight ? "null" : Quote(_startedUtc.ToString("o", CultureInfo.InvariantCulture)), true);
            RawProperty(builder, "finished_utc", blockedPreflight ? "null" : Quote(finishedUtc.ToString("o", CultureInfo.InvariantCulture)), true);
            RawProperty(builder, "phase_completion", $"{{\"preflight\":{(!blockedPreflight && PreflightCompleted).ToString().ToLowerInvariant()},\"ramp\":{RampCompleted.ToString().ToLowerInvariant()},\"warmup\":{WarmupCompleted.ToString().ToLowerInvariant()},\"measurement\":{MeasurementCompleted.ToString().ToLowerInvariant()},\"recovery\":{RecoveryCompleted.ToString().ToLowerInvariant()}}}", true);
            RawProperty(builder, "measurement", BuildMeasurementJson(), true);
            RawProperty(builder, "phase_timing", BuildPhaseTimingJson(), true);
            RawProperty(builder, "orchestration", BuildOrchestrationJson(), true);
            RawProperty(builder, "focus", BuildFocusJson(), true);
            RawProperty(builder, "rendering", BuildRenderingJson(status), true);
            RawProperty(builder, "achieved", BuildAchievedJson(), true);
            if (IsStage14NPCActivity)
                RawProperty(builder, "npc_activity", BuildStage14NPCActivityEvidenceJson(), true);
            RawProperty(
                builder,
                "resources",
                BuildResourceEvidenceJson(
                    ActivityController == null ? (long?)null : ActivityController.BaselineAllocatedBytes,
                    UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                    status),
                true);
            RawProperty(builder, "stop", $"{{\"phase\":{Quote(CurrentPhaseText())},\"reason_code\":{Quote(reasonCode)},\"message\":{Quote(message)}}}", true);
            RawProperty(builder, "correctness", $"{{\"status\":{Quote(correctnessStatus)},\"checked\":{checkedAssertions},\"failed\":{failedAssertions},\"unverified\":{unverified},\"cleanup\":{CleanupComplete.ToString().ToLowerInvariant()}}}", true);
            RawProperty(builder, "metrics", metrics, true);
            RawProperty(builder, "raw_files", rawFiles, true);
            RawProperty(builder, "warnings", BuildWarnings(status, IsGraphicalClient), false);
            builder.AppendLine();
            builder.Append('}');
            File.WriteAllText(SummaryPath, builder.ToString(), new UTF8Encoding(false));
        }

        private string BuildMeasurementJson()
        {
            return "{\"configured_seconds\":" + NumberOrNull(_observability.measurement.configured_seconds)
                + ",\"actual_elapsed_seconds\":" + NumberOrNull(_observability.measurement.actual_elapsed_seconds)
                + ",\"timing_known\":" + _observability.measurement_timing_known.ToString().ToLowerInvariant()
                + ",\"measured_frame_count\":" + _observability.measurement_frame_count.ToString(CultureInfo.InvariantCulture)
                + ",\"measured_sample_count\":" + _observability.measurement_sample_count.ToString(CultureInfo.InvariantCulture)
                + ",\"pause_gap_detected\":" + _observability.pause_gap_detected.ToString().ToLowerInvariant()
                + ",\"max_frame_delivery_gap_seconds\":" + NumberOrNull(_observability.max_frame_delivery_gap_seconds)
                + ",\"pause_gap_reason\":" + Quote(_observability.pause_gap_detectable
                    ? _observability.pause_gap_detected ? "frame_delivery_gap_over_1s" : "none_over_1s"
                    : "timing_unknown") + "}";
        }

        private string BuildStage14NPCActivityEvidenceJson()
        {
            var evidence = Stage14NPCActivityEvidence;
            if (evidence == null)
                return "null";
            var json = "{\"npc_activity_scenario\":" + Quote(evidence.npc_activity_scenario)
                + ",\"requested_npc_count\":" + evidence.requested_npc_count.ToString(CultureInfo.InvariantCulture)
                + ",\"achieved_full_npc_count\":" + evidence.achieved_full_npc_count.ToString(CultureInfo.InvariantCulture)
                + ",\"required_real_player_count\":" + evidence.required_real_player_count.ToString(CultureInfo.InvariantCulture)
                + ",\"achieved_real_player_count\":" + evidence.achieved_real_player_count.ToString(CultureInfo.InvariantCulture)
                + ",\"anchor_actor_id\":" + Quote(evidence.anchor_actor_id)
                + ",\"anchor_initial_context\":" + Quote(evidence.anchor_initial_context)
                + ",\"anchor_initial_position\":" + BuildDoubleArrayJson(evidence.anchor_initial_position);
            if (string.Equals(evidence.npc_activity_scenario, Test_NPCActivityScenario.MassWake.ToString(), StringComparison.Ordinal))
                json += ",\"mass_wake_cluster_centre\":" + BuildDoubleArrayJson(evidence.mass_wake_cluster_centre)
                    + ",\"mass_wake_reached\":" + evidence.mass_wake_reached.ToString().ToLowerInvariant();
            if (string.Equals(evidence.npc_activity_scenario, Test_NPCActivityScenario.CrossInstance.ToString(), StringComparison.Ordinal))
                json += ",\"context_transition_source\":" + Quote(evidence.context_transition_source)
                    + ",\"context_transition_destination\":" + Quote(evidence.context_transition_destination);
            return json + "}";
        }

        private static string BuildDoubleArrayJson(double[] values)
        {
            if (values == null)
                return "null";
            var formatted = new string[values.Length];
            for (var index = 0; index < values.Length; index++)
                formatted[index] = values[index].ToString("R", CultureInfo.InvariantCulture);
            return "[" + string.Join(",", formatted) + "]";
        }

        private string BuildPhaseTimingJson()
        {
            return "{\"timing_domain\":" + Quote(_observability.timing_domain)
                + ",\"ramp\":" + BuildPhaseTiming(_observability.ramp)
                + ",\"warmup\":" + BuildPhaseTiming(_observability.warmup)
                + ",\"measurement\":" + BuildPhaseTiming(_observability.measurement)
                + ",\"recovery\":" + BuildPhaseTiming(_observability.recovery) + "}";
        }

        private static string BuildPhaseTiming(Test_PerformancePhaseTimingState timing)
        {
            return "{\"started_utc\":" + Quote(timing.started_utc)
                + ",\"completed_utc\":" + Quote(timing.completed_utc)
                + ",\"configured_seconds\":" + NumberOrNull(timing.configured_seconds)
                + ",\"actual_elapsed_seconds\":" + NumberOrNull(timing.actual_elapsed_seconds)
                + ",\"timing_known\":" + timing.timing_known.ToString().ToLowerInvariant() + "}";
        }

        private string BuildOrchestrationJson()
        {
            return "{\"queue_dispatch_utc\":" + Quote(_observability.queue_dispatch_utc)
                + ",\"play_mode_entered_utc\":" + Quote(_observability.play_mode_entered_utc)
                + ",\"workload_finalized_utc\":" + Quote(_observability.workload_finalized_utc)
                + ",\"play_mode_exited_utc\":" + Quote(_observability.play_mode_exited_utc)
                + ",\"scene_restored_utc\":" + Quote(_observability.scene_restored_utc)
                + ",\"queue_dispatch_to_play_mode_entered_seconds\":" + NumberOrNull(_observability.queue_dispatch_to_play_mode_entered_seconds)
                + ",\"workload_finalized_to_play_mode_exited_seconds\":" + NumberOrNull(_observability.workload_finalized_to_play_mode_exited_seconds)
                + ",\"play_mode_exit_to_scene_restored_seconds\":" + NumberOrNull(_observability.play_mode_exit_to_scene_restored_seconds)
                + ",\"total_attempt_wall_clock_seconds\":" + NumberOrNull(_observability.total_attempt_wall_clock_seconds)
                + ",\"scene_restoration_status\":" + Quote(_observability.scene_restoration_status)
                + ",\"scene_restoration_diagnostic\":" + Quote(_observability.scene_restoration_diagnostic)
                + ",\"timing_domain\":" + Quote(_observability.timing_domain) + "}";
        }

        private string BuildFocusJson()
        {
            return "{\"context\":" + Quote(_observability.focus_context)
                + ",\"observation_known\":" + _observability.focus_observation_known.ToString().ToLowerInvariant()
                + ",\"run_in_background\":" + _observability.run_in_background.ToString().ToLowerInvariant()
                + ",\"transition_count\":" + _observability.focus_transition_count.ToString(CultureInfo.InvariantCulture)
                + ",\"focused_frame_count\":" + _observability.focused_frame_count.ToString(CultureInfo.InvariantCulture)
                + ",\"unfocused_frame_count\":" + _observability.unfocused_frame_count.ToString(CultureInfo.InvariantCulture)
                + ",\"unknown_focus_frame_count\":" + _observability.unknown_focus_frame_count.ToString(CultureInfo.InvariantCulture)
                + ",\"focused_seconds\":" + NumberOrNull(_observability.focused_seconds)
                + ",\"unfocused_seconds\":" + NumberOrNull(_observability.unfocused_seconds)
                + ",\"unknown_focus_seconds\":" + NumberOrNull(_observability.unknown_focus_seconds) + "}";
        }

        private string BuildRenderingJson(PerformanceRunStatus status)
        {
            if (!IsGraphicalClient)
                return "{\"execution_status\":\"not_applicable\",\"qualification_status\":\"not_applicable\"}";
            return "{\"execution_status\":" + Quote(status == PerformanceRunStatus.CompleteMeasured ? "complete" : "failed")
                + ",\"qualification_status\":" + Quote(_observability.rendering_qualification)
                + ",\"probe_status\":" + Quote(_observability.rendering_probe_status)
                + ",\"reason_code\":" + Quote(_observability.rendering_reason)
                + ",\"expected_visible_geometry\":" + (_observability.expected_visible_object_count > 0 ? "true" : "false")
                + ",\"expected_visible_object_count\":" + _observability.expected_visible_object_count.ToString(CultureInfo.InvariantCulture)
                + ",\"draw_call_p50\":" + (_observability.has_draw_call_p50 ? _observability.draw_call_p50.ToString("R", CultureInfo.InvariantCulture) : "null")
                + ",\"triangle_p50\":" + (_observability.has_triangle_p50 ? _observability.triangle_p50.ToString("R", CultureInfo.InvariantCulture) : "null")
                + ",\"rendering_frame_sample_count\":" + _observability.rendering_frame_sample_count.ToString(CultureInfo.InvariantCulture)
                + ",\"workload_verified\":" + _observability.rendering_workload_verified.ToString().ToLowerInvariant() + "}";
        }

        private string BuildMeasurementDisplay()
        {
            return "configured " + _observability.measurement.configured_seconds.ToString("0.###", CultureInfo.InvariantCulture)
                + " s; actual " + FormatDuration(_observability.measurement.actual_elapsed_seconds)
                + "; " + _observability.measurement_frame_count.ToString(CultureInfo.InvariantCulture)
                + " frames / " + _observability.measurement_sample_count.ToString(CultureInfo.InvariantCulture) + " samples";
        }

        private string BuildOrchestrationDisplay()
        {
            return "Play Mode startup " + FormatDuration(_observability.queue_dispatch_to_play_mode_entered_seconds)
                + "; teardown " + FormatDuration(_observability.workload_finalized_to_play_mode_exited_seconds)
                + "; scene restore " + (_observability.scene_restoration_status ?? "unknown") + " (" + FormatDuration(_observability.play_mode_exit_to_scene_restored_seconds) + ")"
                + (string.IsNullOrEmpty(_observability.scene_restoration_diagnostic) ? string.Empty : "; " + _observability.scene_restoration_diagnostic)
                + "; total attempt wall-clock " + FormatDuration(_observability.total_attempt_wall_clock_seconds);
        }

        private string BuildFocusDisplay()
        {
            return _observability.focus_context + "; transitions " + _observability.focus_transition_count.ToString(CultureInfo.InvariantCulture)
                + "; focused " + _observability.focused_frame_count.ToString(CultureInfo.InvariantCulture)
                + "; unfocused " + _observability.unfocused_frame_count.ToString(CultureInfo.InvariantCulture)
                + "; unknown " + _observability.unknown_focus_frame_count.ToString(CultureInfo.InvariantCulture)
                + "; runInBackground " + _observability.run_in_background;
        }

        private string BuildRenderingDisplay()
        {
            if (!IsGraphicalClient)
                return "not_applicable";
            return _observability.rendering_qualification + "; probe " + _observability.rendering_probe_status
                + (string.IsNullOrEmpty(_observability.rendering_reason) ? string.Empty : "; " + _observability.rendering_reason);
        }

        private static string FormatDuration(double seconds)
        {
            return seconds < 0d ? "unknown" : seconds.ToString("0.###", CultureInfo.InvariantCulture) + " s";
        }

        private static string NumberOrNull(double value)
        {
            return value < 0d ? "null" : value.ToString("R", CultureInfo.InvariantCulture);
        }

        private string BuildRawFilesJson()
        {
            var files = new List<string>();
            var samplePath = Path.Combine(ResultsDirectory, "samples.ndjson");
            if (File.Exists(samplePath))
                files.Add(BuildRawFileEntry("samples.ndjson", samplePath));
            return "[" + string.Join(",", files) + "]";
        }

        private string BuildGraphicalUnverifiedJson()
        {
            var unverified = new List<string>();
            if (GraphicalPlan.RequiresRealClientSession)
                unverified.Add("real_player_session");
            if (GraphicalPlan.RequiresWorldStreaming)
                unverified.Add("normal_world_streaming");
            if (unverified.Count == 0)
                return "[]";
            return "[\"" + string.Join("\",\"", unverified) + "\"]";
        }

        private string BuildAchievedJson()
        {
            if (IsGraphicalClient)
            {
                var objectCount = GraphicalRuntime == null ? 0 : GraphicalRuntime.ActualObjectCount;
                var visibleCount = GraphicalRuntime == null ? 0 : GraphicalRuntime.VisibleObjectCount;
                var routeWaypoints = GraphicalRuntime == null ? 0 : GraphicalRuntime.RouteWaypointsReached;
                var routeComplete = GraphicalRuntime != null && GraphicalRuntime.RouteComplete;
                return "{\"connected_players\":" + GetRealPlayerCount()
                    + ",\"authenticated_players\":0,\"in_world_players\":0,\"npc_actors\":" + (GraphicalRuntime == null ? 0 : GraphicalRuntime.ActualActorCount)
                    + ",\"ecs_entities\":0,\"network_identities\":0,\"gameobjects\":" + objectCount
                    + ",\"scene_tiles_loaded\":" + (WorldFixtureLoaded ? 1 : 0) + ",\"renders_visible\":" + visibleCount
                    + ",\"accepted_abilities\":0,\"graphical\":{\"requested_objects\":" + GraphicalPlan.RequestedObjectCount
                    + ",\"achieved_objects\":" + objectCount + ",\"requested_actors\":" + GraphicalPlan.RequestedActorCount
                    + ",\"achieved_actors\":" + (GraphicalRuntime == null ? 0 : GraphicalRuntime.ActualActorCount)
                    + ",\"route_waypoints_requested\":" + GraphicalPlan.Waypoints.Length + ",\"route_waypoints_reached\":" + routeWaypoints
                    + ",\"route_complete\":" + routeComplete.ToString().ToLowerInvariant() + ",\"requested_nameplates\":" + GraphicalPlan.RequestedNameplateCount
                    + ",\"requested_vfx\":" + GraphicalPlan.RequestedVFXCount + "}}";
            }

            var measuredEntityCount = MeasurementStarted
                ? MeasurementEndEntityCount
                : RampCompletedEntityCount;
            var postCleanupFixtureEntities = PostCleanupObserved ? PostCleanupFixtureEntityCount.ToString(CultureInfo.InvariantCulture) : "null";
            var postCleanupActorEntities = PostCleanupObserved ? PostCleanupActorEntityCount.ToString(CultureInfo.InvariantCulture) : "null";
            if (IsStage14NPCActivity)
            {
                var stage14NPCCount = Stage14NPCActivityEvidence == null ? 0 : Stage14NPCActivityEvidence.achieved_full_npc_count;
                var stage14PlayerCount = Stage14NPCActivityEvidence == null ? 0 : Stage14NPCActivityEvidence.achieved_real_player_count;
                return $"{{\"connected_players\":{stage14PlayerCount},\"authenticated_players\":{stage14PlayerCount},\"in_world_players\":{stage14PlayerCount},\"npc_actors\":{stage14NPCCount},\"ecs_entities\":{measuredEntityCount},\"network_identities\":{stage14NPCCount},\"gameobjects\":{stage14NPCCount},\"scene_tiles_loaded\":{(WorldFixtureLoaded ? 1 : 0)},\"renders_visible\":null,\"accepted_abilities\":0,\"population\":{{\"requested_ecs_entities\":0,\"requested_full_npc_count\":{RequestedActorCount},\"achieved_full_npc_count\":{stage14NPCCount},\"ramp_completed_ecs_entities\":{RampCompletedEntityCount},\"measurement_start_ecs_entities\":{MeasurementStartEntityCount},\"measurement_end_ecs_entities\":{MeasurementEndEntityCount},\"peak_ecs_entities\":{PeakEntityCount},\"post_cleanup_fixture_entities\":{postCleanupFixtureEntities},\"post_cleanup_total_actor_entities\":{postCleanupActorEntities}}},\"activity\":{BuildActivityJson()}}}";
            }
            return $"{{\"connected_players\":{GetRealPlayerCount()},\"authenticated_players\":0,\"in_world_players\":0,\"npc_actors\":0,\"ecs_entities\":{measuredEntityCount},\"network_identities\":0,\"gameobjects\":0,\"scene_tiles_loaded\":{(WorldFixtureLoaded ? 1 : 0)},\"renders_visible\":null,\"accepted_abilities\":0,\"population\":{{\"requested_ecs_entities\":{RequestedActorCount},\"ramp_completed_ecs_entities\":{RampCompletedEntityCount},\"measurement_start_ecs_entities\":{MeasurementStartEntityCount},\"measurement_end_ecs_entities\":{MeasurementEndEntityCount},\"peak_ecs_entities\":{PeakEntityCount},\"post_cleanup_fixture_entities\":{postCleanupFixtureEntities},\"post_cleanup_total_actor_entities\":{postCleanupActorEntities}}},\"activity\":{BuildActivityJson()}}}";
        }

        internal static string BuildActivityJson(Test_PerformanceActivitySnapshot snapshot, string observationPhase)
        {
            if (snapshot == null)
            {
                var observation = Quote("unverified");
                return $"{{\"snapshot_captured\":false,\"observation\":{observation},\"idle\":0,\"patrolling\":0,\"combat\":0,\"wake_surge\":0,\"spawn_events\":0,\"despawn_events\":0,\"spawn_events_total\":0,\"despawn_events_total\":0,\"wake_surge_is_subset_of_combat\":true}}";
            }
            if (string.IsNullOrEmpty(observationPhase))
                throw new ArgumentException("An activity snapshot observation phase is required.", nameof(observationPhase));

            return $"{{\"snapshot_captured\":true,\"observation\":{Quote(observationPhase)},\"idle\":{snapshot.IdleCount},\"patrolling\":{snapshot.PatrollingCount},\"combat\":{snapshot.CombatCount},\"wake_surge\":{snapshot.WakeSurgeCount},\"spawn_events\":{snapshot.SpawnEventCount},\"despawn_events\":{snapshot.DespawnEventCount},\"spawn_events_total\":{snapshot.TotalSpawnEventCount},\"despawn_events_total\":{snapshot.TotalDespawnEventCount},\"wake_surge_is_subset_of_combat\":true}}";
        }

        internal static string BuildResourceEvidenceJson(
            long? baselineAllocatedBytes,
            long totalAllocatedBytes,
            PerformanceRunStatus status)
        {
            var baselineJson = baselineAllocatedBytes.HasValue
                ? baselineAllocatedBytes.Value.ToString(CultureInfo.InvariantCulture)
                : "null";
            var growthJson = baselineAllocatedBytes.HasValue
                ? Test_PerformanceActivityController.GetAllocatedGrowthBytes(baselineAllocatedBytes.Value, totalAllocatedBytes).ToString(CultureInfo.InvariantCulture)
                : "null";
            var ceilingJson = MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES.ToString(CultureInfo.InvariantCulture);
            var ceilingStatus = baselineAllocatedBytes.HasValue
                ? status == PerformanceRunStatus.FailResource ? "exceeded" : "not_exceeded"
                : "unavailable";
            return "{\"baseline_total_allocated_bytes\":" + baselineJson
                + ",\"total_allocated_bytes\":" + totalAllocatedBytes.ToString(CultureInfo.InvariantCulture)
                + ",\"allocated_growth_bytes\":" + growthJson
                + ",\"growth_ceiling_bytes\":" + ceilingJson
                + ",\"ceiling_basis\":" + Quote("growth_from_pre_workload_baseline")
                + ",\"ceiling_status\":" + Quote(ceilingStatus) + "}";
        }

        private string BuildActivityJson()
        {
            return BuildActivityJson(_activitySnapshot, _activitySnapshotPhase);
        }

        private static string BuildRawFileEntry(string relativePath, string path)
        {
            return $"{{\"path\":{Quote(relativePath)},\"bytes\":{new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture)},\"sha256\":{Quote(Test_PerformanceRunMetadata.HashFile(path))}}}";
        }

        private string CurrentPhaseText()
        {
            return _currentPhase.ToString().ToLowerInvariant();
        }

        private void WriteReport(PerformanceRunStatus status, string reasonCode, string message)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Performance workload run");
            builder.AppendLine();
            builder.AppendLine("- Status: `" + StatusText(status) + "`");
            builder.AppendLine("- Run ID: `" + RunID + "`");
            builder.AppendLine("- Profile: `" + Options.EffectiveProfileID + "`");
            builder.AppendLine("- World fixture: `" + (IsGraphicalClient ? GraphicalPlan.ScenarioID : Options.WorldFixture.ToString()) + "` (`" + (WorldPlan == null ? "unavailable" : WorldPlan.AssetPath) + "`)");
            builder.AppendLine("- Workload: `" + Options.WorkloadMode + "`");
            builder.AppendLine("- Seed: `" + Options.Seed.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine("- Git commit: `" + _metadata.Commit + "` (dirty: `" + _metadata.Dirty.ToString().ToLowerInvariant() + "`)");
            if (IsGraphicalClient)
            {
                builder.AppendLine("- Requested graphical objects: `" + GraphicalPlan.RequestedObjectCount + "`");
                builder.AppendLine("- Achieved graphical objects: `" + (GraphicalRuntime == null ? "unverified" : GraphicalRuntime.ActualObjectCount.ToString(CultureInfo.InvariantCulture)) + "`");
                builder.AppendLine("- Requested graphical actors: `" + GraphicalPlan.RequestedActorCount + "`");
                builder.AppendLine("- Achieved graphical actors: `" + (GraphicalRuntime == null ? "unverified" : GraphicalRuntime.ActualActorCount.ToString(CultureInfo.InvariantCulture)) + "`");
                builder.AppendLine("- Route coverage: `" + (GraphicalRuntime == null ? "unverified" : GraphicalRuntime.RouteWaypointsReached + "/" + GraphicalPlan.Waypoints.Length) + "` waypoints");
            }
            else
            {
                builder.AppendLine(IsStage14NPCActivity
                    ? "- Requested full networked NPCs: `" + RequestedActorCount + "`"
                    : "- Requested ECS entities: `" + RequestedActorCount + "`");
                if (IsStage14NPCActivity && Stage14NPCActivityEvidence != null)
                {
                    builder.AppendLine("- NPC activity scenario: `" + Stage14NPCActivityEvidence.npc_activity_scenario + "`");
                    builder.AppendLine("- Real players achieved: `" + Stage14NPCActivityEvidence.achieved_real_player_count + "/" + Stage14NPCActivityEvidence.required_real_player_count + "`");
                    builder.AppendLine("- Full NPCs achieved: `" + Stage14NPCActivityEvidence.achieved_full_npc_count + "/" + Stage14NPCActivityEvidence.requested_npc_count + "`");
                }
            }
            builder.AppendLine("- Ramp-completed ECS entities: `" + RampCompletedEntityCount + "`");
            builder.AppendLine("- Measurement-start ECS entities: `" + MeasurementStartEntityCount + "`");
            builder.AppendLine("- Measurement-end ECS entities: `" + MeasurementEndEntityCount + "`");
            builder.AppendLine("- Peak ECS entities observed: `" + PeakEntityCount + "`");
            builder.AppendLine("- Post-cleanup fixture entities: `" + (PostCleanupObserved ? PostCleanupFixtureEntityCount.ToString(CultureInfo.InvariantCulture) : "unverified") + "`");
            builder.AppendLine("- Post-cleanup total actor entities: `" + (PostCleanupObserved ? PostCleanupActorEntityCount.ToString(CultureInfo.InvariantCulture) : "unverified") + "`");
            if (_activitySnapshot != null)
            {
                builder.AppendLine("- Measured activity snapshot (`" + _activitySnapshotPhase + "`, before teardown): idle=`" + _activitySnapshot.IdleCount + "`, patrolling=`" + _activitySnapshot.PatrollingCount + "`, combat=`" + _activitySnapshot.CombatCount + "`, wake surge=`" + _activitySnapshot.WakeSurgeCount + "`");
                builder.AppendLine("- Measured activity events: spawn=`" + _activitySnapshot.SpawnEventCount + "`, despawn=`" + _activitySnapshot.DespawnEventCount + "`");
                builder.AppendLine("- Total spawn events including ramp: `" + _activitySnapshot.TotalSpawnEventCount + "`");
                builder.AppendLine("- Wake surge is included in combat and is not added as a disjoint population.");
            }
            else
            {
                builder.AppendLine("- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)");
            }
            builder.AppendLine("- Report directory: `" + ResultsDirectory + "`");
            builder.AppendLine("- Measurement: " + BuildMeasurementDisplay());
            builder.AppendLine("- Editor orchestration: " + BuildOrchestrationDisplay());
            builder.AppendLine("- Focus context: " + BuildFocusDisplay());
            if (IsGraphicalClient)
                builder.AppendLine("- Rendering qualification: " + BuildRenderingDisplay());
            builder.AppendLine();
            builder.AppendLine("## Result");
            builder.AppendLine();
            builder.AppendLine(message);
            builder.AppendLine();
            builder.AppendLine("Stop reason: `" + reasonCode + "`");
            builder.AppendLine();
            builder.AppendLine(IsGraphicalClient
                ? "This is an Editor Play Mode rendered-client measurement. It does not qualify authenticated player density, normal PurrNet world streaming, or multi-client network behavior."
                : IsStage14NPCActivity
                    ? "This is an Editor Play Mode full-networked NPC measurement using real PlayerActor connections. It does not qualify standalone headless capacity or production-scale client capacity."
                    : "This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.");
            if (status == PerformanceRunStatus.BlockedPrerequisite)
                builder.AppendLine("No workload execution occurred.");
            if (!string.IsNullOrEmpty(ValidationDiagnostic))
                builder.AppendLine("\nDiagnostic: " + ValidationDiagnostic);
            File.WriteAllText(ReportPath, builder.ToString(), new UTF8Encoding(false));
        }

        private void WriteArtifacts()
        {
            var entries = new List<string>();
            var requiredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fixture.json",
                "run.json",
                "summary.json",
                "report.md",
            };
            var optionalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "samples.ndjson",
                "logs/runner.log",
            };
            var presentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(ResultsDirectory, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(path), "artifacts.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                var relative = path.Substring(ResultsDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                presentPaths.Add(relative);
                var required = requiredPaths.Contains(relative);
                var optional = optionalPaths.Contains(relative) || !required;
                entries.Add($"{{\"path\":{Quote(relative)},\"status\":\"present\",\"required\":{required.ToString().ToLowerInvariant()},\"optional\":{optional.ToString().ToLowerInvariant()},\"sha256\":{Quote(Test_PerformanceRunMetadata.HashFile(path))},\"bytes\":{new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture)},\"media_type\":{Quote(GuessMediaType(path))},\"collector\":\"test_performance_harness\"}}");
            }
            foreach (var relative in requiredPaths)
            {
                if (!presentPaths.Contains(relative))
                    entries.Add($"{{\"path\":{Quote(relative)},\"status\":\"absent\",\"required\":true,\"optional\":false,\"sha256\":\"\",\"bytes\":0,\"media_type\":{Quote(GuessMediaType(relative))},\"collector\":\"test_performance_harness\"}}");
            }
            foreach (var relative in optionalPaths)
            {
                if (!presentPaths.Contains(relative))
                    entries.Add($"{{\"path\":{Quote(relative)},\"status\":\"absent\",\"required\":false,\"optional\":true,\"sha256\":\"\",\"bytes\":0,\"media_type\":{Quote(GuessMediaType(relative))},\"collector\":\"test_performance_harness\"}}");
            }
            entries.Sort(StringComparer.Ordinal);
            File.WriteAllText(
                Path.Combine(ResultsDirectory, "artifacts.json"),
                "{\"schema_version\":\"benchmark-artifacts/v1\",\"run_id\":" + Quote(RunID) + ",\"files\":[" + string.Join(",", entries) + "]}",
                new UTF8Encoding(false));
        }

        private static string GuessMediaType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".json": return "application/json";
                case ".ndjson": return "application/x-ndjson";
                case ".md": return "text/markdown";
                case ".log": return "text/plain";
                default: return "application/octet-stream";
            }
        }

        private int GetRealPlayerCount()
        {
            return PurrNet.NetworkManager.main == null ? 0 : PurrNet.NetworkManager.main.playerCount;
        }

        private string BuildGraphicsIdentity()
        {
            if (!IsGraphicalClient)
                return SystemInfo.graphicsDeviceName;

            var pipeline = GraphicsSettings.currentRenderPipeline;
            return string.Join(
                ";",
                SystemInfo.graphicsDeviceType,
                SystemInfo.graphicsDeviceName,
                "render_pipeline=" + Test_PerformanceEvidence.BuildRendererIdentity(pipeline),
                "resolution=" + Screen.width.ToString(CultureInfo.InvariantCulture) + "x" + Screen.height.ToString(CultureInfo.InvariantCulture),
                "quality=" + QualitySettings.GetQualityLevel().ToString(CultureInfo.InvariantCulture),
                "vsync=" + QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture),
                "target_fps=" + Application.targetFrameRate.ToString(CultureInfo.InvariantCulture));
        }

        private string BuildWarnings(PerformanceRunStatus status, bool graphical)
        {
            var warnings = new List<string>
            {
                graphical ? "Editor Play Mode is not a standalone rendered-client build." : "Editor Play Mode is not standalone headless execution.",
                "No synthetic network clients or authentication bypass were used.",
            };
            if (graphical)
            {
                warnings.Add("Normal PurrNet world streaming and real actor/nameplate/VFX density were not claimed unless the selected fixture preflight and route reported them.");
                if (_observability.rendering_qualification != "verified")
                    warnings.Add("Rendering workload qualification is " + _observability.rendering_qualification + "; raw graphical metrics are preserved but excluded from qualified render-budget cohorts.");
                if (_observability.focus_context != "known_foreground")
                    warnings.Add("Application focus context is " + _observability.focus_context + "; this run is context-qualified provisionally and was not rejected automatically.");
            }
            if (status != PerformanceRunStatus.CompleteMeasured)
                warnings.Add("No successful measured result should be interpreted from this run status.");
            return "[\"" + string.Join("\",\"", warnings.ConvertAll(Escape)) + "\"]";
        }

        private static string ExtractJsonValue(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            var marker = Quote(propertyName) + ":";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (propertyIndex < 0)
                return null;
            var start = propertyIndex + marker.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= json.Length || (json[start] != '[' && json[start] != '{'))
                return null;
            var open = json[start];
            var close = open == '[' ? ']' : '}';
            var depth = 0;
            var escaped = false;
            var inString = false;
            for (var index = start; index < json.Length; index++)
            {
                var character = json[index];
                if (inString)
                {
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        inString = false;
                    continue;
                }
                if (character == '"')
                    inString = true;
                else if (character == open)
                    depth++;
                else if (character == close && --depth == 0)
                    return json.Substring(start, index - start + 1);
            }
            return null;
        }

        private static string ExtractJsonStringValue(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            var marker = Quote(propertyName) + ":";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (propertyIndex < 0)
                return null;
            var start = propertyIndex + marker.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= json.Length || json[start] != '"')
                return null;
            start++;
            var builder = new StringBuilder();
            var escaped = false;
            for (var index = start; index < json.Length; index++)
            {
                var character = json[index];
                if (escaped)
                {
                    builder.Append(character);
                    escaped = false;
                    continue;
                }
                if (character == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (character == '"')
                    return builder.ToString();
                builder.Append(character);
            }
            return null;
        }

        private static Test_ActorFixtureProfile FindActorProfile(Test_ActorFixtureCatalog catalog, string variantID)
        {
            foreach (var profile in catalog.profiles)
                if (profile.variant_id == variantID)
                    return profile;
            throw new InvalidOperationException("Actor profile is missing from the deterministic catalog: " + variantID);
        }

        private static Test_ActorScenarioProfile FindScenario(Test_ActorScenarioCatalog catalog, string variantID)
        {
            foreach (var scenario in catalog.profiles)
                if (scenario.variant_id == variantID)
                    return scenario;
            throw new InvalidOperationException("Actor scenario is missing from the deterministic catalog: " + variantID);
        }

        private static Test_WorldFixturePlan FindWorldPlan(Test_WorldFixtureManifest manifest, string variant)
        {
            if (manifest != null && manifest.Scenes != null)
            {
                foreach (var plan in manifest.Scenes)
                    if (plan.Variant == variant)
                        return plan;
            }
            return null;
        }

        private static string CreateUniqueRunID(string resultRoot)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var candidate = "run-" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                if (!Directory.Exists(Path.Combine(resultRoot, candidate)))
                    return candidate;
            }
            throw new IOException("Could not allocate a unique benchmark result directory.");
        }

        private static Test_WorldFixtureMetadata FindMetadata(Scene scene, out string missingScriptDiagnostic)
        {
            missingScriptDiagnostic = null;
            Test_WorldFixtureMetadata metadata = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                missingScriptDiagnostic = missingScriptDiagnostic ?? FindMissingScript(root, root.name);
                metadata = metadata ?? root.GetComponent<Test_WorldFixtureMetadata>();
            }
            return metadata;
        }

        private static string FindMissingScript(GameObject root, string hierarchyPath)
        {
            var components = root.GetComponents<Component>();
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] == null)
                    return $"GameObject '{hierarchyPath}' component slot {index}";
            }

            for (var index = 0; index < root.transform.childCount; index++)
            {
                var child = root.transform.GetChild(index).gameObject;
                var missingScript = FindMissingScript(child, hierarchyPath + "/" + child.name);
                if (missingScript != null)
                    return missingScript;
            }

            return null;
        }

        private static void AddMetadataMismatch<T>(ICollection<string> mismatches, string fieldName, T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                mismatches.Add($"{fieldName} expected '{expected}' but found '{actual}'");
        }

        private static string AppendDiagnostic(string existing, string addition)
        {
            if (string.IsNullOrEmpty(existing))
                return addition;
            return existing + "\n" + addition;
        }

        private static string StatusText(PerformanceRunStatus status)
        {
            switch (status)
            {
                case PerformanceRunStatus.CompleteMeasured: return "COMPLETE-MEASURED";
                case PerformanceRunStatus.BlockedPrerequisite: return "BLOCKED-PREREQUISITE";
                case PerformanceRunStatus.FailCorrectness: return "FAIL-CORRECTNESS";
                case PerformanceRunStatus.FailProvision: return "FAIL-PROVISION";
                case PerformanceRunStatus.FailResource: return "FAIL-RESOURCE";
                case PerformanceRunStatus.FailTimeout: return "FAIL-TIMEOUT";
                case PerformanceRunStatus.FailInstrumentation: return "FAIL-INSTRUMENTATION";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private static void Property(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append(Quote(name)).Append(':').Append(Quote(value));
            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        private static void RawProperty(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append(Quote(name)).Append(':').Append(value);
            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        internal static string Quote(string value)
        {
            return "\"" + Escape(value ?? string.Empty) + "\"";
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder(value.Length + 16);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(character); break;
                }
            }
            return builder.ToString();
        }

        #pragma warning disable 0649

        [Serializable]
        private sealed class Test_PerformanceTelemetrySummaryDocument
        {
            public Test_PerformanceTelemetryMetricDocument[] metrics;
        }

        [Serializable]
        private sealed class Test_PerformanceTelemetryMetricDocument
        {
            public string metric_id;
            public string observation;
            public int sample_count;
            public double p50;
        }

        #pragma warning restore 0649
    }

    /// <summary>Builds the versioned fixture and run documents without changing project assets.</summary>
    internal static class Test_PerformanceEvidence
    {
        /// <summary>Reads the newest finalized local result without touching raw sample traces.</summary>
        public static Test_PerformanceLastRunView ReadLatestFinalizedResult(string activeResultDirectory)
        {
            return Test_PerformanceResultReader.LoadLatest(activeResultDirectory);
        }

        internal static Test_PerformanceLastRunView ReadFinalizedResult(string resultDirectory)
        {
            return Test_PerformanceResultReader.ReadFinalized(resultDirectory);
        }

        private static string Quote(string value)
        {
            return Test_PerformanceRun.Quote(value);
        }

        public static string BuildFixtureJson(
            Test_PerformanceHarnessOptions options,
            Test_ActorFixtureProfile actor,
            Test_ActorScenarioProfile scenario,
            Test_WorldFixturePlan world,
            Test_PerformanceRunMetadata metadata)
        {
            var isTimedStateScale = actor.profile_id == Test_ActorFixtureLayout.TIMED_SCALE_PROFILE_ID;
            var isStage14NPCActivity = actor.profile_id == Test_ActorFixtureLayout.STAGE_14_NPC_ACTIVITY_PROFILE_ID;
            var provenanceInputPath = isTimedStateScale || isStage14NPCActivity ? actor.provenance?.input_path : Test_ActorFixtureLayout.CATALOG_PATH;
            var provenanceAssetManifestPath = isTimedStateScale ? actor.provenance?.asset_manifest_path : Test_ActorFixtureLayout.ASSET_MANIFEST_PATH;
            var document = new FixtureDocument
            {
                schema_version = "benchmark-fixture/v1",
                profile = new FixtureProfile
                {
                    id = actor.profile_id,
                    revision = actor.revision,
                    spec_revision = isTimedStateScale ? Test_ActorFixtureLayout.TIMED_SCALE_PROFILE_ID : isStage14NPCActivity ? Test_ActorFixtureLayout.STAGE_14_NPC_ACTIVITY_PROFILE_ID : Test_ActorFixtureLayout.SPEC_REVISION,
                    variant = actor.variant_id,
                    tier = actor.tier,
                },
                random = new FixtureRandom
                {
                    master_seed = options.Seed,
                    replicate_seed = actor.replicate_seed,
                    replicate_index = 0,
                    derivation = "sha256-u32le-v1",
                    streams = new[] { "world", "actors", "movement", "abilities", "lifecycle" },
                },
                provenance = new FixtureProvenance
                {
                    fixture_generator = isTimedStateScale ? actor.provenance?.fixture_generator : "Game.Tests.Performance.Harness.Test_PerformanceEvidence",
                    generator_revision = isTimedStateScale ? actor.provenance?.generator_revision : "1.0.0",
                    input_path = provenanceInputPath,
                    asset_manifest_path = provenanceAssetManifestPath,
                    source_commit = metadata.Commit,
                    source_sha256 = ResolveProvenanceHash(metadata.Repository, provenanceInputPath, isTimedStateScale ? actor.provenance?.source_sha256 : null),
                    asset_manifest_sha256 = ResolveProvenanceHash(metadata.Repository, provenanceAssetManifestPath, isTimedStateScale ? actor.provenance?.asset_manifest_sha256 : null),
                },
                required_features = actor.required_features,
                population = BuildPopulation(actor),
                space = BuildSpace(actor.space),
                environment = BuildEnvironment(actor.environment, world),
                schedules = BuildSchedules(scenario),
                network = new FixtureNetwork
                {
                    real_clients = actor.population.players.requested,
                    synthetic_clients = 0,
                    representation = actor.representation,
                    authentication = "ordinary_login_required",
                    transport = "ordinary_project_transport",
                },
                timing = new FixtureTiming
                {
                    preflight_timeout_s = isStage14NPCActivity ? 120f : Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS,
                    ramp_s = isStage14NPCActivity ? 120f : Test_PerformanceRun.RAMP_SECONDS,
                    warmup_s = isStage14NPCActivity ? 60f : Test_PerformanceRun.WARMUP_SECONDS,
                    measurement_s = isStage14NPCActivity ? 300f : Test_PerformanceRun.MEASUREMENT_SECONDS,
                    recovery_s = isStage14NPCActivity ? 60f : Test_PerformanceRun.RECOVERY_SECONDS,
                    sample_interval_ms = (int)(isStage14NPCActivity ? 1000f : Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS * 1000f),
                },
                stops = new FixtureStops
                {
                    max_duration_s = isStage14NPCActivity ? 660f : 45f,
                    max_actor_count = actor.population.npcs.requested,
                    resource_ceiling = "runner records actual resource metrics; no substitute workload is used",
                    conditions = new[] { "measurement_complete", "timeout", "resource_ceiling", "correctness_failure", "user_stop" },
                },
            };
            return JsonUtility.ToJson(document, true);
        }

        public static string BuildRunJson(Test_PerformanceRun run, string projectRoot, Test_PerformanceRunMetadata metadata)
        {
            if (run.IsGraphicalClient)
                return BuildGraphicalRunJson(run, metadata);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Property(builder, "schema_version", "benchmark-run/v1", true);
            Property(builder, "run_id", run.RunID, true);
            RawProperty(builder, "fixture", $"{{\"path\":{Test_PerformanceRun.Quote("fixture.json")},\"sha256\":{Test_PerformanceRun.Quote(run.FixtureHash)}}}", true);
            RawProperty(builder, "git", $"{{\"repository\":{Test_PerformanceRun.Quote(metadata.Repository)},\"branch\":{Test_PerformanceRun.Quote(metadata.GitBranch)},\"branch_status\":{Test_PerformanceRun.Quote(metadata.GitBranchStatus)},\"branch_diagnostic\":{Test_PerformanceRun.Quote(metadata.GitBranchDiagnostic)},\"captured_utc\":{Test_PerformanceRun.Quote(metadata.CapturedUtc)},\"commit_sha\":{Test_PerformanceRun.Quote(metadata.Commit)},\"dirty\":{metadata.Dirty.ToString().ToLowerInvariant()},\"diff_sha256\":{Test_PerformanceRun.Quote(metadata.DiffSha256)}}}", true);
            RawProperty(builder, "unity", $"{{\"version\":{Test_PerformanceRun.Quote(Application.unityVersion)},\"platform\":{Test_PerformanceRun.Quote(Application.platform.ToString())}}}", true);
            RawProperty(builder, "packages", $"{{\"manifest_sha256\":{Test_PerformanceRun.Quote(metadata.PackagesSha256)},\"lock_sha256\":{Test_PerformanceRun.Quote(metadata.PackagesLockSha256)}}}", true);
            RawProperty(builder, "purrnet", $"{{\"source\":\"Assets/ThirdParty/PurrNet\",\"sha256\":{Test_PerformanceRun.Quote(metadata.PurrNetSha256)}}}", true);
            RawProperty(builder, "build", "{\"kind\":\"editor_playmode_server\",\"target\":\"Unity Editor\",\"subtarget\":\"test_only_performance_harness\",\"development\":true,\"deep_profiling\":false,\"executable\":null,\"identity_note\":\"No standalone player was built or launched.\"}", true);
            RawProperty(builder, "render", "{\"mode\":\"server_only\",\"graphical_client\":false,\"frames_rendered\":null}", true);
            RawProperty(builder, "hardware", $"{{\"host_id\":{Test_PerformanceRun.Quote(metadata.HostID)},\"description\":{Test_PerformanceRun.Quote(metadata.Hardware)},\"system_memory_bytes\":{metadata.MemoryBytes.ToString(CultureInfo.InvariantCulture)},\"graphics_device\":{Test_PerformanceRun.Quote(SystemInfo.graphicsDeviceName)}}}", true);
            RawProperty(builder, "process", $"{{\"unity_editor_pid\":{metadata.ProcessID},\"server_processes\":1,\"client_processes\":{(run.IsStage14NPCActivity ? run.ActorProfile.population.players.requested : 0)},\"synthetic_clients\":0}}", true);
            RawProperty(builder, "clocks", "{\"utc\":true,\"monotonic_source\":\"Unity telemetry session\"}", true);
            RawProperty(builder, "application_context", "{\"run_in_background\":" + Application.runInBackground.ToString().ToLowerInvariant() + "}", true);
            RawProperty(builder, "execution", run.IsStage14NPCActivity
                ? "{\"entrypoint\":\"Tools/Performance/Test Workload Runner\",\"ordinary_runtime_path\":true,\"force_server_mode\":false,\"credentials\":\"ordinary_login_required\"}"
                : "{\"entrypoint\":\"Tools/Performance/Test Workload Runner\",\"ordinary_runtime_path\":true,\"force_server_mode\":true,\"credentials\":\"none_for_server_only_data_fixture\"}", true);
            RawProperty(builder, "runtime", $"{{\"scene_fixture\":{Test_PerformanceRun.Quote(run.Options.WorldFixture.ToString())},\"scene_path\":{Test_PerformanceRun.Quote(run.WorldPlan == null ? string.Empty : run.WorldPlan.AssetPath)},\"standalone_qualified\":false,\"network_clients_supported\":{run.IsStage14NPCActivity.ToString().ToLowerInvariant()}}}", true);
            RawProperty(builder, "workload", $"{{\"mode\":{Test_PerformanceRun.Quote(run.Options.WorkloadMode.ToString())},\"profile_id\":{Test_PerformanceRun.Quote(run.Options.EffectiveProfileID)},\"actor_variant_id\":{Test_PerformanceRun.Quote(run.Options.ActorVariantID)},\"actor_count\":{run.Options.ActorCount},\"world_fixture\":{Test_PerformanceRun.Quote(run.Options.WorldFixture.ToString())},\"density\":{Test_PerformanceRun.Quote(run.Options.DensityTier.ToString())},\"seed\":{run.Options.Seed}}}", false);
            if (run.IsStage14NPCActivity)
                RawProperty(builder, "npc_activity", BuildStage14NPCActivityEvidenceJson(run.Stage14NPCActivityEvidence), false);
            builder.AppendLine();
            builder.Append('}');
            return builder.ToString();
        }

        private static string BuildStage14NPCActivityEvidenceJson(Test_NPCActivityScenarioEvidence evidence)
        {
            if (evidence == null)
                return "null";
            var json = "{\"npc_activity_scenario\":" + Test_PerformanceRun.Quote(evidence.npc_activity_scenario)
                + ",\"requested_npc_count\":" + evidence.requested_npc_count.ToString(CultureInfo.InvariantCulture)
                + ",\"achieved_full_npc_count\":" + evidence.achieved_full_npc_count.ToString(CultureInfo.InvariantCulture)
                + ",\"required_real_player_count\":" + evidence.required_real_player_count.ToString(CultureInfo.InvariantCulture)
                + ",\"achieved_real_player_count\":" + evidence.achieved_real_player_count.ToString(CultureInfo.InvariantCulture)
                + ",\"anchor_actor_id\":" + Test_PerformanceRun.Quote(evidence.anchor_actor_id)
                + ",\"anchor_initial_context\":" + Test_PerformanceRun.Quote(evidence.anchor_initial_context)
                + ",\"anchor_initial_position\":" + BuildDoubleArrayJson(evidence.anchor_initial_position);
            if (string.Equals(evidence.npc_activity_scenario, Test_NPCActivityScenario.MassWake.ToString(), StringComparison.Ordinal))
                json += ",\"mass_wake_cluster_centre\":" + BuildDoubleArrayJson(evidence.mass_wake_cluster_centre)
                    + ",\"mass_wake_reached\":" + evidence.mass_wake_reached.ToString().ToLowerInvariant();
            if (string.Equals(evidence.npc_activity_scenario, Test_NPCActivityScenario.CrossInstance.ToString(), StringComparison.Ordinal))
                json += ",\"context_transition_source\":" + Test_PerformanceRun.Quote(evidence.context_transition_source)
                    + ",\"context_transition_destination\":" + Test_PerformanceRun.Quote(evidence.context_transition_destination);
            return json + "}";
        }

        private static string BuildDoubleArrayJson(double[] values)
        {
            if (values == null)
                return "null";
            var formatted = new string[values.Length];
            for (var index = 0; index < values.Length; index++)
                formatted[index] = values[index].ToString("R", CultureInfo.InvariantCulture);
            return "[" + string.Join(",", formatted) + "]";
        }

        public static string BuildGraphicalFixtureJson(
            Test_PerformanceHarnessOptions options,
            Test_GraphicalScenarioPlan plan,
            Test_WorldFixturePlan world,
            Test_PerformanceRunMetadata metadata)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            var sourcePath = "Assets/Tests/Performance/Fixtures/Worlds/world-fixture-manifest.json";
            var sourceHash = Test_PerformanceRunMetadata.HashFile(Path.Combine(metadata.Repository, sourcePath));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Property(builder, "schema_version", "benchmark-fixture/v1", true);
            RawProperty(builder, "profile", $"{{\"id\":{Quote(plan.ProfileID)},\"revision\":{Quote(plan.ProfileRevision)},\"spec_revision\":{Quote(Test_GraphicalFixtureLayout.SPEC_REVISION)},\"variant\":{Quote("S06-I-GEOMETRY")},\"tier\":{Quote(plan.ObjectDensityTier.Replace("_Default", string.Empty).Replace("_Minimal", string.Empty))}}}", true);
            RawProperty(builder, "random", $"{{\"master_seed\":{options.Seed},\"replicate_seed\":{options.Seed},\"replicate_index\":0,\"derivation\":\"sha256-u32le-v1\",\"streams\":[\"world\",\"actors\",\"movement\",\"abilities\",\"network\"]}}", true);
            RawProperty(builder, "provenance", $"{{\"fixture_generator\":{Quote(Test_GraphicalFixtureLayout.ROUTE_GENERATOR)},\"generator_revision\":\"1.0.0\",\"input_path\":{Quote(sourcePath)},\"asset_manifest_path\":{Quote(sourcePath)},\"source_commit\":{Quote(metadata.Commit)},\"source_sha256\":{Quote(sourceHash)},\"asset_manifest_sha256\":{Quote(sourceHash)}}}", true);
            RawProperty(builder, "required_features", BuildGraphicalRequiredFeaturesJson(plan), true);
            RawProperty(builder, "population", BuildGraphicalPopulationJson(plan), true);
            RawProperty(builder, "space", BuildGraphicalSpaceJson(plan, world), true);
            RawProperty(builder, "environment", BuildGraphicalEnvironmentJson(plan, world), true);
            RawProperty(builder, "schedules", BuildGraphicalSchedulesJson(plan), true);
            RawProperty(builder, "network", "{\"preset\":\"NET-LOCAL\",\"transport\":\"PurrNet\",\"shaper\":\"none\",\"target_rtt_ms\":null,\"target_jitter_ms\":null,\"target_loss_ratio\":null,\"connection_attempts_per_s\":0}", true);
            RawProperty(builder, "timing", $"{{\"preflight_timeout_s\":{Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS.ToString("R", CultureInfo.InvariantCulture)},\"ramp_s\":{Test_PerformanceRun.RAMP_SECONDS.ToString("R", CultureInfo.InvariantCulture)},\"warmup_s\":{options.WarmupSeconds.ToString("R", CultureInfo.InvariantCulture)},\"measurement_s\":{options.MeasurementSeconds.ToString("R", CultureInfo.InvariantCulture)},\"recovery_s\":{Test_PerformanceRun.RECOVERY_SECONDS.ToString("R", CultureInfo.InvariantCulture)},\"sample_interval_ms\":500}}", true);
            RawProperty(builder, "stops", "{\"total_timeout_s\":120,\"memory_ceiling_bytes\":4294967296,\"terminate_on_correctness_failure\":true,\"terminate_on_missing_fixture\":true,\"terminate_on_missing_required_telemetry\":true}", false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildGraphicalRunJson(Test_PerformanceRun run, Test_PerformanceRunMetadata metadata)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var qualityNames = QualitySettings.names;
            var quality = QualitySettings.GetQualityLevel() >= 0 && QualitySettings.GetQualityLevel() < qualityNames.Length
                ? qualityNames[QualitySettings.GetQualityLevel()]
                : "unknown";
            var renderer = BuildRendererIdentity(pipeline);
            var resolution = $"[{Screen.width},{Screen.height}]";
            var targetFps = Application.targetFrameRate < 0
                ? "null"
                : Application.targetFrameRate.ToString(CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Property(builder, "schema_version", "benchmark-run/v1", true);
            Property(builder, "run_id", run.RunID, true);
            RawProperty(builder, "fixture_ref", $"{{\"path\":\"fixture.json\",\"sha256\":{Quote(run.FixtureHash)}}}", true);
            Property(builder, "created_utc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), true);
            RawProperty(builder, "git", $"{{\"repository\":\"FrontierDev/ninthage\",\"branch\":{Quote(metadata.GitBranch)},\"branch_status\":{Quote(metadata.GitBranchStatus)},\"branch_diagnostic\":{Quote(metadata.GitBranchDiagnostic)},\"captured_utc\":{Quote(metadata.CapturedUtc)},\"commit_sha\":{Quote(metadata.Commit)},\"dirty\":{metadata.Dirty.ToString().ToLowerInvariant()},\"dirty_diff_sha256\":{Quote(metadata.DiffSha256)}}}", true);
            RawProperty(builder, "toolchain", $"{{\"unity\":{Quote(Application.unityVersion)},\"unity_revision\":{Quote(ReadUnityRevision(metadata.Repository))},\"packages_manifest_sha256\":{Quote(metadata.PackagesSha256)},\"packages_lock_sha256\":{Quote(metadata.PackagesLockSha256)},\"purrnet_source_id\":\"Assets/ThirdParty/PurrNet\",\"purrnet_sha256\":{Quote(metadata.PurrNetSha256)},\"fixture_generator_version\":\"1.0.0\",\"runner_version\":\"graphical-harness/v1\",\"telemetry_version\":\"benchmark-sample/v1\"}}", true);
            RawProperty(builder, "build", $"{{\"kind\":\"rendered_client\",\"target\":{Quote(Application.platform.ToString())},\"subtarget\":\"editor_playmode\",\"development\":{UnityEngine.Debug.isDebugBuild.ToString().ToLowerInvariant()},\"autoconnect_profiler\":false,\"deep_profiling\":false,\"script_debugging\":false,\"defines\":[\"UNITY_EDITOR\"],\"scripting_backend\":\"editor\",\"build_sha256\":null}}", true);
            RawProperty(builder, "render", $"{{\"mode\":\"rendered\",\"graphical_client\":true,\"graphics_api\":{Quote(SystemInfo.graphicsDeviceType.ToString())},\"urprenderer\":{Quote(renderer)},\"quality_level\":{Quote(quality)},\"resolution_px\":{resolution},\"vsync\":{QualitySettings.vSyncCount},\"target_fps\":{targetFps}}}", true);
            RawProperty(builder, "hardware", $"[{{\"host_id\":{Quote(metadata.HostID)},\"role\":\"rendered_client\",\"os\":{Quote(SystemInfo.operatingSystemFamily.ToString())},\"os_version\":{Quote(SystemInfo.operatingSystem)},\"cpu_model\":{Quote(SystemInfo.processorType)},\"physical_cores\":null,\"logical_cores\":{SystemInfo.processorCount},\"ram_bytes\":{metadata.MemoryBytes},\"gpu_model\":{Quote(SystemInfo.graphicsDeviceName)},\"vram_bytes\":{(long)SystemInfo.graphicsMemorySize * 1024L * 1024L},\"gpu_driver\":{Quote(SystemInfo.graphicsDeviceVersion)},\"storage_class\":null}}]", true);
            RawProperty(builder, "processes", $"{{\"server\":0,\"headless_network_clients\":0,\"rendered_clients\":1,\"actor_test_processes\":0,\"client_host_allocation\":[{{\"host_id\":{Quote(metadata.HostID)},\"client_processes\":1}}]}}", true);
            RawProperty(builder, "clocks", "{\"utc_clock_source\":\"host_system_clock\",\"monotonic_clock_source\":\"System.Diagnostics.Stopwatch\",\"simulation_clock_source\":\"unity_player_loop\",\"utc_clock_sync\":\"not_verified\"}", true);
            RawProperty(builder, "application_context", "{\"run_in_background\":" + Application.runInBackground.ToString().ToLowerInvariant() + ",\"focus_observation\":\"summary_observability.focus\"}", true);
            RawProperty(builder, "execution", "{\"command_template\":\"Unity Editor Play Mode via Tools/Performance/Test Workload Runner\",\"environment_id\":\"local-development\",\"test_data_scope\":\"isolated_disposable\",\"profiling_mode\":\"not_enabled\",\"network_shaper_version\":null}", true);
            RawProperty(builder, "runtime_observed", $"{{\"actual_build_sha256\":null,\"actual_content_sha256\":{Quote(run.GraphicalPlan.PlanHash)},\"actual_transport_rtt_ms\":null,\"actual_transport_loss_ratio\":null}}", false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        internal static string BuildRendererIdentity(RenderPipelineAsset pipeline)
        {
            if (pipeline == null)
                return "none";

            var renderer = pipeline.name;
            if (!string.Equals(pipeline.GetType().Name, "UniversalRenderPipelineAsset", StringComparison.Ordinal))
                return renderer + ";renderer_data=not_urp";

            var rendererAsset = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/PC_Renderer.asset");
            if (rendererAsset == null)
                return renderer + ";renderer_data=unavailable";

            var serializedRenderer = new SerializedObject(rendererAsset);
            var renderingMode = serializedRenderer.FindProperty("m_RenderingMode");
            var mode = renderingMode == null
                ? "unavailable"
                : renderingMode.intValue == 2 ? "ForwardPlus" : renderingMode.intValue.ToString(CultureInfo.InvariantCulture);
            return renderer + ";" + rendererAsset.name + "/" + mode;
        }

        private static string ReadUnityRevision(string projectRoot)
        {
            var versionPath = Path.Combine(projectRoot, "ProjectSettings", "ProjectVersion.txt");
            if (!File.Exists(versionPath))
                return "unavailable";

            foreach (var line in File.ReadLines(versionPath))
            {
                const string marker = "m_EditorVersionWithRevision:";
                if (!line.StartsWith(marker, StringComparison.Ordinal))
                    continue;

                var open = line.IndexOf('(');
                var close = line.IndexOf(')', open + 1);
                if (open >= 0 && close > open + 1)
                    return line.Substring(open + 1, close - open - 1);
            }

            return "unavailable";
        }

        private static string ResolveProvenanceHash(string projectRoot, string inputPath, string recordedHash)
        {
            if (string.IsNullOrEmpty(inputPath))
                return recordedHash ?? string.Empty;

            var absolutePath = Path.Combine(projectRoot, inputPath);
            return File.Exists(absolutePath)
                ? Test_PerformanceRunMetadata.HashFile(absolutePath)
                : recordedHash ?? string.Empty;
        }

        private static FixturePopulation BuildPopulation(Test_ActorFixtureProfile actor)
        {
            var players = new List<FixturePlayerGroup>();
            foreach (var group in actor.population.players.groups)
                players.Add(new FixturePlayerGroup { group_id = group.group_id, count = group.count, class_id = group.class_id, role = group.role, cluster_id = group.cluster_id });
            var npcs = new List<FixtureNPCGroup>();
            foreach (var group in actor.population.npcs.groups)
            {
                npcs.Add(new FixtureNPCGroup
                {
                    group_id = group.group_id,
                    definition_id = group.definition_id,
                    count = group.count,
                    class_id = group.class_id,
                    cluster_id = group.cluster_id,
                    representation = actor.representation,
                    activity = new[] { group.activity.idle, group.activity.patrolling, group.activity.engaged },
                });
            }
            return new FixturePopulation
            {
                players = new FixturePlayers { requested = actor.population.players.requested, groups = players.ToArray() },
                npcs = new FixtureNPCs { requested = actor.population.npcs.requested, groups = npcs.ToArray() },
                accounting = new FixtureAccounting
                {
                    ecs_data_only_npcs = actor.population.accounting.ecs_data_only_npcs,
                    full_gameobject_purrnet_npcs = actor.population.accounting.full_gameobject_purrnet_npcs,
                    real_player_connections = actor.population.accounting.real_player_connections,
                    synthetic_network_players = 0,
                },
            };
        }

        private static string BuildGraphicalPopulationJson(Test_GraphicalScenarioPlan plan)
        {
            var groups = plan.RequestedActorCount == 0
                ? "[]"
                : "[{\"definition_id\":\"fixture-visible-actor\",\"role\":\"visible\",\"count\":" + plan.RequestedActorCount + ",\"activity\":{\"idle\":1,\"patrolling\":0,\"engaged\":0},\"cluster_id\":\"camera-route\"}]";
            return "{\"players\":{\"requested\":0,\"connection_kind\":\"none_data_only\",\"groups\":[]},\"npcs\":{\"requested\":"
                + plan.RequestedActorCount + ",\"representation\":\"networked_gameobject_ecs\",\"groups\":" + groups
                + ",\"accounting\":{\"ecs_data_only_npcs\":0,\"full_gameobject_purrnet_npcs\":" + plan.RequestedActorCount
                + ",\"real_player_connections\":0,\"synthetic_network_players\":0}}}";
        }

        private static string BuildGraphicalRequiredFeaturesJson(Test_GraphicalScenarioPlan plan)
        {
            var features = new List<string>
            {
                "seeded_world_fixture",
                "rendered_client",
                "deterministic_camera_route",
            };
            if (plan.RequiresRealClientSession)
                features.Add("real_purrnet_client_session");
            if (plan.RequiresWorldStreaming)
                features.Add("normal_world_streaming");
            return "[\"" + string.Join("\",\"", features) + "\"]";
        }

        private static string BuildGraphicalSpaceJson(Test_GraphicalScenarioPlan plan, Test_WorldFixturePlan world)
        {
            var anchorTileX = world.WorldTileX;
            var anchorTileZ = world.WorldTileZ;
            var waypoint = plan.Waypoints[0];
            return "{\"world_id\":\"fixture-world\",\"map_id\":\"fixture-open-world\",\"instance_id\":null,\"origin_cell_size_m\":100,\"world_tile_size_m\":256,\"generated_grid\":{\"columns\":"
                + plan.FixtureVariants.Length + ",\"rows\":1,\"anchor_tile\":[" + anchorTileX + "," + anchorTileZ
                + "]},\"clusters\":[{\"id\":\"camera-route\",\"center_world_m\":["
                + waypoint.WorldX.ToString("R", CultureInfo.InvariantCulture) + ","
                + waypoint.WorldY.ToString("R", CultureInfo.InvariantCulture) + ","
                + waypoint.WorldZ.ToString("R", CultureInfo.InvariantCulture) + "],\"radius_m\":"
                + plan.RouteDistanceMeters.ToString("R", CultureInfo.InvariantCulture) + ",\"players\":0,\"npcs\":"
                + plan.RequestedActorCount + "}]}";
        }

        private static string BuildGraphicalEnvironmentJson(Test_GraphicalScenarioPlan plan, Test_WorldFixturePlan world)
        {
            if (world.Spawns == null)
                throw new InvalidOperationException("The graphical world fixture plan has no deterministic spawn list for environment evidence.");

            var terrain = CountSpawns(world, "Ground");
            var buildings = CountSpawns(world, "Building");
            var trees = CountSpawns(world, "Tree");
            var undergrowth = CountSpawns(world, "Undergrowth");
            var props = world.ObjectCount - terrain - buildings - trees - undergrowth;
            return $"{{\"tile_archetype\":{Quote(plan.ScenarioID.ToLowerInvariant())},\"terrain_per_tile\":{terrain},\"buildings_per_tile\":{buildings},\"props_per_tile\":{props},\"trees_per_tile\":{trees},\"undergrowth_per_tile\":{undergrowth},\"material_slots_per_tile\":{world.MaterialVariantCount},\"dynamic_lights_per_tile\":0,\"lod_policy_id\":\"fixture-lod-v1\",\"collision_enabled\":true}}";
        }

        private static int CountSpawns(Test_WorldFixturePlan world, string kind)
        {
            var count = 0;
            foreach (var spawn in world.Spawns)
            {
                if (string.Equals(spawn.Kind, kind, StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        private static string BuildGraphicalSchedulesJson(Test_GraphicalScenarioPlan plan)
        {
            return $"{{\"movement\":[{{\"actor_group\":\"camera\",\"route_id\":{Quote(plan.RouteID)},\"loop\":false,\"start_phase\":\"warmup\"}}],\"abilities\":[],\"events\":[]}}";
        }

        private static FixtureSpace BuildSpace(Test_ActorFixtureSpace space)
        {
            var clusters = new List<FixtureCluster>();
            foreach (var cluster in space.clusters)
                clusters.Add(new FixtureCluster { cluster_id = cluster.cluster_id, center_world_m = cluster.center_world_m, radius_m = cluster.radius_m, players = cluster.players, npcs = cluster.npcs });
            return new FixtureSpace { world_id = space.world_id, map_id = space.map_id, instance_id = space.instance_id, clusters = clusters.ToArray() };
        }

        private static FixtureEnvironment BuildEnvironment(Test_ActorFixtureEnvironment environment, Test_WorldFixturePlan world)
        {
            return new FixtureEnvironment
            {
                world_id = environment.world_id,
                map_id = environment.map_id,
                instance_id = environment.instance_id,
                origin_cell_size_m = environment.origin_cell_size_m,
                world_tile_size_m = environment.world_tile_size_m,
                fixture_scene = world == null ? string.Empty : world.AssetPath,
                fixture_scene_hash = world == null ? string.Empty : world.SceneHash,
                object_count = world == null ? 0 : world.ObjectCount,
                navigable = world != null && world.IsNavigable,
            };
        }

        private static FixtureSchedules BuildSchedules(Test_ActorScenarioProfile scenario)
        {
            return new FixtureSchedules
            {
                movement_routes = scenario.movement_routes,
                ability_schedule = scenario.ability_schedule,
                events = scenario.events,
            };
        }

        private static void Property(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append(Test_PerformanceRun.Quote(name)).Append(':').Append(Test_PerformanceRun.Quote(value));
            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        private static void RawProperty(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append(Test_PerformanceRun.Quote(name)).Append(':').Append(value);
            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        [Serializable] private sealed class FixtureDocument { public string schema_version; public FixtureProfile profile; public FixtureRandom random; public FixtureProvenance provenance; public string[] required_features; public FixturePopulation population; public FixtureSpace space; public FixtureEnvironment environment; public FixtureSchedules schedules; public FixtureNetwork network; public FixtureTiming timing; public FixtureStops stops; }
        [Serializable] private sealed class FixtureProfile { public string id; public string revision; public string spec_revision; public string variant; public string tier; }
        [Serializable] private sealed class FixtureRandom { public int master_seed; public int replicate_seed; public int replicate_index; public string derivation; public string[] streams; }
        [Serializable] private sealed class FixtureProvenance { public string fixture_generator; public string generator_revision; public string input_path; public string asset_manifest_path; public string source_commit; public string source_sha256; public string asset_manifest_sha256; }
        [Serializable] private sealed class FixturePopulation { public FixturePlayers players; public FixtureNPCs npcs; public FixtureAccounting accounting; }
        [Serializable] private sealed class FixturePlayers { public int requested; public FixturePlayerGroup[] groups; }
        [Serializable] private sealed class FixturePlayerGroup { public string group_id; public int count; public string class_id; public string role; public string cluster_id; }
        [Serializable] private sealed class FixtureNPCs { public int requested; public FixtureNPCGroup[] groups; }
        [Serializable] private sealed class FixtureNPCGroup { public string group_id; public string definition_id; public int count; public string class_id; public string cluster_id; public string representation; public double[] activity; }
        [Serializable] private sealed class FixtureAccounting { public int ecs_data_only_npcs; public int full_gameobject_purrnet_npcs; public int real_player_connections; public int synthetic_network_players; }
        [Serializable] private sealed class FixtureSpace { public string world_id; public string map_id; public string instance_id; public FixtureCluster[] clusters; }
        [Serializable] private sealed class FixtureCluster { public string cluster_id; public double[] center_world_m; public double radius_m; public int players; public int npcs; }
        [Serializable] private sealed class FixtureEnvironment { public string world_id; public string map_id; public string instance_id; public int origin_cell_size_m; public int world_tile_size_m; public string fixture_scene; public string fixture_scene_hash; public int object_count; public bool navigable; }
        [Serializable] private sealed class FixtureSchedules { public Test_ActorMovementRoute[] movement_routes; public Test_ActorAbilitySchedule[] ability_schedule; public Test_ActorScenarioEvent[] events; }
        [Serializable] private sealed class FixtureNetwork { public int real_clients; public int synthetic_clients; public string representation; public string authentication; public string transport; }
        [Serializable] private sealed class FixtureTiming { public float preflight_timeout_s; public float ramp_s; public float warmup_s; public float measurement_s; public float recovery_s; public int sample_interval_ms; }
        [Serializable] private sealed class FixtureStops { public float max_duration_s; public int max_actor_count; public string resource_ceiling; public string[] conditions; }
    }
}
