using System;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Central library of all loot table definitions in the game.
    /// Accessed by both client (UI, character creation) and server (validation).
    /// </summary>
    [CreateAssetMenu(fileName = "LootTableDefinitionLibrary", menuName = "NinthAge/Definition Libraries/Loot Table")]
    public class LootTableDefinitionLibrary : DataDefinitionLibrary<LootTableDefinition>
    {
        private static LootTableDefinitionLibrary _instance;

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static LootTableDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "LootTableDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(LootTableDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("LootTableDefinitionLibrary is already initialized.");

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
