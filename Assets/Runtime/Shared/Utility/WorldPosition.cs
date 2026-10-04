using UnityEngine;

namespace Game.Shared.Utility
{
    [System.Serializable]
    public struct WorldPosition
    {
        public double x;
        public double y;
        public double z;

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
        public Vector3 ToLocal(WorldPosition origin)
        {
            return new Vector3(
                (float)(x - origin.x),
                (float)(y - origin.y),
                (float)(z - origin.z)
            );
        }

        public static WorldPosition operator +(WorldPosition a, WorldPosition b)
            => new WorldPosition(a.x + b.x, a.y + b.y, a.z + b.z);

        public static WorldPosition operator -(WorldPosition a, WorldPosition b)
            => new WorldPosition(a.x - b.x, a.y - b.y, a.z - b.z);
    }
}