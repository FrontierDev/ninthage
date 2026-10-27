using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Performance;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Game.Client.Performance
{
    /// <summary>Owns opt-in continuous client profiling and its single aggregate report.</summary>
    internal static class ClientPerformanceProfiler
    {
        private const string SOURCE_COMMIT_ENVIRONMENT_VARIABLE = "NINTHAGE_SOURCE_COMMIT";
        private static bool _started;
        private static bool _finalized;
        private static bool _telemetryStarted;
        private static string _reportPath;

        internal static bool IsActive => _started && !_finalized;

        internal static void Start(string profileID)
        {
            if (_started)
                throw new InvalidOperationException("Live client profiling has already been started.");
            if (string.IsNullOrWhiteSpace(profileID))
                throw new ArgumentException("A profiling profile ID is required.", nameof(profileID));

            ClientPerformanceSessionContext context = CreateSessionContext(profileID);
            _started = true;
            _finalized = false;
            _telemetryStarted = false;
            _reportPath = context.ReportPath;
            Application.quitting += OnApplicationQuitting;

            try
            {
                if (PerformanceTelemetry.IsEnabled)
                    throw new InvalidOperationException("A performance telemetry session is already active.");

                _telemetryStarted = true;
                PerformanceTelemetry.StartLiveSession(context);
                ClientInitialization.RegisterPerformanceMetrics();
                Debug.Log($"Live client profiling started. Report: {_reportPath}");
            }
            catch (Exception exception)
            {
                Stop(
                    PerformanceRunStatus.FailInstrumentation,
                    "PROFILE_STARTUP_FAILED",
                    "Live client profiling startup failed: " + exception.Message);
                throw;
            }
        }

        internal static void Stop(PerformanceRunStatus status, string reasonCode, string message)
        {
            if (!_started || _finalized)
                return;

            _finalized = true;
            Application.quitting -= OnApplicationQuitting;
            if (!_telemetryStarted || !PerformanceTelemetry.IsEnabled)
                return;

            try
            {
                PerformanceTelemetry.StopLiveSession(status, reasonCode, LimitMessage(message));
                Debug.Log($"Live client profiling report written: {_reportPath}");
            }
            catch (Exception exception)
            {
                Debug.Error($"Live client profiling report could not be written to '{_reportPath}': {exception.Message}");
            }
        }

        private static void OnApplicationQuitting()
        {
            Stop(
                PerformanceRunStatus.CompleteMeasured,
                "CLIENT_PROCESS_EXIT",
                "The profiled client process exited normally.");
        }

        private static ClientPerformanceSessionContext CreateSessionContext(string profileID)
        {
            NetworkManager networkManager = NetworkManager.main;
            if (networkManager == null)
                throw new InvalidOperationException("Live client profiling requires an active NetworkManager.");

            ITransport rawTransport = networkManager.rawTransport;
            if (rawTransport == null)
                throw new InvalidOperationException("Live client profiling requires an active NetworkManager raw transport.");

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Live client profiling requires a graphics device.");

            string reportDirectory = ResolveReportDirectory();
            int processID = Process.GetCurrentProcess().Id;
            DateTime timestamp = DateTime.UtcNow;
            string reportFileName;
            string runID;
            do
            {
                string timestampText = timestamp.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
                string processIDText = processID.ToString(CultureInfo.InvariantCulture);
                runID = "client-" + timestampText + "-" + processIDText;
                reportFileName = "client-performance-report-" + timestampText + "-" + processIDText + ".json";
                timestamp = timestamp.AddMilliseconds(1d);
            }
            while (File.Exists(Path.Combine(reportDirectory, reportFileName)));

            return new ClientPerformanceSessionContext(
                "live-client",
                runID,
                profileID,
                "continuous",
                ResolveSourceCommit(),
                "runtime",
                1d,
                reportDirectory,
                reportFileName);
        }

        private static string ResolveReportDirectory()
        {
            string consoleLogPath = Application.consoleLogPath;
            if (string.IsNullOrWhiteSpace(consoleLogPath))
                throw new InvalidOperationException("Live client profiling requires a usable Application.consoleLogPath.");

            string fullLogPath;
            try
            {
                fullLogPath = Path.GetFullPath(consoleLogPath);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Application.consoleLogPath could not be normalized.", exception);
            }

            string logDirectory = Path.GetDirectoryName(fullLogPath);
            if (string.IsNullOrWhiteSpace(logDirectory))
                throw new InvalidOperationException("Application.consoleLogPath has no usable parent directory.");

            string reportDirectory = Path.Combine(logDirectory, "Reports");
            try
            {
                Directory.CreateDirectory(reportDirectory);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "The Reports directory beside Application.consoleLogPath could not be created: " + reportDirectory,
                    exception);
            }

            if (!Directory.Exists(reportDirectory))
                throw new InvalidOperationException("The profiling report directory does not exist: " + reportDirectory);

            return reportDirectory;
        }

        private static string ResolveSourceCommit()
        {
            string sourceCommit = Environment.GetEnvironmentVariable(SOURCE_COMMIT_ENVIRONMENT_VARIABLE);
            if (!string.IsNullOrWhiteSpace(sourceCommit))
                return sourceCommit;

            string dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
                throw new InvalidOperationException(
                    "Live client profiling requires source commit metadata through "
                    + SOURCE_COMMIT_ENVIRONMENT_VARIABLE
                    + " when Application.dataPath is unavailable.");

            DirectoryInfo dataDirectory = Directory.GetParent(dataPath);
            if (dataDirectory == null)
            {
                throw new InvalidOperationException(
                    "Live client profiling requires source commit metadata through "
                    + SOURCE_COMMIT_ENVIRONMENT_VARIABLE
                    + " when the Unity project directory cannot be resolved.");
            }

            try
            {
                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse HEAD",
                        WorkingDirectory = dataDirectory.FullName,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                if (!process.Start() || !process.WaitForExit(1000))
                {
                    if (!process.HasExited)
                        process.Kill();
                    throw new InvalidOperationException("The Git source commit could not be resolved within one second.");
                }

                sourceCommit = process.StandardOutput.ReadToEnd().Trim();
                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(sourceCommit))
                    throw new InvalidOperationException("The Git source commit could not be resolved.");

                return sourceCommit;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Live client profiling could not resolve source commit metadata.",
                    exception);
            }
        }

        private static string LimitMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Live client profiling stopped without a diagnostic message.";
            return message.Length <= 1024 ? message : message.Substring(0, 1021) + "...";
        }
    }
}
