using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "ActorStatDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Actor Stat")]
    public class ActorStatDefinitionLibrary : DataDefinitionLibrary<ActorStatDefinition>
    {
        private static ActorStatDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static ActorStatDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ActorStatDefinitionLibrary>("ActorStatDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load ActorStatDefinitionLibrary from Resources");
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
