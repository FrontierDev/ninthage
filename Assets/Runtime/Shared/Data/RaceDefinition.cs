using System;
using System.Collections.Generic;
using Game.Shared.Runtime.Actors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Defines a faction reputation granted when a character starts.</summary>
    public class PlayerStartingReputation
    {
        /// <summary>Gets or sets the starting faction.</summary>
        public FactionDefinition Faction;
        /// <summary>Gets or sets the starting reputation.</summary>
        public int Reputation;
    }

    /// <summary>
    /// Defines a player race (e.g. Human, Elf, Dwarf).
    /// </summary>
    [CreateAssetMenu(fileName = "RaceDefinition_", menuName = "NinthAge/Definitions/Race Definition")]
    public class RaceDefinition : DataDefinition
    {
        [SerializeField]
        [TextArea(3, 5)]
        [FormerlySerializedAs("description")]
        private string _description;

        [SerializeField]
        [FormerlySerializedAs("icon")]
        private Sprite _icon;

        [SerializeField]
        [FormerlySerializedAs("isPlayable")]
        private bool _isPlayable;

        [SerializeField]
        [FormerlySerializedAs("defaultPVPFaction")]
        private FactionDefinition _defaultPVPFaction;
        /// <summary>Gets the default player-versus-player faction.</summary>
        public FactionDefinition DefaultPVPFaction => _defaultPVPFaction;

        [SerializeField]
        [FormerlySerializedAs("startingReputations")]
        private List<PlayerStartingReputation> _startingReputations = new();
        /// <summary>Gets the starting reputation entries.</summary>
        public IReadOnlyList<PlayerStartingReputation> StartingReputations => _startingReputations;

        [SerializeField]
        [FormerlySerializedAs("baseStats")]
        private List<ActorStatBaseEntry> _baseStats = new();
        /// <summary>Gets the base stats for the race.</summary>
        public IReadOnlyList<ActorStatBaseEntry> BaseStats => _baseStats;

        /// <summary>
        /// Detailed description of this race.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Indicates if this race is playable by the player.
        /// </summary>
        public bool IsPlayable => _isPlayable;

        protected override string AssetPrefix => "Race";
    }
}
