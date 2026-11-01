using System;
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

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static DamageSchoolDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "DamageSchoolDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(DamageSchoolDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("DamageSchoolDefinitionLibrary is already initialized.");

            _instance = library;
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
