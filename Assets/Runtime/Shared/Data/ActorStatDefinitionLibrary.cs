using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ActorStatDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Actor Stat")]
    public class ActorStatDefinitionLibrary : DataDefinitionLibrary<ActorStatDefinition>
    {
        private static ActorStatDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ActorStatDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ActorStatDefinitionLibrary>("ActorStatDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load ActorStatDefinitionLibrary from Resources");
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
