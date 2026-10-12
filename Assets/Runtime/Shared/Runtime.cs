using UnityEngine;

namespace Game.Shared
{
    /// <summary>Provides shared runtime environment checks.</summary>
    public static class Runtime
    {
        /// <summary>Gets whether the process is running as a headless server.</summary>
        public static bool IsServer()
        {
            bool isBatchMode = Application.isBatchMode;
            bool hasNoGraphics = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            return isBatchMode || hasNoGraphics;
        }
    }
}
