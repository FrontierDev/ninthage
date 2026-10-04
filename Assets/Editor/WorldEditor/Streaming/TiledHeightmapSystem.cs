using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Game.Editor.WorldEditor.Streaming
{
    /// <summary>
    /// Manages streaming heightmap tiles for massive terrain generation.
    /// Uses 256x256 tiles with disk caching and memory pooling.
    /// </summary>
    public class TiledHeightmapSystem : IDisposable
    {
        public const int TILE_SIZE = 256;
        private const string CACHE_DIRECTORY = "Temp/HeightmapCache";
        private const int MAX_CACHED_TILES = 16;

        private readonly Texture2D _sourceHeightmap;
        private readonly float[] _sourceHeightmapData; // Pre-processed pixel data for thread safety
        private readonly int _sourceWidth;
        private readonly int _sourceHeight;
        private readonly int _tilesX;
        private readonly int _tilesZ;
        private readonly int _worldWidth;  // Desired world size
        private readonly int _worldDepth;  // Desired world depth
        private readonly Dictionary<Vector2Int, CachedTile> _tileCache = new();
        private readonly Queue<Vector2Int> _lruQueue = new();

        public int TotalTilesX => _tilesX;
        public int TotalTilesZ => _tilesZ;
        public Vector2Int WorldSize => new(_tilesX * TILE_SIZE, _tilesZ * TILE_SIZE);

        private struct CachedTile
        {
            public float[] heightdata;
            public long lastAccessTime;
            public bool isDirty;
        }

        public TiledHeightmapSystem(Texture2D sourceHeightmap, int worldWidth, int worldDepth)
        {
            _sourceHeightmap = sourceHeightmap ?? throw new ArgumentNullException(nameof(sourceHeightmap));
            _worldWidth = worldWidth;
            _worldDepth = worldDepth;

            _sourceWidth = sourceHeightmap.width;
            _sourceHeight = sourceHeightmap.height;

            // Calculate tile layout based on WORLD size, not texture size
            _tilesX = Mathf.CeilToInt((float)_worldWidth / TILE_SIZE);
            _tilesZ = Mathf.CeilToInt((float)_worldDepth / TILE_SIZE);

            // Pre-process heightmap data on main thread for thread safety
            _sourceHeightmapData = PreprocessHeightmapData(sourceHeightmap);

            EnsureCacheDirectory();
        }

        /// <summary>
        /// Preprocesses the heightmap texture into a float array on the main thread.
        /// This allows background threads to access the data safely.
        /// </summary>
        private static float[] PreprocessHeightmapData(Texture2D heightmap)
        {
            Color[] pixels = GetTexturePixelsSafe(heightmap);
            var heightData = new float[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                heightData[i] = pixels[i].grayscale;
            }

            return heightData;
        }

        /// <summary>
        /// Safely gets texture pixels, handling both readable and non-readable textures.
        /// </summary>
        private static Color[] GetTexturePixelsSafe(Texture2D texture)
        {
            try
            {
                // Try direct read first
                if (texture.isReadable)
                {
                    return texture.GetPixels();
                }
            }
            catch { }

            // Fallback: create readable copy via RenderTexture
            int width = texture.width;
            int height = texture.height;

            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(texture, tempRT);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tempRT;

            Texture2D readableTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tempRT);

            Color[] result = readableTexture.GetPixels();
            UnityEngine.Object.DestroyImmediate(readableTexture);

            return result;
        }

        /// <summary>
        /// Gets a tile asynchronously, checking memory cache, disk cache, then generating if needed.
        /// </summary>
        public async Task<float[]> GetTileAsync(int tileX, int tileZ)
        {
            var tileKey = new Vector2Int(tileX, tileZ);

            // Check memory cache first
            if (_tileCache.TryGetValue(tileKey, out var cachedTile))
            {
                cachedTile.lastAccessTime = DateTime.UtcNow.Ticks;
                _tileCache[tileKey] = cachedTile;
                return cachedTile.heightdata;
            }

            // Try loading from disk cache
            var diskPath = GetTileCachePath(tileX, tileZ);
            if (File.Exists(diskPath))
            {
                var heightData = await LoadTileFromDiskAsync(diskPath);
                CacheTile(tileKey, heightData, false);
                return heightData;
            }

            // Generate tile from source heightmap
            var generatedData = await GenerateTileAsync(tileX, tileZ);
            CacheTile(tileKey, generatedData, true);

            return generatedData;
        }

        /// <summary>
        /// Gets heightmap data for a chunk region, potentially spanning multiple tiles.
        /// </summary>
        public async Task<float[]> GetChunkHeightmapAsync(int chunkX, int chunkZ, int totalChunksX, int totalChunksZ)
        {
            // Calculate which tile contains this chunk
            int tileX = (chunkX * TILE_SIZE) / TILE_SIZE;
            int tileZ = (chunkZ * TILE_SIZE) / TILE_SIZE;

            var tileData = await GetTileAsync(tileX, tileZ);

            // Extract chunk region from tile (assuming 1:1 mapping for now)
            // For more complex mappings, we'd need to sample across tile boundaries
            return ExtractChunkFromTile(tileData, chunkX % 1, chunkZ % 1);
        }

        /// <summary>
        /// Saves a processed tile to disk cache.
        /// </summary>
        public async Task SaveTileAsync(int tileX, int tileZ, float[] heightData)
        {
            var tileKey = new Vector2Int(tileX, tileZ);
            var diskPath = GetTileCachePath(tileX, tileZ);

            await SaveTileToDiskAsync(heightData, diskPath);
            CacheTile(tileKey, heightData, false);
        }

        private async Task<float[]> GenerateTileAsync(int tileX, int tileZ)
        {
            return await Task.Run(() =>
            {
                var heightData = new float[TILE_SIZE * TILE_SIZE];

                // Calculate sampling coordinates using pre-processed heightmap data
                for (int z = 0; z < TILE_SIZE; z++)
                {
                    for (int x = 0; x < TILE_SIZE; x++)
                    {
                        // Map world coordinates to source heightmap coordinates
                        // This properly stretches the heightmap to fill the world size
                        float worldX = (tileX * TILE_SIZE + x);
                        float worldZ = (tileZ * TILE_SIZE + z);

                        // Convert world coordinates to normalized coordinates (0-1)
                        float normalizedX = worldX / _worldWidth;
                        float normalizedZ = worldZ / _worldDepth;

                        // Map to source heightmap coordinates
                        float sourceX = normalizedX * (_sourceWidth - 1);
                        float sourceZ = normalizedZ * (_sourceHeight - 1);

                        // Bilinear interpolation for smooth sampling (fixes blockiness)
                        int x0 = Mathf.FloorToInt(sourceX);
                        int z0 = Mathf.FloorToInt(sourceZ);
                        int x1 = Mathf.Min(x0 + 1, _sourceWidth - 1);
                        int z1 = Mathf.Min(z0 + 1, _sourceHeight - 1);

                        float fx = sourceX - x0;
                        float fz = sourceZ - z0;

                        // Sample four corners
                        float h00 = _sourceHeightmapData[z0 * _sourceWidth + x0];
                        float h10 = _sourceHeightmapData[z0 * _sourceWidth + x1];
                        float h01 = _sourceHeightmapData[z1 * _sourceWidth + x0];
                        float h11 = _sourceHeightmapData[z1 * _sourceWidth + x1];

                        // Bilinear interpolation
                        float h0 = Mathf.Lerp(h00, h10, fx);
                        float h1 = Mathf.Lerp(h01, h11, fx);
                        float interpolatedHeight = Mathf.Lerp(h0, h1, fz);

                        heightData[z * TILE_SIZE + x] = interpolatedHeight;
                    }
                }

                return heightData;
            });
        }

        private float[] ExtractChunkFromTile(float[] tileData, int chunkOffsetX, int chunkOffsetZ)
        {
            // For 1:1 tile-to-chunk mapping, just return the tile data
            if (chunkOffsetX == 0 && chunkOffsetZ == 0)
                return (float[])tileData.Clone();

            // For more complex mappings, extract the specific region
            var chunkData = new float[TILE_SIZE * TILE_SIZE];
            Array.Copy(tileData, chunkData, chunkData.Length);
            return chunkData;
        }

        private void CacheTile(Vector2Int tileKey, float[] heightData, bool shouldSaveToDisk)
        {
            // Enforce cache size limit
            while (_tileCache.Count >= MAX_CACHED_TILES)
            {
                var oldestKey = _lruQueue.Dequeue();
                _tileCache.Remove(oldestKey);
            }

            var cachedTile = new CachedTile
            {
                heightdata = (float[])heightData.Clone(),
                lastAccessTime = DateTime.UtcNow.Ticks,
                isDirty = shouldSaveToDisk
            };

            _tileCache[tileKey] = cachedTile;
            _lruQueue.Enqueue(tileKey);

            // Save dirty tiles to disk asynchronously
            if (shouldSaveToDisk)
            {
                var diskPath = GetTileCachePath(tileKey.x, tileKey.y);
                _ = SaveTileToDiskAsync(heightData, diskPath);
            }
        }

        private async Task<float[]> LoadTileFromDiskAsync(string path)
        {
            return await Task.Run(() =>
            {
                var bytes = File.ReadAllBytes(path);
                var heightData = new float[TILE_SIZE * TILE_SIZE];

                Buffer.BlockCopy(bytes, 0, heightData, 0, bytes.Length);
                return heightData;
            });
        }

        private async Task SaveTileToDiskAsync(float[] heightData, string path)
        {
            await Task.Run(() =>
            {
                var bytes = new byte[heightData.Length * sizeof(float)];
                Buffer.BlockCopy(heightData, 0, bytes, 0, bytes.Length);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);
            });
        }

        private string GetTileCachePath(int tileX, int tileZ)
        {
            var hashCode = _sourceHeightmap.GetInstanceID();
            return Path.Combine(CACHE_DIRECTORY, $"heightmap_{hashCode}", $"tile_{tileX}_{tileZ}.bin");
        }

        private void EnsureCacheDirectory()
        {
            var cacheDir = Path.Combine(CACHE_DIRECTORY, $"heightmap_{_sourceHeightmap.GetInstanceID()}");
            Directory.CreateDirectory(cacheDir);
        }

        public void Dispose()
        {
            // Save all dirty tiles before disposing
            foreach (var kvp in _tileCache)
            {
                if (kvp.Value.isDirty)
                {
                    var path = GetTileCachePath(kvp.Key.x, kvp.Key.y);
                    SaveTileToDiskAsync(kvp.Value.heightdata, path).Wait(1000); // 1 second timeout
                }
            }

            _tileCache.Clear();
            _lruQueue.Clear();
        }
    }
}