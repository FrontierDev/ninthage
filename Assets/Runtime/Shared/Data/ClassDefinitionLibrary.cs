using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ClassDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Class")]
    public class ClassDefinitionLibrary : DataDefinitionLibrary<ClassDefinition>
    {
        private static ClassDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ClassDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ClassDefinitionLibrary>("ClassDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load ClassDefinitionLibrary from Resources");
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
