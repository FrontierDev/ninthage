using PurrNet.Packing;

namespace Game.Shared.Networking
{
    [System.Serializable]
    public struct StatSnapshotEntry
    {
        public string StatId;
        public float EffectiveMax;
        public float FlatModifier;
        public float PercentModifier;
        public float Current;
    }

    [System.Serializable]
    public struct StatSnapshot
    {
        public StatSnapshotEntry[] Stats;
    }
}