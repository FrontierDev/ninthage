using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Rendering
{
    /// <summary>
    /// Bakes terrain proximity into placement-specific architectural mesh assets.
    /// </summary>
    public static class ArchitectureGroundContactMaskBaker
    {
        private const float MAX_GROUND_PROXIMITY_DISTANCE = 4f;
        private const string OUTPUT_ROOT = "Assets/Generated/ArchitectureGroundContact";
        private const string MENU_PATH = "Ninth Age/Rendering/Bake Architecture Ground Contact";

        [MenuItem(MENU_PATH)]
        private static void BakeSelected()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (Array.FindAll(terrains, terrain => terrain.terrainData != null).Length == 0)
            {
                Debug.LogError("Cannot bake architecture ground contact: no active loaded Terrain with TerrainData exists.");
                return;
            }

            var visited = new HashSet<MeshRenderer>();
            foreach (GameObject root in Selection.gameObjects)
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!visited.Add(renderer))
                        continue;

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                        continue;

                    if (!renderer.gameObject.scene.IsValid() || string.IsNullOrEmpty(renderer.gameObject.scene.path))
                    {
                        Debug.LogError($"Cannot bake {renderer.name}: save its scene before baking.", renderer);
                        continue;
                    }

                    BakeRenderer(renderer, filter, terrains);
                }
            }
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateBakeSelected() => Selection.gameObjects.Length > 0;

        private static void BakeRenderer(MeshRenderer renderer, MeshFilter filter, Terrain[] terrains)
        {
            Mesh source = filter.sharedMesh;

            Vector3[] vertices = source.vertices;
            Color[] colors = source.colors;
            if (colors.Length != vertices.Length)
                colors = new Color[vertices.Length];

            int unresolved = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPosition = renderer.transform.TransformPoint(vertices[i]);
                bool found = false;
                foreach (Terrain terrain in terrains)
                {
                    TerrainData data = terrain.terrainData;
                    if (data == null)
                        continue;

                    Vector3 origin = terrain.transform.position;
                    Vector3 size = data.size;
                    float x = worldPosition.x - origin.x;
                    float z = worldPosition.z - origin.z;
                    if (x < 0f || z < 0f || x > size.x || z > size.z)
                        continue;

                    float terrainHeight = origin.y + data.GetInterpolatedHeight(x / size.x, z / size.z);
                    float verticalDistance = Mathf.Max(0f, worldPosition.y - terrainHeight);
                    colors[i].r = Mathf.Clamp01(1f - verticalDistance / MAX_GROUND_PROXIMITY_DISTANCE);
                    found = true;
                    break;
                }

                if (!found)
                {
                    colors[i].r = 0f;
                    unresolved++;
                }
            }

            string path = GetOutputPath(renderer, source.name);
            EnsureOutputFolder();
            Mesh baked = UnityEngine.Object.Instantiate(source);
            baked.name = source.name;
            baked.colors = colors;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                    Debug.LogError($"Cannot bake {renderer.name}: output path {path} is occupied by a non-mesh asset.", renderer);
                    return;
                }

                AssetDatabase.CreateAsset(baked, path);
                existing = baked;
            }
            else
            {
                EditorUtility.CopySerialized(baked, existing);
                UnityEngine.Object.DestroyImmediate(baked);
                EditorUtility.SetDirty(existing);
            }

            Undo.RecordObject(filter, "Bake Architecture Ground Contact");
            filter.sharedMesh = existing;
            if (PrefabUtility.IsPartOfPrefabInstance(filter))
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            AssetDatabase.SaveAssets();

            if (unresolved > 0)
                Debug.LogWarning($"Ground contact bake for {renderer.name}: {unresolved}/{vertices.Length} vertices are outside loaded Terrain bounds.", renderer);
        }

        private static string GetOutputPath(MeshRenderer renderer, string meshName)
        {
            var hierarchy = new StringBuilder();
            for (Transform current = renderer.transform; current != null; current = current.parent)
                hierarchy.Insert(0, $"/{current.name}[{current.GetSiblingIndex()}]");

            string identity = renderer.gameObject.scene.path + hierarchy + "/" + meshName;
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(identity));

            var suffix = new StringBuilder();
            foreach (byte value in hash)
                suffix.Append(value.ToString("x2"));

            string label = renderer.gameObject.scene.name + "_" + hierarchy + "_" + meshName;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                label = label.Replace(invalid, '_');
            label = label.Replace('/', '_').Replace('\\', '_');
            if (label.Length > 80)
                label = label.Substring(0, 80);
            return $"{OUTPUT_ROOT}/{label}_{suffix}.asset";
        }

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
                AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder(OUTPUT_ROOT))
                AssetDatabase.CreateFolder("Assets/Generated", "ArchitectureGroundContact");
        }
    }
}
