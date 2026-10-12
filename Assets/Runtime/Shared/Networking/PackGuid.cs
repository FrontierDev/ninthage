using PurrNet.Modules;
using PurrNet.Packing;
using System;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for GUID values.</summary>
    public static class PackGUID
    {
        [UsedByIL]
        /// <summary>Writes a GUID to a bit packer.</summary>
        public static void Write(this BitPacker packer, Guid value)
        {
            var bytes = value.ToByteArray(); // always 16 bytes
            for (int i = 0; i < 16; i++)
                packer.Write(bytes[i]);
        }

        [UsedByIL]
        /// <summary>Reads a GUID from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref Guid value)
        {
            var bytes = new byte[16];
            for (int i = 0; i < 16; i++)
                packer.Read(ref bytes[i]);
            value = new Guid(bytes);
        }
    }
}
