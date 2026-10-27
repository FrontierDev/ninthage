using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

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

    /// <summary>Identity and captured client environment for one live profiling session.</summary>
    public sealed class ClientPerformanceSessionContext
    {
        /// <summary>Creates a client profiling context from authored run identity and the current process environment.</summary>
        public ClientPerformanceSessionContext(
            string runGroupID,
            string runID,
            string profileID,
            string profileVariant,
            string sourceCommit,
            string phase,
            double phaseDurationSeconds,
            string resultsDirectory,
            string reportFileName)
        {
            RunGroupID = RequireBounded(runGroupID, nameof(runGroupID), 256);
            RunID = RequireBounded(runID, nameof(runID), 256);
            ProfileID = RequireBounded(profileID, nameof(profileID), 128);
            ProfileVariant = RequireBounded(profileVariant, nameof(profileVariant), 128);
            SourceCommit = RequireGitSHA(sourceCommit, nameof(sourceCommit));
            Phase = RequireBounded(phase, nameof(phase), 128);
            PhaseDurationSeconds = RequirePositiveFinite(phaseDurationSeconds, nameof(phaseDurationSeconds));
            ResultsDirectory = RequireAbsolutePath(resultsDirectory, nameof(resultsDirectory));
            ReportFileName = RequireReportFileName(reportFileName, nameof(reportFileName));

            HostID = RequireRuntimeValue(Environment.MachineName, nameof(HostID));
            ProcessID = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
            Platform = RequireRuntimeValue(Application.platform.ToString(), nameof(Platform));
            UnityVersion = RequireRuntimeValue(Application.unityVersion, nameof(UnityVersion));
            ApplicationVersion = RequireRuntimeValue(Application.version, nameof(ApplicationVersion));
            BuildGUID = Application.buildGUID;
            OperatingSystem = RequireRuntimeValue(SystemInfo.operatingSystem, nameof(OperatingSystem));
            ProcessorType = RequireRuntimeValue(SystemInfo.processorType, nameof(ProcessorType));
            ProcessorCount = SystemInfo.processorCount;
            SystemMemoryMB = SystemInfo.systemMemorySize;

            GraphicsDeviceType = SystemInfo.graphicsDeviceType;
            if (GraphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("A client profiling session requires a graphics device.");

            GraphicsDeviceName = RequireRuntimeValue(SystemInfo.graphicsDeviceName, nameof(GraphicsDeviceName));
            GraphicsDeviceVendor = RequireRuntimeValue(SystemInfo.graphicsDeviceVendor, nameof(GraphicsDeviceVendor));
            GraphicsDeviceVersion = RequireRuntimeValue(SystemInfo.graphicsDeviceVersion, nameof(GraphicsDeviceVersion));
            GraphicsMemoryMB = SystemInfo.graphicsMemorySize;

            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
            if (ScreenWidth <= 0 || ScreenHeight <= 0)
                throw new InvalidOperationException("A client profiling session requires a positive screen size.");

            QualityLevel = QualitySettings.GetQualityLevel();
            string[] qualityNames = QualitySettings.names;
            if (qualityNames == null
                || QualityLevel < 0
                || QualityLevel >= qualityNames.Length
                || string.IsNullOrWhiteSpace(qualityNames[QualityLevel]))
            {
                throw new InvalidOperationException("The active Unity quality level could not be resolved.");
            }

            QualityName = qualityNames[QualityLevel];
            RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
            if (renderPipeline == null || string.IsNullOrWhiteSpace(renderPipeline.name))
                throw new InvalidOperationException("A client profiling session requires a named render pipeline asset.");

            FullScreenMode = Screen.fullScreenMode;
            RenderPipelineAssetName = renderPipeline.name;
            VSyncCount = QualitySettings.vSyncCount;
            TargetFrameRate = Application.targetFrameRate;
            ExecutionMode = Application.isEditor ? "editor" : "standalone";
        }

        /// <summary>Gets the authored run group identifier.</summary>
        public string RunGroupID { get; }

        /// <summary>Gets the authored run identifier.</summary>
        public string RunID { get; }

        /// <summary>Gets the authored profile identifier.</summary>
        public string ProfileID { get; }

        /// <summary>Gets the authored profile variant.</summary>
        public string ProfileVariant { get; }

        /// <summary>Gets the full 40-character source Git SHA.</summary>
        public string SourceCommit { get; }

        /// <summary>Gets the authored phase identifier.</summary>
        public string Phase { get; }

        /// <summary>Gets the authored phase duration in seconds.</summary>
        public double PhaseDurationSeconds { get; }

        /// <summary>Gets the absolute profiling results directory.</summary>
        public string ResultsDirectory { get; }

        /// <summary>Gets the profiling report file name.</summary>
        public string ReportFileName { get; }

        /// <summary>Gets the complete profiling report path.</summary>
        public string ReportPath => Path.Combine(ResultsDirectory, ReportFileName);

        /// <summary>Gets the host identifier captured from the current process.</summary>
        public string HostID { get; }

        /// <summary>Gets the current process identifier.</summary>
        public string ProcessID { get; }

        /// <summary>Gets the Unity platform captured from the current process.</summary>
        public string Platform { get; }

        /// <summary>Gets the Unity Editor/runtime version.</summary>
        public string UnityVersion { get; }

        /// <summary>Gets the application version.</summary>
        public string ApplicationVersion { get; }

        /// <summary>Gets the Unity build GUID.</summary>
        public string BuildGUID { get; }

        /// <summary>Gets the operating system description.</summary>
        public string OperatingSystem { get; }

        /// <summary>Gets the processor type description.</summary>
        public string ProcessorType { get; }

        /// <summary>Gets the processor count.</summary>
        public int ProcessorCount { get; }

        /// <summary>Gets the system memory size in megabytes.</summary>
        public int SystemMemoryMB { get; }

        /// <summary>Gets the graphics device name.</summary>
        public string GraphicsDeviceName { get; }

        /// <summary>Gets the graphics device type.</summary>
        public GraphicsDeviceType GraphicsDeviceType { get; }

        /// <summary>Gets the graphics device vendor.</summary>
        public string GraphicsDeviceVendor { get; }

        /// <summary>Gets the graphics device driver/API version.</summary>
        public string GraphicsDeviceVersion { get; }

        /// <summary>Gets the graphics memory size in megabytes.</summary>
        public int GraphicsMemoryMB { get; }

        /// <summary>Gets the current screen width in pixels.</summary>
        public int ScreenWidth { get; }

        /// <summary>Gets the current screen height in pixels.</summary>
        public int ScreenHeight { get; }

        /// <summary>Gets the current fullscreen mode.</summary>
        public FullScreenMode FullScreenMode { get; }

        /// <summary>Gets the active Unity quality level index.</summary>
        public int QualityLevel { get; }

        /// <summary>Gets the active Unity quality level name.</summary>
        public string QualityName { get; }

        /// <summary>Gets the active render-pipeline asset name.</summary>
        public string RenderPipelineAssetName { get; }

        /// <summary>Gets the active vertical-sync count.</summary>
        public int VSyncCount { get; }

        /// <summary>Gets the configured application target frame rate.</summary>
        public int TargetFrameRate { get; }

        /// <summary>Gets the execution mode, either editor or standalone.</summary>
        public string ExecutionMode { get; }

        internal void Validate()
        {
            if (GraphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("A client profiling session requires a graphics device.");
            if (ScreenWidth <= 0 || ScreenHeight <= 0)
                throw new InvalidOperationException("A client profiling session requires a positive screen size.");
            if (string.IsNullOrWhiteSpace(QualityName))
                throw new InvalidOperationException("The active Unity quality level is unavailable.");
            if (string.IsNullOrWhiteSpace(RenderPipelineAssetName))
                throw new InvalidOperationException("The active render pipeline asset is unavailable.");
        }

        private static string RequireBounded(string value, string parameterName, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A value is required.", parameterName);
            if (value.Length > maximumLength)
                throw new ArgumentException(parameterName + " exceeds its bounded length.", parameterName);
            return value;
        }

        private static string RequireGitSHA(string value, string parameterName)
        {
            if (value == null || value.Length != 40)
                throw new ArgumentException("sourceCommit must be a full 40-character hexadecimal Git SHA.", parameterName);

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isHex = character >= '0' && character <= '9'
                    || character >= 'a' && character <= 'f'
                    || character >= 'A' && character <= 'F';
                if (!isHex)
                    throw new ArgumentException("sourceCommit must be a full 40-character hexadecimal Git SHA.", parameterName);
            }

            return value;
        }

        private static double RequirePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentException(parameterName + " must be finite and greater than zero.", parameterName);
            return value;
        }

        private static string RequireAbsolutePath(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !Path.IsPathRooted(value))
                throw new ArgumentException("An absolute profiling report directory is required.", parameterName);

            try
            {
                return Path.GetFullPath(value);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                throw new ArgumentException("The profiling report directory must be an absolute normalizable path.", parameterName, exception);
            }
        }

        private static string RequireReportFileName(string value, string parameterName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)
                    || !string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal)
                    || !string.Equals(Path.GetExtension(value), ".json", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "The profiling report file name must be a simple file name ending in .json.",
                        parameterName);
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception exception) when (exception is NotSupportedException || exception is PathTooLongException)
            {
                throw new ArgumentException(
                    "The profiling report file name must be a simple file name ending in .json.",
                    parameterName,
                    exception);
            }

            return value;
        }

        private static string RequireRuntimeValue(string value, string valueName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(valueName + " is unavailable in the current process.");
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

        /// <summary>Starts a live client profiling session with captured client environment identity.</summary>
        public static void StartLiveSession(ClientPerformanceSessionContext context)
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
                    "Process quit before the profiling shutdown path finalised the active session.");
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
        private readonly ClientPerformanceSessionContext _clientContext;

        public PerformanceSession(PerformanceSessionContext context)
        {
            Context = context;
            StartedUtc = DateTime.UtcNow;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public PerformanceSession(ClientPerformanceSessionContext context)
        {
            _clientContext = context;
            StartedUtc = DateTime.UtcNow;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public PerformanceSessionContext Context { get; }
        public ClientPerformanceSessionContext ClientContext => _clientContext;
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

            string reportDirectory = _clientContext != null
                ? _clientContext.ResultsDirectory
                : Context.ResultsDirectory;
            string reportPath = _clientContext != null
                ? _clientContext.ReportPath
                : Context.ReportPath;
            Directory.CreateDirectory(reportDirectory);
            WriteAtomically(reportPath, writer => WriteReport(writer, status, reasonCode, message));
            return true;
        }

        private void WriteReport(StreamWriter writer, PerformanceRunStatus status, string reasonCode, string message)
        {
            DateTime finishedUtc = DateTime.UtcNow;
            double elapsedSeconds = (Stopwatch.GetTimestamp() - _startTimestamp) / (double)Stopwatch.Frequency;
            writer.WriteLine('{');
            if (_clientContext != null)
                WriteClientIdentity(writer);
            else
                WriteServerIdentity(writer);

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

        private void WriteServerIdentity(StreamWriter writer)
        {
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
        }

        private void WriteClientIdentity(StreamWriter writer)
        {
            PerformanceJson.WriteProperty(writer, "schema_version", "client-performance-report/v1", true);
            PerformanceJson.WriteProperty(writer, "run_group_id", _clientContext.RunGroupID, true);
            PerformanceJson.WriteProperty(writer, "run_id", _clientContext.RunID, true);
            PerformanceJson.WriteProperty(writer, "profile_id", _clientContext.ProfileID, true);
            PerformanceJson.WriteProperty(writer, "profile_variant", _clientContext.ProfileVariant, true);
            PerformanceJson.WriteProperty(writer, "source_commit", _clientContext.SourceCommit, true);
            PerformanceJson.WriteProperty(writer, "phase", _clientContext.Phase, true);
            PerformanceJson.WriteProperty(writer, "phase_duration_seconds", _clientContext.PhaseDurationSeconds, true);
            PerformanceJson.WriteProperty(writer, "process_id", _clientContext.ProcessID, true);
            PerformanceJson.WriteProperty(writer, "host_id", _clientContext.HostID, true);
            PerformanceJson.WriteProperty(writer, "execution_mode", _clientContext.ExecutionMode, true);
            PerformanceJson.WriteProperty(writer, "unity_platform", _clientContext.Platform, true);
            PerformanceJson.WriteProperty(writer, "unity_version", _clientContext.UnityVersion, true);
            PerformanceJson.WriteProperty(writer, "application_version", _clientContext.ApplicationVersion, true);
            PerformanceJson.WriteProperty(writer, "build_guid", _clientContext.BuildGUID, true);
            PerformanceJson.WriteProperty(writer, "operating_system", _clientContext.OperatingSystem, true);
            PerformanceJson.WriteProperty(writer, "processor_type", _clientContext.ProcessorType, true);
            PerformanceJson.WriteProperty(writer, "processor_count", _clientContext.ProcessorCount, true);
            PerformanceJson.WriteProperty(writer, "system_memory_mb", _clientContext.SystemMemoryMB, true);
            PerformanceJson.WriteProperty(writer, "graphics_device_name", _clientContext.GraphicsDeviceName, true);
            PerformanceJson.WriteProperty(writer, "graphics_device_type", _clientContext.GraphicsDeviceType.ToString(), true);
            PerformanceJson.WriteProperty(writer, "graphics_device_vendor", _clientContext.GraphicsDeviceVendor, true);
            PerformanceJson.WriteProperty(writer, "graphics_device_version", _clientContext.GraphicsDeviceVersion, true);
            PerformanceJson.WriteProperty(writer, "graphics_memory_mb", _clientContext.GraphicsMemoryMB, true);
            PerformanceJson.WriteProperty(writer, "screen_width", _clientContext.ScreenWidth, true);
            PerformanceJson.WriteProperty(writer, "screen_height", _clientContext.ScreenHeight, true);
            PerformanceJson.WriteProperty(writer, "full_screen_mode", _clientContext.FullScreenMode.ToString(), true);
            PerformanceJson.WriteProperty(writer, "quality_level", _clientContext.QualityLevel, true);
            PerformanceJson.WriteProperty(writer, "quality_name", _clientContext.QualityName, true);
            PerformanceJson.WriteProperty(writer, "render_pipeline_asset", _clientContext.RenderPipelineAssetName, true);
            PerformanceJson.WriteProperty(writer, "v_sync_count", _clientContext.VSyncCount, true);
            PerformanceJson.WriteProperty(writer, "target_frame_rate", _clientContext.TargetFrameRate, true);
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
