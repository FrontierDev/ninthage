using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Game.Editor.WorldEditor.Streaming;
using Game.Runtime.Shared;

namespace Game.Editor.WorldEditor
{
    public partial class WorldEditorWindow : EditorWindow
    {
        private async void BeginAsyncTerrainGeneration()
        {
            if (_heightmapTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Heightmap texture is not assigned.", "OK");
                return;
            }

            if (_continentMaskTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Continent Mask texture is not assigned.", "OK");
                return;
            }

            if (_heightmapTexture.width != _continentMaskTexture.width || _heightmapTexture.height != _continentMaskTexture.height)
            {
                EditorUtility.DisplayDialog("Error", "Heightmap and Continent Mask must have the same resolution.", "OK");
                return;
            }

            if (_worldWidth <= 0 || _worldWidth % 256 != 0)
            {
                EditorUtility.DisplayDialog("Error", "World width must be a positive multiple of 256.", "OK");
                return;
            }

            try
            {
                float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                int worldDepth = Mathf.RoundToInt(_worldWidth * aspectRatio);

                _asyncChunksX = _worldWidth / 256;
                _asyncChunksZ = worldDepth / 256;
                _asyncCurrentChunkIndex = 0;

                // Initialize tiled heightmap system for streaming
                _tiledHeightmapSystem?.Dispose();
                _asyncMaskedHeightmap = CreateMaskedHeightmap(_heightmapTexture, _continentMaskTexture);

                // Apply plateau generation before erosion to establish structure
                if (_enablePlateauGeneration)
                {
                    _terrainGenerationText = "Applying plateau generation to full heightmap...";
                    _asyncMaskedHeightmap = await ApplyPlateauGenerationAsync(_asyncMaskedHeightmap, _worldWidth, worldDepth);
                }

                // Setup erosion modes and apply to full heightmap if needed
                _asyncSelectedErosionModes = new List<(IErosionMode, ErosionSettings)>();
                if (_enableErosion)
                {
                    for (int i = 0; i < _availableErosionModes.Count; i++)
                    {
                        if (_erosionModeEnabled[i])
                        {
                            IErosionMode mode = _availableErosionModes[i];
                            ErosionSettings settings = _erosionModeSettings[mode.ModeName];
                            _asyncSelectedErosionModes.Add((mode, settings));
                        }
                    }

                    // Apply erosion to the full heightmap for consistency
                    if (_asyncSelectedErosionModes.Count > 0)
                    {
                        _terrainGenerationText = "Applying erosion to full heightmap...";
                        _asyncMaskedHeightmap = await ApplyErosionToFullHeightmapAsync(_asyncMaskedHeightmap, _worldWidth, worldDepth);
                    }
                }

                _tiledHeightmapSystem = new TiledHeightmapSystem(_asyncMaskedHeightmap, _worldWidth, worldDepth);

                // Initialize async generation state with cancellation support
                _terrainGenerationCTS = new CancellationTokenSource();
                _isGeneratingTerrain = true;
                _cancelTerrainGeneration = false;
                _terrainGenerationProgress = 0f;
                _terrainGenerationText = "Initializing terrain generation...";

                // Start async generation pipeline
                _ = GenerateTerrainPipelineAsync(_terrainGenerationCTS.Token);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to start terrain generation:\n\n{ex.Message}", "OK");
                Debug.LogException(ex);
                ResetAsyncTerrainGeneration();
            }
        }

