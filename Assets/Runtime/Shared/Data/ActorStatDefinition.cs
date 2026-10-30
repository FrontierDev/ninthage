using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Identifies whether a stat is an attribute or a resource.</summary>
    public enum ActorStatCategory { Attribute, Resource }
    /// <summary>Identifies how a resource is resolved after elapsed offline time.</summary>
    public enum ActorStatOfflinePolicy
    {
        Unchanged = 0,
        Regenerate = 1,
        Decay = 2,
        Reset = 3
    }
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
        [FormerlySerializedAs("description")]
        private string _description;

        [SerializeField]
        [FormerlySerializedAs("icon")]
        private Sprite _icon;
        /// <summary>Gets the stat icon.</summary>
        public Sprite Icon => _icon;

        [SerializeField]
        [FormerlySerializedAs("category")]
        private ActorStatCategory _category;

        [SerializeField]
        [FormerlySerializedAs("baseValueMode")]
        private ActorStatBaseValueMode _baseValueMode;

        [SerializeField]
        [FormerlySerializedAs("baseValue")]
        private float _baseValue;

        [SerializeField]
        [FormerlySerializedAs("sourceStat")]
        private ActorStatDefinition _sourceStat;

        [SerializeField]
        [FormerlySerializedAs("multiplier")]
        private float _multiplier = 0f;

        [SerializeField]
        [FormerlySerializedAs("startsAtZero")]
        private bool _startsAtZero;

        [SerializeField]
        [FormerlySerializedAs("regenMode")]
        private ActorStatBaseValueMode _regenMode;

        [SerializeField]
        [FormerlySerializedAs("regenPerSecond")]
        private float _regenPerSecond;

        [SerializeField]
        [FormerlySerializedAs("regenSourceStat")]
        private ActorStatDefinition _regenSourceStat;

        [SerializeField]
        [FormerlySerializedAs("regenMultiplier")]
        private float _regenMultiplier = 0f;

        [SerializeField]
        private ActorStatOfflinePolicy _offlinePolicy = ActorStatOfflinePolicy.Unchanged;

        [SerializeField]
        [FormerlySerializedAs("replicationMode")]
        private ActorStatReplicationMode _replicationMode = ActorStatReplicationMode.Owner;
        /// <summary>Gets the stat replication mode.</summary>
        public ActorStatReplicationMode ReplicationMode => _replicationMode;

        [SerializeField]
        [FormerlySerializedAs("tags")]
        private List<string> _tags = new();
        /// <summary>Gets the tags assigned to this stat.</summary>
        public List<string> Tags => _tags;

        /// <summary>
        /// Detailed description of this stat. This may appear in the character sheet.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Category of this stat (e.g. Attribute, Resource). This can be used for UI grouping and logic.
        /// </summary>
        public ActorStatCategory Category => _category;

        /// <summary>
        /// Determines how the base value of this stat is calculated (e.g. fixed value, derived from other stats).
        /// This can be used to control how the stat is initialized and updated.
        /// </summary>
        public ActorStatBaseValueMode BaseValueMode => _baseValueMode;

        /// <summary>
        /// The base value of this stat. Its meaning depends on the BaseValueMode (e.g. fixed value, multiplier, etc.).
        /// This can be used as the starting point for calculating the current value of the stat.
        /// </summary>
        public float BaseValue => _baseValue;

        /// <summary>
        /// For derived stats, this is the source stat that this stat is based on. For example, a "Health" stat might be derived from a "Constitution" stat.
        /// This can be used to establish dependencies between stats and control how derived stats are calculated.
        /// </summary>
        public ActorStatDefinition SourceStat => _sourceStat;

        /// <summary>
        /// Multiplier applied to the source stat to calculate the derived stat's value.
        /// This can be used to scale the effect of the source stat on the derived stat.
        /// </summary>
        public float Multiplier => _multiplier;

        /// <summary>
        /// If true, this stat starts at 0 instead of the base value. This can be used for stats that should be empty at the start (e.g. Experience).
        /// </summary>
        /// <returns></returns>
        public bool StartsAtZero => _startsAtZero;

        /// <summary>Gets how regeneration is calculated.</summary>
        public ActorStatBaseValueMode RegenMode => _regenMode;
        /// <summary>Gets the regeneration rate per second.</summary>
        public float RegenPerSecond => _regenPerSecond;
        /// <summary>Gets the stat used as the regeneration source.</summary>
        public ActorStatDefinition RegenSourceStat => _regenSourceStat;
        /// <summary>Gets the regeneration multiplier.</summary>
        public float RegenMultiplier => _regenMultiplier;
        /// <summary>
        /// Gets the explicit offline policy for this stat when it is a resource.
        /// Attributes ignore this policy.
        /// Regenerate and Decay use the existing normal authored regeneration rate, while Reset uses the existing resource initialization rule.
        /// The <c>health</c> resource remains explicitly Unchanged under Stage 1.7.
        /// </summary>
        public ActorStatOfflinePolicy OfflinePolicy => _offlinePolicy;

        protected override string AssetPrefix => "ActorStat";
    }
}
