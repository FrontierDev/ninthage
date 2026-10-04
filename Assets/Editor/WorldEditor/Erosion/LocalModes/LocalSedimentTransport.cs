using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Local sediment transport erosion creates dramatic meter-scale material redistribution.
    /// Simulates gravity-driven sediment movement with visible terraces, scarps, and accumulation patterns.
    /// Creates detailed pixel-level features through aggressive slope-based material reworking.
    /// </summary>
    public class LocalSedimentTransport : IErosionMode
    {
        /// <summary>
        /// Base sediment transport multiplier for aggressive redistribution.
        /// </summary>
        private const float TRANSPORT_MULTIPLIER = 2.5f;

        /// <summary>
        /// Minimum slope for any sediment movement.
        /// </summary>
        private const float MIN_SLOPE = 0.001f;

        public string ModeName => "Sediment Transport";
        public string Description => "Aggressive gravity-driven material transport creating terraces, scarps, and visible accumulation.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.75f,
                iterations = 22,
                strength = 0.4f,
                radius = 2.5f,
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
            float seaLevel = Mathf.Clamp01(settings.seaLevel);

            System.Random random = new System.Random(settings.randomSeed);

            // Multi-pass sediment transport with increasing aggression
            for (int iter = 0; iter < iterations; iter++)
            {
                float passIntensity = intensity * (1.0f + iter * 0.02f); // Increase intensity over iterations

                // Aggressive sediment generation and transport
                MaterialRedistribution(result, width, height, passIntensity, strength, random, seaLevel);

                // Create terrace patterns
                if (iter % 4 == 0)
                    CreateTerraces(result, width, height, intensity * 0.6f, random);

                // Enhance scarp formation
                if (iter % 6 == 0)
                    EnhanceScarps(result, width, height, intensity * 0.4f);
            }

            return result;
        }

        private void MaterialRedistribution(float[] heightmap, int width, int height, float intensity, float strength, System.Random random, float seaLevel)
        {
            float[] sedimentMap = new float[width * height];

            // First pass: Generate sediment from unstable slopes
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    if (heightmap[idx] < seaLevel) continue;

                    // Calculate slope to all neighbors
                    float maxSlope = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nIdx = (y + dy) * width + (x + dx);
                            float slope = Mathf.Abs(heightmap[idx] - heightmap[nIdx]);
                            if (slope > maxSlope) maxSlope = slope;
                        }
                    }

                    // Generate sediment based on slope stress
                    if (maxSlope > MIN_SLOPE)
                    {
                        float sedimentGen = maxSlope * intensity * strength * 0.05f;
                        heightmap[idx] -= sedimentGen;
                        sedimentMap[idx] += sedimentGen;
                    }
                }
            }

            // Second pass: Transport sediment aggressively downslope
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    if (sedimentMap[idx] < 0.00001f) continue;

                    float currentHeight = heightmap[idx];
                    float sedimentAmount = sedimentMap[idx];

                    // Find steepest downslope neighbor
                    float steepestDrop = 0;
                    int targetIdx = idx;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nIdx = (y + dy) * width + (x + dx);
                            float drop = currentHeight - heightmap[nIdx];

                            if (drop > steepestDrop)
                            {
                                steepestDrop = drop;
                                targetIdx = nIdx;
                            }
                        }
                    }

                    // Transport sediment
                    if (targetIdx != idx && steepestDrop > MIN_SLOPE)
                    {
                        float transportAmount = sedimentAmount * strength * TRANSPORT_MULTIPLIER * 0.3f;
                        sedimentMap[idx] -= transportAmount;
                        sedimentMap[targetIdx] += transportAmount;
                    }
                }
            }

            // Third pass: Deposit sediment with variation
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (sedimentMap[idx] > 0)
                    {
                        // Deposit with some randomness
                        float depositAmount = sedimentMap[idx] * (0.3f + 0.3f * (float)random.NextDouble());
                        heightmap[idx] += depositAmount;
                    }
                }
            }
        }

        private void CreateTerraces(float[] heightmap, int width, int height, float intensity, System.Random random)
        {
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int idx = y * width + x;
                    float current = heightmap[idx];

                    // Look for slope changes that could be terraced
                    float upslope = 0, downslope = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nIdx = (y + dy) * width + (x + dx);
                            float diff = current - heightmap[nIdx];
                            if (diff > 0) upslope = Mathf.Max(upslope, diff);
                            else downslope = Mathf.Max(downslope, -diff);
                        }
                    }

                    // Create terrace-like flattening where slopes meet
                    if (upslope > 0.01f && downslope > 0.01f)
                    {
                        float terraceAmount = intensity * 0.008f;
                        heightmap[idx] -= terraceAmount * (float)random.NextDouble();
                    }
                }
            }
        }

        private void EnhanceScarps(float[] heightmap, int width, int height, float intensity)
        {
            // Sharpen slope discontinuities to create scarp-like features
            float[] temp = new float[width * height];
            Array.Copy(heightmap, temp, heightmap.Length);

            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int idx = y * width + x;
                    float center = temp[idx];

                    // Calculate local gradient strength
                    float gradX = (temp[(y) * width + (x + 1)] - temp[(y) * width + (x - 1)]) * 0.5f;
                    float gradY = (temp[(y + 1) * width + (x)] - temp[(y - 1) * width + (x)]) * 0.5f;
                    float gradient = Mathf.Sqrt(gradX * gradX + gradY * gradY);

                    if (gradient > 0.005f)
                    {
                        // Enhance the slope discontinuity
                        float scarpEnhance = gradient * intensity * 0.01f;
                        if (gradX > 0 || gradY > 0)
                            heightmap[idx] += scarpEnhance;
                        else
                            heightmap[idx] -= scarpEnhance;
                    }
                }
            }
        }

    }
}