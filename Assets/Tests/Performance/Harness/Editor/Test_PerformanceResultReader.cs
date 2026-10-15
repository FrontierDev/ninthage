using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Represents one displayable measurement phase state.</summary>
    internal sealed class Test_PerformancePhaseView
    {
        public Test_PerformancePhaseView(string name, bool complete)
        {
            Name = name;
            State = complete ? "complete" : "incomplete";
        }

        public string Name { get; }

        public string State { get; }
    }

    /// <summary>Represents one existing metric summary without recalculating telemetry.</summary>
    internal sealed class Test_PerformanceMetricView
    {
        public Test_PerformanceMetricView(
            string label,
            string metricID,
            string unit,
            string observation,
            int sampleCount,
            int attemptedSampleCount,
            int retainedSampleCount,
            int droppedSampleCount,
            double p50,
            double p95,
            double p99,
            double measurementCoverage,
            string reason,
            string retentionPolicy,
            bool present,
            double? originalMeasurementCoverage = null,
            string coverageDiagnostic = null)
        {
            Label = label;
            MetricID = metricID;
            Unit = unit;
            Observation = observation;
            SampleCount = sampleCount;
            AttemptedSampleCount = attemptedSampleCount;
            RetainedSampleCount = retainedSampleCount;
            DroppedSampleCount = droppedSampleCount;
            P50 = p50;
            P95 = p95;
            P99 = p99;
            MeasurementCoverage = measurementCoverage;
            OriginalMeasurementCoverage = originalMeasurementCoverage ?? measurementCoverage;
            CoverageDiagnostic = coverageDiagnostic;
            Reason = reason;
            RetentionPolicy = retentionPolicy;
            Present = present;
        }

        public string Label { get; }

        public string MetricID { get; }

        public string Unit { get; }

        public string Observation { get; }

        public int SampleCount { get; }

        public int AttemptedSampleCount { get; }

        public int RetainedSampleCount { get; }

        public int DroppedSampleCount { get; }

        public double P50 { get; }

        public double P95 { get; }

        public double P99 { get; }

        public double MeasurementCoverage { get; }

        public double OriginalMeasurementCoverage { get; }

        public string CoverageDiagnostic { get; }

        public string Reason { get; }

        public string RetentionPolicy { get; }

        public bool Present { get; }

        public bool IsMeasured => string.Equals(Observation, "measured", StringComparison.OrdinalIgnoreCase);

        public string DisplayValue
        {
            get
            {
                if (!Present)
                    return "not_collected";
                if (!IsMeasured)
                    return GetUnmeasuredState();

                var p99 = SampleCount < 100 || string.Equals(Reason, "insufficient_samples", StringComparison.OrdinalIgnoreCase)
                    ? "insufficient_samples"
                    : FormatNumber(P99);
                var coverage = AttemptedSampleCount > 0
                    ? $"coverage {MeasurementCoverage:P0}"
                    : "coverage unavailable";
                var warning = (AttemptedSampleCount > 0 && MeasurementCoverage < 1d)
                    ? $"; warning retained {RetainedSampleCount}/{AttemptedSampleCount}"
                    : string.Empty;
                var legacyWarning = string.IsNullOrEmpty(CoverageDiagnostic) ? string.Empty : "; " + CoverageDiagnostic;
                return $"samples {RetainedSampleCount}/{AttemptedSampleCount}; p50 {FormatNumber(P50)}; p95 {FormatNumber(P95)}; p99 {p99}; {coverage}; retention {RetentionPolicy}{warning}{legacyWarning} [{Unit}]";
            }
        }

        private string GetUnmeasuredState()
        {
            string state;
            if (string.Equals(Observation, "unavailable", StringComparison.OrdinalIgnoreCase))
                state = "unavailable";
            else if (string.Equals(RetentionPolicy, "not_collected", StringComparison.OrdinalIgnoreCase))
                state = "not_collected";
            else
                state = string.IsNullOrEmpty(Observation) ? "unverified" : Observation;
            return state + (string.IsNullOrEmpty(CoverageDiagnostic) ? string.Empty : "; " + CoverageDiagnostic) + " [" + Unit + "]";
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Groups existing measurement-phase metrics for the Editor results panel.</summary>
    internal sealed class Test_PerformanceMetricGroupView
    {
        public Test_PerformanceMetricGroupView(string title, List<Test_PerformanceMetricView> metrics)
        {
            Title = title;
            Metrics = metrics;
        }

        public string Title { get; }

        public List<Test_PerformanceMetricView> Metrics { get; }
    }

    /// <summary>Read-only view model for the latest finalized local performance attempt.</summary>
    internal sealed class Test_PerformanceLastRunView
    {
        private Test_PerformanceLastRunView()
        {
            Phases = new List<Test_PerformancePhaseView>();
            MetricGroups = new List<Test_PerformanceMetricGroupView>();
        }

        public bool HasAttempt { get; private set; }

        public bool IsVerified { get; private set; }

        public string ErrorMessage { get; private set; }

        public string ResultDirectory { get; private set; }

        public string ReportPath { get; private set; }

        public string RunID { get; private set; }

        public string Timestamp { get; private set; }

        public string Profile { get; private set; }

        public string Workload { get; private set; }

        public string Scenario { get; private set; }

        public string Seed { get; private set; }

        public string Commit { get; private set; }

        public string Branch { get; private set; }

        public string SourceCapturedUtc { get; private set; }

        public string DirtyState { get; private set; }

        public string Duration { get; private set; }

        public string Status { get; private set; }

        public string StopReason { get; private set; }

        public string StopMessage { get; private set; }

        public string Correctness { get; private set; }

        public string Cleanup { get; private set; }

        public string EvidenceScope { get; private set; }

        public string PurrNetStatus { get; private set; }

        public string PopulationSummary { get; private set; }

        public string ActivitySummary { get; private set; }

        public string RouteSummary { get; private set; }

        public List<Test_PerformancePhaseView> Phases { get; }

        public List<Test_PerformanceMetricGroupView> MetricGroups { get; }

        public bool CanOpenReport => !string.IsNullOrEmpty(ReportPath) && IsSafeExistingFile(ReportPath, ResultDirectory);

        public bool CanOpenFolder => !string.IsNullOrEmpty(ResultDirectory) && IsSafeExistingDirectory(ResultDirectory);

        public bool CanCopyRunID => !string.IsNullOrEmpty(RunID);

        public bool CanCopyResultPath => CanOpenFolder;

        public static Test_PerformanceLastRunView LoadLatest(string activeResultDirectory)
        {
            var view = new Test_PerformanceLastRunView();
            string resultRoot;
            try
            {
                resultRoot = GetResultRoot();
            }
            catch (Exception exception)
            {
                view.HasAttempt = true;
                view.ErrorMessage = "The project result root could not be resolved: " + exception.Message;
                return view;
            }

            if (!Directory.Exists(resultRoot))
                return view;
            if (!IsSafeExistingDirectory(resultRoot))
            {
                view.HasAttempt = true;
                view.ResultDirectory = resultRoot;
                view.ErrorMessage = "The benchmark result root is a reparse point and was not read.";
                return view;
            }

            var activeDirectory = NormalizePath(activeResultDirectory);
            Test_PerformanceResultCandidate latest = null;
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(resultRoot);
            }
            catch (Exception exception)
            {
                view.HasAttempt = true;
                view.ResultDirectory = resultRoot;
                view.ErrorMessage = "The benchmark result root could not be enumerated: " + exception.Message;
                return view;
            }

            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                if (!TryGetRunIdentityTimestamp(directoryName, out var identityTimestamp))
                    continue;

                var candidateDirectory = Path.GetFullPath(directory);
                if (!string.IsNullOrEmpty(activeDirectory)
                    && string.Equals(candidateDirectory, activeDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var candidate = BuildCandidate(candidateDirectory, directoryName, identityTimestamp);
                if (latest == null || candidate.CompareTo(latest) > 0)
                    latest = candidate;
            }

            if (latest == null)
                return view;
            return Read(latest.Directory, latest.RunID);
        }

        public static Test_PerformanceLastRunView ReadFinalized(string resultDirectory)
        {
            if (string.IsNullOrEmpty(resultDirectory))
            {
                return new Test_PerformanceLastRunView
                {
                    HasAttempt = true,
                    ErrorMessage = "The finalized result directory is unavailable.",
                };
            }

            return Read(resultDirectory, Path.GetFileName(resultDirectory));
        }

        private static Test_PerformanceResultCandidate BuildCandidate(string directory, string runID, DateTime identityTimestamp)
        {
            var candidate = new Test_PerformanceResultCandidate(directory, runID, identityTimestamp);
            if (!IsSafeExistingDirectory(directory))
                return candidate;
            try
            {
                var summaryPath = Path.Combine(directory, "summary.json");
                if (IsSafeExistingFile(summaryPath, directory))
                {
                    var summary = JsonUtility.FromJson<Test_PerformanceSummaryDocument>(File.ReadAllText(summaryPath));
                    if (summary != null && TryParseUtc(summary.finished_utc, out var finishedUtc))
                        candidate.Timestamp = finishedUtc;
                    else if (summary != null && TryParseUtc(summary.started_utc, out var startedUtc))
                        candidate.Timestamp = startedUtc;
                }

                if (candidate.Timestamp == identityTimestamp)
                {
                    var runPath = Path.Combine(directory, "run.json");
                    if (IsSafeExistingFile(runPath, directory))
                    {
                        var run = JsonUtility.FromJson<Test_PerformanceRunDocument>(File.ReadAllText(runPath));
                        if (run != null && TryParseUtc(run.created_utc, out var createdUtc))
                            candidate.Timestamp = createdUtc;
                    }
                }
            }
            catch
            {
                // Read will report the malformed attempt. The directory identity remains a deterministic sort key.
            }

            return candidate;
        }

        private static Test_PerformanceLastRunView Read(string directory, string directoryRunID)
        {
            var view = new Test_PerformanceLastRunView
            {
                HasAttempt = true,
                ResultDirectory = directory,
                RunID = directoryRunID,
                ReportPath = Path.Combine(directory, "report.md"),
            };

            try
            {
                var resultRoot = GetResultRoot();
                if (!IsSafeExistingDirectory(directory) || !IsWithin(directory, resultRoot))
                    throw new InvalidDataException("The result directory is outside the expected benchmark result root.");
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("The result directory is a reparse point and was not read.");

                var summaryPath = Path.Combine(directory, "summary.json");
                var runPath = Path.Combine(directory, "run.json");
                var fixturePath = Path.Combine(directory, "fixture.json");
                var artifactsPath = Path.Combine(directory, "artifacts.json");
                RequireFile(summaryPath, "summary.json");
                RequireFile(runPath, "run.json");
                RequireFile(fixturePath, "fixture.json");
                RequireFile(view.ReportPath, "report.md");
                RequireFile(artifactsPath, "artifacts.json");

                var summaryJson = File.ReadAllText(summaryPath);
                var summary = ParseJson<Test_PerformanceSummaryDocument>(summaryJson, "summary.json");
                var run = ReadJson<Test_PerformanceRunDocument>(runPath, "run.json");
                var fixture = ReadJson<Test_PerformanceFixtureDocument>(fixturePath, "fixture.json");
                var artifacts = ReadJson<Test_PerformanceArtifactsDocument>(artifactsPath, "artifacts.json");
                ValidateDocuments(directory, directoryRunID, summaryJson, summary, run, fixture, artifacts);
                Populate(view, directory, summaryJson, summary, run, fixture);
                view.IsVerified = true;
            }
            catch (Exception exception)
            {
                view.IsVerified = false;
                view.ErrorMessage = exception.Message;
            }

            return view;
        }

        private static void ValidateDocuments(
            string directory,
            string directoryRunID,
            string summaryJson,
            Test_PerformanceSummaryDocument summary,
            Test_PerformanceRunDocument run,
            Test_PerformanceFixtureDocument fixture,
            Test_PerformanceArtifactsDocument artifacts)
        {
            RequireEqual(summary.schema_version, "benchmark-summary/v1", "summary schema");
            RequireEqual(run.schema_version, "benchmark-run/v1", "run schema");
            RequireEqual(fixture.schema_version, "benchmark-fixture/v1", "fixture schema");
            RequireEqual(artifacts.schema_version, "benchmark-artifacts/v1", "artifact schema");
            RequireEqual(summary.run_id, directoryRunID, "summary run ID");
            RequireEqual(run.run_id, directoryRunID, "run run ID");
            RequireEqual(artifacts.run_id, directoryRunID, "artifact run ID");
            if (!IsKnownStatus(summary.status))
                throw new InvalidDataException("summary.json contains an unknown or missing final status.");
            if (summary.phase_completion == null || summary.achieved == null || summary.stop == null || summary.correctness == null)
                throw new InvalidDataException("summary.json is missing phase, achieved, stop or correctness data.");
            if (summary.metrics == null)
                throw new InvalidDataException("summary.json is missing its metrics array.");
            if (fixture.profile == null || fixture.random == null || fixture.population == null || fixture.population.npcs == null)
                throw new InvalidDataException("fixture.json is missing profile, seed or population data.");
            if (run.git == null || run.build == null || run.render == null)
                throw new InvalidDataException("run.json is missing source, build or render identity.");

            var expectedFixturePath = Path.Combine(directory, "fixture.json");
            var fixtureHash = Test_PerformanceRunMetadata.HashFile(expectedFixturePath);
            RequireEqual(summary.fixture_sha256, fixtureHash, "summary fixture digest");

            var fixtureReference = string.Equals(run.render.mode, "rendered", StringComparison.OrdinalIgnoreCase)
                ? run.fixture_ref
                : run.fixture;
            if (fixtureReference == null)
                throw new InvalidDataException("run.json is missing its fixture reference.");
            RequireRelativePath(fixtureReference.path, "run fixture reference");
            if (!string.Equals(fixtureReference.path, "fixture.json", StringComparison.Ordinal))
                throw new InvalidDataException("run.json points outside the finalized fixture artifact.");
            if (!string.IsNullOrEmpty(fixtureReference.sha256))
                RequireEqual(fixtureReference.sha256, fixtureHash, "run fixture digest");

            var requiredArtifacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fixture.json",
                "run.json",
                "summary.json",
                "report.md",
            };
            if (artifacts.files == null)
                throw new InvalidDataException("artifacts.json is missing its files array.");
            foreach (var artifact in artifacts.files)
            {
                if (artifact == null)
                    throw new InvalidDataException("artifacts.json contains a null file entry.");
                RequireRelativePath(artifact.path, "artifact path");
                var artifactPath = Path.GetFullPath(Path.Combine(directory, artifact.path));
                var absent = string.Equals(artifact.status, "absent", StringComparison.OrdinalIgnoreCase);
                if (absent)
                {
                    if (requiredArtifacts.Contains(artifact.path) || artifact.required)
                        throw new InvalidDataException("Required artifact is explicitly absent: " + artifact.path);
                    continue;
                }
                if (!IsSafeExistingFile(artifactPath, directory))
                    throw new InvalidDataException("Artifact path is missing or outside the result directory: " + artifact.path);
                if (!string.IsNullOrEmpty(artifact.sha256)
                    && !string.Equals(Test_PerformanceRunMetadata.HashFile(artifactPath), artifact.sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Artifact SHA-256 does not match the finalized file: " + artifact.path);
                }
                if ((!string.IsNullOrEmpty(artifact.sha256) || artifact.bytes > 0)
                    && artifact.bytes != new FileInfo(artifactPath).Length)
                    throw new InvalidDataException("Artifact byte count does not match the finalized file: " + artifact.path);
                requiredArtifacts.Remove(artifact.path);
            }
            if (requiredArtifacts.Count > 0)
                throw new InvalidDataException("artifacts.json is missing required entries: " + string.Join(", ", requiredArtifacts));

            foreach (var rawFile in summary.raw_files ?? Array.Empty<Test_PerformanceRawFileDocument>())
            {
                if (rawFile == null)
                    throw new InvalidDataException("summary.json contains a null raw file entry.");
                RequireRelativePath(rawFile.path, "summary raw file path");
                var rawFilePath = Path.GetFullPath(Path.Combine(directory, rawFile.path));
                if (!IsSafeExistingFile(rawFilePath, directory))
                    throw new InvalidDataException("summary.json references a missing raw file: " + rawFile.path);
            }

            var seenMetricIDs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var metric in summary.metrics)
            {
                if (metric == null || string.IsNullOrEmpty(metric.metric_id) || string.IsNullOrEmpty(metric.unit) || string.IsNullOrEmpty(metric.observation))
                    throw new InvalidDataException("summary.json contains a metric with missing identity, unit or observation.");
                if (!seenMetricIDs.Add(metric.metric_id))
                    throw new InvalidDataException("summary.json contains duplicate metric ID: " + metric.metric_id);
                if (metric.sample_count < 0 || metric.attempted_sample_count < 0 || metric.dropped_sample_count < 0)
                    throw new InvalidDataException("summary.json contains a negative metric sample count: " + metric.metric_id);
                if (metric.retained_sample_count < 0 || metric.rejected_sample_count < 0 || metric.capacity_overflow_count < 0 || metric.reservoir_replacement_count < 0)
                    throw new InvalidDataException("summary.json contains a negative metric retention count: " + metric.metric_id);
                var retainedSampleCount = metric.retained_sample_count > 0 ? metric.retained_sample_count : metric.sample_count;
                var metricJson = FindMetricJson(summaryJson, metric.metric_id);
                var hasCapacityOverflowCount = metricJson != null && HasJsonProperty(metricJson, "capacity_overflow_count");
                var hasLegacyCoverageSemantics = TryGetLegacyCoverageSemantics(
                    metricJson,
                    metric.retention_policy,
                    metric.attempted_sample_count,
                    retainedSampleCount,
                    metric.dropped_sample_count,
                    metric.rejected_sample_count,
                    metric.reservoir_replacement_count,
                    metric.measurement_coverage,
                    out var legacyDiagnostic);
                if (retainedSampleCount > metric.attempted_sample_count
                    || metric.rejected_sample_count > metric.attempted_sample_count
                    || retainedSampleCount + metric.rejected_sample_count > metric.attempted_sample_count
                    || (hasCapacityOverflowCount && (metric.capacity_overflow_count > metric.attempted_sample_count
                        || metric.capacity_overflow_count > metric.attempted_sample_count - metric.rejected_sample_count
                        || metric.reservoir_replacement_count > metric.capacity_overflow_count
                        || (long)metric.dropped_sample_count != metric.rejected_sample_count + (long)metric.capacity_overflow_count))
                    || (!hasCapacityOverflowCount
                        && metric.reservoir_replacement_count > 0
                        && !hasLegacyCoverageSemantics
                        && (retainedSampleCount >= metric.attempted_sample_count
                            || (long)metric.dropped_sample_count != metric.rejected_sample_count + (long)metric.reservoir_replacement_count)))
                {
                    throw new InvalidDataException("summary.json contains inconsistent metric retention counts: " + metric.metric_id);
                }
                if (!IsKnownObservation(metric.observation))
                    throw new InvalidDataException("summary.json contains an unknown metric observation: " + metric.metric_id);
                if (!string.Equals(metric.phase, "measurement", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("summary.json contains a metric outside the measurement phase: " + metric.metric_id);
                if (!IsKnownUnit(metric.unit))
                    throw new InvalidDataException("summary.json contains an unknown metric unit: " + metric.metric_id);
                if (!IsMeasuredObservation(metric.observation) && metric.sample_count != 0)
                    throw new InvalidDataException("summary.json reports samples for an unmeasured metric: " + metric.metric_id);
                if (metric.attempted_sample_count > 0)
                {
                    var retention = retainedSampleCount / (double)metric.attempted_sample_count;
                    metric.effective_measurement_coverage = retention;
                    if (hasLegacyCoverageSemantics)
                    {
                        metric.legacy_coverage_semantics = legacyDiagnostic;
                    }
                    else if (metricJson == null || IsJsonPropertyNull(metricJson, "measurement_coverage") || Math.Abs(metric.measurement_coverage - retention) > 0.0000001d)
                    {
                        throw new InvalidDataException("summary.json measurement coverage does not match retained/attempted samples: " + metric.metric_id);
                    }
                    if (HasJsonProperty(metricJson, "valid_sample_coverage")
                        && (IsJsonPropertyNull(metricJson, "valid_sample_coverage")
                            || Math.Abs(ReadJsonNumber(metricJson, "valid_sample_coverage")
                                - (metric.attempted_sample_count - metric.rejected_sample_count) / (double)metric.attempted_sample_count) > 0.0000001d))
                    {
                        throw new InvalidDataException("summary.json valid sample coverage does not match rejected/attempted samples: " + metric.metric_id);
                    }
                    if (string.IsNullOrEmpty(metric.legacy_coverage_semantics)
                        && IsMeasuredObservation(metric.observation)
                        && retention < 1d - MEASUREMENT_DROP_RATIO)
                        throw new InvalidDataException("summary.json reports a measured metric below the retention policy: " + metric.metric_id);
                }
                else
                {
                    metric.effective_measurement_coverage = 0d;
                }
                ValidateMetricNulls(summaryJson, metric);
            }
        }

        private const double MEASUREMENT_DROP_RATIO = 0.05d;

        internal static bool TryGetLegacyCoverageSemantics(
            string metricJson,
            string retentionPolicy,
            int attemptedSampleCount,
            int retainedSampleCount,
            int droppedSampleCount,
            int rejectedSampleCount,
            int reservoirReplacementCount,
            double exportedCoverage,
            out string diagnostic)
        {
            diagnostic = null;
            if (string.IsNullOrEmpty(metricJson)
                || !string.Equals(retentionPolicy, "phase_reserved_reservoir", StringComparison.Ordinal)
                || attemptedSampleCount <= 0
                || retainedSampleCount <= 0
                || retainedSampleCount >= attemptedSampleCount
                || droppedSampleCount < 0
                || reservoirReplacementCount <= 0
                || (!HasJsonProperty(metricJson, "retained_sample_count")
                    && !HasJsonProperty(metricJson, "sample_count"))
                || !HasJsonProperty(metricJson, "dropped_sample_count")
                || !HasJsonProperty(metricJson, "rejected_sample_count")
                || !HasJsonProperty(metricJson, "reservoir_replacement_count")
                || HasJsonProperty(metricJson, "capacity_overflow_count")
                || HasJsonProperty(metricJson, "valid_sample_coverage")
                || (long)droppedSampleCount != attemptedSampleCount - retainedSampleCount
                || (long)droppedSampleCount != rejectedSampleCount + (long)reservoirReplacementCount)
            {
                return false;
            }

            var validInputCoverage = Math.Max(0, attemptedSampleCount - rejectedSampleCount) / (double)attemptedSampleCount;
            if (Math.Abs(exportedCoverage - validInputCoverage) > 0.0000001d)
                return false;

            var effectiveRetention = retainedSampleCount / (double)attemptedSampleCount;
            diagnostic = string.Format(
                CultureInfo.InvariantCulture,
                "legacy_coverage_semantics: exported measurement_coverage={0:0.####} was valid-input coverage; effective retained coverage={1:0.####} ({2}/{3} retained/attempted), so this metric is excluded from qualification",
                exportedCoverage,
                effectiveRetention,
                retainedSampleCount,
                attemptedSampleCount);
            return true;
        }

        private static void ValidateMetricNulls(string summaryJson, Test_PerformanceMetricDocument metric)
        {
            var metricJson = FindMetricJson(summaryJson, metric.metric_id);
            if (metricJson == null)
                throw new InvalidDataException("summary.json metric could not be located in its metrics array: " + metric.metric_id);

            var measured = IsMeasuredObservation(metric.observation);
            var numericFields = new[] { "min", "max", "mean", "p50", "p95", "p99", "measurement_coverage" };
            foreach (var field in numericFields)
            {
                var isNull = IsJsonPropertyNull(metricJson, field);
                var mustBeNull = field == "measurement_coverage"
                    ? metric.attempted_sample_count == 0
                    : !measured || (field == "p99" && metric.sample_count < 100);
                if (mustBeNull != isNull)
                    throw new InvalidDataException($"summary.json has an invalid null/value state for {metric.metric_id}.{field}.");
            }
            if (HasJsonProperty(metricJson, "valid_sample_coverage"))
            {
                var isNull = IsJsonPropertyNull(metricJson, "valid_sample_coverage");
                if (isNull != (metric.attempted_sample_count == 0))
                    throw new InvalidDataException($"summary.json has an invalid null/value state for {metric.metric_id}.valid_sample_coverage.");
            }
        }

        private static string FindMetricJson(string summaryJson, string metricID)
        {
            const string property = "\"metric_id\"";
            var searchStart = 0;
            while (searchStart < summaryJson.Length)
            {
                var propertyIndex = summaryJson.IndexOf(property, searchStart, StringComparison.Ordinal);
                if (propertyIndex < 0)
                    return null;
                var valueStart = propertyIndex + property.Length;
                SkipJsonWhitespace(summaryJson, ref valueStart);
                if (valueStart >= summaryJson.Length || summaryJson[valueStart] != ':')
                    return null;
                valueStart++;
                SkipJsonWhitespace(summaryJson, ref valueStart);
                if (valueStart >= summaryJson.Length || summaryJson[valueStart] != '"')
                    return null;
                var valueEnd = summaryJson.IndexOf('"', valueStart + 1);
                if (valueEnd > valueStart && string.Equals(summaryJson.Substring(valueStart + 1, valueEnd - valueStart - 1), metricID, StringComparison.Ordinal))
                {
                    var objectStart = summaryJson.LastIndexOf('{', propertyIndex);
                    var objectEnd = summaryJson.IndexOf('}', valueEnd);
                    if (objectStart < 0 || objectEnd <= objectStart)
                        return null;
                    return summaryJson.Substring(objectStart, objectEnd - objectStart + 1);
                }
                searchStart = valueEnd > propertyIndex ? valueEnd + 1 : propertyIndex + property.Length;
            }
            return null;
        }

        private static bool IsJsonPropertyNull(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\"";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (propertyIndex < 0)
                throw new InvalidDataException("summary.json metric is missing property: " + propertyName);
            var valueStart = propertyIndex + marker.Length;
            SkipJsonWhitespace(json, ref valueStart);
            if (valueStart >= json.Length || json[valueStart] != ':')
                throw new InvalidDataException("summary.json metric has an invalid property: " + propertyName);
            valueStart++;
            SkipJsonWhitespace(json, ref valueStart);
            return json.Substring(valueStart).StartsWith("null", StringComparison.Ordinal);
        }

        private static bool HasJsonProperty(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\"";
            var searchStart = 0;
            while (searchStart < json.Length)
            {
                var propertyIndex = json.IndexOf(marker, searchStart, StringComparison.Ordinal);
                if (propertyIndex < 0)
                    return false;

                var valueStart = propertyIndex + marker.Length;
                SkipJsonWhitespace(json, ref valueStart);
                if (valueStart < json.Length && json[valueStart] == ':')
                    return true;
                searchStart = propertyIndex + marker.Length;
            }

            return false;
        }

        private static double ReadJsonNumber(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\"";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (propertyIndex < 0)
                throw new InvalidDataException("summary.json metric is missing property: " + propertyName);
            var valueStart = propertyIndex + marker.Length;
            SkipJsonWhitespace(json, ref valueStart);
            if (valueStart >= json.Length || json[valueStart] != ':')
                throw new InvalidDataException("summary.json metric has an invalid property: " + propertyName);
            valueStart++;
            SkipJsonWhitespace(json, ref valueStart);
            var valueEnd = valueStart;
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}')
                valueEnd++;
            if (!double.TryParse(json.Substring(valueStart, valueEnd - valueStart), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("summary.json metric has a non-numeric property: " + propertyName);
            return value;
        }

        private static void SkipJsonWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
        }

        private static void Populate(
            Test_PerformanceLastRunView view,
            string directory,
            string summaryJson,
            Test_PerformanceSummaryDocument summary,
            Test_PerformanceRunDocument run,
            Test_PerformanceFixtureDocument fixture)
        {
            var graphical = IsGraphicalExecution(run);
            var summaryGraphicalPropertyPresent = HasJsonProperty(summaryJson, "graphical")
                && !IsJsonPropertyNull(summaryJson, "graphical");
            if (graphical != summaryGraphicalPropertyPresent)
            {
                throw new InvalidDataException(
                    "summary.json and run.json disagree about graphical execution: run.render.mode='"
                    + (run.render.mode ?? "missing")
                    + "', run.render.graphical_client="
                    + run.render.graphical_client
                    + ", summary.achieved.graphical="
                    + summaryGraphicalPropertyPresent
                    + ".");
            }

            view.Timestamp = FormatTimestamp(summary.finished_utc, summary.started_utc, run.created_utc, view.RunID);
            view.Profile = fixture.profile.id + " / " + fixture.profile.revision;
            view.Workload = fixture.profile.variant;
            view.Scenario = BuildScenario(run, fixture, graphical);
            view.Seed = fixture.random.master_seed.ToString(CultureInfo.InvariantCulture);
            view.Commit = run.git.commit_sha;
            view.Branch = string.IsNullOrEmpty(run.git.branch) ? run.git.branch_status : run.git.branch;
            view.SourceCapturedUtc = run.git.captured_utc;
            view.DirtyState = run.git.dirty ? "dirty" : "clean";
            view.Duration = BuildDuration(summary.started_utc, summary.finished_utc);
            view.Status = summary.status;
            view.StopReason = summary.stop.reason_code;
            view.StopMessage = summary.stop.message;
            view.Correctness = summary.correctness.status;
            view.Cleanup = BuildCleanupState(summary, graphical);
            view.EvidenceScope = graphical
                ? "Editor-only rendered-client fixture data; not a standalone client or authenticated network result."
                : "Editor-only ECS data-only fixture; not standalone headless capacity or real actor/network coverage.";
            view.PopulationSummary = BuildPopulationSummary(summary, fixture, graphical);
            view.ActivitySummary = BuildActivitySummary(summary);
            view.RouteSummary = BuildRouteSummary(summary, graphical);
            view.PurrNetStatus = BuildPurrNetStatus(summary, graphical);
            view.ReportPath = Path.Combine(directory, "report.md");

            view.Phases.Add(new Test_PerformancePhaseView("Preflight", summary.phase_completion.preflight));
            view.Phases.Add(new Test_PerformancePhaseView("Ramp", summary.phase_completion.ramp));
            view.Phases.Add(new Test_PerformancePhaseView("Warmup", summary.phase_completion.warmup));
            view.Phases.Add(new Test_PerformancePhaseView("Measurement", summary.phase_completion.measurement));
            view.Phases.Add(new Test_PerformancePhaseView("Recovery", summary.phase_completion.recovery));

            foreach (var group in BuildMetricGroups(summary.metrics, graphical))
                view.MetricGroups.Add(group);
        }

        private static bool IsGraphicalExecution(Test_PerformanceRunDocument run)
        {
            if (string.Equals(run.render.mode, "rendered", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(run.render.mode, "server_only", StringComparison.OrdinalIgnoreCase))
                return false;
            throw new InvalidDataException("run.json contains an unsupported or missing render.mode: " + (run.render.mode ?? "missing") + ".");
        }

        private static List<Test_PerformanceMetricGroupView> BuildMetricGroups(Test_PerformanceMetricDocument[] metrics, bool graphical)
        {
            var groups = new List<Test_PerformanceMetricGroupView>();
            if (graphical)
            {
                groups.Add(BuildMetricGroup("Client frame CPU / GPU", metrics, new[]
                {
                    new Test_PerformanceMetricSpec("client.rendered.frame_time", "Rendered frame time", "ms"),
                    new Test_PerformanceMetricSpec("client.rendered.frame_interval", "Frame interval", "ms"),
                    new Test_PerformanceMetricSpec("client.main_thread.frame_time", "Main-thread frame time", "ms"),
                    new Test_PerformanceMetricSpec("client.render_thread.frame_time", "Render-thread frame time", "ms"),
                    new Test_PerformanceMetricSpec("client.gpu.frame_time", "GPU frame time", "ms"),
                    new Test_PerformanceMetricSpec("client.frame.spike_count", "Frame spikes", "count"),
                }));
                groups.Add(BuildMetricGroup("Rendering", metrics, new[]
                {
                    new Test_PerformanceMetricSpec("client.graphics.draw_call_count", "Draw calls", "count"),
                    new Test_PerformanceMetricSpec("client.graphics.batch_count", "Batches", "count"),
                    new Test_PerformanceMetricSpec("client.graphics.triangle_count", "Triangles", "count"),
                }));
                groups.Add(BuildMetricGroup("Memory / GC", metrics, new[]
                {
                    new Test_PerformanceMetricSpec("client.memory.total_allocated_bytes", "Total allocated memory", "bytes"),
                    new Test_PerformanceMetricSpec("client.memory.mono_used_bytes", "Mono used memory", "bytes"),
                    new Test_PerformanceMetricSpec("client.memory.mono_heap_bytes", "Mono heap", "bytes"),
                    new Test_PerformanceMetricSpec("client.memory.gc_allocated_bytes", "GC allocated per frame", "bytes"),
                }));
                groups.Add(BuildMetricGroup("Streaming / presentation", metrics, new[]
                {
                    new Test_PerformanceMetricSpec("client.scene.load_duration", "Scene load duration", "ms"),
                    new Test_PerformanceMetricSpec("client.scene.activation_duration", "Scene activation duration", "ms"),
                    new Test_PerformanceMetricSpec("client.scene.unload_callback_duration", "Scene unload callback", "ms"),
                    new Test_PerformanceMetricSpec("client.scene.tracked_count", "Tracked scenes", "count"),
                    new Test_PerformanceMetricSpec("client.actor.visible_count", "Visible real actors", "count"),
                    new Test_PerformanceMetricSpec("client.vfx.active_count", "Active VFX", "count"),
                }));
                groups.Add(BuildMetricGroup("PurrNet / headless coverage", metrics, new[]
                {
                    new Test_PerformanceMetricSpec("client.connection.authenticated_count", "Authenticated clients", "count"),
                    new Test_PerformanceMetricSpec("client.transport.payload_bytes_received", "PurrNet payload received", "bytes"),
                    new Test_PerformanceMetricSpec("server.connection.authenticated_client_count", "Server authenticated clients", "count"),
                    new Test_PerformanceMetricSpec("server.transport.payload_bytes_sent", "Server PurrNet payload sent", "bytes"),
                }));
                return groups;
            }

            groups.Add(BuildMetricGroup("Server ECS / actor CPU timing", metrics, new[]
            {
                new Test_PerformanceMetricSpec("server.ecs.actor_update_duration", "ECS actor update", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.world_spawn_duration", "ECS world spawn", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.resource_regen_duration", "ECS resource regeneration", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.spell_cast_duration", "ECS spell cast", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.cooldown_tick_duration", "ECS cooldown tick", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.aura_tick_duration", "ECS aura tick", "ms"),
                new Test_PerformanceMetricSpec("server.ecs.actor_ticker_duration", "ECS actor ticker", "ms"),
                new Test_PerformanceMetricSpec("server.actor_manager.update_duration", "Actor manager update", "ms"),
                new Test_PerformanceMetricSpec("server.actor_manager.late_update_duration", "Actor manager late update", "ms"),
                new Test_PerformanceMetricSpec("server.actor_manager.npc_aggro_duration", "NPC aggro", "ms"),
                new Test_PerformanceMetricSpec("server.actor_manager.player_ecs_sync_duration", "Player ECS sync", "ms"),
                new Test_PerformanceMetricSpec("server.auto_attack.update_duration", "Auto attack update", "ms"),
                new Test_PerformanceMetricSpec("server.spawn.update_duration", "Spawn manager update", "ms"),
            }));
            groups.Add(BuildMetricGroup("Memory / GC", metrics, new[]
            {
                new Test_PerformanceMetricSpec("server.memory.total_allocated_bytes", "Total allocated memory", "bytes"),
                new Test_PerformanceMetricSpec("server.memory.mono_used_bytes", "Mono used memory", "bytes"),
                new Test_PerformanceMetricSpec("server.memory.mono_heap_bytes", "Mono heap", "bytes"),
                new Test_PerformanceMetricSpec("server.memory.gc_gen0_collection_count", "GC Gen 0 collections", "count"),
                new Test_PerformanceMetricSpec("server.memory.gc_gen1_collection_count", "GC Gen 1 collections", "count"),
                new Test_PerformanceMetricSpec("server.memory.gc_gen2_collection_count", "GC Gen 2 collections", "count"),
            }));
            groups.Add(BuildMetricGroup("Scan / sync / activity", metrics, new[]
            {
                new Test_PerformanceMetricSpec("server.actor.full_entity_scan_count", "Full entity scans", "count"),
                new Test_PerformanceMetricSpec("server.actor.ecs_gameobject_sync_count", "ECS / GameObject sync", "count"),
                new Test_PerformanceMetricSpec("server.actor.npc_overlap_search_count", "NPC overlap searches", "count"),
                new Test_PerformanceMetricSpec("server.actor.collider_candidate_count", "Collider candidates", "count"),
                new Test_PerformanceMetricSpec("server.actor.alloc_heavy_operation_count", "Allocation-heavy operations", "count"),
            }));
            groups.Add(BuildMetricGroup("PurrNet / headless coverage", metrics, new[]
            {
                new Test_PerformanceMetricSpec("server.connection.authenticated_client_count", "Authenticated clients", "count"),
                new Test_PerformanceMetricSpec("server.connection.in_world_client_count", "In-world clients", "count"),
                new Test_PerformanceMetricSpec("server.transport.payload_bytes_sent", "PurrNet payload sent", "bytes"),
                new Test_PerformanceMetricSpec("network.visibility.evaluation_duration", "Visibility evaluation", "ms"),
                new Test_PerformanceMetricSpec("server.simulation.tick_backlog", "Simulation tick backlog", "count"),
            }));
            return groups;
        }

        private static Test_PerformanceMetricGroupView BuildMetricGroup(
            string title,
            Test_PerformanceMetricDocument[] metrics,
            Test_PerformanceMetricSpec[] specs)
        {
            var views = new List<Test_PerformanceMetricView>(specs.Length);
            foreach (var spec in specs)
            {
                Test_PerformanceMetricDocument metric = null;
                foreach (var candidate in metrics)
                {
                    if (candidate != null && string.Equals(candidate.metric_id, spec.MetricID, StringComparison.Ordinal))
                    {
                        metric = candidate;
                        break;
                    }
                }

                if (metric == null)
                {
                    views.Add(new Test_PerformanceMetricView(
                        spec.Label,
                        spec.MetricID,
                        spec.Unit,
                        "not_collected",
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        "Metric absent from summary.json.",
                        "not_collected",
                        false));
                    continue;
                }

                views.Add(new Test_PerformanceMetricView(
                    spec.Label,
                    metric.metric_id,
                    metric.unit,
                    metric.observation,
                    metric.sample_count,
                    metric.attempted_sample_count,
                    metric.retained_sample_count > 0 ? metric.retained_sample_count : metric.sample_count,
                    metric.dropped_sample_count,
                    metric.p50,
                    metric.p95,
                    metric.p99,
                    metric.effective_measurement_coverage,
                    metric.reason,
                    metric.retention_policy,
                    true,
                    metric.measurement_coverage,
                    metric.legacy_coverage_semantics));
            }
            return new Test_PerformanceMetricGroupView(title, views);
        }

        private static string BuildScenario(Test_PerformanceRunDocument run, Test_PerformanceFixtureDocument fixture, bool graphical)
        {
            if (!graphical)
                return run.runtime == null || string.IsNullOrEmpty(run.runtime.scene_fixture) ? "unavailable" : run.runtime.scene_fixture;
            if (fixture.environment != null && !string.IsNullOrEmpty(fixture.environment.tile_archetype))
                return fixture.environment.tile_archetype;
            if (fixture.schedules != null && fixture.schedules.movement_routes != null && fixture.schedules.movement_routes.Length > 0)
                return fixture.schedules.movement_routes[0].route_id;
            return "unavailable";
        }

        private static string BuildCleanupState(Test_PerformanceSummaryDocument summary, bool graphical)
        {
            if (!string.Equals(summary.correctness.status, "pass", StringComparison.OrdinalIgnoreCase))
                return string.Equals(summary.status, "BLOCKED-PREREQUISITE", StringComparison.OrdinalIgnoreCase) ? "unverified" : "failed";
            if (graphical)
                return "pass (reported by correctness)";
            return "pass (reported by correctness)";
        }

        private static string BuildPopulationSummary(Test_PerformanceSummaryDocument summary, Test_PerformanceFixtureDocument fixture, bool graphical)
        {
            if (graphical)
            {
                var graphicalPopulation = summary.achieved.graphical;
                if (graphicalPopulation == null)
                    return "Objects: unavailable; actors: unavailable";
                return $"Objects: requested {graphicalPopulation.requested_objects}, achieved {graphicalPopulation.achieved_objects}; "
                    + $"real actors: requested {graphicalPopulation.requested_actors}, achieved {graphicalPopulation.achieved_actors}";
            }

            var population = summary.achieved.population;
            if (population == null)
                return "ECS entities: unavailable";
            var result = $"ECS entities: requested {population.requested_ecs_entities}, achieved {summary.achieved.ecs_entities}";
            var accounting = fixture.population.accounting;
            if (accounting != null && accounting.full_gameobject_purrnet_npcs > 0)
                result += $"; real actors: requested {fixture.population.npcs.requested}, achieved {summary.achieved.npc_actors}";
            else
                result += "; real actors: not_applicable (ECS data-only)";
            return result;
        }

        private static string BuildActivitySummary(Test_PerformanceSummaryDocument summary)
        {
            var activity = summary.achieved.activity;
            if (activity == null || !activity.snapshot_captured)
                return "Activity distribution: unverified";
            return $"Activity: idle {activity.idle}, patrolling {activity.patrolling}, combat {activity.combat}; "
                + $"wake surge {activity.wake_surge}; churn spawn {activity.spawn_events}, despawn {activity.despawn_events}";
        }

        private static string BuildRouteSummary(Test_PerformanceSummaryDocument summary, bool graphical)
        {
            if (!graphical)
                return "Graphical route: not_applicable";
            var graphicalPopulation = summary.achieved.graphical;
            if (graphicalPopulation == null)
                return "Graphical route: not_applicable";
            var state = graphicalPopulation.route_complete ? "complete" : "incomplete";
            return $"Graphical route: {graphicalPopulation.route_waypoints_reached}/{graphicalPopulation.route_waypoints_requested} waypoints ({state})";
        }

        private static string BuildPurrNetStatus(Test_PerformanceSummaryDocument summary, bool graphical)
        {
            var ids = graphical
                ? new[] { "client.connection.authenticated_count", "client.transport.payload_bytes_received", "server.connection.authenticated_client_count", "server.transport.payload_bytes_sent" }
                : new[] { "server.connection.authenticated_client_count", "server.transport.payload_bytes_sent", "network.visibility.evaluation_duration", "server.simulation.tick_backlog" };
            var states = new List<string>();
            foreach (var id in ids)
            {
                foreach (var metric in summary.metrics)
                {
                    if (!string.Equals(metric.metric_id, id, StringComparison.Ordinal))
                        continue;
                    states.Add(metric.observation == "unavailable" && metric.retention_policy == "not_collected" ? "not_collected" : metric.observation);
                    break;
                }
            }
            return states.Count == 0 ? "not_collected" : string.Join(", ", states);
        }

        private static string BuildDuration(string startedUtc, string finishedUtc)
        {
            if (!TryParseUtc(startedUtc, out var started) || !TryParseUtc(finishedUtc, out var finished) || finished < started)
                return "unavailable";
            return (finished - started).TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture) + " s";
        }

        private static string FormatTimestamp(string finishedUtc, string startedUtc, string createdUtc, string runID)
        {
            if (TryParseUtc(finishedUtc, out var finished))
                return finished.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            if (TryParseUtc(startedUtc, out var started))
                return started.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            if (TryParseUtc(createdUtc, out var created))
                return created.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            if (TryGetRunIdentityTimestamp(runID, out var identity))
                return identity.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            return "unavailable";
        }

        private static bool TryParseUtc(string value, out DateTime timestamp)
        {
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp);
        }

        private static bool TryGetRunIdentityTimestamp(string runID, out DateTime timestamp)
        {
            timestamp = default;
            const string prefix = "run-";
            const string format = "yyyyMMdd'T'HHmmssfff'Z'";
            const int timestampLength = 19;
            if (string.IsNullOrEmpty(runID) || !runID.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var timestampStart = prefix.Length;
            var separatorIndex = timestampStart + timestampLength;
            if (runID.Length <= separatorIndex + 1 || runID[separatorIndex] != '-')
                return false;

            return DateTime.TryParseExact(
                runID.Substring(timestampStart, timestampLength),
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp);
        }

        private static string GetResultRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
                throw new DirectoryNotFoundException("The Unity project root could not be resolved from Application.dataPath.");
            return Path.GetFullPath(Path.Combine(projectRoot.FullName, Test_PerformanceRun.RESULT_ROOT));
        }

        private static T ReadJson<T>(string path, string label)
        {
            return ParseJson<T>(File.ReadAllText(path), label);
        }

        private static T ParseJson<T>(string json, string label)
        {
            try
            {
                var document = JsonUtility.FromJson<T>(json);
                if (document == null)
                    throw new InvalidDataException(label + " is empty or has no JSON object.");
                return document;
            }
            catch (Exception exception) when (!(exception is InvalidDataException))
            {
                throw new InvalidDataException(label + " is malformed: " + exception.Message);
            }
        }

        private static void RequireFile(string path, string label)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Finalized attempt is missing " + label + ".", path);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(label + " is a reparse point and was not read.");
        }

        private static void RequireEqual(string actual, string expected, string label)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"{label} mismatch: expected '{expected}', found '{actual ?? "missing"}'.");
        }

        private static void RequireRelativePath(string path, string label)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
                throw new InvalidDataException(label + " must be a non-empty relative path.");
            var segments = path.Replace('\\', '/').Split('/');
            foreach (var segment in segments)
            {
                if (segment == "..")
                    throw new InvalidDataException(label + " contains path traversal: " + path);
            }
        }

        private static bool IsWithin(string path, string root)
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsSafeExistingDirectory(string path)
        {
            return !string.IsNullOrEmpty(path)
                && Directory.Exists(path)
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }

        private static bool IsSafeExistingFile(string path, string root)
        {
            return File.Exists(path)
                && IsWithin(path, root)
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }

        private static bool IsKnownStatus(string status)
        {
            switch (status)
            {
                case "COMPLETE-MEASURED":
                case "BLOCKED-PREREQUISITE":
                case "FAIL-CORRECTNESS":
                case "FAIL-PROVISION":
                case "FAIL-RESOURCE":
                case "FAIL-TIMEOUT":
                case "FAIL-INSTRUMENTATION":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsKnownObservation(string observation)
        {
            return observation == "measured" || observation == "estimated" || observation == "unsupported" || observation == "unavailable" || observation == "not_applicable";
        }

        private static bool IsMeasuredObservation(string observation)
        {
            return observation == "measured" || observation == "estimated";
        }

        private static bool IsKnownUnit(string unit)
        {
            return unit == "ms"
                || unit == "bytes"
                || unit == "count"
                || unit == "ticks"
                || unit == "events/s"
                || unit == "ratio"
                || unit == "m";
        }

        private sealed class Test_PerformanceResultCandidate
        {
            public Test_PerformanceResultCandidate(string directory, string runID, DateTime timestamp)
            {
                Directory = directory;
                RunID = runID;
                Timestamp = timestamp;
            }

            public string Directory { get; }

            public string RunID { get; }

            public DateTime Timestamp { get; set; }

            public int CompareTo(Test_PerformanceResultCandidate other)
            {
                var timestampComparison = Timestamp.CompareTo(other.Timestamp);
                return timestampComparison != 0 ? timestampComparison : string.Compare(RunID, other.RunID, StringComparison.Ordinal);
            }
        }

        private sealed class Test_PerformanceMetricSpec
        {
            public Test_PerformanceMetricSpec(string metricID, string label, string unit)
            {
                MetricID = metricID;
                Label = label;
                Unit = unit;
            }

            public string MetricID { get; }

            public string Label { get; }

            public string Unit { get; }
        }

        #pragma warning disable 0649

        [Serializable]
        private sealed class Test_PerformanceSummaryDocument
        {
            public string schema_version;
            public string run_id;
            public string fixture_sha256;
            public string status;
            public string started_utc;
            public string finished_utc;
            public Test_PerformancePhaseCompletionDocument phase_completion;
            public Test_PerformanceAchievedDocument achieved;
            public Test_PerformanceStopDocument stop;
            public Test_PerformanceCorrectnessDocument correctness;
            public Test_PerformanceMetricDocument[] metrics;
            public Test_PerformanceRawFileDocument[] raw_files;
        }

        [Serializable]
        private sealed class Test_PerformanceRunDocument
        {
            public string schema_version;
            public string run_id;
            public string created_utc;
            public Test_PerformanceFileReferenceDocument fixture;
            public Test_PerformanceFileReferenceDocument fixture_ref;
            public Test_PerformanceGitDocument git;
            public Test_PerformanceBuildDocument build;
            public Test_PerformanceRenderDocument render;
            public Test_PerformanceRuntimeDocument runtime;
        }

        [Serializable]
        private sealed class Test_PerformanceFixtureDocument
        {
            public string schema_version;
            public Test_PerformanceProfileDocument profile;
            public Test_PerformanceRandomDocument random;
            public Test_PerformanceFixturePopulationDocument population;
            public Test_PerformanceFixtureEnvironmentDocument environment;
            public Test_PerformanceFixtureSchedulesDocument schedules;
        }

        [Serializable]
        private sealed class Test_PerformanceArtifactsDocument
        {
            public string schema_version;
            public string run_id;
            public Test_PerformanceArtifactDocument[] files;
        }

        [Serializable]
        private sealed class Test_PerformancePhaseCompletionDocument { public bool preflight; public bool ramp; public bool warmup; public bool measurement; public bool recovery; }

        [Serializable]
        private sealed class Test_PerformanceAchievedDocument
        {
            public int npc_actors;
            public int ecs_entities;
            public Test_PerformancePopulationDocument population;
            public Test_PerformanceActivityDocument activity;
            public Test_PerformanceGraphicalPopulationDocument graphical;
        }

        [Serializable]
        private sealed class Test_PerformancePopulationDocument
        {
            public int requested_ecs_entities;
            public int measurement_end_ecs_entities;
        }

        [Serializable]
        private sealed class Test_PerformanceGraphicalPopulationDocument
        {
            public int requested_objects;
            public int achieved_objects;
            public int requested_actors;
            public int achieved_actors;
            public int route_waypoints_requested;
            public int route_waypoints_reached;
            public bool route_complete;
        }

        [Serializable]
        private sealed class Test_PerformanceActivityDocument
        {
            public bool snapshot_captured;
            public int idle;
            public int patrolling;
            public int combat;
            public int wake_surge;
            public int spawn_events;
            public int despawn_events;
        }

        [Serializable]
        private sealed class Test_PerformanceStopDocument { public string reason_code; public string message; }

        [Serializable]
        private sealed class Test_PerformanceCorrectnessDocument { public string status; }

        [Serializable]
        private sealed class Test_PerformanceMetricDocument
        {
            public string metric_id;
            public string unit;
            public string observation;
            public string phase;
            public int sample_count;
            public int attempted_sample_count;
            public int retained_sample_count;
            public int dropped_sample_count;
            public int rejected_sample_count;
            public int capacity_overflow_count;
            public int reservoir_replacement_count;
            public double p50;
            public double p95;
            public double p99;
            public double measurement_coverage;
            public double effective_measurement_coverage;
            public string reason;
            public string retention_policy;
            public string legacy_coverage_semantics;
        }

        [Serializable]
        private sealed class Test_PerformanceRawFileDocument { public string path; }

        [Serializable]
        private sealed class Test_PerformanceFileReferenceDocument { public string path; public string sha256; }

        [Serializable]
        private sealed class Test_PerformanceGitDocument
        {
            public string branch;
            public string branch_status;
            public string branch_diagnostic;
            public string captured_utc;
            public string commit_sha;
            public bool dirty;
        }

        [Serializable]
        private sealed class Test_PerformanceBuildDocument { public string kind; }

        [Serializable]
        private sealed class Test_PerformanceRenderDocument { public string mode; public bool graphical_client; }

        [Serializable]
        private sealed class Test_PerformanceRuntimeDocument { public string scene_fixture; }

        [Serializable]
        private sealed class Test_PerformanceProfileDocument { public string id; public string revision; public string variant; }

        [Serializable]
        private sealed class Test_PerformanceRandomDocument { public int master_seed; }

        [Serializable]
        private sealed class Test_PerformanceFixturePopulationDocument
        {
            public Test_PerformanceFixtureNPCDocument npcs;
            public Test_PerformanceFixtureAccountingDocument accounting;
        }

        [Serializable]
        private sealed class Test_PerformanceFixtureNPCDocument { public int requested; }

        [Serializable]
        private sealed class Test_PerformanceFixtureAccountingDocument { public int full_gameobject_purrnet_npcs; }

        [Serializable]
        private sealed class Test_PerformanceFixtureEnvironmentDocument { public string tile_archetype; }

        [Serializable]
        private sealed class Test_PerformanceFixtureSchedulesDocument { public Test_PerformanceFixtureRouteDocument[] movement_routes; }

        [Serializable]
        private sealed class Test_PerformanceFixtureRouteDocument { public string route_id; }

        [Serializable]
        private sealed class Test_PerformanceArtifactDocument
        {
            public string path;
            public string status;
            public bool required;
            public bool optional;
            public string sha256;
            public long bytes;
        }

        #pragma warning restore 0649
    }

    internal static class Test_PerformanceResultReader
    {
        public static Test_PerformanceLastRunView LoadLatest(string activeResultDirectory)
        {
            return Test_PerformanceLastRunView.LoadLatest(activeResultDirectory);
        }

        public static Test_PerformanceLastRunView ReadFinalized(string resultDirectory)
        {
            return Test_PerformanceLastRunView.ReadFinalized(resultDirectory);
        }

        internal static bool TryGetLegacyCoverageSemantics(
            string metricJson,
            string retentionPolicy,
            int attemptedSampleCount,
            int retainedSampleCount,
            int droppedSampleCount,
            int rejectedSampleCount,
            int reservoirReplacementCount,
            double exportedCoverage,
            out string diagnostic)
        {
            return Test_PerformanceLastRunView.TryGetLegacyCoverageSemantics(
                metricJson,
                retentionPolicy,
                attemptedSampleCount,
                retainedSampleCount,
                droppedSampleCount,
                rejectedSampleCount,
                reservoirReplacementCount,
                exportedCoverage,
                out diagnostic);
        }
    }
}
