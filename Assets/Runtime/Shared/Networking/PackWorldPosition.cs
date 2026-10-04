using UnityEngine;
using PurrNet.Modules;
using PurrNet.Packing;
using Game.Shared.Utility;

namespace Game.Shared.Networking
{
    public static class PackWorldPosition
    {
        [UsedByIL]
        public static void Write(this BitPacker packer, WorldPosition value)
        {
            Vector2Int chunk = value.Chunk;
            Vector3 local = value.LocalPosition;
            packer.Write(chunk.x);
            packer.Write(chunk.y);
            packer.Write(local.x);
            packer.Write(local.y);
            packer.Write(local.z);
        }

        [UsedByIL]
        public static void Read(this BitPacker packer, ref WorldPosition value)
        {
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
        }
    }
}
