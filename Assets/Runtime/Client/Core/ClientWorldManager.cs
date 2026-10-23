using Game.Client.Performance;
using Game.Client.UI;
using Game.Shared.Performance;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientWorldManager</c> implementation.
    /// </summary>
    public sealed class ClientWorldManager : MonoBehaviour
    {
        private static readonly PerformanceMarker SCENE_ACTIVATION_MARKER = new(
            ClientPerformanceMetrics.SCENE_ACTIVATION_DURATION,
            "Client.WorldManager.SceneActivation");
        private static readonly PerformanceMarker SCENE_UNLOAD_MARKER = new(
            ClientPerformanceMetrics.SCENE_UNLOAD_CALLBACK_DURATION,
            "Client.WorldManager.SceneUnload");
        private static ClientWorldManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientWorldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        private List<Scene> _loadedScenes = new();
        private long _globalWorldLoadTimestamp;
        private FrameTiming[] _frameTimings;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchCountRecorder;
        private ProfilerRecorder _triangleCountRecorder;
        private ProfilerRecorder _gcAllocatedRecorder;
        private bool _recordersStarted;

        private static readonly float FRAME_SPIKE_THRESHOLD_MS = 33.333334f;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            Game.Shared.Networking.AccountEndpoint.onEnteringWorld += OnEnteringWorldRequested;
            PerformanceTelemetry.SessionEnded += OnTelemetrySessionEnded;
            _initialized = true;
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.AccountEndpoint.onEnteringWorld -= OnEnteringWorldRequested;
            PerformanceTelemetry.SessionEnded -= OnTelemetrySessionEnded;

            if (NetworkManager.main != null)
            {
                NetworkManager.main.sceneModule.onSceneLoaded -= OnSceneLoaded;
                NetworkManager.main.sceneModule.onSceneUnloaded -= OnSceneUnloaded;
            }

            StopProfilers();

            _instance = null;
            _initialized = false;
        }

        private void Update()
        {
            if (!PerformanceTelemetry.IsEnabled)
            {
                StopProfilers();
                return;
            }

            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.TRACKED_SCENE_COUNT, _loadedScenes.Count);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.TOTAL_ALLOCATED_MEMORY,
                UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.MONO_USED_MEMORY,
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.MONO_HEAP_MEMORY,
                UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong());

            if (!_recordersStarted)
            {
                _frameTimings = new FrameTiming[1];
                _gcAllocatedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                    _batchCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
                    _triangleCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                }
                _recordersStarted = true;
            }

            if (_gcAllocatedRecorder.Valid)
                PerformanceTelemetry.TryRecord(
                    ClientPerformanceMetrics.GC_ALLOCATED_MEMORY,
                    _gcAllocatedRecorder.LastValue);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            float frameTimeMilliseconds = Time.unscaledDeltaTime * 1000f;
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.FRAME_INTERVAL, frameTimeMilliseconds);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.FRAME_SPIKE_COUNT,
                frameTimeMilliseconds >= FRAME_SPIKE_THRESHOLD_MS ? 1 : 0);

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                FrameTiming timing = _frameTimings[0];
                if (timing.cpuFrameTime > 0d)
                    PerformanceTelemetry.TryRecord(
                        ClientPerformanceMetrics.RENDERED_FRAME_TIME,
                        timing.cpuFrameTime);
                if (timing.cpuMainThreadFrameTime > 0d)
                    PerformanceTelemetry.TryRecord(
                        ClientPerformanceMetrics.MAIN_THREAD_FRAME_TIME,
                        timing.cpuMainThreadFrameTime);
                if (timing.cpuRenderThreadFrameTime > 0d)
                    PerformanceTelemetry.TryRecord(
                        ClientPerformanceMetrics.RENDER_THREAD_FRAME_TIME,
                        timing.cpuRenderThreadFrameTime);
                if (timing.gpuFrameTime > 0d)
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.GPU_FRAME_TIME, timing.gpuFrameTime);
            }
            if (_drawCallsRecorder.Valid)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.DRAW_CALL_COUNT, _drawCallsRecorder.LastValue);
            if (_batchCountRecorder.Valid)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.BATCH_COUNT, _batchCountRecorder.LastValue);
            if (_triangleCountRecorder.Valid)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.TRIANGLE_COUNT, _triangleCountRecorder.LastValue);
        }

        private void OnTelemetrySessionEnded()
        {
            StopProfilers();
        }

        private void StopProfilers()
        {
            if (_drawCallsRecorder.Valid)
                _drawCallsRecorder.Dispose();
            if (_batchCountRecorder.Valid)
                _batchCountRecorder.Dispose();
            if (_triangleCountRecorder.Valid)
                _triangleCountRecorder.Dispose();
            if (_gcAllocatedRecorder.Valid)
                _gcAllocatedRecorder.Dispose();

            _drawCallsRecorder = default;
            _batchCountRecorder = default;
            _triangleCountRecorder = default;
            _gcAllocatedRecorder = default;
            _frameTimings = null;
            _globalWorldLoadTimestamp = 0;
            _recordersStarted = false;
        }

        private void OnEnteringWorldRequested(Game.Shared.Persistence.CharacterData characterData, string sceneName)
        {
            StartCoroutine(OnEnteringWorld(sceneName));
        }

        /// <summary>
        /// Handles the <c>OnSceneLoaded</c> callback.
        /// </summary>
        public void OnSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!NetworkManager.main.sceneModule.TryGetSceneState(sceneID, out var state)) return;
            if (state.scene.name.StartsWith("World"))
            {
                if (_loadedScenes.Contains(state.scene)) return;

                _loadedScenes.Add(state.scene);
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.SCENE_LOAD_COUNT, 1);
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.STREAMED_SCENE_LOAD_COUNT, 1);
                }

                // Offset the new scene to match the current floating origin.
                using var markerScope = SCENE_ACTIVATION_MARKER.Begin();
                ClientPositionManager.Instance.ApplyOriginToScene(state.scene);
                RecordPendingSceneOperations();
            }
        }

        private void OnSceneUnloaded(SceneID sceneID, bool asServer)
        {
            // PurrNet owns the unload request, so only callback cleanup duration is observable here.
            using var markerScope = SCENE_UNLOAD_MARKER.Begin();
            _loadedScenes.RemoveAll(s => !s.isLoaded);
            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.SCENE_UNLOAD_COUNT, 1);
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.STREAMED_SCENE_UNLOAD_COUNT, 1);
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.TRACKED_SCENE_COUNT, _loadedScenes.Count);
            }
        }

        private IEnumerator OnEnteringWorld(string sceneName)
        {
            LoadingScreenController.Instance.Show(2);

            var spawnChunk = ParseChunkCoordinate(sceneName);
            ClientPositionManager.Instance.SetOriginChunk(spawnChunk);

            // Subscribe BEFORE loading global_world to avoid missing scene callbacks
            NetworkManager.main.sceneModule.onSceneLoaded += OnSceneLoaded;
            NetworkManager.main.sceneModule.onSceneUnloaded += OnSceneUnloaded;

            if (PerformanceTelemetry.IsEnabled)
            {
                _globalWorldLoadTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
                RecordPendingSceneOperations();
            }

            var loadOp = SceneManager.LoadSceneAsync("global_world", LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadOp.isDone);
            if (_globalWorldLoadTimestamp > 0)
            {
                ClientPerformanceMetrics.TryRecordDuration(
                    ClientPerformanceMetrics.SCENE_LOAD_DURATION,
                    _globalWorldLoadTimestamp);
                _globalWorldLoadTimestamp = 0;
                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.SCENE_LOAD_COUNT, 1);
            }
            using (SCENE_ACTIVATION_MARKER.Begin())
                SceneManager.SetActiveScene(SceneManager.GetSceneByName("global_world"));
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.SCENE_ACTIVATION_COUNT, 1);
            RecordPendingSceneOperations();

            yield return new WaitUntil(() => _loadedScenes.Count >= 9);

            Game.Shared.Networking.AccountEndpoint.onEnteredWorld?.Invoke();
            LoadingScreenController.Instance.Finish();
        }

        private void RecordPendingSceneOperations()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            // PurrNet's internal streamed-scene queue is not exposed by its callback API.
            // This sample covers only the project-owned global scene load above.
            int pendingOperationCount = _globalWorldLoadTimestamp > 0 ? 1 : 0;
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.PENDING_SCENE_OPERATION_COUNT, pendingOperationCount);
        }
        #endregion

        /// <summary>
        /// Gets the value provided by <c>GetWorldScenes</c>.
        /// </summary>
        public void GetWorldScenes(List<Scene> buffer)
        {
            buffer.Clear();
            buffer.AddRange(_loadedScenes);
        }

        private static Vector2Int ParseChunkCoordinate(string sceneName)
        {
            // Expected format: "World_X_Y"
            string[] parts = sceneName.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);
            return new Vector2Int(x, y);
        }
    }
}
