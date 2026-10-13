using Game.Server.Core;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using Game.Tests.Performance.Fixtures.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Captures source and machine identity required by benchmark evidence.</summary>
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

        private Test_PerformanceRun(
            Test_PerformanceHarnessOptions options,
            Test_ActorFixtureProfile actorProfile,
            Test_ActorScenarioProfile scenario,
            Test_WorldFixturePlan worldPlan,
            Test_PerformanceRunMetadata metadata,
            string runID,
            string resultsDirectory)
        {
            Options = options;
            ActorProfile = actorProfile;
            Scenario = scenario;
            WorldPlan = worldPlan;
            _metadata = metadata;
            RunID = runID;
            ResultsDirectory = resultsDirectory;
            FixturePath = Path.Combine(resultsDirectory, "fixture.json");
            RunPath = Path.Combine(resultsDirectory, "run.json");
            SummaryPath = Path.Combine(resultsDirectory, "summary.json");
            ReportPath = Path.Combine(resultsDirectory, "report.md");
            _startedUtc = DateTime.UtcNow;
        }

        public Test_PerformanceHarnessOptions Options { get; }

        public Test_ActorFixtureProfile ActorProfile { get; }

        public Test_ActorScenarioProfile Scenario { get; }

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

        public bool CleanupComplete { get; private set; }

        public string RuntimeDiagnostic { get; private set; }

        public Scene LoadedWorldScene { get; private set; }

        public Test_ActorFixtureRuntime FixtureRuntime { get; private set; }

        public Test_PerformanceActivityController ActivityController { get; private set; }

        public int RequestedActorCount => Options.ActorCount;

        public string FixtureHash => Test_PerformanceRunMetadata.HashFile(FixturePath);

        public static Test_PerformanceRun Create(Test_PerformanceHarnessOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var metadata = Test_PerformanceRunMetadata.Capture(projectRoot);
            var catalog = Test_ActorFixtureLayout.BuildCatalog(options.Seed);
            var scenarioCatalog = Test_ActorFixtureLayout.BuildScenarioCatalog(options.Seed);
            var actorProfile = FindActorProfile(catalog, options.ActorVariantID);
            var scenario = FindScenario(scenarioCatalog, options.ActorVariantID);
            var worldManifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = options.Seed,
                DensityTier = options.DensityTier,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            var worldPlan = FindWorldPlan(worldManifest, options.WorldFixture.ToString());
            var resultRoot = Path.Combine(projectRoot, RESULT_ROOT);
            Directory.CreateDirectory(resultRoot);
            var runID = CreateUniqueRunID(resultRoot);
            var resultsDirectory = Path.Combine(resultRoot, runID);
            Directory.CreateDirectory(resultsDirectory);
            var run = new Test_PerformanceRun(options, actorProfile, scenario, worldPlan, metadata, runID, resultsDirectory);
            run.WriteFixtureAndRun(projectRoot);
            return run;
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
                BuildKind = "editor_playmode_server",
                BuildFlags = "test_only;ordinary_server_initialization;no_deep_profiling",
                ProfileRevision = ActorProfile.revision,
                Seed = Options.Seed.ToString(CultureInfo.InvariantCulture),
                Platform = Application.platform.ToString(),
                GraphicsOptions = SystemInfo.graphicsDeviceName,
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

        public void SetLoadedWorld(Scene scene)
        {
            LoadedWorldScene = scene;
            WorldFixtureLoaded = scene.IsValid() && scene.isLoaded && string.Equals(scene.path, WorldPlan.AssetPath, StringComparison.Ordinal);
            if (!WorldFixtureLoaded)
                throw new InvalidOperationException($"Loaded scene identity did not match the selected fixture: expected {WorldPlan.AssetPath}, found {scene.path}.");
            var metadata = FindMetadata(scene);
            if (metadata == null)
                throw new InvalidOperationException($"Selected fixture scene '{scene.path}' has no Test_WorldFixtureMetadata component.");
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

        public void MarkWorkloadApplied()
        {
            if (!WorldFixtureLoaded || FixtureRuntime == null || ActivityController == null)
                throw new InvalidOperationException("The selected world and workload must be applied before marking the workload active.");
            if (FixtureRuntime.DataOnlyEntityCount != RequestedActorCount)
                throw new InvalidOperationException($"Workload applied with {FixtureRuntime.DataOnlyEntityCount} ECS entities; expected {RequestedActorCount}.");
            PerformanceTelemetry.MarkWorkloadApplied();
            WorkloadApplied = true;
            AppendLog("Requested data-only population applied: " + FixtureRuntime.DataOnlyEntityCount.ToString(CultureInfo.InvariantCulture));
        }

        public void BeginPhase(PerformanceMeasurementPhase phase)
        {
            if (!PerformanceTelemetry.BeginPhase(phase))
                throw new InvalidOperationException("Telemetry rejected phase transition to " + phase + ".");
            if (phase == PerformanceMeasurementPhase.Measurement)
                MeasurementStarted = true;
        }

        public void MarkCleanupComplete()
        {
            if (FixtureRuntime == null)
                throw new InvalidOperationException("No actor fixture exists to clean up.");
            FixtureRuntime.AssertCleanupComplete();
            CleanupComplete = true;
            AppendLog("Fixture cleanup returned to its captured baseline.");
        }

        public void Complete(PerformanceRunStatus requestedStatus, string reasonCode, string message)
        {
            if (Finalized)
                return;

            var status = requestedStatus;
            var finalReason = reasonCode;
            var finalMessage = message;
            try
            {
                if (_telemetryStarted && PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(status, reasonCode, message);
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

            if (status == PerformanceRunStatus.CompleteMeasured && (!WorkloadApplied || !MeasurementStarted || !CleanupComplete))
            {
                status = PerformanceRunStatus.FailCorrectness;
                finalReason = "INCOMPLETE_LIFECYCLE";
                finalMessage = "The run did not complete workload, measurement and cleanup invariants.";
            }
            if (status == PerformanceRunStatus.CompleteMeasured && !File.Exists(Path.Combine(ResultsDirectory, "samples.ndjson")))
            {
                status = PerformanceRunStatus.FailInstrumentation;
                finalReason = "NO_MEASUREMENT_SAMPLES";
                finalMessage = "Measurement entered but no bounded telemetry samples were written.";
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
            var fixtureJson = Test_PerformanceEvidence.BuildFixtureJson(Options, ActorProfile, Scenario, WorldPlan, _metadata);
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
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Property(builder, "schema_version", "benchmark-summary/v1", true);
            Property(builder, "run_id", RunID, true);
            Property(builder, "fixture_sha256", FixtureHash, true);
            Property(builder, "status", StatusText(status), true);
            RawProperty(builder, "started_utc", blockedPreflight ? "null" : Quote(_startedUtc.ToString("o", CultureInfo.InvariantCulture)), true);
            RawProperty(builder, "finished_utc", blockedPreflight ? "null" : Quote(finishedUtc.ToString("o", CultureInfo.InvariantCulture)), true);
            RawProperty(builder, "phase_completion", $"{{\"preflight\":{(!blockedPreflight && PreflightCompleted).ToString().ToLowerInvariant()},\"ramp\":{WorkloadApplied.ToString().ToLowerInvariant()},\"warmup\":{WorkloadApplied.ToString().ToLowerInvariant()},\"measurement\":{MeasurementStarted.ToString().ToLowerInvariant()},\"recovery\":{CleanupComplete.ToString().ToLowerInvariant()}}}", true);
            RawProperty(builder, "achieved", $"{{\"connected_players\":{GetRealPlayerCount()},\"authenticated_players\":0,\"in_world_players\":0,\"npc_actors\":0,\"ecs_entities\":{GetECSCount()},\"network_identities\":0,\"gameobjects\":0,\"scene_tiles_loaded\":{(WorldFixtureLoaded ? 1 : 0)},\"renders_visible\":null,\"accepted_abilities\":0,\"activity\":{{\"idle\":{GetIdleCount()},\"patrolling\":{GetPatrollingCount()},\"combat\":{GetCombatCount()},\"wake_surge\":{GetWakeSurgeCount()},\"spawn_events\":{GetSpawnEventCount()},\"despawn_events\":{GetDespawnEventCount()}}}}}", true);
            RawProperty(builder, "resources", $"{{\"total_allocated_bytes\":{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()},\"ceiling_bytes\":{MAX_TOTAL_ALLOCATED_BYTES},\"ceiling_status\":{Quote(status == PerformanceRunStatus.FailResource ? "exceeded" : "not_exceeded")}}}", true);
            RawProperty(builder, "stop", $"{{\"phase\":{Quote(CurrentPhaseText())},\"reason_code\":{Quote(reasonCode)},\"message\":{Quote(message)}}}", true);
            RawProperty(builder, "correctness", $"{{\"status\":{Quote(correctnessStatus)},\"checked\":{(WorkloadApplied ? 4 : 0)},\"failed\":{failedAssertions},\"unverified\":[\"standalone_headless\",\"networked_clients\"]}}", true);
            RawProperty(builder, "metrics", metrics, true);
            RawProperty(builder, "raw_files", rawFiles, true);
            RawProperty(builder, "warnings", BuildWarnings(status), false);
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

        private static string BuildRawFileEntry(string relativePath, string path)
        {
            return $"{{\"path\":{Quote(relativePath)},\"bytes\":{new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture)},\"sha256\":{Quote(Test_PerformanceRunMetadata.HashFile(path))}}}";
        }

        private string CurrentPhaseText()
        {
            if (CleanupComplete)
                return "recovery";
            if (MeasurementStarted)
                return "measurement";
            if (WorkloadApplied)
                return "warmup";
            if (PreflightCompleted)
                return "ramp";
            return "preflight";
        }

        private void WriteReport(PerformanceRunStatus status, string reasonCode, string message)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Performance workload run");
            builder.AppendLine();
            builder.AppendLine("- Status: `" + StatusText(status) + "`");
            builder.AppendLine("- Run ID: `" + RunID + "`");
            builder.AppendLine("- Profile: `" + Options.ProfileID + "`");
            builder.AppendLine("- World fixture: `" + Options.WorldFixture + "` (`" + (WorldPlan == null ? "unavailable" : WorldPlan.AssetPath) + "`)");
            builder.AppendLine("- Workload: `" + Options.WorkloadMode + "`");
            builder.AppendLine("- Seed: `" + Options.Seed.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine("- Git commit: `" + _metadata.Commit + "` (dirty: `" + _metadata.Dirty.ToString().ToLowerInvariant() + "`)");
            builder.AppendLine("- Requested ECS entities: `" + RequestedActorCount + "`");
            builder.AppendLine("- Achieved ECS entities at report time: `" + GetECSCount() + "`");
            builder.AppendLine("- Report directory: `" + ResultsDirectory + "`");
            builder.AppendLine();
            builder.AppendLine("## Result");
            builder.AppendLine();
            builder.AppendLine(message);
            builder.AppendLine();
            builder.AppendLine("Stop reason: `" + reasonCode + "`");
            builder.AppendLine();
            builder.AppendLine("This is an Editor Play Mode server measurement. It does not qualify standalone headless capacity, graphical client rendering, authentication throughput, or multi-client network behavior.");
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

        private int GetECSCount()
        {
            return FixtureRuntime == null ? 0 : FixtureRuntime.DataOnlyEntityCount;
        }

        private int GetCombatCount()
        {
            return ActivityController == null ? 0 : ActivityController.CombatCount;
        }

        private int GetIdleCount()
        {
            return ActivityController == null ? 0 : ActivityController.IdleCount;
        }

        private int GetPatrollingCount()
        {
            return ActivityController == null ? 0 : ActivityController.PatrollingCount;
        }

        private int GetWakeSurgeCount()
        {
            return ActivityController == null ? 0 : ActivityController.WakeSurgeCount;
        }

        private int GetSpawnEventCount()
        {
            return ActivityController == null ? 0 : ActivityController.SpawnEventCount;
        }

        private int GetDespawnEventCount()
        {
            return ActivityController == null ? 0 : ActivityController.DespawnEventCount;
        }

        private int GetRealPlayerCount()
        {
            return PurrNet.NetworkManager.main == null ? 0 : PurrNet.NetworkManager.main.playerCount;
        }

        private static string BuildWarnings(PerformanceRunStatus status)
        {
            var warnings = new List<string>
            {
                "Editor Play Mode is not standalone headless execution.",
                "No synthetic network clients or authentication bypass were used.",
            };
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

        private static Test_WorldFixtureMetadata FindMetadata(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var metadata = root.GetComponent<Test_WorldFixtureMetadata>();
                if (metadata != null)
                    return metadata;
            }
            return null;
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
