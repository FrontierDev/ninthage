using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Represents a runtime item instance owned by a character.</summary>
    public sealed class ItemInstance
    {
        [SerializeField] private string guid;
        [SerializeField] private string itemId;
        [SerializeField] private List<string> modifications;
        [SerializeField] private int durability;
        [SerializeField] private int currentStackSize;

        /// <summary>Gets the base item definition.</summary>
        public ItemDefinition BaseItem => ItemDefinitionLibrary.Instance.GetDefinition(itemId);
        /// <summary>Gets the instance GUID.</summary>
        public string GUID => guid;
        /// <summary>Gets the applied modification identifiers.</summary>
        public List<string> Modifications => modifications;
        /// <summary>Gets or sets durability, clamped to the valid range.</summary>
        public int Durability
        {
            get => durability;
            set => durability = Mathf.Clamp(value, 0, 100);
        }
        /// <summary>Gets or sets the current stack size.</summary>
        public int CurrentStackSize
        {
            get => currentStackSize;
            set
            {
                if (BaseItem != null)
                {
                    currentStackSize = Mathf.Clamp(value, 1, BaseItem.MaxStackSize);
                }
                else
                {
                    currentStackSize = value; // If item definition is missing, just set it without clamping
                }
            }
        }

        /// <summary>Initializes an item instance.</summary>
        public ItemInstance(string guid, string itemId, List<string> modifications, int durability, int currentStackSize)
        {
            this.guid = guid;
            this.itemId = itemId;
            this.modifications = modifications ?? new();
            Durability = durability; // Use property to ensure clamping
            CurrentStackSize = currentStackSize; // Use property to ensure clamping
        }
    }
}
