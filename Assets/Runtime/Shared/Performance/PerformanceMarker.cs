using System;
using System.Diagnostics;
using Unity.Profiling;

namespace Game.Shared.Performance
{
    /// <summary>Combines a Unity profiler marker with an optional bounded duration sample.</summary>
    public readonly struct PerformanceMarker
    {
        private readonly ProfilerMarker _profilerMarker;
        private readonly PerformanceMetricDefinition _definition;

        public PerformanceMarker(PerformanceMetricDefinition definition, string profilerMarkerName)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.Unit != PerformanceMetricUnit.Milliseconds)
                throw new ArgumentException("A duration marker must use milliseconds.", nameof(definition));
            if (string.IsNullOrWhiteSpace(profilerMarkerName))
                throw new ArgumentException("A profiler marker name is required.", nameof(profilerMarkerName));

            _definition = definition;
            _profilerMarker = new ProfilerMarker(profilerMarkerName);
        }

        public PerformanceMarkerScope Begin(long frameOrTick = -1, double intervalSeconds = -1)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return default;

            _profilerMarker.Begin();
            return new PerformanceMarkerScope(
                _profilerMarker,
                _definition,
                Stopwatch.GetTimestamp(),
                frameOrTick,
                intervalSeconds);
        }
    }

    /// <summary>Stack-only scope returned by <see cref="PerformanceMarker.Begin"/>.</summary>
    public readonly struct PerformanceMarkerScope : IDisposable
    {
        private readonly ProfilerMarker _profilerMarker;
        private readonly PerformanceMetricDefinition _definition;
        private readonly long _startedTimestamp;
        private readonly long _frameOrTick;
        private readonly double _intervalSeconds;

        internal PerformanceMarkerScope(
            ProfilerMarker profilerMarker,
            PerformanceMetricDefinition definition,
            long startedTimestamp,
            long frameOrTick,
            double intervalSeconds)
        {
            _profilerMarker = profilerMarker;
            _definition = definition;
            _startedTimestamp = startedTimestamp;
            _frameOrTick = frameOrTick;
            _intervalSeconds = intervalSeconds;
        }

        public void Dispose()
        {
            if (_startedTimestamp == 0)
                return;

            long finishedTimestamp = Stopwatch.GetTimestamp();
            _profilerMarker.End();
            PerformanceTelemetry.TryRecordDuration(
                _definition,
                (finishedTimestamp - _startedTimestamp) * 1000d / Stopwatch.Frequency,
                _frameOrTick,
                _intervalSeconds);
        }
    }
}
