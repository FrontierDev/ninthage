using PurrNet.Modules;
using PurrNet.Packing;

namespace Game.Shared.Networking
{
    public static class PackStatSnapshot
    {
        [UsedByIL]
        public static void Write(this BitPacker packer, StatSnapshotEntry value)
        {
            packer.Write(value.StatId);
            packer.Write(value.EffectiveMax);
            packer.Write(value.Current);
        }

        [UsedByIL]
        public static void Read(this BitPacker packer, ref StatSnapshotEntry value)
        {
            string statId = default;
            float effectiveMax = default;
            float current = default;
            packer.Read(ref statId);
            packer.Read(ref effectiveMax);
            packer.Read(ref current);
            value = new StatSnapshotEntry
            {
                StatId = statId,
                EffectiveMax = effectiveMax,
                Current = current
            };
        }

        [UsedByIL]
        public static void Write(this BitPacker packer, StatSnapshot value)
        {
            int length = value.Stats?.Length ?? 0;
            packer.Write(length);
            for (int i = 0; i < length; i++)
                packer.Write(value.Stats[i]);
        }

        [UsedByIL]
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