        private async Task GenerateTerrainPipelineAsync(CancellationToken cancellationToken)
        {
            try
            {
                int totalChunks = _asyncChunksX * _asyncChunksZ;

                // Count chunks to generate if in selective mode
                int chunksToGenerate = totalChunks;
                if (_selectiveGenerationMode && _chunkMarkedForGeneration != null)
                {
                    chunksToGenerate = 0;
                    for (int x = 0; x < _asyncChunksX; x++)
                    {
                        for (int z = 0; z < _asyncChunksZ; z++)
                        {
                            if (_chunkMarkedForGeneration[x, z])
                                chunksToGenerate++;
                        }
                    }
                }

                var completedChunks = 0;

                // Process chunks in smaller batches to maintain UI responsiveness
                const int batchSize = 4; // Smaller batches for better UI response
                var semaphore = new SemaphoreSlim(batchSize, batchSize);

                _terrainGenerationProgress = 0f;
                _terrainGenerationText = "Starting terrain generation...";

                // Build optimized chunk list - if selective mode, only iterate marked chunks instead of all chunks
                List<(int, int)> chunksToProcess = new List<(int, int)>();
                if (_selectiveGenerationMode && _chunkMarkedForGeneration != null)
                {
                    for (int x = 0; x < _asyncChunksX; x++)
                    {
                        for (int z = 0; z < _asyncChunksZ; z++)
                        {
                            if (_chunkMarkedForGeneration[x, z])
                                chunksToProcess.Add((x, z));
                        }
                    }
                }
                else
                {
                    // Non-selective: iterate all chunks by index
                    for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                    {
                        int chunkX = chunkIndex % _asyncChunksX;
                        int chunkZ = chunkIndex / _asyncChunksX;
                        chunksToProcess.Add((chunkX, chunkZ));
                    }
                }

                // Process chunks in batches with progress tracking
                for (int batchStart = 0; batchStart < chunksToProcess.Count; batchStart += batchSize)
                {
                    var batchEnd = Mathf.Min(batchStart + batchSize, chunksToProcess.Count);
                    var batchTasks = new List<Task>();

                    // Create batch of tasks
                    for (int batchIndex = batchStart; batchIndex < batchEnd; batchIndex++)
                    {
                        var (chunkX, chunkZ) = chunksToProcess[batchIndex];
                        int chunkIndex = chunkZ * _asyncChunksX + chunkX; // Reconstruct index for logging

                        int currentChunkX = chunkX; // Capture for closure
                        int currentChunkZ = chunkZ;
                        int currentIndex = chunkIndex;

                        var chunkTask = ProcessChunkAsync(currentIndex, semaphore, cancellationToken)
                            .ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    // Mark chunk as generated
                                    if (_chunkGenerated != null)
                                        _chunkGenerated[currentChunkX, currentChunkZ] = true;

                                    var completed = System.Threading.Interlocked.Increment(ref completedChunks);
                                    _terrainGenerationProgress = (float)completed / chunksToGenerate;
                                    _terrainGenerationText = $"Generated chunk {completed}/{chunksToGenerate}";
                                }
                            }, TaskScheduler.Current);
                        batchTasks.Add(chunkTask);
                    }

                    // Wait for this batch to complete
                    if (batchTasks.Count > 0)
                        await Task.WhenAll(batchTasks);

                    // Yield control back to Unity's main thread between batches
                    await Task.Delay(10, cancellationToken); // Small delay to let UI update
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    _terrainGenerationText = "Finalizing...";
                    await FinalizeTerrain();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Terrain generation was cancelled");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Terrain generation failed:\n\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
            finally
            {
                UnityEditor.EditorApplication.delayCall += () => ResetAsyncTerrainGeneration();
            }
        }

        private async Task ProcessChunkAsync(int chunkIndex, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                int chunkX = chunkIndex % _asyncChunksX;
                int chunkZ = chunkIndex / _asyncChunksX;

                cancellationToken.ThrowIfCancellationRequested();

                // Get heightmap data for this chunk using streaming system
                var chunkHeights = await _tiledHeightmapSystem.GetChunkHeightmapAsync(
                    chunkX, chunkZ, _asyncChunksX, _asyncChunksZ);
                cancellationToken.ThrowIfCancellationRequested();

                // Calculate actual chunk size based on world dimensions
                float chunkSize = (float)_worldWidth / _asyncChunksX;

                // Generate LOD terrain chunk with Unity LOD Group  
                var lodChunk = await TerrainLODGenerator.CreateLODTerrainChunkAsync(
                    chunkHeights,
                    chunkX,
                    chunkZ,
                    _maxHeight,
                    _seaLevel,
                    _waterPlaneOffset,
                    _waterMaterial,
                    _terrainMaterial,
                    chunkSize);

                // Save as prefab (must be done on main thread)
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (lodChunk != null)
                    {
                        // Add tile coordinate component to root
                        var tileComponent = lodChunk.AddComponent<TerrainTileComponent>();
                        tileComponent.TileX = chunkX;
                        tileComponent.TileZ = chunkZ;

                        string prefabPath = $"Assets/Prefabs/Terrain/Terrain_{chunkX}_{chunkZ}.prefab";
                        PrefabUtility.SaveAsPrefabAsset(lodChunk, prefabPath);
                        UnityEngine.Object.DestroyImmediate(lodChunk);
                    }
                };

                // Return heightmap array to pool
                HeightmapArrayPool.Return(chunkHeights);

