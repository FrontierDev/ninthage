using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor.Streaming
{
    /// <summary>
    /// Generates multiple LOD levels for terrain chunks using Unity LOD Group component.
    /// Creates high, medium, and low detail versions based on distance and importance.
    /// </summary>
    public static class TerrainLODGenerator
    {
        public enum LODDetail
        {
            High = 513,    // Full quality (513x513 = ~262k triangles per chunk)
            Medium = 257,  // Medium distance (257x257 = ~66k triangles per chunk)
            Low = 129      // Far distance (129x129 = ~16k triangles per chunk)
        }

        private readonly struct LODLevel
        {
            public readonly float screenRelativeTransitionHeight;
            public readonly int heightmapResolution;
            public readonly float meshSimplificationFactor;

            public LODLevel(float transition, int resolution, float meshFactor)
            {
                screenRelativeTransitionHeight = transition;
                heightmapResolution = resolution;
                meshSimplificationFactor = meshFactor;
            }
        }

        private static readonly LODLevel[] _lodLevels = new[]
        {
            new LODLevel(0.5f, (int)LODDetail.High, 1.0f),      // LOD 0: High detail
            new LODLevel(0.2f, (int)LODDetail.Medium, 0.5f),    // LOD 1: Medium detail  
            new LODLevel(0.05f, (int)LODDetail.Low, 0.25f),     // LOD 2: Low detail
        };

        /// <summary>
        /// Creates a terrain chunk with multiple LOD levels and Unity LOD Group.
        /// </summary>
        public static async Task<GameObject> CreateLODTerrainChunkAsync(
            float[] heightData,
            int chunkX,
            int chunkZ,
            float maxHeight,
            float seaLevel,
            float waterPlaneOffset,
            Material waterMaterial,
            Material terrainMaterial = null,
            float chunkSize = 256f)
        {
            var chunkRoot = new GameObject($"LODChunk_{chunkX}_{chunkZ}");
            var lodGroup = chunkRoot.AddComponent<LODGroup>();
            var lods = new List<LOD>();

            // Generate each LOD level
            for (int i = 0; i < _lodLevels.Length; i++)
            {
                var lodLevel = _lodLevels[i];
                var lodGameObject = await CreateLODLevelAsync(
                    heightData,
                    chunkX,
                    chunkZ,
                    i,
                    lodLevel,
                    maxHeight,
                    chunkRoot.transform,
                    terrainMaterial,
                    chunkSize);

                // Create LOD with all renderers from this level
                var renderers = lodGameObject.GetComponentsInChildren<Renderer>();

                if (i == 0)
                {
                    // LOD 0: Use Unity Terrain (highest detail)
                    var terrain = lodGameObject.GetComponent<Terrain>();
                    if (terrain != null)
                    {
                        // For Unity Terrain, we need a proxy renderer for LOD group
                        // Find the mesh renderer that should have been created as proxy
                        var proxyRenderer = lodGameObject.GetComponent<MeshRenderer>();
                        if (proxyRenderer != null)
                        {
                            var renderersList = new List<Renderer>(renderers) { proxyRenderer };
                            renderers = renderersList.ToArray();
                        }
                    }
                }
                // For LOD 1+: Mesh renderers should already be present from CreateLODLevelAsync

                var lod = new LOD(lodLevel.screenRelativeTransitionHeight, renderers);
                lods.Add(lod);
            }

            // Configure LOD Group
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();

            // Add water plane (same for all LOD levels)
            CreateWaterPlane(chunkRoot, maxHeight, seaLevel, waterPlaneOffset, chunkX, chunkZ, waterMaterial, chunkSize);

            return chunkRoot;
        }

        private static async Task<GameObject> CreateLODLevelAsync(
            float[] heightData,
            int chunkX,
            int chunkZ,
            int lodIndex,
            LODLevel lodLevel,
            float maxHeight,
            Transform parent,
            Material terrainMaterial = null,
            float chunkSize = 256f)
        {
            // Resample heightmap to ensure it fills the entire terrain properly
            // This handles both upsampling and downsampling with proper stretching
            var resampledHeights = await Task.Run(() =>
                ResampleHeightmap(heightData, lodLevel.heightmapResolution));

            // Yield control to allow UI updates
            await Task.Yield();

            // Create Unity objects on main thread with yields for responsiveness
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = lodLevel.heightmapResolution;
            // Scale terrain size to match world coordinates - use actual chunk size
            terrainData.size = new Vector3(chunkSize, maxHeight, chunkSize);

            // Convert and set heights with a small delay to yield control
            var unity2DHeights = ConvertTo2DArray(resampledHeights, lodLevel.heightmapResolution);
            await Task.Delay(1); // Brief yield to maintain responsiveness

            terrainData.SetHeights(0, 0, unity2DHeights);

            // Save as asset - ensure directory exists first
            var terrainDataPath = $"Assets/Prefabs/Terrain/LOD{lodIndex}_Terrain_{chunkX}_{chunkZ}.asset";

            // Create directory structure if it doesn't exist
            var directoryPath = System.IO.Path.GetDirectoryName(terrainDataPath);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
                AssetDatabase.Refresh(); // Ensure Unity recognizes the new directory
            }

            AssetDatabase.CreateAsset(terrainData, terrainDataPath);

            // Another brief yield before creating GameObjects
            await Task.Delay(1);

            // Create GameObject
            var lodGO = new GameObject($"LOD{lodIndex}_Terrain_{chunkX}_{chunkZ}");
            lodGO.transform.SetParent(parent);
            lodGO.transform.localPosition = Vector3.zero;

            if (lodIndex == 0)
            {
                // LOD 0: Use Unity Terrain (highest detail)
                var terrain = lodGO.AddComponent<Terrain>();
                terrain.terrainData = terrainData;

                // Apply terrain material to fix magenta coloring
                if (terrainMaterial != null)
                {
                    terrain.materialTemplate = terrainMaterial;
                    // For URP/HDRP, also try setting the material on the terrain data
                    if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
                    {
                        var terrainLayer = new TerrainLayer();
                        terrainLayer.diffuseTexture = Texture2D.whiteTexture; // Default white texture
                        terrainLayer.tileSize = new Vector2(15, 15);
                        terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };
                    }
                }

                var collider = lodGO.AddComponent<TerrainCollider>();
                collider.terrainData = terrainData;
            }
            else
            {
                // LOD 1+: Use mesh-based terrain (lower detail levels)
                var meshFilter = lodGO.AddComponent<MeshFilter>();
                var meshRenderer = lodGO.AddComponent<MeshRenderer>();

                // Create terrain mesh
                var terrainMesh = CreateTerrainMesh(resampledHeights, lodLevel.heightmapResolution, maxHeight, chunkSize);
                meshFilter.mesh = terrainMesh;

                // Apply material
                if (terrainMaterial != null)
                {
                    meshRenderer.material = terrainMaterial;
                }

                // Add mesh collider for physics
                var meshCollider = lodGO.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = terrainMesh;
                meshCollider.convex = false;
            }

            return lodGO;
        }

        /// <summary>
        /// Resamples heightmap to target resolution using bilinear filtering.
        /// Properly stretches heightmap data to fill the terrain regardless of source resolution.
        /// </summary>
        private static float[] ResampleHeightmap(float[] sourceHeights, int targetResolution)
        {
            int sourceResolution = Mathf.RoundToInt(Mathf.Sqrt(sourceHeights.Length));

            // If source and target are the same, return as-is
            if (sourceResolution == targetResolution)
                return (float[])sourceHeights.Clone();

            var targetHeights = new float[targetResolution * targetResolution];

            // Calculate scaling factor to stretch source across target range
            float scale = (float)(sourceResolution - 1) / (targetResolution - 1);

            for (int y = 0; y < targetResolution; y++)
            {
                for (int x = 0; x < targetResolution; x++)
                {
                    // Map target coordinates to source coordinates
                    float sourceX = x * scale;
                    float sourceY = y * scale;

                    // Bilinear interpolation for smooth resampling
                    int x0 = Mathf.FloorToInt(sourceX);
                    int y0 = Mathf.FloorToInt(sourceY);
                    int x1 = Mathf.Min(x0 + 1, sourceResolution - 1);
                    int y1 = Mathf.Min(y0 + 1, sourceResolution - 1);

                    float fx = sourceX - x0;
                    float fy = sourceY - y0;

                    // Sample four corners
                    float h00 = sourceHeights[y0 * sourceResolution + x0];
                    float h10 = sourceHeights[y0 * sourceResolution + x1];
                    float h01 = sourceHeights[y1 * sourceResolution + x0];
                    float h11 = sourceHeights[y1 * sourceResolution + x1];

                    // Bilinear interpolation
                    float h0 = Mathf.Lerp(h00, h10, fx);
                    float h1 = Mathf.Lerp(h01, h11, fx);
                    float height = Mathf.Lerp(h0, h1, fy);

                    targetHeights[y * targetResolution + x] = height;
                }
            }

            return targetHeights;
        }

        /// <summary>
        /// Downsamples heightmap to target resolution using bilinear filtering.
        /// </summary>
        private static float[] DownsampleHeightmap(float[] sourceHeights, int targetResolution)
        {
            int sourceResolution = Mathf.RoundToInt(Mathf.Sqrt(sourceHeights.Length));
            var targetHeights = new float[targetResolution * targetResolution];

            float scale = (float)(sourceResolution - 1) / (targetResolution - 1);

            for (int y = 0; y < targetResolution; y++)
            {
                for (int x = 0; x < targetResolution; x++)
                {
                    float sourceX = x * scale;
                    float sourceY = y * scale;

                    // Bilinear interpolation
                    int x0 = Mathf.FloorToInt(sourceX);
                    int y0 = Mathf.FloorToInt(sourceY);
                    int x1 = Mathf.Min(x0 + 1, sourceResolution - 1);
                    int y1 = Mathf.Min(y0 + 1, sourceResolution - 1);

                    float fx = sourceX - x0;
                    float fy = sourceY - y0;

                    float h00 = sourceHeights[y0 * sourceResolution + x0];
                    float h10 = sourceHeights[y0 * sourceResolution + x1];
                    float h01 = sourceHeights[y1 * sourceResolution + x0];
                    float h11 = sourceHeights[y1 * sourceResolution + x1];

                    float h0 = Mathf.Lerp(h00, h10, fx);
                    float h1 = Mathf.Lerp(h01, h11, fx);
                    float height = Mathf.Lerp(h0, h1, fy);

                    targetHeights[y * targetResolution + x] = height;
                }
            }

            return targetHeights;
        }

        /// <summary>
        /// Creates a mesh representation of terrain for LOD rendering.
        /// </summary>
        private static Mesh CreateTerrainMesh(float[] heights, int resolution, float maxHeight, float chunkSize = 256f)
        {
            var mesh = new Mesh();
            mesh.name = $"TerrainMesh_{resolution}";

            // Create vertices based on height data
            var vertices = new Vector3[resolution * resolution];
            var uvs = new Vector2[resolution * resolution];

            float scale = chunkSize / (resolution - 1); // Scale to actual world chunk size

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    float height = heights[index] * maxHeight;

                    vertices[index] = new Vector3(x * scale, height, y * scale);
                    uvs[index] = new Vector2((float)x / (resolution - 1), (float)y / (resolution - 1));
                }
            }

            // Create triangles
            var triangles = new List<int>();
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = y * resolution + x;

                    // First triangle
                    triangles.Add(i);
                    triangles.Add(i + resolution);
                    triangles.Add(i + 1);

                    // Second triangle
                    triangles.Add(i + 1);
                    triangles.Add(i + resolution);
                    triangles.Add(i + resolution + 1);
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs;

            // CRITICAL: Use smooth normals to reduce blocky appearance
            mesh.RecalculateNormals();

            // Apply additional smoothing to reduce blocky appearance
            SmoothMeshNormals(mesh);

            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Smooths mesh normals to reduce blocky appearance on terrain meshes
        /// </summary>
        private static void SmoothMeshNormals(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var smoothNormals = new Vector3[vertices.Length];

            // Group vertices by position and average their normals
            var vertexGroups = new Dictionary<Vector3, List<int>>();

            for (int i = 0; i < vertices.Length; i++)
            {
                var pos = vertices[i];
                // Round position to handle floating point precision
                var roundedPos = new Vector3(
                    Mathf.Round(pos.x * 100f) / 100f,
                    Mathf.Round(pos.y * 100f) / 100f,
                    Mathf.Round(pos.z * 100f) / 100f
                );

                if (!vertexGroups.ContainsKey(roundedPos))
                    vertexGroups[roundedPos] = new List<int>();

                vertexGroups[roundedPos].Add(i);
            }

            // Average normals for vertices at the same position
            foreach (var group in vertexGroups.Values)
            {
                var avgNormal = Vector3.zero;

                foreach (int vertexIndex in group)
                    avgNormal += normals[vertexIndex];

                avgNormal = avgNormal.normalized;

                foreach (int vertexIndex in group)
                    smoothNormals[vertexIndex] = avgNormal;
            }

            mesh.normals = smoothNormals;
        }

        private static float[,] ConvertTo2DArray(float[] heights, int resolution)
        {
            var result = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    result[y, x] = heights[y * resolution + x];
                }
            }
            return result;
        }

        private static Mesh _cachedWaterMesh;

        private static void CreateWaterPlane(GameObject parent, float maxHeight, float seaLevel, float waterPlaneOffset, int chunkX, int chunkZ, Material waterMaterial, float chunkSize = 256f)
        {
            var waterGO = new GameObject("Water");
            waterGO.transform.SetParent(parent.transform);
            waterGO.transform.localPosition = new Vector3(0f, seaLevel + waterPlaneOffset, 0f);

            // Create simple water plane mesh
            var meshFilter = waterGO.AddComponent<MeshFilter>();
            var meshRenderer = waterGO.AddComponent<MeshRenderer>();

            // Reuse the same water plane mesh for all chunks
            if (_cachedWaterMesh == null)
            {
                _cachedWaterMesh = new Mesh();
                _cachedWaterMesh.name = "SharedWaterPlane";

                // Create vertices for a standard 256x256 chunk plane (fixed size, not variable)
                _cachedWaterMesh.vertices = new Vector3[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(256f, 0f, 0f),
                    new Vector3(0f, 0f, 256f),
                    new Vector3(256f, 0f, 256f),
                };

                _cachedWaterMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                _cachedWaterMesh.uv = new Vector2[] {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, 1f), new Vector2(1f, 1f)
                };

                _cachedWaterMesh.RecalculateNormals();
                _cachedWaterMesh.RecalculateBounds();

                // Save mesh as asset so it persists
                string waterMeshPath = "Assets/Prefabs/Terrain/SharedWaterPlane_256.mesh";
                if (!AssetDatabase.LoadAssetAtPath<Mesh>(waterMeshPath))
                {
                    AssetDatabase.CreateAsset(_cachedWaterMesh, waterMeshPath);
                }
                else
                {
                    _cachedWaterMesh = AssetDatabase.LoadAssetAtPath<Mesh>(waterMeshPath);
                }
            }

            // Scale the water plane to match chunk size if needed
            if (chunkSize != 256f)
            {
                waterGO.transform.localScale = new Vector3(chunkSize / 256f, 1f, chunkSize / 256f);
            }

            meshFilter.mesh = _cachedWaterMesh;

            if (waterMaterial != null)
            {
                meshRenderer.material = waterMaterial;
            }
        }
    }
}