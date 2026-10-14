using System;

namespace Game.Shared.Performance
{
    internal sealed class PerformanceSampleBuffer
    {
        // Keep later recovery/context samples from consuming the measurement retention budget.
        private const int NON_MEASUREMENT_SAMPLE_RESERVE = 128;
        // Export a bounded audit trace; summary statistics use the larger phase-aware retention set.
        private const int RAW_TRACE_SAMPLE_BUDGET = 256;
        private const int RAW_TRACE_CONTEXT_BUDGET = 64;

        private readonly double[] _values;
        private readonly long[] _monotonicElapsedNanoseconds;
        private readonly long[] _frameOrTick;
        private readonly double[] _intervalSeconds;
        private readonly PerformanceMeasurementPhase[] _phases;
        private readonly bool[] _hasFrameOrTick;
        private readonly bool[] _hasInterval;
        private readonly double[] _sortedValues;
        private readonly int _measurementCapacity;
        private readonly int _contextCapacity;
        private int _measurementCount;
        private int _measurementNextIndex;
        private int _contextCount;
        private int _contextNextIndex;
        private long _attemptedCount;
        private long _measurementAttemptedCount;
        private long _measurementDroppedCount;
        private long _measurementRejectedCount;
        private long _measurementCapacityOverflowCount;
        private long _measurementReservoirReplacementCount;
        private long _measurementAcceptedCount;
        private ulong _measurementRandomState = 0x9E3779B97F4A7C15UL;
        private bool _hasMeasurementExtrema;
        private double _measurementMinimum;
        private double _measurementMaximum;

        public PerformanceSampleBuffer(int capacity)
        {
            if (capacity < 1 || capacity > PerformanceTelemetry.MAX_SAMPLES_PER_METRIC)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Metric sample capacity is outside the supported bound.");

            _measurementCapacity = capacity == 1
                ? 1
                : capacity - Math.Min(NON_MEASUREMENT_SAMPLE_RESERVE, capacity - 1);
            _contextCapacity = capacity - _measurementCapacity;
            _values = new double[capacity];
            _monotonicElapsedNanoseconds = new long[capacity];
            _frameOrTick = new long[capacity];
            _intervalSeconds = new double[capacity];
            _phases = new PerformanceMeasurementPhase[capacity];
            _hasFrameOrTick = new bool[capacity];
            _hasInterval = new bool[capacity];
            _sortedValues = new double[_measurementCapacity];
        }

        public int Count => _measurementCount + _contextCount;

        public long AttemptedCount => _attemptedCount;

        public long DroppedCount { get; private set; }

        public long MeasurementAttemptedCount => _measurementAttemptedCount;

        public int MeasurementRetainedCount => _measurementCount;

        public long MeasurementDroppedCount => _measurementDroppedCount;

        public long MeasurementRejectedCount => _measurementRejectedCount;

        public long MeasurementCapacityOverflowCount => _measurementCapacityOverflowCount;

        public long MeasurementReservoirReplacementCount => _measurementReservoirReplacementCount;

        public int RawTraceSampleCount => GetRawTraceSampleCount();

        public bool Add(
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds,
            PerformanceMeasurementPhase phase)
        {
            _attemptedCount++;
            if (phase == PerformanceMeasurementPhase.Measurement)
                _measurementAttemptedCount++;

            if (double.IsNaN(value) || double.IsInfinity(value) || monotonicElapsedNanoseconds < 0)
            {
                DropSample(phase);
                return false;
            }
            if (intervalSeconds != -1
                && (intervalSeconds < 0 || double.IsNaN(intervalSeconds) || double.IsInfinity(intervalSeconds)))
            {
                DropSample(phase);
                return false;
            }

            if (phase == PerformanceMeasurementPhase.Measurement)
                return AddMeasurementSample(value, monotonicElapsedNanoseconds, frameOrTick, intervalSeconds, phase);

            AddContextSample(value, monotonicElapsedNanoseconds, frameOrTick, intervalSeconds, phase);
            return true;
        }

