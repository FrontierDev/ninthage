using Game.Shared.Performance;
using PurrNet.Modules;
using PurrNet.Packing;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for stat snapshots.</summary>
    public static class PackStatSnapshot
    {
        private static readonly PerformanceMarker ENTRY_WRITE_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_STAT_ENTRY_WRITE_DURATION,
            "Network.PackStatSnapshot.EntryWrite");
        private static readonly PerformanceMarker ENTRY_READ_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_STAT_ENTRY_READ_DURATION,
            "Network.PackStatSnapshot.EntryRead");
        private static readonly PerformanceMarker SNAPSHOT_WRITE_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_STAT_SNAPSHOT_WRITE_DURATION,
            "Network.PackStatSnapshot.Write");
        private static readonly PerformanceMarker SNAPSHOT_READ_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_STAT_SNAPSHOT_READ_DURATION,
            "Network.PackStatSnapshot.Read");

        [UsedByIL]
        /// <summary>Writes a stat entry to a bit packer.</summary>
        public static void Write(this BitPacker packer, StatSnapshotEntry value)
        {
            using var markerScope = ENTRY_WRITE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBits : 0;
            packer.Write(value.StatID);
            packer.Write(value.EffectiveMax);
            packer.Write(value.FlatModifier);
            packer.Write(value.PercentModifier);
            packer.Write(value.Current);

            if (telemetryEnabled)
                NetworkingPerformanceMetrics.RecordSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_STAT_ENTRY_WRITE_BYTES,
                    NetworkingPerformanceMetrics.EstimateStatEntryPayloadBytes(value),
                    NetworkingPerformanceMetrics.GetEncodedByteCount(startPosition, packer.positionInBits));
        }

        [UsedByIL]
        /// <summary>Reads a stat entry from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref StatSnapshotEntry value)
        {
            using var markerScope = ENTRY_READ_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBits : 0;
            string statID = default;
            float effectiveMax = default;
            float flatModifier = default;
            float percentModifier = default;
            float current = default;
            packer.Read(ref statID);
            packer.Read(ref effectiveMax);
            packer.Read(ref flatModifier);
            packer.Read(ref percentModifier);
            packer.Read(ref current);
            value = new StatSnapshotEntry
            {
                StatID = statID,
                EffectiveMax = effectiveMax,
                FlatModifier = flatModifier,
                PercentModifier = percentModifier,
                Current = current
            };

            if (telemetryEnabled)
                NetworkingPerformanceMetrics.RecordEncodedSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_STAT_ENTRY_READ_BYTES,
                    NetworkingPerformanceMetrics.GetEncodedByteCount(startPosition, packer.positionInBits));
        }

        [UsedByIL]
        /// <summary>Writes a stat snapshot to a bit packer.</summary>
        public static void Write(this BitPacker packer, StatSnapshot value)
        {
            using var markerScope = SNAPSHOT_WRITE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBits : 0;
            int length = value.Stats?.Length ?? 0;
            packer.Write(length);
            for (int i = 0; i < length; i++)
                packer.Write(value.Stats[i]);

            if (telemetryEnabled)
                NetworkingPerformanceMetrics.RecordSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_STAT_SNAPSHOT_WRITE_BYTES,
                    NetworkingPerformanceMetrics.EstimateStatSnapshotPayloadBytes(value),
                    NetworkingPerformanceMetrics.GetEncodedByteCount(startPosition, packer.positionInBits));
        }

        [UsedByIL]
        /// <summary>Reads a stat snapshot from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref StatSnapshot value)
        {
            using var markerScope = SNAPSHOT_READ_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBits : 0;
            int length = default;
            packer.Read(ref length);
            var stats = new StatSnapshotEntry[length];
            for (int i = 0; i < length; i++)
                packer.Read(ref stats[i]);
            value = new StatSnapshot { Stats = stats };

            if (telemetryEnabled)
                NetworkingPerformanceMetrics.RecordEncodedSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_STAT_SNAPSHOT_READ_BYTES,
                    NetworkingPerformanceMetrics.GetEncodedByteCount(startPosition, packer.positionInBits));
        }
    }
}
