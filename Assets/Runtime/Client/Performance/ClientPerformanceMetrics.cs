using System.Diagnostics;
using Game.Shared.Performance;

namespace Game.Client.Performance
{
    /// <summary>Bounded client presentation metrics shared by the existing client managers.</summary>
    internal static class ClientPerformanceMetrics
    {
        private const string STREAMED_SCENE_UNLOAD_UNAVAILABLE_REASON =
            "PurrNet does not expose streamed scene request timestamps; only the unload callback duration is observable.";
        private const string VISIBLE_ACTOR_UNAVAILABLE_REASON =
            "The current client actor endpoint does not expose a matching despawn lifecycle for a complete visible-actor count.";

        internal static readonly PerformanceMetricDefinition RENDERED_FRAME_TIME = Metric(
            PerformanceMetricIds.CLIENT_RENDERED_FRAME_TIME,
            PerformanceMetricUnit.Milliseconds,
            "frame",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition FRAME_INTERVAL = Metric(
            PerformanceMetricIds.CLIENT_FRAME_INTERVAL,
            PerformanceMetricUnit.Milliseconds,
            "frame",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition MAIN_THREAD_FRAME_TIME = Metric(
            PerformanceMetricIds.CLIENT_MAIN_THREAD_FRAME_TIME,
            PerformanceMetricUnit.Milliseconds,
            "frame",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition RENDER_THREAD_FRAME_TIME = Metric(
            PerformanceMetricIds.CLIENT_RENDER_THREAD_FRAME_TIME,
            PerformanceMetricUnit.Milliseconds,
            "frame",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition GPU_FRAME_TIME = Metric(
            PerformanceMetricIds.CLIENT_GPU_FRAME_TIME,
            PerformanceMetricUnit.Milliseconds,
            "frame",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition SCENE_LOAD_DURATION = Metric(
            PerformanceMetricIds.CLIENT_SCENE_LOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition SCENE_ACTIVATION_DURATION = Metric(
            PerformanceMetricIds.CLIENT_SCENE_ACTIVATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "scene",
            "scene_operation");
        // PurrNet does not expose streamed scene request timestamps; this remains unavailable for streamed unloads.
        internal static readonly PerformanceMetricDefinition SCENE_UNLOAD_DURATION = Metric(
            PerformanceMetricIds.CLIENT_SCENE_UNLOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition SCENE_UNLOAD_CALLBACK_DURATION = Metric(
            PerformanceMetricIds.CLIENT_SCENE_UNLOAD_CALLBACK_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition SCENE_LOAD_COUNT = Metric(
            PerformanceMetricIds.CLIENT_SCENE_LOAD_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition SCENE_ACTIVATION_COUNT = Metric(
            PerformanceMetricIds.CLIENT_SCENE_ACTIVATION_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition SCENE_UNLOAD_COUNT = Metric(
            PerformanceMetricIds.CLIENT_SCENE_UNLOAD_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition TRACKED_SCENE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_TRACKED_SCENE_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "client_process");
        internal static readonly PerformanceMetricDefinition STREAMED_SCENE_LOAD_COUNT = Metric(
            PerformanceMetricIds.CLIENT_STREAMED_SCENE_LOAD_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition STREAMED_SCENE_UNLOAD_COUNT = Metric(
            PerformanceMetricIds.CLIENT_STREAMED_SCENE_UNLOAD_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition PENDING_SCENE_OPERATION_COUNT = Metric(
            PerformanceMetricIds.CLIENT_PENDING_SCENE_OPERATION_COUNT,
            PerformanceMetricUnit.Count,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition ORIGIN_SHIFT_DURATION = Metric(
            PerformanceMetricIds.CLIENT_ORIGIN_SHIFT_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "floating_origin",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition ORIGIN_SHIFT_COUNT = Metric(
            PerformanceMetricIds.CLIENT_ORIGIN_SHIFT_COUNT,
            PerformanceMetricUnit.Count,
            "floating_origin",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition ORIGIN_SHIFT_ROOT_COUNT = Metric(
            PerformanceMetricIds.CLIENT_ORIGIN_SHIFT_ROOT_COUNT,
            PerformanceMetricUnit.Count,
            "floating_origin",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition FRAME_SPIKE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_FRAME_SPIKE_COUNT,
            PerformanceMetricUnit.Count,
            "frame",
            "rendered_frame");
        // Actor despawn is not exposed by the current client endpoint, so the client does not emit this metric.
        internal static readonly PerformanceMetricDefinition VISIBLE_ACTOR_COUNT = Metric(
            PerformanceMetricIds.CLIENT_VISIBLE_ACTOR_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition NAMEPLATE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_NAMEPLATE_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition NAMEPLATE_SPAWN_COUNT = Metric(
            PerformanceMetricIds.CLIENT_NAMEPLATE_SPAWN_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition NAMEPLATE_RELEASE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_NAMEPLATE_RELEASE_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition NAMEPLATE_UPDATE_DURATION = Metric(
            PerformanceMetricIds.CLIENT_NAMEPLATE_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition VFX_ACTIVE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_VFX_ACTIVE_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition VFX_SPAWN_COUNT = Metric(
            PerformanceMetricIds.CLIENT_VFX_SPAWN_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition VFX_RELEASE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_VFX_RELEASE_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition UI_TRANSIENT_COUNT = Metric(
            PerformanceMetricIds.CLIENT_UI_TRANSIENT_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition UI_TRANSIENT_SPAWN_COUNT = Metric(
            PerformanceMetricIds.CLIENT_UI_TRANSIENT_SPAWN_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition UI_TRANSIENT_RELEASE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_UI_TRANSIENT_RELEASE_COUNT,
            PerformanceMetricUnit.Count,
            "presentation",
            "presentation_event");
        internal static readonly PerformanceMetricDefinition DRAW_CALL_COUNT = Metric(
            PerformanceMetricIds.CLIENT_DRAW_CALL_COUNT,
            PerformanceMetricUnit.Count,
            "graphics",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition BATCH_COUNT = Metric(
            PerformanceMetricIds.CLIENT_BATCH_COUNT,
            PerformanceMetricUnit.Count,
            "graphics",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition TRIANGLE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_TRIANGLE_COUNT,
            PerformanceMetricUnit.Count,
            "graphics",
            "rendered_frame");
        internal static readonly PerformanceMetricDefinition TOTAL_ALLOCATED_MEMORY = Metric(
            PerformanceMetricIds.CLIENT_MEMORY_TOTAL_ALLOCATED_BYTES,
            PerformanceMetricUnit.Bytes,
            "memory",
            "client_frame");
        internal static readonly PerformanceMetricDefinition MONO_USED_MEMORY = Metric(
            PerformanceMetricIds.CLIENT_MEMORY_MONO_USED_BYTES,
            PerformanceMetricUnit.Bytes,
            "memory",
            "client_frame");
        internal static readonly PerformanceMetricDefinition MONO_HEAP_MEMORY = Metric(
            PerformanceMetricIds.CLIENT_MEMORY_MONO_HEAP_BYTES,
            PerformanceMetricUnit.Bytes,
            "memory",
            "client_frame");
        internal static readonly PerformanceMetricDefinition GC_ALLOCATED_MEMORY = Metric(
            PerformanceMetricIds.CLIENT_MEMORY_GC_ALLOCATED_BYTES,
            PerformanceMetricUnit.Bytes,
            "memory",
            "client_frame");
        internal static readonly PerformanceMetricDefinition ADDRESSABLE_LOAD_DURATION = Metric(
            PerformanceMetricIds.CLIENT_ADDRESSABLE_LOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "addressables",
            "addressable_operation");
        internal static readonly PerformanceMetricDefinition ADDRESSABLE_RELEASE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_ADDRESSABLE_RELEASE_COUNT,
            PerformanceMetricUnit.Count,
            "addressables",
            "addressable_lifetime");
        internal static readonly PerformanceMetricDefinition ADDRESSABLE_PENDING_COUNT = Metric(
            PerformanceMetricIds.CLIENT_ADDRESSABLE_PENDING_COUNT,
            PerformanceMetricUnit.Count,
            "addressables",
            "client_frame");
        internal static readonly PerformanceMetricDefinition AUDIO_LIVE_ADDRESSABLE_HANDLE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_AUDIO_LIVE_ADDRESSABLE_HANDLE_COUNT,
            PerformanceMetricUnit.Count,
            "addressables",
            "client_process");
        internal static readonly PerformanceMetricDefinition CAMERA_LIVE_ADDRESSABLE_HANDLE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_CAMERA_LIVE_ADDRESSABLE_HANDLE_COUNT,
            PerformanceMetricUnit.Count,
            "addressables",
            "client_process");
        internal static readonly PerformanceMetricDefinition VFX_LIVE_ADDRESSABLE_HANDLE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_VFX_LIVE_ADDRESSABLE_HANDLE_COUNT,
            PerformanceMetricUnit.Count,
            "addressables",
            "client_process");
        internal static readonly PerformanceMetricDefinition WORLD_PRESENTATION_CLEANUP_DURATION = Metric(
            PerformanceMetricIds.CLIENT_WORLD_PRESENTATION_CLEANUP_DURATION,
            PerformanceMetricUnit.Milliseconds,
            "scene",
            "scene_operation");
        internal static readonly PerformanceMetricDefinition AUDIO_CACHE_COUNT = Metric(
            PerformanceMetricIds.CLIENT_AUDIO_CACHE_COUNT,
            PerformanceMetricUnit.Count,
            "audio",
            "client_process");

        internal static void RegisterMetrics()
        {
            RegisterMeasured(RENDERED_FRAME_TIME);
            RegisterMeasured(FRAME_INTERVAL);
            RegisterMeasured(MAIN_THREAD_FRAME_TIME);
            RegisterMeasured(RENDER_THREAD_FRAME_TIME);
            RegisterMeasured(GPU_FRAME_TIME);

            RegisterMeasured(SCENE_LOAD_DURATION);
            RegisterMeasured(SCENE_ACTIVATION_DURATION);
            RegisterUnavailable(SCENE_UNLOAD_DURATION, STREAMED_SCENE_UNLOAD_UNAVAILABLE_REASON);
            RegisterMeasured(SCENE_UNLOAD_CALLBACK_DURATION);
            RegisterMeasured(SCENE_LOAD_COUNT);
            RegisterMeasured(SCENE_ACTIVATION_COUNT);
            RegisterMeasured(SCENE_UNLOAD_COUNT);
            RegisterMeasured(TRACKED_SCENE_COUNT);
            RegisterMeasured(STREAMED_SCENE_LOAD_COUNT);
            RegisterMeasured(STREAMED_SCENE_UNLOAD_COUNT);
            RegisterMeasured(PENDING_SCENE_OPERATION_COUNT);

            RegisterMeasured(ORIGIN_SHIFT_DURATION);
            RegisterMeasured(ORIGIN_SHIFT_COUNT);
            RegisterMeasured(ORIGIN_SHIFT_ROOT_COUNT);
            RegisterMeasured(FRAME_SPIKE_COUNT);

            RegisterUnavailable(VISIBLE_ACTOR_COUNT, VISIBLE_ACTOR_UNAVAILABLE_REASON);
            RegisterMeasured(NAMEPLATE_COUNT);
            RegisterMeasured(NAMEPLATE_SPAWN_COUNT);
            RegisterMeasured(NAMEPLATE_RELEASE_COUNT);
            RegisterMeasured(NAMEPLATE_UPDATE_DURATION);
            RegisterMeasured(VFX_ACTIVE_COUNT);
            RegisterMeasured(VFX_SPAWN_COUNT);
            RegisterMeasured(VFX_RELEASE_COUNT);
            RegisterMeasured(UI_TRANSIENT_COUNT);
            RegisterMeasured(UI_TRANSIENT_SPAWN_COUNT);
            RegisterMeasured(UI_TRANSIENT_RELEASE_COUNT);

            RegisterMeasured(DRAW_CALL_COUNT);
            RegisterMeasured(BATCH_COUNT);
            RegisterMeasured(TRIANGLE_COUNT);
            RegisterMeasured(TOTAL_ALLOCATED_MEMORY);
            RegisterMeasured(MONO_USED_MEMORY);
            RegisterMeasured(MONO_HEAP_MEMORY);
            RegisterMeasured(GC_ALLOCATED_MEMORY);
            RegisterMeasured(ADDRESSABLE_LOAD_DURATION);
            RegisterMeasured(ADDRESSABLE_RELEASE_COUNT);
            RegisterMeasured(ADDRESSABLE_PENDING_COUNT);
            RegisterMeasured(AUDIO_LIVE_ADDRESSABLE_HANDLE_COUNT);
            RegisterMeasured(CAMERA_LIVE_ADDRESSABLE_HANDLE_COUNT);
            RegisterMeasured(VFX_LIVE_ADDRESSABLE_HANDLE_COUNT);
            RegisterMeasured(WORLD_PRESENTATION_CLEANUP_DURATION);
            RegisterMeasured(AUDIO_CACHE_COUNT);
        }

        internal static bool TryRecordDuration(PerformanceMetricDefinition metric, long startedTimestamp)
        {
            if (!PerformanceTelemetry.IsEnabled || startedTimestamp <= 0)
                return false;

            long elapsedTimestamp = Stopwatch.GetTimestamp() - startedTimestamp;
            if (elapsedTimestamp < 0)
                return false;

            return PerformanceTelemetry.TryRecord(
                metric,
                elapsedTimestamp * 1000d / Stopwatch.Frequency);
        }

        private static PerformanceMetricDefinition Metric(
            string metricID,
            PerformanceMetricUnit unit,
            string subsystem,
            string scope = "client_frame")
        {
            return new PerformanceMetricDefinition(
                metricID,
                unit,
                PerformanceMetricSide.Client,
                subsystem,
                scope);
        }

        private static void RegisterMeasured(PerformanceMetricDefinition definition)
        {
            PerformanceTelemetry.RegisterMetric(definition, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private static void RegisterUnavailable(PerformanceMetricDefinition definition, string reason)
        {
            PerformanceTelemetry.RegisterMetric(
                definition,
                0,
                PerformanceMetricObservation.Unavailable,
                reason);
        }
    }
}
