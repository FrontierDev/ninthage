using UnityEngine;
using GapperGames;

namespace Game.Editor.WorldEditor
{
    public static class Hydraulics
    {

        public static float[,] Erode(ComputeShader erosion, int numIterations, float[,] heightmap, int heightmapResolution, int erosionResolution)
        {
            // Delegate GPU computation to EDEN_Hydraulics for proper async handling
            // Multi-tile handling is managed by MultiTileHydraulicErosion
            return EDEN_Hydraulics.Erode(erosion, numIterations, heightmap, heightmapResolution, erosionResolution);
        }
    }
}
