using System;

namespace Game.Shared.Performance
{
    internal sealed class PerformanceSampleBuffer
    {
        private readonly double[] _values;
        private readonly long[] _monotonicElapsedNanoseconds;
        private readonly long[] _frameOrTick;
        private readonly double[] _intervalSeconds;
        private readonly PerformanceMeasurementPhase[] _phases;
        private readonly bool[] _hasFrameOrTick;
        private readonly bool[] _hasInterval;
        private readonly double[] _sortedValues;
        private int _count;
        private int _nextIndex;

        public PerformanceSampleBuffer(int capacity)
        {
            if (capacity < 1 || capacity > PerformanceTelemetry.MAX_SAMPLES_PER_METRIC)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Metric sample capacity is outside the supported bound.");

            _values = new double[capacity];
            _monotonicElapsedNanoseconds = new long[capacity];
            _frameOrTick = new long[capacity];
            _intervalSeconds = new double[capacity];
            _phases = new PerformanceMeasurementPhase[capacity];
            _hasFrameOrTick = new bool[capacity];
            _hasInterval = new bool[capacity];
            _sortedValues = new double[capacity];
        }

        public int Count => _count;

        public long DroppedCount { get; private set; }

        public bool Add(
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds,
            PerformanceMeasurementPhase phase)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || monotonicElapsedNanoseconds < 0)
                return false;
            if (intervalSeconds != -1
                && (intervalSeconds < 0 || double.IsNaN(intervalSeconds) || double.IsInfinity(intervalSeconds)))
                return false;

            int index = _nextIndex;
            _values[index] = value;
            _monotonicElapsedNanoseconds[index] = monotonicElapsedNanoseconds;
            _frameOrTick[index] = frameOrTick;
            _intervalSeconds[index] = intervalSeconds;
            _phases[index] = phase;
            _hasFrameOrTick[index] = frameOrTick >= 0;
            _hasInterval[index] = intervalSeconds >= 0;

            _nextIndex = (_nextIndex + 1) % _values.Length;
            if (_count < _values.Length)
                _count++;
            else
                DroppedCount++;

            return true;
        }

        public PerformanceMetricSnapshot CreateSnapshot()
        {
            if (_count == 0)
                return PerformanceMetricSnapshot.Empty(DroppedCount);

            for (int i = 0; i < _count; i++)
                _sortedValues[i] = _values[GetIndex(i)];

            Array.Sort(_sortedValues, 0, _count);

            double sum = 0;
            double minimum = _sortedValues[0];
            double maximum = _sortedValues[_count - 1];
            for (int i = 0; i < _count; i++)
                sum += _sortedValues[i];

            return new PerformanceMetricSnapshot(
                _count,
                minimum,
                maximum,
                sum / _count,
                Percentile(50),
                Percentile(95),
                _count >= 100 ? Percentile(99) : null,
                DroppedCount);
        }

        public void WriteRawSamples(
            System.IO.StreamWriter writer,
            PerformanceMetricDefinition definition,
            PerformanceSessionContext context,
            DateTime startedUtc)
        {
            for (int i = 0; i < _count; i++)
            {
                int index = GetIndex(i);
                writer.Write('{');
                PerformanceJson.WriteProperty(writer, "schema_version", "benchmark-sample/v1", true);
                PerformanceJson.WriteProperty(writer, "run_id", context.RunId, true);
                PerformanceJson.WriteProperty(writer, "metric_id", definition.MetricId, true);
                PerformanceJson.WriteProperty(writer, "source", PerformanceMetricText.ToText(definition.Side), true);
                PerformanceJson.WriteProperty(writer, "host_id", context.HostId, true);
                PerformanceJson.WriteProperty(writer, "process_id", context.ProcessId, true);
                PerformanceJson.WriteProperty(writer, "phase", PerformanceMetricText.ToText(_phases[index]), true);
                PerformanceJson.WriteProperty(writer, "monotonic_elapsed_ns", _monotonicElapsedNanoseconds[index], true);
                PerformanceJson.WriteProperty(
                    writer,
                    "utc",
                    startedUtc.AddTicks(_monotonicElapsedNanoseconds[index] / 100).ToString("o"),
                    true);
                PerformanceJson.WriteProperty(writer, "scope", definition.Scope, true);
                PerformanceJson.WriteProperty(writer, "unit", PerformanceMetricText.ToText(definition.Unit), true);
                PerformanceJson.WriteProperty(writer, "value", _values[index], true);
                PerformanceJson.WriteNullableProperty(writer, "frame_or_tick", _hasFrameOrTick[index] ? _frameOrTick[index] : (long?)null, true);
                PerformanceJson.WriteNullableProperty(writer, "interval_seconds", _hasInterval[index] ? _intervalSeconds[index] : (double?)null, true);
                PerformanceJson.WriteProperty(writer, "subsystem", definition.Subsystem, true);
                PerformanceJson.WriteRawProperty(writer, "tags", "{}", false);
                writer.WriteLine('}');
            }
        }

        private int GetIndex(int orderedIndex)
        {
            int firstIndex = _count == _values.Length ? _nextIndex : 0;
            return (firstIndex + orderedIndex) % _values.Length;
        }

        private double Percentile(int percentile)
        {
            int rank = (int)Math.Ceiling(percentile / 100d * _count);
            return _sortedValues[Math.Max(0, rank - 1)];
        }
    }

    internal sealed class PerformanceMetricSnapshot
    {
        public PerformanceMetricSnapshot(
            int sampleCount,
            double? minimum,
            double? maximum,
            double? mean,
            double? p50,
            double? p95,
            double? p99,
            long droppedSampleCount)
        {
            SampleCount = sampleCount;
            Minimum = minimum;
            Maximum = maximum;
            Mean = mean;
            P50 = p50;
            P95 = p95;
            P99 = p99;
            DroppedSampleCount = droppedSampleCount;
        }

        public int SampleCount { get; }

        public double? Minimum { get; }

        public double? Maximum { get; }

        public double? Mean { get; }

        public double? P50 { get; }

        public double? P95 { get; }

        public double? P99 { get; }

        public long DroppedSampleCount { get; }

        public static PerformanceMetricSnapshot Empty(long droppedSampleCount)
        {
            return new PerformanceMetricSnapshot(0, null, null, null, null, null, null, droppedSampleCount);
        }
    }
}
