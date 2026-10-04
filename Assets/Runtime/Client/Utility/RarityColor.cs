using Game.Shared;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Client.Utility
{
    public static class RarityColor
    {
        public static Color GetColor(ItemQuality rarity)
        {
            switch (rarity)
            {
                case ItemQuality.Poor: return GameConfigurationManager.Config.PoorColor; // Poor (gray)
                case ItemQuality.Common: return GameConfigurationManager.Config.CommonColor; // Common
                case ItemQuality.Uncommon: return GameConfigurationManager.Config.UncommonColor; // Uncommon
                case ItemQuality.Rare: return GameConfigurationManager.Config.RareColor;  // Rare
                case ItemQuality.Epic: return GameConfigurationManager.Config.EpicColor; // Epic
                case ItemQuality.Legendary: return GameConfigurationManager.Config.LegendaryColor; // Legendary (orange)
                default: return Color.clear; // Unknown rarity
            }
        }
    }
}