using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Utility for placing terrain chunk prefabs in the scene.
    /// </summary>
    public static class TerrainChunkPlacer
    {
        private const int CHUNK_SIZE = 256;
        private const string TERRAIN_PREFAB_PATH = "Assets/Prefabs/Terrain";
        private static readonly Regex ChunkNameRegex = new Regex(@"Terrain_(\d+)_(\d+)");

        /// <summary>
        /// Loads all terrain chunk prefabs and instantiates them as children of the container.
        /// Returns the number of chunks placed.
        /// </summary>
        public static int PlaceChunksInScene(GameObject container)
        {
            return PlaceChunksInScene(container, null);
        }

        /// <summary>
        /// Loads selective terrain chunk prefabs and instantiates them based on marked selection.
        /// If markedChunks is null, places all chunks. Otherwise, only places marked chunks.
        /// Returns the number of chunks placed.
        /// </summary>
        public static int PlaceChunksInScene(GameObject container, bool[,] markedChunks)
        {
            if (container == null)
                throw new System.ArgumentNullException(nameof(container));

            if (!AssetDatabase.IsValidFolder(TERRAIN_PREFAB_PATH))
            {
                Debug.LogWarning($"Terrain prefab path not found: {TERRAIN_PREFAB_PATH}");
                return 0;
            }

            // Search for all prefabs with "Terrain" in the name in the terrain folder
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab Terrain", new[] { TERRAIN_PREFAB_PATH });
            var sortedChunks = new SortedDictionary<(int x, int z), string>();

            // Collect and sort prefab paths by chunk coordinates
            foreach (var guid in prefabGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                // Only include terrain chunk prefabs (must match Terrain_X_Z pattern)
                if (TryParseChunkCoordinates(fileName, out int chunkX, out int chunkZ))
                {
                    sortedChunks[(chunkX, chunkZ)] = assetPath;
                }
            }

            if (sortedChunks.Count == 0)
            {
                Debug.LogWarning($"No terrain chunk prefabs found in {TERRAIN_PREFAB_PATH}. Have you generated terrain chunks yet?");
                return 0;
            }

            // Instantiate each chunk
            int instantiatedCount = 0;
            foreach (var kvp in sortedChunks)
            {
                int chunkX = kvp.Key.x;
                int chunkZ = kvp.Key.z;
                string prefabPath = kvp.Value;

                // Skip chunks that aren't marked (if selective placement is enabled)
                if (markedChunks != null)
                {
                    if (chunkX < 0 || chunkX >= markedChunks.GetLength(0) ||
                        chunkZ < 0 || chunkZ >= markedChunks.GetLength(1) ||
                        !markedChunks[chunkX, chunkZ])
                    {
                        continue;
                    }
                }

                try
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, container.transform) as GameObject;
                        if (instance != null)
                        {
                            // Position at correct world location
                            Vector3 worldPosition = new Vector3(
                                chunkX * CHUNK_SIZE,
                                0f,
                                chunkZ * CHUNK_SIZE);
                            instance.transform.position = worldPosition;

                            instantiatedCount++;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to instantiate terrain chunk at ({chunkX}, {chunkZ}): {ex.Message}");
                }
            }

            return instantiatedCount;
        }

        private static bool TryParseChunkCoordinates(string name, out int chunkX, out int chunkZ)
        {
            chunkX = 0;
            chunkZ = 0;

            var match = ChunkNameRegex.Match(name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out chunkX) && int.TryParse(match.Groups[2].Value, out chunkZ))
            {
                return true;
            }

            return false;
        }
    }
}
