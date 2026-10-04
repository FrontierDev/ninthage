using Debug = Game.Shared.FormattedDebug;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Shared
{
    public static class GameConfigurationManager
    {
        private static GameConfiguration config;
        public static GameConfiguration Config
        {
            get
            {
                if (config == null)
                {
                    Debug.Error("Game configuration accessed before initialization!");
                }
                return config;
            }
        }

        private static bool _initialized = false;
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
            config = Resources.Load<GameConfiguration>("GameConfiguration");
            if (config == null)
            {
                Debug.Error("Failed to load game configuration from Resources. Ensure the asset is placed in a Resources folder and named 'GameConfiguration'.");
                return;
            }

            // Marked as initialized.
            _initialized = true;
        }
    }
}