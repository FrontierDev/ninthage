using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared.Performance;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Manages the client's floating origin. When the player's chunk interest
    /// changes, all loaded scene roots are shifted so the current chunk origin
    /// sits at (0, 0, 0) in Unity space, keeping floats precise.
    /// </summary>
    public class ClientPositionManager : MonoBehaviour
    {
        private static readonly PerformanceMarker ORIGIN_SHIFT_MARKER = new(
            ClientPerformanceMetrics.ORIGIN_SHIFT_DURATION,
            "Client.PositionManager.OriginShift");
        private static ClientPositionManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientPositionManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        /// <summary> The chunk the client currently considers as origin. </summary>
        public Vector2Int OriginChunk { get; private set; }

        private int _chunkSize;

        private void Awake()
        {
            _instance = this;
            _chunkSize = Game.Shared.GameConfigurationManager.Config.ChunkSize;
            _initialized = true;
        }

        /// <summary>
        /// Offsets a newly loaded scene's root objects so they align with the
        /// current floating origin. Call this once immediately after a world
        /// scene finishes loading.
        /// </summary>
        public void ApplyOriginToScene(Scene scene)
        {
            using var markerScope = ORIGIN_SHIFT_MARKER.Begin();
            var originOffset = new Vector3(OriginChunk.x * _chunkSize, 0f, OriginChunk.y * _chunkSize);
            int rootCount = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.position -= originOffset;
                rootCount++;
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_ROOT_COUNT, rootCount);
        }

        /// <summary>
        /// Call this when the player crosses a chunk boundary.
        /// Shifts every root object in every loaded scene (including global_actors)
        /// so the new chunk origin sits at world-space (0, 0, 0).
        /// </summary>
        public void SetOriginChunk(Vector2Int newChunk)
        {
            Vector2Int previousOrigin = OriginChunk;
            Vector2Int delta = newChunk - OriginChunk;
            if (delta == Vector2Int.zero)
                return;

            using var markerScope = ORIGIN_SHIFT_MARKER.Begin();

            var shift = new Vector3(delta.x * _chunkSize, 0f, delta.y * _chunkSize);
            int rootCount = 0;

            // Shift world chunk scenes.
            var worldScenes = new List<Scene>();
            ClientWorldManager.Instance.GetWorldScenes(worldScenes);

            foreach (var scene in worldScenes)
            {
                if (!scene.isLoaded) continue;
                rootCount += ShiftSceneRoots(scene, shift);
            }

            // Shift the global_actors scene so networked actors stay aligned.
            var globalActors = SceneManager.GetSceneByName("global_actors");
            if (globalActors.isLoaded)
                rootCount += ShiftSceneRoots(globalActors, shift);

            OriginChunk = newChunk;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var appliedLocalShift = -shift;
            Debug.Log(
                $"[Movement/Origin] old=({previousOrigin.x},{previousOrigin.y}) "
                + $"new=({newChunk.x},{newChunk.y}) "
                + $"localShift=({appliedLocalShift.x:F3},{appliedLocalShift.y:F3},{appliedLocalShift.z:F3}).");
#endif

            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_COUNT, 1);
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_ROOT_COUNT, rootCount);
            }
        }

        private static int ShiftSceneRoots(Scene scene, Vector3 shift)
        {
            int rootCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.position -= shift;
                rootCount++;
            }

            return rootCount;
        }

        /// <summary>
        /// Converts a chunk-coordinate world position to Unity local space
        /// accounting for the current floating origin.
        /// </summary>
        public Vector3 ChunkToLocal(Vector2Int chunk, Vector3 positionInChunk)
        {
            Vector2Int offset = chunk - OriginChunk;
            return new Vector3(
                offset.x * _chunkSize + positionInChunk.x,
                positionInChunk.y,
                offset.y * _chunkSize + positionInChunk.z
            );
        }

        /// <summary>
        /// Single entry point for converting a server-sent chunk-relative position
        /// into Unity local space. All incoming network position handlers should
        /// call this instead of using raw floats.
        /// </summary>
        public Vector3 NetworkToLocal(Vector2Int chunk, Vector3 localPosition)
        {
            return ChunkToLocal(chunk, localPosition);
        }

        /// <summary>
        /// Converts a Unity local-space position back to chunk-relative format
        /// suitable for sending to the server.
        /// </summary>
        public (Vector2Int chunk, Vector3 local) LocalToChunkRelative(Vector3 unityPosition)
        {
            int cx = Mathf.FloorToInt(unityPosition.x / _chunkSize) + OriginChunk.x;
            int cz = Mathf.FloorToInt(unityPosition.z / _chunkSize) + OriginChunk.y;
            var chunk = new Vector2Int(cx, cz);

            Vector2Int offset = chunk - OriginChunk;
            var local = new Vector3(
                unityPosition.x - offset.x * _chunkSize,
                unityPosition.y,
                unityPosition.z - offset.y * _chunkSize
            );
            return (chunk, local);
        }
    }
}
