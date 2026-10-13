using System;

namespace Game.Shared.Performance
{
    /// <summary>Describes the stable identity and dimensions of a telemetry metric.</summary>
    public sealed class PerformanceMetricDefinition
    {
        public PerformanceMetricDefinition(
            string metricId,
            PerformanceMetricUnit unit,
            PerformanceMetricSide side,
            string subsystem,
            string scope)
        {
            if (string.IsNullOrWhiteSpace(metricId))
                throw new ArgumentException("A metric ID is required.", nameof(metricId));
            if (string.IsNullOrWhiteSpace(subsystem))
                throw new ArgumentException("A metric subsystem is required.", nameof(subsystem));
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("A metric scope is required.", nameof(scope));
            if (metricId.Length > 128)
                throw new ArgumentException("A metric ID exceeds its bounded length.", nameof(metricId));
            if (subsystem.Length > 128)
                throw new ArgumentException("A metric subsystem exceeds its bounded length.", nameof(subsystem));
            if (scope.Length > 128)
                throw new ArgumentException("A metric scope exceeds its bounded length.", nameof(scope));

            MetricId = metricId;
            Unit = unit;
            Side = side;
            Subsystem = subsystem;
            Scope = scope;
        }

        public string MetricId { get; }

        public PerformanceMetricUnit Unit { get; }

        public PerformanceMetricSide Side { get; }

        public string Subsystem { get; }

        public string Scope { get; }
    }

    /// <summary>Canonical units used by the benchmark evidence contract.</summary>
    public enum PerformanceMetricUnit
    {
        Milliseconds,
        Bytes,
        Count,
        Ticks,
        EventsPerSecond,
        Ratio,
        Metres
    }

    /// <summary>Identifies the process or runtime side that produced a metric.</summary>
    public enum PerformanceMetricSide
    {
        Shared,
        Server,
        Client
    }

    /// <summary>Describes whether a metric has a direct observation for this run.</summary>
    public enum PerformanceMetricObservation
    {
        Measured,
        Estimated,
        Unsupported,
        Unavailable,
        NotApplicable
    }

    /// <summary>Identifies the lifecycle phase associated with a telemetry sample.</summary>
    public enum PerformanceMeasurementPhase
    {
        Preflight,
        Ramp,
        Warmup,
        Measurement,
        Recovery
    }

    /// <summary>Stable IDs for metrics that later instrumentation may produce.</summary>
    public static class PerformanceMetricIds
    {
        public const string SERVER_SIMULATION_TICK_DURATION = "server.simulation.tick_duration";
        public const string SERVER_SIMULATION_TICK_BACKLOG = "server.simulation.tick_backlog";
        public const string SERVER_ACTORS_DORMANT_COUNT = "server.actors.dormant_count";
        public const string SERVER_SPATIAL_CANDIDATE_COUNT = "server.spatial.candidate_count";
        public const string SERVER_NETWORK_PAYLOAD_BYTES = "server.network.payload_bytes";
        public const string CLIENT_RENDERED_FRAME_TIME = "client.rendered.frame_time";
        public const string CLIENT_GPU_FRAME_TIME = "client.gpu.frame_time";
        public const string SHARED_SCENE_OPERATION_DURATION = "shared.scene.operation_duration";
    }

    internal static class PerformanceMetricText
    {
        public static string ToText(PerformanceMetricUnit unit)
        {
            switch (unit)
            {
                case PerformanceMetricUnit.Milliseconds:
                    return "ms";
                case PerformanceMetricUnit.Bytes:
                    return "bytes";
                case PerformanceMetricUnit.Count:
                    return "count";
                case PerformanceMetricUnit.Ticks:
                    return "ticks";
                case PerformanceMetricUnit.EventsPerSecond:
                    return "events/s";
                case PerformanceMetricUnit.Ratio:
                    return "ratio";
                case PerformanceMetricUnit.Metres:
                    return "m";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown metric unit.");
            }
        }

        public static string ToText(PerformanceMetricSide side)
        {
            switch (side)
            {
                case PerformanceMetricSide.Shared:
                    return "shared";
                case PerformanceMetricSide.Server:
                    return "server";
                case PerformanceMetricSide.Client:
                    return "client";
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown metric side.");
            }
        }

        public static string ToText(PerformanceMetricObservation observation)
        {
            switch (observation)
            {
                case PerformanceMetricObservation.Measured:
                    return "measured";
                case PerformanceMetricObservation.Estimated:
                    return "estimated";
                case PerformanceMetricObservation.Unsupported:
                    return "unsupported";
                case PerformanceMetricObservation.Unavailable:
                    return "unavailable";
                case PerformanceMetricObservation.NotApplicable:
                    return "not_applicable";
                default:
                    throw new ArgumentOutOfRangeException(nameof(observation), observation, "Unknown metric observation.");
            }
        }

        public static string ToText(PerformanceMeasurementPhase phase)
        {
            switch (phase)
            {
                case PerformanceMeasurementPhase.Preflight:
                    return "preflight";
                case PerformanceMeasurementPhase.Ramp:
                    return "ramp";
                case PerformanceMeasurementPhase.Warmup:
                    return "warmup";
                case PerformanceMeasurementPhase.Measurement:
                    return "measurement";
                case PerformanceMeasurementPhase.Recovery:
                    return "recovery";
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown measurement phase.");
            }
        }
    }
}
