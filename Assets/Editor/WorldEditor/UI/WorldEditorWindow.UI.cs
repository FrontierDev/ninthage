using UnityEditor;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    public partial class WorldEditorWindow : EditorWindow
    {
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));

            bool canGenerate = _heightmapTexture != null && _continentMaskTexture != null && _worldWidth > 0 && _worldWidth % 256 == 0 && !_isGeneratingTerrain;
            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button(_isGeneratingTerrain ? "Generating..." : "Generate", EditorStyles.toolbarButton))
                    BeginAsyncTerrainGeneration();
            }

            // Show cancel button during terrain generation
            if (_isGeneratingTerrain)
            {
                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _cancelTerrainGeneration = true;
                }
            }

            if (GUILayout.Button("Place", EditorStyles.toolbarButton))
                PlaceTerrainInScene();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                ClearTerrainPrefabs();

            if (GUILayout.Button("Reset Preview", EditorStyles.toolbarButton))
                ResetErosionPreview();

            GUILayout.Space(5f);

            bool hasSelectedModes = _enableErosion && _erosionModeEnabled != null && System.Array.Exists(_erosionModeEnabled, m => m);
            bool canPreview = _heightmapTexture != null && hasSelectedModes;
            using (new EditorGUI.DisabledScope(!canPreview || _isGeneratingPreview))
            {
                if (GUILayout.Button(_isGeneratingPreview ? "Previewing..." : "Preview", EditorStyles.toolbarButton))
                    GenerateErosionPreview();
            }

            GUILayout.FlexibleSpace();

            // Show progress bar during terrain generation
            if (_isGeneratingTerrain)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(GUILayout.Width(150), GUILayout.Height(16));
                EditorGUI.ProgressBar(progressRect, _terrainGenerationProgress, _terrainGenerationText);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMainLayout()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSidePanel();
            DrawMainPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SIDE_PANEL_WIDTH));
            if (_sidebarModules.Count > 0)
            {
                string[] tabNames = new string[_sidebarModules.Count];
                for (int i = 0; i < _sidebarModules.Count; i++)
                    tabNames[i] = _sidebarModules[i].TabName;

                _selectedSidebarTab = GUILayout.Toolbar(
                    Mathf.Clamp(_selectedSidebarTab, 0, _sidebarModules.Count - 1),
                    tabNames);
                EditorGUILayout.Space(6f);
            }

            _sidePanelScroll = EditorGUILayout.BeginScrollView(_sidePanelScroll);

            if (_sidebarModules.Count > 0)
                _sidebarModules[_selectedSidebarTab].Draw(this);
            else
                EditorGUILayout.HelpBox("No sidebar modules configured.", MessageType.Warning);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawWorldParametersModule()
        {
            EditorGUILayout.LabelField("World Parameters", EditorStyles.boldLabel);
            EditorGUILayout.Space(5f);

            // Heightmap texture field
            Texture2D previousHeightmap = _heightmapTexture;
            EditorGUI.BeginChangeCheck();
            _heightmapTexture = EditorGUILayout.ObjectField(
                "Heightmap",
                _heightmapTexture,
                typeof(Texture2D),
                false) as Texture2D;

            if (EditorGUI.EndChangeCheck())
            {
                if (previousHeightmap == _generatedHeightmapTexture && _heightmapTexture != _generatedHeightmapTexture)
                {
                    Object.DestroyImmediate(_generatedHeightmapTexture);
                    _generatedHeightmapTexture = null;
                }

                _visualizationDirty = true;

                // Rivers feature removed; do not parse river map on heightmap change

                if (_heightmapTexture != null)
                {
                    float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                    _iterativeTextureWidth = _heightmapTexture.width;
                    _iterativeTextureHeight = _heightmapTexture.height;
                    _iterativePreviewWorldWidth = _worldWidth;
                    _iterativePreviewWorldHeight = Mathf.RoundToInt(_worldWidth * aspectRatio);
                    ResetErosionPreview();
                }
            }

            EditorGUILayout.Space(5f);

            // Continent mask texture field
            EditorGUI.BeginChangeCheck();
            _continentMaskTexture = EditorGUILayout.ObjectField(
                "Continent Mask",
                _continentMaskTexture,
                typeof(Texture2D),
                false) as Texture2D;
            if (EditorGUI.EndChangeCheck())
            {
                _visualizationDirty = true;
            }

            EditorGUILayout.Space(3f);

            // Biome map texture field
            EditorGUI.BeginChangeCheck();
            _biomeMapTexture = EditorGUILayout.ObjectField(
                "Biome Map",
                _biomeMapTexture,
                typeof(Texture2D),
                false) as Texture2D;
            if (EditorGUI.EndChangeCheck())
            {
                _visualizationDirty = true;
            }

            EditorGUILayout.Space(10f);

            int newWidth = EditorGUILayout.IntField("World Width (units)", _worldWidth);
            if (newWidth != _worldWidth)
            {
                if (newWidth % 256 != 0)
                {
                    EditorGUILayout.HelpBox(
                        $"Width must be a multiple of 256. Try: {(newWidth / 256) * 256} or {((newWidth / 256) + 1) * 256}",
                        MessageType.Warning);
                }
                _worldWidth = newWidth;

                if (_heightmapTexture != null)
                {
                    float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                    _iterativePreviewWorldWidth = _worldWidth;
                    _iterativePreviewWorldHeight = Mathf.RoundToInt(_worldWidth * aspectRatio);
                    ResetErosionPreview();
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUI.BeginChangeCheck();
            _seaLevel = EditorGUILayout.IntField("Sea Level (Minimum Land Height)", _seaLevel);
            if (EditorGUI.EndChangeCheck())
            {
                _visualizationDirty = true;
            }
            EditorGUILayout.LabelField("Sea Floor (World Height)", "0");

            EditorGUILayout.Space(5f);
            _waterPlaneOffset = EditorGUILayout.FloatField("Water Plane Offset", _waterPlaneOffset);

            EditorGUILayout.Space(10f);
            float newMountainLevel = EditorGUILayout.Slider("Mountain Level", _mountainLevel, 0f, 1f);
            if (newMountainLevel != _mountainLevel)
            {
                _mountainLevel = newMountainLevel;
                _visualizationDirty = true; // Update visualization when mountain level changes
            }

            EditorGUILayout.Space(10f);
            bool newShowPlateaus = EditorGUILayout.Toggle("Show Plateau Levels", _showPlateaus);
            if (newShowPlateaus != _showPlateaus)
            {
                _showPlateaus = newShowPlateaus;
                _visualizationDirty = true;
            }

            if (_showPlateaus)
            {
                int newPlateauCount = EditorGUILayout.IntSlider("Plateau Levels", _plateauCount, 2, 20);
                if (newPlateauCount != _plateauCount)
                {
                    _plateauCount = newPlateauCount;
                    _visualizationDirty = true;
                }
            }

            EditorGUILayout.Space(5f);
            bool newEnablePlateauGeneration = EditorGUILayout.Toggle("Generate Terrain Plateaus", _enablePlateauGeneration);
            if (newEnablePlateauGeneration != _enablePlateauGeneration)
            {
                _enablePlateauGeneration = newEnablePlateauGeneration;
            }

            if (_enablePlateauGeneration)
            {
                GUIContent sharpnessLabel = new GUIContent("Plateau Sharpness",
                    "Controls the steepness of plateau edges. Lower values (0.5) = gentle rolling slopes between all plateau levels. " +
                    "Higher values (5.0) = steep cliffs between higher plateaus. Lower plateaus always have soft edges, while higher plateaus become progressively sharper based on this value.");
                _plateauSharpness = EditorGUILayout.Slider(sharpnessLabel, _plateauSharpness, 0.5f, 5.0f);
            }

            EditorGUILayout.Space(10f);
            _maxHeight = EditorGUILayout.FloatField("Max Height (units)", _maxHeight);
            if (_maxHeight < 0f)
                _maxHeight = 0f;

            EditorGUILayout.Space(10f);
            _waterMaterial = EditorGUILayout.ObjectField(
                "Water Material",
                _waterMaterial,
                typeof(Material),
                false) as Material;

            _terrainMaterial = EditorGUILayout.ObjectField(
                "Terrain Material",
                _terrainMaterial,
                typeof(Material),
                false) as Material;

            // Selective chunk generation controls
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Selective Chunk Generation", EditorStyles.boldLabel);

            _selectiveGenerationMode = EditorGUILayout.Toggle("Enable Selective Mode", _selectiveGenerationMode);

            if (_selectiveGenerationMode)
            {
                EditorGUILayout.LabelField("Visualization Toggles:", EditorStyles.miniLabel);
                _visualizeGeneratedChunks = EditorGUILayout.Toggle("  Show Generated (Green)", _visualizeGeneratedChunks);
                _visualizeMarkedChunks = EditorGUILayout.Toggle("  Show Marked (Yellow)", _visualizeMarkedChunks);
                _visualizeUnmarkedChunks = EditorGUILayout.Toggle("  Show Unmarked (Grey)", _visualizeUnmarkedChunks);

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Mark Region for Generation:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start X:", GUILayout.Width(60f));
                _selectionStartChunkX = EditorGUILayout.IntField(_selectionStartChunkX);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start Z:", GUILayout.Width(60f));
                _selectionStartChunkZ = EditorGUILayout.IntField(_selectionStartChunkZ);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Width (X):", GUILayout.Width(60f));
                _selectionWidthChunks = EditorGUILayout.IntField(_selectionWidthChunks);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Height (Z):", GUILayout.Width(60f));
                _selectionHeightChunks = EditorGUILayout.IntField(_selectionHeightChunks);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5f);
                if (GUILayout.Button("Mark Region", GUILayout.Height(25f)))
                {
                    if (_heightmapTexture != null && _chunkMarkedForGeneration != null)
                    {
                        float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                        int chunksX = _worldWidth / 256;
                        int chunksZ = Mathf.RoundToInt(_worldWidth * aspectRatio) / 256;

                        int startX = Mathf.Clamp(_selectionStartChunkX, 0, chunksX - 1);
                        int startZ = Mathf.Clamp(_selectionStartChunkZ, 0, chunksZ - 1);
                        int width = Mathf.Clamp(_selectionWidthChunks, 1, chunksX - startX);
                        int height = Mathf.Clamp(_selectionHeightChunks, 1, chunksZ - startZ);

                        int markedCount = 0;
                        for (int z = startZ; z < startZ + height; z++)
                        {
                            for (int x = startX; x < startX + width; x++)
                            {
                                // Invert Z coordinate to match terrain generation (Z=0 at bottom, increases upward)
                                int actualZ = chunksZ - 1 - z;
                                _chunkMarkedForGeneration[x, actualZ] = true;
                                markedCount++;
                            }
                        }

                        Debug.Log($"Marked {markedCount} chunks in region X:[{startX}, {startX + width - 1}] Z:[{startZ}, {startZ + height - 1}]");
                        Repaint();
                    }
                }

                if (GUILayout.Button("Clear All Marked Chunks", GUILayout.Height(25f)))
                {
                    if (_chunkMarkedForGeneration != null)
                    {
                        System.Array.Clear(_chunkMarkedForGeneration, 0, _chunkMarkedForGeneration.Length);
                        Debug.Log("Cleared all marked chunks");
                        Repaint();
                    }
                }
            }

            EditorGUILayout.Space(10f);

            if (_heightmapTexture != null)
            {
                float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                int calculatedDepth = Mathf.RoundToInt(_worldWidth * aspectRatio);

                EditorGUILayout.LabelField("Calculated Dimensions:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Heightmap: {_heightmapTexture.width} × {_heightmapTexture.height}");
                EditorGUILayout.LabelField($"World Size: {_worldWidth} × {calculatedDepth}");
                EditorGUILayout.LabelField($"Aspect Ratio: {aspectRatio:F2}:1");

                int chunksX = _worldWidth / 256;
                int chunksZ = calculatedDepth / 256;
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField("Chunk Grid:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{chunksX} × {chunksZ} ({chunksX * chunksZ} chunks)");

                EditorGUILayout.Space(10f);
                _showPreview = EditorGUILayout.Toggle("Show Preview", _showPreview);

                if (_showPreview)
                {
                    EditorGUILayout.HelpBox(
                        $"Each chunk: 256 × 256 units\n" +
                        $"Terrain colliders enabled\n" +
                        $"Server physics ready",
                        MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Load a heightmap to see dimensions.", MessageType.Info);
            }
        }

        private void DrawErosionModule()
        {
            EditorGUILayout.LabelField("Global Erosion", EditorStyles.boldLabel);
            EditorGUILayout.Space(3f);
            _enableErosion = EditorGUILayout.Toggle("Enable Global Erosion", _enableErosion);
            EditorGUILayout.Space(8f);

            if (_enableErosion && _availableErosionModes != null && _availableErosionModes.Count > 0)
            {
                // Erosion Modes Section
                EditorGUILayout.LabelField("Erosion Modes", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Select and configure erosion effects (applied in order):", EditorStyles.miniLabel);
                EditorGUILayout.Space(2f);
                EditorGUI.indentLevel++;

                for (int i = 0; i < _availableErosionModes.Count; i++)
                {
                    IErosionMode mode = _availableErosionModes[i];
                    _erosionModeEnabled[i] = EditorGUILayout.Toggle(mode.ModeName, _erosionModeEnabled[i]);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(12f);

                bool anyModeEnabled = false;
                for (int i = 0; i < _availableErosionModes.Count; i++)
                {
                    if (!_erosionModeEnabled[i])
                        continue;

                    anyModeEnabled = true;
                    IErosionMode mode = _availableErosionModes[i];
                    string modeName = mode.ModeName;

                    // Mode header with description
                    EditorGUILayout.LabelField(modeName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(mode.Description, EditorStyles.miniLabel);
                    EditorGUILayout.Space(1f);
                    EditorGUI.indentLevel++;

                    ErosionSettings settings = _erosionModeSettings[modeName];

                    // Mode-specific intuitive parameters with tooltips
                    if (modeName == "Curvature Smoothing")
                    {
                        var intensityContent = new GUIContent("Smoothing Intensity",
                            "Controls how much the terrain curves are smoothed. 0.1-0.3 = subtle smoothing, " +
                            "0.5 = moderate smoothing, 0.7+ = aggressive flattening of gentle slopes. " +
                            "Uses Laplacian filter to blend heights at each point.");
                        settings.intensity = EditorGUILayout.Slider(intensityContent, settings.intensity, 0.1f, 1f);

                        var iterationsContent = new GUIContent("Passes",
                            "How many times to apply smoothing. More passes accumulate the effect: " +
                            "1 pass = very subtle, 2 passes = gentle smoothing, 3+ passes = strong smoothing. " +
                            "Each pass can be time-consuming on large maps.");
                        settings.iterations = EditorGUILayout.IntSlider(iterationsContent, settings.iterations, 1, 4);

                        var strengthContent = new GUIContent("Blend Factor",
                            "How much of the smoothed result to use. 0.1 = barely smooth (mostly original), " +
                            "0.5 = 50/50 blend, 1.0 = fully smoothed. Use lower values for subtle effects.");
                        settings.strength = EditorGUILayout.Slider(strengthContent, settings.strength, 0.1f, 1f);

                        var thresholdContent = new GUIContent("Cliff Protection",
                            "Minimum slope steepness (angle) to prevent smoothing. Steep cliffs and ridges above this threshold stay unchanged. " +
                            "0.1 = very strict (preserve almost everything), 1.0 = moderate, 2.0 = aggressive smoothing even on slopes.");
                        settings.radius = EditorGUILayout.Slider(thresholdContent, settings.radius, 0.1f, 2.0f);
                    }
                    else if (modeName == "Thermal Erosion")
                    {
                        var intensityContent = new GUIContent("Erosion Strength", "Overall intensity of thermal erosion effects. Higher values move more material.");
                        settings.intensity = EditorGUILayout.Slider(intensityContent, settings.intensity, 0.1f, 1f);

                        var iterationsContent = new GUIContent("Passes", "Number of thermal erosion iterations. More passes = stronger weathering effects.");
                        settings.iterations = EditorGUILayout.IntSlider(iterationsContent, settings.iterations, 1, 3);

                        var transferContent = new GUIContent("Transfer Rate", "How quickly material moves from steep areas to lower areas. Higher = more aggressive redistribution.");
                        settings.strength = EditorGUILayout.Slider(transferContent, settings.strength, 0.1f, 1f);

                        var talusContent = new GUIContent("Stability Angle", "Slope angle (degrees) above which loose material will slide downhill. Typical values: 30-45°.");
                        settings.radius = EditorGUILayout.Slider(talusContent, settings.radius, 25f, 65f);
                    }
                    else if (modeName == "Micro Smoothing")
                    {
                        var intensityContent = new GUIContent("Smoothing Amount", "Very subtle smoothing intensity. Keep low to remove noise without flattening terrain.");
                        settings.intensity = EditorGUILayout.Slider(intensityContent, settings.intensity, 0.05f, 0.3f);

                        var blendContent = new GUIContent("Effect Strength", "How much micro-smoothing to apply. Higher values remove more fine detail.");
                        settings.strength = EditorGUILayout.Slider(blendContent, settings.strength, 0.1f, 1f);

                        var limitContent = new GUIContent("Feature Protection", "Slope steepness above which micro-smoothing is disabled to preserve terrain features.");
                        settings.radius = EditorGUILayout.Slider(limitContent, settings.radius, 0.5f, 3.0f);

                        // Micro smoothing is always single iteration
                        settings.iterations = 1;
                    }
                    else
                    {
                        // Generic parameters for other erosion modes
                        var intensityContent = new GUIContent("Intensity", "Overall strength of the erosion effect.");
                        settings.intensity = EditorGUILayout.Slider(intensityContent, settings.intensity, 0f, 1f);

                        var iterationsContent = new GUIContent("Iterations", "Number of processing iterations.");
                        settings.iterations = EditorGUILayout.IntSlider(iterationsContent, settings.iterations, 1, 10);

                        var strengthContent = new GUIContent("Strength", "Processing strength multiplier.");
                        settings.strength = EditorGUILayout.Slider(strengthContent, settings.strength, 0f, 1f);

                        var radiusContent = new GUIContent("Radius", "Processing radius or threshold parameter.");
                        settings.radius = EditorGUILayout.Slider(radiusContent, settings.radius, 0.1f, 5f);
                    }

                    _erosionModeSettings[modeName] = settings;

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(10f);
                }

                if (!anyModeEnabled)
                {
                    EditorGUILayout.HelpBox("Select at least one erosion mode to apply.", MessageType.Info);
                }
            }
            else if (_enableErosion)
            {
                EditorGUILayout.HelpBox("No erosion modes found. Check erosion system initialization.", MessageType.Warning);
            }

            EditorGUILayout.Space(12f);
        }



        private void DrawMainPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Visualization mode selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("View:", GUILayout.Width(50f));
            _visualizationMode = (VisualizationMode)EditorGUILayout.EnumPopup(_visualizationMode, GUILayout.Width(150f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);

            // Dispatch to appropriate visualization
            switch (_visualizationMode)
            {
                case VisualizationMode.Heightmap:
                    DrawHeightmapPanel();
                    break;
                case VisualizationMode.MapOverlay:
                    DrawMapOverlayPanel();
                    break;
                case VisualizationMode.TerrainViewer:
                    DrawTerrainViewerPanel();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHeightmapPanel()
        {
            EditorGUILayout.LabelField("Heightmap Visualization (Land Only, WASD: Pan, Scroll: Zoom)", EditorStyles.boldLabel);

            if (_heightmapTexture == null)
            {
                EditorGUILayout.HelpBox("Load a heightmap in the World Parameters tab to view the combined visualization.", MessageType.Info);
                return;
            }

            if (_continentMaskTexture == null)
            {
                EditorGUILayout.HelpBox("Load a continent mask in the World Parameters tab to view the combined visualization.", MessageType.Warning);
                return;
            }

            if (_continentMaskTexture.width != _heightmapTexture.width || _continentMaskTexture.height != _heightmapTexture.height)
            {
                EditorGUILayout.HelpBox(
                    $"Resolution mismatch: Continent mask ({_continentMaskTexture.width}×{_continentMaskTexture.height}) must match heightmap ({_heightmapTexture.width}×{_heightmapTexture.height}).",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Visualization", GUILayout.Height(30f)))
            {
                _visualizationDirty = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);

            if (_visualizationDirty)
            {
                RefreshCombinedHeightmapVisualization();
            }

            DrawVisualizationPanel(_cachedCombinedHeightmapTexture, "Heightmap (Grayscale)");
        }

        private void DrawVisualizationPanel(Texture2D displayTexture, string label)
        {
            Rect mainAreaRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.BeginGroup(mainAreaRect);
            GUI.Box(new Rect(0, 0, mainAreaRect.width, mainAreaRect.height), "", GUI.skin.box);

            float worldAspectRatio = (float)_iterativePreviewWorldHeight / _iterativePreviewWorldWidth;
            float displayWidth = mainAreaRect.width * _zoomLevel;
            float displayHeight = displayWidth * worldAspectRatio;

            Rect scaledRect = new Rect(
                (mainAreaRect.width - displayWidth) * 0.5f + _panOffset.x,
                (mainAreaRect.height - displayHeight) * 0.5f + _panOffset.y,
                displayWidth,
                displayHeight);

            if (displayTexture != null)
            {
                GUI.DrawTexture(scaledRect, displayTexture);
            }

            // Draw chunk grid with selective generation visualization overlay
            DrawGridOverlay(scaledRect);

            EditorGUI.LabelField(new Rect(5f, 5f, 100f, 20f), $"Zoom: {_zoomLevel:F1}x", EditorStyles.helpBox);
            EditorGUI.LabelField(new Rect(5f, 25f, 190f, 20f), $"World: {_iterativePreviewWorldWidth}×{_iterativePreviewWorldHeight}", EditorStyles.helpBox);
            EditorGUI.LabelField(new Rect(5f, 45f, 190f, 20f), $"Resolution: {_heightmapTexture.width}×{_heightmapTexture.height}", EditorStyles.helpBox);
            EditorGUI.LabelField(new Rect(5f, 65f, 190f, 20f), $"Mode: {label}", EditorStyles.helpBox);
            if (_isGeneratingPreview && _iterativeHeightmap != null && _iterativeErosionModes != null && _iterativeModeIndex < _iterativeErosionModes.Count)
            {
                float totalModes = _iterativeErosionModes.Count;
                float modeProgress = _iterativeModeIndex / totalModes;
                float iterationProgress = _iterativeIteration / (float)_iterativeErosionModes[_iterativeModeIndex].Item2.iterations;
                float totalProgress = (modeProgress + (iterationProgress / totalModes)) * 100f;

                Rect progressBarRect = new Rect(5f, mainAreaRect.height - 35f, 200f, 20f);
                GUI.Box(progressBarRect, "");
                Rect fillRect = new Rect(progressBarRect.x + 2f, progressBarRect.y + 2f, (progressBarRect.width - 4f) * Mathf.Clamp01(totalProgress / 100f), progressBarRect.height - 4f);
                EditorGUI.DrawRect(fillRect, new Color(0.2f, 0.7f, 0.2f, 0.8f));
                EditorGUI.LabelField(progressBarRect, $"{totalProgress:F1}% {_iterativeProgressText}", EditorStyles.miniLabel);
            }

            GUI.EndGroup();
        }

        private void RefreshCombinedHeightmapVisualization()
        {
            if (_heightmapTexture == null || _continentMaskTexture == null)
            {
                _visualizationDirty = false;
                return;
            }

            if (_continentMaskTexture.width != _heightmapTexture.width || _continentMaskTexture.height != _heightmapTexture.height)
            {
                _visualizationDirty = false;
                return;
            }

            Texture2D sourceTexture = _heightmapTexture;
            Texture2D continentTexture = _continentMaskTexture;
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            if (_cachedCombinedHeightmapTexture == null || _cachedCombinedHeightmapTexture.width != width || _cachedCombinedHeightmapTexture.height != height)
            {
                if (_cachedCombinedHeightmapTexture != null)
                    Object.DestroyImmediate(_cachedCombinedHeightmapTexture);
                _cachedCombinedHeightmapTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                _cachedCombinedHeightmapTexture.wrapMode = TextureWrapMode.Clamp;
                _cachedCombinedHeightmapTexture.filterMode = FilterMode.Point;
            }

            Color[] sourcePixels = GetTexturePixels(sourceTexture, width, height);
            Color[] continentPixels = GetTexturePixels(continentTexture, width, height);
            Color[] resultPixels = new Color[sourcePixels.Length];

            int mountainPixelCount = 0;
            float maxHeightValue = 0f;
            float normalizedSeaLevel = _seaLevel / _maxHeight;

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color continentPixel = continentPixels[i];
                bool isLand = continentPixel.r > 0.5f || continentPixel.g > 0.5f || continentPixel.b > 0.5f;

                if (isLand)
                {
                    Color heightPixel = sourcePixels[i];
                    float heightValue = heightPixel.r; // Use red channel for height
                    maxHeightValue = Mathf.Max(maxHeightValue, heightValue);

                    if (_showPlateaus && heightValue >= normalizedSeaLevel && heightValue < _mountainLevel)
                    {
                        // Plateau mode: show discrete height bands between sea level and mountain level
                        float plateauRange = heightValue - normalizedSeaLevel;
                        float maxPlateauRange = _mountainLevel - normalizedSeaLevel;
                        float normalizedPlateauHeight = plateauRange / maxPlateauRange;

                        int plateauIndex = Mathf.FloorToInt(normalizedPlateauHeight * _plateauCount);
                        plateauIndex = Mathf.Clamp(plateauIndex, 0, _plateauCount - 1);

                        // Generate distinct color for this plateau level using HSV
                        float hue = plateauIndex / (float)_plateauCount;
                        Color plateauColor = Color.HSVToRGB(hue, 0.85f, 0.9f);
                        plateauColor.a = 1f;
                        resultPixels[i] = plateauColor;
                    }
                    else if (heightValue >= _mountainLevel)
                    {
                        // Mountains: show red above mountain level
                        resultPixels[i] = new Color(1f, 0.2f, 0.2f, 1f); // Red for mountains
                        mountainPixelCount++;
                    }
                    else if (heightValue >= normalizedSeaLevel)
                    {
                        // Regular terrain between sea level and mountain level (grayscale when not in plateau mode)
                        resultPixels[i] = heightPixel;
                    }
                    else
                    {
                        // Below sea level but marked as land - show as shallow water
                        resultPixels[i] = new Color(0.2f, 0.4f, 0.8f, 1f);
                    }
                }
                else
                {
                    resultPixels[i] = new Color(0f, 0.3f, 0.7f, 1f); // Blue for water
                }
            }

            if (!_showPlateaus)
            {
                Debug.Log($"Mountain visualization: {mountainPixelCount} pixels above mountain level {_mountainLevel:F3}. Max height value: {maxHeightValue:F3}");
            }
            else
            {
                Debug.Log($"Plateau visualization: {_plateauCount} levels between sea level {normalizedSeaLevel:F3} and mountain level {_mountainLevel:F3}. Max height value: {maxHeightValue:F3}");
            }

            _cachedCombinedHeightmapTexture.SetPixels(resultPixels);
            _cachedCombinedHeightmapTexture.Apply(false, false);
            _visualizationDirty = false;
        }

        private void DrawChunkGrid(Rect displayRect, int worldWidth, int worldHeight)
        {
            const int CHUNK_SIZE = 256;
            Color gridColor = new Color(1f, 1f, 0f, 0.5f);

            // Calculate how many chunks fit in world
            int chunksX = Mathf.CeilToInt((float)worldWidth / CHUNK_SIZE);
            int chunksY = Mathf.CeilToInt((float)worldHeight / CHUNK_SIZE);

            // Only draw grid if chunks are large enough to be visible
            if (chunksX <= 1 || chunksY <= 1)
                return;

            // Calculate pixel size per chunk
            float pixelsPerChunkX = displayRect.width / chunksX;
            float pixelsPerChunkY = displayRect.height / chunksY;

            // Draw vertical lines as thin rectangles
            for (int i = 1; i < chunksX; i++)
            {
                float x = displayRect.x + (i * pixelsPerChunkX);
                float lineWidth = Mathf.Max(1f, pixelsPerChunkX * 0.01f); // Adaptive line width
                EditorGUI.DrawRect(new Rect(x - lineWidth * 0.5f, displayRect.y, lineWidth, displayRect.height), gridColor);
            }

            // Draw horizontal lines as thin rectangles
            for (int i = 1; i < chunksY; i++)
            {
                float y = displayRect.y + (i * pixelsPerChunkY);
                float lineHeight = Mathf.Max(1f, pixelsPerChunkY * 0.01f); // Adaptive line height
                EditorGUI.DrawRect(new Rect(displayRect.x, y - lineHeight * 0.5f, displayRect.width, lineHeight), gridColor);
            }
        }

        private void DrawMapOverlayPanel()
        {
            EditorGUILayout.LabelField("Biomes (WASD: Pan, Scroll: Zoom)", EditorStyles.boldLabel);

            if (_continentMaskTexture.width != _biomeMapTexture.width ||
                _continentMaskTexture.height != _biomeMapTexture.height)
            {
                EditorGUILayout.HelpBox(
                    $"All maps must have the same resolution!\n" +
                    $"Continent: {_continentMaskTexture.width}×{_continentMaskTexture.height}\n" +
                    $"Biome: {_biomeMapTexture.width}×{_biomeMapTexture.height}\n",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Visualization", GUILayout.Height(30f)))
            {
                _visualizationDirty = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);

            if (_visualizationDirty)
            {
                RefreshBiomesVisualization();
            }

            Rect mainAreaRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.BeginGroup(mainAreaRect);
            GUI.Box(new Rect(0, 0, mainAreaRect.width, mainAreaRect.height), "", GUI.skin.box);

            float worldAspectRatio = (float)_continentMaskTexture.height / _continentMaskTexture.width;
            float displayWidth = mainAreaRect.width * _zoomLevel;
            float displayHeight = displayWidth * worldAspectRatio;

            Rect scaledRect = new Rect(
                (mainAreaRect.width - displayWidth) * 0.5f + _panOffset.x,
                (mainAreaRect.height - displayHeight) * 0.5f + _panOffset.y,
                displayWidth,
                displayHeight);

            if (_cachedBiomesVisualizationTexture != null)
            {
                GUI.DrawTexture(scaledRect, _cachedBiomesVisualizationTexture);
            }

            // Draw chunk grid
            DrawChunkGrid(scaledRect, _iterativePreviewWorldWidth, _iterativePreviewWorldHeight);

            EditorGUI.LabelField(new Rect(5f, 5f, 100f, 20f), $"Zoom: {_zoomLevel:F1}x", EditorStyles.helpBox);
            EditorGUI.LabelField(new Rect(5f, 25f, 190f, 20f), $"World: {_iterativePreviewWorldWidth}×{_iterativePreviewWorldHeight}", EditorStyles.helpBox);
            EditorGUI.LabelField(new Rect(5f, 45f, 190f, 20f), $"Resolution: {_continentMaskTexture.width}×{_continentMaskTexture.height}", EditorStyles.helpBox);

            GUI.EndGroup();
        }

        private void RefreshBiomesVisualization()
        {
            if (_continentMaskTexture == null || _biomeMapTexture == null)
            {
                _visualizationDirty = false;
                return;
            }

            if (_continentMaskTexture.width != _biomeMapTexture.width || _continentMaskTexture.height != _biomeMapTexture.height)
            {
                _visualizationDirty = false;
                return;
            }

            int width = _continentMaskTexture.width;
            int height = _continentMaskTexture.height;

            if (_cachedBiomesVisualizationTexture == null || _cachedBiomesVisualizationTexture.width != width || _cachedBiomesVisualizationTexture.height != height)
            {
                if (_cachedBiomesVisualizationTexture != null)
                    Object.DestroyImmediate(_cachedBiomesVisualizationTexture);
                _cachedBiomesVisualizationTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                _cachedBiomesVisualizationTexture.wrapMode = TextureWrapMode.Clamp;
                _cachedBiomesVisualizationTexture.filterMode = FilterMode.Point;
            }

            Color[] continentPixels = GetTexturePixels(_continentMaskTexture, width, height);
            Color[] biomePixels = GetTexturePixels(_biomeMapTexture, width, height);
            Color[] resultPixels = new Color[continentPixels.Length];

            for (int i = 0; i < continentPixels.Length; i++)
            {
                Color continentPixel = continentPixels[i];
                bool isLand = continentPixel.r > 0.5f || continentPixel.g > 0.5f || continentPixel.b > 0.5f;

                if (isLand)
                {
                    resultPixels[i] = biomePixels[i];
                }
                else
                {
                    resultPixels[i] = new Color(0f, 0.3f, 0.7f, 1f);
                }
            }

            _cachedBiomesVisualizationTexture.SetPixels(resultPixels);
            _cachedBiomesVisualizationTexture.Apply(false, false);
            _visualizationDirty = false;
        }

        private void DrawTerrainViewerPanel()
        {
            EditorGUILayout.LabelField("Terrain Viewer", EditorStyles.boldLabel);

            Rect mainAreaRect = GUILayoutUtility.GetRect(
                0, 0,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            GUI.BeginGroup(mainAreaRect);
            Rect groupRect = new Rect(0, 0, mainAreaRect.width, mainAreaRect.height);
            GUI.Box(groupRect, "", GUI.skin.box);

            // Display placeholder
            string placeholderText = "Terrain Viewer\n(Coming Soon)\n\nThis will display a 3D preview\nof the generated terrain chunks\nwith real-time rendering.";
            Vector2 textSize = GUI.skin.box.CalcSize(new GUIContent(placeholderText));
            float left = (mainAreaRect.width - textSize.x) * 0.5f;
            float top = (mainAreaRect.height - textSize.y) * 0.5f;

            GUI.Label(new Rect(left, top, textSize.x, textSize.y), placeholderText, EditorStyles.helpBox);

            GUI.EndGroup();
        }

        private void DrawDragDropZone()
        {
            Rect dropZone = GUILayoutUtility.GetRect(0f, 100f, GUILayout.ExpandWidth(true));
            GUI.Box(dropZone, "Drag heightmap texture here", EditorStyles.helpBox);

            if (dropZone.Contains(Event.current.mousePosition))
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (obj is Texture2D texture)
                                {
                                    _heightmapTexture = texture;
                                    float aspectRatio = (float)texture.height / texture.width;
                                    _iterativeTextureWidth = texture.width;
                                    _iterativeTextureHeight = texture.height;
                                    _iterativePreviewWorldWidth = _worldWidth;
                                    _iterativePreviewWorldHeight = Mathf.RoundToInt(_worldWidth * aspectRatio);
                                    ResetErosionPreview();
                                    Event.current.Use();
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
