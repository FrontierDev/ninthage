using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Generates Perlin noise-based terrain detail for per-chunk microscopic variations.
    /// Used to add fine detail to otherwise flat terrain chunks.
    /// </summary>
    public static class PerlinNoiseDetail
    {
        /// <summary>
        /// Applies per-chunk Perlin noise detail to heightmap.
        /// Creates small variations that make terrain feel uneven without affecting overall structure.
        /// </summary>
        public static void ApplyDetail(float[] chunkHeights, int chunkSize, float strength, float scale, int seed)
        {
            if (chunkHeights == null || chunkHeights.Length != chunkSize * chunkSize)
                return;

            strength = Mathf.Clamp01(strength);
            scale = Mathf.Max(0.1f, scale);

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int idx = z * chunkSize + x;

                    // Generate Perlin noise at this position
                    float noiseValue = GetPerlinNoise(x, z, scale, seed);

                    // Apply noise as small variation
                    // Range: -0.05 to +0.05 with strength multiplier
                    float detail = (noiseValue - 0.5f) * 0.1f * strength;

                    chunkHeights[idx] = Mathf.Clamp01(chunkHeights[idx] + detail);
                }
            }
        }

        /// <summary>
        /// Generates Perlin noise value for a position.
        /// Uses Unity's built-in Mathf.PerlinNoise for consistency.
        /// </summary>
        private static float GetPerlinNoise(int x, int z, float scale, int seed)
        {
            // Use seed to offset the noise space
            float seedOffset = seed * 0.00001f;

            // Sample at multiple octaves for natural variation
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            // 3 octaves of Perlin noise for natural-looking detail
            for (int octave = 0; octave < 3; octave++)
            {
                float sampleX = (x * frequency / scale) + seedOffset;
                float sampleZ = (z * frequency / scale) + seedOffset;

                value += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                maxValue += amplitude;

                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return value / maxValue;
        }
    }
}
