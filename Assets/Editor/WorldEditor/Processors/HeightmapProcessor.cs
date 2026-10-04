using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Utility for extracting heightmap data from Texture2D.
    /// </summary>
    public static class HeightmapProcessor
    {
        /// <summary>
        /// Samples a region directly from the heightmap texture using bilinear interpolation.
        /// The heightmap is stretched to cover the full world dimensions.
        /// Returns normalized heights [0, 1] for the requested region.
        /// Uses bilinear interpolation for smooth results while ensuring seam prevention at chunk boundaries.
        /// </summary>
        public static float[] SampleHeightmapRegionFromTexture(
            Texture2D heightmapTexture,
            int chunkX,
            int chunkZ,
            int chunkSize,
            int totalChunksX,
            int totalChunksZ)
        {
            if (heightmapTexture == null)
                throw new System.ArgumentNullException(nameof(heightmapTexture));

            // Calculate total world dimensions
            float totalWorldWidth = totalChunksX * chunkSize;
            float totalWorldDepth = totalChunksZ * chunkSize;

            int textureWidth = heightmapTexture.width;
            int textureHeight = heightmapTexture.height;

            float[] chunkHeights = new float[chunkSize * chunkSize];

            // Sample from texture using bilinear interpolation with seam prevention
            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    // Map world position to texture pixel coordinates
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;

                    // Calculate normalized UV coordinates
                    float uvX = worldX / totalWorldWidth;
                    float uvZ = worldZ / totalWorldDepth;

                    // Clamp to valid range
                    uvX = Mathf.Clamp01(uvX);
                    uvZ = Mathf.Clamp01(uvZ);

                    float heightValue;

                    // For chunk boundary vertices, use nearest-neighbor to prevent seams
                    if (IsChunkBoundaryVertex(x, z, chunkSize, chunkX, chunkZ, totalChunksX, totalChunksZ))
                    {
                        // Use nearest-neighbor for boundary vertices to ensure exact matches
                        int pixelX = Mathf.RoundToInt(uvX * (textureWidth - 1));
                        int pixelZ = Mathf.RoundToInt(uvZ * (textureHeight - 1));
                        pixelX = Mathf.Clamp(pixelX, 0, textureWidth - 1);
                        pixelZ = Mathf.Clamp(pixelZ, 0, textureHeight - 1);

                        Color pixelColor = heightmapTexture.GetPixel(pixelX, pixelZ);
                        heightValue = pixelColor.grayscale;
                    }
                    else
                    {
                        // Use bilinear interpolation for interior vertices
                        heightValue = SampleBilinear(heightmapTexture, uvX, uvZ, textureWidth, textureHeight);
                    }

                    int destIndex = z * chunkSize + x;
                    chunkHeights[destIndex] = heightValue;
                }
            }

            return chunkHeights;
        }

        /// <summary>
        /// Determines if a vertex is on a chunk boundary that needs seam prevention.
        /// </summary>
        private static bool IsChunkBoundaryVertex(int x, int z, int chunkSize, int chunkX, int chunkZ, int totalChunksX, int totalChunksZ)
        {
            // Check if vertex is on the edge of the chunk AND not on the world boundary
            bool onLeftEdge = (x == 0) && (chunkX > 0);
            bool onRightEdge = (x == chunkSize - 1) && (chunkX < totalChunksX - 1);
            bool onBottomEdge = (z == 0) && (chunkZ > 0);
            bool onTopEdge = (z == chunkSize - 1) && (chunkZ < totalChunksZ - 1);

            return onLeftEdge || onRightEdge || onBottomEdge || onTopEdge;
        }

        /// <summary>
        /// Performs bilinear interpolation sampling from a heightmap texture.
        /// </summary>
        private static float SampleBilinear(Texture2D texture, float uvX, float uvZ, int textureWidth, int textureHeight)
        {
            // Convert UV to texture coordinates
            float texX = uvX * (textureWidth - 1);
            float texZ = uvZ * (textureHeight - 1);

            // Get the four surrounding pixel coordinates
            int x0 = Mathf.FloorToInt(texX);
            int x1 = Mathf.CeilToInt(texX);
            int z0 = Mathf.FloorToInt(texZ);
            int z1 = Mathf.CeilToInt(texZ);

            // Clamp to texture bounds
            x0 = Mathf.Clamp(x0, 0, textureWidth - 1);
            x1 = Mathf.Clamp(x1, 0, textureWidth - 1);
            z0 = Mathf.Clamp(z0, 0, textureHeight - 1);
            z1 = Mathf.Clamp(z1, 0, textureHeight - 1);

            // Calculate interpolation weights
            float fracX = texX - x0;
            float fracZ = texZ - z0;

            // Sample the four surrounding pixels
            float h00 = texture.GetPixel(x0, z0).grayscale; // bottom-left
            float h10 = texture.GetPixel(x1, z0).grayscale; // bottom-right
            float h01 = texture.GetPixel(x0, z1).grayscale; // top-left
            float h11 = texture.GetPixel(x1, z1).grayscale; // top-right

            // Perform bilinear interpolation
            float h0 = Mathf.Lerp(h00, h10, fracX); // bottom interpolation
            float h1 = Mathf.Lerp(h01, h11, fracX); // top interpolation
            float result = Mathf.Lerp(h0, h1, fracZ); // final interpolation

            return result;
        }
    }
}
