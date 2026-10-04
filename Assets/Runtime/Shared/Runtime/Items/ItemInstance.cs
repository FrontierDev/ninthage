using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Shared
{
    public sealed class ItemInstance
    {
        [SerializeField] private string guid;
        [SerializeField] private string itemId;
        [SerializeField] private List<string> modifications;
        [SerializeField] private int durability;
        [SerializeField] private int currentStackSize;

        public ItemDefinition BaseItem => ItemDefinitionLibrary.Instance.GetDefinition(itemId);
        public string GUID => guid;
        public List<string> Modifications => modifications;
        public int Durability
        {
            get => durability;
            set => durability = Mathf.Clamp(value, 0, 100);
        }
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

        public ItemInstance(string guid, string itemId, List<string> modifications, int durability, int currentStackSize)
        {
            this.guid = guid;
            this.itemId = itemId;
            this.modifications = modifications ?? new List<string>();
            Durability = durability; // Use property to ensure clamping
            CurrentStackSize = currentStackSize; // Use property to ensure clamping
        }
    }
}