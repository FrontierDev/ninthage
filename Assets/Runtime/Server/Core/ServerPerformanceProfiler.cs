using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Owns opt-in live server profiling and its single aggregate report.</summary>
    internal static class ServerPerformanceProfiler
    {
        private static bool _started;
        private static bool _finalized;
        private static string _reportPath;

        internal static bool IsActive => _started && !_finalized;

        internal static void Start(string worldName)
        {
            if (_started)
                throw new InvalidOperationException("Live server profiling has already been started.");
            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("A profiling world name is required.", nameof(worldName));

            PerformanceSessionContext context = CreateSessionContext(worldName);
            _started = true;
            _finalized = false;
            _reportPath = context.ReportPath;
            Application.quitting += OnApplicationQuitting;

            try
            {
                PerformanceTelemetry.StartLiveSession(context);
                ServerInitialization.RegisterPerformanceMetrics();
                Debug.Log($"Live server profiling started. Report: {_reportPath}");
            }
            catch (Exception exception)
            {
                Stop(
                    PerformanceRunStatus.FailInstrumentation,
                    "PROFILE_STARTUP_FAILED",
                    "Live profiling startup failed: " + exception.Message);
                throw;
            }
        }

        internal static void Stop(PerformanceRunStatus status, string reasonCode, string message)
        {
            if (!_started || _finalized)
                return;

            _finalized = true;
            Application.quitting -= OnApplicationQuitting;
            if (!PerformanceTelemetry.IsEnabled)
                return;

            try
            {
                PerformanceTelemetry.StopLiveSession(status, reasonCode, LimitMessage(message));
                Debug.Log($"Live server profiling report written: {_reportPath}");
            }
            catch (Exception exception)
            {
                Debug.Error($"Live server profiling report could not be written to '{_reportPath}': {exception.Message}");
            }
        }

        private static void OnApplicationQuitting()
        {
            Stop(
                PerformanceRunStatus.FailInstrumentation,
                "PROCESS_QUIT",
                "The application quit before the normal server shutdown path finalised profiling.");
        }

        private static PerformanceSessionContext CreateSessionContext(string worldName)
        {
            string reportDirectory = ResolveReportDirectory();
            int processID = Process.GetCurrentProcess().Id;
            DateTime timestamp = DateTime.UtcNow;
            string reportFileName;
            string runID;
            do
            {
                string timestampText = timestamp.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
                runID = "server-" + timestampText + "-" + processID.ToString(CultureInfo.InvariantCulture);
                reportFileName = "performance-report-" + timestampText + "-" + processID.ToString(CultureInfo.InvariantCulture) + ".json";
                timestamp = timestamp.AddMilliseconds(1d);
            }
            while (File.Exists(Path.Combine(reportDirectory, reportFileName)));

            return new PerformanceSessionContext(runID, reportDirectory, reportFileName, worldName);
        }

        private static string ResolveReportDirectory()
        {
            string consoleLogPath = Application.consoleLogPath;
            if (string.IsNullOrWhiteSpace(consoleLogPath))
                throw new InvalidOperationException("Live profiling requires a usable Application.consoleLogPath.");

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

        private static string LimitMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Live server profiling stopped without a diagnostic message.";
            return message.Length <= 1024 ? message : message.Substring(0, 1021) + "...";
        }
    }
}