        public PerformanceMetricSnapshot CreateSnapshot()
        {
            if (_measurementCount == 0)
                return PerformanceMetricSnapshot.Empty(
                    _measurementAttemptedCount,
                    _measurementDroppedCount,
                    GetRawTraceSampleCount(),
                    _measurementRejectedCount,
                    _measurementCapacityOverflowCount,
                    _measurementReservoirReplacementCount);

            for (int i = 0; i < _measurementCount; i++)
                _sortedValues[i] = _values[GetMeasurementIndex(i)];

            Array.Sort(_sortedValues, 0, _measurementCount);

            double sum = 0;
            for (int i = 0; i < _measurementCount; i++)
                sum += _sortedValues[i];

            return new PerformanceMetricSnapshot(
                _measurementCount,
                _measurementMinimum,
                _measurementMaximum,
                sum / _measurementCount,
                Percentile(50),
                Percentile(95),
                _measurementCount >= 100 ? Percentile(99) : null,
                _measurementAttemptedCount,
                _measurementDroppedCount,
                GetRawTraceSampleCount(),
                _measurementRejectedCount,
                _measurementCapacityOverflowCount,
                _measurementReservoirReplacementCount);
        }

        public void WriteRawSamples(
            System.IO.StreamWriter writer,
            PerformanceMetricDefinition definition,
            PerformanceSessionContext context,
            DateTime startedUtc)
        {
            int measurementBudget = Math.Min(_measurementCount, RAW_TRACE_SAMPLE_BUDGET);
            int remainingBudget = RAW_TRACE_SAMPLE_BUDGET - measurementBudget;
            int contextBudget = _measurementCount == 0
                ? Math.Min(_contextCount, RAW_TRACE_SAMPLE_BUDGET)
                : Math.Min(_contextCount, Math.Min(RAW_TRACE_CONTEXT_BUDGET, remainingBudget));

            WriteRawSegment(
                writer,
                definition,
                context,
                startedUtc,
                false,
                contextBudget);
            WriteRawSegment(
                writer,
                definition,
                context,
                startedUtc,
                true,
                measurementBudget);
        }

        private bool AddMeasurementSample(
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds,
            PerformanceMeasurementPhase phase)
        {
            _measurementAcceptedCount++;
            if (!_hasMeasurementExtrema)
            {
                _measurementMinimum = value;
                _measurementMaximum = value;
                _hasMeasurementExtrema = true;
            }
            else
            {
                _measurementMinimum = Math.Min(_measurementMinimum, value);
                _measurementMaximum = Math.Max(_measurementMaximum, value);
            }

            int index;
            if (_measurementCount < _measurementCapacity)
            {
                index = _measurementNextIndex;
                _measurementNextIndex = (_measurementNextIndex + 1) % _measurementCapacity;
                _measurementCount++;
            }
            else
            {
                // Reservoir replacement keeps the retained measurement population unbiased instead of
                // retaining only an arbitrary recent window when the measurement budget is exceeded.
                _measurementDroppedCount++;
                _measurementCapacityOverflowCount++;
                DroppedCount++;
                int reservoirIndex = NextReservoirIndex(_measurementAcceptedCount);
                if (reservoirIndex >= _measurementCapacity)
                    return true;

                index = reservoirIndex;
                _measurementReservoirReplacementCount++;
            }

            StoreSample(index, value, monotonicElapsedNanoseconds, frameOrTick, intervalSeconds, phase);
            return true;
        }

        private void AddContextSample(
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds,
            PerformanceMeasurementPhase phase)
        {
            if (_contextCapacity == 0)
            {
                DroppedCount++;
                return;
            }

            if (_contextCount == _contextCapacity)
                DroppedCount++;
            else
                _contextCount++;

            int index = _measurementCapacity + _contextNextIndex;
            _contextNextIndex = (_contextNextIndex + 1) % _contextCapacity;
            StoreSample(index, value, monotonicElapsedNanoseconds, frameOrTick, intervalSeconds, phase);
        }

        private void DropSample(PerformanceMeasurementPhase phase)
        {
            DroppedCount++;
            if (phase == PerformanceMeasurementPhase.Measurement)
            {
                _measurementDroppedCount++;
                _measurementRejectedCount++;
            }
        }

        private void StoreSample(
            int index,
            double value,
            long monotonicElapsedNanoseconds,
            long frameOrTick,
            double intervalSeconds,
            PerformanceMeasurementPhase phase)
        {
            _values[index] = value;
            _monotonicElapsedNanoseconds[index] = monotonicElapsedNanoseconds;
            _frameOrTick[index] = frameOrTick;
            _intervalSeconds[index] = intervalSeconds;
            _phases[index] = phase;
            _hasFrameOrTick[index] = frameOrTick >= 0;
            _hasInterval[index] = intervalSeconds >= 0;
        }

