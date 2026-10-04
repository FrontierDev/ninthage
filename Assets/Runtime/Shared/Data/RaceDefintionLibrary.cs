using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all race definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "RaceDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Race")]
    public class RaceDefinitionLibrary : DataDefinitionLibrary<RaceDefinition>
    {
        private static RaceDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static RaceDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<RaceDefinitionLibrary>("RaceDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load RaceDefinitionLibrary from Resources");
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
