using UnityEngine;
using System.Collections;
using Unity.Entities;

namespace Game.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper instance;

        [SerializeField] protected bool forceServerMode = false; // Editor testing toggle for server mode

        #region Lifecycle
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Detect execution context and route accordingly
            if (IsHeadlessServer() || forceServerMode)
            {
                // Server mode: no rendering, no input—pure simulation
                Game.Server.ServerInitialization.Begin(this);
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
            bool isBatchMode = Application.isBatchMode;
            bool hasNoGraphics = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            return isBatchMode || hasNoGraphics;
        }
        #endregion
    }
}