using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all class definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "DamageSchoolDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Damage School")]
    public class DamageSchoolDefinitionLibrary : DataDefinitionLibrary<DamageSchoolDefinition>
    {
        private static DamageSchoolDefinitionLibrary _instance;

        /// <summary>
        /// Get the singleton instance (loaded from Resources).
        /// </summary>
        public static DamageSchoolDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DamageSchoolDefinitionLibrary>("DamageSchoolDefinitionLibrary");
                    if (_instance == null)
                    {
                        Debug.Error("Could not load DamageSchoolDefinitionLibrary from Resources");
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
