using UnityEngine;
using UnityEditor;
using System.IO;
using Game.Runtime.Shared;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Drives the TerrainAutoPainter compute shader to classify a Unity Terrain's
    /// heightmap and write the results directly into the terrain's splatmaps
    /// (alphamaps). Called from TerrainTileComponentEditor when the user clicks
    /// "Auto-Paint Terrain".
    ///
    /// Classification rules (slope-only, three bands):
    ///   1. slope > cliffThreshold              → layer 3 (cliff / exposed rock)
    ///   2. slope > slopeThreshold (≤ cliff)    → layer 1 (mid-slope / transition)
    ///   3. otherwise                           → layer 0 (base terrain)
    ///
    /// Layers 4-7 (control_1) are zeroed so they remain available for manual
    /// painting without being overwritten by auto-paint.
    /// </summary>
    public static class TerrainAutoPainterHelper
    {
        private const int KernelThreadGroupSize = 8;
        private const int RequiredLayerCount = 8;

        // Folders for generated layer assets and debug output.
        private const string LayerAssetFolder = "Assets/Editor/Terrain/Layers";
        private const string DebugOutputFolder = "Assets/Editor/Terrain/Debug";

        // Default thresholds in degrees. Public so the inspector can seed sliders.
        public const float DefaultSlopeThreshold = 15f;
        public const float DefaultTalusThreshold = 25f;
        public const float DefaultCliffThreshold = 40f;
        public const float DefaultBlendWidth = 5f; // half-width of smoothstep band in degrees
        public const int DefaultAlphamapResolution = 1024;
        public const float DefaultTextureTileSize = 15f; // world metres per texture tile
        // Snow / ice defaults. Elevations in world metres; slopes in degrees.
        public const float DefaultSnowElevation = 800f;
        public const float DefaultSnowHeightBlend = 30f;
        public const float DefaultSnowSlopeLimit = 35f;
        public const float DefaultSnowSlopeBlend = 5f;
        public const float DefaultIceElevation = 1200f;
        public const float DefaultIceHeightBlend = 30f;
        public const float DefaultIceSlopeLimit = 20f;
        public const float DefaultIceSlopeBlend = 5f;
        private const string SliceCopyShaderPath = "Assets/Shaders/TerrainArraySliceCopy.shader";

        // Cached Shader.PropertyToID values to avoid repeated string lookups.
        private static readonly int HeightmapId = Shader.PropertyToID("_Heightmap");
        private static readonly int ControlMap0Id = Shader.PropertyToID("_ControlMap0");
        private static readonly int ControlMap1Id = Shader.PropertyToID("_ControlMap1");
        private static readonly int CliffThresholdId = Shader.PropertyToID("_CliffThreshold");
        private static readonly int TalusThresholdId = Shader.PropertyToID("_TalusThreshold");
        private static readonly int SlopeThresholdId = Shader.PropertyToID("_SlopeThreshold");
        private static readonly int BlendWidthId = Shader.PropertyToID("_BlendWidth");
        private static readonly int TalusCurvatureBlendId = Shader.PropertyToID("_TalusCurvatureBlend");
        private static readonly int HeightmapSizeId = Shader.PropertyToID("_HeightmapSize");
        private static readonly int AlphamapSizeId = Shader.PropertyToID("_AlphamapSize");
        private static readonly int TerrainBaseYId = Shader.PropertyToID("_TerrainBaseY");
        private static readonly int TerrainHeightRangeId = Shader.PropertyToID("_TerrainHeightRange");
        private static readonly int SnowElevationId = Shader.PropertyToID("_SnowElevation");
        private static readonly int SnowHeightBlendId = Shader.PropertyToID("_SnowHeightBlend");
        private static readonly int SnowSlopeLimitId = Shader.PropertyToID("_SnowSlopeLimit");
        private static readonly int SnowSlopeBlendId = Shader.PropertyToID("_SnowSlopeBlend");
        private static readonly int IceElevationId = Shader.PropertyToID("_IceElevation");
        private static readonly int IceHeightBlendId = Shader.PropertyToID("_IceHeightBlend");
        private static readonly int IceSlopeLimitId = Shader.PropertyToID("_IceSlopeLimit");
        private static readonly int IceSlopeBlendId = Shader.PropertyToID("_IceSlopeBlend");

        /// <summary>
        /// Runs the auto-paint compute shader and applies the result to
        /// <paramref name="tile"/>'s Unity Terrain alphamaps.
        /// </summary>
        /// <param name="tile">The tile whose child Terrain will be painted.</param>
        /// <param name="shader">The TerrainAutoPainter compute shader asset.</param>
        /// <param name="slopeThreshold">
        /// Angle in degrees above which a texel becomes mid-slope (layer 1). Must be &lt; talusThreshold.
        /// </param>
        /// <param name="talusThreshold">
        /// Angle in degrees above which a texel becomes talus/scree (layer 2). Must be &lt; cliffThreshold.
        /// </param>
        /// <param name="cliffThreshold">
        /// Sobel gradient magnitude above which a texel is classified as cliff.
        /// </param>
        public static void PaintTile(
            TerrainTileComponent tile,
            ComputeShader shader,
            float slopeThreshold = DefaultSlopeThreshold,
            float talusThreshold = DefaultTalusThreshold,
            float cliffThreshold = DefaultCliffThreshold,
            float blendWidth = DefaultBlendWidth,
            int alphamapResolution = DefaultAlphamapResolution,
            float textureTileSize = DefaultTextureTileSize,
            float snowElevation = DefaultSnowElevation,
            float snowHeightBlend = DefaultSnowHeightBlend,
            float snowSlopeLimit = DefaultSnowSlopeLimit,
            float snowSlopeBlend = DefaultSnowSlopeBlend,
            float iceElevation = DefaultIceElevation,
            float iceHeightBlend = DefaultIceHeightBlend,
            float iceSlopeLimit = DefaultIceSlopeLimit,
            float iceSlopeBlend = DefaultIceSlopeBlend)
        {
            if (tile == null)
                throw new System.ArgumentNullException(nameof(tile));
            if (shader == null)
                throw new System.ArgumentNullException(nameof(shader));

            Terrain terrain = tile.GetComponentInChildren<Terrain>();
            if (terrain == null)
                throw new System.InvalidOperationException(
                    "No Terrain component found on or under the tile GameObject.");

            TerrainData terrainData = terrain.terrainData;

            // ── 1. Ensure exactly 8 valid TerrainLayer assets are assigned ─────────
            // Creates placeholder TerrainLayer assets (no diffuse texture) for any
            // null or missing slots. Guarantees two splatmap textures in the terrain
            // data and clears the Missing reference warnings in the terrain inspector.
            // Placeholder textures are intentionally empty: the custom terrain shader
            // reads from the ZoneDefinition Texture2DArray rather than TerrainLayers.
            EnsureEightTerrainLayers(terrainData, tile.ZoneDefinition, textureTileSize);

            // Set alphamap resolution before reading dimensions — Unity clamps to
            // the nearest valid power-of-2 between 16 and 4096. Setting it here
            // ensures the splatmap texture is (re)created at the requested size
            // before we dispatch the compute shader or write alphamaps.
            int clampedRes = Mathf.Clamp(Mathf.ClosestPowerOfTwo(alphamapResolution), 16, 4096);
            if (terrainData.alphamapResolution != clampedRes)
                terrainData.alphamapResolution = clampedRes;

            int hmSize = terrainData.heightmapResolution;
            int amWidth = terrainData.alphamapWidth;
            int amHeight = terrainData.alphamapHeight;
            int layerCount = terrainData.alphamapLayers; // Always 8 after EnsureEightTerrainLayers.

            // ── 2. Export the terrain heightmap into a GPU-readable Texture2D ─────
            float[,] heights = terrainData.GetHeights(0, 0, hmSize, hmSize);

            // Use RFloat so the single-channel height value is preserved with full
            // float precision. Unity's compute binding exposes it as Texture2D<float4>
            // with height in the .r component.
            Texture2D heightmapTex = new Texture2D(hmSize, hmSize, TextureFormat.RFloat, mipChain: false);
            heightmapTex.filterMode = FilterMode.Bilinear;

            Color[] hmPixels = new Color[hmSize * hmSize];
            for (int z = 0; z < hmSize; z++)
            {
                for (int x = 0; x < hmSize; x++)
                {
                    // TerrainData.GetHeights returns [row, col] → [z, x].
                    float h = heights[z, x];
                    hmPixels[z * hmSize + x] = new Color(h, 0f, 0f, 1f);
                }
            }

            heightmapTex.SetPixels(hmPixels);
            heightmapTex.Apply(updateMipmaps: false);

            // ── 2. Create RenderTextures for compute shader output ────────────────
            RenderTexture rt0 = new RenderTexture(amWidth, amHeight, 0, RenderTextureFormat.ARGB32);
            rt0.enableRandomWrite = true;
            rt0.Create();

            RenderTexture rt1 = new RenderTexture(amWidth, amHeight, 0, RenderTextureFormat.ARGB32);
            rt1.enableRandomWrite = true;
            rt1.Create();

            // ── 3. Bind parameters and dispatch ──────────────────────────────────
            int kernel = shader.FindKernel("ClassifyTerrain");

            shader.SetTexture(kernel, HeightmapId, heightmapTex);
            shader.SetTexture(kernel, ControlMap0Id, rt0);
            shader.SetTexture(kernel, ControlMap1Id, rt1);

            // Convert both degree thresholds to raw Sobel magnitude.
            // Formula: sobelMag = tan(angleDeg × π/180) × 4 × size.x / (size.y × (hmSize−1))
            float scale = 4f * terrainData.size.x / (terrainData.size.y * (hmSize - 1));
            float rawSlopeThreshold = Mathf.Tan(slopeThreshold * Mathf.Deg2Rad) * scale;
            float rawTalusThreshold = Mathf.Tan(talusThreshold * Mathf.Deg2Rad) * scale;
            float rawCliffThreshold = Mathf.Tan(cliffThreshold * Mathf.Deg2Rad) * scale;
            float rawBlendWidth = Mathf.Tan(blendWidth * Mathf.Deg2Rad) * scale;
            float rawSnowSlopeLimit = Mathf.Tan(snowSlopeLimit * Mathf.Deg2Rad) * scale;
            float rawSnowSlopeBlend = Mathf.Tan(snowSlopeBlend * Mathf.Deg2Rad) * scale;
            float rawIceSlopeLimit = Mathf.Tan(iceSlopeLimit * Mathf.Deg2Rad) * scale;
            float rawIceSlopeBlend = Mathf.Tan(iceSlopeBlend * Mathf.Deg2Rad) * scale;

            shader.SetFloat(SlopeThresholdId, rawSlopeThreshold);
            shader.SetFloat(TalusThresholdId, rawTalusThreshold);
            shader.SetFloat(CliffThresholdId, rawCliffThreshold);
            shader.SetFloat(BlendWidthId, rawBlendWidth);
            // Auto-derived: at a cliff base the Laplacian is ~rawCliffThreshold/(hmSize-1)
            shader.SetFloat(TalusCurvatureBlendId, rawCliffThreshold / (float)(hmSize - 1));
            shader.SetInt(HeightmapSizeId, hmSize);
            shader.SetInt(AlphamapSizeId, amWidth); // Unity alphamaps are square
            shader.SetFloat(TerrainBaseYId, terrain.transform.position.y);
            shader.SetFloat(TerrainHeightRangeId, terrainData.size.y);
            shader.SetFloat(SnowElevationId, snowElevation);
            shader.SetFloat(SnowHeightBlendId, snowHeightBlend);
            shader.SetFloat(SnowSlopeLimitId, rawSnowSlopeLimit);
            shader.SetFloat(SnowSlopeBlendId, rawSnowSlopeBlend);
            shader.SetFloat(IceElevationId, iceElevation);
            shader.SetFloat(IceHeightBlendId, iceHeightBlend);
            shader.SetFloat(IceSlopeLimitId, rawIceSlopeLimit);
            shader.SetFloat(IceSlopeBlendId, rawIceSlopeBlend);

            int groupX = Mathf.CeilToInt((float)amWidth / KernelThreadGroupSize);
            int groupY = Mathf.CeilToInt((float)amHeight / KernelThreadGroupSize);
            shader.Dispatch(kernel, groupX, groupY, 1);

            // ── 4. Read results back to CPU ───────────────────────────────────────
            Texture2D readback0 = ReadRenderTexture(rt0, amWidth, amHeight);
            Texture2D readback1 = ReadRenderTexture(rt1, amWidth, amHeight);

            // ── 5. Save debug PNGs ────────────────────────────────────────────────
            // Written to Assets/Editor/Terrain/Debug/Tile_{x}_{z}/ so the output can
            // be inspected in the Project window without sampling the terrain directly.
            SaveDebugMaps(readback0, readback1, tile.TileX, tile.TileZ);

            // ── 6. Convert to float[,,] alphamap layout and apply ────────────────
            // Unity's alphamap array is indexed [height-row, width-col, layer].
            float[,,] alphamaps = new float[amHeight, amWidth, layerCount];

            for (int z = 0; z < amHeight; z++)
            {
                for (int x = 0; x < amWidth; x++)
                {
                    Color c0 = readback0.GetPixel(x, z);
                    Color c1 = readback1.GetPixel(x, z);

                    if (layerCount > 0) alphamaps[z, x, 0] = c0.r;
                    if (layerCount > 1) alphamaps[z, x, 1] = c0.g;
                    if (layerCount > 2) alphamaps[z, x, 2] = c0.b;
                    if (layerCount > 3) alphamaps[z, x, 3] = c0.a;
                    if (layerCount > 4) alphamaps[z, x, 4] = c1.r;
                    if (layerCount > 5) alphamaps[z, x, 5] = c1.g;
                    if (layerCount > 6) alphamaps[z, x, 6] = c1.b;
                    if (layerCount > 7) alphamaps[z, x, 7] = c1.a;
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);

            // Regenerate the terrain basemap — the precomputed low-res blend that
            // Unity uses beyond terrain.basemapDistance. Without this, the old baked
            // basemap persists and causes a visible hard boundary where Unity switches
            // from per-pixel splatmapping to the stale basemap.
            terrain.terrainData.SetBaseMapDirty();

            // Mark the TerrainData asset dirty so Unity knows to save it, then
            // flush all pending asset writes so the terrain updates in the scene.
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssetIfDirty(terrainData);

            // Notify the Terrain component itself so the scene view repaints.
            terrain.Flush();
            EditorUtility.SetDirty(terrain);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);

            // ── 7. Release GPU and temporary CPU resources ────────────────────────
            rt0.Release();
            rt1.Release();
            Object.DestroyImmediate(heightmapTex);
            Object.DestroyImmediate(readback0);
            Object.DestroyImmediate(readback1);
        }

        // Ensures the terrain has exactly RequiredLayerCount (8) valid TerrainLayer
        // assets shared by all tiles using the same ZoneDefinition. Assets live
        // under Assets/Editor/Terrain/Layers/{zoneName}/ and are reused across tiles.
        // Always assigns the zone's shared layers — no preservation of prior terrain
        // layers — so that every tile painted with this zone gets the correct set.
        private static void EnsureEightTerrainLayers(
            TerrainData terrainData, ZoneDefinition zone, float textureTileSize = DefaultTextureTileSize)
        {
            TerrainLayer[] layers = new TerrainLayer[RequiredLayerCount];

            // Per-zone folder — shared across all tiles that use this zone.
            string zoneName = zone != null ? zone.zoneName : "Default";
            string zoneFolder = $"{LayerAssetFolder}/{zoneName}";
            string fullDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", zoneFolder));
            Directory.CreateDirectory(fullDir);

            bool anyDirty = false;

            for (int i = 0; i < RequiredLayerCount; i++)
            {
                // Load or create the shared TerrainLayer asset for this zone.
                string layerPath = $"{zoneFolder}/Layer_{i}.terrainlayer";
                TerrainLayer reused = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if (reused != null)
                {
                    layers[i] = reused;
                    // Update tileSize in case it changed since the asset was created.
                    if (reused.tileSize != new Vector2(textureTileSize, textureTileSize))
                    {
                        reused.tileSize = new Vector2(textureTileSize, textureTileSize);
                        EditorUtility.SetDirty(reused);
                        anyDirty = true;
                    }
                }
                else
                {
                    TerrainLayer layer = new TerrainLayer();
                    layer.name = $"{zoneName}_Layer_{i}";
                    layer.tileSize = new Vector2(textureTileSize, textureTileSize);
                    layer.metallic = 0f;
                    layer.smoothness = 0f;
                    AssetDatabase.CreateAsset(layer, layerPath);
                    layers[i] = layer;
                    anyDirty = true;
                }

                if (zone == null)
                    continue;

                bool layerDirty = false;

                // ── Albedo (diffuse) ─────────────────────────────────────────
                if (zone.albedoArray != null && i < zone.albedoArray.depth)
                {
                    Texture2D slice = ExtractArraySlice(
                        zone.albedoArray, i,
                        $"{zoneFolder}/Albedo_{i}.png",
                        linear: false);
                    if (slice != null)
                    {
                        layers[i].diffuseTexture = slice;
                        layerDirty = true;
                    }
                }

                // ── Normal map ───────────────────────────────────────────────
                if (zone.normalArray != null && i < zone.normalArray.depth)
                {
                    Texture2D slice = ExtractArraySlice(
                        zone.normalArray, i,
                        $"{zoneFolder}/Normal_{i}.png",
                        linear: true);
                    if (slice != null)
                    {
                        layers[i].normalMapTexture = slice;
                        layerDirty = true;
                    }
                }

                if (layerDirty)
                {
                    EditorUtility.SetDirty(layers[i]);
                    anyDirty = true;
                }
            }

            if (anyDirty)
                AssetDatabase.SaveAssets();

            terrainData.terrainLayers = layers;
        }

        // Extracts slice <paramref name="sliceIndex"/> from a Texture2DArray,
        // saves it as a PNG asset at <paramref name="assetPath"/>, imports it
        // with correct settings, and returns the imported Texture2D.
        //
        // Requires the source Texture2DArray to have Read/Write enabled.
        // Returns null and logs a warning if the array is not CPU-readable.
        // Extracts a single slice from a Texture2DArray via a graphics Blit so that
        // the standard sRGB sample/encode path is used on every platform.
        //
        // Pipeline for albedo (linear=false):
        //   Sample sRGB array  → GPU decodes sRGB→linear  → sRGB RT encodes linear→sRGB
        //   ReadPixels from sRGB RT → raw sRGB bytes  → import as sRGB → correct!
        //
        // Pipeline for normals (linear=true):
        //   Sample linear array → no conversion  → linear RT → ReadPixels → linear PNG
        //   import as linear → correct!
        private static Texture2D ExtractArraySlice(
            Texture2DArray sourceArray, int sliceIndex, string assetPath, bool linear)
        {
            // Invalidate the cached PNG if the source array asset is newer.
            // This ensures re-extraction runs automatically whenever the
            // Texture2DArray is rebuilt, without requiring manual file deletion.
            string sourcePath = AssetDatabase.GetAssetPath(sourceArray);
            string absOutPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            string absSrcPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", sourcePath));

            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (existing != null && File.Exists(absOutPath) && File.Exists(absSrcPath)
                && File.GetLastWriteTimeUtc(absOutPath) >= File.GetLastWriteTimeUtc(absSrcPath))
            {
                return existing;
            }

            Shader sliceShader = AssetDatabase.LoadAssetAtPath<Shader>(SliceCopyShaderPath);
            if (sliceShader == null)
                throw new System.InvalidOperationException(
                    $"[TerrainAutoPainter] Cannot find slice copy shader at '{SliceCopyShaderPath}'. "
                    + "Ensure TerrainArraySliceCopy.shader exists at that path.");

            int w = sourceArray.width;
            int h = sourceArray.height;

            Material mat = new Material(sliceShader);
            mat.SetTexture("_SourceArray", sourceArray);
            mat.SetFloat("_SliceIndex", sliceIndex);

            // sRGB RT for albedo: GPU encodes linear→sRGB on write, so ReadPixels
            // returns the original sRGB-encoded bytes.
            // Linear RT for normals: values pass through unchanged.
            RenderTextureReadWrite rwMode = linear
                ? RenderTextureReadWrite.Linear
                : RenderTextureReadWrite.sRGB;
            RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, rwMode);
            rt.Create();

            Graphics.Blit(Texture2D.whiteTexture, rt, mat);
            Object.DestroyImmediate(mat);

            // Albedo: RGB24 discards the alpha channel so the TerrainLayer diffuse
            // is always fully opaque (alpha in terrain layer diffuse is smoothness).
            TextureFormat readbackFormat = linear ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D slice = new Texture2D(w, h, readbackFormat, mipChain: false, linear: linear);
            slice.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            slice.Apply(updateMipmaps: false);
            RenderTexture.active = previous;
            rt.Release();
            Object.DestroyImmediate(rt);

            // Persist as a PNG asset.
            string absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            File.WriteAllBytes(absPath, slice.EncodeToPNG());
            Object.DestroyImmediate(slice);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Apply correct import settings.
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = linear
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = !linear;
                importer.mipmapEnabled = true;
                if (!linear)
                {
                    // No alpha in terrain diffuse textures.
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.alphaIsTransparency = false;
                }
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        // Writes the two control map readback textures to the debug output folder as
        // PNG files so the auto-paint result can be inspected in the Project window.
        private static void SaveDebugMaps(Texture2D cm0, Texture2D cm1, int tileX, int tileZ)
        {
            string debugFolder = $"{DebugOutputFolder}/Tile_{tileX}_{tileZ}";
            string fullDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", debugFolder));
            Directory.CreateDirectory(fullDir);

            string assetPath0 = $"{debugFolder}/ControlMap0.png";
            string assetPath1 = $"{debugFolder}/ControlMap1.png";

            File.WriteAllBytes(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath0)),
                cm0.EncodeToPNG());
            File.WriteAllBytes(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath1)),
                cm1.EncodeToPNG());

            AssetDatabase.ImportAsset(assetPath0, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(assetPath1, ImportAssetOptions.ForceUpdate);
        }

        // Reads a RenderTexture into a new Texture2D on the CPU.
        private static Texture2D ReadRenderTexture(RenderTexture rt, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(updateMipmaps: false);

            RenderTexture.active = previous;
            return result;
        }
    }
}
