using Game.Shared.Utility;
using PurrNet.Modules;
using PurrNet.Packing;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for accepted actor positions.</summary>
    public static class PackActorPositionSnapshot
    {
        [UsedByIL]
        /// <summary>Writes an accepted actor position snapshot to a bit packer.</summary>
        public static void Write(this BitPacker packer, ActorPositionSnapshot value)
        {
            packer.Write(value.IsValid);
            packer.Write(value.TickID);
            packer.Write(value.Position);
        }

        [UsedByIL]
        /// <summary>Reads an accepted actor position snapshot from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref ActorPositionSnapshot value)
        {
            bool isValid = default;
            long tickID = default;
            WorldPosition position = default;

            packer.Read(ref isValid);
            packer.Read(ref tickID);
            packer.Read(ref position);

            value = new ActorPositionSnapshot
            {
                IsValid = isValid,
                TickID = tickID,
                Position = position
            };
        }
    }
}
