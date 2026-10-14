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
            var machineIdentity = string.Join("|", SystemInfo.operatingSystem, SystemInfo.processorType, SystemInfo.processorCount.ToString(CultureInfo.InvariantCulture));
            return new Test_PerformanceRunMetadata
            {
                Repository = projectRoot,
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
        public const long MAX_TOTAL_ALLOCATED_BYTES = 4L * 1024L * 1024L * 1024L;

        private readonly StringBuilder _log = new();
        private readonly Test_PerformanceRunMetadata _metadata;
        private readonly DateTime _startedUtc;
        private string _telemetrySummary;
        private bool _telemetryStarted;
        private bool _measurementSamplesAvailable;
        private PerformanceMeasurementPhase _currentPhase = PerformanceMeasurementPhase.Preflight;
        private Test_PerformanceActivitySnapshot _activitySnapshot;
        private string _activitySnapshotPhase;

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

        public Test_GraphicalFixtureRuntime GraphicalRuntime { get; private set; }

        public bool IsGraphicalClient => Options.IsGraphicalClient;

        public int RequestedActorCount => Options.ActorCount;

        public string FixtureHash => Test_PerformanceRunMetadata.HashFile(FixturePath);

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

            var catalog = Test_ActorFixtureLayout.BuildCatalog(options.Seed);
            var scenarioCatalog = Test_ActorFixtureLayout.BuildScenarioCatalog(options.Seed);
            actorProfile = FindActorProfile(catalog, options.ActorVariantID);
            scenario = FindScenario(scenarioCatalog, options.ActorVariantID);
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
                GitCommitSha = _metadata.Commit,
                GitDirty = _metadata.Dirty,
                BuildKind = IsGraphicalClient ? "editor_playmode_rendered_client" : "editor_playmode_server",
                BuildFlags = IsGraphicalClient
                    ? "test_only;ordinary_client_initialization;no_deep_profiling"
                    : "test_only;ordinary_server_initialization;no_deep_profiling",
                ProfileRevision = IsGraphicalClient ? GraphicalPlan.ProfileRevision : ActorProfile.revision,
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
            var metadata = FindMetadata(scene, out var hasMissingScript);
            if (hasMissingScript)
            {
                throw new InvalidOperationException(
                    $"Selected fixture scene '{scene.path}' contains an unresolved script reference, so its fixture metadata cannot be trusted. Check the test runtime assembly and imported script GUIDs.");
            }

            if (metadata == null)
            {
                throw new InvalidOperationException(
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

        public void CreateDataOnlyRuntime(bool activityEnabled)
        {
            FixtureRuntime = new Test_ActorFixtureRuntime(ActorProfile);
            FixtureRuntime.StartDataOnly(0);
            ActivityController = new Test_PerformanceActivityController(FixtureRuntime, activityEnabled);
            PreflightCompleted = true;
            AppendLog("Data-only ECS fixture prepared with zero entities for incremental ramp.");
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
            };
        }

        private void RestoreLifecycle(Test_PerformancePendingRunState state)
        {
            ValidationDiagnostic = null;
            StopRequested = state.stopRequested;
            var telemetrySessionWasActive = state.telemetryStarted;
            _telemetryStarted = false;
            Finalized = state.finalized;
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
                if (!IsGraphicalClient && (FixtureRuntime == null || ActivityController == null))
                    throw new InvalidOperationException("The data-only fixture must exist before measurement begins.");
                if (!IsGraphicalClient && FixtureRuntime.DataOnlyEntityCount != RequestedActorCount)
                {
                    throw new InvalidOperationException(
                        $"Measurement cannot begin with {FixtureRuntime.DataOnlyEntityCount} ECS entities; expected {RequestedActorCount}.");
                }
            }

            if (!PerformanceTelemetry.BeginPhase(phase))
                throw new InvalidOperationException("Telemetry rejected phase transition to " + phase + ".");
            _currentPhase = phase;
            if (phase == PerformanceMeasurementPhase.Measurement)
            {
                if (!IsGraphicalClient)
                    ActivityController.BeginMeasurement();
                MeasurementStarted = true;
                MeasurementStartEntityCount = IsGraphicalClient ? GraphicalRuntime.ActualObjectCount : FixtureRuntime.DataOnlyEntityCount;
                ObserveEntityCount(MeasurementStartEntityCount);
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
                    || !ActivitySnapshotCaptured
                    || !CleanupComplete);
            if (status == PerformanceRunStatus.CompleteMeasured && (graphicalLifecycleIncomplete || serverLifecycleIncomplete))
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

            WriteSummary(status, finalReason, finalMessage);
            var logDirectory = Path.Combine(ResultsDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            File.WriteAllText(Path.Combine(logDirectory, "runner.log"), _log.ToString(), new UTF8Encoding(false));
            WriteReport(status, finalReason, finalMessage);
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
            RawProperty(builder, "achieved", BuildAchievedJson(), true);
            RawProperty(builder, "resources", $"{{\"total_allocated_bytes\":{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()},\"ceiling_bytes\":{MAX_TOTAL_ALLOCATED_BYTES},\"ceiling_status\":{Quote(status == PerformanceRunStatus.FailResource ? "exceeded" : "not_exceeded")}}}", true);
            RawProperty(builder, "stop", $"{{\"phase\":{Quote(CurrentPhaseText())},\"reason_code\":{Quote(reasonCode)},\"message\":{Quote(message)}}}", true);
            RawProperty(builder, "correctness", $"{{\"status\":{Quote(correctnessStatus)},\"checked\":{checkedAssertions},\"failed\":{failedAssertions},\"unverified\":{unverified}}}", true);
            RawProperty(builder, "metrics", metrics, true);
            RawProperty(builder, "raw_files", rawFiles, true);
            RawProperty(builder, "warnings", BuildWarnings(status, IsGraphicalClient), false);
            builder.AppendLine();
            builder.Append('}');
            File.WriteAllText(SummaryPath, builder.ToString(), new UTF8Encoding(false));
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
                builder.AppendLine("- Requested ECS entities: `" + RequestedActorCount + "`");
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
            builder.AppendLine();
            builder.AppendLine("## Result");
            builder.AppendLine();
            builder.AppendLine(message);
            builder.AppendLine();
            builder.AppendLine("Stop reason: `" + reasonCode + "`");
            builder.AppendLine();
            builder.AppendLine(IsGraphicalClient
                ? "This is an Editor Play Mode rendered-client measurement. It does not qualify authenticated player density, normal PurrNet world streaming, or multi-client network behavior."
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
            foreach (var path in Directory.GetFiles(ResultsDirectory, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(path), "artifacts.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                var relative = path.Substring(ResultsDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                entries.Add($"{{\"path\":{Quote(relative)},\"sha256\":{Quote(Test_PerformanceRunMetadata.HashFile(path))},\"bytes\":{new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture)},\"media_type\":{Quote(GuessMediaType(path))},\"collector\":\"test_performance_harness\"}}");
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

        private static string BuildWarnings(PerformanceRunStatus status, bool graphical)
        {
            var warnings = new List<string>
            {
                graphical ? "Editor Play Mode is not a standalone rendered-client build." : "Editor Play Mode is not standalone headless execution.",
                "No synthetic network clients or authentication bypass were used.",
            };
            if (graphical)
                warnings.Add("Normal PurrNet world streaming and real actor/nameplate/VFX density were not claimed unless the selected fixture preflight and route reported them.");
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

        private static Test_WorldFixtureMetadata FindMetadata(Scene scene, out bool hasMissingScript)
        {
            hasMissingScript = false;
            Test_WorldFixtureMetadata metadata = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (ContainsMissingScript(root))
                    hasMissingScript = true;
                metadata = metadata ?? root.GetComponent<Test_WorldFixtureMetadata>();
            }
            return metadata;
        }

        private static bool ContainsMissingScript(GameObject root)
        {
            foreach (var component in root.GetComponents<Component>())
            {
                if (component == null)
                    return true;
            }

            for (var index = 0; index < root.transform.childCount; index++)
            {
                if (ContainsMissingScript(root.transform.GetChild(index).gameObject))
                    return true;
            }

            return false;
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
    }

    /// <summary>Builds the versioned fixture and run documents without changing project assets.</summary>
    internal static class Test_PerformanceEvidence
    {
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
            var document = new FixtureDocument
            {
                schema_version = "benchmark-fixture/v1",
                profile = new FixtureProfile
                {
                    id = actor.profile_id,
                    revision = actor.revision,
                    spec_revision = Test_ActorFixtureLayout.SPEC_REVISION,
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
                    fixture_generator = "Game.Tests.Performance.Harness.Test_PerformanceEvidence",
                    generator_revision = "1.0.0",
                    input_path = Test_ActorFixtureLayout.CATALOG_PATH,
                    asset_manifest_path = Test_ActorFixtureLayout.ASSET_MANIFEST_PATH,
                    source_commit = metadata.Commit,
                    source_sha256 = Test_PerformanceRunMetadata.HashFile(Path.Combine(metadata.Repository, Test_ActorFixtureLayout.CATALOG_PATH)),
                    asset_manifest_sha256 = Test_PerformanceRunMetadata.HashFile(Path.Combine(metadata.Repository, Test_ActorFixtureLayout.ASSET_MANIFEST_PATH)),
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
                    preflight_timeout_s = Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS,
                    ramp_s = Test_PerformanceRun.RAMP_SECONDS,
                    warmup_s = Test_PerformanceRun.WARMUP_SECONDS,
                    measurement_s = Test_PerformanceRun.MEASUREMENT_SECONDS,
                    recovery_s = Test_PerformanceRun.RECOVERY_SECONDS,
                    sample_interval_ms = (int)(Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS * 1000f),
                },
                stops = new FixtureStops
                {
                    max_duration_s = 45f,
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
            RawProperty(builder, "git", $"{{\"repository\":{Test_PerformanceRun.Quote(metadata.Repository)},\"commit_sha\":{Test_PerformanceRun.Quote(metadata.Commit)},\"dirty\":{metadata.Dirty.ToString().ToLowerInvariant()},\"diff_sha256\":{Test_PerformanceRun.Quote(metadata.DiffSha256)}}}", true);
            RawProperty(builder, "unity", $"{{\"version\":{Test_PerformanceRun.Quote(Application.unityVersion)},\"platform\":{Test_PerformanceRun.Quote(Application.platform.ToString())}}}", true);
            RawProperty(builder, "packages", $"{{\"manifest_sha256\":{Test_PerformanceRun.Quote(metadata.PackagesSha256)},\"lock_sha256\":{Test_PerformanceRun.Quote(metadata.PackagesLockSha256)}}}", true);
            RawProperty(builder, "purrnet", $"{{\"source\":\"Assets/ThirdParty/PurrNet\",\"sha256\":{Test_PerformanceRun.Quote(metadata.PurrNetSha256)}}}", true);
            RawProperty(builder, "build", "{\"kind\":\"editor_playmode_server\",\"target\":\"Unity Editor\",\"subtarget\":\"test_only_performance_harness\",\"development\":true,\"deep_profiling\":false,\"executable\":null,\"identity_note\":\"No standalone player was built or launched.\"}", true);
            RawProperty(builder, "render", "{\"mode\":\"server_only\",\"graphical_client\":false,\"frames_rendered\":null}", true);
            RawProperty(builder, "hardware", $"{{\"host_id\":{Test_PerformanceRun.Quote(metadata.HostID)},\"description\":{Test_PerformanceRun.Quote(metadata.Hardware)},\"system_memory_bytes\":{metadata.MemoryBytes.ToString(CultureInfo.InvariantCulture)},\"graphics_device\":{Test_PerformanceRun.Quote(SystemInfo.graphicsDeviceName)}}}", true);
            RawProperty(builder, "process", $"{{\"unity_editor_pid\":{metadata.ProcessID},\"server_processes\":1,\"client_processes\":0,\"synthetic_clients\":0}}", true);
            RawProperty(builder, "clocks", "{\"utc\":true,\"monotonic_source\":\"Unity telemetry session\"}", true);
            RawProperty(builder, "execution", "{\"entrypoint\":\"Tools/Performance/Test Workload Runner\",\"ordinary_runtime_path\":true,\"force_server_mode\":true,\"credentials\":\"none_for_server_only_data_fixture\"}", true);
            RawProperty(builder, "runtime", $"{{\"scene_fixture\":{Test_PerformanceRun.Quote(run.Options.WorldFixture.ToString())},\"scene_path\":{Test_PerformanceRun.Quote(run.WorldPlan == null ? string.Empty : run.WorldPlan.AssetPath)},\"standalone_qualified\":false,\"network_clients_supported\":false}}", false);
            builder.AppendLine();
            builder.Append('}');
            return builder.ToString();
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
            RawProperty(builder, "git", $"{{\"repository\":\"FrontierDev/ninthage\",\"commit_sha\":{Quote(metadata.Commit)},\"dirty\":{metadata.Dirty.ToString().ToLowerInvariant()},\"dirty_diff_sha256\":{Quote(metadata.DiffSha256)}}}", true);
            RawProperty(builder, "toolchain", $"{{\"unity\":{Quote(Application.unityVersion)},\"unity_revision\":{Quote(ReadUnityRevision(metadata.Repository))},\"packages_manifest_sha256\":{Quote(metadata.PackagesSha256)},\"packages_lock_sha256\":{Quote(metadata.PackagesLockSha256)},\"purrnet_source_id\":\"Assets/ThirdParty/PurrNet\",\"purrnet_sha256\":{Quote(metadata.PurrNetSha256)},\"fixture_generator_version\":\"1.0.0\",\"runner_version\":\"graphical-harness/v1\",\"telemetry_version\":\"benchmark-sample/v1\"}}", true);
            RawProperty(builder, "build", $"{{\"kind\":\"rendered_client\",\"target\":{Quote(Application.platform.ToString())},\"subtarget\":\"editor_playmode\",\"development\":{UnityEngine.Debug.isDebugBuild.ToString().ToLowerInvariant()},\"autoconnect_profiler\":false,\"deep_profiling\":false,\"script_debugging\":false,\"defines\":[\"UNITY_EDITOR\"],\"scripting_backend\":\"editor\",\"build_sha256\":null}}", true);
            RawProperty(builder, "render", $"{{\"mode\":\"rendered\",\"graphics_api\":{Quote(SystemInfo.graphicsDeviceType.ToString())},\"urprenderer\":{Quote(renderer)},\"quality_level\":{Quote(quality)},\"resolution_px\":{resolution},\"vsync\":{QualitySettings.vSyncCount},\"target_fps\":{targetFps}}}", true);
            RawProperty(builder, "hardware", $"[{{\"host_id\":{Quote(metadata.HostID)},\"role\":\"rendered_client\",\"os\":{Quote(SystemInfo.operatingSystemFamily.ToString())},\"os_version\":{Quote(SystemInfo.operatingSystem)},\"cpu_model\":{Quote(SystemInfo.processorType)},\"physical_cores\":null,\"logical_cores\":{SystemInfo.processorCount},\"ram_bytes\":{metadata.MemoryBytes},\"gpu_model\":{Quote(SystemInfo.graphicsDeviceName)},\"vram_bytes\":{(long)SystemInfo.graphicsMemorySize * 1024L * 1024L},\"gpu_driver\":{Quote(SystemInfo.graphicsDeviceVersion)},\"storage_class\":null}}]", true);
            RawProperty(builder, "processes", $"{{\"server\":0,\"headless_network_clients\":0,\"rendered_clients\":1,\"actor_test_processes\":0,\"client_host_allocation\":[{{\"host_id\":{Quote(metadata.HostID)},\"client_processes\":1}}]}}", true);
            RawProperty(builder, "clocks", "{\"utc_clock_source\":\"host_system_clock\",\"monotonic_clock_source\":\"System.Diagnostics.Stopwatch\",\"simulation_clock_source\":\"unity_player_loop\",\"utc_clock_sync\":\"not_verified\"}", true);
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
