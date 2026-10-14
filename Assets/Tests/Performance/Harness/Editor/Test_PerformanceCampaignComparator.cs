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
        public const string PROVISIONAL = "PROVISIONAL/INSUFFICIENT EVIDENCE";
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
                var dirty = source == null ? "unknown" : source.dirty ? "dirty (informational)" : "clean (informational)";
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
                    + "; dirty " + source.dirty + " (informational)"
                    + "; diff " + (source.diff_sha256 ?? "unknown") + " (informational)"
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
        public string ProfileID;
        public bool IsGraphical;
        public string Classification;
        public string Reason;
        public bool HasInsufficientRequiredEvidence;
        public int BaselineValidCount;
        public int CandidateValidCount;
        public readonly List<Test_PerformanceCampaignAttemptView> BaselineAttempts = new();
        public readonly List<Test_PerformanceCampaignAttemptView> CandidateAttempts = new();
        public readonly List<Test_PerformanceCampaignMetricComparison> Metrics = new();
    }

    internal sealed class Test_PerformanceCampaignAttemptView
    {
        public string CampaignID;
        public string AttemptID;
        public string JobID;
        public string RunID;
        public string State;
        public string ResultStatus;
        public string StopReason;
        public string Diagnostic;
        public string ResultPath;
        public string ReportPath;
        public string GroupKey;
        public string IdentityKey;
        public string ProfileID;
        public Test_PerformanceCampaignSource Source;
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
        public int AttemptedSampleCount;
        public int RetainedSampleCount;
        public double MeasurementCoverage;
        public bool HasP50;
        public bool HasP95;
        public bool HasP99;
        public double P50;
        public double P95;
        public double P99;
        public bool HasMaximum;
        public double Maximum;
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
        public bool IsRequired;
        public string RequiredStatistics;
        public int BaselineAvailable;
        public int CandidateAvailable;
        public int BaselineQualified;
        public int CandidateQualified;
        public int BaselineP50Qualified;
        public int CandidateP50Qualified;
        public int BaselineP95Qualified;
        public int CandidateP95Qualified;
        public int BaselineP99Qualified;
        public int CandidateP99Qualified;
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

    internal sealed class Test_PerformanceCampaignMetricRequirement
    {
        public string MetricID;
        public string Unit;
        public string Source;
        public string Scope;
        public string[] Statistics;

        public bool Matches(Test_PerformanceCampaignMetricSample sample)
        {
            return sample != null
                && string.Equals(sample.MetricID, MetricID, StringComparison.Ordinal)
                && string.Equals(sample.Unit, Unit, StringComparison.Ordinal)
                && string.Equals(sample.Source, Source, StringComparison.Ordinal)
                && (string.IsNullOrEmpty(Scope) || string.Equals(sample.Scope, Scope, StringComparison.Ordinal));
        }

        public bool IsQualified(Test_PerformanceCampaignMetricSample sample)
        {
            if (!Matches(sample))
                return false;
            foreach (var statistic in Statistics)
            {
                if (statistic == "p50" && !sample.HasP50
                    || statistic == "p95" && !sample.HasP95
                    || statistic == "p99" && !sample.HasP99)
                    return false;
            }
            return true;
        }

        public string DescribeStatistics()
        {
            return string.Join("/", Statistics ?? Array.Empty<string>());
        }
    }

    internal sealed class Test_PerformanceCampaignLinkedQualificationReport
    {
        public string RootCampaignID;
        public string Status;
        public string Summary;
        public readonly List<string> Reasons = new();
        public readonly List<Test_PerformanceCampaignComparisonGroup> Groups = new();
    }

    internal sealed class Test_PerformanceCampaignDistribution
    {
        public int Count;
        public double MedianP50;
        public double MedianP95;
        public double? MedianP99;
        public double MinP50;
        public double MaxP50;
        public double MinP95;
        public double MaxP95;
        public double? MinP99;
        public double? MaxP99;
        public double IqrP50;
        public double? CoefficientOfVariationP50;
        public double? MaximumObserved;

        public string Describe()
        {
            var p99 = MedianP99.HasValue ? MedianP99.Value.ToString("0.####", CultureInfo.InvariantCulture) : "unavailable";
            return "n " + Count + "; median run p50 " + MedianP50.ToString("0.####", CultureInfo.InvariantCulture)
                + "; median run p95 " + MedianP95.ToString("0.####", CultureInfo.InvariantCulture)
                + "; median run p99 " + p99
                + "; p50 min/max " + MinP50.ToString("0.####", CultureInfo.InvariantCulture) + "/" + MaxP50.ToString("0.####", CultureInfo.InvariantCulture)
                + "; p50 IQR " + IqrP50.ToString("0.####", CultureInfo.InvariantCulture)
                + "; p50 CV " + (CoefficientOfVariationP50.HasValue ? CoefficientOfVariationP50.Value.ToString("P1", CultureInfo.InvariantCulture) : "unavailable")
                + "; max run p95 " + MaxP95.ToString("0.####", CultureInfo.InvariantCulture)
                + "; max run p99 " + (MaxP99.HasValue ? MaxP99.Value.ToString("0.####", CultureInfo.InvariantCulture) : "unavailable")
                + "; max observed sample " + (MaximumObserved.HasValue ? MaximumObserved.Value.ToString("0.####", CultureInfo.InvariantCulture) : "unavailable");
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
                && (group.HasInsufficientRequiredEvidence
                    || group.BaselineValidCount < minimumValidRepeats
                    || group.CandidateValidCount < minimumValidRepeats));
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

        /// <summary>
        /// Builds an explicit read-only qualification cohort for a campaign and its linked make-up
        /// campaigns. Ordinary baseline/candidate comparisons intentionally do not use this path.
        /// </summary>
        public static Test_PerformanceCampaignLinkedQualificationReport BuildLinkedQualificationCohort(
            IReadOnlyList<Test_PerformanceCampaignArchive> archives,
            string campaignID,
            int minimumValidRepeats = DEFAULT_MIN_VALID_REPEATS)
        {
            var report = new Test_PerformanceCampaignLinkedQualificationReport
            {
                RootCampaignID = campaignID,
                Status = Test_PerformanceCampaignComparisonStatus.ERROR,
            };
            var selected = FindArchive(archives, campaignID);
            if (selected == null || !selected.IsValid)
            {
                report.Summary = "The selected campaign archive is unavailable or invalid.";
                return report;
            }

            var root = ResolveLinkedRoot(archives, selected, report.Reasons);
            if (root == null)
            {
                report.Summary = "The linked campaign root could not be resolved.";
                return report;
            }
            report.RootCampaignID = root.CampaignID;
            var attemptsByCampaign = new Dictionary<string, List<Test_PerformanceCampaignAttemptView>>(StringComparer.Ordinal);
            foreach (var archive in archives ?? Array.Empty<Test_PerformanceCampaignArchive>())
            {
                if (!archive.IsValid || !IsLinkedToRoot(archives, archive, root.CampaignID))
                    continue;
                attemptsByCampaign[archive.CampaignID] = ReadAttempts(archive);
            }

            foreach (var sourceJob in root.Manifest.jobs ?? new List<Test_PerformanceCampaignJob>())
            {
                var sourceGroupKey = BuildCoreGroupKey(sourceJob, null);
                var group = new Test_PerformanceCampaignComparisonGroup
                {
                    Label = sourceJob.DisplayName,
                    ProfileID = sourceJob.options?.EffectiveProfileID,
                    IsGraphical = sourceJob.options?.IsGraphicalClient == true,
                    Classification = "linked qualification cohort",
                };
                AddCohortAttempts(group, root, sourceJob, attemptsByCampaign, sourceGroupKey, report.Reasons);
                foreach (var archive in archives ?? Array.Empty<Test_PerformanceCampaignArchive>())
                {
                    if (archive == null || archive == root || !archive.IsValid || !IsLinkedToRoot(archives, archive, root.CampaignID))
                        continue;
                    var parentArchive = FindArchive(archives, archive.Manifest.linked_campaign_id);
                    var parentJob = parentArchive == null ? null : FindJob(parentArchive.Manifest, archive.Manifest.makeup_for_job_id);
                    if (parentJob == null || !string.Equals(BuildCoreGroupKey(parentJob, null), sourceGroupKey, StringComparison.Ordinal))
                        continue;
                    AddCohortAttempts(group, archive, null, attemptsByCampaign, sourceGroupKey, report.Reasons);
                }

                if (group.BaselineAttempts.Count == 0)
                    continue;
                ValidateCohortIdentity(group, root, report.Reasons);
                BuildCohortMetricComparisons(group, minimumValidRepeats);
                group.BaselineValidCount = group.BaselineAttempts.Count(attempt => attempt.IsValid);
                group.HasInsufficientRequiredEvidence = group.Metrics.Any(metric => metric.IsRequired && metric.BaselineQualified < minimumValidRepeats);
                report.Groups.Add(group);
            }

            var hasIncompatible = report.Groups.Any(group => !string.IsNullOrEmpty(group.Reason));
            var hasInsufficient = report.Groups.Any(group => group.HasInsufficientRequiredEvidence
                || group.BaselineValidCount < minimumValidRepeats);
            report.Status = hasIncompatible
                ? Test_PerformanceCampaignComparisonStatus.NOT_COMPARABLE
                : hasInsufficient
                    ? Test_PerformanceCampaignComparisonStatus.PROVISIONAL
                    : Test_PerformanceCampaignComparisonStatus.COMPARABLE;
            report.Summary = "linked root " + report.RootCampaignID + "; groups " + report.Groups.Count
                + "; qualified runs " + report.Groups.Sum(group => group.BaselineValidCount)
                + "; minimum " + minimumValidRepeats + "; duplicate, failed and incompatible attempts remain listed and excluded.";
            return report;
        }

        private static void AddCohortAttempts(
            Test_PerformanceCampaignComparisonGroup group,
            Test_PerformanceCampaignArchive archive,
            Test_PerformanceCampaignJob sourceJob,
            Dictionary<string, List<Test_PerformanceCampaignAttemptView>> attemptsByCampaign,
            string sourceGroupKey,
            List<string> reasons)
        {
            if (!attemptsByCampaign.TryGetValue(archive.CampaignID, out var attempts))
                return;
            var selectedJobIDs = new HashSet<string>(StringComparer.Ordinal);
            if (sourceJob != null)
                selectedJobIDs.Add(sourceJob.job_id);
            else
            {
                foreach (var job in archive.Manifest.jobs ?? new List<Test_PerformanceCampaignJob>())
                {
                    if (string.Equals(BuildCoreGroupKey(job, null), sourceGroupKey, StringComparison.Ordinal))
                        selectedJobIDs.Add(job.job_id);
                }
            }
            foreach (var attempt in attempts)
            {
                if (!selectedJobIDs.Contains(attempt.JobID))
                    continue;
                if (!string.IsNullOrEmpty(attempt.RunID)
                    && group.BaselineAttempts.Any(existing => string.Equals(existing.RunID, attempt.RunID, StringComparison.Ordinal)))
                {
                    attempt.IsValid = false;
                    attempt.Diagnostic = AddReason(attempt.Diagnostic, "Linked cohort exclusion: duplicate run ID.");
                    reasons.Add(attempt.RunID + ": duplicate run ID was retained in the inventory but excluded from qualification.");
                }
                group.BaselineAttempts.Add(attempt);
                if (attempt.Source == null)
                    reasons.Add((attempt.RunID ?? attempt.AttemptID ?? "unknown run") + ": source provenance is missing from the linked cohort.");
            }
        }

        private static void ValidateCohortIdentity(
            Test_PerformanceCampaignComparisonGroup group,
            Test_PerformanceCampaignArchive root,
            List<string> reasons)
        {
            var valid = group.BaselineAttempts.Where(attempt => attempt.IsValid).ToList();
            if (valid.Count == 0)
                return;
            var reference = valid[0];
            foreach (var attempt in group.BaselineAttempts)
            {
                if (!attempt.IsValid)
                    continue;
                if (!SourcesMatchForQualification(root.Manifest.source, attempt.Source, out var sourceReason))
                {
                    attempt.IsValid = false;
                    attempt.Diagnostic = AddReason(attempt.Diagnostic, "Linked cohort exclusion: " + sourceReason);
                    group.Reason = AddReason(group.Reason, attempt.RunID + ": " + sourceReason);
                }
                else if (!string.Equals(reference.IdentityKey, attempt.IdentityKey, StringComparison.Ordinal))
                {
                    attempt.IsValid = false;
                    attempt.Diagnostic = AddReason(attempt.Diagnostic, "Linked cohort exclusion: fixture/build identity differs from the source cohort.");
                    group.Reason = AddReason(group.Reason, attempt.RunID + ": fixture/build identity differs from the source cohort.");
                }
            }
            foreach (var requirement in GetRequiredMetricRequirements(group))
            {
                foreach (var attempt in group.BaselineAttempts.Where(attempt => attempt.IsValid).ToList())
                {
                    var conflictingMetric = attempt.Metrics.FirstOrDefault(metric =>
                        string.Equals(metric.MetricID, requirement.MetricID, StringComparison.Ordinal)
                        && !requirement.Matches(metric));
                    if (conflictingMetric == null)
                        continue;
                    attempt.IsValid = false;
                    attempt.Diagnostic = AddReason(attempt.Diagnostic, "Linked cohort exclusion: required metric identity, unit or scope differs from the source cohort.");
                    group.Reason = AddReason(group.Reason, attempt.RunID + ": required metric identity, unit or scope differs from the source cohort.");
                }
            }

            var policies = group.BaselineAttempts.Where(attempt => attempt.IsValid)
                .SelectMany(attempt => attempt.Metrics)
                .GroupBy(metric => metric.MetricID + "|" + metric.Unit + "|" + metric.Source + "|" + metric.Scope, StringComparer.Ordinal);
            foreach (var policy in policies)
            {
                var policyNames = policy.Select(metric => metric.RetentionPolicy ?? UNKNOWN).Distinct(StringComparer.Ordinal).ToList();
                if (policyNames.Count <= 1)
                    continue;

                var expectedPolicy = policyNames[0];
                group.Reason = AddReason(group.Reason, "Linked cohort retention/sampling protocol differs between attempts.");
                foreach (var attempt in group.BaselineAttempts.Where(attempt => attempt.IsValid).ToList())
                {
                    var mismatchedMetric = attempt.Metrics.FirstOrDefault(metric =>
                        metric.Key.StartsWith(policy.Key, StringComparison.Ordinal)
                        && !string.Equals(metric.RetentionPolicy ?? UNKNOWN, expectedPolicy, StringComparison.Ordinal));
                    if (mismatchedMetric == null)
                        continue;
                    attempt.IsValid = false;
                    attempt.Diagnostic = AddReason(attempt.Diagnostic, "Linked cohort exclusion: retention/sampling protocol differs from the source cohort.");
                    group.Reason = AddReason(group.Reason, attempt.RunID + ": retention/sampling protocol differs from the source cohort.");
                }
            }
        }

        private static bool SourcesMatchForQualification(
            Test_PerformanceCampaignSource expected,
            Test_PerformanceCampaignSource actual,
            out string reason)
        {
            reason = null;
            if (expected == null || actual == null)
            {
                reason = "branch, commit or environment provenance is missing";
                return false;
            }
            if (!string.Equals(expected.git_branch, actual.git_branch, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.commit_sha, actual.commit_sha, StringComparison.OrdinalIgnoreCase))
            {
                reason = "branch/full commit differs from the source campaign";
                return false;
            }
            if (!string.Equals(expected.hardware, actual.hardware, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.operating_system, actual.operating_system, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.unity_version, actual.unity_version, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.packages_sha256, actual.packages_sha256, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.packages_lock_sha256, actual.packages_lock_sha256, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.purrnet_sha256, actual.purrnet_sha256, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.graphics_device, actual.graphics_device, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(expected.graphics_settings, actual.graphics_settings, StringComparison.OrdinalIgnoreCase))
            {
                reason = "hardware, Unity, dependency or graphics identity differs from the source campaign";
                return false;
            }
            return true;
        }

        private static void BuildCohortMetricComparisons(Test_PerformanceCampaignComparisonGroup group, int minimumValidRepeats)
        {
            var samples = group.BaselineAttempts.Where(attempt => attempt.IsValid).SelectMany(attempt => attempt.Metrics).ToList();
            foreach (var requirement in GetRequiredMetricRequirements(group))
            {
                var matching = samples.Where(requirement.Matches).ToList();
                var comparison = BuildCohortMetricComparison(matching, requirement, minimumValidRepeats, group);
                group.Metrics.Add(comparison);
            }
        }

        private static Test_PerformanceCampaignMetricComparison BuildCohortMetricComparison(
            List<Test_PerformanceCampaignMetricSample> samples,
            Test_PerformanceCampaignMetricRequirement requirement,
            int minimumValidRepeats,
            Test_PerformanceCampaignComparisonGroup group)
        {
            var first = samples.FirstOrDefault();
            var comparison = new Test_PerformanceCampaignMetricComparison
            {
                MetricID = first?.MetricID ?? requirement.MetricID,
                Unit = first?.Unit ?? requirement.Unit,
                Source = first?.Source ?? requirement.Source,
                Scope = first?.Scope ?? requirement.Scope,
                SamplePolicy = first?.RetentionPolicy,
                Semantics = DescribeSemantics(first?.Unit ?? requirement.Unit),
                Direction = DescribeDirection(first?.Unit ?? requirement.Unit),
                IsRequired = true,
                RequiredStatistics = requirement.DescribeStatistics(),
                BaselineAvailable = samples.Count,
                BaselineQualified = samples.Count(requirement.IsQualified),
                BaselineP50Qualified = samples.Count(sample => sample.HasP50),
                BaselineP95Qualified = samples.Count(sample => sample.HasP95),
                BaselineP99Qualified = samples.Count(sample => sample.HasP99),
                Baseline = BuildDistribution(samples),
                Candidate = new Test_PerformanceCampaignDistribution(),
            };
            comparison.Caveat = comparison.BaselineQualified < minimumValidRepeats
                ? "INSUFFICIENT EVIDENCE: linked cohort qualified N " + comparison.BaselineQualified + ", minimum " + minimumValidRepeats + "."
                : "Linked make-up cohort only; this is not a baseline/candidate regression comparison.";
            comparison.Caveat = AddReason(comparison.Caveat, DescribeExclusions(group, requirement, first?.Key));
            return comparison;
        }

        private static Test_PerformanceCampaignArchive ResolveLinkedRoot(
            IReadOnlyList<Test_PerformanceCampaignArchive> archives,
            Test_PerformanceCampaignArchive selected,
            List<string> reasons)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = selected;
            while (!string.IsNullOrEmpty(current.Manifest.linked_campaign_id))
            {
                if (!seen.Add(current.CampaignID))
                {
                    reasons.Add("Linked campaign cycle detected.");
                    return null;
                }
                var parent = FindArchive(archives, current.Manifest.linked_campaign_id);
                if (parent == null || !parent.IsValid)
                {
                    reasons.Add("Linked parent campaign is unavailable: " + current.Manifest.linked_campaign_id);
                    return null;
                }
                current = parent;
            }
            return current;
        }

        private static bool IsLinkedToRoot(
            IReadOnlyList<Test_PerformanceCampaignArchive> archives,
            Test_PerformanceCampaignArchive archive,
            string rootCampaignID)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = archive;
            while (current != null)
            {
                if (!seen.Add(current.CampaignID))
                    return false;
                if (string.Equals(current.CampaignID, rootCampaignID, StringComparison.Ordinal))
                    return true;
                if (string.IsNullOrEmpty(current.Manifest.linked_campaign_id))
                    return false;
                current = FindArchive(archives, current.Manifest.linked_campaign_id);
            }
            return false;
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
            var hard = false;
            if (baseline.dirty != candidate.dirty)
                reasons.Add("Dirty state differs; this is informational provenance only and is not a qualification veto.");
            if (!string.Equals(baseline.diff_sha256, candidate.diff_sha256, StringComparison.OrdinalIgnoreCase))
                reasons.Add("Working-tree diff identity differs; this is informational provenance only.");
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
                    CampaignID = archive.CampaignID,
                    AttemptID = attempt.attempt_id,
                    JobID = attempt.job_id,
                    RunID = attempt.run_id,
                    State = attempt.state,
                    ResultStatus = attempt.result_status,
                    StopReason = attempt.stop_reason,
                    Diagnostic = attempt.diagnostic,
                    ResultPath = attempt.result_path,
                    ReportPath = attempt.report_path,
                    GroupKey = BuildCoreGroupKey(job, null),
                    IdentityKey = BuildIdentityKey(job, null, null),
                    ProfileID = job?.options?.EffectiveProfileID,
                    Source = attempt.source ?? archive.Manifest.source,
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
                    var fixture = ReadJson<Test_PerformanceComparisonFixtureDocument>(Path.Combine(attempt.result_path, "fixture.json"));
                    if (run.run_id != attempt.run_id || summary.run_id != attempt.run_id)
                        throw new InvalidDataException("Finalized artifact run IDs do not match the campaign attempt.");
                    var expectedReportPath = Path.Combine(attempt.result_path, "report.md");
                    if (string.IsNullOrEmpty(attempt.report_path) || !string.Equals(Path.GetFullPath(attempt.report_path), Path.GetFullPath(expectedReportPath), StringComparison.OrdinalIgnoreCase))
                    {
                        view.Diagnostic = AddReason(view.Diagnostic, "The campaign report path is missing or stale; the finalized report location was used for display.");
                        view.ReportPath = expectedReportPath;
                    }
                    ValidateAttemptCorrectness(job, summary, fixture, run, out var correctnessDiagnostic);
                    if (!string.IsNullOrEmpty(correctnessDiagnostic))
                        throw new InvalidDataException(correctnessDiagnostic);
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

        private static void ValidateAttemptCorrectness(
            Test_PerformanceCampaignJob job,
            Test_PerformanceComparisonSummaryDocument summary,
            Test_PerformanceComparisonFixtureDocument fixture,
            Test_PerformanceComparisonRunDocument run,
            out string diagnostic)
        {
            diagnostic = null;
            if (summary.correctness == null
                || !string.Equals(summary.correctness.status, "pass", StringComparison.OrdinalIgnoreCase)
                || summary.correctness.failed != 0
                || !summary.correctness.cleanup)
            {
                diagnostic = "Attempt correctness/lifecycle failed: correctness must be pass with zero failed checks and cleanup=true.";
                return;
            }
            if (summary.phase_completion == null
                || !summary.phase_completion.preflight
                || !summary.phase_completion.ramp
                || !summary.phase_completion.warmup
                || !summary.phase_completion.measurement
                || !summary.phase_completion.recovery)
            {
                diagnostic = "Attempt correctness/lifecycle failed: preflight, ramp, warmup, measurement and recovery must all be complete.";
                return;
            }
            var graphicalExecution = job?.options?.IsGraphicalClient == true
                || string.Equals(run.render?.mode, "rendered", StringComparison.OrdinalIgnoreCase);
            if (graphicalExecution)
            {
                var graphical = summary.achieved?.graphical;
                if (graphical == null
                    || graphical.requested_objects != graphical.achieved_objects
                    || graphical.route_waypoints_requested != graphical.route_waypoints_reached
                    || !graphical.route_complete)
                {
                    diagnostic = "Attempt correctness/lifecycle failed: graphical object and route completion invariants did not pass.";
                    return;
                }
            }
            else if (job?.options?.IsServerOnlyDataMode == true
                && string.Equals(run.render?.mode, "server_only", StringComparison.OrdinalIgnoreCase))
            {
                var population = summary.achieved?.population;
                var expectedEntities = fixture?.population?.npcs?.accounting?.ecs_data_only_npcs ?? 0;
                if (population == null
                    || expectedEntities <= 0
                    || population.requested_ecs_entities != expectedEntities
                    || population.measurement_end_ecs_entities != expectedEntities
                    || population.post_cleanup_fixture_entities != 0
                    || population.post_cleanup_total_actor_entities != 0)
                {
                    diagnostic = "Attempt correctness/lifecycle failed: server population or post-cleanup invariants did not pass.";
                }
            }
            else
            {
                diagnostic = "Attempt correctness/lifecycle could not be qualified: execution mode is unsupported or its server/graphical identity is missing.";
            }
        }

        private static void AddAttempts(Dictionary<string, Test_PerformanceCampaignComparisonGroup> groups, List<Test_PerformanceCampaignAttemptView> attempts, bool baseline)
        {
            foreach (var attempt in attempts)
            {
                var key = attempt.GroupKey ?? "unclassified";
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new Test_PerformanceCampaignComparisonGroup
                    {
                        Label = key,
                        ProfileID = attempt.ProfileID,
                        IsGraphical = attempt.ProfileID == "P1-S0-06",
                    };
                    groups.Add(key, group);
                }
                else if (string.IsNullOrEmpty(group.ProfileID))
                {
                    group.ProfileID = attempt.ProfileID;
                    group.IsGraphical = attempt.ProfileID == "P1-S0-06";
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
            var samples = group.BaselineAttempts.Concat(group.CandidateAttempts)
                .Where(attempt => attempt.IsValid)
                .SelectMany(attempt => attempt.Metrics)
                .ToList();
            var requirements = GetRequiredMetricRequirements(group);
            foreach (var requirement in requirements)
            {
                var baselineSamples = group.BaselineAttempts
                    .Where(attempt => attempt.IsValid)
                    .SelectMany(attempt => attempt.Metrics)
                    .Where(requirement.Matches)
                    .ToList();
                var candidateSamples = group.CandidateAttempts
                    .Where(attempt => attempt.IsValid)
                    .SelectMany(attempt => attempt.Metrics)
                    .Where(requirement.Matches)
                    .ToList();
                var comparison = BuildMetricComparison(
                    baselineSamples,
                    candidateSamples,
                    requirement,
                    minimumValidRepeats,
                    group);
                group.Metrics.Add(comparison);
                group.HasInsufficientRequiredEvidence |= comparison.IsRequired
                    && (comparison.BaselineQualified < minimumValidRepeats || comparison.CandidateQualified < minimumValidRepeats);
            }

            foreach (var key in samples.Select(sample => sample.Key).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                var representative = samples.First(sample => sample.Key == key);
                if (requirements.Any(requirement => requirement.Matches(representative)))
                    continue;
                var baselineSamples = group.BaselineAttempts
                    .Where(attempt => attempt.IsValid)
                    .SelectMany(attempt => attempt.Metrics)
                    .Where(sample => sample.Key == key)
                    .ToList();
                var candidateSamples = group.CandidateAttempts
                    .Where(attempt => attempt.IsValid)
                    .SelectMany(attempt => attempt.Metrics)
                    .Where(sample => sample.Key == key)
                    .ToList();
                group.Metrics.Add(BuildMetricComparison(baselineSamples, candidateSamples, null, minimumValidRepeats, group));
            }
        }

        private static Test_PerformanceCampaignMetricComparison BuildMetricComparison(
            List<Test_PerformanceCampaignMetricSample> baselineSamples,
            List<Test_PerformanceCampaignMetricSample> candidateSamples,
            Test_PerformanceCampaignMetricRequirement requirement,
            int minimumValidRepeats,
            Test_PerformanceCampaignComparisonGroup group)
        {
            var first = baselineSamples.Concat(candidateSamples).FirstOrDefault();
            var comparison = new Test_PerformanceCampaignMetricComparison
            {
                MetricID = first?.MetricID ?? requirement?.MetricID,
                Unit = first?.Unit ?? requirement?.Unit,
                Source = first?.Source ?? requirement?.Source,
                Scope = first?.Scope ?? requirement?.Scope ?? UNKNOWN,
                SamplePolicy = first?.RetentionPolicy,
                Semantics = DescribeSemantics(first?.Unit ?? requirement?.Unit),
                Direction = DescribeDirection(first?.Unit ?? requirement?.Unit),
                IsRequired = requirement != null,
                RequiredStatistics = requirement?.DescribeStatistics(),
                BaselineAvailable = baselineSamples.Count,
                CandidateAvailable = candidateSamples.Count,
                BaselineP50Qualified = baselineSamples.Count(sample => sample.HasP50),
                CandidateP50Qualified = candidateSamples.Count(sample => sample.HasP50),
                BaselineP95Qualified = baselineSamples.Count(sample => sample.HasP95),
                CandidateP95Qualified = candidateSamples.Count(sample => sample.HasP95),
                BaselineP99Qualified = baselineSamples.Count(sample => sample.HasP99),
                CandidateP99Qualified = candidateSamples.Count(sample => sample.HasP99),
                Baseline = BuildDistribution(baselineSamples),
                Candidate = BuildDistribution(candidateSamples),
            };
            comparison.BaselineQualified = requirement == null
                ? comparison.BaselineP50Qualified
                : baselineSamples.Count(requirement.IsQualified);
            comparison.CandidateQualified = requirement == null
                ? comparison.CandidateP50Qualified
                : candidateSamples.Count(requirement.IsQualified);
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
            }
            if (comparison.IsRequired)
            {
                comparison.Caveat = AddReason(
                    comparison.Caveat,
                    comparison.BaselineQualified < minimumValidRepeats || comparison.CandidateQualified < minimumValidRepeats
                        ? "INSUFFICIENT EVIDENCE: required " + comparison.RequiredStatistics + " qualified N is " + comparison.BaselineQualified + "/" + comparison.CandidateQualified + ", minimum " + minimumValidRepeats + "."
                        : null);
            }
            else if (comparison.BaselineQualified < minimumValidRepeats || comparison.CandidateQualified < minimumValidRepeats)
            {
                comparison.Caveat = AddReason(comparison.Caveat, "Descriptive only: fewer than " + minimumValidRepeats + " qualified independent runs.");
            }
            comparison.Caveat = AddReason(comparison.Caveat, "Medians are across runs; p95/p99 are medians of per-run percentiles, not pooled samples.");
            comparison.Caveat = AddReason(comparison.Caveat, DescribeExclusions(group, requirement, first?.Key));
            return comparison;
        }

        private static List<Test_PerformanceCampaignMetricRequirement> GetRequiredMetricRequirements(Test_PerformanceCampaignComparisonGroup group)
        {
            if (group == null || (group.ProfileID != "P1-S0-04" && group.ProfileID != "P1-S0-06"))
                return new List<Test_PerformanceCampaignMetricRequirement>();
            if (group.ProfileID == "P1-S0-04")
            {
                return new List<Test_PerformanceCampaignMetricRequirement>
                {
                    Requirement("server.actor_manager.update_duration", "ms", "server", "server_update", "p99"),
                    Requirement("server.ecs.actor_update_duration", "ms", "server", "server_update", "p99"),
                    Requirement("server.memory.total_allocated_bytes", "bytes", "server", "server_update", "p99"),
                    Requirement("server.actor.full_entity_scan_count", "count", "server", "server_frame", "p50", "p95", "p99"),
                };
            }
            return new List<Test_PerformanceCampaignMetricRequirement>
            {
                Requirement("client.rendered.frame_time", "ms", "client", "rendered_frame", "p95", "p99"),
                Requirement("client.main_thread.frame_time", "ms", "client", "rendered_frame", "p99"),
                Requirement("client.render_thread.frame_time", "ms", "client", "rendered_frame", "p99"),
                Requirement("client.gpu.frame_time", "ms", "client", "rendered_frame", "p99"),
                Requirement("client.memory.total_allocated_bytes", "bytes", "client", "client_frame", "p50"),
                Requirement("client.graphics.draw_call_count", "count", "client", "rendered_frame", "p50"),
                Requirement("client.graphics.triangle_count", "count", "client", "rendered_frame", "p50"),
            };
        }

        private static Test_PerformanceCampaignMetricRequirement Requirement(string metricID, string unit, string source, string scope, params string[] statistics)
        {
            return new Test_PerformanceCampaignMetricRequirement
            {
                MetricID = metricID,
                Unit = unit,
                Source = source,
                Scope = scope,
                Statistics = statistics,
            };
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
            var maximums = samples.Where(sample => sample.HasMaximum).Select(sample => sample.Maximum).ToList();
            return new Test_PerformanceCampaignDistribution
            {
                Count = p50.Count,
                MedianP50 = Median(p50),
                MedianP95 = Median(p95),
                MedianP99 = p99.Count == 0 ? (double?)null : Median(p99),
                MinP50 = p50.Count == 0 ? 0d : p50[0],
                MaxP50 = p50.Count == 0 ? 0d : p50[p50.Count - 1],
                MinP95 = p95.Count == 0 ? 0d : p95[0],
                MaxP95 = p95.Count == 0 ? 0d : p95[p95.Count - 1],
                MinP99 = p99.Count == 0 ? (double?)null : p99[0],
                MaxP99 = p99.Count == 0 ? (double?)null : p99[p99.Count - 1],
                IqrP50 = Iqr(p50),
                CoefficientOfVariationP50 = CoefficientOfVariation(p50),
                MaximumObserved = maximums.Count == 0 ? (double?)null : maximums.Max(),
            };
        }

        private static double? CoefficientOfVariation(List<double> values)
        {
            if (values.Count < 2)
                return null;
            var mean = values.Average();
            if (Math.Abs(mean) < double.Epsilon)
                return null;
            var variance = values.Sum(value => Math.Pow(value - mean, 2d)) / (values.Count - 1);
            return Math.Sqrt(variance) / Math.Abs(mean);
        }

        private static string DescribeExclusions(
            Test_PerformanceCampaignComparisonGroup group,
            Test_PerformanceCampaignMetricRequirement requirement,
            string key)
        {
            var reasons = new List<string>();
            foreach (var attempt in group.BaselineAttempts.Concat(group.CandidateAttempts))
            {
                if (!attempt.IsValid)
                {
                    reasons.Add((attempt.RunID ?? attempt.AttemptID ?? "unknown run") + ": " + (attempt.Diagnostic ?? "attempt not valid"));
                    continue;
                }
                var metric = requirement == null
                    ? attempt.Metrics.FirstOrDefault(value => value.Key == key)
                    : attempt.Metrics.FirstOrDefault(requirement.Matches);
                if (metric == null || (metric != null && (requirement == null ? !metric.HasP50 : !requirement.IsQualified(metric))))
                    reasons.Add((attempt.RunID ?? attempt.AttemptID ?? "unknown run") + ": metric unavailable or below the required retention/statistic policy");
            }
            return reasons.Count == 0 ? null : "Exclusions: " + string.Join(" | ", reasons);
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
                var retainedSampleCount = metric.retained_sample_count > 0 ? metric.retained_sample_count : metric.sample_count;
                var measurementCoverage = metric.attempted_sample_count > 0
                    ? retainedSampleCount / (double)metric.attempted_sample_count
                    : (double?)null;
                var qualified = metric.observation == "measured"
                    && metric.sample_count > 0
                    && measurementCoverage.HasValue
                    && measurementCoverage.Value >= MIN_MEASUREMENT_COVERAGE;
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
                    AttemptedSampleCount = metric.attempted_sample_count,
                    RetainedSampleCount = retainedSampleCount,
                    MeasurementCoverage = measurementCoverage ?? 0d,
                    HasP50 = qualified,
                    HasP95 = qualified,
                    HasP99 = qualified && retainedSampleCount >= 100 && metric.reason != "insufficient_samples",
                    P50 = metric.p50,
                    P95 = metric.p95,
                    P99 = metric.p99,
                    HasMaximum = qualified,
                    Maximum = metric.max,
                });
            }
            return samples;
        }

        private static string BuildCoreGroupKey(Test_PerformanceCampaignJob job, Test_PerformanceComparisonRunDocument run)
        {
            var options = job?.options;
            var core = options == null
                ? job?.job_id ?? "unclassified"
                : string.Join("|", options.EffectiveProfileID, options.WorkloadMode, options.WorldFixture, options.GraphicalScenario, options.DensityTier, options.ActorDensity, options.ActorCount.ToString(CultureInfo.InvariantCulture), options.Seed.ToString(CultureInfo.InvariantCulture), Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS.ToString("R", CultureInfo.InvariantCulture), Test_PerformanceRun.RAMP_SECONDS.ToString("R", CultureInfo.InvariantCulture), options.WarmupSeconds.ToString("R", CultureInfo.InvariantCulture), options.MeasurementSeconds.ToString("R", CultureInfo.InvariantCulture), Test_PerformanceRun.RECOVERY_SECONDS.ToString("R", CultureInfo.InvariantCulture), options.ActorVariantID);
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
            if (string.IsNullOrEmpty(reason))
                return existing;
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
            if (unit == "m")
                return "distance; inspect route/fixture identity";
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
            public Test_PerformanceComparisonPhaseDocument phase_completion;
            public Test_PerformanceComparisonCorrectnessDocument correctness;
            public Test_PerformanceComparisonAchievedDocument achieved;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonPhaseDocument
        {
            public bool preflight;
            public bool ramp;
            public bool warmup;
            public bool measurement;
            public bool recovery;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonCorrectnessDocument
        {
            public string status;
            public int failed;
            public bool cleanup;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonAchievedDocument
        {
            public Test_PerformanceComparisonPopulationDocument population;
            public Test_PerformanceComparisonGraphicalDocument graphical;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonPopulationDocument
        {
            public int requested_ecs_entities;
            public int measurement_end_ecs_entities;
            public int post_cleanup_fixture_entities;
            public int post_cleanup_total_actor_entities;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonGraphicalDocument
        {
            public int requested_objects;
            public int achieved_objects;
            public int route_waypoints_requested;
            public int route_waypoints_reached;
            public bool route_complete;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonFixtureDocument
        {
            public Test_PerformanceComparisonFixturePopulationDocument population;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonFixturePopulationDocument
        {
            public Test_PerformanceComparisonFixtureNpcDocument npcs;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonFixtureNpcDocument
        {
            public Test_PerformanceComparisonFixtureAccountingDocument accounting;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonFixtureAccountingDocument
        {
            public int ecs_data_only_npcs;
        }

        [Serializable]
        private sealed class Test_PerformanceComparisonMetricDocument
        {
            public string metric_id;
            public string unit;
            public string observation;
            public string phase;
            public int sample_count;
            public int attempted_sample_count;
            public int retained_sample_count;
            public double p50;
            public double p95;
            public double p99;
            public double max;
            public double measurement_coverage;
            public string reason;
            public string source;
            public string scope;
            public string retention_policy;
        }

        #pragma warning restore 0649
    }
}
