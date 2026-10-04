using UnityEngine;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Multi-tile hydraulic erosion using a 3x3 local grid approach via LocalErosionWindow.
    /// For large-scale multi-tile erosion, use the LocalErosionWindow editor window per tile.
    /// </summary>
    public static class MultiTileHydraulicErosion
    {
        /// <summary>
        /// Applies erosion to a 3x3 tile grid with the centerTerrain at the center.
        /// This is the entry point for multi-tile erosion—use LocalErosionWindow for the UI.
        /// </summary>
        public static void ErodeTerrainGrid(
            List<Terrain> terrains,
            int gridWidth,
            int gridHeight,
            ComputeShader hydraulicShader,
            int iterations,
            float inertia = 0.05f,
            float sedimentCapacityFactor = 4f,
            float minSedimentCapacity = 0.01f,
            float erodeSpeed = 0.3f,
            float depositSpeed = 0.3f,
            float evaporateSpeed = 0.01f,
            float gravity = 4f,
            int maxDropletLifetime = 30)
        {
            if (terrains == null || terrains.Count == 0)
                return;

            // Use LocalTileErosion for the actual multi-tile implementation
            if (terrains.Count > 0)
            {
                LocalTileErosion.ErodeLocalGrid(
                    terrains,
                    terrains[0],
                    ErosionMode.Hydraulic,
                    hydraulicShader,
                    null,  // smoothSharpenShader not needed for Hydraulic mode
                    iterations,
                    256,
                    3,
                    // Default wind parameters (unused for hydraulic)
                    0.5f,
                    new Vector2(1, 2),
                    257,
                    // Default terrace parameters (unused for hydraulic)
                    15,
                    20,
                    // Default smooth parameters (unused for hydraulic)
                    2,
                    // Default sharpen parameters (unused for hydraulic)
                    5,
                    10,
                    0.7f);
            }
        }
    }
}
