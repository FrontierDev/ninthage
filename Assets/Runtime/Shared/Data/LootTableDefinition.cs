using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Defines one weighted item entry in a loot table.</summary>
    public class LootTableEntry
    {
        [SerializeField, FormerlySerializedAs("item")] private ItemDefinition _item;
        /// <summary>Gets the item awarded by this entry.</summary>
        public ItemDefinition Item => _item;

        // Relative weight among all eligible entries.
        [SerializeField, Min(0f), FormerlySerializedAs("weight")] private float _weight = 1f;
        /// <summary>Gets the relative entry weight.</summary>
        public float Weight => _weight;

        [SerializeField, Range(0f, 1f), FormerlySerializedAs("chance")] private float _chance = 1.0f;
        /// <summary>Gets the normalized drop chance.</summary>
        public float Chance => _chance;

        /// <summary>Sets the normalized drop chance for this entry.</summary>
        public void SetDropChance(float newChance)
        {
            _chance = Mathf.Clamp01(newChance);
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
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("rollChance")] private float _rollChance = 1.0f;
        /// <summary>Gets the chance for this table to be rolled.</summary>
        public float RollChance => _rollChance;

        // Chance for this loot table to yield nothing.
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("nullRollChance")] private float _nullRollChance = 0.1f;
        /// <summary>Gets the chance for the table to yield no item.</summary>
        public float NullRollChance => _nullRollChance;

        // Editor-authored weights.
        [SerializeField, FormerlySerializedAs("entryDefinitions")] private List<LootTableEntry> _entryDefinitions = new();
        /// <summary>Gets the authored loot entries.</summary>
        public IReadOnlyList<LootTableEntry> EntryDefinitions => _entryDefinitions;

        /// <summary>
        /// Rolls this table and returns a single item, or null if the table does not proc
        /// or if the null roll is selected.
        /// </summary>
        public bool Roll(out ItemDefinition item)
        {
            item = null;

            if (_entryDefinitions == null || _entryDefinitions.Count == 0)
                return false;

            // Table-level proc check
            if (_rollChance < 1f)
            {
                if (UnityEngine.Random.Range(0f, 1f) > _rollChance)
                    return false;
            }

            // Roll and test null-roll first
            float roll = UnityEngine.Random.Range(0f, 1f);
            float cumulative = _nullRollChance;
            if (roll < cumulative)
                return false;

            // Walk entries by their serialized `Chance` values (these are set in OnDefinitionValidate)
            for (int i = 0; i < _entryDefinitions.Count; i++)
            {
                var entry = _entryDefinitions[i];
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
            for (int i = _entryDefinitions.Count - 1; i >= 0; i--)
            {
                var entry = _entryDefinitions[i];
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
            float nonNullChance = 1f - _nullRollChance;

            // Get the total weight of the entry definitions.
            totalWeight = _entryDefinitions.Sum(x => x.Weight);

            // Map each loot entry's weight to a chance.
            foreach (var def in _entryDefinitions)
            {
                var contribution = def.Weight / totalWeight;
                def.SetDropChance(nonNullChance * contribution);
            }
        }
    }
}
