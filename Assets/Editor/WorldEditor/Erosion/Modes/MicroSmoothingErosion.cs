using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Micro-smoothing erosion for ultra-light noise removal with macro form preservation.
    /// Applies subtle 3x3 neighborhood averaging with strict slope limitations.
    /// Designed to remove high-frequency artifacts without flattening terrain features.
    /// 
    /// Parameter Mapping:
    /// - intensity: Smoothing intensity [0,1] (typically very low 0.05-0.3)
    /// - iterations: Always 1 for micro-smoothing
    /// - strength: Blend factor with original [0,1]
    /// - radius: Slope limit threshold [0.5-3.0]
    /// </summary>
    public class MicroSmoothingErosion : IErosionMode
    {
        private const int DEFAULT_PADDING = 8; // Lower padding for subtle effect
        private const float DEFAULT_INTENSITY = 0.15f;
        private const float DEFAULT_SLOPE_LIMIT = 1.5f;

        public string ModeName => "Micro Smoothing";
        public string Description => "Ultra-light noise removal with macro form preservation. Apply last for final polish.";

        public ErosionSettings GetDefaultSettings()
        {
            return new ErosionSettings
            {
                intensity = 0.15f,            // Very subtle smoothing
                iterations = 1,               // Always single pass
                strength = 0.3f,              // Light blend factor
                radius = 1.5f,                // Moderate feature protection
                randomSeed = 9999,            // Not used - deterministic
                seaLevel = 0.0f,              // Not used in this mode
                worldWidth = 256f,            // Will be overwritten during processing
                worldHeight = 256f            // Will be overwritten during processing
            };
        }

        public float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings)
        {
            if (heightmap == null || heightmap.Length != width * height)
                throw new ArgumentException("Invalid heightmap dimensions");

            // Calculate world scale cell size using actual world dimensions
            float cellSize = ErosionUtils.CalculateCellSize(width, height, settings.worldWidth, settings.worldHeight);

            // Use radius as slope limit threshold, clamp to reasonable range
            float slopeLimit = Mathf.Clamp(settings.radius, 0.5f, 3.0f);
            int padding = DEFAULT_PADDING;

            // Apply padding for seamless tile boundaries (minimal for micro effects)
            float[] paddedMap = ErosionUtils.ExpandWithPadding(heightmap, width, height, padding);
            int paddedWidth = width + 2 * padding;
            int paddedHeight = height + 2 * padding;

            // Calculate cell size for padded map (maintains world scale)
            float paddedCellSize = ErosionUtils.CalculateCellSize(paddedWidth, paddedHeight,
                settings.worldWidth + (2 * padding * cellSize),
                settings.worldHeight + (2 * padding * cellSize));

            // Process with micro smoothing (always single pass)
            float[] processedMap = ApplyMicroSmoothing(paddedMap, paddedWidth, paddedHeight, settings, slopeLimit, paddedCellSize);

            // Extract core region without padding
            return ErosionUtils.ExtractCoreRegion(processedMap, paddedWidth, paddedHeight, padding);
        }

        /// <summary>
        /// Applies ultra-light smoothing for noise removal while preserving macro forms.
        /// </summary>
        private float[] ApplyMicroSmoothing(float[] heightmap, int width, int height, ErosionSettings settings, float slopeLimit, float cellSize)
        {
            float[] result = new float[heightmap.Length];
            Array.Copy(heightmap, result, heightmap.Length);

            // Clamp parameters for micro-smoothing
            float intensity = Mathf.Clamp(settings.intensity, 0.05f, 0.3f); // Keep very low
            float blendFactor = Mathf.Clamp01(settings.strength);

            // Single pass only - micro effects should be subtle
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = ErosionUtils.GetIndex(x, y, width);
                    float originalHeight = heightmap[index];

                    // Skip if local slope is too steep (preserve macro features) - use world-scale slope
                    float localSlope = ErosionUtils.CalculateSlope(x, y, heightmap, width, height, cellSize);
                    if (localSlope > slopeLimit)
                    {
                        continue;
                    }

                    // Calculate 3x3 neighborhood average for ultra-light smoothing
                    float smoothedHeight = Calculate3x3Average(x, y, heightmap, width, height);

                    // Apply very subtle smoothing
                    float microSmoothHeight = Mathf.Lerp(originalHeight, smoothedHeight, intensity);

                    // Blend with original based on strength parameter
                    result[index] = Mathf.Lerp(originalHeight, microSmoothHeight, blendFactor);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates simple 3x3 neighborhood average for subtle smoothing.
        /// </summary>
        private float Calculate3x3Average(int x, int y, float[] heightmap, int width, int height)
        {
            float sum = 0f;
            int count = 0;

            // Sample 3x3 neighborhood including center
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = Mathf.Clamp(x + dx, 0, width - 1);
                    int ny = Mathf.Clamp(y + dy, 0, height - 1);
                    int neighborIndex = ErosionUtils.GetIndex(nx, ny, width);
                    sum += heightmap[neighborIndex];
                    count++;
                }
            }

            return count > 0 ? sum / count : heightmap[ErosionUtils.GetIndex(x, y, width)];
        }
    }
}