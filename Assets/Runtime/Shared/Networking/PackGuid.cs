using System;
using PurrNet.Modules;
using PurrNet.Packing;

namespace Game.Shared.Networking
{
    public static class PackGuid
    {
        [UsedByIL]
        public static void Write(this BitPacker packer, Guid value)
        {
            var bytes = value.ToByteArray(); // always 16 bytes
            for (int i = 0; i < 16; i++)
                packer.Write(bytes[i]);
        }

        [UsedByIL]
        public static void Read(this BitPacker packer, ref Guid value)
        {
            var bytes = new byte[16];
            for (int i = 0; i < 16; i++)
                packer.Read(ref bytes[i]);
            value = new Guid(bytes);
        }
    }
}