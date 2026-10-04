using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all quest definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Quest")]
    public class QuestDefinitionLibrary : DataDefinitionLibrary<QuestDefinition>
    {
        private static QuestDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static QuestDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<QuestDefinitionLibrary>("QuestDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load QuestDefinitionLibrary from Resources");
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
