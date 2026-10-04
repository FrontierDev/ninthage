using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all talent definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "TalentDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Talent Definition")]
    public class TalentDefinitionLibrary : DataDefinitionLibrary<TalentDefinition>
    {
        private static TalentDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static TalentDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<TalentDefinitionLibrary>("TalentDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load TalentDefinitionLibrary from Resources");
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
