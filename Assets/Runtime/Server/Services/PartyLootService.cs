using System.Collections.Generic;
using System.Diagnostics;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Utility;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.Services
{
    /// <summary>
    /// Each instance of the party loot service tracks a pool of loot for a party on the server.
    /// The server compiles the loot and distributes it to each player.
    /// </summary>
    [ToDo("This is a very basic implementation that just gives all loot to each player. This will need to be reworked once the party system is implemented.")]
    public sealed class PartyLootService
    {
        // For now, we're only handling single players.
        private PlayerInventory playerInventory;
        private List<ItemInstance> lootPool = new List<ItemInstance>();

        public PartyLootService(PlayerActor playerActor)
        {
            this.playerInventory = playerActor.GetComponent<PlayerInventory>();
        }

        public void AddLootFromSource(IReadOnlyList<NPCLootTableEntry> source)
        {
            foreach (var entry in source)
            {
                var rolls = entry.Rolls;
                var table = entry.LootTable;

                Debug.Log($"Rolling loot table {table.DisplayName} for {rolls} rolls.");

                for (int i = 0; i < rolls; i++)
                {
                    Debug.Log($"Rolling loot table {table.DisplayName}, roll {i + 1} of {rolls}.");

                    // If an item was rolled, add it to the loot.
                    if (table.Roll(out ItemDefinition item))
                    {
                        AddToPool(item);
                        FormattedDebug.Log($"Loot table {table.DisplayName} rolled item: {item.DisplayName}");
                    }
                    else
                    {
                        FormattedDebug.Log($"Loot table {table.DisplayName} rolled with no item.");
                    }
                }
            }
        }

        public void AddToPool(ItemDefinition item)
        {
            if (item == null) return;

            // If item can stack, try to merge into an existing loot stack
            if (item.CanStack)
            {
                var existing = lootPool.Find(x => x.BaseItem != null &&
                                                  x.BaseItem.DefinitionId == item.DefinitionId &&
                                                  x.CurrentStackSize < item.MaxStackSize);
                if (existing != null)
                {
                    existing.CurrentStackSize += 1;
                    FormattedDebug.Log($"Adding {item.DisplayName} to loot pool.");
                    return;
                }
                else
                {
                    var newInstance = new ItemInstance(System.Guid.NewGuid().ToString("N"),
                                item.DefinitionId,
                                new List<string>(),
                                100,
                                1);
                    lootPool.Add(newInstance);
                    FormattedDebug.Log($"Adding {item.DisplayName} to loot pool.");

                }
            }
            else
            {
                var newInstance = new ItemInstance(System.Guid.NewGuid().ToString("N"),
                                item.DefinitionId,
                                new List<string>(),
                                100,
                                1);
                lootPool.Add(newInstance);
                FormattedDebug.Log($"Adding {item.DisplayName} to loot pool.");
            }
        }

        public void DistributeLoot()
        {
            var leftovers = new List<ItemInstance>();

            foreach (var template in lootPool)
            {
                if (template == null || template.BaseItem == null) continue;

                int amountToGive = template.CurrentStackSize;
                if (amountToGive <= 0) continue;

                playerInventory.AddItem(template, out int remainder, amountToGive);

                if (remainder > 0)
                {
                    var leftover = new ItemInstance(System.Guid.NewGuid().ToString("N"),
                                                     template.BaseItem.DefinitionId,
                                                     new List<string>(template.Modifications),
                                                     template.Durability,
                                                     remainder);
                    leftovers.Add(leftover);
                }
            }

            lootPool = leftovers;
        }
    }
}