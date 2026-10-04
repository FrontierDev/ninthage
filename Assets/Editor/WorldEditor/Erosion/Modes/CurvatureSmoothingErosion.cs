using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Curvature-based smoothing erosion using Laplacian operator with slope limiting.
    /// Preserves cliffs and large-scale features while smoothing moderate slopes.
    /// Designed for ultra-fast editor performance with configurable padding.
    /// 
    /// Parameter Mapping:
    /// - intensity: Smoothing strength [0,1]
    /// - iterations: Number of smoothing passes [1-4]  
    /// - strength: Blend factor with original height [0,1]
    /// - radius: Slope threshold for cliff preservation [0.1-2.0]
    /// </summary>
    public class CurvatureSmoothingErosion : IErosionMode
    {
        private const int DEFAULT_PADDING = 16;
        private const float TERMINATION_THRESHOLD = 0.001f;

        public string ModeName => "Curvature Smoothing";
        public string Description => "Laplacian smoothing with slope limiting to preserve cliffs and macro features.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.3f,        // Smoothing intensity
                iterations = 3,          // 2-4 iterations recommended
                strength = 0.5f,         // Balanced blend with original
                radius = 0.8f,           // Moderate cliff protection
                randomSeed = 1234,       // Not used - deterministic
                seaLevel = 0.0f,         // Not used in this mode
                worldWidth = 256f,       // Will be overwritten during processing
                worldHeight = 256f       // Will be overwritten during processing
            };
        }

        public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)
        {
            if (heightmap == null || heightmap.Length != width * height)
                throw new ArgumentException("Invalid heightmap dimensions");

            // Calculate world scale cell size using actual world dimensions
            float cellSize = ErosionUtils.CalculateCellSize(width, height, settings.worldWidth, settings.worldHeight);

            // Use radius as slope threshold, clamp to reasonable range
            float slopeThreshold = Mathf.Clamp(settings.radius, 0.1f, 2.0f);
            int padding = DEFAULT_PADDING;

            // Apply padding for seamless tile boundaries
            float[] paddedMap = ErosionUtils.ExpandWithPadding(heightmap, width, height, padding);
            int paddedWidth = width + 2 * padding;
            int paddedHeight = height + 2 * padding;

            // Calculate cell size for padded map (maintains world scale)
            float paddedCellSize = ErosionUtils.CalculateCellSize(paddedWidth, paddedHeight,
                settings.worldWidth + (2 * padding * cellSize),
                settings.worldHeight + (2 * padding * cellSize));

            // Process with curvature smoothing
            float[] processedMap = ApplyCurvatureSmoothing(paddedMap, paddedWidth, paddedHeight, settings, slopeThreshold, paddedCellSize);

            // Extract core region without padding
            return ErosionUtils.ExtractCoreRegion(processedMap, paddedWidth, paddedHeight, padding);
        }

        /// <summary>
        /// Applies curvature-based smoothing using Laplacian operator with slope limiting.
        /// </summary>
        private float[] ApplyCurvatureSmoothing(float[] heightmap, int width, int height, ErosionSettings settings, float slopeThreshold, float cellSize)
        {
            float[] result = new float[heightmap.Length];
            float[] tempMap = new float[heightmap.Length];
            Array.Copy(heightmap, result, heightmap.Length);

            int iterations = Mathf.Clamp(settings.iterations, 1, 4);
            float intensity = Mathf.Clamp01(settings.intensity);
            float blendFactor = Mathf.Clamp01(settings.strength);

            for (int iter = 0; iter < iterations; iter++)
            {
                Array.Copy(result, tempMap, result.Length);
                float totalChange = 0f;
                int changedPixels = 0;

                // Apply Laplacian smoothing with slope limiting
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int index = ErosionUtils.GetIndex(x, y, width);

                        // Skip if slope is too steep (preserve cliffs) - use world-scale slope
                        float slope = ErosionUtils.CalculateSlope(x, y, tempMap, width, height, cellSize);
                        if (slope > slopeThreshold)
                        {
                            continue;
                        }

                        // Calculate Laplacian (curvature) with world scale normalization
                        float laplacian = ErosionUtils.CalculateLaplacian(x, y, tempMap, width, height, cellSize);

                        // Apply smoothing with intensity
                        float originalHeight = tempMap[index];
                        float smoothedHeight = originalHeight + laplacian * intensity * cellSize * cellSize;

                        // Blend with original based on strength parameter
                        float finalHeight = Mathf.Lerp(originalHeight, smoothedHeight, blendFactor);

                        result[index] = finalHeight;

                        // Track change for early termination
                        float change = Mathf.Abs(finalHeight - originalHeight);
                        totalChange += change;
                        changedPixels++;
                    }
                }

                // Early termination if change is minimal
                if (changedPixels > 0)
                {
                    float averageChange = totalChange / changedPixels;
                    if (averageChange < TERMINATION_THRESHOLD)
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }
}