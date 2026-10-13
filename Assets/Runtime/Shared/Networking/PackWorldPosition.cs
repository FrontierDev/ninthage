using Game.Shared.Performance;
using Game.Shared.Utility;
using PurrNet.Modules;
using PurrNet.Packing;
using UnityEngine;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for world positions.</summary>
    public static class PackWorldPosition
    {
        private static readonly PerformanceMarker WRITE_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_WORLD_POSITION_WRITE_DURATION,
            "Network.PackWorldPosition.Write");
        private static readonly PerformanceMarker READ_MARKER = new(
            NetworkingPerformanceMetrics.SERIALIZER_WORLD_POSITION_READ_DURATION,
            "Network.PackWorldPosition.Read");

        [UsedByIL]
        /// <summary>Writes a world position to a bit packer.</summary>
        public static void Write(this BitPacker packer, WorldPosition value)
        {
            using var markerScope = WRITE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBytes : 0;
            Vector2Int chunk = value.Chunk;
            Vector3 local = value.LocalPosition;
            packer.Write(chunk.x);
            packer.Write(chunk.y);
            packer.Write(local.x);
            packer.Write(local.y);
            packer.Write(local.z);

            if (telemetryEnabled)
            {
                NetworkingPerformanceMetrics.RecordSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_WORLD_POSITION_WRITE_BYTES,
                    NetworkingPerformanceMetrics.EstimateWorldPositionPayloadBytes(),
                    packer.positionInBytes - startPosition);
            }
        }

        [UsedByIL]
        /// <summary>Reads a world position from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref WorldPosition value)
        {
            using var markerScope = READ_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int startPosition = telemetryEnabled ? packer.positionInBytes : 0;
            int cx = default;
            int cy = default;
            float lx = default;
            float ly = default;
            float lz = default;
            packer.Read(ref cx);
            packer.Read(ref cy);
            packer.Read(ref lx);
            packer.Read(ref ly);
            packer.Read(ref lz);
            value = WorldPosition.FromChunkLocal(
                new Vector2Int(cx, cy),
                new Vector3(lx, ly, lz)
            );

            if (telemetryEnabled)
                NetworkingPerformanceMetrics.RecordEncodedSerializer(
                    NetworkingPerformanceMetrics.SERIALIZER_WORLD_POSITION_READ_BYTES,
                    packer.positionInBytes - startPosition);
        }
    }
}
