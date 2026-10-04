using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Fine-scale hydraulic erosion optimized for meter-scale terrain detail.
    /// Creates detailed channels, gullies, and drainage patterns with aggressive pixel-level modifications.
    /// Uses multiple erosion passes and randomized flow patterns for realistic and visible results.
    /// </summary>
    public class LocalFineHydraulicErosion : IErosionMode
    {
        /// <summary>
        /// Base sediment carrying capacity multiplier.
        /// </summary>
        private const float SEDIMENT_CAPACITY = 4.0f;

        /// <summary>
        /// Minimum water height to continue erosion.
        /// </summary>
        private const float MIN_WATER = 0.00001f;

        public string ModeName => "Fine-Scale Hydraulic";
        public string Description => "Creates detailed water channels and gully patterns with aggressive pixel-level erosion.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.7f,
                iterations = 20,
                strength = 0.12f,
                radius = 1.2f,
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
            float intensity = Mathf.Clamp(settings.intensity, 0.1f, 1.0f);
            float strength = Mathf.Clamp(settings.strength, 0.01f, 1.0f);
            float seaLevel = Mathf.Clamp01(settings.seaLevel);

            System.Random random = new System.Random(settings.randomSeed);

            // Multi-scale approach: create channels at different scales
            for (int iter = 0; iter < iterations; iter++)
            {
                // Simulate water flow and erosion
                SimulateWaterFlow(result, width, height, intensity, strength, random, seaLevel);

                // Add groove/channel detail
                if (iter % 3 == 0)
                    EnhanceChannelDetail(result, width, height, intensity * 0.7f, random);

                // Sharpen edges for more rugged appearance
                if (iter % 5 == 0)
                    SharpenFeatures(result, width, height, intensity * 0.5f);
            }

            return result;
        }

        private void SimulateWaterFlow(float[] heightmap, int width, int height, float intensity, float strength, System.Random random, float seaLevel)
        {
            float[] waterMap = new float[width * height];
            float[] sedimentMap = new float[width * height];

            // Add rain as random droplets
            for (int i = 0; i < width * height; i++)
            {
                if (heightmap[i] >= seaLevel)
                {
                    float randomRain = (float)random.NextDouble();
                    if (randomRain < 0.05f) // 5% chance for each pixel
                    {
                        waterMap[i] = 0.15f * strength * intensity;
                    }
                }
            }

            // Multiple passes of water flow
            for (int pass = 0; pass < 4; pass++)
            {
                float[] newWaterMap = new float[width * height];
                float[] newSedimentMap = new float[width * height];
                Array.Copy(sedimentMap, newSedimentMap, sedimentMap.Length);

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        if (waterMap[idx] < MIN_WATER) continue;

                        float currentHeight = heightmap[idx];
                        float waterLevel = currentHeight + waterMap[idx];

                        // Find steepest descent among 8 neighbors
                        float maxDrop = 0;
                        int bestIdx = idx;
                        int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
                        int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

                        for (int d = 0; d < 8; d++)
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];
                            if (nx <= 0 || nx >= width - 1 || ny <= 0 || ny >= height - 1) continue;

                            int nIdx = ny * width + nx;
                            float neighborWater = waterMap[nIdx];
                            float neighborWaterLevel = heightmap[nIdx] + neighborWater;
                            float drop = waterLevel - neighborWaterLevel;

                            if (drop > maxDrop)
                            {
                                maxDrop = drop;
                                bestIdx = nIdx;
                            }
                        }

                        if (bestIdx != idx && maxDrop > MIN_WATER)
                        {
                            // Transfer water
                            float transfer = waterMap[idx] * 0.8f;
                            newWaterMap[idx] -= transfer;
                            newWaterMap[bestIdx] += transfer;

                            // Erosion during transport
                            float erosionAmount = transfer * strength * intensity * 0.08f;
                            heightmap[idx] -= erosionAmount;
                            newSedimentMap[idx] += erosionAmount;

                            // Deposition at destination
                            float depositAmount = Mathf.Min(erosionAmount * 0.3f, newSedimentMap[idx]);
                            heightmap[bestIdx] += depositAmount * 0.5f;
                            newSedimentMap[idx] -= depositAmount * 0.5f;
                        }
                        else
                        {
                            // Local deposition
                            float deposit = newSedimentMap[idx] * 0.4f;
                            heightmap[idx] += deposit;
                            newSedimentMap[idx] -= deposit;

                            // Evaporate water
                            newWaterMap[idx] = waterMap[idx] * 0.6f;
                        }
                    }
                }

                Array.Copy(newWaterMap, waterMap, waterMap.Length);
                Array.Copy(newSedimentMap, sedimentMap, sedimentMap.Length);
            }
        }

        private void EnhanceChannelDetail(float[] heightmap, int width, int height, float intensity, System.Random random)
        {
            // Identify and deepen channels
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int idx = y * width + x;

                    // Check if this is a low point (potential channel)
                    float centerHeight = heightmap[idx];
                    int lowerNeighbors = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nIdx = (y + dy) * width + (x + dx);
                            if (heightmap[nIdx] > centerHeight)
                                lowerNeighbors++;
                        }
                    }

                    // If surrounded by higher terrain, deepen the channel
                    if (lowerNeighbors >= 6)
                    {
                        float deepening = intensity * 0.02f * (float)random.NextDouble();
                        heightmap[idx] -= deepening;
                    }
                }
            }
        }

        private void SharpenFeatures(float[] heightmap, int width, int height, float intensity)
        {
            // Apply edge enhancement to create sharper features
            float[] temp = new float[width * height];
            Array.Copy(heightmap, temp, heightmap.Length);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;

                    // Simple edge detection + enhancement
                    float center = temp[idx];
                    float sumNeighbors = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            sumNeighbors += temp[(y + dy) * width + (x + dx)];
                        }
                    }

                    float avgNeighbor = sumNeighbors / 8f;
                    float diff = center - avgNeighbor;
                    heightmap[idx] = center + diff * intensity * 0.2f;
                }
            }
        }
    }
}