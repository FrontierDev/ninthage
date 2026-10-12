using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Describes how an equipment slot changed.</summary>
    public enum EquipmentSlotChangeType : byte
    {
        Set,
        Removed
    }

    /// <summary>Describes one equipment slot change.</summary>
    public struct EquipmentSlotChange
    {
        public ItemSlot Slot;
        public EquipmentSlotChangeType ChangeType;
        public ItemInstance Item; // null when ChangeType == Removed

        /// <summary>Initializes an equipment slot change.</summary>
        public EquipmentSlotChange(ItemSlot slot, EquipmentSlotChangeType changeType, ItemInstance item = null)
        {
            Slot = slot;
            ChangeType = changeType;
            Item = item;
        }
    }

    /// <summary>Contains a set of equipment changes.</summary>
    public struct EquipmentDelta
    {
        public List<EquipmentSlotChange> Changes;

        /// <summary>Initializes an equipment delta.</summary>
        public EquipmentDelta(List<EquipmentSlotChange> changes)
        {
            Changes = changes;
        }
    }

    /// <summary>Manages a player's equipped items.</summary>
    public sealed class PlayerEquipment : NetworkBehaviour
    {
        [SerializeField] private readonly Dictionary<ItemSlot, ItemInstance> equipment = new();
        /// <summary>Gets the equipped items by slot.</summary>
        public IReadOnlyDictionary<ItemSlot, ItemInstance> Equipment => equipment;

        private PlayerInventory playerInventory;
        private ActorStatContainer _statContainer;

        private void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
            _statContainer = GetComponent<ActorStatContainer>();

            foreach (var slot in System.Enum.GetValues(typeof(ItemSlot)))
            {
                equipment.Add((ItemSlot)slot, null);
            }
        }

        /// <summary>Gets an equipped item for a slot, if present.</summary>
        public bool TryGetItem(ItemSlot slot, out ItemInstance item)
        {
            if (equipment.ContainsKey(slot))
            {
                item = equipment[slot];
                return item != null;
            }

            item = null;
            return false;
        }

        /// <summary>Loads equipment from persisted character data.</summary>
        public void LoadEquipment(CharacterData data)
        {
            equipment.Clear();
            foreach (var slot in System.Enum.GetValues(typeof(ItemSlot)))
            {
                equipment.Add((ItemSlot)slot, null);
            }

            foreach (var entry in data.SavedEquipment)
            {
                var itemInstance = new ItemInstance(entry.ItemGuid,
                    entry.ItemId,
                    entry.Modifications,
                    entry.Durability,
                    entry.StackSize);
                equipment[(ItemSlot)entry.SlotIndex] = itemInstance;
            }

            if (!Game.Shared.Runtime.IsServer())
                CharacterEndpoint.onClientEquipmentUpdated?.Invoke();
            else
                GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

        /// <summary>Equips an item and updates the inventory.</summary>
        public void EquipItem(ItemSlot slot, ItemInstance itemInstance, int? inventorySlot = null)
        {
            // Check if the player can equip the item (TO DO)

            // Is there an item in that slot already?
            bool hasExistingItem = equipment[slot] != null;
            if (hasExistingItem)
            {
                // The player must have an inventory slot, so we can just add the 
                // existing item back to the inventory. Remove the new item from the inventory,
                // and update the equipment slot with the new item.
                var existingItem = equipment[slot];
                equipment[slot] = itemInstance;
                playerInventory.RemoveItemAtSlot(inventorySlot.Value, 1);
                playerInventory.AddItem(existingItem, out var remainder, 1);

            }
            else
            {
                // No existing item, so just equip the new item and remove it from the inventory.
                equipment[slot] = itemInstance;
                playerInventory.RemoveItemAtSlot(inventorySlot.Value, 1);
            }

            var delta = new EquipmentDelta(new List<EquipmentSlotChange>
            {
                new EquipmentSlotChange(slot, EquipmentSlotChangeType.Set, itemInstance)
            });

            if (Game.Shared.Runtime.IsServer())
            {
                CharacterEndpoint.onUpdateStatsRequest?.Invoke(_statContainer);
                Client_UpdateEquipment(owner.Value, delta);
            }

            GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

        /// <summary>Unequips an item into the inventory.</summary>
        public void UnequipItem(ItemSlot slot)
        {
            if (equipment.ContainsKey(slot))
            {
                if (playerInventory.GetRemainingSlots() > 0)
                {
                    var existingItem = equipment[slot];
                    equipment[slot] = null;
                    playerInventory.AddItem(existingItem, out var remainder, 1);

                    var delta = new EquipmentDelta(new List<EquipmentSlotChange>
                    {
                        new EquipmentSlotChange(slot, EquipmentSlotChangeType.Removed)
                    });

                    if (Game.Shared.Runtime.IsServer())
                    {
                        Client_UpdateEquipment(owner.Value, delta);
                        GetComponent<ActorSpellcaster>().RecalculateOverrides();
                        CharacterEndpoint.onUpdateStatsRequest?.Invoke(_statContainer);
                    }
                }
                else
                {
                    Debug.LogWarning("Cannot unequip item - inventory is full.");
                }
            }
            else
            {
                Debug.LogWarning($"No item equipped in slot {slot} to unequip.");
            }
        }

        [ServerRpc]
        /// <summary>Requests that the server equip an inventory item.</summary>
        public void Server_RequestEquipItem(ItemSlot slot, int inventorySlot, RPCInfo rpcInfo = default)
        {
            var clientId = rpcInfo.sender;
            if (clientId != owner.Value)
                return;

            var itemInstance = playerInventory.GetItemAtSlot(inventorySlot);
            if (itemInstance != null)
            {
                EquipItem(slot, itemInstance, inventorySlot);
            }
        }

        [ServerRpc]
        /// <summary>Requests that the server unequip an item.</summary>
        public void Server_RequestUnequipItem(ItemSlot slot, RPCInfo rpcInfo = default)
        {
            var clientId = rpcInfo.sender;
            if (clientId != owner.Value)
                return;

            UnequipItem(slot);
        }

        [TargetRpc]
        /// <summary>Applies an equipment update on a client.</summary>
        public void Client_UpdateEquipment(PlayerID target, EquipmentDelta delta, RPCInfo rpcInfo = default)
        {
            foreach (var change in delta.Changes)
            {
                switch (change.ChangeType)
                {
                    case EquipmentSlotChangeType.Set:
                        equipment[change.Slot] = change.Item;
                        break;
                    case EquipmentSlotChangeType.Removed:
                        equipment[change.Slot] = null;
                        break;
                }
            }

            CharacterEndpoint.onClientEquipmentUpdated?.Invoke();
        }
    }
}
