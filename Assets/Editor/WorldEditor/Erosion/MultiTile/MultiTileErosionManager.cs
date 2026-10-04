using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Manages hydraulic erosion across multiple terrain tiles with border synchronization.
    /// Uses EDEN hydraulics with proper tile-by-tile processing and border blending.
    /// </summary>
    public static class MultiTileErosionManager
    {
        /// <summary>
        /// Applies hydraulic erosion to a grid of terrain chunks with continuous water flow across boundaries.
        /// </summary>
        public static void ApplyHydraulicErosion(
            List<Terrain> terrains,
            int gridWidth,
            int gridHeight,
            int iterations,
            float inertia = 0.05f,
            float sedimentCapacityFactor = 4f,
            float minSedimentCapacity = 0.01f,
            float erodeSpeed = 0.3f,
            float depositSpeed = 0.3f,
            float evaporateSpeed = 0.01f,
            float gravity = 4f,
            int maxDropletLifetime = 30,
            ComputeShader hydraulicShader = null)
        {
            if (terrains == null || terrains.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No terrains selected for erosion.", "OK");
                return;
            }

            if (hydraulicShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Hydraulic Erosion Compute Shader not assigned.", "OK");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Multi-Tile Erosion", "Applying EDEN hydraulic erosion...", 0f);

                // Apply EDEN erosion to the terrain grid
                MultiTileHydraulicErosion.ErodeTerrainGrid(
                    terrains,
                    gridWidth,
                    gridHeight,
                    hydraulicShader,
                    iterations,
                    inertia,
                    sedimentCapacityFactor,
                    minSedimentCapacity,
                    erodeSpeed,
                    depositSpeed,
                    evaporateSpeed,
                    gravity,
                    maxDropletLifetime);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Success", "Multi-tile hydraulic erosion completed successfully.", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Multi-tile erosion failed: {ex.Message}\n\n{ex.StackTrace}", "OK");
                Debug.LogException(ex);
            }
        }
    }
}
