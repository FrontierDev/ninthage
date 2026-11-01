using System;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all faction definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "FactionDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Faction")]
    public class FactionDefinitionLibrary : DataDefinitionLibrary<FactionDefinition>
    {
        private static FactionDefinitionLibrary _instance;

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static FactionDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "FactionDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(FactionDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("FactionDefinitionLibrary is already initialized.");

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
