using PurrNet.Packing;

namespace Game.Shared.Networking
{
    [System.Serializable]
    /// <summary>Contains the serialized values for one actor stat.</summary>
    public struct StatSnapshotEntry
    {
        /// <summary>Gets or sets the stat identifier.</summary>
        public string StatID;
        /// <summary>Gets or sets the effective maximum value.</summary>
        public float EffectiveMax;
        /// <summary>Gets or sets the flat modifier.</summary>
        public float FlatModifier;
        /// <summary>Gets or sets the percentage modifier.</summary>
        public float PercentModifier;
        /// <summary>Gets or sets the current value.</summary>
        public float Current;
    }

    [System.Serializable]
    /// <summary>Contains the serialized stat values for an actor.</summary>
    public struct StatSnapshot
    {
        /// <summary>Gets or sets the serialized stat entries.</summary>
        public StatSnapshotEntry[] Stats;
    }
}
