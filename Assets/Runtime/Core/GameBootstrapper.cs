using Game.Shared.Benchmark;
using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Selects the client or headless-server startup path and bootstraps the corresponding runtime initialization sequence.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;

        [SerializeField] protected bool forceServerMode = false; // Editor testing toggle for server mode

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (!BenchmarkLaunchOptions.TryParseCurrentProcess(out var benchmarkOptions, out var benchmarkError))
            {
                Debug.LogError($"Invalid benchmark launch configuration: {benchmarkError}");
                Application.Quit();
                return;
            }

            if (benchmarkOptions != null)
            {
                try
                {
                    benchmarkOptions.WriteRunMetadata();
                    Game.Shared.Runtime.Runtime.SetBenchmarkRole(benchmarkOptions.Role == BenchmarkRole.Server);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to initialize benchmark launch: {exception.Message}");
                    Application.Quit();
                    return;
                }

                if (benchmarkOptions.Role == BenchmarkRole.Server)
                    Game.Server.Core.ServerInitialization.Begin(this, benchmarkOptions);
                else
                    Game.Client.ClientInitialization.Begin(this, benchmarkOptions);

                return;
            }

            // Detect execution context and route accordingly. This is unchanged for ordinary launches.
            if (IsHeadlessServer() || forceServerMode)
            {
                // Server mode: no rendering, no input—pure simulation
                Game.Server.Core.ServerInitialization.Begin(this);
            }
            else
            {
                // Client mode: rendering, input, UI enabled
                Game.Client.ClientInitialization.Begin(this);
            }
        }

        /// <summary>
        /// Determines if the game is running as a headless server.
        /// Headless servers are detected by:
        /// - Application.isBatchMode: -batchmode flag (dedicated server builds)
        /// - GraphicsDeviceType.Null: No graphics device available (alternative headless detection)
        ///
        /// This approach works for both standalone builds and editor testing.
        /// </summary>
        /// <returns>True if headless server mode, false for client mode</returns>
        private static bool IsHeadlessServer()
        {
            var isBatchMode = Application.isBatchMode;
            var hasNoGraphics = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            return isBatchMode || hasNoGraphics;
        }
        #endregion
    }
}
