using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Tests.Performance.Harness
{
    internal static class Test_PerformanceCampaignState
    {
        public const string DRAFT = "draft";
        public const string RUNNING = "running";
        public const string PAUSED = "paused";
        public const string COMPLETED = "completed";
        public const string CANCELLED = "cancelled";
    }

    internal static class Test_PerformanceCampaignAttemptState
    {
        public const string QUEUED = "queued";
        public const string RUNNING = "running";
        public const string COMPLETED = "completed";
        public const string FAILED = "failed";
        public const string BLOCKED = "blocked";
        public const string SKIPPED = "skipped";
        public const string CANCELLED = "cancelled";
        public const string UNVERIFIED = "unverified";
        public const string INCOMPLETE = "incomplete";
    }

    internal static class Test_PerformanceCampaignStopPolicy
    {
        public const string STOP_ON_FAILURE = "stop_on_failure";
        public const string CONTINUE_AFTER_FAILURE = "continue_after_failure";
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignSource
    {
        public string captured_utc;
        public string git_branch;
        public string git_branch_status;
        public string git_branch_diagnostic;
        public string commit_sha;
        public string source_sha256;
        public bool dirty;
        public string diff_sha256;
        public string packages_sha256;
        public string packages_lock_sha256;
        public string purrnet_sha256;
        public string hardware;
        public string operating_system;
        public string unity_version;
        public string graphics_device;
        public string graphics_settings;

        public string DescribeDifference(Test_PerformanceCampaignSource current)
        {
            if (current == null)
                return "The current source identity could not be captured.";

            var differences = new List<string>();
            if (!string.IsNullOrEmpty(git_branch) && !string.IsNullOrEmpty(current.git_branch))
                AddDifference(differences, "Git branch", git_branch, current.git_branch);
            else if (!string.Equals(git_branch, current.git_branch, StringComparison.OrdinalIgnoreCase))
                differences.Add($"Git branch changed from '{git_branch ?? "unknown/not recorded"}' to '{current.git_branch ?? "unknown/not recorded"}'");
            AddDifference(differences, "commit SHA", commit_sha, current.commit_sha);
            AddDifference(differences, "Packages manifest SHA", packages_sha256, current.packages_sha256);
            AddDifference(differences, "packages lock SHA", packages_lock_sha256, current.packages_lock_sha256);
            AddDifference(differences, "PurrNet SHA", purrnet_sha256, current.purrnet_sha256);
            AddDifference(differences, "hardware", hardware, current.hardware);
            AddDifference(differences, "operating system", operating_system, current.operating_system);
            AddDifference(differences, "Unity version", unity_version, current.unity_version);
            AddDifference(differences, "graphics device", graphics_device, current.graphics_device);
            AddDifference(differences, "graphics settings", graphics_settings, current.graphics_settings);
            return differences.Count == 0 ? null : string.Join("; ", differences);
        }

        private static void AddDifference(List<string> differences, string label, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                differences.Add($"{label} changed from '{expected}' to '{actual}'");
        }
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignFixtureIdentity
    {
        public string configuration_sha256;
        public string profile_id;
        public string profile_revision;
        public string actor_variant_id;
        public string world_fixture_id;
        public string world_scene_path;
        public string world_scene_guid;
        public string world_scene_hash;
        public string graphical_plan_hash;
        public string graphical_route_id;

        public string DescribeDifference(Test_PerformanceCampaignFixtureIdentity current)
        {
            if (current == null)
                return "The current fixture/configuration identity could not be captured.";
            if (string.Equals(configuration_sha256, current.configuration_sha256, StringComparison.OrdinalIgnoreCase))
                return null;
            return $"Fixture/configuration identity changed from '{configuration_sha256}' to '{current.configuration_sha256}'.";
        }
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignJob
    {
        public string job_id;
        public Test_PerformanceHarnessOptions options;
        public int repeat_count = 1;
        public string validation_state = "unvalidated";
        public string validation_diagnostic;
        public Test_PerformanceCampaignFixtureIdentity fixture_identity;

        public string DisplayName
        {
            get
            {
                if (options == null)
                    return job_id ?? "invalid job";
                var scenario = options.IsGraphicalClient ? options.GraphicalScenario.ToString() : options.WorldFixture.ToString();
                return $"{options.EffectiveProfileID} / {options.WorkloadMode} / {scenario}";
            }
        }

        public Test_PerformanceCampaignJob Clone(string newJobID)
        {
            return new Test_PerformanceCampaignJob
            {
                job_id = newJobID,
                options = options?.Clone(),
                repeat_count = repeat_count,
                validation_state = validation_state,
                validation_diagnostic = validation_diagnostic,
                fixture_identity = fixture_identity,
            };
        }
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignAttempt
    {
        public string attempt_id;
        public string job_id;
        public int repeat_index;
        public string state = Test_PerformanceCampaignAttemptState.QUEUED;
        public string started_utc;
        public string finished_utc;
        public string run_id;
        public string result_path;
        public string report_path;
        public string result_status;
        public string stop_reason;
        public string diagnostic;
        public Test_PerformanceCampaignSource source;
        public string dispatch_utc;
        public string queue_settled_utc;
        public double dispatch_to_queue_settled_seconds = -1d;
    }

    [Serializable]
    internal sealed class Test_PerformanceProfileCoverage
    {
        public string profile_id;
        public string mode;
        public string status = "BLOCKED";
        public string capability;
        public string runnable_alternative;
        public string next_issue_or_stage;
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignManifest
    {
        public string schema_version = "performance-campaign/v1";
        public string campaign_id;
        public string created_utc;
        public string updated_utc;
        public string started_utc;
        public string finished_utc;
        public string state = Test_PerformanceCampaignState.DRAFT;
        public string stop_policy = Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE;
        public string pause_reason;
        public string queue_diagnostic;
        public bool stop_after_current;
        public bool cancel_remaining;
        public Test_PerformanceCampaignSource source;
        public List<string> excluded_scenarios = new();
        public List<Test_PerformanceProfileCoverage> profile_coverage = new();
        public string linked_campaign_id;
        public string makeup_for_job_id;
        public List<Test_PerformanceCampaignJob> jobs = new();
        public List<Test_PerformanceCampaignAttempt> attempts = new();
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignIndex
    {
        public string schema_version = "performance-campaign-index/v1";
        public string campaign_id;
        public string updated_utc;
        public string state;
        public string stop_policy;
        public string linked_campaign_id;
        public Test_PerformanceCampaignSource source;
        public List<Test_PerformanceProfileCoverage> profile_coverage = new();
        public List<Test_PerformanceCampaignIndexAttempt> attempts = new();
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignIndexAttempt
    {
        public string job_id;
        public int repeat_index;
        public string run_id;
        public string result_path;
        public string status;
        public string stop_reason;
        public Test_PerformanceCampaignSource source;
        public string profile_id;
        public string fixture_sha256;
        public string seed;
        public string configuration_sha256;
        public string hardware;
        public string unity_version;
        public string graphics_device;
        public string started_utc;
        public string finished_utc;
        public string correctness_status;
        public string cleanup_status;
        public string artifact_index_status;
        public Test_PerformanceCampaignIndexArtifact[] artifacts;
    }

    [Serializable]
    internal sealed class Test_PerformanceCampaignIndexArtifact
    {
        public string path;
        public string sha256;
        public long bytes;
        public bool required;
        public bool optional;
        public string status;
    }

    /// <summary>Persists and sequentially coordinates test-owned performance campaign jobs.</summary>
    [InitializeOnLoad]
    internal static class Test_PerformanceJobQueue
    {
        public const int MAX_JOBS = 50;
        public const int MAX_REPEAT_COUNT = 20;
        public const int MAX_TOTAL_ATTEMPTS = 100;

        private const string CAMPAIGNS_DIRECTORY_NAME = "Campaigns";
        private const string CAMPAIGN_FILE_NAME = "campaign.json";
        private const string INDEX_FILE_NAME = "index.json";
        private const string VALID = "valid";
        private const string BLOCKED = "blocked";
        private const string UNVALIDATED = "unvalidated";

        private static Test_PerformanceCampaignManifest _manifest;
        private static string _campaignDirectory;
        private static string _restoreDiagnostic;
        private static int _activeAttemptIndex = -1;
        private static bool _restoreScheduled;
        private static bool _advanceScheduled;
        private static bool _finalizeScheduled;
        private static string _lastCompletedRunID;
        private static long _activeAttemptDispatchTimestamp;

        static Test_PerformanceJobQueue()
        {
            Test_PerformanceHarnessController.StateChanged += OnControllerStateChanged;
            Test_PerformanceHarnessController.LifecycleSettled += OnControllerLifecycleSettled;
            EditorApplication.delayCall += RestorePersistedCampaign;
        }

        public static event Action StateChanged;

        public static bool HasCampaign => _manifest != null;

        public static bool CanEdit => _manifest == null || string.Equals(_manifest.state, Test_PerformanceCampaignState.DRAFT, StringComparison.Ordinal);

        public static bool CanCreateNewDraft => !IsRunning;

        public static bool IsRunning => _manifest != null && string.Equals(_manifest.state, Test_PerformanceCampaignState.RUNNING, StringComparison.Ordinal);

        public static string CampaignID => _manifest?.campaign_id;

        public static string CampaignState => _manifest?.state;

        public static string StopPolicy => _manifest?.stop_policy ?? Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE;

        public static string PauseReason => _manifest?.pause_reason;

        public static string QueueDiagnostic => _restoreDiagnostic ?? _manifest?.queue_diagnostic;

        public static string SourceSummary
        {
            get
            {
                if (_manifest?.source == null)
                    return "not captured";
                var branch = string.IsNullOrEmpty(_manifest.source.git_branch)
                    ? string.IsNullOrEmpty(_manifest.source.git_branch_status) ? "unknown" : _manifest.source.git_branch_status
                    : _manifest.source.git_branch;
                var abbreviatedSHA = string.IsNullOrEmpty(_manifest.source.commit_sha)
                    ? "unknown"
                    : _manifest.source.commit_sha.Substring(0, Math.Min(12, _manifest.source.commit_sha.Length));
                return $"{branch} @ {abbreviatedSHA} / {(_manifest.source.dirty ? "dirty (informational)" : "clean (informational)")}";
            }
        }

        public static string SourceDetails
        {
            get
            {
                if (_manifest?.source == null)
                    return "Source provenance was not recorded.";
                var source = _manifest.source;
                var branch = string.IsNullOrEmpty(source.git_branch) ? "unknown/not recorded" : source.git_branch;
                var branchState = string.IsNullOrEmpty(source.git_branch_status) ? "unknown" : source.git_branch_status;
                var diagnostic = string.IsNullOrEmpty(source.git_branch_diagnostic) ? string.Empty : $"; diagnostic {source.git_branch_diagnostic}";
                return $"branch {branch} ({branchState}); commit {source.commit_sha ?? "unknown"}; dirty {source.dirty} (informational); diff {source.diff_sha256 ?? "unknown"} (informational); captured {source.captured_utc ?? "unknown"}{diagnostic}";
            }
        }

        public static IReadOnlyList<Test_PerformanceCampaignJob> Jobs => _manifest == null ? (IReadOnlyList<Test_PerformanceCampaignJob>)Array.Empty<Test_PerformanceCampaignJob>() : _manifest.jobs;

        public static IReadOnlyList<Test_PerformanceCampaignAttempt> Attempts => _manifest == null ? (IReadOnlyList<Test_PerformanceCampaignAttempt>)Array.Empty<Test_PerformanceCampaignAttempt>() : _manifest.attempts;

        public static IReadOnlyList<string> ExcludedScenarios => _manifest == null ? (IReadOnlyList<string>)Array.Empty<string>() : _manifest.excluded_scenarios;

        public static IReadOnlyList<Test_PerformanceProfileCoverage> ProfileCoverage => _manifest == null
            ? (IReadOnlyList<Test_PerformanceProfileCoverage>)Array.Empty<Test_PerformanceProfileCoverage>()
            : _manifest.profile_coverage;

        public static int TotalAttempts => _manifest == null ? 0 : _manifest.attempts.Count > 0 ? _manifest.attempts.Count : CountRequestedAttempts();

        public static int CompletedCount => CountAttempts(Test_PerformanceCampaignAttemptState.COMPLETED);
        public static int RunningCount => CountAttempts(Test_PerformanceCampaignAttemptState.RUNNING);
        public static int FailedCount => CountAttempts(Test_PerformanceCampaignAttemptState.FAILED);
        public static int BlockedCount => CountAttempts(Test_PerformanceCampaignAttemptState.BLOCKED);
        public static int SkippedCount => CountAttempts(Test_PerformanceCampaignAttemptState.SKIPPED);
        public static int CancelledCount => CountAttempts(Test_PerformanceCampaignAttemptState.CANCELLED);
        public static int UnverifiedCount => CountAttempts(Test_PerformanceCampaignAttemptState.UNVERIFIED) + CountAttempts(Test_PerformanceCampaignAttemptState.INCOMPLETE);
        public static int QueuedCount => _manifest == null
            ? 0
            : _manifest.attempts.Count > 0
                ? CountAttempts(Test_PerformanceCampaignAttemptState.QUEUED)
                : CountRequestedAttempts();

        public static string ProgressText
        {
            get
            {
                var settled = CompletedCount + FailedCount + BlockedCount + SkippedCount + CancelledCount + UnverifiedCount;
                return $"{settled}/{TotalAttempts} attempts settled";
            }
        }

        public static Test_PerformanceCampaignAttempt CurrentAttempt
        {
            get
            {
                if (_manifest == null || _activeAttemptIndex < 0 || _activeAttemptIndex >= _manifest.attempts.Count)
                    return null;
                return _manifest.attempts[_activeAttemptIndex];
            }
        }

        public static string CurrentAttemptIndex
        {
            get
            {
                return _activeAttemptIndex < 0 ? null : $"{_activeAttemptIndex + 1}/{TotalAttempts}";
            }
        }

        public static string CurrentJobDisplayName => CurrentAttempt == null ? null : FindJob(CurrentAttempt.job_id)?.DisplayName;

        public static bool AddCurrentConfiguration(Test_PerformanceHarnessOptions options, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be edited.", out diagnostic);
            if (options == null)
                return FailUI("A current workload configuration is required.", out diagnostic);
            if (!EnsureDraftCampaign(out diagnostic))
                return false;
            if (_manifest.jobs.Count >= MAX_JOBS)
                return FailUI($"The queue is limited to {MAX_JOBS} jobs.", out diagnostic);

            var job = BuildJob(options.Clone(), out var validationDiagnostic);
            _manifest.jobs.Add(job);
            _manifest.queue_diagnostic = validationDiagnostic;
            Persist();
            NotifyChanged();
            diagnostic = validationDiagnostic;
            return true;
        }

        public static bool SetRepeatCount(string jobID, int repeatCount, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be edited.", out diagnostic);
            if (repeatCount < 1 || repeatCount > MAX_REPEAT_COUNT)
                return FailUI($"Repeat count must be between 1 and {MAX_REPEAT_COUNT}.", out diagnostic);
            var job = FindJob(jobID);
            if (job == null)
                return FailUI("The selected queue job no longer exists.", out diagnostic);
            if (CountRequestedAttempts(jobID, repeatCount) > MAX_TOTAL_ATTEMPTS)
                return FailUI($"The queue is limited to {MAX_TOTAL_ATTEMPTS} total attempts.", out diagnostic);
            job.repeat_count = repeatCount;
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool DuplicateJob(string jobID, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be edited.", out diagnostic);
            if (_manifest == null || _manifest.jobs.Count >= MAX_JOBS)
                return FailUI($"The queue is limited to {MAX_JOBS} jobs.", out diagnostic);
            var index = FindJobIndex(jobID);
            if (index < 0)
                return FailUI("The selected queue job no longer exists.", out diagnostic);
            var duplicate = _manifest.jobs[index].Clone(CreateID("job"));
            _manifest.jobs.Insert(index + 1, duplicate);
            if (CountRequestedAttempts() > MAX_TOTAL_ATTEMPTS)
            {
                _manifest.jobs.RemoveAt(index + 1);
                return FailUI($"The queue is limited to {MAX_TOTAL_ATTEMPTS} total attempts.", out diagnostic);
            }
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool RemoveJob(string jobID, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be edited.", out diagnostic);
            var index = FindJobIndex(jobID);
            if (index < 0)
                return FailUI("The selected queue job no longer exists.", out diagnostic);
            _manifest.jobs.RemoveAt(index);
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool MoveJob(string jobID, int offset, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be edited.", out diagnostic);
            var index = FindJobIndex(jobID);
            var targetIndex = index + offset;
            if (index < 0 || targetIndex < 0 || targetIndex >= _manifest.jobs.Count)
                return false;
            var job = _manifest.jobs[index];
            _manifest.jobs.RemoveAt(index);
            _manifest.jobs.Insert(targetIndex, job);
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool ClearDraft(out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can be cleared.", out diagnostic);
            if (_manifest != null)
            {
                _manifest.state = Test_PerformanceCampaignState.CANCELLED;
                _manifest.pause_reason = "Draft cleared by the user.";
                Persist();
            }
            _manifest = null;
            _campaignDirectory = null;
            _restoreDiagnostic = null;
            NotifyChanged();
            return true;
        }

        public static bool CreateNewDraft(out string diagnostic)
        {
            diagnostic = null;
            if (!CanCreateNewDraft)
                return FailUI("The current campaign is still running.", out diagnostic);
            _manifest = null;
            _campaignDirectory = null;
            _restoreDiagnostic = null;
            if (!EnsureDraftCampaign(out diagnostic))
                return false;
            NotifyChanged();
            return true;
        }

        public static bool CreateMakeupCampaign(string jobID, int repeatCount, out string diagnostic)
        {
            diagnostic = null;
            if (!CanCreateNewDraft)
                return FailUI("The current campaign is still running.", out diagnostic);
            if (_manifest == null
                || (_manifest.state != Test_PerformanceCampaignState.COMPLETED
                    && _manifest.state != Test_PerformanceCampaignState.CANCELLED
                    && _manifest.state != Test_PerformanceCampaignState.PAUSED))
            {
                return FailUI("A make-up campaign can only be created from a settled campaign.", out diagnostic);
            }
            if (repeatCount < 1 || repeatCount > MAX_REPEAT_COUNT)
                return FailUI($"Make-up repeats must be between 1 and {MAX_REPEAT_COUNT}.", out diagnostic);
            var sourceManifest = _manifest;
            var sourceCampaignID = sourceManifest.campaign_id;
            var sourceJob = FindJob(jobID);
            if (sourceJob == null)
                return FailUI("The selected make-up workload no longer exists.", out diagnostic);
            _manifest = null;
            _campaignDirectory = null;
            if (!EnsureDraftCampaign(out diagnostic))
            {
                _manifest = sourceManifest;
                return false;
            }

            var makeupJob = sourceJob.Clone(CreateID("job"));
            makeupJob.repeat_count = repeatCount;
            _manifest.linked_campaign_id = sourceCampaignID;
            _manifest.makeup_for_job_id = jobID;
            _manifest.jobs.Add(makeupJob);
            _manifest.queue_diagnostic = "Make-up repeats preserve the original campaign; every attempt receives new evidence and a new run ID.";
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool SetStopPolicy(string policy, out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can change its failure policy.", out diagnostic);
            if (policy != Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE
                && policy != Test_PerformanceCampaignStopPolicy.CONTINUE_AFTER_FAILURE)
            {
                return FailUI("The selected failure policy is invalid.", out diagnostic);
            }
            if (!EnsureDraftCampaign(out diagnostic))
                return false;
            _manifest.stop_policy = policy;
            Persist();
            NotifyChanged();
            return true;
        }

        public static bool LoadSupportedBaselinePreset(out string diagnostic)
        {
            diagnostic = null;
            if (!CanEdit)
                return FailUI("Only a draft campaign can load a preset.", out diagnostic);
            if (!EnsureDraftCampaign(out diagnostic))
                return false;

            var candidates = new List<Test_PerformanceHarnessOptions>
            {
                CreateServerBaseline(Test_PerformanceWorkloadMode.EcsDataOnlyMinimal),
                CreateServerBaseline(Test_PerformanceWorkloadMode.EcsDataOnlyActivity),
                CreateGraphicalBaseline(Test_GraphicalScenario.Outdoor),
                CreateGraphicalBaseline(Test_GraphicalScenario.Forest),
                CreateGraphicalBaseline(Test_GraphicalScenario.Settlement),
            };
            _manifest.jobs.Clear();
            _manifest.excluded_scenarios.Clear();
            _manifest.profile_coverage = BuildProfileCoverage();
            _manifest.queue_diagnostic = null;
            foreach (var candidate in candidates)
            {
                var job = BuildJob(candidate, out var validationDiagnostic);
                if (job.validation_state != VALID)
                {
                    _manifest.excluded_scenarios.Add(job.DisplayName + ": " + validationDiagnostic);
                    continue;
                }
                job.repeat_count = 5;
                _manifest.jobs.Add(job);
            }
            if (_manifest.jobs.Count == 0)
                _manifest.queue_diagnostic = "No supported baseline scenario passed the current fixture and Editor capability checks.";
            else if (CountRequestedAttempts() > MAX_TOTAL_ATTEMPTS)
                _manifest.queue_diagnostic = $"The preset exceeds the bounded {MAX_TOTAL_ATTEMPTS}-attempt queue limit.";
            Persist();
            NotifyChanged();
            diagnostic = _manifest.queue_diagnostic;
            return _manifest.jobs.Count > 0;
        }

        public static bool StartOrResume(out string diagnostic)
        {
            diagnostic = null;
            if (_manifest == null)
                return FailUI("Add at least one workload configuration before starting a queue.", out diagnostic);
            if (IsRunning)
                return true;
            if (_manifest.state == Test_PerformanceCampaignState.COMPLETED || _manifest.state == Test_PerformanceCampaignState.CANCELLED)
                return FailUI("This campaign is already terminal; create a new draft for another campaign.", out diagnostic);
            if (Test_PerformanceHarnessController.IsRunning)
                return FailUI("A single performance run is already active.", out diagnostic);
            var wasPaused = _manifest.state == Test_PerformanceCampaignState.PAUSED;
            if (!PrepareCampaignForLaunch(out diagnostic))
                return false;
            _manifest.state = Test_PerformanceCampaignState.RUNNING;
            _manifest.pause_reason = null;
            _manifest.queue_diagnostic = null;
            if (string.IsNullOrEmpty(_manifest.started_utc))
                _manifest.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            _manifest.finished_utc = null;
            _manifest.stop_after_current = false;
            _manifest.cancel_remaining = false;
            if (!wasPaused && _manifest.stop_policy == Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE && BlockedCount > 0)
            {
                _manifest.state = Test_PerformanceCampaignState.PAUSED;
                _manifest.pause_reason = "Queue paused before launch because prevalidation blocked one or more attempts.";
                Persist();
                NotifyChanged();
                return true;
            }
            Persist();
            NotifyChanged();
            ScheduleAdvance();
            return true;
        }

        public static void StopAfterCurrent()
        {
            if (!IsRunning)
                return;
            _manifest.stop_after_current = true;
            _manifest.pause_reason = "The queue will pause after the current attempt finalizes.";
            Persist();
            NotifyChanged();
        }

        public static void StopCurrentAndCancelRemaining()
        {
            if (!IsRunning)
                return;
            _manifest.cancel_remaining = true;
            _manifest.stop_after_current = true;
            _manifest.pause_reason = "The current attempt is stopping; remaining attempts will be cancelled.";
            CancelQueuedAttempts();
            if (Test_PerformanceHarnessController.IsRunning)
                Test_PerformanceHarnessController.RequestStop();
            else
                FinishCampaignAsCancelled();
            Persist();
            NotifyChanged();
        }

        public static bool CancelPausedCampaign(out string diagnostic)
        {
            diagnostic = null;
            if (_manifest == null)
                return FailUI("No campaign is available to cancel.", out diagnostic);
            if (_manifest.state != Test_PerformanceCampaignState.PAUSED)
                return FailUI("Only a paused campaign can be cancelled from this state.", out diagnostic);

            _manifest.cancel_remaining = true;
            _manifest.stop_after_current = true;
            CancelQueuedAttempts();
            FinishCampaignAsCancelled();
            return true;
        }

        public static bool CanOpenReport(Test_PerformanceCampaignAttempt attempt) => attempt != null && IsSafeResultFile(attempt.report_path);

        public static bool CanOpenFolder(Test_PerformanceCampaignAttempt attempt) => attempt != null && IsSafeResultDirectory(attempt.result_path);

        public static List<string> BuildScenarioSummaryLines()
        {
            var lines = new List<string>();
            if (_manifest == null)
                return lines;
            foreach (var job in _manifest.jobs)
            {
                var completed = 0;
                var blocked = 0;
                var unverified = 0;
                foreach (var attempt in _manifest.attempts)
                {
                    if (attempt.job_id != job.job_id)
                        continue;
                    if (attempt.state == Test_PerformanceCampaignAttemptState.COMPLETED)
                        completed++;
                    else if (attempt.state == Test_PerformanceCampaignAttemptState.BLOCKED)
                        blocked++;
                    else if (attempt.state == Test_PerformanceCampaignAttemptState.UNVERIFIED || attempt.state == Test_PerformanceCampaignAttemptState.INCOMPLETE)
                        unverified++;
                }
                lines.Add($"{job.DisplayName}: valid completed {completed}/{job.repeat_count}; blocked {blocked}; unverified {unverified}");
            }
            foreach (var profile in _manifest.profile_coverage ?? new List<Test_PerformanceProfileCoverage>())
            {
                lines.Add($"{profile.profile_id} / {profile.mode}: {profile.status}; {profile.capability}; alternative: {profile.runnable_alternative}; next: {profile.next_issue_or_stage}");
            }
            return lines;
        }

        public static void OpenReport(Test_PerformanceCampaignAttempt attempt)
        {
            if (CanOpenReport(attempt))
                EditorUtility.OpenWithDefaultApp(attempt.report_path);
        }

        public static void OpenFolder(Test_PerformanceCampaignAttempt attempt)
        {
            if (CanOpenFolder(attempt))
                EditorUtility.RevealInFinder(attempt.result_path);
        }

        private static void RestorePersistedCampaign()
        {
            if (_restoreScheduled)
                return;
            _restoreScheduled = true;
            try
            {
                var campaignRoot = GetCampaignRoot();
                if (!Directory.Exists(campaignRoot))
                    return;
                if ((File.GetAttributes(campaignRoot) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("The campaign root is a reparse point and was not read.");

                Test_PerformanceCampaignManifest latest = null;
                string latestDirectory = null;
                var directories = Directory.GetDirectories(campaignRoot);
                Array.Sort(directories, StringComparer.Ordinal);
                foreach (var directory in directories)
                {
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("A campaign directory is a reparse point and was not read: " + directory);
                    var campaignPath = Path.Combine(directory, CAMPAIGN_FILE_NAME);
                    if (!File.Exists(campaignPath))
                        continue;
                    if ((File.GetAttributes(campaignPath) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("A campaign manifest is a reparse point and was not read: " + campaignPath);
                    Test_PerformanceCampaignManifest candidate;
                    try
                    {
                        candidate = JsonUtility.FromJson<Test_PerformanceCampaignManifest>(File.ReadAllText(campaignPath));
                        ValidateManifest(candidate, directory);
                    }
                    catch (Exception exception)
                    {
                        _restoreDiagnostic = "A persisted campaign could not be restored: " + exception.Message;
                        return;
                    }
                    if (latest == null || CompareCampaignTimestamp(candidate, latest) > 0)
                    {
                        latest = candidate;
                        latestDirectory = directory;
                    }
                }

                _manifest = latest;
                _campaignDirectory = latestDirectory;
                if (_manifest == null)
                    return;
                RestoreRuntimeAttemptState();
                NotifyChanged();
            }
            catch (Exception exception)
            {
                _restoreDiagnostic = "Campaign discovery failed closed: " + exception.Message;
            }
            finally
            {
                _restoreScheduled = false;
            }
        }

        private static void RestoreRuntimeAttemptState()
        {
            _activeAttemptIndex = FindAttemptIndex(Test_PerformanceCampaignAttemptState.RUNNING);
            if (!IsRunning)
                return;
            if (_activeAttemptIndex < 0)
            {
                PauseCampaign("The Editor restarted before the next queued attempt launched; explicit resume is required.");
                return;
            }
            var attempt = _manifest.attempts[_activeAttemptIndex];
            if (Test_PerformanceHarnessController.IsRunning
                && (string.IsNullOrEmpty(attempt.run_id)
                    || string.Equals(attempt.run_id, Test_PerformanceHarnessController.ActiveRunID, StringComparison.Ordinal)))
            {
                return;
            }

            if (TryReconcileRecoveredFinalizedAttempt(attempt))
                return;

            attempt.state = Test_PerformanceCampaignAttemptState.INCOMPLETE;
            attempt.diagnostic = "The Editor restarted or the pending runner could not be restored; the attempt was not repeated.";
            _manifest.state = Test_PerformanceCampaignState.PAUSED;
            _manifest.pause_reason = attempt.diagnostic;
            _activeAttemptIndex = -1;
            Persist();
        }

        private static bool TryReconcileRecoveredFinalizedAttempt(Test_PerformanceCampaignAttempt attempt)
        {
            if (attempt == null
                || string.IsNullOrEmpty(attempt.run_id)
                || !string.Equals(attempt.run_id, Test_PerformanceHarnessController.LastCompletedRunID, StringComparison.Ordinal)
                || string.IsNullOrEmpty(Test_PerformanceHarnessController.LastCompletedResultDirectory))
            {
                return false;
            }

            attempt.result_path = Test_PerformanceHarnessController.LastCompletedResultDirectory;
            attempt.report_path = Test_PerformanceHarnessController.LastReportPath ?? attempt.report_path;
            FinalizeActiveAttempt();
            return true;
        }

        private static bool PrepareCampaignForLaunch(out string diagnostic)
        {
            diagnostic = null;
            if (_manifest.jobs.Count == 0)
                return FailUI("The queue is empty.", out diagnostic);
            if (CountRequestedAttempts() > MAX_TOTAL_ATTEMPTS)
                return FailUI($"The queue is limited to {MAX_TOTAL_ATTEMPTS} total attempts.", out diagnostic);

            var source = CaptureSourceIdentity(out diagnostic);
            if (source == null)
                return false;
            var campaignHasStarted = _manifest.attempts.Count > 0
                && CountAttempts(Test_PerformanceCampaignAttemptState.QUEUED) < _manifest.attempts.Count;
            if (_manifest.source != null && campaignHasStarted)
            {
                var sourceDifference = _manifest.source.DescribeDifference(source);
                if (sourceDifference != null)
                    return FailCampaignClosed($"Source identity changed; the campaign will not mix results. {sourceDifference}", out diagnostic);
            }

            var validJobCount = 0;
            foreach (var job in _manifest.jobs)
            {
                if (!TryBuildFixtureIdentity(job.options, out var identity, out var jobDiagnostic))
                {
                    job.validation_state = BLOCKED;
                    job.validation_diagnostic = jobDiagnostic;
                    continue;
                }
                if (job.fixture_identity != null && campaignHasStarted)
                {
                    var difference = job.fixture_identity.DescribeDifference(identity);
                    if (difference != null)
                    {
                        job.validation_state = BLOCKED;
                        job.validation_diagnostic = "The queued fixture/configuration changed before launch. " + difference;
                        continue;
                    }
                }
                job.fixture_identity = identity;
                job.validation_state = VALID;
                job.validation_diagnostic = null;
                validJobCount++;
            }

            if (validJobCount == 0)
                return FailUI("No queued job passed fixture and configuration validation.", out diagnostic);
            if (!TryValidateControllerPrerequisite(_manifest.jobs, out diagnostic))
            {
                Persist();
                NotifyChanged();
                return false;
            }

            if (!campaignHasStarted)
                _manifest.source = source;
            BuildAttemptsIfNeeded();
            MarkInvalidJobsBlocked();
            Persist();
            return true;
        }

        private static bool TryValidateControllerPrerequisite(List<Test_PerformanceCampaignJob> jobs, out string diagnostic)
        {
            diagnostic = null;
            var validJobCount = 0;
            foreach (var job in jobs)
            {
                if (job.validation_state != VALID)
                    continue;
                if (Test_PerformanceHarnessController.TryValidateLaunchPrerequisites(job.options, out diagnostic))
                {
                    validJobCount++;
                    continue;
                }

                job.validation_state = BLOCKED;
                job.validation_diagnostic = diagnostic;
            }

            if (validJobCount > 0)
                return true;

            diagnostic = "No valid queue job remains after preflight validation.";
            return false;
        }

        private static void BuildAttemptsIfNeeded()
        {
            if (_manifest.attempts.Count > 0)
                return;
            foreach (var job in _manifest.jobs)
            {
                for (var repeatIndex = 1; repeatIndex <= job.repeat_count; repeatIndex++)
                {
                    _manifest.attempts.Add(new Test_PerformanceCampaignAttempt
                    {
                        attempt_id = CreateID("attempt"),
                        job_id = job.job_id,
                        repeat_index = repeatIndex,
                    });
                }
            }
        }

        private static void MarkInvalidJobsBlocked()
        {
            foreach (var attempt in _manifest.attempts)
            {
                if (attempt.state != Test_PerformanceCampaignAttemptState.QUEUED)
                    continue;
                var job = FindJob(attempt.job_id);
                if (job == null || job.validation_state == VALID)
                    continue;
                attempt.state = Test_PerformanceCampaignAttemptState.BLOCKED;
                attempt.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                attempt.result_status = "BLOCKED-PREREQUISITE";
                attempt.stop_reason = "QUEUE_PREVALIDATION_BLOCKED";
                attempt.diagnostic = job.validation_diagnostic;
            }
        }

        private static void ScheduleAdvance()
        {
            if (_advanceScheduled)
                return;
            _advanceScheduled = true;
            EditorApplication.delayCall += AdvanceAfterEditorTurn;
        }

        private static void AdvanceAfterEditorTurn()
        {
            _advanceScheduled = false;
            if (!IsRunning || _activeAttemptIndex >= 0 || Test_PerformanceHarnessController.IsRunning)
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (_manifest.stop_after_current)
            {
                PauseCampaign("Queue paused before the next attempt as requested.");
                return;
            }
            StartNextAttempt();
        }

        private static void StartNextAttempt()
        {
            var attemptIndex = FindAttemptIndex(Test_PerformanceCampaignAttemptState.QUEUED);
            if (attemptIndex < 0)
            {
                if (UnverifiedCount > 0 || FailedCount > 0 || BlockedCount > 0)
                    PauseCampaign("The queue reached the end with failed, blocked or unverified attempts.");
                else
                    FinishCampaignAsCompleted();
                return;
            }

            var attempt = _manifest.attempts[attemptIndex];
            var job = FindJob(attempt.job_id);
            if (job == null)
            {
                SetAttemptFailed(attempt, "The campaign manifest references a missing job snapshot.", Test_PerformanceCampaignAttemptState.UNVERIFIED);
                HandleAttemptSettled(attempt);
                return;
            }
            if (job.validation_state != VALID)
            {
                attempt.state = Test_PerformanceCampaignAttemptState.BLOCKED;
                attempt.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                attempt.stop_reason = "QUEUE_PREVALIDATION_BLOCKED";
                attempt.diagnostic = job.validation_diagnostic;
                HandleAttemptSettled(attempt);
                return;
            }
            if (!CheckSourceBeforeAttempt(out var sourceDiagnostic))
            {
                PauseCampaign(sourceDiagnostic);
                return;
            }
            var attemptSource = CaptureSourceIdentity(out var attemptSourceDiagnostic);
            if (attemptSource == null)
            {
                PauseCampaign(attemptSourceDiagnostic);
                return;
            }
            if (!TryBuildFixtureIdentity(job.options, out var currentIdentity, out var validationDiagnostic))
            {
                PauseCampaign("The queued job failed immediate preflight: " + validationDiagnostic);
                return;
            }
            var fixtureDifference = job.fixture_identity?.DescribeDifference(currentIdentity);
            if (fixtureDifference != null)
            {
                PauseCampaign("The queued fixture/configuration changed before this attempt. " + fixtureDifference);
                return;
            }
            if (!Test_PerformanceHarnessController.TryValidateLaunchPrerequisites(job.options, out var controllerDiagnostic))
            {
                PauseCampaign(controllerDiagnostic);
                return;
            }

            _activeAttemptIndex = attemptIndex;
            _activeAttemptDispatchTimestamp = Stopwatch.GetTimestamp();
            _lastCompletedRunID = Test_PerformanceHarnessController.LastCompletedRunID;
            attempt.state = Test_PerformanceCampaignAttemptState.RUNNING;
            var dispatchUtc = DateTime.UtcNow;
            attempt.started_utc = dispatchUtc.ToString("o", CultureInfo.InvariantCulture);
            attempt.dispatch_utc = attempt.started_utc;
            attempt.source = attemptSource;
            attempt.diagnostic = null;
            Persist();
            NotifyChanged();

            try
            {
                Test_PerformanceHarnessController.StartQueued(
                    job.options.Clone(),
                    _activeAttemptDispatchTimestamp,
                    dispatchUtc);
            }
            catch (Exception exception)
            {
                SetAttemptFailed(attempt, exception.Message, Test_PerformanceCampaignAttemptState.FAILED);
                _activeAttemptIndex = -1;
                HandleAttemptSettled(attempt);
                return;
            }

            if (Test_PerformanceHarnessController.IsRunning)
            {
                attempt.run_id = Test_PerformanceHarnessController.ActiveRunID;
                attempt.result_path = Test_PerformanceHarnessController.ActiveResultDirectory;
                attempt.report_path = Test_PerformanceHarnessController.LastReportPath;
                Persist();
                NotifyChanged();
                return;
            }

            if (!string.Equals(_lastCompletedRunID, Test_PerformanceHarnessController.LastCompletedRunID, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(Test_PerformanceHarnessController.LastCompletedRunID))
            {
                attempt.run_id = Test_PerformanceHarnessController.LastCompletedRunID;
                attempt.result_path = Test_PerformanceHarnessController.LastCompletedResultDirectory;
                attempt.report_path = Test_PerformanceHarnessController.LastReportPath;
                Persist();
                ScheduleFinalize();
                return;
            }

            SetAttemptFailed(attempt, Test_PerformanceHarnessController.Status, Test_PerformanceCampaignAttemptState.FAILED);
            _activeAttemptIndex = -1;
            HandleAttemptSettled(attempt);
        }

        private static void OnControllerStateChanged()
        {
            if (!IsRunning)
                return;
            if (_activeAttemptIndex >= 0 && !Test_PerformanceHarnessController.IsRunning)
                ScheduleFinalize();
        }

        private static void OnControllerLifecycleSettled()
        {
            if (!IsRunning || _activeAttemptIndex < 0)
                return;
            ScheduleFinalize();
        }

        private static void ScheduleFinalize()
        {
            if (_finalizeScheduled)
                return;
            _finalizeScheduled = true;
            EditorApplication.delayCall += FinalizeAfterEditorTurn;
        }

        private static void FinalizeAfterEditorTurn()
        {
            _finalizeScheduled = false;
            if (!IsRunning || _activeAttemptIndex < 0)
                return;
            if (Test_PerformanceHarnessController.IsRunning || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            FinalizeActiveAttempt();
        }

        private static void FinalizeActiveAttempt()
        {
            var attempt = CurrentAttempt;
            if (attempt == null)
                return;
            if (string.IsNullOrEmpty(attempt.result_path))
            {
                SetAttemptFailed(attempt, "The controller finalized without exposing a result directory.", Test_PerformanceCampaignAttemptState.UNVERIFIED);
                _activeAttemptIndex = -1;
                HandleAttemptSettled(attempt);
                return;
            }

            var result = Test_PerformanceEvidence.ReadFinalizedResult(attempt.result_path);
            attempt.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            attempt.report_path = result.ReportPath ?? attempt.report_path;
            attempt.result_status = result.Status;
            attempt.stop_reason = result.StopReason;
            if (!result.IsVerified)
            {
                attempt.state = Test_PerformanceCampaignAttemptState.UNVERIFIED;
                attempt.diagnostic = result.ErrorMessage;
            }
            else if (!SourceMatchesAttempt(attempt, result, out var sourceDiagnostic))
            {
                attempt.state = Test_PerformanceCampaignAttemptState.UNVERIFIED;
                attempt.diagnostic = sourceDiagnostic;
            }
            else if (result.Status == "COMPLETE-MEASURED")
            {
                attempt.state = Test_PerformanceCampaignAttemptState.COMPLETED;
                attempt.diagnostic = null;
            }
            else if (result.Status == "BLOCKED-PREREQUISITE")
            {
                attempt.state = Test_PerformanceCampaignAttemptState.BLOCKED;
                attempt.diagnostic = result.StopMessage;
            }
            else
            {
                attempt.state = Test_PerformanceCampaignAttemptState.FAILED;
                attempt.diagnostic = result.StopMessage;
            }
            _activeAttemptIndex = -1;
            Persist();
            NotifyChanged();
            HandleAttemptSettled(attempt);
        }

        private static bool SourceMatchesAttempt(
            Test_PerformanceCampaignAttempt attempt,
            Test_PerformanceLastRunView result,
            out string diagnostic)
        {
            diagnostic = null;
            if (attempt?.source == null)
                return true;
            var expectedBranch = string.IsNullOrEmpty(attempt.source.git_branch)
                ? attempt.source.git_branch_status
                : attempt.source.git_branch;
            var actualBranch = result?.Branch;
            if (!string.Equals(expectedBranch, actualBranch, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(attempt.source.commit_sha, result?.Commit, StringComparison.OrdinalIgnoreCase))
            {
                diagnostic = "Attempt source identity changed while the run was executing; the result was preserved but not mixed into this campaign cohort. "
                    + "queued " + (expectedBranch ?? "unknown") + " @ " + (attempt.source.commit_sha ?? "unknown")
                    + "; evidence " + (actualBranch ?? "unknown") + " @ " + (result?.Commit ?? "unknown") + ".";
                return false;
            }
            return true;
        }

        private static void HandleAttemptSettled(Test_PerformanceCampaignAttempt attempt)
        {
            attempt.queue_settled_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            if (_activeAttemptDispatchTimestamp > 0)
            {
                var elapsedTicks = Stopwatch.GetTimestamp() - _activeAttemptDispatchTimestamp;
                attempt.dispatch_to_queue_settled_seconds = elapsedTicks < 0
                    ? -1d
                    : elapsedTicks / (double)Stopwatch.Frequency;
            }
            _activeAttemptDispatchTimestamp = 0;
            Persist();
            NotifyChanged();
            if (_manifest.cancel_remaining)
            {
                CancelQueuedAttempts();
                FinishCampaignAsCancelled();
                return;
            }
            if (attempt.state != Test_PerformanceCampaignAttemptState.COMPLETED
                && _manifest.stop_policy == Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE)
            {
                PauseCampaign("Queue paused after " + attempt.state + " attempt " + attempt.repeat_index + ". Explicit resume is required.");
                return;
            }
            if (_manifest.stop_after_current)
            {
                PauseCampaign("Queue paused after the current attempt as requested.");
                return;
            }
            ScheduleAdvance();
        }

        private static void SetAttemptFailed(Test_PerformanceCampaignAttempt attempt, string diagnostic, string state)
        {
            attempt.state = state;
            attempt.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            attempt.diagnostic = diagnostic;
            attempt.stop_reason = "QUEUE_COORDINATOR";
            attempt.result_status = state == Test_PerformanceCampaignAttemptState.UNVERIFIED ? "UNVERIFIED" : "FAIL-PROVISION";
        }

        private static void PauseCampaign(string reason)
        {
            _manifest.state = Test_PerformanceCampaignState.PAUSED;
            _manifest.pause_reason = reason;
            _manifest.queue_diagnostic = reason;
            Persist();
            NotifyChanged();
        }

        private static bool FailCampaignClosed(string reason, out string diagnostic)
        {
            PauseCampaign(reason);
            diagnostic = reason;
            return false;
        }

        private static void FinishCampaignAsCompleted()
        {
            _manifest.state = Test_PerformanceCampaignState.COMPLETED;
            _manifest.pause_reason = null;
            _manifest.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Persist();
            NotifyChanged();
        }

        private static void FinishCampaignAsCancelled()
        {
            _manifest.state = Test_PerformanceCampaignState.CANCELLED;
            _manifest.pause_reason = "Remaining campaign attempts were cancelled by the user.";
            _manifest.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Persist();
            NotifyChanged();
        }

        private static void CancelQueuedAttempts()
        {
            foreach (var attempt in _manifest.attempts)
            {
                if (attempt.state != Test_PerformanceCampaignAttemptState.QUEUED)
                    continue;
                attempt.state = Test_PerformanceCampaignAttemptState.CANCELLED;
                attempt.finished_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                attempt.stop_reason = "QUEUE_CANCELLED";
                attempt.diagnostic = "Cancelled before launch by the user.";
            }
        }

        private static bool CheckSourceBeforeAttempt(out string diagnostic)
        {
            diagnostic = null;
            var current = CaptureSourceIdentity(out diagnostic);
            if (current == null)
                return false;
            var difference = _manifest.source?.DescribeDifference(current);
            if (difference == null)
                return true;
            diagnostic = "Source identity changed; the campaign will not mix results. " + difference;
            return false;
        }

        private static Test_PerformanceCampaignJob BuildJob(Test_PerformanceHarnessOptions options, out string diagnostic)
        {
            var job = new Test_PerformanceCampaignJob
            {
                job_id = CreateID("job"),
                options = options,
                repeat_count = 1,
                validation_state = UNVALIDATED,
            };
            if (TryBuildFixtureIdentity(options, out var identity, out diagnostic))
            {
                job.fixture_identity = identity;
                job.validation_state = VALID;
            }
            else
            {
                job.validation_state = BLOCKED;
                job.validation_diagnostic = diagnostic;
            }
            return job;
        }

        private static bool TryBuildFixtureIdentity(
            Test_PerformanceHarnessOptions options,
            out Test_PerformanceCampaignFixtureIdentity identity,
            out string diagnostic)
        {
            identity = null;
            diagnostic = null;
            if (options == null)
            {
                diagnostic = "The job has no options snapshot.";
                return false;
            }
            try
            {
                if (!options.TryValidate(out diagnostic, out var worldPlan, out var actorProfile))
                    return false;
                var graphicalPlan = options.IsGraphicalClient
                    ? Test_GraphicalFixtureLayout.BuildPlan(options.GraphicalScenario, options.DensityTier, options.ActorDensity, options.Seed)
                    : null;
                var builder = new StringBuilder();
                builder.Append(options.EffectiveProfileID).Append('|')
                    .Append(options.WorkloadMode).Append('|')
                    .Append(options.WorldFixture).Append('|')
                    .Append(options.DensityTier).Append('|')
                    .Append(options.ActorCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(options.Seed.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(options.GraphicalScenario).Append('|')
                    .Append(options.ActorDensity).Append('|')
                    .Append(options.WarmupSeconds.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(options.MeasurementSeconds.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(options.ActorVariantID).Append('|');
                if (actorProfile != null)
                    builder.Append(actorProfile.profile_id).Append('|').Append(actorProfile.variant_id).Append('|').Append(actorProfile.revision).Append('|').Append(actorProfile.provenance?.input_path);
                if (worldPlan != null)
                    builder.Append('|').Append(worldPlan.FixtureID).Append('|').Append(worldPlan.AssetPath).Append('|').Append(worldPlan.MetaGUID).Append('|').Append(worldPlan.SceneHash);
                if (graphicalPlan != null)
                {
                    builder.Append('|').Append(graphicalPlan.PlanHash).Append('|').Append(graphicalPlan.RouteID);
                    foreach (var path in graphicalPlan.FixtureScenePaths)
                        builder.Append('|').Append(path);
                }
                identity = new Test_PerformanceCampaignFixtureIdentity
                {
                    configuration_sha256 = Test_PerformanceRunMetadata.HashString(builder.ToString()),
                    profile_id = options.EffectiveProfileID,
                    profile_revision = actorProfile?.revision ?? graphicalPlan?.ProfileRevision,
                    actor_variant_id = options.ActorVariantID,
                    world_fixture_id = worldPlan?.FixtureID,
                    world_scene_path = worldPlan?.AssetPath,
                    world_scene_guid = worldPlan?.MetaGUID,
                    world_scene_hash = worldPlan?.SceneHash,
                    graphical_plan_hash = graphicalPlan?.PlanHash,
                    graphical_route_id = graphicalPlan?.RouteID,
                };
                return true;
            }
            catch (Exception exception)
            {
                diagnostic = exception.Message;
                return false;
            }
        }

        private static Test_PerformanceCampaignSource CaptureSourceIdentity(out string diagnostic)
        {
            diagnostic = null;
            try
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                    throw new DirectoryNotFoundException("The Unity project root could not be resolved.");
                var metadata = Test_PerformanceRunMetadata.Capture(projectRoot);
                CaptureGitBranch(projectRoot, out var branch, out var branchStatus, out var branchDiagnostic);
                return new Test_PerformanceCampaignSource
                {
                    captured_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    git_branch = branch,
                    git_branch_status = branchStatus,
                    git_branch_diagnostic = branchDiagnostic,
                    commit_sha = metadata.Commit,
                    source_sha256 = Test_PerformanceRunMetadata.HashString(string.Join("|", metadata.Commit, metadata.PackagesSha256, metadata.PackagesLockSha256, metadata.PurrNetSha256)),
                    dirty = metadata.Dirty,
                    diff_sha256 = metadata.DiffSha256,
                    packages_sha256 = metadata.PackagesSha256,
                    packages_lock_sha256 = metadata.PackagesLockSha256,
                    purrnet_sha256 = metadata.PurrNetSha256,
                    hardware = metadata.Hardware,
                    operating_system = SystemInfo.operatingSystem,
                    unity_version = Application.unityVersion,
                    graphics_device = SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName,
                    graphics_settings = CaptureGraphicsSettings(),
                };
            }
            catch (Exception exception)
            {
                diagnostic = "Campaign source identity could not be captured: " + exception.Message;
                return null;
            }
        }

        private static void CaptureGitBranch(
            string projectRoot,
            out string branch,
            out string branchStatus,
            out string diagnostic)
        {
            branch = null;
            diagnostic = null;
            if (TryRunGit(projectRoot, "symbolic-ref --quiet --short HEAD", out var output, out var error))
            {
                branch = output.Trim();
                if (!string.IsNullOrEmpty(branch))
                {
                    branchStatus = "named";
                    return;
                }
            }

            if (TryRunGit(projectRoot, "rev-parse --is-inside-work-tree", out output, out error)
                && string.Equals(output.Trim(), "true", StringComparison.OrdinalIgnoreCase))
            {
                branchStatus = "detached";
                diagnostic = "HEAD is detached; no branch name was inferred.";
                return;
            }

            branchStatus = "unavailable";
            diagnostic = string.IsNullOrWhiteSpace(error)
                ? "Git branch lookup did not return a symbolic branch or a valid work-tree state."
                : error.Trim();
        }

        private static bool TryRunGit(string projectRoot, string arguments, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;
            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo
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
                    {
                        error = "Git could not be started.";
                        return false;
                    }

                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static string CaptureGraphicsSettings()
        {
            var quality = QualitySettings.GetQualityLevel();
            var vsync = QualitySettings.vSyncCount;
            return $"quality_level={quality};vsync={vsync};target_fps={Application.targetFrameRate};render_pipeline={GraphicsSettings.currentRenderPipeline?.name ?? "none"}";
        }

        private static Test_PerformanceHarnessOptions CreateServerBaseline(Test_PerformanceWorkloadMode mode)
        {
            return new Test_PerformanceHarnessOptions
            {
                ProfileID = Test_ActorFixtureLayout.PROFILE_ID,
                WorldFixture = Test_WorldFixtureVariant.Outdoor,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                WorkloadMode = mode,
                ActorCount = Test_ActorFixtureLayout.NPC_COUNT,
                Seed = Test_ActorFixtureLayout.DEFAULT_SEED,
                WarmupSeconds = Test_GraphicalFixtureLayout.DEFAULT_WARMUP_SECONDS,
                MeasurementSeconds = Test_GraphicalFixtureLayout.DEFAULT_MEASUREMENT_SECONDS,
            };
        }

        private static Test_PerformanceHarnessOptions CreateGraphicalBaseline(Test_GraphicalScenario scenario)
        {
            return new Test_PerformanceHarnessOptions
            {
                ProfileID = Test_GraphicalFixtureLayout.PROFILE_ID,
                WorldFixture = Test_WorldFixtureVariant.Outdoor,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                ActorCount = 0,
                Seed = Test_GraphicalFixtureLayout.DEFAULT_SEED,
                GraphicalScenario = scenario,
                ActorDensity = Test_GraphicalActorDensity.None,
                WarmupSeconds = Test_GraphicalFixtureLayout.DEFAULT_WARMUP_SECONDS,
                MeasurementSeconds = Test_GraphicalFixtureLayout.DEFAULT_MEASUREMENT_SECONDS,
            };
        }

        private static List<Test_PerformanceProfileCoverage> BuildProfileCoverage()
        {
            var profiles = new List<Test_PerformanceProfileCoverage>
            {
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-01",
                    mode = "server + rendered client / networked city",
                    capability = "Blocked: generated city density, real authenticated clients, cross-tile interests and required telemetry are not available in the current runner.",
                    runnable_alternative = "P1-S0-04 ECS data-only or P1-S0-06 isolated rendered geometry.",
                    next_issue_or_stage = "#43-#52; later Stage 1.2/1.5 client and world-interest work.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-02",
                    mode = "networked server + rendered clients / PvP combat",
                    capability = "Blocked: repeatable faction, legal PvP target and multi-client correctness fixtures are not available.",
                    runnable_alternative = "P1-S0-04 server data-only or P1-S0-06 rendered geometry.",
                    next_issue_or_stage = "#43-#52 and later Stage 1.2/1.5 combat and network qualification.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-03",
                    mode = "networked server + rendered clients / boss and AoE",
                    capability = "Blocked: authored boss encounter, legal AoE target fan-out and replayable combat fixtures are not available.",
                    runnable_alternative = "P1-S0-04 server data-only or P1-S0-06 rendered geometry.",
                    next_issue_or_stage = "#45 and #51/#52 content and correctness verification.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-04",
                    mode = "Editor server / ECS data-only and activity",
                    status = "SUPPORTED-NOT-RUN",
                    capability = "The existing runner supports ECS data-only minimal/activity snapshots; full PurrNet dormant and wake variants remain blocked.",
                    runnable_alternative = "EcsDataOnlyMinimal and EcsDataOnlyActivity are the current runnable variants.",
                    next_issue_or_stage = "Run five qualified repeats for each required metric; Stage 1.4 for true dormant/wake simulation.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-05",
                    mode = "rendered client + streamed world / traversal",
                    capability = "Blocked: the current harness does not execute the required six-tile route, streaming transitions and origin-boundary correctness contract.",
                    runnable_alternative = "P1-S0-06 fixed Outdoor/Forest/Settlement rendered fixtures without a streaming claim.",
                    next_issue_or_stage = "#44/#46/#49/#52 and Stages 1.2/1.6 streaming corrections.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-06",
                    mode = "Editor rendered client / geometry, forest and settlement",
                    status = "SUPPORTED-NOT-RUN",
                    capability = "The existing graphical fixture runner supports isolated rendered Outdoor, Forest and Settlement routes; it does not qualify network density or production streaming.",
                    runnable_alternative = "Outdoor, Forest and Settlement are the current runnable variants.",
                    next_issue_or_stage = "Run five qualified repeats for each required metric; #44/#49/#52 for production-density and client telemetry expansion.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-07",
                    mode = "server + rendered clients / login and reconnect bursts",
                    capability = "Blocked: safe disposable account generation, real client admission and reconnect correctness are not available in the current runner.",
                    runnable_alternative = "P1-S0-04 server data-only or P1-S0-06 rendered geometry.",
                    next_issue_or_stage = "#46/#51 and Stages 1.7-1.8 reconnect and durable-session work.",
                },
                new Test_PerformanceProfileCoverage
                {
                    profile_id = "P1-S0-08",
                    mode = "Editor server / long-duration authoritative soak",
                    capability = "Blocked: the current harness has no compliant multi-hour churn, persistence, leak and scheduler-backlog observation contract.",
                    runnable_alternative = "P1-S0-04 bounded short server measurement only; it is not a soak or production-capacity claim.",
                    next_issue_or_stage = "#47-#51; Stage 1.1 scheduler telemetry and Stages 1.7-1.8 durability.",
                },
            };
            if (_manifest == null)
                return profiles;

            foreach (var profile in profiles)
            {
                if (profile.status == "BLOCKED")
                    continue;
                var jobIDs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var job in _manifest.jobs ?? new List<Test_PerformanceCampaignJob>())
                {
                    if (job?.options?.EffectiveProfileID == profile.profile_id)
                        jobIDs.Add(job.job_id);
                }
                var measured = (_manifest.attempts ?? new List<Test_PerformanceCampaignAttempt>())
                    .Exists(attempt => jobIDs.Contains(attempt.job_id) && attempt.state == Test_PerformanceCampaignAttemptState.COMPLETED);
                profile.status = measured ? "SUPPORTED-MEASURED" : "SUPPORTED-NOT-RUN";
            }
            return profiles;
        }

        private static bool EnsureDraftCampaign(out string diagnostic)
        {
            diagnostic = null;
            if (_manifest != null)
                return true;
            try
            {
                var campaignRoot = GetCampaignRoot();
                Directory.CreateDirectory(campaignRoot);
                var campaignID = CreateID("campaign");
                _campaignDirectory = Path.Combine(campaignRoot, campaignID);
                Directory.CreateDirectory(_campaignDirectory);
                _manifest = new Test_PerformanceCampaignManifest
                {
                    campaign_id = campaignID,
                    created_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    updated_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                };
                Persist();
                return true;
            }
            catch (Exception exception)
            {
                return FailUI("Campaign storage could not be created: " + exception.Message, out diagnostic);
            }
        }

        private static void Persist()
        {
            if (_manifest == null || string.IsNullOrEmpty(_campaignDirectory))
                return;
            _manifest.updated_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            _manifest.profile_coverage = BuildProfileCoverage();
            WriteAtomic(Path.Combine(_campaignDirectory, CAMPAIGN_FILE_NAME), _manifest);
            var index = new Test_PerformanceCampaignIndex
            {
                campaign_id = _manifest.campaign_id,
                updated_utc = _manifest.updated_utc,
                state = _manifest.state,
                stop_policy = _manifest.stop_policy,
                linked_campaign_id = _manifest.linked_campaign_id,
                source = _manifest.source,
                profile_coverage = _manifest.profile_coverage,
            };
            foreach (var attempt in _manifest.attempts)
                index.attempts.Add(BuildIndexAttempt(attempt));
            WriteAtomic(Path.Combine(_campaignDirectory, INDEX_FILE_NAME), index);
        }

        private static Test_PerformanceCampaignIndexAttempt BuildIndexAttempt(Test_PerformanceCampaignAttempt attempt)
        {
            var job = FindJob(attempt.job_id);
            var source = attempt.source ?? _manifest.source;
            var index = new Test_PerformanceCampaignIndexAttempt
            {
                job_id = attempt.job_id,
                repeat_index = attempt.repeat_index,
                run_id = attempt.run_id,
                result_path = attempt.result_path,
                status = attempt.result_status ?? attempt.state,
                stop_reason = attempt.stop_reason,
                source = source,
                profile_id = job?.options?.EffectiveProfileID,
                seed = job == null ? null : job.options.Seed.ToString(CultureInfo.InvariantCulture),
                configuration_sha256 = job?.fixture_identity?.configuration_sha256,
                hardware = source?.hardware,
                unity_version = source?.unity_version,
                graphics_device = source?.graphics_device,
                started_utc = attempt.started_utc,
                finished_utc = attempt.finished_utc,
                artifact_index_status = "absent",
            };
            if (string.IsNullOrEmpty(attempt.result_path) || !Directory.Exists(attempt.result_path))
                return index;

            var summaryPath = Path.Combine(attempt.result_path, "summary.json");
            if (File.Exists(summaryPath))
            {
                var summary = JsonUtility.FromJson<Test_PerformanceCampaignIndexSummary>(File.ReadAllText(summaryPath));
                if (summary != null)
                {
                    index.fixture_sha256 = summary.fixture_sha256;
                    index.correctness_status = summary.correctness?.status;
                    index.cleanup_status = summary.correctness == null
                        ? null
                        : summary.correctness.cleanup ? "complete" : "incomplete";
                }
            }
            var artifactsPath = Path.Combine(attempt.result_path, "artifacts.json");
            if (!File.Exists(artifactsPath))
                return index;
            var artifactDocument = JsonUtility.FromJson<Test_PerformanceCampaignArtifactDocument>(File.ReadAllText(artifactsPath));
            if (artifactDocument?.files == null)
            {
                index.artifact_index_status = "invalid";
                return index;
            }
            var verified = true;
            var entries = new List<Test_PerformanceCampaignIndexArtifact>();
            foreach (var artifact in artifactDocument.files)
            {
                if (artifact == null)
                {
                    verified = false;
                    continue;
                }
                var present = !string.Equals(artifact.status, "absent", StringComparison.OrdinalIgnoreCase);
                var entry = new Test_PerformanceCampaignIndexArtifact
                {
                    path = artifact.path,
                    sha256 = artifact.sha256,
                    bytes = artifact.bytes,
                    required = artifact.required,
                    optional = artifact.optional || !artifact.required,
                    status = present ? "present" : "absent",
                };
                if (present)
                {
                    var path = Path.GetFullPath(Path.Combine(attempt.result_path, artifact.path ?? string.Empty));
                    if (!IsSafeResultFile(path) || string.IsNullOrEmpty(artifact.sha256)
                        || !string.Equals(Test_PerformanceRunMetadata.HashFile(path), artifact.sha256, StringComparison.OrdinalIgnoreCase)
                        || artifact.bytes != new FileInfo(path).Length)
                    {
                        verified = false;
                    }
                }
                else if (artifact.required)
                {
                    verified = false;
                }
                entries.Add(entry);
            }
            index.artifacts = entries.ToArray();
            index.artifact_index_status = verified ? "verified" : "hash_mismatch_or_missing";
            return index;
        }

        #pragma warning disable 0649

        [Serializable]
        private sealed class Test_PerformanceCampaignArtifactDocument
        {
            public Test_PerformanceCampaignIndexArtifact[] files;
        }

        [Serializable]
        private sealed class Test_PerformanceCampaignIndexSummary
        {
            public string fixture_sha256;
            public Test_PerformanceCampaignIndexCorrectness correctness;
        }

        [Serializable]
        private sealed class Test_PerformanceCampaignIndexCorrectness
        {
            public string status;
            public bool cleanup;
        }

        #pragma warning restore 0649

        private static void WriteAtomic(string path, object value)
        {
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            var temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            File.WriteAllText(temporaryPath, JsonUtility.ToJson(value, true), new UTF8Encoding(false));
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporaryPath, path);
        }

        private static void ValidateManifest(Test_PerformanceCampaignManifest manifest, string directory)
        {
            if (manifest == null)
                throw new InvalidDataException("The campaign manifest is empty.");
            if (manifest.schema_version != "performance-campaign/v1")
                throw new InvalidDataException("The campaign manifest schema is unsupported.");
            if (string.IsNullOrEmpty(manifest.campaign_id)
                || !string.Equals(Path.GetFileName(directory), manifest.campaign_id, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The campaign manifest identity does not match its directory.");
            }
            if (manifest.jobs == null || manifest.jobs.Count > MAX_JOBS)
                throw new InvalidDataException("The campaign manifest exceeds the bounded job limit.");
            if (manifest.attempts == null || manifest.attempts.Count > MAX_TOTAL_ATTEMPTS)
                throw new InvalidDataException("The campaign manifest exceeds the bounded attempt limit.");
            foreach (var job in manifest.jobs)
            {
                if (job == null || string.IsNullOrEmpty(job.job_id) || job.options == null || job.repeat_count < 1 || job.repeat_count > MAX_REPEAT_COUNT)
                    throw new InvalidDataException("The campaign manifest contains an invalid job snapshot.");
            }
            foreach (var attempt in manifest.attempts)
            {
                if (attempt == null || string.IsNullOrEmpty(attempt.attempt_id) || FindJob(manifest, attempt.job_id) == null)
                    throw new InvalidDataException("The campaign manifest contains an invalid attempt mapping.");
                ValidateStoredPath(attempt.result_path, "attempt result path");
                ValidateStoredPath(attempt.report_path, "attempt report path");
            }
        }

        private static void ValidateStoredPath(string path, string label)
        {
            if (string.IsNullOrEmpty(path))
                return;
            var resultRoot = GetResultRoot();
            var fullPath = Path.GetFullPath(path);
            var campaignRoot = Path.Combine(resultRoot, CAMPAIGNS_DIRECTORY_NAME) + Path.DirectorySeparatorChar;
            if (!IsWithin(fullPath, resultRoot) || fullPath.StartsWith(campaignRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(label + " is outside an individual benchmark result directory.");
        }

        private static int CompareCampaignTimestamp(Test_PerformanceCampaignManifest left, Test_PerformanceCampaignManifest right)
        {
            return ParseTimestamp(left.updated_utc, left.created_utc).CompareTo(ParseTimestamp(right.updated_utc, right.created_utc));
        }

        private static DateTime ParseTimestamp(string primary, string fallback)
        {
            if (DateTime.TryParse(primary, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
                return timestamp;
            if (DateTime.TryParse(fallback, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
                return timestamp;
            return DateTime.MinValue;
        }

        private static int CountAttempts(string state)
        {
            if (_manifest?.attempts == null)
                return 0;
            var count = 0;
            foreach (var attempt in _manifest.attempts)
            {
                if (attempt.state == state)
                    count++;
            }
            return count;
        }

        private static int CountRequestedAttempts()
        {
            if (_manifest?.jobs == null)
                return 0;
            var count = 0;
            foreach (var job in _manifest.jobs)
                count += Math.Max(0, job.repeat_count);
            return count;
        }

        private static int CountRequestedAttempts(string changedJobID, int changedRepeatCount)
        {
            var count = 0;
            foreach (var job in _manifest.jobs)
                count += job.job_id == changedJobID ? changedRepeatCount : Math.Max(0, job.repeat_count);
            return count;
        }

        private static int FindJobIndex(string jobID)
        {
            if (_manifest?.jobs == null)
                return -1;
            for (var index = 0; index < _manifest.jobs.Count; index++)
            {
                if (string.Equals(_manifest.jobs[index].job_id, jobID, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        private static Test_PerformanceCampaignJob FindJob(string jobID)
        {
            return FindJob(_manifest, jobID);
        }

        private static Test_PerformanceCampaignJob FindJob(Test_PerformanceCampaignManifest manifest, string jobID)
        {
            if (manifest?.jobs == null)
                return null;
            foreach (var job in manifest.jobs)
            {
                if (string.Equals(job.job_id, jobID, StringComparison.Ordinal))
                    return job;
            }
            return null;
        }

        private static int FindAttemptIndex(string state)
        {
            if (_manifest?.attempts == null)
                return -1;
            for (var index = 0; index < _manifest.attempts.Count; index++)
            {
                if (_manifest.attempts[index].state == state)
                    return index;
            }
            return -1;
        }

        private static string CreateID(string prefix)
        {
            return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static string GetResultRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
                throw new DirectoryNotFoundException("The Unity project root could not be resolved.");
            return Path.GetFullPath(Path.Combine(projectRoot.FullName, Test_PerformanceRun.RESULT_ROOT));
        }

        private static string GetCampaignRoot()
        {
            return Path.Combine(GetResultRoot(), CAMPAIGNS_DIRECTORY_NAME);
        }

        private static bool IsSafeResultDirectory(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path)
                    && Directory.Exists(path)
                    && IsWithin(path, GetResultRoot())
                    && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSafeResultFile(string path)
        {
            try
            {
                return !string.IsNullOrEmpty(path)
                    && File.Exists(path)
                    && IsWithin(path, GetResultRoot())
                    && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
            }
            catch
            {
                return false;
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

        private static bool FailUI(string message, out string diagnostic)
        {
            diagnostic = message;
            NotifyChanged();
            return false;
        }

        private static void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
