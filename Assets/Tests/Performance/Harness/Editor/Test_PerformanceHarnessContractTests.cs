using NUnit.Framework;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Regression coverage for deterministic runner selection and explicit capability boundaries.</summary>
    public sealed class Test_PerformanceHarnessContractTests
    {
        [Test]
        public void DataOnlyModesUseTheEcsFixtureWithoutRealPlayers()
        {
            var options = new Test_PerformanceHarnessOptions();

            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(options.IsServerOnlyDataMode, Is.True);
            Assert.That(options.ActorCount, Is.EqualTo(100));
            Assert.That(options.Seed, Is.EqualTo(424242));
        }

        [Test]
        public void ActivityModeKeepsTheSameDeterministicActorVariant()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
            };

            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(options.IsServerOnlyDataMode, Is.True);
        }

        [Test]
        public void ActivityChurnIsMeasurementRelativeAndDeterministic()
        {
            var duration = Test_PerformanceRun.MEASUREMENT_SECONDS;
            var respawnThreshold = duration * 0.4f;

            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Warmup,
                    duration * 0.25f,
                    duration,
                    false,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    duration * 0.25f,
                    duration,
                    false,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.Despawn));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    respawnThreshold - 0.001f,
                    duration,
                    true,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    respawnThreshold,
                    duration,
                    true,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.Respawn));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    duration,
                    duration,
                    true,
                    true),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
        }

        [Test]
        public void ActivityEvidenceKeepsMeasuredSnapshotSeparateFromCumulativeEvents()
        {
            var snapshot = new Test_PerformanceActivitySnapshot(85, 4, 11, 10, 5, 5, 105, 5);
            var json = Test_PerformanceRun.BuildActivityJson(snapshot, "measurement_end_pre_cleanup");

            Assert.That(json, Does.Contain("\"snapshot_captured\":true"));
            Assert.That(json, Does.Contain("\"observation\":\"measurement_end_pre_cleanup\""));
            Assert.That(json, Does.Contain("\"idle\":85"));
            Assert.That(json, Does.Contain("\"combat\":11"));
            Assert.That(json, Does.Contain("\"wake_surge\":10"));
            Assert.That(json, Does.Contain("\"spawn_events\":5"));
            Assert.That(json, Does.Contain("\"spawn_events_total\":105"));
            Assert.That(json, Does.Contain("\"wake_surge_is_subset_of_combat\":true"));
        }

        [Test]
        public void InterruptedActivityEvidenceRemainsUnverified()
        {
            var json = Test_PerformanceRun.BuildActivityJson(null, null);

            Assert.That(json, Does.Contain("\"snapshot_captured\":false"));
            Assert.That(json, Does.Contain("\"observation\":\"unverified\""));
        }

        [Test]
        public void PerformanceSummaryUsesMeasurementSamplesOnly()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("phase-filter");
            var summary = CaptureTelemetrySummary(
                "performance-phase-filter",
                resultsDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Ramp);
                    PerformanceTelemetry.TryRecord(metric, 1000);
                    PerformanceTelemetry.TryRecord(metric, 2000);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 100; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Recovery);
                    PerformanceTelemetry.TryRecord(metric, 5000);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.phase-filter");

            Assert.That(metricJson, Does.Contain("\"phase\":\"measurement\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":100"));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":100"));
            Assert.That(metricJson, Does.Contain("\"min\":1"));
            Assert.That(metricJson, Does.Contain("\"max\":100"));
            Assert.That(metricJson, Does.Contain("\"mean\":50.5"));
            Assert.That(metricJson, Does.Contain("\"p50\":50"));
            Assert.That(metricJson, Does.Contain("\"p95\":95"));
            Assert.That(metricJson, Does.Contain("\"p99\":99"));
            Assert.That(metricJson, Does.Contain("\"retention_policy\":\"phase_reserved_exact\""));
        }

        [Test]
        public void ZeroMeasurementSamplesAreUnavailableForMeasurementPhase()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("no-measurement");
            var summary = CaptureTelemetrySummary(
                "performance-no-measurement",
                resultsDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Ramp);
                    PerformanceTelemetry.TryRecord(metric, 1000);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.no-measurement");

            Assert.That(metricJson, Does.Contain("\"phase\":\"measurement\""));
            Assert.That(metricJson, Does.Contain("\"observation\":\"unavailable\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":0"));
            Assert.That(metricJson, Does.Contain("\"min\":null"));
            Assert.That(metricJson, Does.Contain("\"reason\":\"no_measurement_samples\""));
            Assert.That(metricJson, Does.Contain("\"raw_trace\":\"samples.ndjson\""));
        }

        [Test]
        public void RepeatedTelemetrySessionsDoNotReusePreviousMeasurementSamples()
        {
            var firstDirectory = CreateTelemetryTestDirectory("repeated-first");
            var firstSummary = CaptureTelemetrySummary(
                "performance-repeated-first",
                firstDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 100; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });

            var secondDirectory = CreateTelemetryTestDirectory("repeated-second");
            var secondSummary = CaptureTelemetrySummary(
                "performance-repeated-second",
                secondDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    PerformanceTelemetry.TryRecord(metric, 7);
                });

            Assert.That(ExtractMetricJson(firstSummary, "test.performance.repeated-first"), Does.Contain("\"sample_count\":100"));
            Assert.That(ExtractMetricJson(secondSummary, "test.performance.repeated-second"), Does.Contain("\"sample_count\":1"));
        }

        [Test]
        public void MeasurementRetentionAndRawTraceBudgetsAreExplicitlyQualified()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("retention-budget");
            var summary = CaptureTelemetrySummary(
                "performance-retention-budget",
                resultsDirectory,
                512,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 1000; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.retention-budget");

            Assert.That(summary, Does.Contain("\"status\":\"FAIL-INSTRUMENTATION\""));
            Assert.That(metricJson, Does.Contain("\"observation\":\"unavailable\""));
            Assert.That(metricJson, Does.Contain("\"reason\":\"measurement_sample_coverage_below_policy\""));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":1000"));
            Assert.That(metricJson, Does.Contain("\"sample_count\":0"));
            Assert.That(metricJson, Does.Contain("\"retained_sample_count\":384"));
            Assert.That(metricJson, Does.Contain("\"dropped_sample_count\":616"));
            Assert.That(metricJson, Does.Contain("\"raw_trace_sample_count\":256"));
            Assert.That(metricJson, Does.Contain("\"retention_policy\":\"phase_reserved_reservoir\""));
        }

        [Test]
        public void UnsupportedExecutionModesHaveExplicitDiagnostics()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.StandaloneHeadlessServer,
            };
            Assert.That(options.GetUnsupportedModeDiagnostic(), Does.Contain("blocked"));

            options.WorkloadMode = Test_PerformanceWorkloadMode.NetworkedClients;
            Assert.That(options.GetUnsupportedModeDiagnostic(), Does.Contain("real-client"));
        }

        [Test]
        public void FixtureScriptPrerequisitesAreBlockedInsteadOfRunnerErrors()
        {
            var classifyFailure = typeof(Test_PerformanceHarnessController).GetMethod(
                "ClassifyFailure",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(classifyFailure, Is.Not.Null);
            if (classifyFailure == null)
                return;

            var classification = classifyFailure.Invoke(
                null,
                new object[] { new Test_PerformancePrerequisiteException("fixture script is unavailable") });
            var classificationType = classification.GetType();

            Assert.That(classificationType.GetProperty("ReasonCode").GetValue(classification), Is.EqualTo("PREREQUISITE_BLOCKED"));
            Assert.That(classificationType.GetProperty("Status").GetValue(classification), Is.EqualTo(PerformanceRunStatus.BlockedPrerequisite));
        }

        [UnityTest]
        [Explicit("Requires the real Bootstrapper to MainMenu Play Mode transition; use the manual Issue #52 closure gate for the complete graphical workload.")]
        public IEnumerator RuntimeRunnerRootSurvivesBootstrapperTransition()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                Assert.Ignore("The Bootstrapper transition test must start from Edit Mode.");

            yield return new EnterPlayMode();

            GameObject runnerObject = null;
            var completed = false;
            try
            {
                runnerObject = Test_PerformanceHarnessController.CreateRuntimeRunnerObject();
                runnerObject.AddComponent<Test_PerformanceHarnessBehaviour>();
                var loadOperation = SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
                Assert.That(loadOperation, Is.Not.Null, "Unity did not create the Bootstrapper load operation.");
                if (loadOperation == null)
                    yield break;

                while (!loadOperation.isDone)
                    yield return null;

                var deadline = Time.realtimeSinceStartup + Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS;
                while (SceneManager.GetSceneByName("Bootstrapper").isLoaded
                    && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.True, "The ordinary client did not load MainMenu during Bootstrapper transition.");
                Assert.That(SceneManager.GetSceneByName("Bootstrapper").isLoaded, Is.False, "The ordinary client did not unload Bootstrapper during initialization.");
                Assert.That(runnerObject, Is.Not.Null, "The test runner root was destroyed during Bootstrapper unload.");
                completed = true;
            }
            finally
            {
                if (runnerObject != null)
                    UnityEngine.Object.Destroy(runnerObject);
                if (!completed && EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = false;
            }

            if (completed)
                yield return new ExitPlayMode();
        }

        [Test]
        public void GraphicalScenarioPlansAreDeterministicAndSeeded()
        {
            var first = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Forest,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);
            var second = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Forest,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(first.IsReady, Is.True);
            Assert.That(first.PlanHash, Is.EqualTo(second.PlanHash));
            Assert.That(first.Waypoints, Has.Length.EqualTo(4));
            Assert.That(first.RequestedObjectCount, Is.GreaterThan(200));
            Assert.That(first.RequiresWorldStreaming, Is.False);
            Test_GraphicalFixtureLayout.ValidatePlan(first);
        }

        [Test]
        public void GraphicalScenarioSeedAndActorDensityDoNotDowngradeSilently()
        {
            var changedSeed = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Outdoor,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED + 1);
            var actors = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Outdoor,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.VisibleTen,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(changedSeed.Status, Is.EqualTo("Blocked"));
            Assert.That(changedSeed.BlockedReason, Does.Contain("seed"));
            Assert.That(actors.RequestedActorCount, Is.EqualTo(10));
            Assert.That(actors.RequiresRealClientSession, Is.True);
        }

        [Test]
        public void AdjacentGraphicalScenarioRequiresNormalStreamingPath()
        {
            var plan = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.AdjacentTiles,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(plan.RequiresWorldStreaming, Is.True);
            Assert.That(plan.FixtureVariants, Has.Length.EqualTo(2));
            Assert.That(plan.Waypoints[1].WorldTileX, Is.EqualTo(0));
            Assert.That(plan.Waypoints[2].WorldTileX, Is.EqualTo(1));
        }

        [Test]
        public void TrackedHarnessSceneAndFixtureInputsRemainPresent()
        {
            Assert.That(File.Exists(Test_PerformanceRun.HARNESS_SCENE_PATH), Is.True);
            Assert.That(File.Exists(Test_ActorFixtureLayout.CATALOG_PATH), Is.True);
            Assert.That(File.Exists(Test_ActorFixtureLayout.SCENARIO_PATH), Is.True);
        }

        [Test]
        public void PendingRunStatePreservesRunIdentityAndValidatedOptions()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorldFixture = Test_WorldFixtureVariant.Forest,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
            };
            var state = new Test_PerformancePendingRunState
            {
                runID = "run-domain-reload-regression",
                resultsDirectory = "C:/project/Builds/Benchmarks/Results/run-domain-reload-regression",
                fixturePath = "C:/project/Builds/Benchmarks/Results/run-domain-reload-regression/fixture.json",
                options = Test_PerformancePendingRunOptions.Capture(options),
                lifecycle = "starting",
            };

            var restoredState = JsonUtility.FromJson<Test_PerformancePendingRunState>(JsonUtility.ToJson(state));
            var restoredOptions = restoredState.options.ToOptions();
            Assert.That(restoredState.runID, Is.EqualTo(state.runID));
            Assert.That(restoredState.resultsDirectory, Is.EqualTo(state.resultsDirectory));
            Assert.That(restoredState.fixturePath, Is.EqualTo(state.fixturePath));
            Assert.That(restoredState.lifecycle, Is.EqualTo("starting"));
            Assert.That(restoredOptions.WorldFixture, Is.EqualTo(options.WorldFixture));
            Assert.That(restoredOptions.WorkloadMode, Is.EqualTo(options.WorkloadMode));
            Assert.That(restoredOptions.Seed, Is.EqualTo(options.Seed));
        }

        [Test]
        public void PreviousSceneSetupStatePreservesRestoreMarkers()
        {
            var original = new[]
            {
                new SceneSetup
                {
                    path = "Assets/Tests/Performance/Harness/Editor/PerformanceHarnessServer.unity",
                    isLoaded = true,
                    isActive = true,
                    isSubScene = false,
                },
            };

            var state = Test_PerformanceSceneSetupState.Capture(original);
            var restored = state[0].ToSceneSetup();
            Assert.That(restored.path, Is.EqualTo(original[0].path));
            Assert.That(restored.isLoaded, Is.EqualTo(original[0].isLoaded));
            Assert.That(restored.isActive, Is.EqualTo(original[0].isActive));
            Assert.That(restored.isSubScene, Is.EqualTo(original[0].isSubScene));
        }

        private static string CaptureTelemetrySummary(
            string runID,
            string resultsDirectory,
            int capacity,
            Action<PerformanceMetricDefinition> recordSamples)
        {
            var metricID = "test.performance." + runID.Substring("performance-".Length);
            var metric = new PerformanceMetricDefinition(
                metricID,
                PerformanceMetricUnit.Milliseconds,
                PerformanceMetricSide.Server,
                "test",
                "test_run");
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext(runID, resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, capacity);
                PerformanceTelemetry.MarkWorkloadApplied();
                recordSamples(metric);
                Assert.That(
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.CompleteMeasured,
                        "TEST_COMPLETE",
                        "Telemetry contract test completed."),
                    Is.True);
                return File.ReadAllText(Path.Combine(resultsDirectory, "summary.json"));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        private static string CreateTelemetryTestDirectory(string name)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "NinthAgePerformanceTests",
                name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string ExtractMetricJson(string summary, string metricID)
        {
            var marker = "\"metric_id\":\"" + metricID + "\"";
            var start = summary.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "Metric was not present in summary.json.");
            var end = summary.IndexOf('}', start);
            Assert.That(end, Is.GreaterThan(start), "Metric JSON was not terminated.");
            return summary.Substring(start, end - start + 1);
        }
    }
}
