using System.Collections;
using Game.Shared.Runtime;
using Unity.Entities;
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
        [SerializeField] private string editorServerWorldName = string.Empty; // Explicit world identity for forced Editor/test server mode

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

            var headlessServer = IsHeadlessServer();
            Runtime.Initialize(ResolveRuntimeRole(headlessServer, forceServerMode));

            if (headlessServer)
            {
                // Server mode: no rendering, no input—pure simulation
                Game.Server.Core.ServerInitialization.Begin(this);
            }
            else if (forceServerMode)
            {
                // Editor/test server mode supplies its world identity through the serialized test bootstrap.
                Game.Server.Core.ServerInitialization.Begin(this, editorServerWorldName);
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

        private static RuntimeRole ResolveRuntimeRole(bool headlessServer, bool forceServerMode)
        {
            return headlessServer || forceServerMode
                ? RuntimeRole.Server
                : RuntimeRole.Client;
        }
        #endregion
    }
}
