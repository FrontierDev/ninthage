using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Shared.Performance
{
    /// <summary>Validated identity and output context supplied by a test runner or measurement harness.</summary>
    public sealed class PerformanceSessionContext
    {
        public PerformanceSessionContext(string runId, string resultsDirectory)
        {
            RunId = RequireBounded(runId, nameof(runId), 256);
            if (string.IsNullOrWhiteSpace(resultsDirectory) || !Path.IsPathRooted(resultsDirectory))
                throw new ArgumentException("An absolute test result directory is required.", nameof(resultsDirectory));

            ResultsDirectory = Path.GetFullPath(resultsDirectory);
        }

        public string RunId { get; }

        public string ResultsDirectory { get; }

        public string FixtureSha256 { get; set; }

        public string GitRepository { get; set; }

        public string GitBranch { get; set; }

        public string GitCapturedUtc { get; set; }

        public string GitCommitSha { get; set; }

        public bool? GitDirty { get; set; }

        public string BuildKind { get; set; }

        public string BuildFlags { get; set; }

        public string ProfileRevision { get; set; }

        public string Seed { get; set; }

        public string Platform { get; set; }

        public string GraphicsOptions { get; set; }

        public string Hardware { get; set; }

        public string HostId { get; set; }

        public string ProcessId { get; set; }

        internal void Validate()
        {
            ValidateOptional(FixtureSha256, nameof(FixtureSha256), 128);
            ValidateOptional(GitRepository, nameof(GitRepository), 256);
            ValidateOptional(GitBranch, nameof(GitBranch), 256);
            ValidateOptional(GitCapturedUtc, nameof(GitCapturedUtc), 64);
            ValidateOptional(GitCommitSha, nameof(GitCommitSha), 64);
            if (!string.IsNullOrEmpty(GitCommitSha) && !IsHex(GitCommitSha))
                throw new ArgumentException("GitCommitSha must contain only hexadecimal characters.", nameof(GitCommitSha));
            if (!string.IsNullOrEmpty(GitCommitSha) && GitCommitSha.Length != 40 && GitCommitSha.Length != 64)
                throw new ArgumentException("GitCommitSha must be a full 40- or 64-character SHA.", nameof(GitCommitSha));
            if (!string.IsNullOrEmpty(FixtureSha256) && (FixtureSha256.Length != 64 || !IsHex(FixtureSha256)))
                throw new ArgumentException("FixtureSha256 must be a full 64-character hexadecimal digest.", nameof(FixtureSha256));

            ValidateOptional(BuildKind, nameof(BuildKind), 128);
            ValidateOptional(BuildFlags, nameof(BuildFlags), 1024);
            ValidateOptional(ProfileRevision, nameof(ProfileRevision), 256);
            ValidateOptional(Seed, nameof(Seed), 128);
            ValidateOptional(Platform, nameof(Platform), 256);
            ValidateOptional(GraphicsOptions, nameof(GraphicsOptions), 1024);
            ValidateOptional(Hardware, nameof(Hardware), 1024);
            ValidateOptional(HostId, nameof(HostId), 256);
            ValidateOptional(ProcessId, nameof(ProcessId), 256);

            if (string.IsNullOrWhiteSpace(HostId))
                throw new ArgumentException("HostId is required for a structured measurement session.", nameof(HostId));
            if (string.IsNullOrWhiteSpace(ProcessId))
                throw new ArgumentException("ProcessId is required for a structured measurement session.", nameof(ProcessId));
        }

        private static string RequireBounded(string value, string parameterName, int maximumLength)
        {
            ValidateOptional(value, parameterName, maximumLength);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A value is required.", parameterName);
            return value;
        }

        private static void ValidateOptional(string value, string parameterName, int maximumLength)
        {
            if (value != null && value.Length > maximumLength)
                throw new ArgumentException(parameterName + " exceeds its bounded length.", parameterName);
        }

        private static bool IsHex(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool isHex = character >= '0' && character <= '9'
                    || character >= 'a' && character <= 'f'
                    || character >= 'A' && character <= 'F';
                if (!isHex)
                    return false;
            }

            return true;
        }
    }

    /// <summary>Canonical terminal states used in a benchmark summary.</summary>
    public enum PerformanceRunStatus
    {
        CompleteMeasured,
        BlockedPrerequisite,
        FailCorrectness,
        FailProvision,
        FailResource,
        FailTimeout,
        FailInstrumentation
    }

    /// <summary>Controls the bounded, opt-in performance measurement session.</summary>
    public static class PerformanceTelemetry
    {
        internal const int MAX_SAMPLES_PER_METRIC = 10000;
        // Bounded capacity for the supported five-second Editor workloads. The sample buffer
        // reserves a small context partition, leaving 9,872 statistical slots.
        public const int RECOMMENDED_SAMPLES_PER_METRIC = 10000;
        internal const int MAX_REGISTERED_METRICS = 256;
        // A result with more than this invalid-input loss cannot advertise representative statistics.
        internal const double MAX_MEASUREMENT_DROP_RATIO = 0.05d;
        private static PerformanceSession _session;

        /// <summary>Raised after a telemetry session becomes active.</summary>
        public static event Action SessionStarted;

        /// <summary>Raised after a telemetry session has been stopped and cleared.</summary>
        public static event Action SessionEnded;

        /// <summary>Gets whether a real, explicitly started measurement session is active.</summary>
        public static bool IsEnabled => _session != null;

        /// <summary>Starts an opt-in session. No ordinary client/server startup path calls this method.</summary>
        public static void StartSession(PerformanceSessionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (_session != null)
                throw new InvalidOperationException("A performance telemetry session is already active.");

            context.Validate();
            _session = new PerformanceSession(context);
            Application.quitting += HandleApplicationQuitting;
            SessionStarted?.Invoke();
        }

        /// <summary>Registers bounded storage and the declared observation state for one metric.</summary>
        public static void RegisterMetric(
            PerformanceMetricDefinition definition,
            int capacity,
            PerformanceMetricObservation observation = PerformanceMetricObservation.Measured,
            string reason = null)
        {
            RequireSession().RegisterMetric(definition, capacity, observation, reason);
        }

        /// <summary>Marks that the runner applied the requested workload.</summary>
        public static void MarkWorkloadApplied()
        {
            RequireSession().MarkWorkloadApplied();
        }

        /// <summary>Advances the explicit measurement lifecycle phase.</summary>
        public static bool BeginPhase(PerformanceMeasurementPhase phase)
        {
            return RequireSession().BeginPhase(phase);
        }

        /// <summary>Records one finite metric sample without allocating or logging in the callback.</summary>
        public static bool TryRecord(
            PerformanceMetricDefinition definition,
            double value,
            long frameOrTick = -1,
            double intervalSeconds = -1)
        {
            if (_session == null)
                return false;

            return _session.TryRecord(
                definition,
                value,
                GetElapsedNanoseconds(),
                frameOrTick,
                intervalSeconds);
        }

        /// <summary>Finalises and writes a qualified session. Returns false when workload or measurement was not applied.</summary>
        public static bool StopSession(
            PerformanceRunStatus status,
            string reasonCode,
            string message)
        {
            PerformanceSession session = RequireSession();
            try
            {
                return session.Finalize(status, reasonCode, message, false);
            }
            finally
            {
                Shutdown(session);
            }
        }

        /// <summary>Writes a crash-safe partial result for an active, qualified session.</summary>
        public static bool FlushPartial(string message = "Session ended before finalisation.")
        {
            PerformanceSession session = RequireSession();
            return session.Finalize(
                PerformanceRunStatus.FailInstrumentation,
                "PROCESS_CRASH",
                message,
                true);
        }

        internal static void TryRecordDuration(
            PerformanceMetricDefinition definition,
            double milliseconds,
            long frameOrTick,
            double intervalSeconds)
        {
            if (_session == null)
                return;

            _session.TryRecord(
                definition,
                milliseconds,
                GetElapsedNanoseconds(),
                frameOrTick,
                intervalSeconds);
        }

        private static void HandleApplicationQuitting()
        {
            if (_session != null)
                FlushPartial("Process quit before finalisation.");
        }

        private static void Shutdown(PerformanceSession session)
        {
            if (!ReferenceEquals(_session, session))
                return;

            Application.quitting -= HandleApplicationQuitting;
            _session = null;
            SessionEnded?.Invoke();
        }

        private static PerformanceSession RequireSession()
        {
            if (_session == null)
                throw new InvalidOperationException("No performance telemetry session is active.");
            return _session;
        }

        private static long GetElapsedNanoseconds()
        {
            return (long)((Stopwatch.GetTimestamp() - _session.StartTimestamp) * (1000000000d / Stopwatch.Frequency));
        }
    }

    internal sealed class PerformanceSession
    {
        private readonly Dictionary<string, PerformanceMetricRegistration> _registrations =
            new Dictionary<string, PerformanceMetricRegistration>(StringComparer.Ordinal);
        private readonly long _startTimestamp;
        private PerformanceMeasurementPhase _currentPhase = PerformanceMeasurementPhase.Preflight;
        private bool _workloadApplied;
        private bool _measurementPhaseEntered;
        private bool _preflightComplete;
        private bool _rampComplete;
        private bool _warmupComplete;
        private bool _recoveryComplete;

        public PerformanceSession(PerformanceSessionContext context)
        {
            Context = context;
            StartedUtc = DateTime.UtcNow;
            _startTimestamp = Stopwatch.GetTimestamp();
            _preflightComplete = true;
        }

        public PerformanceSessionContext Context { get; }

        public DateTime StartedUtc { get; }

        public long StartTimestamp => _startTimestamp;

        public void RegisterMetric(
            PerformanceMetricDefinition definition,
            int capacity,
            PerformanceMetricObservation observation,
            string reason)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (observation != PerformanceMetricObservation.Measured
                && observation != PerformanceMetricObservation.Estimated
                && string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required for an unmeasured metric.", nameof(reason));
            if (reason != null && reason.Length > 512)
                throw new ArgumentException("Metric reason exceeds its bounded length.", nameof(reason));

            if (_registrations.TryGetValue(definition.MetricId, out PerformanceMetricRegistration existing))
            {
                if (existing.Matches(definition, capacity, observation, reason))
                    return;

                throw new InvalidOperationException("A metric with this ID was registered with different dimensions: " + definition.MetricId);
            }
            if (_registrations.Count >= PerformanceTelemetry.MAX_REGISTERED_METRICS)
                throw new InvalidOperationException("The maximum number of registered metrics has been reached.");

            _registrations.Add(
                definition.MetricId,
                new PerformanceMetricRegistration(
                    definition,
                    capacity,
                    observation,
                    reason,
                    observation == PerformanceMetricObservation.Measured
                        || observation == PerformanceMetricObservation.Estimated
                        ? new PerformanceSampleBuffer(capacity)
                        : null));
        }

        public void MarkWorkloadApplied()
        {
            _workloadApplied = true;
        }

        public bool BeginPhase(PerformanceMeasurementPhase phase)
        {
            if (phase == PerformanceMeasurementPhase.Measurement && !_workloadApplied)
                return false;

            _currentPhase = phase;
            switch (phase)
            {
                case PerformanceMeasurementPhase.Ramp:
                    _rampComplete = true;
                    break;
                case PerformanceMeasurementPhase.Warmup:
                    _warmupComplete = true;
                    break;
                case PerformanceMeasurementPhase.Measurement:
                    _measurementPhaseEntered = true;
                    break;
                case PerformanceMeasurementPhase.Recovery:
                    _recoveryComplete = true;
                    break;
            }

            return true;
        }

        public bool TryRecord(
            PerformanceMetricDefinition definition,
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds)
        {
            if (definition == null
                || !_registrations.TryGetValue(definition.MetricId, out PerformanceMetricRegistration registration)
                || registration.Buffer == null)
                return false;

            return registration.Buffer.Add(
                value,
                monotonicElapsedNanoseconds,
                frameOrTick,
                intervalSeconds,
                _currentPhase);
        }

        public bool Finalize(
            PerformanceRunStatus status,
            string reasonCode,
            string message,
            bool partial)
        {
            if (!_workloadApplied || !_measurementPhaseEntered)
                return false;

            ValidateStop(reasonCode, message);
            if (status == PerformanceRunStatus.CompleteMeasured
                && TryGetMeasurementCoverageFailure(out string coverageFailureMetricID, out double coverageRatio))
            {
                status = PerformanceRunStatus.FailInstrumentation;
                reasonCode = "MEASUREMENT_SAMPLE_COVERAGE";
                message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Metric '{0}' accepted only {1:P2} of its finite measurement inputs; the configured minimum valid-input coverage is {2:P0}.",
                    coverageFailureMetricID,
                    coverageRatio,
                    1d - PerformanceTelemetry.MAX_MEASUREMENT_DROP_RATIO);
            }
            Directory.CreateDirectory(Context.ResultsDirectory);

            string rawTracePath = WriteRawTrace();
            string summaryName = partial ? "summary.partial.json" : "summary.json";
            string summaryPath = Path.Combine(Context.ResultsDirectory, summaryName);
            WriteAtomically(summaryPath, writer => WriteSummary(writer, status, reasonCode, message, rawTracePath, partial));
            return true;
        }

        private string WriteRawTrace()
        {
            bool hasSamples = false;
            foreach (PerformanceMetricRegistration registration in _registrations.Values)
            {
                if (registration.Buffer != null && registration.Buffer.Count > 0)
                {
                    hasSamples = true;
                    break;
                }
            }

            if (!hasSamples)
                return null;

            string relativePath = "samples.ndjson";
            string path = Path.Combine(Context.ResultsDirectory, relativePath);
            WriteAtomically(path, writer =>
            {
                foreach (PerformanceMetricRegistration registration in _registrations.Values)
                {
                    if (registration.Buffer != null && registration.Buffer.Count > 0)
                        registration.Buffer.WriteRawSamples(writer, registration.Definition, Context, StartedUtc);
                }
            });
            return relativePath;
        }

        private void WriteSummary(
            StreamWriter writer,
            PerformanceRunStatus status,
            string reasonCode,
            string message,
            string rawTracePath,
            bool partial)
        {
            writer.WriteLine('{');
            PerformanceJson.WriteProperty(writer, "schema_version", "benchmark-summary/v1", true);
            PerformanceJson.WriteProperty(writer, "run_id", Context.RunId, true);
            PerformanceJson.WriteNullableProperty(writer, "fixture_sha256", Context.FixtureSha256, true);
            PerformanceJson.WriteProperty(writer, "status", PerformanceRunText.ToText(status), true);
            PerformanceJson.WriteProperty(writer, "started_utc", StartedUtc.ToString("o"), true);
            PerformanceJson.WriteProperty(writer, "finished_utc", DateTime.UtcNow.ToString("o"), true);
            PerformanceJson.WriteRawProperty(writer, "phase_completion", BuildPhaseCompletionJson(), true);
            PerformanceJson.WriteRawProperty(writer, "session", BuildSessionJson(), true);
            PerformanceJson.WriteRawProperty(writer, "stop", BuildStopJson(reasonCode, message), true);
            PerformanceJson.WriteRawProperty(writer, "correctness", "{\"status\":\"unverified\",\"checked\":0,\"failed\":0,\"unverified\":[]}", true);
            PerformanceJson.WriteRawProperty(writer, "metrics", BuildMetricsJson(), true);
            PerformanceJson.WriteRawProperty(
                writer,
                "raw_files",
                rawTracePath == null ? "[]" : "[\"samples.ndjson\"]",
                true);
            PerformanceJson.WriteRawProperty(writer, "warnings", BuildWarningsJson(), false);
            writer.WriteLine();
            writer.Write('}');
        }

        private string BuildPhaseCompletionJson()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{\"preflight\":{0},\"ramp\":{1},\"warmup\":{2},\"measurement\":{3},\"recovery\":{4}}}",
                _preflightComplete ? "true" : "false",
                _rampComplete ? "true" : "false",
                _warmupComplete ? "true" : "false",
                _measurementPhaseEntered ? "true" : "false",
                _recoveryComplete ? "true" : "false");
        }

        private string BuildSessionJson()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append('{');
            PerformanceJson.AppendStringProperty(builder, "git_repository", Context.GitRepository, true);
            PerformanceJson.AppendStringProperty(builder, "git_branch", Context.GitBranch, true);
            PerformanceJson.AppendStringProperty(builder, "git_captured_utc", Context.GitCapturedUtc, true);
            PerformanceJson.AppendStringProperty(builder, "git_sha", Context.GitCommitSha, true);
            PerformanceJson.AppendNullableProperty(builder, "git_dirty", Context.GitDirty, true);
            PerformanceJson.AppendStringProperty(builder, "build_kind", Context.BuildKind, true);
            PerformanceJson.AppendStringProperty(builder, "build_flags", Context.BuildFlags, true);
            PerformanceJson.AppendStringProperty(builder, "profile_revision", Context.ProfileRevision, true);
            PerformanceJson.AppendStringProperty(builder, "seed", Context.Seed, true);
            PerformanceJson.AppendStringProperty(builder, "platform", Context.Platform, true);
            PerformanceJson.AppendStringProperty(builder, "graphics_options", Context.GraphicsOptions, true);
            PerformanceJson.AppendStringProperty(builder, "hardware", Context.Hardware, true);
            PerformanceJson.AppendStringProperty(builder, "host_id", Context.HostId, true);
            PerformanceJson.AppendStringProperty(builder, "process_id", Context.ProcessId, false);
            builder.Append('}');
            return builder.ToString();
        }

        private string BuildStopJson(string reasonCode, string message)
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append('{');
            PerformanceJson.AppendStringProperty(builder, "phase", PerformanceMetricText.ToText(_currentPhase), true);
            PerformanceJson.AppendStringProperty(builder, "reason_code", reasonCode, true);
            PerformanceJson.AppendStringProperty(builder, "message", message, false);
            builder.Append('}');
            return builder.ToString();
        }

        private string BuildMetricsJson()
        {
            StringBuilder builder = new StringBuilder(Math.Max(128, _registrations.Count * 256));
            builder.Append('[');
            bool first = true;
            foreach (PerformanceMetricRegistration registration in _registrations.Values)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                AppendMetric(builder, registration);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static void AppendMetric(StringBuilder builder, PerformanceMetricRegistration registration)
        {
            PerformanceMetricSnapshot snapshot = registration.Buffer == null
                ? PerformanceMetricSnapshot.Empty(0, 0, 0)
                : registration.Buffer.CreateSnapshot();
            PerformanceMetricObservation observation = registration.Observation;
            string reason = registration.Reason;
            if ((observation == PerformanceMetricObservation.Measured
                    || observation == PerformanceMetricObservation.Estimated)
                && snapshot.SampleCount == 0)
            {
                observation = PerformanceMetricObservation.Unavailable;
                reason = "no_measurement_samples";
            }
            else if (snapshot.CoverageRatio.HasValue
                && snapshot.CoverageRatio.Value < 1d - PerformanceTelemetry.MAX_MEASUREMENT_DROP_RATIO)
            {
                observation = PerformanceMetricObservation.Unavailable;
                reason = "measurement_sample_coverage_below_policy";
            }
            else if (snapshot.SampleCount > 0 && snapshot.P99 == null && string.IsNullOrEmpty(reason))
            {
                reason = "insufficient_samples";
            }

            int retainedSampleCount = snapshot.SampleCount;
            double? measurementCoverage = snapshot.CoverageRatio;
            bool publishesStatistics = observation == PerformanceMetricObservation.Measured
                || observation == PerformanceMetricObservation.Estimated;
            if (!publishesStatistics)
            {
                snapshot = PerformanceMetricSnapshot.Empty(
                    snapshot.AttemptedSampleCount,
                    snapshot.DroppedSampleCount,
                    snapshot.RawTraceSampleCount,
                    snapshot.RejectedSampleCount,
                    snapshot.CapacityOverflowCount,
                    snapshot.ReservoirReplacementCount);
            }

            builder.Append('{');
            PerformanceJson.AppendStringProperty(builder, "metric_id", registration.Definition.MetricId, true);
            PerformanceJson.AppendStringProperty(builder, "source", PerformanceMetricText.ToText(registration.Definition.Side), true);
            PerformanceJson.AppendStringProperty(builder, "subsystem", registration.Definition.Subsystem, true);
            PerformanceJson.AppendStringProperty(builder, "scope", registration.Definition.Scope, true);
            PerformanceJson.AppendStringProperty(builder, "phase", PerformanceMetricText.ToText(PerformanceMeasurementPhase.Measurement), true);
            PerformanceJson.AppendStringProperty(builder, "unit", PerformanceMetricText.ToText(registration.Definition.Unit), true);
            PerformanceJson.AppendStringProperty(builder, "observation", PerformanceMetricText.ToText(observation), true);
            PerformanceJson.AppendNumberProperty(builder, "sample_count", snapshot.SampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "attempted_sample_count", snapshot.AttemptedSampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "retained_sample_count", retainedSampleCount, true);
            PerformanceJson.AppendNullableProperty(builder, "min", snapshot.Minimum, true);
            PerformanceJson.AppendNullableProperty(builder, "max", snapshot.Maximum, true);
            PerformanceJson.AppendNullableProperty(builder, "mean", snapshot.Mean, true);
            PerformanceJson.AppendNullableProperty(builder, "p50", snapshot.P50, true);
            PerformanceJson.AppendNullableProperty(builder, "p95", snapshot.P95, true);
            PerformanceJson.AppendNullableProperty(builder, "p99", snapshot.P99, true);
            PerformanceJson.AppendNullableProperty(builder, "raw_trace", registration.Buffer != null && registration.Buffer.Count > 0 ? "samples.ndjson" : null, true);
            PerformanceJson.AppendNullableProperty(builder, "reason", reason, true);
            PerformanceJson.AppendNumberProperty(builder, "dropped_sample_count", snapshot.DroppedSampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "rejected_sample_count", snapshot.RejectedSampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "capacity_overflow_count", snapshot.CapacityOverflowCount, true);
            PerformanceJson.AppendNumberProperty(builder, "reservoir_replacement_count", snapshot.ReservoirReplacementCount, true);
            PerformanceJson.AppendNumberProperty(builder, "raw_trace_sample_count", snapshot.RawTraceSampleCount, true);
            PerformanceJson.AppendNullableProperty(builder, "valid_sample_coverage", snapshot.ValidInputCoverageRatio, true);
            PerformanceJson.AppendNullableProperty(builder, "measurement_coverage", measurementCoverage, true);
            PerformanceJson.AppendStringProperty(
                builder,
                "retention_policy",
                registration.Buffer == null
                    ? "not_collected"
                    : snapshot.IsReservoirSampled ? "phase_reserved_reservoir" : "phase_reserved_exact",
                false);
            builder.Append('}');
        }

        private string BuildWarningsJson()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append('[');
            bool first = true;
            foreach (PerformanceMetricRegistration registration in _registrations.Values)
            {
                if (registration.Buffer == null
                    || (registration.Buffer.DroppedCount == 0
                        && registration.Buffer.RawTraceSampleCount == registration.Buffer.Count))
                    continue;
                if (!first)
                    builder.Append(',');
                first = false;
                string warning = registration.Definition.MetricId + ": ";
                if (registration.Buffer.MeasurementCapacityOverflowCount > 0)
                {
                    warning += "bounded statistical retention overflowed the measurement capacity";
                    if (registration.Buffer.MeasurementAttemptedCount > 0)
                    {
                        warning += string.Format(
                            CultureInfo.InvariantCulture,
                            "; retained {0} of {1} attempts; valid input {2:P2}; retention {3:P2}; overflows {4}; replacements {5}",
                            registration.Buffer.MeasurementRetainedCount,
                            registration.Buffer.MeasurementAttemptedCount,
                            (registration.Buffer.MeasurementAttemptedCount - registration.Buffer.MeasurementRejectedCount)
                                / (double)registration.Buffer.MeasurementAttemptedCount,
                            registration.Buffer.MeasurementRetainedCount
                                / (double)registration.Buffer.MeasurementAttemptedCount,
                            registration.Buffer.MeasurementCapacityOverflowCount,
                            registration.Buffer.MeasurementReservoirReplacementCount);
                    }
                }
                if (registration.Buffer.MeasurementRejectedCount > 0)
                {
                    if (registration.Buffer.MeasurementCapacityOverflowCount > 0)
                        warning += "; ";
                    warning += string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} invalid measurement samples rejected",
                        registration.Buffer.MeasurementRejectedCount);
                }
                if (registration.Buffer.DroppedCount > 0
                    && registration.Buffer.MeasurementCapacityOverflowCount == 0
                    && registration.Buffer.MeasurementRejectedCount == 0)
                {
                    warning += "bounded context retention dropped samples";
                }
                if (registration.Buffer.RawTraceSampleCount < registration.Buffer.Count)
                {
                    if (registration.Buffer.DroppedCount > 0)
                        warning += "; ";
                    warning += "raw trace was deliberately phase-budgeted";
                }
                warning += ".";
                PerformanceJson.AppendQuoted(builder, warning);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private bool TryGetMeasurementCoverageFailure(out string metricID, out double coverageRatio)
        {
            foreach (PerformanceMetricRegistration registration in _registrations.Values)
            {
                if (registration.Buffer == null
                    || (registration.Observation != PerformanceMetricObservation.Measured
                        && registration.Observation != PerformanceMetricObservation.Estimated))
                    continue;

                PerformanceMetricSnapshot snapshot = registration.Buffer.CreateSnapshot();
                if (!snapshot.CoverageRatio.HasValue)
                    continue;

                coverageRatio = snapshot.CoverageRatio.Value;
                if (coverageRatio < 1d - PerformanceTelemetry.MAX_MEASUREMENT_DROP_RATIO)
                {
                    metricID = registration.Definition.MetricId;
                    return true;
                }
            }

            metricID = null;
            coverageRatio = 1d;
            return false;
        }

        private static void ValidateStop(string reasonCode, string message)
        {
            if (string.IsNullOrWhiteSpace(reasonCode))
                throw new ArgumentException("A stop reason code is required.", nameof(reasonCode));
            if (reasonCode.Length > 128)
                throw new ArgumentException("Stop reason code exceeds its bounded length.", nameof(reasonCode));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A stop message is required.", nameof(message));
            if (message.Length > 1024)
                throw new ArgumentException("Stop message exceeds its bounded length.", nameof(message));
        }

        private static void WriteAtomically(string path, Action<StreamWriter> write)
        {
            string temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 16 * 1024, FileOptions.WriteThrough))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 16 * 1024))
            {
                write(writer);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporaryPath, path);
        }
    }

    internal sealed class PerformanceMetricRegistration
    {
        public PerformanceMetricRegistration(
            PerformanceMetricDefinition definition,
            int capacity,
            PerformanceMetricObservation observation,
            string reason,
            PerformanceSampleBuffer buffer)
        {
            Definition = definition;
            Capacity = capacity;
            Observation = observation;
            Reason = reason;
            Buffer = buffer;
        }

        public PerformanceMetricDefinition Definition { get; }

        public int Capacity { get; }

        public PerformanceMetricObservation Observation { get; }

        public string Reason { get; }

        public PerformanceSampleBuffer Buffer { get; }

        public bool Matches(
            PerformanceMetricDefinition definition,
            int capacity,
            PerformanceMetricObservation observation,
            string reason)
        {
            return Capacity == capacity
                && Observation == observation
                && string.Equals(Reason, reason, StringComparison.Ordinal)
                && Definition.Unit == definition.Unit
                && Definition.Side == definition.Side
                && string.Equals(Definition.Subsystem, definition.Subsystem, StringComparison.Ordinal)
                && string.Equals(Definition.Scope, definition.Scope, StringComparison.Ordinal);
        }
    }

    internal static class PerformanceRunText
    {
        public static string ToText(PerformanceRunStatus status)
        {
            switch (status)
            {
                case PerformanceRunStatus.CompleteMeasured:
                    return "COMPLETE-MEASURED";
                case PerformanceRunStatus.BlockedPrerequisite:
                    return "BLOCKED-PREREQUISITE";
                case PerformanceRunStatus.FailCorrectness:
                    return "FAIL-CORRECTNESS";
                case PerformanceRunStatus.FailProvision:
                    return "FAIL-PROVISION";
                case PerformanceRunStatus.FailResource:
                    return "FAIL-RESOURCE";
                case PerformanceRunStatus.FailTimeout:
                    return "FAIL-TIMEOUT";
                case PerformanceRunStatus.FailInstrumentation:
                    return "FAIL-INSTRUMENTATION";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown performance run status.");
            }
        }
    }

    internal static class PerformanceJson
    {
        public static void WriteProperty(StreamWriter writer, string name, string value, bool trailingComma)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            if (value == null)
                writer.Write("null");
            else
                WriteQuoted(writer, value);
            if (trailingComma)
                writer.Write(',');
        }

        public static void WriteProperty(StreamWriter writer, string name, long value, bool trailingComma)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            writer.Write(value.ToString(CultureInfo.InvariantCulture));
            if (trailingComma)
                writer.Write(',');
        }

        public static void WriteProperty(StreamWriter writer, string name, double value, bool trailingComma)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
            if (trailingComma)
                writer.Write(',');
        }

        public static void WriteNullableProperty(StreamWriter writer, string name, long? value, bool trailingComma)
        {
            WriteNullableTextProperty(writer, name, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null, trailingComma);
        }

        public static void WriteNullableProperty(StreamWriter writer, string name, double? value, bool trailingComma)
        {
            WriteNullableTextProperty(writer, name, value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : null, trailingComma);
        }

        public static void WriteNullableProperty(StreamWriter writer, string name, bool? value, bool trailingComma)
        {
            WriteNullableTextProperty(writer, name, value.HasValue ? (value.Value ? "true" : "false") : null, trailingComma, true);
        }

        public static void WriteNullableProperty(StreamWriter writer, string name, string value, bool trailingComma)
        {
            WriteNullableTextProperty(writer, name, value, trailingComma);
        }

        public static void WriteRawProperty(StreamWriter writer, string name, string rawJson, bool trailingComma)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            writer.Write(rawJson);
            if (trailingComma)
                writer.Write(',');
        }

        public static void AppendStringProperty(StringBuilder builder, string name, string value, bool trailingComma)
        {
            AppendName(builder, name);
            if (value == null)
                builder.Append("null");
            else
                AppendQuoted(builder, value);
            AppendComma(builder, trailingComma);
        }

        public static void AppendNullableProperty(StringBuilder builder, string name, double? value, bool trailingComma)
        {
            AppendName(builder, name);
            if (value.HasValue)
                builder.Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            else
                builder.Append("null");
            AppendComma(builder, trailingComma);
        }

        public static void AppendNullableProperty(StringBuilder builder, string name, long? value, bool trailingComma)
        {
            AppendName(builder, name);
            if (value.HasValue)
                builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
            else
                builder.Append("null");
            AppendComma(builder, trailingComma);
        }

        public static void AppendNullableProperty(StringBuilder builder, string name, bool? value, bool trailingComma)
        {
            AppendName(builder, name);
            if (value.HasValue)
                builder.Append(value.Value ? "true" : "false");
            else
                builder.Append("null");
            AppendComma(builder, trailingComma);
        }

        public static void AppendNullableProperty(StringBuilder builder, string name, string value, bool trailingComma)
        {
            AppendName(builder, name);
            if (value == null)
                builder.Append("null");
            else
                AppendQuoted(builder, value);
            AppendComma(builder, trailingComma);
        }

        public static void AppendNumberProperty(StringBuilder builder, string name, int value, bool trailingComma)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            AppendComma(builder, trailingComma);
        }

        public static void AppendNumberProperty(StringBuilder builder, string name, long value, bool trailingComma)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            AppendComma(builder, trailingComma);
        }

        public static void AppendQuoted(StringBuilder builder, string value)
        {
            builder.Append('"');
            builder.Append(Escape(value));
            builder.Append('"');
        }

        private static void WriteNullableTextProperty(StreamWriter writer, string name, string value, bool trailingComma, bool raw = false)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            if (value == null)
                writer.Write("null");
            else if (raw)
                writer.Write(value);
            else
                WriteQuoted(writer, value);
            if (trailingComma)
                writer.Write(',');
        }

        private static void WriteQuoted(StreamWriter writer, string value)
        {
            writer.Write('"');
            writer.Write(Escape(value));
            writer.Write('"');
        }

        private static void AppendName(StringBuilder builder, string name)
        {
            builder.Append('"');
            builder.Append(Escape(name));
            builder.Append("\":");
        }

        private static void AppendComma(StringBuilder builder, bool trailingComma)
        {
            if (trailingComma)
                builder.Append(',');
        }

        private static string Escape(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < 32)
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