        private int GetMeasurementIndex(int orderedIndex)
        {
            int firstIndex = _measurementCount == _measurementCapacity ? _measurementNextIndex : 0;
            return (firstIndex + orderedIndex) % _measurementCapacity;
        }

        private int GetContextIndex(int orderedIndex)
        {
            int firstIndex = _contextCount == _contextCapacity ? _contextNextIndex : 0;
            return _measurementCapacity + (firstIndex + orderedIndex) % _contextCapacity;
        }

        private int GetRawTraceSampleCount()
        {
            int measurementCount = Math.Min(_measurementCount, RAW_TRACE_SAMPLE_BUDGET);
            if (_measurementCount == 0)
                return Math.Min(_contextCount, RAW_TRACE_SAMPLE_BUDGET);

            int remainingBudget = RAW_TRACE_SAMPLE_BUDGET - measurementCount;
            return measurementCount + Math.Min(_contextCount, Math.Min(RAW_TRACE_CONTEXT_BUDGET, remainingBudget));
        }

        private void WriteRawSegment(
            System.IO.StreamWriter writer,
            PerformanceMetricDefinition definition,
            PerformanceSessionContext context,
            DateTime startedUtc,
            bool measurement,
            int budget)
        {
            int count = measurement ? _measurementCount : _contextCount;
            for (int outputIndex = 0; outputIndex < budget; outputIndex++)
            {
                int orderedIndex = SelectOrderedIndex(outputIndex, count, budget);
                int index = measurement ? GetMeasurementIndex(orderedIndex) : GetContextIndex(orderedIndex);
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

        private int SelectOrderedIndex(int outputIndex, int count, int budget)
        {
            if (count <= budget || budget <= 1)
                return outputIndex;

            return (int)((long)outputIndex * (count - 1) / (budget - 1));
        }

        private int NextReservoirIndex(long upperBound)
        {
            _measurementRandomState ^= _measurementRandomState << 7;
            _measurementRandomState ^= _measurementRandomState >> 9;
            _measurementRandomState ^= _measurementRandomState << 8;
            return (int)(_measurementRandomState % (ulong)upperBound);
        }

        private double Percentile(int percentile)
        {
            int rank = (int)Math.Ceiling(percentile / 100d * _measurementCount);
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
            long attemptedSampleCount,
            long droppedSampleCount,
            int rawTraceSampleCount,
            long rejectedSampleCount = 0,
            long capacityOverflowCount = 0,
            long reservoirReplacementCount = 0)
        {
            SampleCount = sampleCount;
            Minimum = minimum;
            Maximum = maximum;
            Mean = mean;
            P50 = p50;
            P95 = p95;
            P99 = p99;
            AttemptedSampleCount = attemptedSampleCount;
            DroppedSampleCount = droppedSampleCount;
            RawTraceSampleCount = rawTraceSampleCount;
            RejectedSampleCount = rejectedSampleCount;
            CapacityOverflowCount = capacityOverflowCount;
            ReservoirReplacementCount = reservoirReplacementCount;
        }

        public int SampleCount { get; }

        public double? Minimum { get; }

        public double? Maximum { get; }

        public double? Mean { get; }

        public double? P50 { get; }

        public double? P95 { get; }

        public double? P99 { get; }

        public long AttemptedSampleCount { get; }

        public long DroppedSampleCount { get; }

        public long RejectedSampleCount { get; }

        public long CapacityOverflowCount { get; }

        public long ReservoirReplacementCount { get; }

        public int RawTraceSampleCount { get; }

        public double? ValidInputCoverageRatio => AttemptedSampleCount > 0
            ? Math.Max(0L, AttemptedSampleCount - RejectedSampleCount) / (double)AttemptedSampleCount
            : (double?)null;

        public double? RetentionRatio => AttemptedSampleCount > 0
            ? SampleCount / (double)AttemptedSampleCount
            : (double?)null;

        public double? CoverageRatio => RetentionRatio;

        public bool IsReservoirSampled => CapacityOverflowCount > 0;

        public static PerformanceMetricSnapshot Empty(
            long attemptedSampleCount,
            long droppedSampleCount,
            int rawTraceSampleCount,
            long rejectedSampleCount = 0,
            long capacityOverflowCount = 0,
            long reservoirReplacementCount = 0)
        {
            return new PerformanceMetricSnapshot(
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                attemptedSampleCount,
                droppedSampleCount,
                rawTraceSampleCount,
                rejectedSampleCount,
                capacityOverflowCount,
                reservoirReplacementCount);
        }
    }
}
