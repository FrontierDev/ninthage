using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "AuraDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Aura Definition")]
    public class AuraDefinitionLibrary : DataDefinitionLibrary<AuraDefinition>
    {
        private static AuraDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static AuraDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AuraDefinitionLibrary>("AuraDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load AuraDefinitionLibrary from Resources");
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
