using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all faction definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "FactionDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Faction")]
    public class FactionDefinitionLibrary : DataDefinitionLibrary<FactionDefinition>
    {
        private static FactionDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static FactionDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<FactionDefinitionLibrary>("FactionDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load FactionDefinitionLibrary from Resources");
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
