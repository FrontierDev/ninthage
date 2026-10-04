using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all race definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "SpellDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Spell")]
    public class SpellDefinitionLibrary : DataDefinitionLibrary<SpellDefinition>
    {
        private static SpellDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static SpellDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<SpellDefinitionLibrary>("SpellDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load SpellDefinitionLibrary from Resources");
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
