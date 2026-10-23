using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Shared
{
    /// <summary>Loads and exposes the shared game configuration.</summary>
    public static class GameConfigurationManager
    {
        private static GameConfiguration _config;
        public static GameConfiguration Config
        {
            get
            {
                if (_config == null)
                {
                    Debug.Error("Game configuration accessed before initialization!");
                }
                return _config;
            }
        }

        private static bool _initialized = false;
        /// <summary>Gets whether configuration initialization has completed.</summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize the GameBalanceManager by loading config from Addressables.
        /// Uses reflection to load Addressables without requiring direct assembly reference.
        /// Loads synchronously via WaitForCompletion() to avoid type reference issues.
        /// </summary>
        public static void Initialize()
        {
            // Check for duplicate.
            if (_initialized)
            {
                Debug.Warning("Attempt to initialize game configuration but it isalready initialized.");
                return;
            }

            // Load the configuration asset from Resources.
            _config = Resources.Load<GameConfiguration>("GameConfiguration");
            if (_config == null)
            {
                Debug.Error("Failed to load game configuration from Resources. Ensure the asset is placed in a Resources folder and named 'GameConfiguration'.");
                return;
            }

            _config.ValidateWorldGridConfiguration();

            // Marked as initialized.
            _initialized = true;
        }
    }
}
