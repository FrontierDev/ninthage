using System;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Loads and exposes the authored world-location definitions.</summary>
    [CreateAssetMenu(
        fileName = "WorldLocationDefinitionLibrary",
        menuName = "NinthAge/Definition Libraries/World Location")]
    public sealed class WorldLocationDefinitionLibrary : DataDefinitionLibrary<WorldLocationDefinition>
    {
        private static WorldLocationDefinitionLibrary _instance;

        /// <summary>Gets the library published by <see cref="GameContentBootstrap"/>.</summary>
        public static WorldLocationDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException(
                        "WorldLocationDefinitionLibrary was accessed before GameContentBootstrap initialization.");

                return _instance;
            }
        }

        internal static void Initialize(WorldLocationDefinitionLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));
            if (_instance != null)
                throw new InvalidOperationException("WorldLocationDefinitionLibrary is already initialized.");

            _instance = library;
        }

        /// <summary>Resets the singleton instance for controlled reloads.</summary>
        public static void ResetInstance()
        {
            _instance = null;
        }
    }
}
