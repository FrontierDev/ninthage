using System;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Publishes and exposes the shared game configuration.</summary>
    public static class GameConfigurationManager
    {
        private static GameConfiguration _config;

        /// <summary>Gets the loaded game configuration.</summary>
        public static GameConfiguration Config
        {
            get
            {
                if (_config == null)
                    throw new InvalidOperationException(
                        "Game configuration was accessed before GameContentBootstrap initialization.");

                return _config;
            }
        }

        private static bool _initialized;

        /// <summary>Gets whether configuration initialization has completed.</summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Publishes a previously loaded game configuration after validating it.
        /// </summary>
        /// <param name="config">The configuration loaded by <see cref="GameContentBootstrap"/>.</param>
        public static void Initialize(GameConfiguration config)
        {
            if (_initialized)
                throw new InvalidOperationException("Game configuration is already initialized.");
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.ValidateWorldGridConfiguration();
            config.ValidateSpatialConfiguration();
            config.ValidateSessionLifecycleConfiguration();

            _config = config;
            _initialized = true;
        }

        /// <summary>Clears the published configuration during controlled runtime teardown.</summary>
        internal static void Reset()
        {
            _initialized = false;
            _config = null;
        }
    }
}
