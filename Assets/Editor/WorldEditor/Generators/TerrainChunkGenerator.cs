using UnityEditor;
using UnityEngine;
using Game.Editor.WorldEditor;
using Game.Runtime.Shared;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Generates terrain chunks from heightmap data and saves them as prefabs.
    /// Supports optional erosion processing before chunk generation.
    /// </summary>
    public static class TerrainChunkGenerator
    {
        private const int CHUNK_SIZE = 256;
        private const int TERRAIN_HEIGHTMAP_RESOLUTION = 2049; // 2049x2049 vertices = 8 vertices per unit in 256x256 chunk
        private const string TERRAIN_PREFAB_PATH = "Assets/Prefabs/Terrain";

        /// <summary>
        /// Generates terrain chunks from heightmap texture with optional sequential multi-mode erosion.
        /// Erosion modes are applied in order to the same heightmap.
        /// </summary>
        public static void GenerateChunks(
            Texture2D heightmapTexture,
            int chunksX,
            int chunksZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial,
            List<(IErosionMode, ErosionSettings)> erosionModes)
        {
            // If erosion modes provided, apply them sequentially to the heightmap
            float[] baseHeightmap = null;
            if (erosionModes != null && erosionModes.Count > 0)
            {
                baseHeightmap = LoadHeightmapFromTexture(heightmapTexture);

                // Calculate actual world dimensions from chunk layout
                float worldWidth = chunksX * CHUNK_SIZE;
                float worldHeight = chunksZ * CHUNK_SIZE;

                // Apply each erosion mode sequentially
                for (int modeIndex = 0; modeIndex < erosionModes.Count; modeIndex++)
                {
                    var (mode, settings) = erosionModes[modeIndex];

                    // Update settings with actual world dimensions for proper scaling
                    var updatedSettings = settings;
                    updatedSettings.worldWidth = worldWidth;
                    updatedSettings.worldHeight = worldHeight;

                    baseHeightmap = mode.Erode(
                        baseHeightmap,
                        heightmapTexture.width,
                        heightmapTexture.height,
                        updatedSettings);

                    EditorUtility.DisplayProgressBar(
                        "Generating Terrain Chunks",
                        $"Erosion {modeIndex + 1}/{erosionModes.Count} ({mode.ModeName}) complete...",
                        0.1f + (modeIndex / (float)erosionModes.Count) * 0.2f);
                }

                EditorUtility.DisplayProgressBar(
                    "Generating Terrain Chunks",
                    "All erosion modes applied, generating chunks...",
                    0.3f);
            }

            GenerateChunks(
                heightmapTexture,
                chunksX,
                chunksZ,
                maxHeight,
                seaLevel,
                waterPlaneOffset,
                waterMaterial,
                baseHeightmap);

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Generates terrain chunks from heightmap texture without erosion.
        /// </summary>
        public static void GenerateChunks(
            Texture2D heightmapTexture,
            int chunksX,
            int chunksZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial)
        {
            List<(IErosionMode, ErosionSettings)> noErosion = null;
            GenerateChunks(heightmapTexture, chunksX, chunksZ, maxHeight, seaLevel, waterPlaneOffset, waterMaterial, noErosion);
        }

        /// <summary>
        /// Internal: Generates terrain chunks from heightmap texture with optional pre-eroded heightmap.
        /// </summary>
        private static void GenerateChunks(
            Texture2D heightmapTexture,
            int chunksX,
            int chunksZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial,
            float[] preerodedHeightmap)
        {
            // Ensure prefab directory exists
            EnsurePrefabDirectory();

            for (int z = 0; z < chunksZ; z++)
            {
                for (int x = 0; x < chunksX; x++)
                {
                    float progress = (z * chunksX + x) / (float)(chunksX * chunksZ);
                    EditorUtility.DisplayProgressBar(
                        "Generating Terrain Chunks",
                        $"Chunk {x}, {z}...",
                        progress);

                    GenerateChunk(
                        heightmapTexture,
                        x,
                        z,
                        chunksX,
                        chunksZ,
                        maxHeight,
                        seaLevel,
                        waterPlaneOffset,
                        waterMaterial,
                        preerodedHeightmap);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Generates a single terrain chunk with optional erosion processing.
        /// </summary>
        public static void GenerateChunk(
            Texture2D heightmapTexture,
            int chunkX,
            int chunkZ,
            int totalChunksX,
            int totalChunksZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial,
            List<(IErosionMode, ErosionSettings)> erosionModes)
        {
            EnsurePrefabDirectory();

            // Sample base heightmap for this chunk
            float[] chunkHeights = HeightmapProcessor.SampleHeightmapRegionFromTexture(
                heightmapTexture,
                chunkX,
                chunkZ,
                CHUNK_SIZE,
                totalChunksX,
                totalChunksZ);

            // Apply erosion modes if provided
            if (erosionModes != null && erosionModes.Count > 0)
            {
                // Calculate world dimensions for erosion calculations
                float worldWidth = totalChunksX * CHUNK_SIZE;
                float worldHeight = totalChunksZ * CHUNK_SIZE;

                // Apply each erosion mode sequentially to this chunk
                for (int modeIndex = 0; modeIndex < erosionModes.Count; modeIndex++)
                {
                    var (erosionMode, settings) = erosionModes[modeIndex];

                    // Apply erosion to the chunk heightmap
                    chunkHeights = erosionMode.Erode(
                        chunkHeights,
                        CHUNK_SIZE,
                        CHUNK_SIZE,
                        new ErosionSettings
                        {
                            intensity = settings.intensity,
                            iterations = settings.iterations,
                            strength = settings.strength,
                            radius = settings.radius,
                            randomSeed = settings.randomSeed + (chunkX * 1000 + chunkZ), // Per-chunk variation
                            seaLevel = settings.seaLevel,
                            worldWidth = worldWidth,
                            worldHeight = worldHeight
                        });
                }
            }

            // Create and save terrain data asset
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = TERRAIN_HEIGHTMAP_RESOLUTION;
            terrainData.size = new Vector3(CHUNK_SIZE, maxHeight, CHUNK_SIZE);

            // Set heightmap
            SetTerrainHeights(terrainData, chunkHeights);

            // Save terrain data as asset
            string terrainDataPath = $"{TERRAIN_PREFAB_PATH}/Terrain_{chunkX}_{chunkZ}.asset";
            AssetDatabase.CreateAsset(terrainData, terrainDataPath);

            // Create root GameObject for chunk
            GameObject chunkRoot = new GameObject($"Chunk_{chunkX}_{chunkZ}");

            // Create terrain GameObject
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = $"Terrain_{chunkX}_{chunkZ}";
            terrainGO.transform.SetParent(chunkRoot.transform);
            terrainGO.transform.localPosition = Vector3.zero;

            // Enable auto-reconnect so adjacent terrain tiles stitch together seamlessly
            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.allowAutoConnect = true;

            // Ensure terrain collider is present
            TerrainCollider collider = terrainGO.GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = terrainGO.AddComponent<TerrainCollider>();
            }
            collider.terrainData = terrainData;

            // Create water plane at sea level
            CreateWaterPlane(chunkRoot, maxHeight, seaLevel, waterPlaneOffset, chunkX, chunkZ, waterMaterial);

            // Add tile coordinate component to root for debugging and organization
            TerrainTileComponent tileComponent = chunkRoot.AddComponent<TerrainTileComponent>();
            tileComponent.TileX = chunkX;
            tileComponent.TileZ = chunkZ;

            // Save as prefab at local origin (placer will position it in world space)
            string prefabPath = $"{TERRAIN_PREFAB_PATH}/Terrain_{chunkX}_{chunkZ}.prefab";
            PrefabUtility.SaveAsPrefabAsset(chunkRoot, prefabPath);

            // Clean up the temporary GameObject
            Object.DestroyImmediate(chunkRoot);
        }

        private static void GenerateChunk(
            Texture2D heightmapTexture,
            int chunkX,
            int chunkZ,
            int totalChunksX,
            int totalChunksZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial,
            float[] preerodedHeightmap)
        {
            // Sample heightmap region
            float[] chunkHeights;
            if (preerodedHeightmap != null)
            {
                // Use pre-eroded heightmap
                chunkHeights = SampleChunkFromHeightmap(
                    preerodedHeightmap,
                    chunkX,
                    chunkZ,
                    heightmapTexture.width,
                    heightmapTexture.height,
                    totalChunksX,
                    totalChunksZ);
            }
            else
            {
                // Sample directly from texture (no erosion)
                chunkHeights = HeightmapProcessor.SampleHeightmapRegionFromTexture(
                    heightmapTexture,
                    chunkX,
                    chunkZ,
                    CHUNK_SIZE,
                    totalChunksX,
                    totalChunksZ);
            }

            // Create and save terrain data asset
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = TERRAIN_HEIGHTMAP_RESOLUTION;
            terrainData.size = new Vector3(CHUNK_SIZE, maxHeight, CHUNK_SIZE);

            // Set heightmap
            SetTerrainHeights(terrainData, chunkHeights);

            // Save terrain data as asset
            string terrainDataPath = $"{TERRAIN_PREFAB_PATH}/Terrain_{chunkX}_{chunkZ}.asset";
            AssetDatabase.CreateAsset(terrainData, terrainDataPath);

            // Create root GameObject for chunk
            GameObject chunkRoot = new GameObject($"Chunk_{chunkX}_{chunkZ}");

            // Create terrain GameObject
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = $"Terrain_{chunkX}_{chunkZ}";
            terrainGO.transform.SetParent(chunkRoot.transform);
            terrainGO.transform.localPosition = Vector3.zero;

            // Enable auto-reconnect so adjacent terrain tiles stitch together seamlessly
            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.allowAutoConnect = true;

            // Ensure terrain collider is present
            TerrainCollider collider = terrainGO.GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = terrainGO.AddComponent<TerrainCollider>();
            }
            collider.terrainData = terrainData;

            // Create water plane at sea level
            CreateWaterPlane(chunkRoot, maxHeight, seaLevel, waterPlaneOffset, chunkX, chunkZ, waterMaterial);

            // Add tile coordinate component to root for debugging and organization
            TerrainTileComponent tileComponent = chunkRoot.AddComponent<TerrainTileComponent>();
            tileComponent.TileX = chunkX;
            tileComponent.TileZ = chunkZ;

            // Save as prefab at local origin (placer will position it in world space)
            string prefabPath = $"{TERRAIN_PREFAB_PATH}/Terrain_{chunkX}_{chunkZ}.prefab";
            PrefabUtility.SaveAsPrefabAsset(chunkRoot, prefabPath);

            // Clean up the temporary GameObject
            Object.DestroyImmediate(chunkRoot);
        }

        /// <summary>
        /// Loads full heightmap from texture into a flattened array.
        /// </summary>
        private static float[] LoadHeightmapFromTexture(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            float[] heightmap = new float[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                heightmap[i] = pixels[i].grayscale;
            }

            return heightmap;
        }

        /// <summary>
        /// Samples a chunk region from a pre-eroded heightmap array.
        /// </summary>
        private static float[] SampleChunkFromHeightmap(float[] fullHeightmap, int chunkX, int chunkZ, int texWidth, int texHeight, int totalChunksX, int totalChunksZ)
        {
            float[] chunkHeights = new float[CHUNK_SIZE * CHUNK_SIZE];

            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    // Calculate world position
                    float worldX = chunkX * CHUNK_SIZE + x;
                    float worldZ = chunkZ * CHUNK_SIZE + z;

                    // Calculate total world dimensions
                    float totalWorldWidth = totalChunksX * CHUNK_SIZE;
                    float totalWorldHeight = totalChunksZ * CHUNK_SIZE;

                    // Map to texture coordinates
                    float uvX = worldX / totalWorldWidth;
                    float uvZ = worldZ / totalWorldHeight;

                    // Sample nearest texel
                    int texX = (int)(uvX * texWidth + 0.5f);
                    int texZ = (int)(uvZ * texHeight + 0.5f);

                    // Clamp to valid range
                    texX = Mathf.Clamp(texX, 0, texWidth - 1);
                    texZ = Mathf.Clamp(texZ, 0, texHeight - 1);

                    int sourceIndex = texZ * texWidth + texX;
                    int targetIndex = z * CHUNK_SIZE + x;

                    chunkHeights[targetIndex] = fullHeightmap[sourceIndex];
                }
            }

            return chunkHeights;
        }

        private static void SetTerrainHeights(TerrainData terrainData, float[] heights)
        {
            int resolution = terrainData.heightmapResolution;
            float[,] heightMap = new float[resolution, resolution];

            // Fill heightmap with bilinear interpolation to preserve erosion detail
            // Note: Unity terrain uses [z, x] indexing for heightmaps
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Map terrain heightmap coordinates to source heightmap coordinates
                    float sampleX = (float)x / (resolution - 1) * (CHUNK_SIZE - 1);
                    float sampleZ = (float)z / (resolution - 1) * (CHUNK_SIZE - 1);

                    // Get integer and fractional parts for bilinear interpolation
                    int ix = (int)sampleX;
                    int iz = (int)sampleZ;
                    float fracX = sampleX - ix;
                    float fracZ = sampleZ - iz;

                    // Clamp to valid range (allowing +1 for interpolation)
                    ix = Mathf.Clamp(ix, 0, CHUNK_SIZE - 1);
                    iz = Mathf.Clamp(iz, 0, CHUNK_SIZE - 1);

                    int ix1 = Mathf.Min(ix + 1, CHUNK_SIZE - 1);
                    int iz1 = Mathf.Min(iz + 1, CHUNK_SIZE - 1);

                    // Sample 4 neighboring points
                    float h00 = heights[iz * CHUNK_SIZE + ix];
                    float h10 = heights[iz * CHUNK_SIZE + ix1];
                    float h01 = heights[iz1 * CHUNK_SIZE + ix];
                    float h11 = heights[iz1 * CHUNK_SIZE + ix1];

                    // Bilinear interpolation
                    float h0 = Mathf.Lerp(h00, h10, fracX);
                    float h1 = Mathf.Lerp(h01, h11, fracX);
                    float interpolated = Mathf.Lerp(h0, h1, fracZ);

                    heightMap[z, x] = interpolated;
                }
            }

            terrainData.SetHeights(0, 0, heightMap);
        }

        private static void EnsurePrefabDirectory()
        {
            if (!AssetDatabase.IsValidFolder(TERRAIN_PREFAB_PATH))
            {
                string parentPath = "Assets/Prefabs";
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder(parentPath, "Terrain");
            }
        }

        private static void CreateWaterPlane(GameObject parent, float maxHeight, float seaLevel, float waterPlaneOffset, int chunkX, int chunkZ, Material waterMaterial)
        {
            // Create a simple plane mesh for water
            GameObject waterGO = new GameObject("Water");
            waterGO.transform.SetParent(parent.transform);

            // Add mesh filter and renderer
            MeshFilter meshFilter = waterGO.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = waterGO.AddComponent<MeshRenderer>();

            // Create simple plane mesh
            Mesh waterMesh = new Mesh();
            waterMesh.name = $"WaterPlane_{chunkX}_{chunkZ}";

            // Create vertices for a plane that covers the chunk area
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(CHUNK_SIZE, 0f, 0f),
                new Vector3(0f, 0f, CHUNK_SIZE),
                new Vector3(CHUNK_SIZE, 0f, CHUNK_SIZE),
            };

            // Create triangle indices
            int[] triangles = new int[6]
            {
                0, 2, 1,
                1, 2, 3,
            };

            // Create UVs
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            waterMesh.vertices = vertices;
            waterMesh.triangles = triangles;
            waterMesh.uv = uv;
            waterMesh.RecalculateNormals();

            // Save mesh as asset
            string meshAssetPath = $"{TERRAIN_PREFAB_PATH}/WaterMesh_{chunkX}_{chunkZ}.asset";
            AssetDatabase.CreateAsset(waterMesh, meshAssetPath);

            meshFilter.mesh = waterMesh;

            // Assign the provided water material
            if (waterMaterial != null)
            {
                meshRenderer.material = waterMaterial;
            }

            // Position water at sea level + offset height
            waterGO.transform.localPosition = new Vector3(0f, seaLevel + waterPlaneOffset, 0f);
        }
    }
}
