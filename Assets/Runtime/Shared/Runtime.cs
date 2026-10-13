using System;
using UnityEngine;

namespace Game.Shared.Runtime
{
    /// <summary>Provides shared runtime environment checks.</summary>
    public static class Runtime
    {
        private static bool? _benchmarkServerMode;

        /// <summary>Gets whether this process has an explicit benchmark role.</summary>
        public static bool IsBenchmarkMode => _benchmarkServerMode.HasValue;

        /// <summary>Gets whether this process is the non-rendered benchmark client.</summary>
        public static bool IsBenchmarkClient => _benchmarkServerMode.HasValue && !_benchmarkServerMode.Value;

        /// <summary>Sets the role override used only by an explicitly configured benchmark process.</summary>
        public static void SetBenchmarkRole(bool isServer)
        {
            if (_benchmarkServerMode.HasValue && _benchmarkServerMode.Value != isServer)
                throw new InvalidOperationException("The benchmark runtime role cannot be changed after startup.");

            _benchmarkServerMode = isServer;
        }

        /// <summary>Gets whether the process is running as a headless server.</summary>
        public static bool IsServer()
        {
            if (_benchmarkServerMode.HasValue)
                return _benchmarkServerMode.Value;

            bool isBatchMode = Application.isBatchMode;
            bool hasNoGraphics = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            return isBatchMode || hasNoGraphics;
        }
    }
}
