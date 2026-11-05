using Game.Client.Rendering;
using Game.Shared.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Rendering
{
    /// <summary>Bakes placed architecture contact into a chunk-scoped weathering atlas.</summary>
    public static class ArchitectureWeatheringAtlasBaker
    {
        private const int ATLAS_RESOLUTION = 2048;
        private const int TILE_RESOLUTION = 64;
        private const int TILE_GUTTER = 1;
        private const int MAX_RENDERERS_PER_CHUNK = 1024;
        private const float CONTACT_FULL_HEIGHT = 0.10f;
        private const float CONTACT_FADE_HEIGHT = 1.50f;
        private const float UV_TOLERANCE = 0.00001f;
        private const string OUTPUT_ROOT = "Assets/Generated/ArchitectureWeathering";
        private const string SHADER_NAME = "Ninth Age/Architecture Lit Weathering";

        [MenuItem("Ninth Age/Rendering/Bake Selected Architecture Weathering Atlas")]
        private static void BakeSelected()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains.Length == 0)
            {
                Debug.LogError("Cannot bake architecture weathering: no active loaded Terrain exists.");
                return;
            }
            foreach (GameObject root in Selection.gameObjects)
            {
                TerrainTileComponent[] tiles = root.GetComponents<TerrainTileComponent>();
                Terrain[] chunkTerrains = root.GetComponentsInChildren<Terrain>(true);
                if (!root.scene.IsValid() || !root.scene.isLoaded || tiles.Length != 1
                    || chunkTerrains.Length != 1 || chunkTerrains[0].terrainData == null)
                {
                    Debug.LogError($"Cannot bake {root.name}: select a loaded chunk root with one TerrainTileComponent and one child Terrain with TerrainData.", root);
                    continue;
                }
                Bake(root, tiles[0], terrains);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Bake(GameObject root, TerrainTileComponent tile, Terrain[] terrains)
        {
            var candidates = new List<MeshRenderer>();
            foreach (GameObject sceneRoot in root.scene.GetRootGameObjects())
            {
                foreach (MeshRenderer renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                        continue;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && material.shader != null && material.shader.name == SHADER_NAME)
                        {
                            candidates.Add(renderer);
                            break;
                        }
                    }
                }
            }
            candidates.Sort((a, b) => string.CompareOrdinal(HierarchyPath(a.transform), HierarchyPath(b.transform)));
            if (candidates.Count > MAX_RENDERERS_PER_CHUNK)
            {
                Debug.LogError($"Cannot bake {root.name}: {candidates.Count} architecture renderers exceed {MAX_RENDERERS_PER_CHUNK} atlas slots.", root);
                return;
            }

            var pixels = new byte[ATLAS_RESOLUTION * ATLAS_RESOLUTION];
            var bindings = new List<ArchitectureWeatheringAtlasData.RendererBinding>();
            int tilesPerAxis = ATLAS_RESOLUTION / TILE_RESOLUTION;
            for (int i = 0; i < candidates.Count; i++)
            {
                MeshRenderer renderer = candidates[i];
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Vector2[] uv = mesh.uv2;
                if (!ValidUVs(uv, mesh.vertexCount))
                {
                    string reason = uv.Length != mesh.vertexCount || mesh.vertexCount == 0
                        ? $"has {uv.Length} UV2 coordinates for {mesh.vertexCount} vertices; a secondary UV set is required"
                        : "has UV2 coordinates outside [0,1] or containing NaN";
                    Debug.LogError($"Skipping {renderer.name}: mesh {mesh.name} {reason}. Source: {AssetDatabase.GetAssetPath(mesh)}.", renderer);
                    continue;
                }
                int tileX = i % tilesPerAxis;
                int tileY = i / tilesPerAxis;
                float scale = (float)(TILE_RESOLUTION - 2 * TILE_GUTTER) / ATLAS_RESOLUTION;
                var atlasST = new Vector4(scale, scale,
                    (float)(tileX * TILE_RESOLUTION + TILE_GUTTER) / ATLAS_RESOLUTION,
                    (float)(tileY * TILE_RESOLUTION + TILE_GUTTER) / ATLAS_RESOLUTION);
                Vector3[] positions = mesh.vertices;
                for (int vertex = 0; vertex < positions.Length; vertex++)
                    positions[vertex] = renderer.transform.TransformPoint(positions[vertex]);
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    if (mesh.GetTopology(submesh) != MeshTopology.Triangles)
                        continue;
                    int[] indices = mesh.GetTriangles(submesh);
                    for (int t = 0; t < indices.Length; t += 3)
                        Rasterize(pixels, uv, positions, indices[t], indices[t + 1], indices[t + 2], tileX, tileY, terrains);
                }
                FillGutter(pixels, tileX, tileY);
                bindings.Add(new ArchitectureWeatheringAtlasData.RendererBinding(renderer, atlasST));
            }

            if (candidates.Count > 0 && bindings.Count == 0)
            {
                Debug.LogError($"Cannot bake {root.name}: all {candidates.Count} architecture renderers were skipped; the existing atlas and bindings were not changed.", root);
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
                AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder(OUTPUT_ROOT))
                AssetDatabase.CreateFolder("Assets/Generated", "ArchitectureWeathering");
            string path = $"{OUTPUT_ROOT}/World_{tile.TileX}_{tile.TileZ}_ArchitectureWeathering.asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null && AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                Debug.LogError($"Cannot bake {root.name}: {path} is occupied by another asset type.", root);
                return;
            }
            var atlas = new Texture2D(ATLAS_RESOLUTION, ATLAS_RESOLUTION, TextureFormat.R8, false, true)
            {
                name = $"World_{tile.TileX}_{tile.TileZ}_ArchitectureWeathering",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            atlas.SetPixelData(pixels, 0);
            atlas.Apply(false, false);
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(atlas, path);
                existing = atlas;
            }
            else
            {
                EditorUtility.CopySerialized(atlas, existing);
                UnityEngine.Object.DestroyImmediate(atlas);
                EditorUtility.SetDirty(existing);
            }
            ArchitectureWeatheringAtlasData data = root.GetComponent<ArchitectureWeatheringAtlasData>();
            if (data == null)
                data = Undo.AddComponent<ArchitectureWeatheringAtlasData>(root);
            else
                Undo.RecordObject(data, "Bake Architecture Weathering Atlas");
            data.Configure(new Vector2Int(tile.TileX, tile.TileZ), existing, bindings.ToArray());
            if (PrefabUtility.IsPartOfPrefabInstance(data))
                PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Baked {bindings.Count} architecture renderer slots into {path}.", root);
        }

        private static bool ValidUVs(Vector2[] uv, int vertexCount)
        {
            if (uv.Length != vertexCount || vertexCount == 0)
                return false;
            foreach (Vector2 value in uv)
                if (float.IsNaN(value.x) || float.IsNaN(value.y) || value.x < -UV_TOLERANCE || value.y < -UV_TOLERANCE
                    || value.x > 1f + UV_TOLERANCE || value.y > 1f + UV_TOLERANCE)
                    return false;
            return true;
        }

        private static string HierarchyPath(Transform transform)
        {
            var path = new StringBuilder();
            for (Transform current = transform; current != null; current = current.parent)
                path.Insert(0, $"/{current.GetSiblingIndex():D8}:{current.name}");
            return path.ToString();
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 point)
        {
            return (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        }

        private static void Rasterize(byte[] pixels, Vector2[] uv, Vector3[] positions, int i0, int i1, int i2,
            int tileX, int tileY, Terrain[] terrains)
        {
            int left = tileX * TILE_RESOLUTION + TILE_GUTTER;
            int bottom = tileY * TILE_RESOLUTION + TILE_GUTTER;
            int usable = TILE_RESOLUTION - 2 * TILE_GUTTER;
            Vector2 offset = new Vector2(left, bottom);
            Vector2 a = offset + uv[i0] * usable;
            Vector2 b = offset + uv[i1] * usable;
            Vector2 c = offset + uv[i2] * usable;
            float area = Edge(a, b, c);
            if (Mathf.Abs(area) < 0.000001f)
                return;
            int minX = Mathf.Max(left, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(left + usable - 1, Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(bottom, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(bottom + usable - 1, Mathf.FloorToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = Edge(b, c, point) / area;
                    float w1 = Edge(c, a, point) / area;
                    float w2 = 1f - w0 - w1;
                    if (w0 < 0f || w1 < 0f || w2 < 0f)
                        continue;
                    Vector3 position = positions[i0] * w0 + positions[i1] * w1 + positions[i2] * w2;
                    foreach (Terrain terrain in terrains)
                    {
                        TerrainData data = terrain.terrainData;
                        if (data == null)
                            continue;
                        Vector3 local = position - terrain.transform.position;
                        Vector3 size = data.size;
                        if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
                            continue;
                        float height = terrain.transform.position.y + data.GetInterpolatedHeight(local.x / size.x, local.z / size.z);
                        float distance = Mathf.Max(0f, position.y - height);
                        float fade = Mathf.InverseLerp(CONTACT_FULL_HEIGHT, CONTACT_FADE_HEIGHT, distance);
                        float mask = 1f - Mathf.SmoothStep(0f, 1f, fade);
                        int index = y * ATLAS_RESOLUTION + x;
                        pixels[index] = (byte)Mathf.Max(pixels[index], Mathf.RoundToInt(Mathf.Clamp01(mask) * 255f));
                        break;
                    }
                }
            }
        }

        private static void FillGutter(byte[] pixels, int tileX, int tileY)
        {
            int x0 = tileX * TILE_RESOLUTION;
            int y0 = tileY * TILE_RESOLUTION;
            for (int i = 1; i < TILE_RESOLUTION - 1; i++)
            {
                pixels[(y0 + i) * ATLAS_RESOLUTION + x0] = pixels[(y0 + i) * ATLAS_RESOLUTION + x0 + 1];
                pixels[(y0 + i) * ATLAS_RESOLUTION + x0 + TILE_RESOLUTION - 1] = pixels[(y0 + i) * ATLAS_RESOLUTION + x0 + TILE_RESOLUTION - 2];
            }
            for (int i = 0; i < TILE_RESOLUTION; i++)
            {
                pixels[y0 * ATLAS_RESOLUTION + x0 + i] = pixels[(y0 + 1) * ATLAS_RESOLUTION + x0 + i];
                pixels[(y0 + TILE_RESOLUTION - 1) * ATLAS_RESOLUTION + x0 + i] = pixels[(y0 + TILE_RESOLUTION - 2) * ATLAS_RESOLUTION + x0 + i];
            }
        }
    }
}
