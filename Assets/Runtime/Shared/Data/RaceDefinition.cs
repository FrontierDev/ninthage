using System;
using System.Collections.Generic;
using UnityEngine;

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
        private string description;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private bool isPlayable;

        [SerializeField]
        private FactionDefinition defaultPVPFaction;
        /// <summary>Gets the default player-versus-player faction.</summary>
        public FactionDefinition DefaultPVPFaction => defaultPVPFaction;

        [SerializeField]
        private List<PlayerStartingReputation> startingReputations = new();
        /// <summary>Gets the starting reputation entries.</summary>
        public IReadOnlyList<PlayerStartingReputation> StartingReputations => startingReputations;

        [SerializeField]
        private List<ActorStatBaseEntry> baseStats = new();
        /// <summary>Gets the base stats for the race.</summary>
        public IReadOnlyList<ActorStatBaseEntry> BaseStats => baseStats;

        /// <summary>
        /// Detailed description of this race.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Indicates if this race is playable by the player.
        /// </summary>
        public bool IsPlayable => isPlayable;

        protected override string AssetPrefix => "Race";
    }
}
