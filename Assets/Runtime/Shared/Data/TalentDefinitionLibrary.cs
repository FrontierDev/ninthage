using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all talent definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "TalentDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Talent Definition")]
    public class TalentDefinitionLibrary : DataDefinitionLibrary<TalentDefinition>
    {
        private static TalentDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static TalentDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TalentDefinitionLibrary>("TalentDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load TalentDefinitionLibrary from Resources");
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
