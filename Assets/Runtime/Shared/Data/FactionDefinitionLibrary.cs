using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all faction definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "FactionDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Faction")]
    public class FactionDefinitionLibrary : DataDefinitionLibrary<FactionDefinition>
    {
        private static FactionDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static FactionDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<FactionDefinitionLibrary>("FactionDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load FactionDefinitionLibrary from Resources");
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
