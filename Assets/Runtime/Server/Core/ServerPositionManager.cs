using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using Game.Shared.Utility;

namespace Game.Server
{
    /// <summary>
    /// Server-side position authority. Converts between double-precision
    /// WorldPosition and chunk-relative (Vector2Int, Vector3) pairs.
    /// Also provides scaffolding for future per-chunk PhysX origin recentering.
    /// </summary>
    public sealed class ServerPositionManager : MonoBehaviour
    {
        private static ServerPositionManager _instance;
        public static ServerPositionManager Instance => _instance;
        private static bool _initialized = false;
        public static bool IsInitialized => _initialized;

        private int chunkSize;

        /// <summary>
        /// The chunk the server currently considers as the physics origin.
        /// Reserved for future PhysX recentering; initially (0,0).
        /// </summary>
        public Vector2Int ServerOriginChunk { get; private set; }

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerPositionManager is already initialized!");
                return;
            }
            _instance = this;
            chunkSize = Game.Shared.GameConfigurationManager.Config.ChunkSize;
            ServerOriginChunk = Vector2Int.zero;
            _initialized = true;
        }

        /// <summary>
        /// Convert a chunk-relative position to an absolute WorldPosition (doubles).
        /// </summary>
        public WorldPosition ToWorld(Vector2Int chunk, Vector3 localPos)
        {
            return WorldPosition.FromChunkLocal(chunk, localPos);
        }

        /// <summary>
        /// Convert an absolute WorldPosition (doubles) to a chunk-relative pair.
        /// The returned float3 values are always in the [0, ChunkSize) range.
        /// </summary>
        public (Vector2Int chunk, Vector3 local) ToChunkLocal(WorldPosition worldPos)
        {
            return (worldPos.Chunk, worldPos.LocalPosition);
        }

        /// <summary>
        /// Stub for future PhysX recentering. When implemented, this will shift
        /// all physics bodies and colliders so that the given chunk sits at (0,0,0)
        /// in PhysX space, keeping floats precise for server-side physics.
        /// </summary>
        public void RecentrePhysicsOrigin(Vector2Int newOriginChunk)
        {
            if (newOriginChunk == ServerOriginChunk) return;
            Debug.Log($"ServerPositionManager: RecentrePhysicsOrigin requested from {ServerOriginChunk} to {newOriginChunk} (NYI).");
            ServerOriginChunk = newOriginChunk;
        }
    }
}
