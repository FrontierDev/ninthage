using System;

namespace Game.Shared.Performance
{
    internal sealed class PerformanceSampleBuffer
    {
        private readonly double[] _values;
        private readonly double[] _sortedValues;
        private readonly int _capacity;
        private int _count;
        private int _nextIndex;
        private long _attemptedCount;
        private long _rejectedCount;
        private long _capacityOverflowCount;
        private long _reservoirReplacementCount;
        private long _acceptedCount;
        private ulong _randomState = 0x9E3779B97F4A7C15UL;
        private bool _hasExtrema;
        private double _minimum;
        private double _maximum;

        public PerformanceSampleBuffer(int capacity)
        {
            if (capacity < 1 || capacity > PerformanceTelemetry.MAX_SAMPLES_PER_METRIC)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Metric sample capacity is outside the supported bound.");

            _capacity = capacity;
            _values = new double[capacity];
            _sortedValues = new double[capacity];
        }

        public int Count => _count;

        public long AttemptedCount => _attemptedCount;

        public long DroppedCount => _rejectedCount + _capacityOverflowCount;

        public long RejectedCount => _rejectedCount;

        public long CapacityOverflowCount => _capacityOverflowCount;

        public long ReservoirReplacementCount => _reservoirReplacementCount;

        public double? ValidInputCoverageRatio => _attemptedCount > 0
            ? Math.Max(0L, _attemptedCount - _rejectedCount) / (double)_attemptedCount
            : (double?)null;

        public double? RetentionRatio => _attemptedCount > 0
            ? _count / (double)_attemptedCount
            : (double?)null;

        public bool Add(double value)
        {
            _attemptedCount++;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                _rejectedCount++;
                return false;
            }

            _acceptedCount++;
            if (!_hasExtrema)
            {
                _minimum = value;
                _maximum = value;
                _hasExtrema = true;
            }
            else
            {
                _minimum = Math.Min(_minimum, value);
                _maximum = Math.Max(_maximum, value);
            }

            int index;
            if (_count < _capacity)
            {
                index = _nextIndex;
                _nextIndex = (_nextIndex + 1) % _capacity;
                _count++;
            }
            else
            {
                _capacityOverflowCount++;
                int reservoirIndex = NextReservoirIndex(_acceptedCount);
                if (reservoirIndex >= _capacity)
                    return true;

                index = reservoirIndex;
                _reservoirReplacementCount++;
            }

            _values[index] = value;
            return true;
        }

        public PerformanceMetricSnapshot CreateSnapshot()
        {
            if (_count == 0)
            {
                return PerformanceMetricSnapshot.Empty(
                    _attemptedCount,
                    _rejectedCount,
                    _capacityOverflowCount,
                    _reservoirReplacementCount);
            }

            Array.Copy(_values, _sortedValues, _count);
            Array.Sort(_sortedValues, 0, _count);
            double sum = 0d;
            for (int i = 0; i < _count; i++)
                sum += _sortedValues[i];

            return new PerformanceMetricSnapshot(
                _count,
                _minimum,
                _maximum,
                sum / _count,
                Percentile(50),
                Percentile(95),
                _count >= 100 ? Percentile(99) : null,
                _attemptedCount,
                _rejectedCount,
                _capacityOverflowCount,
                _reservoirReplacementCount,
                ValidInputCoverageRatio,
                RetentionRatio);
        }

        private int NextReservoirIndex(long upperBound)
        {
            _randomState ^= _randomState << 7;
            _randomState ^= _randomState >> 9;
            _randomState ^= _randomState << 8;
            return (int)(_randomState % (ulong)upperBound);
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
            long attemptedSampleCount,
            long rejectedSampleCount,
            long capacityOverflowCount,
            long reservoirReplacementCount,
            double? validInputCoverageRatio,
            double? retentionRatio)
        {
            SampleCount = sampleCount;
            Minimum = minimum;
            Maximum = maximum;
            Mean = mean;
            P50 = p50;
            P95 = p95;
            P99 = p99;
            AttemptedSampleCount = attemptedSampleCount;
            RejectedSampleCount = rejectedSampleCount;
            CapacityOverflowCount = capacityOverflowCount;
            ReservoirReplacementCount = reservoirReplacementCount;
            ValidInputCoverageRatio = validInputCoverageRatio;
            RetentionRatio = retentionRatio;
        }

        public int SampleCount { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public double? Mean { get; }
        public double? P50 { get; }
        public double? P95 { get; }
        public double? P99 { get; }
        public long AttemptedSampleCount { get; }
        public long RejectedSampleCount { get; }
        public long CapacityOverflowCount { get; }
        public long ReservoirReplacementCount { get; }
        public double? ValidInputCoverageRatio { get; }
        public double? RetentionRatio { get; }

        public static PerformanceMetricSnapshot Empty(
            long attemptedSampleCount,
            long rejectedSampleCount,
            long capacityOverflowCount,
            long reservoirReplacementCount)
        {
            double? validCoverage = attemptedSampleCount > 0
                ? Math.Max(0L, attemptedSampleCount - rejectedSampleCount) / (double)attemptedSampleCount
                : (double?)null;
            return new PerformanceMetricSnapshot(
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                attemptedSampleCount,
                rejectedSampleCount,
                capacityOverflowCount,
                reservoirReplacementCount,
                validCoverage,
                attemptedSampleCount > 0 ? 0d : (double?)null);
        }
    }
}
