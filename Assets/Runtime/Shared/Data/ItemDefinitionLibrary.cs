using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Item Definition")]
    public class ItemDefinitionLibrary : DataDefinitionLibrary<ItemDefinition>
    {
        private static ItemDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ItemDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ItemDefinitionLibrary>("ItemDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load ItemDefinitionLibrary from Resources");
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
