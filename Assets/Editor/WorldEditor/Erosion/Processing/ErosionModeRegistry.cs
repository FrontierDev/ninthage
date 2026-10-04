using System;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Registry for discovering and managing all available erosion modes.
    /// Uses reflection to automatically find all IErosionMode implementations.
    /// </summary>
    public static class ErosionModeRegistry
    {
        private static List<IErosionMode> _cachedModes;

        /// <summary>
        /// Gets all available erosion modes.
        /// Cached after first call for performance.
        /// </summary>
        public static List<IErosionMode> GetAvailableModes()
        {
            if (_cachedModes != null)
                return _cachedModes;

            _cachedModes = new List<IErosionMode>();

            // Discover all types that implement IErosionMode
            Assembly assembly = typeof(IErosionMode).Assembly;
            Type[] types = assembly.GetTypes();

            foreach (var type in types)
            {
                // Check if type implements IErosionMode and is not abstract
                if (typeof(IErosionMode).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsInterface)
                {
                    try
                    {
                        // Instantiate the mode
                        IErosionMode mode = (IErosionMode)Activator.CreateInstance(type);
                        _cachedModes.Add(mode);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Failed to instantiate erosion mode {type.Name}: {ex.Message}");
                    }
                }
            }

            // Sort by name for consistent ordering
            _cachedModes.Sort((a, b) => string.Compare(a.ModeName, b.ModeName, StringComparison.Ordinal));

            return _cachedModes;
        }

        /// <summary>
        /// Gets a specific erosion mode by name.
        /// Returns null if not found.
        /// </summary>
        public static IErosionMode GetModeByName(string modeName)
        {
            foreach (var mode in GetAvailableModes())
            {
                if (mode.ModeName == modeName)
                    return mode;
            }

            return null;
        }

        /// <summary>
        /// Clears the cached modes list.
        /// Call this if new modes are dynamically loaded.
        /// </summary>
        public static void ClearCache()
        {
            _cachedModes = null;
        }
    }
}
