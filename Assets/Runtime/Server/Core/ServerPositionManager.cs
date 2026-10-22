using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Utility;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>
    /// Server-side position authority for network/world coordinate conversion.
    /// </summary>
    public sealed class ServerPositionManager : MonoBehaviour
    {
        private static ServerPositionManager _instance;
        /// <summary>Gets the active server position manager.</summary>
        public static ServerPositionManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the position manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerPositionManager is already initialized!");
                return;
            }

            _instance = this;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }

        /// <summary>Converts a chunk-relative position to an absolute WorldPosition for network encoding.</summary>
        public WorldPosition ToWorld(Vector2Int chunk, Vector3 localPos)
        {
            return WorldPosition.FromChunkLocal(chunk, localPos);
        }

        /// <summary>
        /// Converts an absolute WorldPosition to its existing chunk-relative network encoding.
        /// This preserves the existing chunk/local transport representation.
        /// </summary>
        public (Vector2Int chunk, Vector3 local) ToChunkLocal(WorldPosition worldPos)
        {
            return (worldPos.Chunk, worldPos.LocalPosition);
        }

    }
}
