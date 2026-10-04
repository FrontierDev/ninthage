using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Hydraulic erosion: water flow simulation.
    /// Erodes valleys and creates natural-looking water channels.
    /// Computationally heavier than thermal erosion.
    /// </summary>
    public class HydraulicErosion : IErosionMode
    {
        /// <summary>
        /// Water evaporation rate per iteration.
        /// </summary>
        private const float EVAPORATION_RATE = 0.05f;

        /// <summary>
        /// Minimum water level to carry sediment.
        /// </summary>
        private const float MIN_WATER_LEVEL = 0.0001f;

        /// <summary>
        /// Sediment carrying capacity multiplier (higher = more sediment transport).
        /// </summary>
        private const float SEDIMENT_CAPACITY = 0.8f;

        public string ModeName => "Hydraulic Erosion";
        public string Description => "Water flow simulation that creates realistic valleys and channels.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.7f,
                iterations = 30,
                strength = 0.15f,
                radius = 2f,
                randomSeed = 42
            };
        }

        public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)
        {
            if (heightmap == null || heightmap.Length != width * height)
                throw new ArgumentException("Invalid heightmap");

            float[] result = new float[heightmap.Length];
            Array.Copy(heightmap, result, heightmap.Length);

            int iterations = Math.Max(1, settings.iterations);
            float strength = Mathf.Clamp01(settings.strength);
            float seaLevel = Mathf.Clamp01(settings.seaLevel);

            // Initialize water and sediment maps
            float[] waterMap = new float[width * height];
            float[] sedimentMap = new float[width * height];

            // Simulate water flow and erosion (only above sea level)
            for (int iter = 0; iter < iterations; iter++)
            {
                // Reset sediment each iteration
                Array.Clear(sedimentMap, 0, sedimentMap.Length);

                // Add rain (water source) - only above sea level
                for (int i = 0; i < waterMap.Length; i++)
                {
                    if (result[i] >= seaLevel)
                        waterMap[i] += strength * 0.05f; // Increased rain amount for more water
                }

                HydraulicPass(result, waterMap, sedimentMap, width, height, strength, seaLevel);

                // Evaporate water more gradually
                for (int i = 0; i < waterMap.Length; i++)
                {
                    waterMap[i] *= (1f - EVAPORATION_RATE);
                }
            }

            return result;
        }

        private void HydraulicPass(float[] heightmap, float[] waterMap, float[] sedimentMap, int width, int height, float strength, float seaLevel)
        {
            // Multi-pass flow: water flows downslope and erodes
            for (int pass = 0; pass < 3; pass++) // Multiple passes per iteration for stronger effect
            {
                float[] tempHeightmap = new float[heightmap.Length];
                Array.Copy(heightmap, tempHeightmap, heightmap.Length);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = y * width + x;

                        if (waterMap[idx] < MIN_WATER_LEVEL)
                            continue;

                        float currentHeight = tempHeightmap[idx];

                        // Skip erosion below sea level
                        if (currentHeight < seaLevel)
                            continue;

                        float currentWater = waterMap[idx];

                        // Find steepest downslope neighbor
                        int steepestIdx = -1;
                        float maxSlope = 0f;

                        int[] neighbors = GetNeighbors(x, y, width, height);
                        foreach (int neighborIdx in neighbors)
                        {
                            if (neighborIdx >= 0)
                            {
                                float neighborHeight = tempHeightmap[neighborIdx];
                                float slope = currentHeight - neighborHeight;
                                if (slope > maxSlope)
                                {
                                    maxSlope = slope;
                                    steepestIdx = neighborIdx;
                                }
                            }
                        }

                        // If found a downslope neighbor, flow and erode
                        if (steepestIdx >= 0 && maxSlope > 0.001f)
                        {
                            // Stronger erosion: water cuts deeper
                            float waterFlow = currentWater * maxSlope;
                            float transportCapacity = waterFlow * SEDIMENT_CAPACITY * strength * 0.01f; // Much stronger erosion

                            // Erode current cell based on available sediment capacity
                            float erosionAmount = Mathf.Min(transportCapacity * 2f, tempHeightmap[idx] - seaLevel);
                            if (erosionAmount > 0f)
                            {
                                tempHeightmap[idx] -= erosionAmount;
                                sedimentMap[idx] += erosionAmount;
                            }

                            // Transfer water downslope (most of it, not just 50%)
                            float waterTransfer = Mathf.Min(waterFlow * 0.8f, currentWater); // Transfer 80% of water
                            waterMap[idx] -= waterTransfer;
                            waterMap[steepestIdx] += waterTransfer;

                            // Transfer sediment based on water capacity
                            float sedimentTransfer = Mathf.Min(sedimentMap[idx], transportCapacity);
                            sedimentMap[idx] -= sedimentTransfer;
                            sedimentMap[steepestIdx] += sedimentTransfer;
                        }
                    }
                }

                // Deposit sediment gradually along flow path (not just at endpoints)
                for (int i = 0; i < sedimentMap.Length; i++)
                {
                    if (sedimentMap[i] > 0f)
                    {
                        // Deposit based on water velocity (slower water deposits more)
                        float depositRate = 0.3f * (1f - Mathf.Clamp01(waterMap[i]));
                        float depositAmount = sedimentMap[i] * depositRate;
                        heightmap[i] += depositAmount;
                        sedimentMap[i] -= depositAmount;
                    }
                }

                Array.Copy(tempHeightmap, heightmap, heightmap.Length);
            }
        }

        private int[] GetNeighbors(int x, int y, int width, int height)
        {
            int[] neighbors = new int[8];
            int idx = 0;

            // 8-directional neighbors (includes diagonals for more realistic flow)
            int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    neighbors[idx] = ny * width + nx;
                    idx++;
                }
                else
                {
                    neighbors[idx] = -1;
                    idx++;
                }
            }

            return neighbors;
        }
    }
}
