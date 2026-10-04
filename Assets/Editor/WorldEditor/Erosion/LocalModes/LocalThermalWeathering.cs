using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Local thermal weathering erosion creates dramatic meter-scale slope-based breakdown.
    /// Simulates aggressive freeze-thaw cycles, rock fracturing, and granular rockfall patterns.
    /// Creates visible scarp slopes, talus deposits, and steep cliff features at pixel level.
    /// </summary>
    public class LocalThermalWeathering : IErosionMode
    {
        /// <summary>
        /// Default talus angle in degrees - slopes steeper than this collapse.
        /// </summary>
        private const float DEFAULT_TALUS_ANGLE = 32f;

        /// <summary>
        /// Aggressive weathering multiplier.
        /// </summary>
        private const float WEATHERING_MULTIPLIER = 3.0f;

        public string ModeName => "Thermal Weathering";
        public string Description => "Aggressive slope collapse and rockfall creating dramatic scarps and talus deposits.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.8f,
                iterations = 20,
                strength = 0.45f,
                radius = 32f,
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
            float strength = Mathf.Clamp(settings.strength, 0.1f, 1.0f);
            float talusAngle = Mathf.Clamp(settings.radius, 20f, 50f);
            float talusSlope = Mathf.Tan(talusAngle * Mathf.Deg2Rad);
            float seaLevel = Mathf.Clamp01(settings.seaLevel);

            System.Random random = new System.Random(settings.randomSeed);

            // Multi-pass weathering with increasing aggressiveness
            for (int iter = 0; iter < iterations; iter++)
            {
                float passIntensity = intensity * (1.0f + iter * 0.03f);

                // Primary weathering and slope collapse
                ApplySleeperCollapse(result, width, height, passIntensity, strength, talusSlope, seaLevel, random);

                // Create rockfall/talus patterns
                if (iter % 3 == 0)
                    CreateRockfallDeposits(result, width, height, intensity, random);

                // Sharpen cliffs
                if (iter % 4 == 0)
                    SharpenCliffs(result, width, height, intensity * 0.5f);
            }

            return result;
        }

        private void ApplySleeperCollapse(float[] heightmap, int width, int height, float intensity, float strength,
                                          float talusSlope, float seaLevel, System.Random random)
        {
            float[] heightDeltas = new float[width * height];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    if (heightmap[idx] < seaLevel) continue;

                    float currentHeight = heightmap[idx];
                    float maxSlope = 0;
                    int collapseTargetIdx = -1;

                    // Check all 8 neighbors for steep slopes
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx <= 0 || nx >= width - 1 || ny <= 0 || ny >= height - 1) continue;

                            int nIdx = ny * width + nx;
                            float neighborHeight = heightmap[nIdx];
                            float heightDiff = currentHeight - neighborHeight;
                            float distance = (dx != 0 && dy != 0) ? 1.414f : 1.0f;
                            float slope = heightDiff / distance;

                            if (slope > maxSlope)
                            {
                                maxSlope = slope;
                                collapseTargetIdx = nIdx;
                            }
                        }
                    }

                    // If slope exceeds talus angle, cause collapse
                    if (maxSlope > talusSlope && collapseTargetIdx >= 0)
                    {
                        float slopeExcess = (maxSlope - talusSlope) / talusSlope;
                        float collapseAmount = intensity * strength * slopeExcess * WEATHERING_MULTIPLIER * 0.008f;
                        collapseAmount *= (0.7f + 0.6f * (float)random.NextDouble()); // Random variation

                        heightDeltas[idx] -= collapseAmount;
                        heightDeltas[collapseTargetIdx] += collapseAmount * 0.7f; // Some material reaches target

                        // Scatter remaining material to nearby cells
                        float scatter = collapseAmount * 0.3f;
                        int scatterIdx = idx + (random.Next(8) % 4); // Scatter to random neighbor
                        if (scatterIdx >= 0 && scatterIdx < width * height)
                            heightDeltas[scatterIdx] += scatter;
                    }
                }
            }

            // Apply all collapsed material
            for (int i = 0; i < heightmap.Length; i++)
            {
                heightmap[i] += heightDeltas[i];
            }
        }

        private void CreateRockfallDeposits(float[] heightmap, int width, int height, float intensity, System.Random random)
        {
            // Create talus-like deposits at the base of steep slopes
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int idx = y * width + x;

                    // Look for steep terrain above
                    float steepnessAbove = 0;
                    int steepSourceIdx = -1;

                    for (int dy = -2; dy <= 0; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (dy == 0 && dx == 0) continue;
                            int srcIdx = (y + dy) * width + (x + dx);
                            float heightDiff = heightmap[srcIdx] - heightmap[idx];

                            if (heightDiff > steepnessAbove)
                            {
                                steepnessAbove = heightDiff;
                                steepSourceIdx = srcIdx;
                            }
                        }
                    }

                    // Create talus accumulation
                    if (steepnessAbove > 0.02f && steepSourceIdx >= 0)
                    {
                        float depositAmount = steepnessAbove * intensity * 0.006f * (float)random.NextDouble();
                        heightmap[idx] += depositAmount;
                    }
                }
            }
        }

        private void SharpenCliffs(float[] heightmap, int width, int height, float intensity)
        {
            // Enhance cliff edges by sharpening slope discontinuities
            float[] temp = new float[width * height];
            Array.Copy(heightmap, temp, heightmap.Length);

            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int idx = y * width + x;
                    float center = temp[idx];

                    // Calculate surrounding slopes
                    float maxUpslope = 0, maxDownslope = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nIdx = (y + dy) * width + (x + dx);
                            float diff = center - temp[nIdx];
                            if (diff > 0) maxUpslope = Mathf.Max(maxUpslope, diff);
                            else maxDownslope = Mathf.Max(maxDownslope, -diff);
                        }
                    }

                    // Sharpen cliffs (large slope discrepancies)
                    float slopeDiscrepancy = Mathf.Abs(maxUpslope - maxDownslope);
                    if (slopeDiscrepancy > 0.01f)
                    {
                        float sharpening = slopeDiscrepancy * intensity * 0.015f;
                        if (maxUpslope > maxDownslope)
                            heightmap[idx] += sharpening;
                        else
                            heightmap[idx] -= sharpening;
                    }
                }
            }
        }
    }
}