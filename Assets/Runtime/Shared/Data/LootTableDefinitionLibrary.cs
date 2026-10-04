using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all loot table definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "LootTableDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Loot Table")]
    public class LootTableDefinitionLibrary : DataDefinitionLibrary<LootTableDefinition>
    {
        private static LootTableDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static LootTableDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<LootTableDefinitionLibrary>("LootTableDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load LootTableDefinitionLibrary from Resources");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Reset singleton instance (for testing or reloading).
        /// </summary>
        public static void ResetInstance()
        {
            instance = null;
        }
    }
}
