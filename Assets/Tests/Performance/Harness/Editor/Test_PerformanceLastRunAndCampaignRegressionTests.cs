using Game.Tests.Performance.Fixtures;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Regression coverage for finalized result selection, display contracts and campaign persistence.</summary>
    public sealed class Test_PerformanceLastRunAndCampaignRegressionTests
    {
        private readonly List<string> _createdResultDirectories = new();
        private string _createdCampaignID;

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_createdCampaignID))
            {
                if (Test_PerformanceJobQueue.HasCampaign
                    && string.Equals(Test_PerformanceJobQueue.CampaignID, _createdCampaignID, StringComparison.Ordinal))
                {
                    Test_PerformanceJobQueue.ClearDraft(out _);
                }

                var campaignDirectory = GetCampaignDirectory(_createdCampaignID);
                if (Directory.Exists(campaignDirectory))
                    Directory.Delete(campaignDirectory, true);
            }

            foreach (var directory in _createdResultDirectories)
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void ReaderSeparatesServerAndGraphicalScenarios()
        {
            var serverDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(-2),
                status: "BLOCKED-PREREQUISITE",
                metrics: "[]");
            var graphicalDirectory = CreateResult(
                graphical: true,
                scenario: "Forest",
                finishedUtc: DateTime.UtcNow.AddMinutes(-1),
                status: "FAIL-INSTRUMENTATION",
                metrics: "[]");

            var server = Test_PerformanceResultReader.ReadFinalized(serverDirectory);
            var graphical = Test_PerformanceResultReader.ReadFinalized(graphicalDirectory);

            Assert.That(server.IsVerified, Is.True, server.ErrorMessage);
            Assert.That(server.EvidenceScope, Does.Contain("ECS data-only"));
            Assert.That(server.Scenario, Is.EqualTo("Outdoor"));
            Assert.That(server.Seed, Is.EqualTo("424242"));
            Assert.That(server.RouteSummary, Is.EqualTo("Graphical route: not_applicable"));
            Assert.That(graphical.IsVerified, Is.True, graphical.ErrorMessage);
            Assert.That(graphical.EvidenceScope, Does.Contain("rendered-client"));
            Assert.That(graphical.Scenario, Is.EqualTo("Forest"));
            Assert.That(graphical.Workload, Is.EqualTo("S06-I-GEOMETRY"));
            foreach (var group in graphical.MetricGroups)
            {
                foreach (var metric in group.Metrics)
                    Assert.That(metric.MetricID, Does.Not.StartWith("server."));
            }
        }

        [Test]
        public void ReaderSelectsLatestFinalizedFailureAndExcludesActiveDirectory()
        {
            var successfulDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(1),
                status: "COMPLETE-MEASURED",
                metrics: "[]");
            var failedDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(2),
                status: "FAIL-TIMEOUT",
                metrics: "[]");
            var activeDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(3),
                status: "COMPLETE-MEASURED",
                metrics: "[]");

            var latest = Test_PerformanceResultReader.LoadLatest(activeDirectory);

            Assert.That(latest.IsVerified, Is.True, latest.ErrorMessage);
            Assert.That(latest.RunID, Is.EqualTo(Path.GetFileName(failedDirectory)));
            Assert.That(latest.Status, Is.EqualTo("FAIL-TIMEOUT"));
            Assert.That(latest.RunID, Is.Not.EqualTo(Path.GetFileName(successfulDirectory)));
            Assert.That(latest.RunID, Is.Not.EqualTo(Path.GetFileName(activeDirectory)));
        }

        [Test]
        public void ReaderPreservesUnitsCoverageZerosAndUnavailablePercentiles()
        {
            const string metrics = "["
                + "{\"metric_id\":\"server.ecs.actor_update_duration\",\"unit\":\"ms\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":100,\"attempted_sample_count\":125,\"dropped_sample_count\":25,\"min\":1,\"max\":10,\"mean\":5,\"p50\":4,\"p95\":9,\"p99\":10,\"measurement_coverage\":0.8,\"reason\":\"\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"server.memory.total_allocated_bytes\",\"unit\":\"bytes\",\"observation\":\"unavailable\",\"phase\":\"measurement\",\"sample_count\":0,\"attempted_sample_count\":0,\"dropped_sample_count\":0,\"min\":null,\"max\":null,\"mean\":null,\"p50\":null,\"p95\":null,\"p99\":null,\"measurement_coverage\":null,\"reason\":\"no source\",\"retention_policy\":\"not_collected\"},"
                + "{\"metric_id\":\"server.ecs.world_spawn_duration\",\"unit\":\"ms\",\"observation\":\"unavailable\",\"phase\":\"measurement\",\"sample_count\":0,\"attempted_sample_count\":125,\"dropped_sample_count\":25,\"min\":null,\"max\":null,\"mean\":null,\"p50\":null,\"p95\":null,\"p99\":null,\"measurement_coverage\":0.8,\"reason\":\"measurement_sample_coverage_below_policy\",\"retention_policy\":\"phase_reserved_reservoir\"},"
                + "{\"metric_id\":\"server.memory.mono_used_bytes\",\"unit\":\"bytes\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":0,\"max\":0,\"mean\":0,\"p50\":0,\"p95\":0,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"}"
                + "]";
            var directory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(4),
                status: "FAIL-INSTRUMENTATION",
                metrics: metrics);

            var view = Test_PerformanceResultReader.ReadFinalized(directory);
            var measured = FindMetric(view, "server.ecs.actor_update_duration");
            var unavailable = FindMetric(view, "server.memory.total_allocated_bytes");
            var coverageRejected = FindMetric(view, "server.ecs.world_spawn_duration");
            var zero = FindMetric(view, "server.memory.mono_used_bytes");

            Assert.That(view.IsVerified, Is.True, view.ErrorMessage);
            Assert.That(measured.SampleCount, Is.EqualTo(100));
            Assert.That(measured.P95, Is.EqualTo(9));
            Assert.That(measured.P99, Is.EqualTo(10));
            Assert.That(measured.MeasurementCoverage, Is.EqualTo(0.8d));
            Assert.That(measured.DisplayValue, Does.Contain("warning retained 100/125"));
            Assert.That(measured.DisplayValue, Does.Contain("[ms]"));
            Assert.That(unavailable.Observation, Is.EqualTo("unavailable"));
            Assert.That(unavailable.DisplayValue, Is.EqualTo("unavailable [bytes]"));
            Assert.That(coverageRejected.Observation, Is.EqualTo("unavailable"));
            Assert.That(coverageRejected.MeasurementCoverage, Is.EqualTo(0.8d));
            Assert.That(coverageRejected.DisplayValue, Is.EqualTo("unavailable [ms]"));
            Assert.That(zero.P50, Is.EqualTo(0));
            Assert.That(zero.DisplayValue, Does.Contain("p50 0"));
            Assert.That(zero.DisplayValue, Does.Contain("p99 insufficient_samples"));
        }

        [Test]
        public void ReaderRejectsMalformedPartialWrongIdentitySchemaAndOutsideResults()
        {
            var directory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(5),
                status: "BLOCKED-PREREQUISITE",
                metrics: "[]");
            var summaryPath = Path.Combine(directory, "summary.json");
            var originalSummary = File.ReadAllText(summaryPath);

            File.WriteAllText(summaryPath, "{");
            AssertUnverified(directory, "malformed");

            File.Delete(summaryPath);
            AssertUnverified(directory, "missing");

            File.WriteAllText(summaryPath, originalSummary);
            File.Delete(Path.Combine(directory, "report.md"));
            AssertUnverified(directory, "partial artifact");
            File.WriteAllText(Path.Combine(directory, "report.md"), "# test result");

            File.WriteAllText(summaryPath, originalSummary.Replace("benchmark-summary/v1", "unknown/v9"));
            AssertUnverified(directory, "schema");

            File.WriteAllText(summaryPath, originalSummary.Replace(Path.GetFileName(directory), "run-wrong-id"));
            AssertUnverified(directory, "run ID");

            var outside = Test_PerformanceResultReader.ReadFinalized(Application.dataPath);
            Assert.That(outside.IsVerified, Is.False);
            Assert.That(outside.ErrorMessage, Does.Contain("outside"));
        }

        [Test]
        public void ReaderDoesNotShowOlderMetricsWhenNewestAttemptIsMalformed()
        {
            var olderDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(-1),
                status: "COMPLETE-MEASURED",
                metrics: "[{\"metric_id\":\"server.ecs.actor_update_duration\",\"unit\":\"ms\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":100,\"attempted_sample_count\":100,\"dropped_sample_count\":0,\"min\":1,\"max\":2,\"mean\":1,\"p50\":1,\"p95\":2,\"p99\":2,\"measurement_coverage\":1,\"reason\":\"\",\"retention_policy\":\"phase_reserved_exact\"}]");
            var newestDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow,
                status: "FAIL-INSTRUMENTATION",
                metrics: "[]");
            File.WriteAllText(Path.Combine(newestDirectory, "summary.json"), "{");

            var latest = Test_PerformanceResultReader.LoadLatest(null);

            Assert.That(latest.IsVerified, Is.False);
            Assert.That(latest.RunID, Is.EqualTo(Path.GetFileName(newestDirectory)));
            Assert.That(latest.MetricGroups, Is.Empty);
            Assert.That(latest.ErrorMessage, Does.Contain("malformed"));
            Assert.That(olderDirectory, Is.Not.Null);
        }

        [Test]
        public void OptionsAndJobsCaptureIndependentImmutableSnapshots()
        {
            var options = new Test_PerformanceHarnessOptions();
            var clone = options.Clone();
            clone.WorldFixture = Test_WorldFixtureVariant.Forest;
            clone.ActorCount = 1;

            var job = new Test_PerformanceCampaignJob
            {
                job_id = "job-original",
                options = options,
                repeat_count = 5,
                validation_state = "valid",
            };
            var duplicate = job.Clone("job-duplicate");
            duplicate.options.Seed = options.Seed + 1;

            Assert.That(options.WorldFixture, Is.EqualTo(Test_WorldFixtureVariant.Outdoor));
            Assert.That(options.ActorCount, Is.EqualTo(100));
            Assert.That(job.options.Seed, Is.EqualTo(424242));
            Assert.That(duplicate.job_id, Is.Not.EqualTo(job.job_id));
            Assert.That(duplicate.options, Is.Not.SameAs(job.options));
            Assert.That(job.options.Seed, Is.EqualTo(424242));
        }

        [Test]
        public void UnsupportedOptionsFailWithExplicitDiagnostics()
        {
            var wrongProfile = new Test_PerformanceHarnessOptions { ProfileID = "unknown-profile" };
            Assert.That(wrongProfile.TryValidate(out var profileDiagnostic, out _, out _), Is.False);
            Assert.That(profileDiagnostic, Does.Contain("Only the tracked profile"));

            var wrongSeed = new Test_PerformanceHarnessOptions { Seed = 7 };
            Assert.That(wrongSeed.TryValidate(out var seedDiagnostic, out _, out _), Is.False);
            Assert.That(seedDiagnostic, Does.Contain("seed 424242"));

            foreach (var mode in new[]
            {
                Test_PerformanceWorkloadMode.FullNetworkedActors,
                Test_PerformanceWorkloadMode.StandaloneHeadlessServer,
                Test_PerformanceWorkloadMode.NetworkedClients,
            })
            {
                var options = new Test_PerformanceHarnessOptions { WorkloadMode = mode };
                Assert.That(options.TryValidate(out var diagnostic, out _, out _), Is.False, mode.ToString());
                Assert.That(diagnostic, Does.Contain("require").Or.Contain("blocked"), mode.ToString());
            }

            var actorDensity = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                ActorDensity = Test_GraphicalActorDensity.VisibleTen,
            };
            Assert.That(actorDensity.TryValidate(out var actorDiagnostic, out _, out _), Is.False);
            Assert.That(actorDiagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CampaignSourceAndFixtureIdentityReportDifferences()
        {
            var source = new Test_PerformanceCampaignSource
            {
                commit_sha = "commit-a",
                source_sha256 = "source-a",
                dirty = true,
                diff_sha256 = "diff-a",
                packages_sha256 = "packages-a",
                packages_lock_sha256 = "lock-a",
                purrnet_sha256 = "purrnet-a",
            };
            var changedSource = new Test_PerformanceCampaignSource
            {
                commit_sha = "commit-a",
                source_sha256 = "source-b",
                dirty = false,
                diff_sha256 = "diff-b",
                packages_sha256 = "packages-a",
                packages_lock_sha256 = "lock-a",
                purrnet_sha256 = "purrnet-a",
            };
            var fixture = new Test_PerformanceCampaignFixtureIdentity { configuration_sha256 = "fixture-a" };
            var changedFixture = new Test_PerformanceCampaignFixtureIdentity { configuration_sha256 = "fixture-b" };

            var sourceDifference = source.DescribeDifference(changedSource);

            Assert.That(source.DescribeDifference(source), Is.Null);
            Assert.That(sourceDifference, Does.Contain("source SHA"));
            Assert.That(sourceDifference, Does.Contain("dirty flag"));
            Assert.That(sourceDifference, Does.Contain("working-tree diff SHA"));
            Assert.That(fixture.DescribeDifference(fixture), Is.Null);
            Assert.That(fixture.DescribeDifference(changedFixture), Does.Contain("Fixture/configuration identity changed"));
        }

        [Test]
        public void CampaignManifestRoundTripPreservesOrderAttemptsAndEvidenceIdentity()
        {
            var manifest = new Test_PerformanceCampaignManifest
            {
                campaign_id = "campaign-test",
                state = Test_PerformanceCampaignState.PAUSED,
                stop_policy = Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE,
                source = new Test_PerformanceCampaignSource { source_sha256 = "source-test", dirty = true },
            };
            manifest.jobs.Add(new Test_PerformanceCampaignJob
            {
                job_id = "job-a",
                options = new Test_PerformanceHarnessOptions { WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal },
                repeat_count = 2,
                validation_state = "valid",
            });
            manifest.jobs.Add(new Test_PerformanceCampaignJob
            {
                job_id = "job-b",
                options = new Test_PerformanceHarnessOptions { WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity },
                repeat_count = 3,
                validation_state = "blocked",
                validation_diagnostic = "fixture blocked",
            });
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = "attempt-a-1",
                job_id = "job-a",
                repeat_index = 1,
                state = Test_PerformanceCampaignAttemptState.COMPLETED,
                run_id = "run-a-1",
                result_path = "Builds/Benchmarks/Results/run-a-1",
                result_status = "COMPLETE-MEASURED",
                stop_reason = "COMPLETE",
            });
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = "attempt-b-1",
                job_id = "job-b",
                repeat_index = 1,
                state = Test_PerformanceCampaignAttemptState.BLOCKED,
                result_status = "BLOCKED-PREREQUISITE",
                stop_reason = "QUEUE_PREVALIDATION_BLOCKED",
            });
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = "attempt-a-2",
                job_id = "job-a",
                repeat_index = 2,
                state = Test_PerformanceCampaignAttemptState.FAILED,
                result_status = "FAIL-TIMEOUT",
                stop_reason = "RUN_TIMEOUT",
            });
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = "attempt-b-2",
                job_id = "job-b",
                repeat_index = 2,
                state = Test_PerformanceCampaignAttemptState.CANCELLED,
                stop_reason = "QUEUE_CANCELLED",
            });
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = "attempt-b-3",
                job_id = "job-b",
                repeat_index = 3,
                state = Test_PerformanceCampaignAttemptState.INCOMPLETE,
                diagnostic = "Editor restarted",
            });

            var roundTrip = JsonUtility.FromJson<Test_PerformanceCampaignManifest>(JsonUtility.ToJson(manifest));

            Assert.That(roundTrip.schema_version, Is.EqualTo("performance-campaign/v1"));
            Assert.That(roundTrip.state, Is.EqualTo(Test_PerformanceCampaignState.PAUSED));
            Assert.That(roundTrip.jobs, Has.Count.EqualTo(2));
            Assert.That(roundTrip.jobs[0].job_id, Is.EqualTo("job-a"));
            Assert.That(roundTrip.jobs[1].job_id, Is.EqualTo("job-b"));
            Assert.That(roundTrip.jobs[1].validation_diagnostic, Is.EqualTo("fixture blocked"));
            Assert.That(roundTrip.attempts, Has.Count.EqualTo(5));
            Assert.That(roundTrip.attempts[0].run_id, Is.EqualTo("run-a-1"));
            Assert.That(roundTrip.attempts[0].result_path, Does.Contain("run-a-1"));
            Assert.That(roundTrip.attempts[0].result_status, Is.EqualTo("COMPLETE-MEASURED"));
            Assert.That(roundTrip.attempts[1].state, Is.EqualTo(Test_PerformanceCampaignAttemptState.BLOCKED));
            Assert.That(roundTrip.attempts[2].result_status, Is.EqualTo("FAIL-TIMEOUT"));
            Assert.That(roundTrip.attempts[3].state, Is.EqualTo(Test_PerformanceCampaignAttemptState.CANCELLED));
            Assert.That(roundTrip.attempts[4].diagnostic, Is.EqualTo("Editor restarted"));

            var index = new Test_PerformanceCampaignIndex
            {
                campaign_id = "campaign-test",
                state = Test_PerformanceCampaignState.PAUSED,
                stop_policy = Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE,
            };
            index.attempts.Add(new Test_PerformanceCampaignIndexAttempt
            {
                job_id = "job-a",
                repeat_index = 1,
                run_id = "run-a-1",
                result_path = "Builds/Benchmarks/Results/run-a-1",
                status = "COMPLETE-MEASURED",
                stop_reason = "COMPLETE",
            });
            var indexRoundTrip = JsonUtility.FromJson<Test_PerformanceCampaignIndex>(JsonUtility.ToJson(index));
            Assert.That(indexRoundTrip.schema_version, Is.EqualTo("performance-campaign-index/v1"));
            Assert.That(indexRoundTrip.attempts[0].run_id, Is.EqualTo("run-a-1"));
            Assert.That(indexRoundTrip.attempts[0].result_path, Does.Contain("run-a-1"));
            Assert.That(indexRoundTrip.attempts[0].stop_reason, Is.EqualTo("COMPLETE"));
        }

        [Test]
        public void QueueLimitsAndPoliciesAreExplicitAndBounded()
        {
            Assert.That(Test_PerformanceJobQueue.MAX_JOBS, Is.EqualTo(50));
            Assert.That(Test_PerformanceJobQueue.MAX_REPEAT_COUNT, Is.EqualTo(20));
            Assert.That(Test_PerformanceJobQueue.MAX_TOTAL_ATTEMPTS, Is.EqualTo(100));
            Assert.That(Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE, Is.EqualTo("stop_on_failure"));
            Assert.That(Test_PerformanceCampaignStopPolicy.CONTINUE_AFTER_FAILURE, Is.EqualTo("continue_after_failure"));

            var job = new Test_PerformanceCampaignJob
            {
                job_id = "bounded-job",
                options = new Test_PerformanceHarnessOptions(),
                repeat_count = Test_PerformanceJobQueue.MAX_REPEAT_COUNT,
            };
            Assert.That(job.repeat_count, Is.LessThanOrEqualTo(Test_PerformanceJobQueue.MAX_REPEAT_COUNT));
            Assert.That((long)Test_PerformanceJobQueue.MAX_JOBS * Test_PerformanceJobQueue.MAX_REPEAT_COUNT, Is.GreaterThanOrEqualTo(Test_PerformanceJobQueue.MAX_TOTAL_ATTEMPTS));
        }

        [Test]
        public void SupportedBaselinePresetRetainsOnlyValidatedScenariosAndFiveRepeats()
        {
            if (Test_PerformanceJobQueue.HasCampaign)
                Assert.Ignore("A persisted campaign exists; the regression test will not modify user campaign state.");

            Assert.That(Test_PerformanceJobQueue.LoadSupportedBaselinePreset(out var diagnostic), Is.True, diagnostic);
            _createdCampaignID = Test_PerformanceJobQueue.CampaignID;

            Assert.That(Test_PerformanceJobQueue.Jobs.Count + Test_PerformanceJobQueue.ExcludedScenarios.Count, Is.EqualTo(5));
            foreach (var job in Test_PerformanceJobQueue.Jobs)
            {
                Assert.That(job.repeat_count, Is.EqualTo(5));
                Assert.That(job.options.Seed, Is.EqualTo(424242));
                Assert.That(job.options.DensityTier, Is.EqualTo(Test_WorldFixtureDensityTier.D1_Default));
                if (job.options.IsGraphicalClient)
                {
                    Assert.That(job.options.ActorDensity, Is.EqualTo(Test_GraphicalActorDensity.None));
                    Assert.That(job.options.ActorCount, Is.EqualTo(0));
                }
                else
                    Assert.That(job.options.ActorCount, Is.EqualTo(100));
            }

            foreach (var excluded in Test_PerformanceJobQueue.ExcludedScenarios)
                Assert.That(excluded, Does.Contain(":"));
            Assert.That(Test_PerformanceJobQueue.TotalAttempts, Is.EqualTo(Test_PerformanceJobQueue.Jobs.Count * 5));
        }

        [Test]
        public void DraftQueueOperationsPreserveOrderAndRejectInvalidRepeats()
        {
            if (Test_PerformanceJobQueue.HasCampaign)
                Assert.Ignore("A persisted campaign exists; the regression test will not modify user campaign state.");

            var firstOptions = new Test_PerformanceHarnessOptions { WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal };
            var secondOptions = new Test_PerformanceHarnessOptions { WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity };
            Assert.That(Test_PerformanceJobQueue.AddCurrentConfiguration(firstOptions, out var diagnostic), Is.True, diagnostic);
            _createdCampaignID = Test_PerformanceJobQueue.CampaignID;
            var firstID = Test_PerformanceJobQueue.Jobs[0].job_id;
            firstOptions.WorldFixture = Test_WorldFixtureVariant.Forest;
            Assert.That(Test_PerformanceJobQueue.Jobs[0].options.WorldFixture, Is.EqualTo(Test_WorldFixtureVariant.Outdoor));

            Assert.That(Test_PerformanceJobQueue.AddCurrentConfiguration(secondOptions, out diagnostic), Is.True, diagnostic);
            var secondID = Test_PerformanceJobQueue.Jobs[1].job_id;
            Assert.That(Test_PerformanceJobQueue.SetRepeatCount(firstID, 3, out diagnostic), Is.True, diagnostic);
            Assert.That(Test_PerformanceJobQueue.SetRepeatCount(firstID, 0, out diagnostic), Is.False);
            Assert.That(Test_PerformanceJobQueue.SetRepeatCount(firstID, Test_PerformanceJobQueue.MAX_REPEAT_COUNT + 1, out diagnostic), Is.False);

            Assert.That(Test_PerformanceJobQueue.DuplicateJob(firstID, out diagnostic), Is.True, diagnostic);
            var duplicateID = Test_PerformanceJobQueue.Jobs[1].job_id;
            Assert.That(duplicateID, Is.Not.EqualTo(firstID));
            Assert.That(Test_PerformanceJobQueue.MoveJob(duplicateID, 1, out diagnostic), Is.True, diagnostic);
            Assert.That(Test_PerformanceJobQueue.Jobs[0].job_id, Is.EqualTo(firstID));
            Assert.That(Test_PerformanceJobQueue.Jobs[1].job_id, Is.EqualTo(secondID));
            Assert.That(Test_PerformanceJobQueue.Jobs[2].job_id, Is.EqualTo(duplicateID));
            Assert.That(Test_PerformanceJobQueue.RemoveJob(duplicateID, out diagnostic), Is.True, diagnostic);
            Assert.That(Test_PerformanceJobQueue.Jobs, Has.Count.EqualTo(2));
            Assert.That(Test_PerformanceJobQueue.QueuedCount, Is.EqualTo(4));

            var campaignDirectory = GetCampaignDirectory(_createdCampaignID);
            var campaignPath = Path.Combine(campaignDirectory, "campaign.json");
            var indexPath = Path.Combine(campaignDirectory, "index.json");
            Assert.That(File.Exists(campaignPath), Is.True);
            Assert.That(File.Exists(indexPath), Is.True);
            Assert.That(JsonUtility.FromJson<Test_PerformanceCampaignManifest>(File.ReadAllText(campaignPath)), Is.Not.Null);
            Assert.That(JsonUtility.FromJson<Test_PerformanceCampaignIndex>(File.ReadAllText(indexPath)), Is.Not.Null);
            Assert.That(Directory.GetFiles(campaignDirectory, "*.tmp"), Is.Empty);
        }

        private static Test_PerformanceMetricView FindMetric(Test_PerformanceLastRunView view, string metricID)
        {
            foreach (var group in view.MetricGroups)
            {
                foreach (var metric in group.Metrics)
                {
                    if (metric.MetricID == metricID)
                        return metric;
                }
            }

            Assert.Fail("Metric was not present: " + metricID);
            return null;
        }

        private static void AssertUnverified(string directory, string context)
        {
            var view = Test_PerformanceResultReader.ReadFinalized(directory);
            Assert.That(view.IsVerified, Is.False, context);
            Assert.That(view.ErrorMessage, Is.Not.Null.And.Not.Empty, context);
        }

        private string CreateResult(bool graphical, string scenario, DateTime finishedUtc, string status, string metrics)
        {
            var runID = "run-" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var directory = Path.Combine(GetResultRoot(), runID);
            Directory.CreateDirectory(directory);
            _createdResultDirectories.Add(directory);

            var fixtureJson = graphical
                ? $"{{\"schema_version\":\"benchmark-fixture/v1\",\"profile\":{{\"id\":\"P1-S0-06\",\"revision\":\"1.0.0\",\"variant\":\"S06-I-GEOMETRY\"}},\"random\":{{\"master_seed\":424242}},\"population\":{{\"npcs\":{{\"requested\":0}},\"accounting\":{{\"full_gameobject_purrnet_npcs\":0}}}},\"environment\":{{\"tile_archetype\":\"{scenario}\"}},\"schedules\":{{\"movement_routes\":[]}}}}"
                : "{\"schema_version\":\"benchmark-fixture/v1\",\"profile\":{\"id\":\"P1-S0-04\",\"revision\":\"1.0.0\",\"variant\":\"S04-I-DATA\"},\"random\":{\"master_seed\":424242},\"population\":{\"npcs\":{\"requested\":100},\"accounting\":{\"full_gameobject_purrnet_npcs\":0}},\"environment\":{\"tile_archetype\":\"Outdoor\"},\"schedules\":{\"movement_routes\":[]}}";
            var fixturePath = Path.Combine(directory, "fixture.json");
            File.WriteAllText(fixturePath, fixtureJson);
            var fixtureHash = Test_PerformanceRunMetadata.HashFile(fixturePath);

            var runJson = graphical
                ? $"{{\"schema_version\":\"benchmark-run/v1\",\"run_id\":\"{runID}\",\"created_utc\":\"{finishedUtc.AddSeconds(-1):o}\",\"fixture_ref\":{{\"path\":\"fixture.json\",\"sha256\":\"{fixtureHash}\"}},\"git\":{{\"commit_sha\":\"test-commit\",\"dirty\":true}},\"build\":{{\"kind\":\"rendered_client\"}},\"render\":{{\"mode\":\"rendered\",\"graphical_client\":true}}}}"
                : $"{{\"schema_version\":\"benchmark-run/v1\",\"run_id\":\"{runID}\",\"created_utc\":\"{finishedUtc.AddSeconds(-1):o}\",\"fixture\":{{\"path\":\"fixture.json\",\"sha256\":\"{fixtureHash}\"}},\"git\":{{\"commit_sha\":\"test-commit\",\"dirty\":true}},\"build\":{{\"kind\":\"editor_playmode_server\"}},\"render\":{{\"mode\":\"server_only\",\"graphical_client\":false}},\"runtime\":{{\"scene_fixture\":\"Outdoor\"}}}}";
            File.WriteAllText(Path.Combine(directory, "run.json"), runJson);

            var achieved = graphical
                ? "{\"npc_actors\":0,\"ecs_entities\":0,\"graphical\":{\"requested_objects\":10,\"achieved_objects\":10,\"requested_actors\":0,\"achieved_actors\":0,\"route_waypoints_requested\":0,\"route_waypoints_reached\":0,\"route_complete\":true}}"
                : "{\"npc_actors\":0,\"ecs_entities\":100,\"population\":{\"requested_ecs_entities\":100,\"measurement_end_ecs_entities\":100},\"activity\":{\"snapshot_captured\":false}}";
            var correctness = status == "COMPLETE-MEASURED" ? "pass" : "unverified";
            var summaryJson = $"{{\"schema_version\":\"benchmark-summary/v1\",\"run_id\":\"{runID}\",\"fixture_sha256\":\"{fixtureHash}\",\"status\":\"{status}\",\"started_utc\":\"{finishedUtc.AddSeconds(-2):o}\",\"finished_utc\":\"{finishedUtc:o}\",\"phase_completion\":{{\"preflight\":false,\"ramp\":false,\"warmup\":false,\"measurement\":false,\"recovery\":false}},\"achieved\":{achieved},\"stop\":{{\"reason_code\":\"TEST_STOP\",\"message\":\"test result\"}},\"correctness\":{{\"status\":\"{correctness}\",\"checked\":0,\"failed\":0,\"unverified\":[]}},\"metrics\":{metrics},\"raw_files\":[]}}";
            File.WriteAllText(Path.Combine(directory, "summary.json"), summaryJson);
            File.WriteAllText(Path.Combine(directory, "report.md"), "# test result");
            File.WriteAllText(Path.Combine(directory, "artifacts.json"), $"{{\"schema_version\":\"benchmark-artifacts/v1\",\"run_id\":\"{runID}\",\"files\":[{{\"path\":\"fixture.json\"}},{{\"path\":\"run.json\"}},{{\"path\":\"summary.json\"}},{{\"path\":\"report.md\"}}]}}");
            return directory;
        }

        private static string GetResultRoot()
        {
            return Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, Test_PerformanceRun.RESULT_ROOT));
        }

        private static string GetCampaignDirectory(string campaignID)
        {
            return Path.Combine(GetResultRoot(), "Campaigns", campaignID);
        }
    }
}
