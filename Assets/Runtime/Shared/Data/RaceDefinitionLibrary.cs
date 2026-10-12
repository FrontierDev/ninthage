using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all race definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "RaceDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Race")]
    public class RaceDefinitionLibrary : DataDefinitionLibrary<RaceDefinition>
    {
        private static RaceDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static RaceDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<RaceDefinitionLibrary>("RaceDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load RaceDefinitionLibrary from Resources");
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
