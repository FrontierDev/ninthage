using Game.Tests.Performance.Fixtures;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Regression coverage for finalized result selection, display contracts and campaign persistence.</summary>
    public sealed class Test_PerformanceLastRunAndCampaignRegressionTests
    {
        private readonly List<string> _createdResultDirectories = new();
        private readonly List<string> _createdCampaignDirectories = new();
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

            foreach (var campaignDirectory in _createdCampaignDirectories)
            {
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
        public void ReaderSelectsFixtureReferenceFromDeclaredExecutionMode()
        {
            var serverDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(-2),
                status: "COMPLETE-MEASURED",
                metrics: "[]",
                includeConflictingFixtureReferences: true);
            var graphicalDirectory = CreateResult(
                graphical: true,
                scenario: "Forest",
                finishedUtc: DateTime.UtcNow.AddMinutes(-1),
                status: "COMPLETE-MEASURED",
                metrics: "[]",
                includeConflictingFixtureReferences: true);

            var server = Test_PerformanceResultReader.ReadFinalized(serverDirectory);
            var graphical = Test_PerformanceResultReader.ReadFinalized(graphicalDirectory);

            Assert.That(server.IsVerified, Is.True, server.ErrorMessage);
            Assert.That(graphical.IsVerified, Is.True, graphical.ErrorMessage);

            var invalidGraphicalDirectory = CreateResult(
                graphical: true,
                scenario: "Forest",
                finishedUtc: DateTime.UtcNow,
                status: "COMPLETE-MEASURED",
                metrics: "[]");
            var invalidRunPath = Path.Combine(invalidGraphicalDirectory, "run.json");
            var invalidRunJson = File.ReadAllText(invalidRunPath).Replace(
                "\"fixture_ref\":{\"path\":\"fixture.json\"",
                "\"fixture_ref\":{\"path\":\"missing.json\"",
                StringComparison.Ordinal);
            File.WriteAllText(invalidRunPath, invalidRunJson);

            AssertUnverified(invalidGraphicalDirectory, "incorrect graphical fixture reference");
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
        public void ProductionGraphicalEvidenceWriterUsesTheReaderValidationPath()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                GraphicalScenario = Test_GraphicalScenario.Outdoor,
            };
            var run = Test_PerformanceRun.Create(options);
            _createdResultDirectories.Add(run.ResultsDirectory);
            run.Block("test-only production exporter validation");

            var view = Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory);

            Assert.That(view.IsVerified, Is.True, view.ErrorMessage);
            Assert.That(File.ReadAllText(run.RunPath), Does.Contain("\"fixture_ref\""));
            Assert.That(File.ReadAllText(run.RunPath), Does.Contain("\"mode\":\"rendered\""));
        }

        [Test]
        public void ComparatorUsesMetricQualifiedNAndReevaluatesAfterEligibleRun()
        {
            var baselineID = "campaign-regression-baseline-" + Guid.NewGuid().ToString("N");
            var candidateID = "campaign-regression-candidate-" + Guid.NewGuid().ToString("N");
            var baselineDirectory = CreateCampaignDirectory(baselineID);
            var candidateDirectory = CreateCampaignDirectory(candidateID);
            _createdCampaignDirectories.Add(baselineDirectory);
            _createdCampaignDirectories.Add(candidateDirectory);

            var baseline = BuildGraphicalCampaign(baselineID, 5);
            var candidate = BuildGraphicalCampaign(candidateID, 5);
            for (var index = 0; index < 5; index++)
            {
                var baselineResult = CreateResult(
                    graphical: true,
                    scenario: "Outdoor",
                    finishedUtc: DateTime.UtcNow.AddMinutes(-20 + index),
                    status: "COMPLETE-MEASURED",
                    metrics: BuildRequiredGraphicalMetrics(includeRejectedFrameMetric: false));
                var candidateResult = CreateResult(
                    graphical: true,
                    scenario: "Outdoor",
                    finishedUtc: DateTime.UtcNow.AddMinutes(-10 + index),
                    status: "COMPLETE-MEASURED",
                    metrics: BuildRequiredGraphicalMetrics(includeRejectedFrameMetric: index == 0));
                AddCompletedAttempt(baseline, index, baselineResult);
                AddCompletedAttempt(candidate, index, candidateResult);
            }
            WriteCampaignManifest(baselineDirectory, baseline);
            WriteCampaignManifest(candidateDirectory, candidate);

            var firstReport = CompareCampaigns(baselineID, candidateID);
            var firstGroup = firstReport.Groups.Single(group => group.ProfileID == "P1-S0-06");
            var firstFrameMetric = firstGroup.Metrics.Single(metric => metric.MetricID == "client.rendered.frame_time");
            var firstRouteMetric = firstGroup.Metrics.Single(metric => metric.MetricID == "harness.graphical.route_distance");
            Assert.That(firstReport.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.PROVISIONAL));
            Assert.That(firstGroup.CandidateValidCount, Is.EqualTo(5));
            Assert.That(firstFrameMetric.CandidateQualified, Is.EqualTo(4));
            Assert.That(firstFrameMetric.CandidateP99Qualified, Is.EqualTo(4));
            Assert.That(firstRouteMetric.Candidate.MedianP99, Is.Null);

            var eligibleResult = CreateResult(
                graphical: true,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow,
                status: "COMPLETE-MEASURED",
                metrics: BuildRequiredGraphicalMetrics(includeRejectedFrameMetric: false));
            AddCompletedAttempt(candidate, 5, eligibleResult);
            WriteCampaignManifest(candidateDirectory, candidate);

            var secondReport = CompareCampaigns(baselineID, candidateID);
            var secondGroup = secondReport.Groups.Single(group => group.ProfileID == "P1-S0-06");
            var secondFrameMetric = secondGroup.Metrics.Single(metric => metric.MetricID == "client.rendered.frame_time");
            Assert.That(secondReport.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.COMPARABLE));
            Assert.That(secondGroup.CandidateValidCount, Is.EqualTo(6));
            Assert.That(secondFrameMetric.CandidateQualified, Is.EqualTo(5));
            Assert.That(secondFrameMetric.CandidateP99Qualified, Is.EqualTo(5));
        }

        [Test]
        public void ComparatorReportsMissingScenarioGroupsAsProvisional()
        {
            var baselineID = "campaign-five-scenario-baseline-" + Guid.NewGuid().ToString("N");
            var candidateID = "campaign-outdoor-only-candidate-" + Guid.NewGuid().ToString("N");
            var baselineDirectory = CreateCampaignDirectory(baselineID);
            var candidateDirectory = CreateCampaignDirectory(candidateID);
            _createdCampaignDirectories.Add(baselineDirectory);
            _createdCampaignDirectories.Add(candidateDirectory);

            var baseline = BuildFiveScenarioCampaign(baselineID, 1);
            var candidate = BuildGraphicalCampaign(candidateID, 1);
            for (var index = 0; index < baseline.jobs.Count; index++)
            {
                var job = baseline.jobs[index];
                var graphical = job.options.IsGraphicalClient;
                var scenario = graphical ? job.options.GraphicalScenario.ToString() : job.options.WorldFixture.ToString();
                var metrics = graphical ? BuildRequiredGraphicalMetrics(false) : BuildRequiredServerMetrics(false);
                AddCompletedAttempt(
                    baseline,
                    index,
                    CreateResult(graphical, scenario, DateTime.UtcNow.AddMinutes(-10 + index), "COMPLETE-MEASURED", metrics),
                    job.job_id);
            }
            AddCompletedAttempt(
                candidate,
                0,
                CreateResult(true, "Outdoor", DateTime.UtcNow, "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false)));
            WriteCampaignManifest(baselineDirectory, baseline);
            WriteCampaignManifest(candidateDirectory, candidate);

            var report = CompareCampaigns(baselineID, candidateID, 1);
            var matchedOutdoor = report.Groups.Single(group => group.Label == "P1-S0-06 / GraphicalClient / Outdoor");

            Assert.That(report.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.PROVISIONAL));
            Assert.That(matchedOutdoor.Classification, Is.EqualTo(Test_PerformanceCampaignGroupClassification.MATCHED));
            Assert.That(matchedOutdoor.BaselineValidCount, Is.EqualTo(1));
            Assert.That(matchedOutdoor.CandidateValidCount, Is.EqualTo(1));
            Assert.That(matchedOutdoor.Metrics.Where(metric => metric.IsRequired).All(metric => metric.CandidateQualified == 1), Is.True);
            Assert.That(report.Groups.Where(group => group.Classification == Test_PerformanceCampaignGroupClassification.BASELINE_ONLY)
                .Select(group => group.Label), Is.EquivalentTo(new[]
                {
                    "P1-S0-04 / EcsDataOnlyActivity / Outdoor",
                    "P1-S0-04 / EcsDataOnlyMinimal / Outdoor",
                    "P1-S0-06 / GraphicalClient / Forest",
                    "P1-S0-06 / GraphicalClient / Settlement",
                }));
            Assert.That(report.Reasons, Has.Some.EqualTo("Candidate campaign " + candidateID + " lacks scenario groups: P1-S0-04 / EcsDataOnlyActivity / Outdoor, P1-S0-04 / EcsDataOnlyMinimal / Outdoor, P1-S0-06 / GraphicalClient / Forest, P1-S0-06 / GraphicalClient / Settlement."));
            Assert.That(report.Summary, Does.Contain("missing candidate groups [P1-S0-04 / EcsDataOnlyActivity / Outdoor"));

            var fullyMatchedID = "campaign-five-scenario-matched-" + Guid.NewGuid().ToString("N");
            var fullyMatchedDirectory = CreateCampaignDirectory(fullyMatchedID);
            _createdCampaignDirectories.Add(fullyMatchedDirectory);
            var fullyMatched = BuildFiveScenarioCampaign(fullyMatchedID, 1);
            for (var index = 0; index < fullyMatched.jobs.Count; index++)
            {
                var job = fullyMatched.jobs[index];
                var graphical = job.options.IsGraphicalClient;
                var scenario = graphical ? job.options.GraphicalScenario.ToString() : job.options.WorldFixture.ToString();
                AddCompletedAttempt(
                    fullyMatched,
                    index,
                    CreateResult(
                        graphical,
                        scenario,
                        DateTime.UtcNow.AddMinutes(1 + index),
                        "COMPLETE-MEASURED",
                        graphical ? BuildRequiredGraphicalMetrics(false) : BuildRequiredServerMetrics(false)),
                    job.job_id);
            }
            WriteCampaignManifest(fullyMatchedDirectory, fullyMatched);

            var matchedReport = CompareCampaigns(baselineID, fullyMatchedID, 1);
            Assert.That(matchedReport.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.COMPARABLE));
            Assert.That(matchedReport.Groups.All(group => group.Classification == Test_PerformanceCampaignGroupClassification.MATCHED), Is.True);
        }

        [Test]
        public void ComparatorExcludesForgedCompleteAttemptBeforeMetricQualification()
        {
            var baselineID = "campaign-lifecycle-baseline-" + Guid.NewGuid().ToString("N");
            var candidateID = "campaign-lifecycle-candidate-" + Guid.NewGuid().ToString("N");
            var baselineDirectory = CreateCampaignDirectory(baselineID);
            var candidateDirectory = CreateCampaignDirectory(candidateID);
            _createdCampaignDirectories.Add(baselineDirectory);
            _createdCampaignDirectories.Add(candidateDirectory);

            var baseline = BuildGraphicalCampaign(baselineID, 1);
            var candidate = BuildGraphicalCampaign(candidateID, 1);
            var baselineResult = CreateResult(true, "Outdoor", DateTime.UtcNow.AddMinutes(-2), "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false));
            var candidateResult = CreateResult(true, "Outdoor", DateTime.UtcNow.AddMinutes(-1), "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false));
            var candidateSummaryPath = Path.Combine(candidateResult, "summary.json");
            File.WriteAllText(candidateSummaryPath, File.ReadAllText(candidateSummaryPath).Replace(
                "\"status\":\"pass\"",
                "\"status\":\"failed\"",
                StringComparison.Ordinal));
            AddCompletedAttempt(baseline, 0, baselineResult);
            AddCompletedAttempt(candidate, 0, candidateResult);
            WriteCampaignManifest(baselineDirectory, baseline);
            WriteCampaignManifest(candidateDirectory, candidate);

            var report = CompareCampaigns(baselineID, candidateID, 1);
            var group = report.Groups.Single(group => group.ProfileID == "P1-S0-06");

            Assert.That(report.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.PROVISIONAL));
            Assert.That(group.BaselineValidCount, Is.EqualTo(1));
            Assert.That(group.CandidateValidCount, Is.EqualTo(0));
            Assert.That(group.Metrics.All(metric => metric.CandidateQualified == 0), Is.True);
            Assert.That(group.CandidateAttempts[0].IsValid, Is.False);
            Assert.That(group.CandidateAttempts[0].Diagnostic, Does.Contain("correctness/lifecycle"));
        }

        [Test]
        public void LinkedMakeupCohortPoolsEquivalentRunsAndPreservesExclusions()
        {
            var rootID = "campaign-linked-root-" + Guid.NewGuid().ToString("N");
            var makeupID = "campaign-linked-makeup-" + Guid.NewGuid().ToString("N");
            var rootDirectory = CreateCampaignDirectory(rootID);
            var makeupDirectory = CreateCampaignDirectory(makeupID);
            _createdCampaignDirectories.Add(rootDirectory);
            _createdCampaignDirectories.Add(makeupDirectory);

            var root = BuildGraphicalCampaign(rootID, 4);
            var makeup = BuildGraphicalCampaign(makeupID, 1);
            makeup.linked_campaign_id = rootID;
            makeup.makeup_for_job_id = "job-graphical";
            for (var index = 0; index < 4; index++)
            {
                var result = CreateResult(true, "Outdoor", DateTime.UtcNow.AddMinutes(-10 + index), "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false));
                AddCompletedAttempt(root, index, result);
            }
            var makeupResult = CreateResult(true, "Outdoor", DateTime.UtcNow.AddMinutes(-2), "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false));
            AddCompletedAttempt(makeup, 0, makeupResult);
            makeup.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = makeupID + "-duplicate",
                job_id = "job-graphical",
                repeat_index = 2,
                state = Test_PerformanceCampaignAttemptState.COMPLETED,
                run_id = root.attempts[0].run_id,
                result_path = root.attempts[0].result_path,
                report_path = root.attempts[0].report_path,
                result_status = "COMPLETE-MEASURED",
            });
            makeup.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = makeupID + "-failed",
                job_id = "job-graphical",
                repeat_index = 3,
                state = Test_PerformanceCampaignAttemptState.FAILED,
                result_status = "FAIL-TIMEOUT",
                diagnostic = "test failure retained in inventory",
            });
            WriteCampaignManifest(rootDirectory, root);
            WriteCampaignManifest(makeupDirectory, makeup);

            var archives = Test_PerformanceCampaignComparator.LoadArchives(out var diagnostic);
            Assert.That(diagnostic, Is.Null.Or.Empty);
            var report = Test_PerformanceCampaignComparator.BuildLinkedQualificationCohort(archives, makeupID);
            var group = report.Groups.Single();

            Assert.That(report.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.COMPARABLE));
            Assert.That(group.BaselineAttempts, Has.Count.EqualTo(7));
            Assert.That(group.BaselineValidCount, Is.EqualTo(5));
            Assert.That(group.Metrics.All(metric => metric.BaselineQualified == 5), Is.True);
            Assert.That(group.BaselineAttempts.Any(attempt => !attempt.IsValid && (attempt.Diagnostic ?? string.Empty).Contains("duplicate run ID")), Is.True);
            Assert.That(group.BaselineAttempts.Any(attempt => !attempt.IsValid && attempt.State == Test_PerformanceCampaignAttemptState.FAILED), Is.True);
            Assert.That(group.BaselineAttempts.Where(attempt => attempt.IsValid).Select(attempt => attempt.CampaignID).Distinct().Count(), Is.EqualTo(2));
            Assert.That(report.Summary, Does.Contain("duplicate"));
        }

        [Test]
        public void LinkedMakeupWithChangedCommitIsExcludedAsIncompatible()
        {
            var rootID = "campaign-linked-incompatible-root-" + Guid.NewGuid().ToString("N");
            var makeupID = "campaign-linked-incompatible-makeup-" + Guid.NewGuid().ToString("N");
            var rootDirectory = CreateCampaignDirectory(rootID);
            var makeupDirectory = CreateCampaignDirectory(makeupID);
            _createdCampaignDirectories.Add(rootDirectory);
            _createdCampaignDirectories.Add(makeupDirectory);

            var root = BuildGraphicalCampaign(rootID, 4);
            var makeup = BuildGraphicalCampaign(makeupID, 1);
            makeup.linked_campaign_id = rootID;
            makeup.makeup_for_job_id = "job-graphical";
            makeup.source.commit_sha = "changed-commit";
            for (var index = 0; index < 4; index++)
                AddCompletedAttempt(root, index, CreateResult(true, "Outdoor", DateTime.UtcNow.AddMinutes(-10 + index), "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false)));
            AddCompletedAttempt(makeup, 0, CreateResult(true, "Outdoor", DateTime.UtcNow, "COMPLETE-MEASURED", BuildRequiredGraphicalMetrics(false)));
            WriteCampaignManifest(rootDirectory, root);
            WriteCampaignManifest(makeupDirectory, makeup);

            var archives = Test_PerformanceCampaignComparator.LoadArchives(out var diagnostic);
            Assert.That(diagnostic, Is.Null.Or.Empty);
            var report = Test_PerformanceCampaignComparator.BuildLinkedQualificationCohort(archives, makeupID);
            var group = report.Groups.Single();

            Assert.That(report.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.NOT_COMPARABLE));
            Assert.That(group.BaselineValidCount, Is.EqualTo(4));
            Assert.That(group.Reason, Does.Contain("commit"));
            Assert.That(group.BaselineAttempts.Any(attempt => !attempt.IsValid && attempt.RunID == Path.GetFileName(makeup.attempts[0].result_path)), Is.True);
        }

        [Test]
        public void ReaderPreservesUnitsCoverageZerosAndUnavailablePercentiles()
        {
            const string metrics = "["
                + "{\"metric_id\":\"server.ecs.actor_update_duration\",\"unit\":\"ms\",\"observation\":\"unavailable\",\"phase\":\"measurement\",\"sample_count\":0,\"attempted_sample_count\":125,\"retained_sample_count\":100,\"dropped_sample_count\":25,\"min\":null,\"max\":null,\"mean\":null,\"p50\":null,\"p95\":null,\"p99\":null,\"measurement_coverage\":0.8,\"reason\":\"measurement_sample_coverage_below_policy\",\"retention_policy\":\"phase_reserved_reservoir\"},"
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
            Assert.That(measured.SampleCount, Is.EqualTo(0));
            Assert.That(measured.RetainedSampleCount, Is.EqualTo(100));
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
        public void ReaderAcceptsCanonicalTelemetryUnitsAndRejectsUnsupportedUnits()
        {
            const string metrics = "["
                + "{\"metric_id\":\"test.unit.milliseconds\",\"unit\":\"ms\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"test.unit.bytes\",\"unit\":\"bytes\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"test.unit.count\",\"unit\":\"count\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"test.unit.ticks\",\"unit\":\"ticks\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"harness.graphical.event_rate\",\"unit\":\"events/s\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"test.unit.ratio\",\"unit\":\"ratio\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"},"
                + "{\"metric_id\":\"harness.graphical.route_distance\",\"unit\":\"m\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":1,\"attempted_sample_count\":1,\"dropped_sample_count\":0,\"min\":1,\"max\":1,\"mean\":1,\"p50\":1,\"p95\":1,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"}"
                + "]";
            var directory = CreateResult(
                graphical: true,
                scenario: "Forest",
                finishedUtc: DateTime.UtcNow.AddMinutes(6),
                status: "FAIL-INSTRUMENTATION",
                metrics: metrics);

            var view = Test_PerformanceResultReader.ReadFinalized(directory);

            Assert.That(view.IsVerified, Is.True, view.ErrorMessage);

            var unsupportedDirectory = CreateResult(
                graphical: true,
                scenario: "Forest",
                finishedUtc: DateTime.UtcNow.AddMinutes(7),
                status: "FAIL-INSTRUMENTATION",
                metrics: metrics.Replace("\"unit\":\"m\"", "\"unit\":\"furlongs\"", StringComparison.Ordinal));
            var unsupported = Test_PerformanceResultReader.ReadFinalized(unsupportedDirectory);

            Assert.That(unsupported.IsVerified, Is.False);
            Assert.That(unsupported.ErrorMessage, Does.Contain("unknown metric unit"));
        }

        [Test]
        public void ReaderPreservesHistoricalReservoirCoverageAndRejectsForgedCurrentCoverage()
        {
            var directory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow,
                status: "COMPLETE-MEASURED",
                metrics: "["
                    + LegacyReservoirMetricJson("server.actor_manager.update_duration", "ms", "server_update", 384, 1000, 4d, 5d, 6d)
                    + ","
                    + MetricJsonWithSource("server.ecs.actor_update_duration", "server", "ms", "server_update", 4d, 5d, 6d)
                    + "]");

            var view = Test_PerformanceResultReader.ReadFinalized(directory);
            var legacy = FindMetric(view, "server.actor_manager.update_duration");
            var unaffected = FindMetric(view, "server.ecs.actor_update_duration");

            Assert.That(view.IsVerified, Is.True, view.ErrorMessage);
            Assert.That(view.Status, Is.EqualTo("COMPLETE-MEASURED"));
            Assert.That(legacy.MeasurementCoverage, Is.EqualTo(0.384d).Within(0.0000001d));
            Assert.That(legacy.OriginalMeasurementCoverage, Is.EqualTo(1d));
            Assert.That(legacy.CoverageDiagnostic, Does.Contain("legacy_coverage_semantics"));
            Assert.That(legacy.DisplayValue, Does.Contain("exported measurement_coverage=1"));
            Assert.That(legacy.DisplayValue, Does.Contain("effective retained coverage=0.384"));
            Assert.That(unaffected.MeasurementCoverage, Is.EqualTo(1d));
            Assert.That(unaffected.CoverageDiagnostic, Is.Null.Or.Empty);

            var forgedDirectory = CreateResult(
                graphical: false,
                scenario: "Outdoor",
                finishedUtc: DateTime.UtcNow.AddMinutes(1),
                status: "COMPLETE-MEASURED",
                metrics: "[" + CurrentMetricJsonWithForgedCoverage("server.actor_manager.update_duration", "server_update") + "]");
            var forged = Test_PerformanceResultReader.ReadFinalized(forgedDirectory);

            Assert.That(forged.IsVerified, Is.False);
            Assert.That(forged.ErrorMessage, Does.Contain("measurement coverage does not match retained/attempted samples"));
        }

        [Test]
        public void ComparatorUsesEffectiveHistoricalRetentionForQualification()
        {
            var baselineID = "campaign-legacy-baseline-" + Guid.NewGuid().ToString("N");
            var candidateID = "campaign-legacy-candidate-" + Guid.NewGuid().ToString("N");
            var baselineDirectory = CreateCampaignDirectory(baselineID);
            var candidateDirectory = CreateCampaignDirectory(candidateID);
            _createdCampaignDirectories.Add(baselineDirectory);
            _createdCampaignDirectories.Add(candidateDirectory);

            var baseline = BuildServerCampaign(baselineID, 1);
            var candidate = BuildServerCampaign(candidateID, 1);
            AddCompletedAttempt(
                baseline,
                0,
                CreateResult(false, "Outdoor", DateTime.UtcNow.AddMinutes(-1), "COMPLETE-MEASURED", BuildRequiredServerMetrics(true)),
                "job-server");
            AddCompletedAttempt(
                candidate,
                0,
                CreateResult(false, "Outdoor", DateTime.UtcNow, "COMPLETE-MEASURED", BuildRequiredServerMetrics(true)),
                "job-server");
            WriteCampaignManifest(baselineDirectory, baseline);
            WriteCampaignManifest(candidateDirectory, candidate);

            var report = CompareCampaigns(baselineID, candidateID, 1);
            var group = report.Groups.Single();
            var legacyMetric = group.Metrics.Single(metric => metric.MetricID == "server.actor_manager.update_duration");
            var unaffectedMetric = group.Metrics.Single(metric => metric.MetricID == "server.ecs.actor_update_duration");

            Assert.That(report.Status, Is.EqualTo(Test_PerformanceCampaignComparisonStatus.PROVISIONAL));
            Assert.That(group.CandidateAttempts.Single().IsValid, Is.True);
            Assert.That(legacyMetric.CandidateQualified, Is.EqualTo(0));
            Assert.That(unaffectedMetric.CandidateQualified, Is.EqualTo(1));
            Assert.That(legacyMetric.Caveat, Does.Contain("legacy_coverage_semantics"));
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
            Assert.That(sourceDifference, Is.Null, "Dirty and diff-only changes are informational and must not pause a campaign.");
            changedSource.commit_sha = "commit-b";
            Assert.That(source.DescribeDifference(changedSource), Does.Contain("commit SHA"));
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
            Assert.That(Test_PerformanceJobQueue.ProfileCoverage, Has.Count.EqualTo(8));
            for (var index = 0; index < Test_PerformanceJobQueue.ProfileCoverage.Count; index++)
            {
                var profile = Test_PerformanceJobQueue.ProfileCoverage[index];
                Assert.That(profile.profile_id, Is.EqualTo("P1-S0-0" + (index + 1)));
                Assert.That(profile.status, Is.EqualTo(index == 3 || index == 5 ? "SUPPORTED-NOT-RUN" : "BLOCKED"));
                Assert.That(profile.capability, Is.Not.Null.And.Not.Empty);
                Assert.That(profile.runnable_alternative, Is.Not.Null.And.Not.Empty);
                Assert.That(profile.next_issue_or_stage, Is.Not.Null.And.Not.Empty);
            }
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

        private string CreateCampaignDirectory(string campaignID)
        {
            var directory = GetCampaignDirectory(campaignID);
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static Test_PerformanceCampaignManifest BuildGraphicalCampaign(string campaignID, int repeatCount)
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                GraphicalScenario = Test_GraphicalScenario.Outdoor,
            };
            return new Test_PerformanceCampaignManifest
            {
                campaign_id = campaignID,
                state = Test_PerformanceCampaignState.COMPLETED,
                created_utc = DateTime.UtcNow.AddMinutes(-30).ToString("o"),
                updated_utc = DateTime.UtcNow.ToString("o"),
                finished_utc = DateTime.UtcNow.ToString("o"),
                source = BuildComparableSource(),
                jobs = new List<Test_PerformanceCampaignJob>
                {
                    new Test_PerformanceCampaignJob
                    {
                        job_id = "job-graphical",
                        options = options,
                        repeat_count = repeatCount,
                        validation_state = "valid",
                        fixture_identity = new Test_PerformanceCampaignFixtureIdentity
                        {
                            configuration_sha256 = "graphical-fixture-identity",
                            profile_id = "P1-S0-06",
                            profile_revision = "1.0.0",
                            actor_variant_id = "P1-S0-06-I-GEOMETRY",
                        },
                    },
                },
            };
        }

        private static Test_PerformanceCampaignManifest BuildFiveScenarioCampaign(string campaignID, int repeatCount)
        {
            var jobs = new List<Test_PerformanceCampaignJob>
            {
                BuildCampaignJob(
                    "job-server-minimal",
                    new Test_PerformanceHarnessOptions
                    {
                        WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal,
                    },
                    "server-minimal-fixture-identity"),
                BuildCampaignJob(
                    "job-server-activity",
                    new Test_PerformanceHarnessOptions
                    {
                        WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
                    },
                    "server-activity-fixture-identity"),
                BuildCampaignJob(
                    "job-graphical-outdoor",
                    new Test_PerformanceHarnessOptions
                    {
                        WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                        GraphicalScenario = Test_GraphicalScenario.Outdoor,
                    },
                    "graphical-fixture-identity"),
                BuildCampaignJob(
                    "job-graphical-forest",
                    new Test_PerformanceHarnessOptions
                    {
                        WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                        GraphicalScenario = Test_GraphicalScenario.Forest,
                    },
                    "graphical-forest-fixture-identity"),
                BuildCampaignJob(
                    "job-graphical-settlement",
                    new Test_PerformanceHarnessOptions
                    {
                        WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                        GraphicalScenario = Test_GraphicalScenario.Settlement,
                    },
                    "graphical-settlement-fixture-identity"),
            };
            foreach (var job in jobs)
                job.repeat_count = repeatCount;
            return new Test_PerformanceCampaignManifest
            {
                campaign_id = campaignID,
                state = Test_PerformanceCampaignState.COMPLETED,
                created_utc = DateTime.UtcNow.AddMinutes(-30).ToString("o"),
                updated_utc = DateTime.UtcNow.ToString("o"),
                finished_utc = DateTime.UtcNow.ToString("o"),
                source = BuildComparableSource(),
                jobs = jobs,
            };
        }

        private static Test_PerformanceCampaignJob BuildCampaignJob(
            string jobID,
            Test_PerformanceHarnessOptions options,
            string configurationIdentity)
        {
            return new Test_PerformanceCampaignJob
            {
                job_id = jobID,
                options = options,
                repeat_count = 1,
                validation_state = "valid",
                fixture_identity = new Test_PerformanceCampaignFixtureIdentity
                {
                    configuration_sha256 = configurationIdentity,
                    profile_id = options.EffectiveProfileID,
                    profile_revision = "1.0.0",
                    actor_variant_id = options.ActorVariantID,
                },
            };
        }

        private static Test_PerformanceCampaignManifest BuildServerCampaign(string campaignID, int repeatCount)
        {
            var job = BuildCampaignJob(
                "job-server",
                new Test_PerformanceHarnessOptions
                {
                    WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal,
                },
                "server-fixture-identity");
            job.repeat_count = repeatCount;
            return new Test_PerformanceCampaignManifest
            {
                campaign_id = campaignID,
                state = Test_PerformanceCampaignState.COMPLETED,
                created_utc = DateTime.UtcNow.AddMinutes(-30).ToString("o"),
                updated_utc = DateTime.UtcNow.ToString("o"),
                finished_utc = DateTime.UtcNow.ToString("o"),
                source = BuildComparableSource(),
                jobs = new List<Test_PerformanceCampaignJob>
                {
                    job,
                },
            };
        }

        private static Test_PerformanceCampaignSource BuildComparableSource()
        {
            return new Test_PerformanceCampaignSource
            {
                git_branch = "dev",
                commit_sha = "test-commit",
                source_sha256 = "source-sha",
                dirty = false,
                diff_sha256 = "diff-sha",
                packages_sha256 = "packages-sha",
                packages_lock_sha256 = "packages-lock-sha",
                purrnet_sha256 = "purrnet-sha",
                hardware = "test-hardware",
                operating_system = "test-os",
                unity_version = "test-unity",
                graphics_device = "test-gpu",
                graphics_settings = "test-settings",
            };
        }

        private static void AddCompletedAttempt(
            Test_PerformanceCampaignManifest manifest,
            int repeatIndex,
            string resultDirectory,
            string jobID = "job-graphical")
        {
            var runID = Path.GetFileName(resultDirectory);
            manifest.attempts.Add(new Test_PerformanceCampaignAttempt
            {
                attempt_id = manifest.campaign_id + "-attempt-" + repeatIndex,
                job_id = jobID,
                repeat_index = repeatIndex + 1,
                state = Test_PerformanceCampaignAttemptState.COMPLETED,
                run_id = runID,
                result_path = resultDirectory,
                report_path = Path.Combine(resultDirectory, "report.md"),
                result_status = "COMPLETE-MEASURED",
                stop_reason = "COMPLETE",
            });
        }

        private static void WriteCampaignManifest(string directory, Test_PerformanceCampaignManifest manifest)
        {
            File.WriteAllText(Path.Combine(directory, "campaign.json"), JsonUtility.ToJson(manifest, true));
        }

        private static Test_PerformanceCampaignComparisonReport CompareCampaigns(
            string baselineID,
            string candidateID,
            int minimumValidRepeats = Test_PerformanceCampaignComparator.DEFAULT_MIN_VALID_REPEATS)
        {
            var archives = Test_PerformanceCampaignComparator.LoadArchives(out var diagnostic);
            Assert.That(diagnostic, Is.Null.Or.Empty);
            return Test_PerformanceCampaignComparator.Compare(archives, baselineID, candidateID, minimumValidRepeats);
        }

        private static string BuildRequiredGraphicalMetrics(bool includeRejectedFrameMetric)
        {
            var metrics = new List<string>
            {
                includeRejectedFrameMetric
                    ? "{\"metric_id\":\"client.rendered.frame_time\",\"source\":\"client\",\"scope\":\"rendered_frame\",\"unit\":\"ms\",\"observation\":\"unavailable\",\"phase\":\"measurement\",\"sample_count\":0,\"retained_sample_count\":90,\"attempted_sample_count\":100,\"dropped_sample_count\":10,\"min\":null,\"max\":null,\"mean\":null,\"p50\":null,\"p95\":null,\"p99\":null,\"measurement_coverage\":0.9,\"reason\":\"measurement_sample_coverage_below_policy\",\"retention_policy\":\"phase_reserved_reservoir\"}"
                    : MetricJson("client.rendered.frame_time", "ms", "rendered_frame", 4d, 5d, 6d),
                MetricJson("client.main_thread.frame_time", "ms", "rendered_frame", 2d, 3d, 4d),
                MetricJson("client.render_thread.frame_time", "ms", "rendered_frame", 1d, 2d, 3d),
                MetricJson("client.gpu.frame_time", "ms", "rendered_frame", 1d, 1.5d, 2d),
                MetricJson("client.memory.total_allocated_bytes", "bytes", "client_frame", 100d, 100d, 100d),
                MetricJson("client.graphics.draw_call_count", "count", "rendered_frame", 26d, 26d, 26d),
                MetricJson("client.graphics.triangle_count", "count", "rendered_frame", 1930d, 1930d, 1930d),
                "{\"metric_id\":\"harness.graphical.route_distance\",\"source\":\"client\",\"scope\":\"graphical_route\",\"unit\":\"m\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":11,\"retained_sample_count\":11,\"attempted_sample_count\":11,\"dropped_sample_count\":0,\"min\":1,\"max\":11,\"mean\":6,\"p50\":6,\"p95\":11,\"p99\":null,\"measurement_coverage\":1,\"reason\":\"insufficient_samples\",\"retention_policy\":\"phase_reserved_exact\"}",
            };
            return "[" + string.Join(",", metrics) + "]";
        }

        private static string BuildRequiredServerMetrics(bool includeLegacyMetric)
        {
            var metrics = new List<string>
            {
                includeLegacyMetric
                    ? LegacyReservoirMetricJson("server.actor_manager.update_duration", "ms", "server_update", 4d, 5d, 6d)
                    : MetricJsonWithSource("server.actor_manager.update_duration", "server", "ms", "server_update", 4d, 5d, 6d),
                MetricJsonWithSource("server.ecs.actor_update_duration", "server", "ms", "server_update", 4d, 5d, 6d),
                MetricJsonWithSource("server.memory.total_allocated_bytes", "server", "bytes", "server_update", 100d, 100d, 100d),
                MetricJsonWithSource("server.actor.full_entity_scan_count", "server", "count", "server_frame", 26d, 26d, 26d),
            };
            return "[" + string.Join(",", metrics) + "]";
        }

        private static string MetricJson(string metricID, string unit, string scope, double p50, double p95, double p99)
        {
            return MetricJsonWithSource(metricID, "client", unit, scope, p50, p95, p99);
        }

        private static string MetricJsonWithSource(string metricID, string source, string unit, string scope, double p50, double p95, double p99)
        {
            return "{\"metric_id\":\"" + metricID + "\",\"source\":\"" + source + "\",\"scope\":\"" + scope + "\",\"unit\":\"" + unit + "\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":100,\"retained_sample_count\":100,\"attempted_sample_count\":100,\"dropped_sample_count\":0,\"min\":1,\"max\":100,\"mean\":50,\"p50\":" + p50.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"p95\":" + p95.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"p99\":" + p99.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"measurement_coverage\":1,\"reason\":\"\",\"retention_policy\":\"phase_reserved_exact\"}";
        }

        private static string LegacyReservoirMetricJson(string metricID, string unit, string scope, double p50, double p95, double p99)
        {
            return LegacyReservoirMetricJson(metricID, unit, scope, 384, 1000, p50, p95, p99);
        }

        private static string LegacyReservoirMetricJson(string metricID, string unit, string scope, int retained, int attempted, double p50, double p95, double p99)
        {
            var reservoirReplacementCount = attempted - retained;
            return "{\"metric_id\":\"" + metricID + "\",\"source\":\"server\",\"scope\":\"" + scope + "\",\"unit\":\"" + unit + "\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":" + retained + ",\"retained_sample_count\":" + retained + ",\"attempted_sample_count\":" + attempted + ",\"dropped_sample_count\":" + reservoirReplacementCount + ",\"rejected_sample_count\":0,\"reservoir_replacement_count\":" + reservoirReplacementCount + ",\"min\":1,\"max\":100,\"mean\":50,\"p50\":" + p50.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"p95\":" + p95.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"p99\":" + p99.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"measurement_coverage\":1,\"reason\":\"\",\"retention_policy\":\"phase_reserved_reservoir\"}";
        }

        private static string CurrentMetricJsonWithForgedCoverage(string metricID, string scope)
        {
            return "{\"metric_id\":\"" + metricID + "\",\"source\":\"server\",\"scope\":\"" + scope + "\",\"unit\":\"ms\",\"observation\":\"measured\",\"phase\":\"measurement\",\"sample_count\":100,\"retained_sample_count\":100,\"attempted_sample_count\":100,\"dropped_sample_count\":0,\"rejected_sample_count\":0,\"capacity_overflow_count\":0,\"reservoir_replacement_count\":0,\"valid_sample_coverage\":1,\"min\":1,\"max\":100,\"mean\":50,\"p50\":4,\"p95\":5,\"p99\":6,\"measurement_coverage\":0.9,\"reason\":\"\",\"retention_policy\":\"phase_reserved_reservoir\"}";
        }

        private string CreateResult(
            bool graphical,
            string scenario,
            DateTime finishedUtc,
            string status,
            string metrics,
            bool includeConflictingFixtureReferences = false)
        {
            var runID = "run-" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var directory = Path.Combine(GetResultRoot(), runID);
            Directory.CreateDirectory(directory);
            _createdResultDirectories.Add(directory);

            var fixtureJson = graphical
                ? $"{{\"schema_version\":\"benchmark-fixture/v1\",\"profile\":{{\"id\":\"P1-S0-06\",\"revision\":\"1.0.0\",\"variant\":\"S06-I-GEOMETRY\"}},\"random\":{{\"master_seed\":424242}},\"population\":{{\"npcs\":{{\"requested\":0}},\"accounting\":{{\"full_gameobject_purrnet_npcs\":0}}}},\"environment\":{{\"tile_archetype\":\"{scenario}\"}},\"schedules\":{{\"movement_routes\":[]}}}}"
                : "{\"schema_version\":\"benchmark-fixture/v1\",\"profile\":{\"id\":\"P1-S0-04\",\"revision\":\"1.0.0\",\"variant\":\"S04-I-DATA\"},\"random\":{\"master_seed\":424242},\"population\":{\"npcs\":{\"requested\":100},\"accounting\":{\"ecs_data_only_npcs\":100,\"full_gameobject_purrnet_npcs\":0}},\"environment\":{\"tile_archetype\":\"Outdoor\"},\"schedules\":{\"movement_routes\":[]}}";
            var fixturePath = Path.Combine(directory, "fixture.json");
            File.WriteAllText(fixturePath, fixtureJson);
            var fixtureHash = Test_PerformanceRunMetadata.HashFile(fixturePath);

            var conflictingFixture = includeConflictingFixtureReferences
                ? "\"fixture\":{\"path\":\"wrong.json\",\"sha256\":\"wrong\"},"
                : string.Empty;
            var conflictingFixtureReference = includeConflictingFixtureReferences
                ? ",\"fixture_ref\":{\"path\":\"wrong.json\",\"sha256\":\"wrong\"}"
                : string.Empty;
            var runJson = graphical
                ? $"{{\"schema_version\":\"benchmark-run/v1\",\"run_id\":\"{runID}\",\"created_utc\":\"{finishedUtc.AddSeconds(-1):o}\",{conflictingFixture}\"fixture_ref\":{{\"path\":\"fixture.json\",\"sha256\":\"{fixtureHash}\"}},\"git\":{{\"commit_sha\":\"test-commit\",\"dirty\":true}},\"build\":{{\"kind\":\"rendered_client\"}},\"render\":{{\"mode\":\"rendered\",\"graphical_client\":true}}}}"
                : $"{{\"schema_version\":\"benchmark-run/v1\",\"run_id\":\"{runID}\",\"created_utc\":\"{finishedUtc.AddSeconds(-1):o}\",\"fixture\":{{\"path\":\"fixture.json\",\"sha256\":\"{fixtureHash}\"}}{conflictingFixtureReference},\"git\":{{\"commit_sha\":\"test-commit\",\"dirty\":true}},\"build\":{{\"kind\":\"editor_playmode_server\"}},\"render\":{{\"mode\":\"server_only\",\"graphical_client\":false}},\"runtime\":{{\"scene_fixture\":\"Outdoor\"}}}}";
            File.WriteAllText(Path.Combine(directory, "run.json"), runJson);

            var achieved = graphical
                ? "{\"npc_actors\":0,\"ecs_entities\":0,\"graphical\":{\"requested_objects\":10,\"achieved_objects\":10,\"requested_actors\":0,\"achieved_actors\":0,\"route_waypoints_requested\":0,\"route_waypoints_reached\":0,\"route_complete\":true}}"
                : "{\"npc_actors\":0,\"ecs_entities\":100,\"population\":{\"requested_ecs_entities\":100,\"measurement_end_ecs_entities\":100,\"post_cleanup_fixture_entities\":0,\"post_cleanup_total_actor_entities\":0},\"activity\":{\"snapshot_captured\":false}}";
            var correctness = status == "COMPLETE-MEASURED" ? "pass" : "unverified";
            var complete = status == "COMPLETE-MEASURED";
            var summaryJson = $"{{\"schema_version\":\"benchmark-summary/v1\",\"run_id\":\"{runID}\",\"fixture_sha256\":\"{fixtureHash}\",\"status\":\"{status}\",\"started_utc\":\"{finishedUtc.AddSeconds(-2):o}\",\"finished_utc\":\"{finishedUtc:o}\",\"phase_completion\":{{\"preflight\":{complete.ToString().ToLowerInvariant()},\"ramp\":{complete.ToString().ToLowerInvariant()},\"warmup\":{complete.ToString().ToLowerInvariant()},\"measurement\":{complete.ToString().ToLowerInvariant()},\"recovery\":{complete.ToString().ToLowerInvariant()}}},\"achieved\":{achieved},\"stop\":{{\"reason_code\":\"TEST_STOP\",\"message\":\"test result\"}},\"correctness\":{{\"status\":\"{correctness}\",\"checked\":{(complete ? 4 : 0)},\"failed\":0,\"unverified\":[],\"cleanup\":{complete.ToString().ToLowerInvariant()}}},\"metrics\":{metrics},\"raw_files\":[]}}";
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
