using System;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Shared utility functions for high-performance erosion algorithms.
    /// Provides cache-efficient 1D array operations and padding management for seamless tile processing.
    /// </summary>
    public static class ErosionUtils
    {
        /// <summary>
        /// Converts 2D coordinates to 1D array index for cache-efficient access.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="width">Array width</param>
        /// <returns>1D index</returns>
        public static int GetIndex(int x, int y, int width)
        {
            return y * width + x;
        }

        /// <summary>
        /// Expands heightmap with padding using edge replication for seamless tile boundaries.
        /// </summary>
        /// <param name="heightmap">Original heightmap data</param>
        /// <param name="width">Original width</param>
        /// <param name="height">Original height</param>
        /// <param name="padding">Padding pixels to add on all sides</param>
        /// <returns>New array with padding applied</returns>
        public static float[] ExpandWithPadding(float[] heightmap, int width, int height, int padding)
        {
            int paddedWidth = width + 2 * padding;
            int paddedHeight = height + 2 * padding;
            float[] padded = new float[paddedWidth * paddedHeight];

            // Copy original data to center
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcIndex = GetIndex(x, y, width);
                    int dstIndex = GetIndex(x + padding, y + padding, paddedWidth);
                    padded[dstIndex] = heightmap[srcIndex];
                }
            }

            // Replicate edges
            // Top and bottom edges
            for (int y = 0; y < padding; y++)
            {
                for (int x = padding; x < paddedWidth - padding; x++)
                {
                    // Top edge
                    int topSrcIndex = GetIndex(x, padding, paddedWidth);
                    int topDstIndex = GetIndex(x, y, paddedWidth);
                    padded[topDstIndex] = padded[topSrcIndex];

                    // Bottom edge
                    int bottomSrcIndex = GetIndex(x, paddedHeight - padding - 1, paddedWidth);
                    int bottomDstIndex = GetIndex(x, paddedHeight - 1 - y, paddedWidth);
                    padded[bottomDstIndex] = padded[bottomSrcIndex];
                }
            }

            // Left and right edges
            for (int y = 0; y < paddedHeight; y++)
            {
                for (int x = 0; x < padding; x++)
                {
                    // Left edge
                    int leftSrcIndex = GetIndex(padding, y, paddedWidth);
                    int leftDstIndex = GetIndex(x, y, paddedWidth);
                    padded[leftDstIndex] = padded[leftSrcIndex];

                    // Right edge
                    int rightSrcIndex = GetIndex(paddedWidth - padding - 1, y, paddedWidth);
                    int rightDstIndex = GetIndex(paddedWidth - 1 - x, y, paddedWidth);
                    padded[rightDstIndex] = padded[rightSrcIndex];
                }
            }

            return padded;
        }

        /// <summary>
        /// Extracts core region from padded heightmap.
        /// </summary>
        /// <param name="paddedHeightmap">Padded heightmap data</param>
        /// <param name="paddedWidth">Width of padded array</param>
        /// <param name="paddedHeight">Height of padded array</param>
        /// <param name="padding">Padding amount to remove</param>
        /// <returns>Core region without padding</returns>
        public static float[] ExtractCoreRegion(float[] paddedHeightmap, int paddedWidth, int paddedHeight, int padding)
        {
            int coreWidth = paddedWidth - 2 * padding;
            int coreHeight = paddedHeight - 2 * padding;
            float[] core = new float[coreWidth * coreHeight];

            for (int y = 0; y < coreHeight; y++)
            {
                for (int x = 0; x < coreWidth; x++)
                {
                    int srcIndex = GetIndex(x + padding, y + padding, paddedWidth);
                    int dstIndex = GetIndex(x, y, coreWidth);
                    core[dstIndex] = paddedHeightmap[srcIndex];
                }
            }

            return core;
        }

        /// <summary>
        /// Calculates local slope magnitude at given coordinates using central differences.
        /// Properly accounts for world scale by dividing height differences by horizontal distance.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="heightmap">Heightmap data</param>
        /// <param name="width">Heightmap width</param>
        /// <param name="height">Heightmap height</param>
        /// <param name="cellSize">World units per pixel (default 1.0 for 256x256 chunk)</param>
        /// <returns>Slope magnitude in world units</returns>
        public static float CalculateSlope(int x, int y, float[] heightmap, int width, int height, float cellSize = 1.0f)
        {
            // Clamp coordinates to valid range
            int x1 = Mathf.Max(0, x - 1);
            int x2 = Mathf.Min(width - 1, x + 1);
            int y1 = Mathf.Max(0, y - 1);
            int y2 = Mathf.Min(height - 1, y + 1);

            // Central differences in height
            float dx = heightmap[GetIndex(x2, y, width)] - heightmap[GetIndex(x1, y, width)];
            float dy = heightmap[GetIndex(x, y2, width)] - heightmap[GetIndex(x, y1, width)];

            // Horizontal distance for central difference (spans 2 pixels)
            float horizontalDistance = cellSize * 2.0f;

            // Calculate slope as rise/run
            float slopeX = dx / horizontalDistance;
            float slopeY = dy / horizontalDistance;

            return Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
        }

        /// <summary>
        /// Gets height values of 8 neighbors around given coordinate.
        /// </summary>
        /// <param name="x">Center X coordinate</param>
        /// <param name="y">Center Y coordinate</param>
        /// <param name="heightmap">Heightmap data</param>
        /// <param name="width">Heightmap width</param>
        /// <param name="height">Heightmap height</param>
        /// <returns>Array of 8 neighbor heights (clockwise from top-left)</returns>
        public static float[] Get8Neighbors(int x, int y, float[] heightmap, int width, int height)
        {
            float[] neighbors = new float[8];

            // Offsets for 8 neighbors (clockwise from top-left)
            int[] offsetsX = { -1, 0, 1, 1, 1, 0, -1, -1 };
            int[] offsetsY = { -1, -1, -1, 0, 1, 1, 1, 0 };

            for (int i = 0; i < 8; i++)
            {
                int nx = Mathf.Clamp(x + offsetsX[i], 0, width - 1);
                int ny = Mathf.Clamp(y + offsetsY[i], 0, height - 1);
                neighbors[i] = heightmap[GetIndex(nx, ny, width)];
            }

            return neighbors;
        }

        /// <summary>
        /// Applies 3x3 Laplacian operator for smoothing calculations.
        /// Properly normalized for world scale.
        /// </summary>
        /// <param name="x">Center X coordinate</param>
        /// <param name="y">Center Y coordinate</param>
        /// <param name="heightmap">Heightmap data</param>
        /// <param name="width">Heightmap width</param>
        /// <param name="height">Heightmap height</param>
        /// <param name="cellSize">World units per pixel (default 1.0 for 256x256 chunk)</param>
        /// <returns>Laplacian value normalized for world scale</returns>
        public static float CalculateLaplacian(int x, int y, float[] heightmap, int width, int height, float cellSize = 1.0f)
        {
            float center = heightmap[GetIndex(x, y, width)];
            float[] neighbors = Get8Neighbors(x, y, heightmap, width, height);

            float neighborSum = 0f;
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighborSum += neighbors[i];
            }

            // Laplacian operator normalized by cell size squared
            float laplacian = neighborSum - 8f * center;
            return laplacian / (cellSize * cellSize);
        }

        /// <summary>
        /// Calculates world scale cell size for a given heightmap representing a terrain area.
        /// </summary>
        /// <param name="heightmapWidth">Width of heightmap in pixels</param>
        /// <param name="heightmapHeight">Height of heightmap in pixels</param>
        /// <param name="worldWidth">Width of represented world area in world units</param>
        /// <param name="worldHeight">Height of represented world area in world units</param>
        /// <returns>Cell size in world units per pixel (uses minimum for square pixels)</returns>
        public static float CalculateCellSize(int heightmapWidth, int heightmapHeight, float worldWidth, float worldHeight)
        {
            float cellSizeX = worldWidth / heightmapWidth;
            float cellSizeY = worldHeight / heightmapHeight;

            // Use minimum to ensure square pixels (should be equal for proper heightmaps)
            return Mathf.Min(cellSizeX, cellSizeY);
        }

        /// <summary>
        /// Calculates world scale cell size for a given heightmap representing a terrain chunk.
        /// </summary>
        /// <param name="heightmapWidth">Width of heightmap in pixels</param>
        /// <param name="chunkSize">Size of terrain chunk in world units (default 256)</param>
        /// <returns>World units per pixel</returns>
        [System.Obsolete("Use CalculateCellSize(width, height, worldWidth, worldHeight) instead")]
        public static float CalculateCellSize(int heightmapWidth, float chunkSize = 256f)
        {
            return chunkSize / heightmapWidth;
        }

        /// <summary>
        /// Checks if coordinates are within heightmap bounds.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="width">Heightmap width</param>
        /// <param name="height">Heightmap height</param>
        /// <returns>True if coordinates are valid</returns>
        public static bool IsValidCoordinate(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}