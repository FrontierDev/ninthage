using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    internal static class Test_PerformanceCampaignComparisonStatus
    {
        public const string COMPARABLE = "COMPARABLE";
        public const string PROVISIONAL = "PROVISIONAL/INSUFFICIENT REPEATS";
        public const string NOT_COMPARABLE = "NOT COMPARABLE";
        public const string ERROR = "ERROR";
    }

    internal static class Test_PerformanceCampaignGroupClassification
    {
        public const string MATCHED = "matched";
        public const string CANDIDATE_ONLY = "candidate only";
        public const string BASELINE_ONLY = "baseline only";
        public const string INCOMPATIBLE = "incompatible";
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignArchive
    {
        public string Directory;
        public string Diagnostic;
        public Test_PerformanceCampaignManifest Manifest;

        public bool IsValid => Manifest != null && string.IsNullOrEmpty(Diagnostic);
        public bool IsCompleted => IsValid && Manifest.state == Test_PerformanceCampaignState.COMPLETED;
        public string CampaignID => Manifest?.campaign_id ?? Path.GetFileName(Directory) ?? "invalid campaign";

        public string DisplayLabel
        {
            get
            {
                var completed = Manifest?.attempts == null ? 0 : Manifest.attempts.Count(a => a.state == Test_PerformanceCampaignAttemptState.COMPLETED);
                var attempted = Manifest?.attempts?.Count ?? 0;
                var source = Manifest?.source;
                var branch = source?.git_branch ?? "unknown/not recorded";
                var commit = source?.commit_sha;
                var shortCommit = string.IsNullOrEmpty(commit) ? "unknown" : commit.Substring(0, Math.Min(12, commit.Length));
                var dirty = source == null ? "unknown" : source.dirty ? "dirty" : "clean";
                return CampaignID + " / " + (Manifest?.state ?? "invalid") + " / " + branch + " @ " + shortCommit + " / " + dirty + " / " + completed + "/" + attempted + " completed";
            }
        }

        public string SourceDisplay
        {
            get
            {
                var source = Manifest?.source;
                if (source == null)
                    return "branch unknown/not recorded; commit unknown; dirty unknown";
                return "branch " + (source.git_branch ?? "unknown/not recorded")
                    + "; commit " + (source.commit_sha ?? "unknown")
                    + "; dirty " + source.dirty
                    + "; diff " + (source.diff_sha256 ?? "unknown")
                    + "; state " + (source.git_branch_status ?? "unknown")
                    + (string.IsNullOrEmpty(source.git_branch_diagnostic) ? string.Empty : "; diagnostic " + source.git_branch_diagnostic);
            }
        }

        public string EnvironmentDisplay
        {
            get
            {
                var source = Manifest?.source;
                if (source == null)
                    return "environment not recorded";
                return "hardware " + (source.hardware ?? "unknown")
                    + "; OS " + (source.operating_system ?? "unknown")
                    + "; Unity " + (source.unity_version ?? "unknown")
                    + "; graphics " + (source.graphics_device ?? "unknown")
                    + "; settings " + (source.graphics_settings ?? "unknown")
                    + "; packages " + (source.packages_sha256 ?? "unknown")
                    + "; PurrNet " + (source.purrnet_sha256 ?? "unknown");
            }
        }

        public string DateDisplay => "started " + (Manifest?.started_utc ?? Manifest?.created_utc ?? "unknown")
            + "; finished " + (Manifest?.finished_utc ?? "not finished");
    }

    internal sealed class Test_PerformanceCampaignComparisonReport
    {
        public Test_PerformanceCampaignArchive Baseline;
        public Test_PerformanceCampaignArchive Candidate;
        public string Status;
        public string Summary;
        public readonly List<string> Reasons = new();
        public readonly List<Test_PerformanceCampaignComparisonGroup> Groups = new();
    }

    internal sealed class Test_PerformanceCampaignComparisonGroup
    {
        public string Label;
        public string Classification;
        public string Reason;
        public int BaselineValidCount;
        public int CandidateValidCount;
        public readonly List<Test_PerformanceCampaignAttemptView> BaselineAttempts = new();
        public readonly List<Test_PerformanceCampaignAttemptView> CandidateAttempts = new();
        public readonly List<Test_PerformanceCampaignMetricComparison> Metrics = new();
    }

    internal sealed class Test_PerformanceCampaignAttemptView
    {
        public string AttemptID;
        public string RunID;
        public string State;
        public string ResultStatus;
        public string StopReason;
        public string Diagnostic;
        public string ResultPath;
        public string ReportPath;
        public string GroupKey;
        public string IdentityKey;
        public bool IsValid;
        public readonly List<Test_PerformanceCampaignMetricSample> Metrics = new();
    }

    internal sealed class Test_PerformanceCampaignMetricSample
    {
        public string MetricID;
        public string Unit;
        public string Source;
        public string Scope;
        public string RetentionPolicy;
        public string Observation;
        public string Reason;
        public int SampleCount;
        public bool HasP50;
        public bool HasP95;
        public bool HasP99;
        public double P50;
        public double P95;
        public double P99;
        public string Key => MetricID + "|" + Unit + "|" + Source + "|" + Scope + "|" + (RetentionPolicy ?? "unknown");
    }

    internal sealed class Test_PerformanceCampaignMetricComparison
    {
        public string MetricID;
        public string Unit;
        public string Source;
        public string Scope;
        public string SamplePolicy;
        public string Semantics;
        public string Direction;
        public int BaselineAvailable;
        public int CandidateAvailable;
        public int BaselineQualified;
        public int CandidateQualified;
        public Test_PerformanceCampaignDistribution Baseline;
        public Test_PerformanceCampaignDistribution Candidate;
        public double? DeltaP50;
        public double? DeltaP95;
        public double? DeltaP99;
        public double? DeltaP50Percent;
        public double? DeltaP95Percent;
        public double? DeltaP99Percent;
        public string Caveat;
    }

    internal sealed class Test_PerformanceCampaignDistribution
    {
        public int Count;
        public double MedianP50;
        public double MedianP95;
        public double? MedianP99;
        public double MinP50;
        public double MaxP50;
        public double IqrP50;

        public string Describe()
        {
            var p99 = MedianP99.HasValue ? MedianP99.Value.ToString("0.####", CultureInfo.InvariantCulture) : "unavailable";
            return "n " + Count + "; median run p50 " + MedianP50.ToString("0.####", CultureInfo.InvariantCulture)
                + "; median run p95 " + MedianP95.ToString("0.####", CultureInfo.InvariantCulture)
                + "; median run p99 " + p99
                + "; p50 min/max " + MinP50.ToString("0.####", CultureInfo.InvariantCulture) + "/" + MaxP50.ToString("0.####", CultureInfo.InvariantCulture)
                + "; p50 IQR " + IqrP50.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Read-only campaign archive comparison. It never reads samples.ndjson or writes evidence.</summary>
    internal static class Test_PerformanceCampaignComparator
    {
        public const int DEFAULT_MIN_VALID_REPEATS = 5;
        public const double MIN_MEASUREMENT_COVERAGE = 0.95d;

        private const string CAMPAIGN_FILE_NAME = "campaign.json";
        private const string CAMPAIGNS_DIRECTORY_NAME = "Campaigns";
        private const string UNKNOWN = "unknown";

        public static List<Test_PerformanceCampaignArchive> LoadArchives(out string diagnostic)
        {
            diagnostic = null;
            var archives = new List<Test_PerformanceCampaignArchive>();
            string root;
            try
            {
                root = GetCampaignsRoot();
            }
            catch (Exception exception)
            {
                diagnostic = "The campaign root could not be resolved: " + exception.Message;
                return archives;
            }
            if (!Directory.Exists(root))
                return archives;
            if (!IsSafeDirectory(root))
            {
                diagnostic = "The campaign root is a reparse point and was not read.";
                return archives;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(root);
            }
            catch (Exception exception)
            {
                diagnostic = "The campaign root could not be enumerated: " + exception.Message;
                return archives;
            }

            foreach (var directory in directories)
            {
                if (!IsSafeDirectory(directory))
                    continue;
                var archive = new Test_PerformanceCampaignArchive { Directory = Path.GetFullPath(directory) };
                try
                {
                    var manifestPath = Path.Combine(directory, CAMPAIGN_FILE_NAME);
                    if (!IsSafeFile(manifestPath, directory))
                        throw new InvalidDataException("campaign.json is missing or unsafe.");
                    archive.Manifest = JsonUtility.FromJson<Test_PerformanceCampaignManifest>(File.ReadAllText(manifestPath));
                    if (archive.Manifest == null || string.IsNullOrEmpty(archive.Manifest.campaign_id))
                        throw new InvalidDataException("campaign.json is empty or has no campaign_id.");
                    if (archive.Manifest.campaign_id != Path.GetFileName(directory))
                        throw new InvalidDataException("campaign_id does not match its archive directory.");
                    if (archive.Manifest.attempts == null)
                        archive.Manifest.attempts = new List<Test_PerformanceCampaignAttempt>();
                }
                catch (Exception exception)
                {
                    archive.Diagnostic = exception.Message;
                }
                archives.Add(archive);
            }
            archives.Sort((left, right) => string.Compare(SortTimestamp(right), SortTimestamp(left), StringComparison.Ordinal));
            return archives;
        }

        public static Test_PerformanceCampaignComparisonReport Compare(
            IReadOnlyList<Test_PerformanceCampaignArchive> archives,
            string baselineID,
            string candidateID,
            int minimumValidRepeats = DEFAULT_MIN_VALID_REPEATS)
        {
            var report = new Test_PerformanceCampaignComparisonReport
            {
                Baseline = FindArchive(archives, baselineID),
                Candidate = FindArchive(archives, candidateID),
            };
            if (report.Baseline == null || report.Candidate == null)
                return Fail(report, "Select two archived campaigns before comparing.", "The selected campaign archive is unavailable; no older report was substituted.");
            if (!report.Baseline.IsValid || !report.Candidate.IsValid)
            {
                AddArchiveDiagnostic(report, report.Baseline);
                AddArchiveDiagnostic(report, report.Candidate);
                return Fail(report, "One or both selected campaign manifests are invalid.", null);
            }
            minimumValidRepeats = Math.Max(1, minimumValidRepeats);

            var hardReasons = CompareProvenance(report.Baseline.Manifest.source, report.Candidate.Manifest.source, report.Reasons);
            if (!report.Baseline.IsCompleted)
            {
                report.Reasons.Add("Baseline campaign is " + (report.Baseline.Manifest.state ?? UNKNOWN) + "; it is not treated as complete.");
                hardReasons = true;
            }
            if (!report.Candidate.IsCompleted)
            {
                report.Reasons.Add("Candidate campaign is " + (report.Candidate.Manifest.state ?? UNKNOWN) + "; it is not treated as complete.");
                hardReasons = true;
            }

            var baselineAttempts = ReadAttempts(report.Baseline);
            var candidateAttempts = ReadAttempts(report.Candidate);
            var groups = new Dictionary<string, Test_PerformanceCampaignComparisonGroup>(StringComparer.Ordinal);
            AddAttempts(groups, baselineAttempts, true);
            AddAttempts(groups, candidateAttempts, false);
            foreach (var group in groups.Values.OrderBy(value => value.Label, StringComparer.Ordinal))
            {
                group.BaselineValidCount = group.BaselineAttempts.Count(attempt => attempt.IsValid);
                group.CandidateValidCount = group.CandidateAttempts.Count(attempt => attempt.IsValid);
                if (group.BaselineAttempts.Count == 0)
                    group.Classification = Test_PerformanceCampaignGroupClassification.CANDIDATE_ONLY;
                else if (group.CandidateAttempts.Count == 0)
                    group.Classification = Test_PerformanceCampaignGroupClassification.BASELINE_ONLY;
                else if (!IdentitiesMatch(group))
                {
                    group.Classification = Test_PerformanceCampaignGroupClassification.INCOMPATIBLE;
                    group.Reason = "Fixture/configuration identity, build/render identity or finalized fixture hash differs.";
                    hardReasons = true;
                }
                else
                {
                    group.Classification = Test_PerformanceCampaignGroupClassification.MATCHED;
                    BuildMetricComparisons(group, minimumValidRepeats);
                    if (HasMetricPolicyMismatch(group))
                    {
                        group.Reason = AddReason(group.Reason, "Metric sample/retention policy differs between the selected campaigns.");
                        hardReasons = true;
                    }
                    if (group.BaselineValidCount < minimumValidRepeats || group.CandidateValidCount < minimumValidRepeats)
                        report.Reasons.Add(group.Label + ": fewer than " + minimumValidRepeats + " valid independent repeats; descriptive statistics are provisional.");
                }
                report.Groups.Add(group);
            }

            var matched = report.Groups.Count(group => group.Classification == Test_PerformanceCampaignGroupClassification.MATCHED);
            var incompatible = report.Groups.Count(group => group.Classification == Test_PerformanceCampaignGroupClassification.INCOMPATIBLE);
            var candidateOnly = report.Groups.Count(group => group.Classification == Test_PerformanceCampaignGroupClassification.CANDIDATE_ONLY);
            var baselineOnly = report.Groups.Count(group => group.Classification == Test_PerformanceCampaignGroupClassification.BASELINE_ONLY);
            var insufficient = report.Groups.Any(group => group.Classification == Test_PerformanceCampaignGroupClassification.MATCHED
                && (group.BaselineValidCount < minimumValidRepeats || group.CandidateValidCount < minimumValidRepeats));
            report.Status = hardReasons || incompatible > 0
                ? Test_PerformanceCampaignComparisonStatus.NOT_COMPARABLE
                : insufficient || matched == 0
                    ? Test_PerformanceCampaignComparisonStatus.PROVISIONAL
                    : Test_PerformanceCampaignComparisonStatus.COMPARABLE;
            report.Summary = "matched " + matched + "; candidate only " + candidateOnly + "; baseline only " + baselineOnly
                + "; incompatible " + incompatible + "; valid runs baseline " + baselineAttempts.Count(attempt => attempt.IsValid) + "/" + baselineAttempts.Count
                + ", candidate " + candidateAttempts.Count(attempt => attempt.IsValid) + "/" + candidateAttempts.Count
                + "; minimum " + minimumValidRepeats + " valid repeats; coverage policy " + MIN_MEASUREMENT_COVERAGE.ToString("P0", CultureInfo.InvariantCulture);
            return report;
        }

        public static void OpenEvidence(string path, string campaignDirectory)
        {
            if (!string.IsNullOrEmpty(path) && IsSafeFile(path, GetResultRoot()) && !IsWithin(path, GetCampaignsRoot()))
                EditorUtility.RevealInFinder(path);
        }

        private static Test_PerformanceCampaignComparisonReport Fail(Test_PerformanceCampaignComparisonReport report, string summary, string reason)
        {
            report.Status = Test_PerformanceCampaignComparisonStatus.ERROR;
            report.Summary = summary;
            if (!string.IsNullOrEmpty(reason))
                report.Reasons.Add(reason);
            return report;
        }

        private static void AddArchiveDiagnostic(Test_PerformanceCampaignComparisonReport report, Test_PerformanceCampaignArchive archive)
        {
            if (archive != null && !string.IsNullOrEmpty(archive.Diagnostic))
                report.Reasons.Add(archive.CampaignID + ": " + archive.Diagnostic);
        }

        private static bool CompareProvenance(Test_PerformanceCampaignSource baseline, Test_PerformanceCampaignSource candidate, List<string> reasons)
        {
            if (baseline == null || candidate == null)
            {
                reasons.Add("Source provenance is missing from one or both campaigns; comparison is unqualified.");
                return true;
            }
            var hard = baseline.dirty != candidate.dirty;
            if (baseline.dirty != candidate.dirty)
                reasons.Add("Dirty state differs; source changes are not safely comparable.");
            hard |= CompareRequired(reasons, "hardware", baseline.hardware, candidate.hardware);
            hard |= CompareRequired(reasons, "operating system", baseline.operating_system, candidate.operating_system);
            hard |= CompareRequired(reasons, "Unity version", baseline.unity_version, candidate.unity_version);
            hard |= CompareRequired(reasons, "packages", baseline.packages_sha256, candidate.packages_sha256);
            hard |= CompareRequired(reasons, "PurrNet", baseline.purrnet_sha256, candidate.purrnet_sha256);
            hard |= CompareRequired(reasons, "graphics device", baseline.graphics_device, candidate.graphics_device);
            hard |= CompareRequired(reasons, "graphics settings", baseline.graphics_settings, candidate.graphics_settings);
            if (!string.Equals(baseline.git_branch, candidate.git_branch, StringComparison.OrdinalIgnoreCase))
                reasons.Add("Cross-branch/source comparison: '" + (baseline.git_branch ?? "unknown/not recorded") + "' vs '" + (candidate.git_branch ?? "unknown/not recorded") + "'. Branch differences are allowed, but intentional version comparison must be reviewed.");
            if (!string.Equals(baseline.commit_sha, candidate.commit_sha, StringComparison.OrdinalIgnoreCase))
                reasons.Add("Source revision differs: '" + (baseline.commit_sha ?? "unknown") + "' vs '" + (candidate.commit_sha ?? "unknown") + "'.");
            return hard;
        }

        private static bool CompareRequired(List<string> reasons, string label, string baseline, string candidate)
        {
            if (string.IsNullOrEmpty(baseline) || string.IsNullOrEmpty(candidate))
            {
                reasons.Add(label + " is missing from one or both campaigns; comparison is unqualified.");
                return true;
            }
            if (!string.Equals(baseline, candidate, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add(label + " differs: '" + baseline + "' vs '" + candidate + "'.");
                return true;
            }
            return false;
        }

        private static List<Test_PerformanceCampaignAttemptView> ReadAttempts(Test_PerformanceCampaignArchive archive)
        {
            var views = new List<Test_PerformanceCampaignAttemptView>();
            foreach (var attempt in archive.Manifest.attempts ?? new List<Test_PerformanceCampaignAttempt>())
            {
                var job = FindJob(archive.Manifest, attempt.job_id);
                var view = new Test_PerformanceCampaignAttemptView
                {
                    AttemptID = attempt.attempt_id,
                    RunID = attempt.run_id,
                    State = attempt.state,
                    ResultStatus = attempt.result_status,
                    StopReason = attempt.stop_reason,
                    Diagnostic = attempt.diagnostic,
                    ResultPath = attempt.result_path,
                    ReportPath = attempt.report_path,
                    GroupKey = BuildCoreGroupKey(job, null),
                    IdentityKey = BuildIdentityKey(job, null, null),
                };
                try
                {
                    var resultRoot = GetResultRoot();
                    if (!string.IsNullOrEmpty(attempt.result_path)
                        && (!IsWithin(attempt.result_path, resultRoot) || IsWithin(attempt.result_path, GetCampaignsRoot()) || !Directory.Exists(attempt.result_path)))
                        view.Diagnostic = AddReason(view.Diagnostic, "The campaign result path is stale or outside the expected result root.");
                    if (!string.IsNullOrEmpty(attempt.report_path)
                        && (!IsWithin(attempt.report_path, resultRoot) || IsWithin(attempt.report_path, GetCampaignsRoot()) || !File.Exists(attempt.report_path)))
                        view.Diagnostic = AddReason(view.Diagnostic, "The campaign report path is stale or outside the expected result root.");
                }
                catch (Exception exception)
                {
                    view.Diagnostic = AddReason(view.Diagnostic, "Campaign evidence path validation failed: " + exception.Message);
                }
                if (attempt.state != Test_PerformanceCampaignAttemptState.COMPLETED)
                {
                    view.Diagnostic = AddReason(view.Diagnostic, "Attempt state is not completed.");
                    views.Add(view);
                    continue;
                }
                try
                {
                    var resultRoot = GetResultRoot();
                    if (string.IsNullOrEmpty(attempt.result_path) || !IsSafeDirectory(attempt.result_path) || !IsWithin(attempt.result_path, resultRoot) || IsWithin(attempt.result_path, GetCampaignsRoot()))
                        throw new InvalidDataException("The finalized result path is missing, unsafe or outside the benchmark result root.");
                    var finalized = Test_PerformanceResultReader.ReadFinalized(attempt.result_path);
                    if (!finalized.IsVerified)
                        throw new InvalidDataException(finalized.ErrorMessage ?? "The finalized result failed structured identity validation.");
                    if (finalized.Status != "COMPLETE-MEASURED")
                        throw new InvalidDataException("The attempt status is not COMPLETE-MEASURED.");
                    var run = ReadJson<Test_PerformanceComparisonRunDocument>(Path.Combine(attempt.result_path, "run.json"));
                    var summary = ReadJson<Test_PerformanceComparisonSummaryDocument>(Path.Combine(attempt.result_path, "summary.json"));
                    if (run.run_id != attempt.run_id || summary.run_id != attempt.run_id)
                        throw new InvalidDataException("Finalized artifact run IDs do not match the campaign attempt.");
                    var expectedReportPath = Path.Combine(attempt.result_path, "report.md");
                    if (string.IsNullOrEmpty(attempt.report_path) || !string.Equals(Path.GetFullPath(attempt.report_path), Path.GetFullPath(expectedReportPath), StringComparison.OrdinalIgnoreCase))
                    {
                        view.Diagnostic = AddReason(view.Diagnostic, "The campaign report path is missing or stale; the finalized report location was used for display.");
                        view.ReportPath = expectedReportPath;
                    }
                    view.GroupKey = BuildCoreGroupKey(job, run);
                    view.IdentityKey = BuildIdentityKey(job, run, summary.fixture_sha256);
                    view.Metrics.AddRange(ReadMetricSamples(summary));
                    view.IsValid = true;
                }
                catch (Exception exception)
                {
                    view.Diagnostic = AddReason(view.Diagnostic, exception.Message);
                }
                views.Add(view);
            }
            return views;
        }

        private static void AddAttempts(Dictionary<string, Test_PerformanceCampaignComparisonGroup> groups, List<Test_PerformanceCampaignAttemptView> attempts, bool baseline)
        {
            foreach (var attempt in attempts)
            {
                var key = attempt.GroupKey ?? "unclassified";
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new Test_PerformanceCampaignComparisonGroup { Label = key };
                    groups.Add(key, group);
                }
                if (baseline)
                    group.BaselineAttempts.Add(attempt);
                else
                    group.CandidateAttempts.Add(attempt);
            }
        }

        private static bool IdentitiesMatch(Test_PerformanceCampaignComparisonGroup group)
        {
            var identities = group.BaselineAttempts.Concat(group.CandidateAttempts)
                .Where(attempt => attempt.IsValid)
                .Select(attempt => attempt.IdentityKey)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var baselineHasValid = group.BaselineAttempts.Any(attempt => attempt.IsValid);
            var candidateHasValid = group.CandidateAttempts.Any(attempt => attempt.IsValid);
            return !baselineHasValid || !candidateHasValid || identities.Count <= 1;
        }

        private static void BuildMetricComparisons(Test_PerformanceCampaignComparisonGroup group, int minimumValidRepeats)
        {
            var samples = group.BaselineAttempts.Concat(group.CandidateAttempts).Where(attempt => attempt.IsValid).SelectMany(attempt => attempt.Metrics).ToList();
            foreach (var key in samples.Select(sample => sample.Key).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                var baselineSamples = group.BaselineAttempts.SelectMany(attempt => attempt.Metrics).Where(sample => sample.Key == key).ToList();
                var candidateSamples = group.CandidateAttempts.SelectMany(attempt => attempt.Metrics).Where(sample => sample.Key == key).ToList();
                var first = baselineSamples.Concat(candidateSamples).First();
                var comparison = new Test_PerformanceCampaignMetricComparison
                {
                    MetricID = first.MetricID,
                    Unit = first.Unit,
                    Source = first.Source,
                    Scope = first.Scope,
                    SamplePolicy = first.RetentionPolicy,
                    Semantics = DescribeSemantics(first.Unit),
                    Direction = DescribeDirection(first.Unit),
                    BaselineAvailable = CountAvailable(group.BaselineAttempts, key),
                    CandidateAvailable = CountAvailable(group.CandidateAttempts, key),
                    BaselineQualified = baselineSamples.Count(sample => sample.HasP50),
                    CandidateQualified = candidateSamples.Count(sample => sample.HasP50),
                    Baseline = BuildDistribution(baselineSamples),
                    Candidate = BuildDistribution(candidateSamples),
                };
                if (comparison.Baseline.Count == 0 || comparison.Candidate.Count == 0)
                    comparison.Caveat = "Metric is unavailable or unqualified on one side; no zero was substituted.";
                else
                {
                    comparison.DeltaP50 = comparison.Candidate.MedianP50 - comparison.Baseline.MedianP50;
                    comparison.DeltaP95 = comparison.Candidate.MedianP95 - comparison.Baseline.MedianP95;
                    comparison.DeltaP99 = comparison.Candidate.MedianP99.HasValue && comparison.Baseline.MedianP99.HasValue ? comparison.Candidate.MedianP99.Value - comparison.Baseline.MedianP99.Value : (double?)null;
                    comparison.DeltaP50Percent = PercentDelta(comparison.Baseline.MedianP50, comparison.DeltaP50.Value);
                    comparison.DeltaP95Percent = PercentDelta(comparison.Baseline.MedianP95, comparison.DeltaP95.Value);
                    comparison.DeltaP99Percent = comparison.DeltaP99.HasValue ? PercentDelta(comparison.Baseline.MedianP99.Value, comparison.DeltaP99.Value) : (double?)null;
                    comparison.Caveat = comparison.Baseline.Count < minimumValidRepeats || comparison.Candidate.Count < minimumValidRepeats
                        ? "PROVISIONAL: fewer than " + minimumValidRepeats + " valid independent runs."
                        : null;
                }
                comparison.Caveat = AddReason(comparison.Caveat, "Medians are across runs; p95/p99 are medians of per-run percentiles, not pooled samples.");
                group.Metrics.Add(comparison);
            }
        }

        private static int CountAvailable(List<Test_PerformanceCampaignAttemptView> attempts, string key)
        {
            return attempts.Count(attempt => attempt.IsValid && attempt.Metrics.Any(metric => metric.Key == key));
        }

        private static bool HasMetricPolicyMismatch(Test_PerformanceCampaignComparisonGroup group)
        {
            var policies = group.BaselineAttempts.Concat(group.CandidateAttempts)
                .Where(attempt => attempt.IsValid)
                .SelectMany(attempt => attempt.Metrics)
                .GroupBy(metric => metric.MetricID + "|" + metric.Unit + "|" + metric.Source + "|" + metric.Scope, StringComparer.Ordinal);
            return policies.Any(policy => policy.Select(metric => metric.RetentionPolicy ?? UNKNOWN).Distinct(StringComparer.Ordinal).Count() > 1);
        }

        private static Test_PerformanceCampaignDistribution BuildDistribution(List<Test_PerformanceCampaignMetricSample> samples)
        {
            var p50 = samples.Where(sample => sample.HasP50).Select(sample => sample.P50).OrderBy(value => value).ToList();
            var p95 = samples.Where(sample => sample.HasP95).Select(sample => sample.P95).OrderBy(value => value).ToList();
            var p99 = samples.Where(sample => sample.HasP99).Select(sample => sample.P99).OrderBy(value => value).ToList();
            return new Test_PerformanceCampaignDistribution
            {
                Count = p50.Count,
                MedianP50 = Median(p50),
                MedianP95 = Median(p95),
                MedianP99 = p99.Count == 0 ? (double?)null : Median(p99),
                MinP50 = p50.Count == 0 ? 0d : p50[0],
                MaxP50 = p50.Count == 0 ? 0d : p50[p50.Count - 1],
                IqrP50 = Iqr(p50),
            };
        }

        private static double? PercentDelta(double baseline, double delta)
        {
            return Math.Abs(baseline) < double.Epsilon ? (double?)null : delta / baseline * 100d;
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
                return 0d;
            var middle = values.Count / 2;
            return values.Count % 2 == 0 ? (values[middle - 1] + values[middle]) / 2d : values[middle];
        }

        private static double Iqr(List<double> values)
        {
            return values.Count < 2 ? 0d : Percentile(values, 0.75d) - Percentile(values, 0.25d);
        }

        private static double Percentile(List<double> values, double percentile)
        {
            var position = (values.Count - 1) * percentile;
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            return lower == upper ? values[lower] : values[lower] + (values[upper] - values[lower]) * (position - lower);
        }

        private static List<Test_PerformanceCampaignMetricSample> ReadMetricSamples(Test_PerformanceComparisonSummaryDocument summary)
        {
            var samples = new List<Test_PerformanceCampaignMetricSample>();
            foreach (var metric in summary.metrics ?? Array.Empty<Test_PerformanceComparisonMetricDocument>())
            {
                if (metric.phase != "measurement")
                    continue;
                var qualified = metric.observation == "measured"
                    && metric.sample_count > 0
                    && metric.measurement_coverage >= MIN_MEASUREMENT_COVERAGE;
                samples.Add(new Test_PerformanceCampaignMetricSample
                {
                    MetricID = metric.metric_id,
                    Unit = metric.unit,
                    Source = string.IsNullOrEmpty(metric.source) ? InferSource(metric.metric_id) : metric.source,
                    Scope = string.IsNullOrEmpty(metric.scope) ? UNKNOWN : metric.scope,
                    Observation = metric.observation,
                    Reason = metric.reason,
                    RetentionPolicy = metric.retention_policy,
                    SampleCount = metric.sample_count,
                    HasP50 = qualified,
                    HasP95 = qualified,
                    HasP99 = qualified && metric.sample_count >= 100 && metric.reason != "insufficient_samples",
                    P50 = metric.p50,
                    P95 = metric.p95,
                    P99 = metric.p99,
                });
            }
            return samples;
        }

        private static string BuildCoreGroupKey(Test_PerformanceCampaignJob job, Test_PerformanceComparisonRunDocument run)
        {
            var options = job?.options;
            var core = options == null
                ? job?.job_id ?? "unclassified"
                : string.Join("|", options.EffectiveProfileID, options.WorkloadMode, options.WorldFixture, options.GraphicalScenario, options.DensityTier, options.ActorDensity, options.ActorCount.ToString(CultureInfo.InvariantCulture), options.Seed.ToString(CultureInfo.InvariantCulture), options.WarmupSeconds.ToString("R", CultureInfo.InvariantCulture), options.MeasurementSeconds.ToString("R", CultureInfo.InvariantCulture), options.ActorVariantID);
            return core;
        }

        private static string BuildIdentityKey(Test_PerformanceCampaignJob job, Test_PerformanceComparisonRunDocument run, string fixtureHash)
        {
            var identity = job?.fixture_identity == null
                ? UNKNOWN
                : string.Join("|", job.fixture_identity.configuration_sha256, job.fixture_identity.profile_revision, job.fixture_identity.actor_variant_id, job.fixture_identity.world_scene_guid, job.fixture_identity.world_scene_hash, job.fixture_identity.graphical_plan_hash, job.fixture_identity.graphical_route_id, fixtureHash ?? UNKNOWN);
            var build = run?.build?.kind ?? UNKNOWN;
            var render = run?.render?.mode ?? UNKNOWN;
            return identity + "|build=" + build + "|render=" + render;
        }

        private static Test_PerformanceCampaignJob FindJob(Test_PerformanceCampaignManifest manifest, string jobID)
        {
            return manifest.jobs?.FirstOrDefault(job => job.job_id == jobID);
        }

        private static Test_PerformanceCampaignArchive FindArchive(IReadOnlyList<Test_PerformanceCampaignArchive> archives, string campaignID)
        {
            return archives?.FirstOrDefault(archive => archive.CampaignID == campaignID);
        }

        private static string SortTimestamp(Test_PerformanceCampaignArchive archive)
        {
            return archive.Manifest?.finished_utc ?? archive.Manifest?.updated_utc ?? archive.Manifest?.created_utc ?? string.Empty;
        }

        private static string AddReason(string existing, string reason)
        {
            return string.IsNullOrEmpty(existing) ? reason : existing + " " + reason;
        }

        private static string InferSource(string metricID)
        {
            return !string.IsNullOrEmpty(metricID) && metricID.StartsWith("client.", StringComparison.OrdinalIgnoreCase) ? "graphical/client" : "server";
        }

        private static string DescribeSemantics(string unit)
        {
            if (unit == "ms")
                return "duration/latency";
            if (unit == "bytes")
                return "memory/byte snapshot or volume";
            if (unit == "count" || unit == "ticks" || unit == "events/s")
                return "counter or rate; inspect metric identity";
            return unit ?? UNKNOWN;
        }

        private static string DescribeDirection(string unit)
        {
            return unit == "ms" || unit == "bytes" ? "lower is usually better" : "contextual; no blanket better/worse rule";
        }

        private static T ReadJson<T>(string path)
        {
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Missing or unsafe finalized evidence: " + Path.GetFileName(path));
            var value = JsonUtility.FromJson<T>(File.ReadAllText(path));
            if (value == null)
                throw new InvalidDataException("Malformed finalized evidence: " + Path.GetFileName(path));
            return value;
        }

        private static string GetResultRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
                throw new DirectoryNotFoundException("The Unity project root could not be resolved.");
            return Path.GetFullPath(Path.Combine(projectRoot.FullName, Test_PerformanceRun.RESULT_ROOT));
        }

        private static string GetCampaignsRoot() => Path.Combine(GetResultRoot(), CAMPAIGNS_DIRECTORY_NAME);

        private static bool IsSafeDirectory(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }

        private static bool IsSafeFile(string path, string root)
        {
            return File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 && IsWithin(path, root);
        }

        private static bool IsWithin(string path, string root)
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return fullPath == fullRoot || fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || fullPath.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        #pragma warning disable 0649

        [Serializable]
        private sealed class Test_PerformanceComparisonRunDocument
        {
            public string run_id;
            public Test_PerformanceComparisonBuildDocument build;
            public Test_PerformanceComparisonRenderDocument render;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonBuildDocument { public string kind; }
        [Serializable]
        private sealed class Test_PerformanceComparisonRenderDocument { public string mode; }

        [Serializable]
        private sealed class Test_PerformanceComparisonSummaryDocument
        {
            public string run_id;
            public string fixture_sha256;
            public Test_PerformanceComparisonMetricDocument[] metrics;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonMetricDocument
        {
            public string metric_id;
            public string unit;
            public string observation;
            public string phase;
            public int sample_count;
            public double p50;
            public double p95;
            public double p99;
            public double measurement_coverage;
            public string reason;
            public string source;
            public string scope;
            public string retention_policy;
        }

        #pragma warning restore 0649
    }
}
