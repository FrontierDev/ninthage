using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all race definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "SpellDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Spell")]
    public class SpellDefinitionLibrary : DataDefinitionLibrary<SpellDefinition>
    {
        private static SpellDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static SpellDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SpellDefinitionLibrary>("SpellDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load SpellDefinitionLibrary from Resources");
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
