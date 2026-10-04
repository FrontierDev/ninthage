using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Item Definition")]
    public class ItemDefinitionLibrary : DataDefinitionLibrary<ItemDefinition>
    {
        private static ItemDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ItemDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ItemDefinitionLibrary>("ItemDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load ItemDefinitionLibrary from Resources");
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
