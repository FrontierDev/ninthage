using UnityEngine;

namespace Game.Shared.Utility
{
    [System.Serializable]
    /// <summary>Represents a double-precision position in the streamed world.</summary>
    public struct WorldPosition
    {
        /// <summary>Gets or sets the world X coordinate.</summary>
        public double x;
        /// <summary>Gets or sets the world Y coordinate.</summary>
        public double y;
        /// <summary>Gets or sets the world Z coordinate.</summary>
        public double z;

        /// <summary>Gets whether all world coordinates are finite.</summary>
        public bool IsFinite => !double.IsNaN(x) && !double.IsInfinity(x)
            && !double.IsNaN(y) && !double.IsInfinity(y)
            && !double.IsNaN(z) && !double.IsInfinity(z);

        /// <summary>Initializes a world position.</summary>
        public WorldPosition(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// The chunk coordinate this position falls in.
        /// </summary>
        public Vector2Int Chunk
        {
            get
            {
                int cs = GameConfigurationManager.Config.ChunkSize;
                int cx = (int)System.Math.Floor(x / cs);
                int cz = (int)System.Math.Floor(z / cs);
                return new Vector2Int(cx, cz);
            }
        }

        /// <summary>
        /// The position local to the chunk (values in [0, ChunkSize) range).
        /// </summary>
        public Vector3 LocalPosition
        {
            get
            {
                int cs = GameConfigurationManager.Config.ChunkSize;
                double lx = x - System.Math.Floor(x / cs) * cs;
                double ly = y;
                double lz = z - System.Math.Floor(z / cs) * cs;
                return new Vector3((float)lx, (float)ly, (float)lz);
            }
        }

        /// <summary>
        /// Construct a WorldPosition from a chunk coordinate and a chunk-local offset.
        /// </summary>
        public static WorldPosition FromChunkLocal(Vector2Int chunk, Vector3 local)
        {
            int cs = GameConfigurationManager.Config.ChunkSize;
            return new WorldPosition(
                (double)chunk.x * cs + local.x,
                local.y,
                (double)chunk.y * cs + local.z
            );
        }

        // Convert to local-space Vector3 relative to a floating origin
        /// <summary>Converts this position to local coordinates relative to an origin.</summary>
        public Vector3 ToLocal(WorldPosition origin)
        {
            return new Vector3(
                (float)(x - origin.x),
                (float)(y - origin.y),
                (float)(z - origin.z)
            );
        }

        /// <summary>Adds two world positions.</summary>
        public static WorldPosition operator +(WorldPosition a, WorldPosition b)
            => new WorldPosition(a.x + b.x, a.y + b.y, a.z + b.z);

        /// <summary>Subtracts two world positions.</summary>
        public static WorldPosition operator -(WorldPosition a, WorldPosition b)
            => new WorldPosition(a.x - b.x, a.y - b.y, a.z - b.z);
    }
}
