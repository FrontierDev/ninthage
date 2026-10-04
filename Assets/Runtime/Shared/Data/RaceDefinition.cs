using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class PlayerStartingReputation
    {
        public FactionDefinition Faction;
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
        public FactionDefinition DefaultPVPFaction => defaultPVPFaction;

        [SerializeField]
        private List<PlayerStartingReputation> startingReputations = new();
        public IReadOnlyList<PlayerStartingReputation> StartingReputations => startingReputations;

        [SerializeField]
        private List<ActorStatBaseEntry> baseStats = new();
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
