using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Shared.Performance
{
    /// <summary>Identity and output context for one live server profiling session.</summary>
    public sealed class PerformanceSessionContext
    {
        public PerformanceSessionContext(
            string runId,
            string profileId,
            string resultsDirectory,
            string reportFileName,
            string worldName,
            string buildGuid,
            string operatingSystem,
            string processorType,
            int processorCount,
            int systemMemoryMB,
            string transportType,
            int serverPlayerLoopTargetFrameRate,
            float serverSimulationTickDurationSeconds,
            int serverSimulationMaxCatchUpTicks)
        {
            RunId = RequireBounded(runId, nameof(runId), 256);
            ProfileId = RequireBounded(profileId, nameof(profileId), 128);
            WorldName = RequireBounded(worldName, nameof(worldName), 256);
            if (string.IsNullOrWhiteSpace(resultsDirectory) || !Path.IsPathRooted(resultsDirectory))
                throw new ArgumentException("An absolute profiling report directory is required.", nameof(resultsDirectory));
            if (string.IsNullOrWhiteSpace(reportFileName)
                || !string.Equals(Path.GetFileName(reportFileName), reportFileName, StringComparison.Ordinal)
                || !string.Equals(Path.GetExtension(reportFileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The profiling report file name must be a file name ending in .json.", nameof(reportFileName));
            }

            ResultsDirectory = Path.GetFullPath(resultsDirectory);
            ReportFileName = reportFileName;
            HostId = Environment.MachineName;
            ProcessId = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
            Platform = Application.platform.ToString();
            UnityVersion = Application.unityVersion;
            ApplicationVersion = Application.version;
            BuildGuid = RequireBounded(buildGuid, nameof(buildGuid), 128);
            OperatingSystem = RequireBounded(operatingSystem, nameof(operatingSystem), 256);
            ProcessorType = RequireBounded(processorType, nameof(processorType), 256);
            ProcessorCount = processorCount;
            SystemMemoryMB = systemMemoryMB;
            TransportType = RequireBounded(transportType, nameof(transportType), 256);
            ServerPlayerLoopTargetFrameRate = serverPlayerLoopTargetFrameRate;
            ServerSimulationTickDurationSeconds = serverSimulationTickDurationSeconds;
            ServerSimulationMaxCatchUpTicks = serverSimulationMaxCatchUpTicks;
        }

        public string RunId { get; }
        public string ProfileId { get; }
        public string WorldName { get; }
        public string ResultsDirectory { get; }
        public string ReportFileName { get; }
        public string ReportPath => Path.Combine(ResultsDirectory, ReportFileName);
        public string HostId { get; }
        public string ProcessId { get; }
        public string Platform { get; }
        public string UnityVersion { get; }
        public string ApplicationVersion { get; }
        public string BuildGuid { get; }
        public string OperatingSystem { get; }
        public string ProcessorType { get; }
        public int ProcessorCount { get; }
        public int SystemMemoryMB { get; }
        public string TransportType { get; }
        public int ServerPlayerLoopTargetFrameRate { get; }
        public float ServerSimulationTickDurationSeconds { get; }
        public int ServerSimulationMaxCatchUpTicks { get; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(HostId))
                throw new ArgumentException("HostId is required for a live profiling session.", nameof(HostId));
            if (string.IsNullOrWhiteSpace(ProcessId))
                throw new ArgumentException("ProcessId is required for a live profiling session.", nameof(ProcessId));
        }

        private static string RequireBounded(string value, string parameterName, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A value is required.", parameterName);
            if (value.Length > maximumLength)
                throw new ArgumentException(parameterName + " exceeds its bounded length.", parameterName);
            return value;
        }
    }

    /// <summary>Canonical terminal states for a live server profiling report.</summary>
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

    /// <summary>Controls bounded aggregation for an opt-in live server profiling session.</summary>
    public static class PerformanceTelemetry
    {
        internal const int MAX_SAMPLES_PER_METRIC = 10000;
        public const int RECOMMENDED_SAMPLES_PER_METRIC = 10000;
        internal const int MAX_REGISTERED_METRICS = 256;
        internal const double MAX_MEASUREMENT_DROP_RATIO = 0.05d;
        private static PerformanceSession _session;

        /// <summary>Raised after a live telemetry session becomes active.</summary>
        public static event Action SessionStarted;

        /// <summary>Raised after a live telemetry session has been stopped and cleared.</summary>
        public static event Action SessionEnded;

        public static bool IsEnabled => _session != null;

        /// <summary>Starts a live session immediately; no lifecycle phases or workload setup are required.</summary>
        public static void StartLiveSession(PerformanceSessionContext context)
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

        public static void RegisterMetric(
            PerformanceMetricDefinition definition,
            int capacity,
            PerformanceMetricObservation observation = PerformanceMetricObservation.Measured,
            string reason = null)
        {
            RequireSession().RegisterMetric(definition, capacity, observation, reason);
        }

        /// <summary>Records one finite metric sample without allocating or logging in the producer.</summary>
        public static bool TryRecord(
            PerformanceMetricDefinition definition,
            double value,
            long frameOrTick = -1,
            double intervalSeconds = -1)
        {
            if (_session == null)
                return false;

            return _session.TryRecord(definition, value, GetElapsedNanoseconds(), frameOrTick, intervalSeconds);
        }

        /// <summary>Finalises the active live session and writes its single aggregate report.</summary>
        public static bool StopLiveSession(PerformanceRunStatus status, string reasonCode, string message)
        {
            PerformanceSession session = RequireSession();
            try
            {
                return session.Finalize(status, reasonCode, message);
            }
            finally
            {
                Shutdown(session);
            }
        }

        internal static void TryRecordDuration(
            PerformanceMetricDefinition definition,
            double milliseconds,
            long frameOrTick,
            double intervalSeconds)
        {
            if (_session == null)
                return;

            _session.TryRecord(definition, milliseconds, GetElapsedNanoseconds(), frameOrTick, intervalSeconds);
        }

        private static void HandleApplicationQuitting()
        {
            if (_session == null)
                return;

            try
            {
                StopLiveSession(
                    PerformanceRunStatus.FailInstrumentation,
                    "PROCESS_QUIT",
                    "Process quit before the server shutdown path finalised profiling.");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("Live performance report finalisation failed during process quit: " + exception.Message);
            }
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
                throw new InvalidOperationException("No live performance telemetry session is active.");
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

        public PerformanceSession(PerformanceSessionContext context)
        {
            Context = context;
            StartedUtc = DateTime.UtcNow;
            _startTimestamp = Stopwatch.GetTimestamp();
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
            {
                throw new ArgumentException("A reason is required for an unmeasured metric.", nameof(reason));
            }
            if (reason != null && reason.Length > 512)
                throw new ArgumentException("Metric reason exceeds its bounded length.", nameof(reason));

            if (_registrations.TryGetValue(definition.MetricId, out PerformanceMetricRegistration existing))
            {
                if (existing.Matches(definition, capacity, observation, reason))
                    return;

                throw new InvalidOperationException(
                    "A metric with this ID was registered with different dimensions: " + definition.MetricId);
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
            {
                return false;
            }

            return registration.Buffer.Add(value);
        }

        public bool Finalize(PerformanceRunStatus status, string reasonCode, string message)
        {
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
            WriteAtomically(Context.ReportPath, writer => WriteReport(writer, status, reasonCode, message));
            return true;
        }

        private void WriteReport(StreamWriter writer, PerformanceRunStatus status, string reasonCode, string message)
        {
            DateTime finishedUtc = DateTime.UtcNow;
            double elapsedSeconds = (Stopwatch.GetTimestamp() - _startTimestamp) / (double)Stopwatch.Frequency;
            writer.WriteLine('{');
            PerformanceJson.WriteProperty(writer, "schema_version", "server-performance-report/v1", true);
            PerformanceJson.WriteProperty(writer, "run_id", Context.RunId, true);
            PerformanceJson.WriteProperty(writer, "profile_id", Context.ProfileId, true);
            PerformanceJson.WriteProperty(writer, "world_name", Context.WorldName, true);
            PerformanceJson.WriteProperty(writer, "process_id", Context.ProcessId, true);
            PerformanceJson.WriteProperty(writer, "host_id", Context.HostId, true);
            PerformanceJson.WriteProperty(writer, "unity_platform", Context.Platform, true);
            PerformanceJson.WriteProperty(writer, "unity_version", Context.UnityVersion, true);
            PerformanceJson.WriteProperty(writer, "application_version", Context.ApplicationVersion, true);
            PerformanceJson.WriteProperty(writer, "build_guid", Context.BuildGuid, true);
            PerformanceJson.WriteProperty(writer, "operating_system", Context.OperatingSystem, true);
            PerformanceJson.WriteProperty(writer, "processor_type", Context.ProcessorType, true);
            PerformanceJson.WriteProperty(writer, "processor_count", Context.ProcessorCount, true);
            PerformanceJson.WriteProperty(writer, "system_memory_mb", Context.SystemMemoryMB, true);
            PerformanceJson.WriteProperty(writer, "transport_type", Context.TransportType, true);
            PerformanceJson.WriteProperty(
                writer,
                "server_player_loop_target_frame_rate",
                Context.ServerPlayerLoopTargetFrameRate,
                true);
            PerformanceJson.WriteProperty(
                writer,
                "server_simulation_tick_duration_seconds",
                Context.ServerSimulationTickDurationSeconds,
                true);
            PerformanceJson.WriteProperty(
                writer,
                "server_simulation_max_catch_up_ticks",
                Context.ServerSimulationMaxCatchUpTicks,
                true);
            PerformanceJson.WriteProperty(writer, "started_utc", StartedUtc.ToString("o"), true);
            PerformanceJson.WriteProperty(writer, "finished_utc", finishedUtc.ToString("o"), true);
            PerformanceJson.WriteProperty(writer, "elapsed_seconds", elapsedSeconds, true);
            PerformanceJson.WriteProperty(writer, "status", PerformanceRunText.ToText(status), true);
            PerformanceJson.WriteRawProperty(writer, "shutdown", BuildShutdownJson(status, reasonCode, message), true);
            PerformanceJson.WriteRawProperty(writer, "metrics", BuildMetricsJson(), true);
            PerformanceJson.WriteRawProperty(writer, "warnings", BuildWarningsJson(), false);
            writer.WriteLine();
            writer.Write('}');
        }

        private string BuildShutdownJson(PerformanceRunStatus status, string reasonCode, string message)
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append('{');
            PerformanceJson.AppendStringProperty(builder, "status", PerformanceRunText.ToText(status), true);
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
                ? PerformanceMetricSnapshot.Empty(0, 0, 0, 0)
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
            else if (snapshot.ValidInputCoverageRatio.HasValue
                && snapshot.ValidInputCoverageRatio.Value < 1d - PerformanceTelemetry.MAX_MEASUREMENT_DROP_RATIO)
            {
                observation = PerformanceMetricObservation.Unavailable;
                reason = "measurement_sample_coverage_below_policy";
            }

            bool publishesStatistics = observation == PerformanceMetricObservation.Measured
                || observation == PerformanceMetricObservation.Estimated;
            if (!publishesStatistics)
            {
                snapshot = PerformanceMetricSnapshot.Empty(
                    snapshot.AttemptedSampleCount,
                    snapshot.RejectedSampleCount,
                    snapshot.CapacityOverflowCount,
                    snapshot.ReservoirReplacementCount);
            }

            builder.Append('{');
            PerformanceJson.AppendStringProperty(builder, "metric_id", registration.Definition.MetricId, true);
            PerformanceJson.AppendStringProperty(builder, "side", PerformanceMetricText.ToText(registration.Definition.Side), true);
            PerformanceJson.AppendStringProperty(builder, "subsystem", registration.Definition.Subsystem, true);
            PerformanceJson.AppendStringProperty(builder, "scope", registration.Definition.Scope, true);
            PerformanceJson.AppendStringProperty(builder, "unit", PerformanceMetricText.ToText(registration.Definition.Unit), true);
            PerformanceJson.AppendStringProperty(builder, "observation", PerformanceMetricText.ToText(observation), true);
            PerformanceJson.AppendNumberProperty(builder, "sample_count", snapshot.SampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "attempted_sample_count", snapshot.AttemptedSampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "retained_sample_count", snapshot.SampleCount, true);
            PerformanceJson.AppendNullableProperty(builder, "min", snapshot.Minimum, true);
            PerformanceJson.AppendNullableProperty(builder, "max", snapshot.Maximum, true);
            PerformanceJson.AppendNullableProperty(builder, "mean", snapshot.Mean, true);
            PerformanceJson.AppendNullableProperty(builder, "p50", snapshot.P50, true);
            PerformanceJson.AppendNullableProperty(builder, "p95", snapshot.P95, true);
            PerformanceJson.AppendNullableProperty(builder, "p99", snapshot.P99, true);
            PerformanceJson.AppendStringProperty(builder, "reason", reason, true);
            PerformanceJson.AppendNumberProperty(builder, "rejected_sample_count", snapshot.RejectedSampleCount, true);
            PerformanceJson.AppendNumberProperty(builder, "capacity_overflow_count", snapshot.CapacityOverflowCount, true);
            PerformanceJson.AppendNumberProperty(builder, "reservoir_replacement_count", snapshot.ReservoirReplacementCount, true);
            PerformanceJson.AppendNullableProperty(builder, "valid_sample_coverage", snapshot.ValidInputCoverageRatio, true);
            PerformanceJson.AppendNullableProperty(builder, "retention_ratio", snapshot.RetentionRatio, true);
            PerformanceJson.AppendStringProperty(
                builder,
                "retention_policy",
                registration.Buffer == null ? "not_collected" : "bounded_reservoir",
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
                if (registration.Buffer == null || registration.Buffer.DroppedCount == 0)
                    continue;

                if (!first)
                    builder.Append(',');
                first = false;
                string warning = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: bounded retention dropped {1} samples ({2} invalid, {3} reservoir overflow).",
                    registration.Definition.MetricId,
                    registration.Buffer.DroppedCount,
                    registration.Buffer.RejectedCount,
                    registration.Buffer.CapacityOverflowCount);
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
                {
                    continue;
                }

                PerformanceMetricSnapshot snapshot = registration.Buffer.CreateSnapshot();
                if (!snapshot.ValidInputCoverageRatio.HasValue)
                    continue;

                coverageRatio = snapshot.ValidInputCoverageRatio.Value;
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

            using (FileStream stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                16 * 1024,
                FileOptions.WriteThrough))
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

        public static void WriteProperty(StreamWriter writer, string name, double value, bool trailingComma)
        {
            writer.Write('"');
            writer.Write(Escape(name));
            writer.Write("\":");
            writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
            if (trailingComma)
                writer.Write(',');
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
            builder.Append(value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "null");
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
