using Game.Shared.Data;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Shared
{
    /// <summary>Defines one NPC loot table and its number of rolls.</summary>
    [System.Serializable]
    public class NPCLootTableEntry
    {
        [SerializeField] private LootTableDefinition lootTable;
        /// <summary>Gets the loot table definition.</summary>
        public LootTableDefinition LootTable => lootTable;

        [SerializeField, Range(0f, 1f)] private int rolls = 1;
        /// <summary>Gets the number of rolls.</summary>
        public int Rolls => rolls;
    }

    /// <summary>Manages shared NPC combat, threat, dialogue, and loot state.</summary>
    public class NPCBehavior : NetworkBehaviour
    {
        private Dictionary<PlayerActor, int> threatTable = new();
        /// <summary>Gets threat values by player.</summary>
        public IReadOnlyDictionary<PlayerActor, int> ThreatTable => threatTable;

        [SerializeField] private int baseExperience = 100;
        /// <summary>Gets the experience awarded by this NPC.</summary>
        public int BaseExperience => baseExperience;

        [Tooltip("The range at which this NPC will become aggressive towards other units.")]
        [SerializeField] private float aggroRange = 0f;
        /// <summary>Gets the aggression range.</summary>
        public float AggroRange => aggroRange;

        [SerializeField] private float aggroDelay = 3f;
        /// <summary>Gets the aggression delay.</summary>
        public float AggroDelay => aggroDelay;

        [SerializeField] private FactionDefinition combatFaction;
        /// <summary>Gets the NPC's combat faction.</summary>
        public FactionDefinition CombatFaction => combatFaction;

        [SerializeField] private DialogueDefinition dialogue;
        /// <summary>Gets the NPC dialogue definition.</summary>
        public DialogueDefinition Dialogue => dialogue;

        [SerializeField] private List<NPCLootTableEntry> lootTable = new();
        /// <summary>Gets the configured loot entries.</summary>
        public IReadOnlyList<NPCLootTableEntry> LootTable => lootTable;

        /// <summary>Raised when the NPC dies.</summary>
        public Action onDeath;
        /// <summary>Raised when the NPC takes damage.</summary>
        public Action<Actor, int> onDamaged;

        private void Awake()
        {
            onDeath += OnDeath;
            onDamaged += OnDamaged;
        }

        /// <summary>
        /// Handles the NPC's death, such as playing death animations and notifying clients.
        /// </summary>
        private void OnDeath()
        {
            GetComponent<NetworkAnimator>().SetTrigger("Death");
            GetComponent<NavMeshAgent>().isStopped = true;
        }

        /// <summary>Records damage from an actor in the threat table.</summary>
        public void OnDamaged(Actor source, int damage)
        {
            if (source is PlayerActor playerSource)
            {
                // Increase threat for the attacking player.
                if (!threatTable.ContainsKey(playerSource))
                {
                    threatTable[playerSource] = 0;
                }

                threatTable[playerSource] += damage;
            }
        }
    }
}
