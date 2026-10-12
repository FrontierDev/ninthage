using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Identifies whether a stat is an attribute or a resource.</summary>
    public enum ActorStatCategory { Attribute, Resource }
    /// <summary>Identifies how a stat's base value is calculated.</summary>
    public enum ActorStatBaseValueMode { Fixed, Derived, Rating }
    /// <summary>Identifies which clients receive stat updates.</summary>
    public enum ActorStatReplicationMode { Server, Owner, Observers }

    /// <summary>
    /// Defines a stat that an actor can have.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorStatDefinition_", menuName = "NinthAge/Definitions/Actor Stat Definition")]
    public class ActorStatDefinition : DataDefinition
    {
        [SerializeField]
        [TextArea(3, 5)]
        private string description;

        [SerializeField]
        private Sprite icon;
        /// <summary>Gets the stat icon.</summary>
        public Sprite Icon => icon;

        [SerializeField]
        private ActorStatCategory category;

        [SerializeField]
        private ActorStatBaseValueMode baseValueMode;

        [SerializeField]
        private float baseValue;

        [SerializeField]
        private ActorStatDefinition sourceStat;

        [SerializeField]
        private float multiplier = 0f;

        [SerializeField]
        private bool startsAtZero;

        [SerializeField]
        private ActorStatBaseValueMode regenMode;

        [SerializeField]
        private float regenPerSecond;

        [SerializeField]
        private ActorStatDefinition regenSourceStat;

        [SerializeField]
        private float regenMultiplier = 0f;

        [SerializeField]
        private ActorStatReplicationMode replicationMode = ActorStatReplicationMode.Owner;
        /// <summary>Gets the stat replication mode.</summary>
        public ActorStatReplicationMode ReplicationMode => replicationMode;

        [SerializeField]
        private List<string> tags = new();
        /// <summary>Gets the tags assigned to this stat.</summary>
        public List<string> Tags => tags;

        /// <summary>
        /// Detailed description of this stat. This may appear in the character sheet.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Category of this stat (e.g. Attribute, Resource). This can be used for UI grouping and logic.
        /// </summary>
        public ActorStatCategory Category => category;

        /// <summary>
        /// Determines how the base value of this stat is calculated (e.g. fixed value, derived from other stats).
        /// This can be used to control how the stat is initialized and updated.
        /// </summary>
        public ActorStatBaseValueMode BaseValueMode => baseValueMode;

        /// <summary>
        /// The base value of this stat. Its meaning depends on the BaseValueMode (e.g. fixed value, multiplier, etc.).
        /// This can be used as the starting point for calculating the current value of the stat.
        /// </summary>
        public float BaseValue => baseValue;

        /// <summary>
        /// For derived stats, this is the source stat that this stat is based on. For example, a "Health" stat might be derived from a "Constitution" stat.
        /// This can be used to establish dependencies between stats and control how derived stats are calculated.
        /// </summary>
        public ActorStatDefinition SourceStat => sourceStat;

        /// <summary>
        /// Multiplier applied to the source stat to calculate the derived stat's value.
        /// This can be used to scale the effect of the source stat on the derived stat.
        /// </summary>
        public float Multiplier => multiplier;

        /// <summary>
        /// If true, this stat starts at 0 instead of the base value. This can be used for stats that should be empty at the start (e.g. Experience).
        /// </summary>
        /// <returns></returns>
        public bool StartsAtZero => startsAtZero;

        /// <summary>Gets how regeneration is calculated.</summary>
        public ActorStatBaseValueMode RegenMode => regenMode;
        /// <summary>Gets the regeneration rate per second.</summary>
        public float RegenPerSecond => regenPerSecond;
        /// <summary>Gets the stat used as the regeneration source.</summary>
        public ActorStatDefinition RegenSourceStat => regenSourceStat;
        /// <summary>Gets the regeneration multiplier.</summary>
        public float RegenMultiplier => regenMultiplier;

        protected override string AssetPrefix => "ActorStat";
    }
}