                _asyncCurrentChunkIndex = chunkIndex + 1;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<Texture2D> ApplyPlateauGenerationAsync(Texture2D sourceTexture, int worldWidth, int worldDepth)
        {
            // Extract texture data on main thread
            var sourcePixels = sourceTexture.GetPixels();
            int textureWidth = sourceTexture.width;
            int textureHeight = sourceTexture.height;
            TextureFormat textureFormat = sourceTexture.format;

            float normalizedSeaLevel = _seaLevel / _maxHeight;
            float plateauRange = _mountainLevel - normalizedSeaLevel;
            float plateauStep = plateauRange / _plateauCount;

            UnityEngine.Debug.Log($"Applying plateau generation: {_plateauCount} levels between sea level {normalizedSeaLevel:F3} and mountain level {_mountainLevel:F3}");

            // Process plateaus on background thread
            var processedPixels = await Task.Run(() =>
            {
                Color[] resultPixels = new Color[sourcePixels.Length];

                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    Color pixel = sourcePixels[i];
                    float heightValue = pixel.r;

                    // Only apply plateaus between sea level and mountain level
                    if (heightValue >= normalizedSeaLevel && heightValue < _mountainLevel)
                    {
                        // Calculate which plateau level this height falls into
                        float relativeHeight = (heightValue - normalizedSeaLevel) / plateauRange;
                        int plateauLevel = Mathf.FloorToInt(relativeHeight * _plateauCount);
                        plateauLevel = Mathf.Clamp(plateauLevel, 0, _plateauCount - 1);

                        // Calculate target plateau height
                        float plateauHeight = normalizedSeaLevel + (plateauLevel * plateauStep) + (plateauStep * 0.5f);

                        // Apply sharpness - higher plateaus get sharper edges
                        float plateauFactor = (float)plateauLevel / (_plateauCount - 1); // 0 to 1
                        float currentSharpness = Mathf.Lerp(0.3f, _plateauSharpness, plateauFactor);

                        // Smooth transition to plateau height
                        float difference = plateauHeight - heightValue;
                        float adjustedHeight = heightValue + (difference * currentSharpness * 0.1f);

                        resultPixels[i] = new Color(adjustedHeight, pixel.g, pixel.b, pixel.a);
                    }
                    else
                    {
                        resultPixels[i] = pixel; // Keep original height for mountains and underwater
                    }
                }

                return resultPixels;
            });

