using System;
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

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static TalentDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "TalentDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(TalentDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("TalentDefinitionLibrary is already initialized.");

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
