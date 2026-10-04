using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using PurrNet;

namespace Game.Client
{
    /// <summary>
    /// Manages the client's floating origin. When the player's chunk interest
    /// changes, all loaded scene roots are shifted so the current chunk origin
    /// sits at (0, 0, 0) in Unity space, keeping floats precise.
    /// </summary>
    public class ClientPositionManager : MonoBehaviour
    {
        private static ClientPositionManager _instance;
        public static ClientPositionManager Instance => _instance;
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        /// <summary> The chunk the client currently considers as origin. </summary>
        public Vector2Int OriginChunk { get; private set; }

        private int chunkSize;

        private void Awake()
        {
            _instance = this;
            chunkSize = Game.Shared.GameConfigurationManager.Config.ChunkSize;
            _initialized = true;
        }

        /// <summary>
        /// Offsets a newly loaded scene's root objects so they align with the
        /// current floating origin. Call this once immediately after a world
        /// scene finishes loading.
        /// </summary>
        public void ApplyOriginToScene(Scene scene)
        {
            Vector3 originOffset = new Vector3(OriginChunk.x * chunkSize, 0f, OriginChunk.y * chunkSize);

            foreach (var root in scene.GetRootGameObjects())
                root.transform.position -= originOffset;
        }

        /// <summary>
        /// Call this when the player crosses a chunk boundary.
        /// Shifts every root object in every loaded scene (including global_actors)
        /// so the new chunk origin sits at world-space (0, 0, 0).
        /// </summary>
        public void SetOriginChunk(Vector2Int newChunk)
        {
            Vector2Int delta = newChunk - OriginChunk;
            if (delta == Vector2Int.zero)
                return;

            Vector3 shift = new Vector3(delta.x * chunkSize, 0f, delta.y * chunkSize);

            // Shift world chunk scenes.
            var worldScenes = new List<Scene>();
            ClientWorldManager.Instance.GetWorldScenes(worldScenes);

            foreach (var scene in worldScenes)
            {
                if (!scene.isLoaded) continue;
                ShiftSceneRoots(scene, shift);
            }

            // Shift the global_actors scene so networked actors stay aligned.
            var globalActors = SceneManager.GetSceneByName("global_actors");
            if (globalActors.isLoaded)
                ShiftSceneRoots(globalActors, shift);

            OriginChunk = newChunk;
        }

        private static void ShiftSceneRoots(Scene scene, Vector3 shift)
        {
            foreach (var root in scene.GetRootGameObjects())
                root.transform.position -= shift;
        }

        /// <summary>
        /// Converts a chunk-coordinate world position to Unity local space
        /// accounting for the current floating origin.
        /// </summary>
        public Vector3 ChunkToLocal(Vector2Int chunk, Vector3 positionInChunk)
        {
            Vector2Int offset = chunk - OriginChunk;
            return new Vector3(
                offset.x * chunkSize + positionInChunk.x,
                positionInChunk.y,
                offset.y * chunkSize + positionInChunk.z
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
            int cx = Mathf.FloorToInt(unityPosition.x / chunkSize) + OriginChunk.x;
            int cz = Mathf.FloorToInt(unityPosition.z / chunkSize) + OriginChunk.y;
            var chunk = new Vector2Int(cx, cz);

            Vector2Int offset = chunk - OriginChunk;
            Vector3 local = new Vector3(
                unityPosition.x - offset.x * chunkSize,
                unityPosition.y,
                unityPosition.z - offset.y * chunkSize
            );
            return (chunk, local);
        }
    }
}