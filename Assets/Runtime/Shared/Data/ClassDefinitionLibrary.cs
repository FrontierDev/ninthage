using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ClassDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Class")]
    public class ClassDefinitionLibrary : DataDefinitionLibrary<ClassDefinition>
    {
        private static ClassDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ClassDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ClassDefinitionLibrary>("ClassDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load ClassDefinitionLibrary from Resources");
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
