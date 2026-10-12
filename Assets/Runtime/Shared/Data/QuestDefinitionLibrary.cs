using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all quest definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Quest")]
    public class QuestDefinitionLibrary : DataDefinitionLibrary<QuestDefinition>
    {
        private static QuestDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static QuestDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<QuestDefinitionLibrary>("QuestDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load QuestDefinitionLibrary from Resources");
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
