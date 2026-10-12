using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Items
{
    /// <summary>Represents a runtime item instance owned by a character.</summary>
    public sealed class ItemInstance
    {
        [SerializeField, FormerlySerializedAs("guid")] private string _guid;
        [SerializeField, FormerlySerializedAs("itemId")] private string _itemID;
        [SerializeField, FormerlySerializedAs("modifications")] private List<string> _modifications;
        [SerializeField, FormerlySerializedAs("durability")] private int _durability;
        [SerializeField, FormerlySerializedAs("currentStackSize")] private int _currentStackSize;

        /// <summary>Gets the base item definition.</summary>
        public ItemDefinition BaseItem => ItemDefinitionLibrary.Instance.GetDefinition(_itemID);
        /// <summary>Gets the instance GUID.</summary>
        public string GUID => _guid;
        /// <summary>Gets the applied modification identifiers.</summary>
        public List<string> Modifications => _modifications;
        /// <summary>Gets or sets durability, clamped to the valid range.</summary>
        public int Durability
        {
            get => _durability;
            set => _durability = Mathf.Clamp(value, 0, 100);
        }
        /// <summary>Gets or sets the current stack size.</summary>
        public int CurrentStackSize
        {
            get => _currentStackSize;
            set
            {
                if (BaseItem != null)
                {
                    _currentStackSize = Mathf.Clamp(value, 1, BaseItem.MaxStackSize);
                }
                else
                {
                    _currentStackSize = value; // If item definition is missing, just set it without clamping
                }
            }
        }

        /// <summary>Initializes an item instance.</summary>
        public ItemInstance(string guid, string itemId, List<string> modifications, int durability, int currentStackSize)
        {
            _guid = guid;
            _itemID = itemId;
            _modifications = modifications ?? new();
            Durability = durability; // Use property to ensure clamping
            CurrentStackSize = currentStackSize; // Use property to ensure clamping
        }
    }
}
