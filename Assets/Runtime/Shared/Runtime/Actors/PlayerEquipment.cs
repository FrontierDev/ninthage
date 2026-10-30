using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Items;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Actors
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
        [SerializeField, FormerlySerializedAs("equipment")] private readonly Dictionary<ItemSlot, ItemInstance> _equipment = new();
        /// <summary>Gets the equipped items by slot.</summary>
        public IReadOnlyDictionary<ItemSlot, ItemInstance> Equipment => _equipment;

        [FormerlySerializedAs("playerInventory")] private PlayerInventory _playerInventory;
        private ActorStatContainer _statContainer;

        private void Awake()
        {
            _playerInventory = GetComponent<PlayerInventory>();
            _statContainer = GetComponent<ActorStatContainer>();

            foreach (var slot in System.Enum.GetValues(typeof(ItemSlot)))
            {
                _equipment.Add((ItemSlot)slot, null);
            }
        }

        /// <summary>Gets an equipped item for a slot, if present.</summary>
        public bool TryGetItem(ItemSlot slot, out ItemInstance item)
        {
            if (_equipment.ContainsKey(slot))
            {
                item = _equipment[slot];
                return item != null;
            }

            item = null;
            return false;
        }

        /// <summary>Loads equipment from persisted character data.</summary>
        public void LoadEquipment(CharacterData data)
        {
            _equipment.Clear();
            foreach (var slot in System.Enum.GetValues(typeof(ItemSlot)))
            {
                _equipment.Add((ItemSlot)slot, null);
            }

            foreach (var entry in data.SavedEquipment)
            {
                var itemInstance = new ItemInstance(entry.ItemGUID,
                    entry.ItemID,
                    entry.Modifications,
                    entry.Durability,
                    entry.StackSize);
                _equipment[(ItemSlot)entry.SlotIndex] = itemInstance;
            }

            if (!Game.Shared.Runtime.Runtime.IsServer())
                CharacterEndpoint.onClientEquipmentUpdated?.Invoke();
            else
                GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

        /// <summary>Equips an item and updates the inventory.</summary>
        public void EquipItem(ItemSlot slot, ItemInstance itemInstance, int? inventorySlot = null)
        {
            if (!Game.Shared.Runtime.Runtime.IsServer()
                || _playerInventory == null
                || !inventorySlot.HasValue
                || !Enum.IsDefined(typeof(ItemSlot), slot)
                || slot == ItemSlot.None
                || !_equipment.TryGetValue(slot, out ItemInstance existingItem))
            {
                return;
            }

            ItemInstance inventoryItem = _playerInventory.GetItemAtSlot(inventorySlot.Value);
            ItemDefinition definition = inventoryItem?.BaseItem;
            if (inventoryItem == null
                || inventoryItem != itemInstance
                || inventoryItem.CurrentStackSize != 1
                || definition == null
                || definition.ValidSlots == null
                || !ContainsSlot(definition.ValidSlots, slot)
                || (existingItem != null
                    && string.Equals(existingItem.GUID, inventoryItem.GUID, StringComparison.Ordinal)))
            {
                return;
            }

            ItemInstance inventoryReplacement = existingItem;
            if (!_playerInventory.TrySetItemAtSlot(
                inventorySlot.Value,
                inventoryReplacement,
                true,
                out InventorySlotChange inventoryChange))
            {
                return;
            }

            _equipment[slot] = inventoryItem;
            var delta = new EquipmentDelta(new List<EquipmentSlotChange>
            {
                new EquipmentSlotChange(slot, EquipmentSlotChangeType.Set, itemInstance)
            });

            _playerInventory.SendDelta(new List<InventorySlotChange> { inventoryChange });
            CharacterEndpoint.onUpdateStatsRequest?.Invoke(_statContainer);
            NetworkingPerformanceMetrics.RecordOwnerPrivateTargetPublication();
            Client_UpdateEquipment(owner.Value, delta);

            GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

        /// <summary>Unequips an item into the inventory.</summary>
        public void UnequipItem(ItemSlot slot)
        {
            if (!Game.Shared.Runtime.Runtime.IsServer()
                || _playerInventory == null
                || !Enum.IsDefined(typeof(ItemSlot), slot)
                || slot == ItemSlot.None
                || !_equipment.TryGetValue(slot, out ItemInstance existingItem)
                || existingItem == null)
            {
                return;
            }

            int freeInventorySlot = -1;
            for (int slotIndex = 0; slotIndex < _playerInventory.MaxSlots; slotIndex++)
            {
                if (_playerInventory.GetItemAtSlot(slotIndex) == null)
                {
                    freeInventorySlot = slotIndex;
                    break;
                }
            }

            if (freeInventorySlot < 0)
                return;

            if (!_playerInventory.TrySetItemAtSlot(
                freeInventorySlot,
                existingItem,
                false,
                out InventorySlotChange inventoryChange))
            {
                return;
            }

            _equipment[slot] = null;
            var delta = new EquipmentDelta(new List<EquipmentSlotChange>
            {
                new EquipmentSlotChange(slot, EquipmentSlotChangeType.Removed)
            });

            _playerInventory.SendDelta(new List<InventorySlotChange> { inventoryChange });
            NetworkingPerformanceMetrics.RecordOwnerPrivateTargetPublication();
            Client_UpdateEquipment(owner.Value, delta);
            GetComponent<ActorSpellcaster>().RecalculateOverrides();
            CharacterEndpoint.onUpdateStatsRequest?.Invoke(_statContainer);
        }

        [ServerRpc]
        /// <summary>Requests that the server equip an inventory item.</summary>
        public void Server_RequestEquipItem(ItemSlot slot, int inventorySlot, RPCInfo rpcInfo = default)
        {
            var clientId = rpcInfo.sender;
            if (clientId != owner.Value)
                return;

            var itemInstance = _playerInventory.GetItemAtSlot(inventorySlot);
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

        private static bool ContainsSlot(IReadOnlyList<ItemSlot> validSlots, ItemSlot slot)
        {
            for (int index = 0; index < validSlots.Count; index++)
            {
                if (validSlots[index] == slot)
                    return true;
            }

            return false;
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
                        _equipment[change.Slot] = change.Item;
                        break;
                    case EquipmentSlotChangeType.Removed:
                        _equipment[change.Slot] = null;
                        break;
                }
            }

            CharacterEndpoint.onClientEquipmentUpdated?.Invoke();
        }
    }
}
