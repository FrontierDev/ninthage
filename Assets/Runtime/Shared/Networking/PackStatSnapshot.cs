using PurrNet.Modules;
using PurrNet.Packing;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for stat snapshots.</summary>
    public static class PackStatSnapshot
    {
        [UsedByIL]
        /// <summary>Writes a stat entry to a bit packer.</summary>
        public static void Write(this BitPacker packer, StatSnapshotEntry value)
        {
            packer.Write(value.StatID);
            packer.Write(value.EffectiveMax);
            packer.Write(value.Current);
        }

        [UsedByIL]
        /// <summary>Reads a stat entry from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref StatSnapshotEntry value)
        {
            string statID = default;
            float effectiveMax = default;
            float current = default;
            packer.Read(ref statID);
            packer.Read(ref effectiveMax);
            packer.Read(ref current);
            value = new StatSnapshotEntry
            {
                StatID = statID,
                EffectiveMax = effectiveMax,
                Current = current
            };
        }

        [UsedByIL]
        /// <summary>Writes a stat snapshot to a bit packer.</summary>
        public static void Write(this BitPacker packer, StatSnapshot value)
        {
            int length = value.Stats?.Length ?? 0;
            packer.Write(length);
            for (int i = 0; i < length; i++)
                packer.Write(value.Stats[i]);
        }

        [UsedByIL]
        /// <summary>Reads a stat snapshot from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref StatSnapshot value)
        {
            int length = default;
            packer.Read(ref length);
            var stats = new StatSnapshotEntry[length];
            for (int i = 0; i < length; i++)
                packer.Read(ref stats[i]);
            value = new StatSnapshot { Stats = stats };
        }
    }
}
