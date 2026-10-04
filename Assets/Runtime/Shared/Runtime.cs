using UnityEngine;

namespace Game.Shared
{
    public static class Runtime
    {
        public static bool IsServer()
        {
            bool isBatchMode = Application.isBatchMode;
            bool hasNoGraphics = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            return isBatchMode || hasNoGraphics;
        }
    }
}