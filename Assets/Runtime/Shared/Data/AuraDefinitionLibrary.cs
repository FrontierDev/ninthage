using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "AuraDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Aura Definition")]
    public class AuraDefinitionLibrary : DataDefinitionLibrary<AuraDefinition>
    {
        private static AuraDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static AuraDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AuraDefinitionLibrary>("AuraDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load AuraDefinitionLibrary from Resources");
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
