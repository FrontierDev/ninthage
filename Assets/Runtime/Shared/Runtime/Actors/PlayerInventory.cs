using System.Collections.Generic;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public enum InventorySlotChangeType : byte
    {
        Set,
        Removed
    }

    public struct InventorySlotChange
    {
        public int SlotIndex;
        public InventorySlotChangeType ChangeType;
        public ItemInstance Item; // null when ChangeType == Removed

        public InventorySlotChange(int slotIndex, InventorySlotChangeType changeType, ItemInstance item = null)
        {
            SlotIndex = slotIndex;
            ChangeType = changeType;
            Item = item;
        }
    }

    public struct InventoryDelta
    {
        public List<InventorySlotChange> Changes;

        public InventoryDelta(List<InventorySlotChange> changes)
        {
            Changes = changes;
        }
    }

    public sealed class PlayerInventory : NetworkBehaviour
    {
        // Dictionary of int (slot index) to ItemInstance, representing the player's inventory slots
        [SerializeField] private readonly Dictionary<int, ItemInstance> inventory = new Dictionary<int, ItemInstance>();
        public IReadOnlyDictionary<int, ItemInstance> Inventory => inventory;

        private int maxSlots = 20; // Default max slots, can be modified by player progression or items
        public int MaxSlots => maxSlots;

        public void LoadInventory(CharacterData data)
        {
            inventory.Clear();
            foreach (var entry in data.SavedInventory)
            {
                var itemInstance = new ItemInstance(entry.ItemGuid,
                    entry.ItemId,
                    entry.Modifications,
                    entry.Durability, entry.StackSize);
                inventory[entry.SlotIndex] = itemInstance;
            }

            if (!Game.Shared.Runtime.IsServer())
                CharacterService.onClientInventoryUpdated?.Invoke();
        }

        public int GetRemainingSlots()
        {
            return maxSlots - inventory.Count;
        }

        public ItemInstance GetItemAtSlot(int slotIndex)
        {
            inventory.TryGetValue(slotIndex, out var item);
            return item;
        }

        /// <summary>
        /// Adds the given amount of an item to the inventory. Stacks onto existing compatible slots first,
        /// then fills empty slots. Outputs the remainder that could not be added (0 = all added).
        /// </summary>
        public bool AddItem(ItemInstance item, out int remainder, int amount = 1)
        {
            if (!Game.Shared.Runtime.IsServer() || amount <= 0)
            {
                remainder = amount;
                return false;
            }

            var definition = item.BaseItem;
            bool canStack = definition != null && definition.CanStack;
            int maxStack = definition != null ? definition.MaxStackSize : 1;
            int remaining = amount;
            var changes = new List<InventorySlotChange>();

            // First pass: stack onto existing slots with the same item
            if (canStack)
            {
                foreach (var kvp in inventory)
                {
                    if (remaining <= 0) break;
                    var existing = kvp.Value;
                    if (existing.BaseItem != definition) continue;

                    int space = maxStack - existing.CurrentStackSize;
                    if (space <= 0) continue;

                    int toAdd = remaining < space ? remaining : space;
                    existing.CurrentStackSize += toAdd;
                    remaining -= toAdd;
                    changes.Add(new InventorySlotChange(kvp.Key, InventorySlotChangeType.Set, existing));
                }
            }

            // Second pass: place into empty slots
            for (int i = 0; i < maxSlots && remaining > 0; i++)
            {
                if (inventory.ContainsKey(i)) continue;

                int stackAmount = canStack ? (remaining < maxStack ? remaining : maxStack) : 1;
                var newInstance = new ItemInstance(System.Guid.NewGuid().ToString("N"), definition.DefinitionId, new List<string>(item.Modifications), item.Durability, stackAmount);
                inventory[i] = newInstance;
                remaining -= stackAmount;
                changes.Add(new InventorySlotChange(i, InventorySlotChangeType.Set, newInstance));
            }

            if (changes.Count > 0)
                SendDelta(changes);

            remainder = remaining;
            return remaining <= 0;
        }

        /// <summary>
        /// Removes the given amount of items matching the specified definition from the inventory.
        /// Returns the number of items that could not be removed (0 = all removed).
        /// </summary>
        public int RemoveItem(string itemId, int amount = 1)
        {
            if (!Game.Shared.Runtime.IsServer() || amount <= 0)
                return amount;

            int remaining = amount;
            var slotsToRemove = new List<int>();
            var changes = new List<InventorySlotChange>();

            foreach (var kvp in inventory)
            {
                if (remaining <= 0) break;
                var existing = kvp.Value;
                if (existing.BaseItem == null || existing.BaseItem.DefinitionId != itemId) continue;

                if (existing.CurrentStackSize <= remaining)
                {
                    remaining -= existing.CurrentStackSize;
                    slotsToRemove.Add(kvp.Key);
                    changes.Add(new InventorySlotChange(kvp.Key, InventorySlotChangeType.Removed));
                }
                else
                {
                    existing.CurrentStackSize -= remaining;
                    remaining = 0;
                    changes.Add(new InventorySlotChange(kvp.Key, InventorySlotChangeType.Set, existing));
                }
            }

            foreach (int slot in slotsToRemove)
                inventory.Remove(slot);

            if (changes.Count > 0)
                SendDelta(changes);

            return remaining;
        }

        /// <summary>
        /// Removes the item from a specific slot, optionally only a partial amount from the stack.
        /// Returns true if the slot was found and the removal succeeded.
        /// </summary>
        public bool RemoveItemAtSlot(int slotIndex, int amount = 1)
        {
            if (!Game.Shared.Runtime.IsServer() || amount <= 0)
                return false;

            if (!inventory.TryGetValue(slotIndex, out var existing))
                return false;

            var changes = new List<InventorySlotChange>();

            if (existing.CurrentStackSize <= amount)
            {
                inventory.Remove(slotIndex);
                changes.Add(new InventorySlotChange(slotIndex, InventorySlotChangeType.Removed));
            }
            else
            {
                existing.CurrentStackSize -= amount;
                changes.Add(new InventorySlotChange(slotIndex, InventorySlotChangeType.Set, existing));
            }

            SendDelta(changes);
            return true;
        }

        /// <summary>
        /// Moves an item from one slot to another. If the target slot contains a compatible stackable item,
        /// the stacks are merged (respecting max stack size). Otherwise the items are swapped.
        /// Returns true if the move was performed.
        /// </summary>
        public bool MoveItem(int fromSlot, int toSlot)
        {
            if (!Game.Shared.Runtime.IsServer()) return false;
            if (fromSlot == toSlot) return false;
            if (toSlot < 0 || toSlot >= maxSlots) return false;
            if (!inventory.TryGetValue(fromSlot, out var fromItem)) return false;

            var changes = new List<InventorySlotChange>();

            if (inventory.TryGetValue(toSlot, out var toItem))
            {
                // If same stackable item, merge into target
                var fromDef = fromItem.BaseItem;
                var toDef = toItem.BaseItem;
                if (fromDef != null && fromDef == toDef && fromDef.CanStack &&
                    toItem.CurrentStackSize < fromDef.MaxStackSize)
                {
                    int space = fromDef.MaxStackSize - toItem.CurrentStackSize;
                    int toMove = fromItem.CurrentStackSize < space ? fromItem.CurrentStackSize : space;
                    toItem.CurrentStackSize += toMove;
                    changes.Add(new InventorySlotChange(toSlot, InventorySlotChangeType.Set, toItem));

                    if (fromItem.CurrentStackSize - toMove <= 0)
                    {
                        inventory.Remove(fromSlot);
                        changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Removed));
                    }
                    else
                    {
                        fromItem.CurrentStackSize -= toMove;
                        changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Set, fromItem));
                    }
                }
                else
                {
                    // Swap
                    inventory[fromSlot] = toItem;
                    inventory[toSlot] = fromItem;
                    changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Set, toItem));
                    changes.Add(new InventorySlotChange(toSlot, InventorySlotChangeType.Set, fromItem));
                }
            }
            else
            {
                // Target slot is empty — just move
                inventory.Remove(fromSlot);
                inventory[toSlot] = fromItem;
                changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Removed));
                changes.Add(new InventorySlotChange(toSlot, InventorySlotChangeType.Set, fromItem));
            }

            SendDelta(changes);
            return true;
        }

        /// <summary>
        /// Splits a stack by moving a specified amount from the source slot into an empty target slot.
        /// Returns true if the split was performed.
        /// </summary>
        public bool SplitStack(int fromSlot, int toSlot, int amount)
        {
            if (!Game.Shared.Runtime.IsServer()) return false;
            if (fromSlot == toSlot || amount <= 0) return false;
            if (toSlot < 0 || toSlot >= maxSlots) return false;
            if (!inventory.TryGetValue(fromSlot, out var fromItem)) return false;
            if (inventory.ContainsKey(toSlot)) return false; // Target must be empty
            if (fromItem.CurrentStackSize <= amount) return false; // Must leave at least 1 in source

            var definition = fromItem.BaseItem;
            if (definition == null || !definition.CanStack) return false;

            fromItem.CurrentStackSize -= amount;

            var newInstance = new ItemInstance(
                System.Guid.NewGuid().ToString("N"),
                definition.DefinitionId,
                new List<string>(fromItem.Modifications),
                fromItem.Durability,
                amount);
            inventory[toSlot] = newInstance;

            var changes = new List<InventorySlotChange>
            {
                new InventorySlotChange(fromSlot, InventorySlotChangeType.Set, fromItem),
                new InventorySlotChange(toSlot, InventorySlotChangeType.Set, newInstance)
            };
            SendDelta(changes);
            return true;
        }

        /// <summary>
        /// Merges the entire stack from the source slot into the target slot.
        /// Both slots must contain the same stackable item. Returns true if a merge occurred.
        /// Any overflow remains in the source slot.
        /// </summary>
        public bool MergeStacks(int fromSlot, int toSlot)
        {
            if (!Game.Shared.Runtime.IsServer()) return false;
            if (fromSlot == toSlot) return false;
            if (!inventory.TryGetValue(fromSlot, out var fromItem)) return false;
            if (!inventory.TryGetValue(toSlot, out var toItem)) return false;

            var definition = fromItem.BaseItem;
            if (definition == null || !definition.CanStack) return false;
            if (toItem.BaseItem != definition) return false;
            if (toItem.CurrentStackSize >= definition.MaxStackSize) return false;

            int space = definition.MaxStackSize - toItem.CurrentStackSize;
            int toMove = fromItem.CurrentStackSize < space ? fromItem.CurrentStackSize : space;
            toItem.CurrentStackSize += toMove;

            var changes = new List<InventorySlotChange>();
            changes.Add(new InventorySlotChange(toSlot, InventorySlotChangeType.Set, toItem));

            if (fromItem.CurrentStackSize - toMove <= 0)
            {
                inventory.Remove(fromSlot);
                changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Removed));
            }
            else
            {
                fromItem.CurrentStackSize -= toMove;
                changes.Add(new InventorySlotChange(fromSlot, InventorySlotChangeType.Set, fromItem));
            }

            SendDelta(changes);
            return true;
        }

        private void SendDelta(List<InventorySlotChange> changes)
        {
            if (!owner.HasValue) return;
            Client_UpdateInventory(owner.Value, new InventoryDelta(changes));
        }

        [ServerRpc]
        public void Server_RequestMoveItem(int fromSlot, int toSlot, RPCInfo rpcInfo = default)
        {
            if (rpcInfo.sender != owner) return; // Only allow the owning player to request moves
            MoveItem(fromSlot, toSlot);
        }

        [ServerRpc]
        public void Server_RequestSplitStack(int fromSlot, int toSlot, int amount, RPCInfo rpcInfo = default)
        {
            if (rpcInfo.sender != owner) return; // Only allow the owning player to request moves
            SplitStack(fromSlot, toSlot, amount);
        }

        [ServerRpc]
        public void Server_RequestMergeStacks(int fromSlot, int toSlot, RPCInfo rpcInfo)
        {
            if (rpcInfo.sender != owner) return; // Only allow the owning player to request moves
            MergeStacks(fromSlot, toSlot);
        }

        [TargetRpc]
        public void Client_UpdateInventory(PlayerID target, InventoryDelta delta, RPCInfo rpcInfo = default)
        {
            foreach (var change in delta.Changes)
            {
                switch (change.ChangeType)
                {
                    case InventorySlotChangeType.Set:
                        inventory[change.SlotIndex] = change.Item;
                        break;
                    case InventorySlotChangeType.Removed:
                        inventory.Remove(change.SlotIndex);
                        break;
                }
            }

            CharacterService.onClientInventoryUpdated?.Invoke();
        }
    }
}