            // Apply results on main thread
            Texture2D result = new Texture2D(textureWidth, textureHeight, textureFormat, false);
            result.SetPixels(processedPixels);
            result.Apply(false, false);
            return result;
        }

        private async Task<Texture2D> ApplyErosionToFullHeightmapAsync(Texture2D heightmapTexture, float worldWidth, float worldHeight)
        {
            // Extract texture data on main thread
            var pixels = heightmapTexture.GetPixels();
            int textureWidth = heightmapTexture.width;
            int textureHeight = heightmapTexture.height;

            UnityEngine.Debug.Log($"Applying erosion to full heightmap: {textureWidth}x{textureHeight}, {_asyncSelectedErosionModes.Count} modes");

            // Process erosion on background thread
            var erodedHeights = await Task.Run(() =>
            {
                // Convert texture to float array
                var heightArray = new float[pixels.Length];
                var originalHeights = new float[pixels.Length]; // Store original heights for plateau clamping
                for (int i = 0; i < pixels.Length; i++)
                {
                    heightArray[i] = pixels[i].r; // Use red channel for height
                    originalHeights[i] = pixels[i].r;
                }

                // Calculate plateau parameters for clamping
                float normalizedSeaLevel = _seaLevel / _maxHeight;
                float plateauRange = _mountainLevel - normalizedSeaLevel;
                float plateauStep = plateauRange / _plateauCount;

                // Apply each erosion mode to the full heightmap
                foreach (var (erosionMode, settings) in _asyncSelectedErosionModes)
                {
                    var erosionSettings = new ErosionSettings
                    {
                        intensity = settings.intensity,
                        iterations = settings.iterations,
                        strength = settings.strength,
                        radius = settings.radius,
                        randomSeed = settings.randomSeed,
                        seaLevel = settings.seaLevel,
                        worldWidth = worldWidth,
                        worldHeight = worldHeight
                    };

                    UnityEngine.Debug.Log($"Applying {erosionMode.ModeName} to full heightmap - intensity: {settings.intensity}, iterations: {settings.iterations}");

                    var erodedHeights = erosionMode.Erode(heightArray, textureWidth, textureHeight, erosionSettings);

                    // Apply plateau clamping if plateau generation is enabled
                    if (_enablePlateauGeneration)
                    {
                        for (int i = 0; i < erodedHeights.Length; i++)
                        {
                            float originalHeight = originalHeights[i];
                            float erodedHeight = erodedHeights[i];

                            // Only clamp within plateau range
                            if (originalHeight >= normalizedSeaLevel && originalHeight < _mountainLevel)
                            {
                                // Calculate which plateau level the original height belongs to
                                float relativeHeight = (originalHeight - normalizedSeaLevel) / plateauRange;
                                int plateauLevel = Mathf.FloorToInt(relativeHeight * _plateauCount);
                                plateauLevel = Mathf.Clamp(plateauLevel, 0, _plateauCount - 1);

                                // Calculate plateau bounds
                                float plateauMin = normalizedSeaLevel + (plateauLevel * plateauStep);
                                float plateauMax = normalizedSeaLevel + ((plateauLevel + 1) * plateauStep);

                                // Clamp eroded height to stay within the original plateau level
                                erodedHeights[i] = Mathf.Clamp(erodedHeight, plateauMin, plateauMax);
                            }
                        }
                    }

                    heightArray = erodedHeights;
                }

                return heightArray;
            });

            // Create new texture on main thread
            var newTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);
            var newPixels = new Color[erodedHeights.Length];
            for (int i = 0; i < erodedHeights.Length; i++)
            {
                float height = erodedHeights[i];
                newPixels[i] = new Color(height, height, height, 1f);
            }
            newTexture.SetPixels(newPixels);
            newTexture.Apply();

            UnityEngine.Debug.Log($"Full heightmap erosion complete");
            return newTexture;
        }

        private void UpdateAsyncTerrainGeneration()
        {
            if (_cancelTerrainGeneration && _terrainGenerationCTS != null && !_terrainGenerationCTS.IsCancellationRequested)
            {
                _terrainGenerationCTS.Cancel();
                _terrainGenerationText = "Cancelling...";
            }
        }

        private async Task FinalizeTerrain()
        {
            await Task.Run(() =>
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    int totalChunks = _asyncChunksX * _asyncChunksZ;
                    EditorUtility.DisplayDialog(
                        "Success",
                        $"Generated {totalChunks} LOD terrain chunks with streaming system.\n\n" +
                        $"Saved to: Assets/Prefabs/Terrain/\n" +
                        $"Each chunk includes multiple LOD levels with Unity LOD Groups.",
                        "OK");

                    AssetDatabase.Refresh();
                };
            });
        }

        private void ResetAsyncTerrainGeneration()
        {
            _isGeneratingTerrain = false;
            _cancelTerrainGeneration = false;
            _terrainGenerationProgress = 0f;
            _terrainGenerationText = "";
            _asyncCurrentChunkIndex = 0;
            _asyncSelectedErosionModes = null;

            _terrainGenerationCTS?.Cancel();
            _terrainGenerationCTS?.Dispose();
            _terrainGenerationCTS = null;

            _tiledHeightmapSystem?.Dispose();
            _tiledHeightmapSystem = null;

            if (_asyncMaskedHeightmap != null)
            {
                UnityEngine.Object.DestroyImmediate(_asyncMaskedHeightmap);
                _asyncMaskedHeightmap = null;
            }

            // Clear memory pools
            HeightmapArrayPool.Clear();
        }
        private void GenerateTerrainChunks()
        {
            if (_heightmapTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Heightmap texture is not assigned.", "OK");
                return;
            }

            if (_continentMaskTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Continent Mask texture is not assigned.", "OK");
                return;
            }

            if (_heightmapTexture.width != _continentMaskTexture.width || _heightmapTexture.height != _continentMaskTexture.height)
            {
                EditorUtility.DisplayDialog("Error", "Heightmap and Continent Mask must have the same resolution.", "OK");
                return;
            }

            if (_worldWidth <= 0 || _worldWidth % 256 != 0)
            {
                EditorUtility.DisplayDialog("Error", "World width must be a positive multiple of 256.", "OK");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Terrain Generation", "Processing heightmap...", 0f);

                float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                int worldDepth = Mathf.RoundToInt(_worldWidth * aspectRatio);

                int chunksX = _worldWidth / 256;
                int chunksZ = worldDepth / 256;

                EditorUtility.DisplayProgressBar("Terrain Generation", "Masking heightmap with continent...", 0.2f);

                // Create masked heightmap using continent mask
                Texture2D maskedHeightmap = CreateMaskedHeightmap(_heightmapTexture, _continentMaskTexture);

                EditorUtility.DisplayProgressBar("Terrain Generation", "Generating chunks...", 0.3f);

                List<(IErosionMode, ErosionSettings)> selectedErosionModes = new List<(IErosionMode, ErosionSettings)>();

                if (_enableErosion)
                {
                    for (int i = 0; i < _availableErosionModes.Count; i++)
                    {
                        if (_erosionModeEnabled[i])
                        {
                            IErosionMode mode = _availableErosionModes[i];
                            ErosionSettings settings = _erosionModeSettings[mode.ModeName];
                            selectedErosionModes.Add((mode, settings));
                        }
                    }
                }

                TerrainChunkGenerator.GenerateChunks(
                    maskedHeightmap,
                    chunksX,
                    chunksZ,
                    _maxHeight,
                    _seaLevel,
                    _waterPlaneOffset,
                    _waterMaterial,
                    selectedErosionModes);

                // Cleanup temporary texture
                UnityEngine.Object.DestroyImmediate(maskedHeightmap);

                EditorUtility.DisplayProgressBar("Terrain Generation", "Saving prefabs...", 0.7f);
                EditorUtility.DisplayProgressBar("Terrain Generation", "Complete!", 1f);
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Generated {chunksX * chunksZ} terrain chunks.\n\n" +
                    $"Saved to: Assets/Prefabs/Terrain/",
                    "OK");

                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Terrain generation failed:\n\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
        }

        private Texture2D CreateMaskedHeightmap(Texture2D heightmapTexture, Texture2D continentMask)
        {
            int width = heightmapTexture.width;
            int height = heightmapTexture.height;

            Texture2D maskedHeightmap = new Texture2D(width, height, TextureFormat.RGB24, false);
            maskedHeightmap.wrapMode = TextureWrapMode.Clamp;
            maskedHeightmap.filterMode = FilterMode.Point;

            Color[] heightPixels = GetTexturePixels(heightmapTexture, width, height);
            Color[] continentPixels = GetTexturePixels(continentMask, width, height);
            Color[] resultPixels = new Color[heightPixels.Length];

            // Use sea level as the minimum land height

            for (int i = 0; i < heightPixels.Length; i++)
            {
                Color continentPixel = continentPixels[i];
                bool isLand = continentPixel.r > 0.5f || continentPixel.g > 0.5f || continentPixel.b > 0.5f;

                if (isLand)
                {
                    // Land height = sea level + heightmap value * maxHeight
                    float grayscaleHeight = heightPixels[i].grayscale;
                    float mappedHeight = _seaLevel + (grayscaleHeight * _maxHeight);
                    float normalizedHeight = mappedHeight / (_seaLevel + _maxHeight);
                    resultPixels[i] = new Color(normalizedHeight, normalizedHeight, normalizedHeight, 1f);
                }
                else
                {
                    // Set sea to black (height 0)
                    resultPixels[i] = new Color(0f, 0f, 0f, 1f);
                }
            }

            maskedHeightmap.SetPixels(resultPixels);
            maskedHeightmap.Apply(false, false);
            return maskedHeightmap;
        }

        private Color[] GetTexturePixels(Texture2D texture, int width, int height)
        {
            // Try direct read first
            try
            {
                Color[] pixels = texture.GetPixels();
                if (pixels != null && pixels.Length == width * height)
                {
                    return pixels;
                }
            }
            catch { }

            // Fallback: create readable copy via RenderTexture
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

        private void PlaceTerrainInScene()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Placing Terrain", "Loading prefabs...", 0f);

                GameObject containerGO = FindOrCreateContainer();
                if (containerGO == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to create container GameObject.", "OK");
                    return;
                }

                int childCount = containerGO.transform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(containerGO.transform.GetChild(i).gameObject);
                }

                // If in selective generation mode, only place marked chunks
                bool[,] chunksToPlace = (_selectiveGenerationMode && _chunkMarkedForGeneration != null)
                    ? _chunkMarkedForGeneration
                    : null;

                int instantiatedCount = TerrainChunkPlacer.PlaceChunksInScene(containerGO, chunksToPlace);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Instantiated {instantiatedCount} terrain chunks in the scene.",
                    "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Failed to place terrain:\n\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
        }

        private void ClearTerrainPrefabs()
        {
            if (EditorUtility.DisplayDialog(
                "Clear Terrain Prefabs",
                "Delete all terrain prefabs in Assets/Prefabs/Terrain/?",
                "Delete",
                "Cancel"))
            {
                string prefabPath = "Assets/Prefabs/Terrain";
                if (AssetDatabase.IsValidFolder(prefabPath))
                {
                    var guids = AssetDatabase.FindAssets("*", new[] { prefabPath });
                    foreach (var guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Success", "Cleared terrain prefabs.", "OK");
                }
            }
        }

        private GameObject FindOrCreateContainer()
        {
            GameObject existing = GameObject.Find("[Editor] WorldTerrain");
            if (existing != null)
                return existing;

            GameObject container = new GameObject("[Editor] WorldTerrain");
            return container;
        }
    }
}
