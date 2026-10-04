using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "DamageSchoolDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Damage School")]
    public class DamageSchoolDefinitionLibrary : DataDefinitionLibrary<DamageSchoolDefinition>
    {
        private static DamageSchoolDefinitionLibrary instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static DamageSchoolDefinitionLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<DamageSchoolDefinitionLibrary>("DamageSchoolDefinitionLibrary");
                    if (instance == null)
                    {
                        Debug.Error("Could not load DamageSchoolDefinitionLibrary from Resources");
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
