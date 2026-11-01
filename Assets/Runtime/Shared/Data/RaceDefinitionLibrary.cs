using System;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all race definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "RaceDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Race")]
    public class RaceDefinitionLibrary : DataDefinitionLibrary<RaceDefinition>
    {
        private static RaceDefinitionLibrary _instance;

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static RaceDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "RaceDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(RaceDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("RaceDefinitionLibrary is already initialized.");

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
