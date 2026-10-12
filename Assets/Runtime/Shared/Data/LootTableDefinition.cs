using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Defines one weighted item entry in a loot table.</summary>
    public class LootTableEntry
    {
        [SerializeField] private ItemDefinition item;
        /// <summary>Gets the item awarded by this entry.</summary>
        public ItemDefinition Item => item;

        // Relative weight among all eligible entries.
        [SerializeField, Min(0f)] private float weight = 1f;
        /// <summary>Gets the relative entry weight.</summary>
        public float Weight => weight;

        [SerializeField, Range(0f, 1f)] private float chance = 1.0f;
        /// <summary>Gets the normalized drop chance.</summary>
        public float Chance => chance;

        /// <summary>Sets the normalized drop chance for this entry.</summary>
        public void SetDropChance(float newChance)
        {
            chance = Mathf.Clamp01(newChance);
        }
    }

    /// <summary>
    /// Defines a loot table. NPCs will have a main loot table that is always rolled (but could have a
    /// chance to not drop anything). Additional loot tables that are rolled separately (e.g. a rare
    /// loot table that has a small chance to be rolled, but guarantees a rare item if it is).
    /// </summary>
    [CreateAssetMenu(fileName = "LootTableDefinition_", menuName = "NinthAge/Definitions/Loot Table")]
    public class LootTableDefinition : DataDefinition
    {
        // Chance for this table itself to be rolled.
        [SerializeField, Range(0f, 1f)] private float rollChance = 1.0f;
        /// <summary>Gets the chance for this table to be rolled.</summary>
        public float RollChance => rollChance;

        // Chance for this loot table to yield nothing.
        [SerializeField, Range(0f, 1f)] private float nullRollChance = 0.1f;
        /// <summary>Gets the chance for the table to yield no item.</summary>
        public float NullRollChance => nullRollChance;

        // Editor-authored weights.
        [SerializeField] private List<LootTableEntry> entryDefinitions = new();
        /// <summary>Gets the authored loot entries.</summary>
        public IReadOnlyList<LootTableEntry> EntryDefinitions => entryDefinitions;

        /// <summary>
        /// Rolls this table and returns a single item, or null if the table does not proc
        /// or if the null roll is selected.
        /// </summary>
        public bool Roll(out ItemDefinition item)
        {
            item = null;

            if (entryDefinitions == null || entryDefinitions.Count == 0)
                return false;

            // Table-level proc check
            if (rollChance < 1f)
            {
                if (UnityEngine.Random.Range(0f, 1f) > rollChance)
                    return false;
            }

            // Roll and test null-roll first
            float roll = UnityEngine.Random.Range(0f, 1f);
            float cumulative = nullRollChance;
            if (roll < cumulative)
                return false;

            // Walk entries by their serialized `Chance` values (these are set in OnDefinitionValidate)
            for (int i = 0; i < entryDefinitions.Count; i++)
            {
                var entry = entryDefinitions[i];
                if (entry == null || entry.Item == null) continue;
                float chance = entry.Chance;
                if (chance <= 0f) continue;

                cumulative += chance;
                if (roll < cumulative)
                {
                    item = entry.Item;
                    return true;
                }
            }

            // Fallback for floating-point rounding: return the last valid entry if any
            for (int i = entryDefinitions.Count - 1; i >= 0; i--)
            {
                var entry = entryDefinitions[i];
                if (entry != null && entry.Item != null && entry.Chance > 0f)
                {
                    item = entry.Item;
                    return true;
                }
            }

            return false;
        }

        protected override string AssetPrefix => "LootTable";

        protected override void OnDefinitionValidate()
        {
            float totalWeight = 0f;
            float nonNullChance = 1f - nullRollChance;

            // Get the total weight of the entry definitions.
            totalWeight = entryDefinitions.Sum(x => x.Weight);

            // Map each loot entry's weight to a chance.
            foreach (var def in entryDefinitions)
            {
                var contribution = def.Weight / totalWeight;
                def.SetDropChance(nonNullChance * contribution);
            }
        }
    }
}
