using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all loot table definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "LootTableDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Loot Table")]
    public class LootTableDefinitionLibrary : DataDefinitionLibrary<LootTableDefinition>
    {
        private static LootTableDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static LootTableDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LootTableDefinitionLibrary>("LootTableDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load LootTableDefinitionLibrary from Resources");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Reset singleton instance (for testing or reloading).
        /// </summary>
        public static void ResetInstance()
        {
            _instance = null;
        }
    }
}
