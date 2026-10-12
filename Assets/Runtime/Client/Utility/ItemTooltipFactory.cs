using Game.Client.UI;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Items;
using System.Collections.Generic;

namespace Game.Client.Utility
{
    /// <summary>
    /// Provides the client-side <c>ItemTooltipFactory</c> implementation.
    /// </summary>
    public static class ItemTooltipFactory
    {
        private static Dictionary<ItemInstance, UI_TooltipLine[]> _cache = new();

        /// <summary>
        /// Gets the value provided by <c>GetOrCreateTooltip</c>.
        /// </summary>
        public static UI_TooltipLine[] GetOrCreateTooltip(ItemInstance item)
        {
            if (_cache.TryGetValue(item, out var tooltip))
            {
                return tooltip;
            }
            else
            {
                var newTooltip = CreateTooltip(item);
                _cache[item] = newTooltip;
                return newTooltip;
            }
        }

        private static UI_TooltipLine[] CreateTooltip(ItemInstance item)
        {
            var lines = new List<UI_TooltipLine>();

            // Add stats
            switch (item.BaseItem.ItemType)
            {
                case ItemType.Weapon:
                    lines.AddRange(CreateWeaponLines(item));
                    break;
                    // Add cases for other item types as needed
            }
            return lines.ToArray();
        }

        private static UI_TooltipLine[] CreateWeaponLines(ItemInstance item)
        {
            var lines = new List<UI_TooltipLine>();

            // Add damage line
            var damageLine = new UI_TooltipLine()
            {
                text = new string[]
                {
                    $"{item.BaseItem.DamagePerSecond} {item.BaseItem.DamageSchool.DisplayName} per second",
                    $"({item.BaseItem.SwingTimer.ToString("F1")}s)"
                }
            };

            lines.Add(damageLine);
            return lines.ToArray();
        }

        private static UI_TooltipLine[] CreateArmorLines(ItemInstance item)
        {
            var lines = new List<UI_TooltipLine>();

            // Add armor line
            var armorLine = new UI_TooltipLine()
            {
                text = new string[]
                {
                    $"{item.BaseItem.ArmorWeight} Armour",
                }
            };

            lines.Add(armorLine);
            return lines.ToArray();
        }

        private static UI_TooltipLine[] CreateStatLines(ItemInstance item)
        {
            var lines = new List<UI_TooltipLine>();

            foreach (var stat in item.BaseItem.Stats)
            {
                var plusString = stat.Value > 0 ? "+" : "";
                if (stat.Value < 0)
                    plusString = "-"; // Don't show plus for negative stats
                else
                    plusString = "+";

                var statLine = new UI_TooltipLine()
                { text = new string[] { $"{plusString}{stat.Value} {stat.Stat.DisplayName}" } };
                lines.Add(statLine);
            }

            return lines.ToArray();
        }
    }
}
