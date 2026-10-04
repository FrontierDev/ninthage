using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Manages a unified heightmap buffer across multiple terrain tiles with border padding.
    /// Handles loading tiles into a unified buffer, extracting eroded results, and synchronizing borders.
    /// </summary>
    public class MultiTileErosionBuffer
    {
        public float[] UnifiedHeightmap { get; private set; }
        public int UnifiedWidth { get; private set; }
        public int UnifiedHeight { get; private set; }

        private List<Terrain> _terrains;
        private int _gridWidth;
        private int _gridHeight;
        private int _borderSize;
        private Dictionary<int, (int tileX, int tileZ, int resolution)> _tileMetadata;
        private Dictionary<int, Rect> _tileRects; // Rect in unified buffer space

        /// <summary>
        /// Creates a multi-tile erosion buffer from a grid of terrains.
        /// </summary>
        public static MultiTileErosionBuffer Create(List<Terrain> terrains, int gridWidth, int gridHeight, int borderSize)
        {
            if (terrains == null || terrains.Count == 0)
                return null;

            var buffer = new MultiTileErosionBuffer
            {
                _terrains = terrains,
                _gridWidth = gridWidth,
                _gridHeight = gridHeight,
                _borderSize = borderSize,
                _tileMetadata = new Dictionary<int, (int, int, int)>(),
                _tileRects = new Dictionary<int, Rect>()
            };

            if (!buffer.LoadTerrainsIntoBuffer())
                return null;

            return buffer;
        }

        private bool LoadTerrainsIntoBuffer()
        {
            if (_terrains.Count != _gridWidth * _gridHeight)
            {
                Debug.LogError($"Terrain count ({_terrains.Count}) does not match grid dimensions ({_gridWidth}x{_gridHeight})");
                return false;
            }

            // Get terrain resolution from first terrain
            if (_terrains[0]?.terrainData == null)
                return false;

            int tileResolution = _terrains[0].terrainData.heightmapResolution;

            // Verify all terrains have same resolution
            foreach (var terrain in _terrains)
            {
                if (terrain?.terrainData == null || terrain.terrainData.heightmapResolution != tileResolution)
                {
                    Debug.LogError("All terrains must have the same heightmap resolution.");
                    return false;
                }
            }

            // Calculate unified buffer size
            UnifiedWidth = _gridWidth * tileResolution + (_gridWidth + 1) * _borderSize;
            UnifiedHeight = _gridHeight * tileResolution + (_gridHeight + 1) * _borderSize;
            UnifiedHeightmap = new float[UnifiedWidth * UnifiedHeight];

            // Load all terrains into unified buffer
            for (int terrainIndex = 0; terrainIndex < _terrains.Count; terrainIndex++)
            {
                var terrain = _terrains[terrainIndex];
                if (terrain?.terrainData == null)
                    continue;

                int tileX = terrainIndex % _gridWidth;
                int tileZ = terrainIndex / _gridWidth;

                // Store metadata
                _tileMetadata[terrainIndex] = (tileX, tileZ, tileResolution);

                // Calculate position in unified buffer
                int bufferX = tileX * (tileResolution + _borderSize) + _borderSize;
                int bufferZ = tileZ * (tileResolution + _borderSize) + _borderSize;

                // Store rect for later extraction
                _tileRects[terrainIndex] = new Rect(bufferX, bufferZ, tileResolution, tileResolution);

                // Load heightmap
                float[,] heightmap = terrain.terrainData.GetHeights(0, 0, tileResolution, tileResolution);

                // Copy into unified buffer
                for (int z = 0; z < tileResolution; z++)
                {
                    for (int x = 0; x < tileResolution; x++)
                    {
                        int unifiedIdx = (bufferZ + z) * UnifiedWidth + (bufferX + x);
                        UnifiedHeightmap[unifiedIdx] = heightmap[z, x];
                    }
                }

                // Fill borders from neighboring tiles
                FillBorders(terrainIndex, tileX, tileZ, tileResolution);
            }

            return true;
        }

        private void FillBorders(int terrainIndex, int tileX, int tileZ, int resolution)
        {
            int bufferX = tileX * (resolution + _borderSize) + _borderSize;
            int bufferZ = tileZ * (resolution + _borderSize) + _borderSize;

            // Fill left border (from west neighbor)
            if (tileX > 0)
            {
                int westTerrainIdx = (tileZ * _gridWidth) + (tileX - 1);
                var westTerrain = _terrains[westTerrainIdx];
                if (westTerrain?.terrainData != null)
                {
                    float[,] westHeightmap = westTerrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    for (int z = 0; z < resolution; z++)
                    {
                        for (int b = 0; b < _borderSize; b++)
                        {
                            int unifiedIdx = (bufferZ + z) * UnifiedWidth + (bufferX - _borderSize + b);
                            int sourceX = resolution - _borderSize + b;
                            UnifiedHeightmap[unifiedIdx] = westHeightmap[z, sourceX];
                        }
                    }
                }
            }

            // Fill right border (from east neighbor)
            if (tileX < _gridWidth - 1)
            {
                int eastTerrainIdx = (tileZ * _gridWidth) + (tileX + 1);
                var eastTerrain = _terrains[eastTerrainIdx];
                if (eastTerrain?.terrainData != null)
                {
                    float[,] eastHeightmap = eastTerrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    for (int z = 0; z < resolution; z++)
                    {
                        for (int b = 0; b < _borderSize; b++)
                        {
                            int unifiedIdx = (bufferZ + z) * UnifiedWidth + (bufferX + resolution + b);
                            UnifiedHeightmap[unifiedIdx] = eastHeightmap[z, b];
                        }
                    }
                }
            }

            // Fill bottom border (from south neighbor)
            if (tileZ > 0)
            {
                int southTerrainIdx = ((tileZ - 1) * _gridWidth) + tileX;
                var southTerrain = _terrains[southTerrainIdx];
                if (southTerrain?.terrainData != null)
                {
                    float[,] southHeightmap = southTerrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int b = 0; b < _borderSize; b++)
                        {
                            int unifiedIdx = (bufferZ - _borderSize + b) * UnifiedWidth + (bufferX + x);
                            int sourceZ = resolution - _borderSize + b;
                            UnifiedHeightmap[unifiedIdx] = southHeightmap[sourceZ, x];
                        }
                    }
                }
            }

            // Fill top border (from north neighbor)
            if (tileZ < _gridHeight - 1)
            {
                int northTerrainIdx = ((tileZ + 1) * _gridWidth) + tileX;
                var northTerrain = _terrains[northTerrainIdx];
                if (northTerrain?.terrainData != null)
                {
                    float[,] northHeightmap = northTerrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int b = 0; b < _borderSize; b++)
                        {
                            int unifiedIdx = (bufferZ + resolution + b) * UnifiedWidth + (bufferX + x);
                            UnifiedHeightmap[unifiedIdx] = northHeightmap[b, x];
                        }
                    }
                }
            }
        }

        public void SetUnifiedHeightmap(float[] heightmap)
        {
            if (heightmap.Length == UnifiedHeightmap.Length)
            {
                System.Array.Copy(heightmap, UnifiedHeightmap, heightmap.Length);
            }
        }

        /// <summary>
        /// Extracts eroded tile heightmaps from the unified buffer and synchronizes borders.
        /// </summary>
        public Dictionary<int, float[,]> ExtractTileHeightmaps()
        {
            var result = new Dictionary<int, float[,]>();

            foreach (var kvp in _tileRects)
            {
                int terrainIndex = kvp.Key;
                Rect tileRect = kvp.Value;

                if (!_tileMetadata.TryGetValue(terrainIndex, out var metadata))
                    continue;

                int resolution = metadata.Item3;
                float[,] tileHeightmap = new float[resolution, resolution];

                // Extract tile
                int bufferX = (int)tileRect.x;
                int bufferZ = (int)tileRect.y;

                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int unifiedIdx = (bufferZ + z) * UnifiedWidth + (bufferX + x);
                        tileHeightmap[z, x] = UnifiedHeightmap[unifiedIdx];
                    }
                }

                result[terrainIndex] = tileHeightmap;
            }

            // Synchronize borders back to neighboring tiles
            SynchronizeBordersWithNeighbors(result);

            return result;
        }

        private void SynchronizeBordersWithNeighbors(Dictionary<int, float[,]> tileHeightmaps)
        {
            foreach (var kvp in tileHeightmaps)
            {
                int terrainIndex = kvp.Key;
                float[,] heightmap = kvp.Value;

                if (!_tileMetadata.TryGetValue(terrainIndex, out var metadata))
                    continue;

                int tileX = metadata.Item1;
                int tileZ = metadata.Item2;
                int resolution = metadata.Item3;

                // Share borders with east neighbor
                if (tileX < _gridWidth - 1)
                {
                    int eastIdx = (tileZ * _gridWidth) + (tileX + 1);
                    if (tileHeightmaps.TryGetValue(eastIdx, out float[,] eastHeightmap))
                    {
                        for (int z = 0; z < resolution; z++)
                        {
                            eastHeightmap[z, 0] = heightmap[z, resolution - 1];
                        }
                    }
                }

                // Share borders with west neighbor
                if (tileX > 0)
                {
                    int westIdx = (tileZ * _gridWidth) + (tileX - 1);
                    if (tileHeightmaps.TryGetValue(westIdx, out float[,] westHeightmap))
                    {
                        for (int z = 0; z < resolution; z++)
                        {
                            westHeightmap[z, resolution - 1] = heightmap[z, 0];
                        }
                    }
                }

                // Share borders with north neighbor
                if (tileZ < _gridHeight - 1)
                {
                    int northIdx = ((tileZ + 1) * _gridWidth) + tileX;
                    if (tileHeightmaps.TryGetValue(northIdx, out float[,] northHeightmap))
                    {
                        for (int x = 0; x < resolution; x++)
                        {
                            northHeightmap[0, x] = heightmap[resolution - 1, x];
                        }
                    }
                }

                // Share borders with south neighbor
                if (tileZ > 0)
                {
                    int southIdx = ((tileZ - 1) * _gridWidth) + tileX;
                    if (tileHeightmaps.TryGetValue(southIdx, out float[,] southHeightmap))
                    {
                        for (int x = 0; x < resolution; x++)
                        {
                            southHeightmap[resolution - 1, x] = heightmap[0, x];
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            UnifiedHeightmap = null;
            _terrains?.Clear();
            _tileMetadata?.Clear();
            _tileRects?.Clear();
        }
    }
}
