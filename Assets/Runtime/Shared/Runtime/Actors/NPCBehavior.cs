using System;
using System.Collections.Generic;
using Game.Shared.Data;
using PurrNet;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Shared
{
    [System.Serializable]
    public class NPCLootTableEntry
    {
        [SerializeField] private LootTableDefinition lootTable;
        public LootTableDefinition LootTable => lootTable;

        [SerializeField, Range(0f, 1f)] private int rolls = 1;
        public int Rolls => rolls;
    }

    public class NPCBehavior : NetworkBehaviour
    {
        private Dictionary<PlayerActor, int> threatTable = new Dictionary<PlayerActor, int>();
        public IReadOnlyDictionary<PlayerActor, int> ThreatTable => threatTable;

        [SerializeField] private int baseExperience = 100;
        public int BaseExperience => baseExperience;

        [Tooltip("The range at which this NPC will become aggressive towards other units.")]
        [SerializeField] private float aggroRange = 0f;
        public float AggroRange => aggroRange;

        [SerializeField] private float aggroDelay = 3f;
        public float AggroDelay => aggroDelay;

        [SerializeField] private FactionDefinition combatFaction;
        public FactionDefinition CombatFaction => combatFaction;

        [SerializeField] private DialogueDefinition dialogue;
        public DialogueDefinition Dialogue => dialogue;

        [SerializeField] private List<NPCLootTableEntry> lootTable = new();
        public IReadOnlyList<NPCLootTableEntry> LootTable => lootTable;

        public Action onDeath;
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