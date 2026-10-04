using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Game.Editor.WorldEditor
{
    public enum VisualizationMode
    {
        Heightmap,
        MapOverlay,
        TerrainViewer
    }

    public partial class WorldEditorWindow : EditorWindow
    {
        // Heightmap and display
        private Texture2D _heightmapTexture;
        private Texture2D _generatedHeightmapTexture;
        private Texture2D _cachedCombinedHeightmapTexture;
        private Texture2D _cachedBiomesVisualizationTexture;
        private bool _visualizationDirty = true;

        // New map inputs
        private Texture2D _continentMaskTexture;
        private Texture2D _biomeMapTexture;

        private Texture2D _coastlineMaskTexture;
        private float _generatedGeoMinX;
        private float _generatedGeoMaxY;
        private float _generatedGeoRangeX;
        private float _generatedGeoRangeY;
        private Material _waterMaterial;
        private Material _terrainMaterial;

        // World parameters
        private int _worldWidth = 256;
        private bool _flipCoastlineMaskY = false;
        private int _seaLevel = 10;
        private float _waterPlaneOffset = -2f;
        private float _mountainLevel = 0.7f;
        private float _maxHeight = 100f;

        // Plateau mode
        private bool _showPlateaus = false;
        private int _plateauCount = 5;
        private bool _enablePlateauGeneration = false;
        private float _plateauSharpness = 2.0f; // Higher values = sharper transitions for higher plateaus

        // View and interaction
        private bool _showPreview = true;
        private Vector2 _sidePanelScroll = Vector2.zero;
        private Vector2 _panOffset = Vector2.zero;
        private float _zoomLevel = 1f;
        private int _selectedSidebarTab = 0;
        private readonly List<IWorldEditorWindowModule> _sidebarModules = new List<IWorldEditorWindowModule>();
        private VisualizationMode _visualizationMode = VisualizationMode.Heightmap;

        // Erosion settings
        private bool _enableErosion = false;
        private List<IErosionMode> _availableErosionModes;
        private string[] _erosionModeNames;
        private bool[] _erosionModeEnabled;
        private Dictionary<string, ErosionSettings> _erosionModeSettings = new Dictionary<string, ErosionSettings>();



        // Erosion preview
        private float[] _previewHeightmap;
        private Texture2D _previewHeightmapTexture;
        private bool _isGeneratingPreview = false;

        // Async terrain generation
        private bool _isGeneratingTerrain = false;
        private float _terrainGenerationProgress = 0f;
        private string _terrainGenerationText = "";
        private bool _cancelTerrainGeneration = false;

        // Async terrain generation state
        private Texture2D _asyncMaskedHeightmap;
        private int _asyncChunksX;
        private int _asyncChunksZ;
        private int _asyncCurrentChunkIndex;
        private List<(IErosionMode, ErosionSettings)> _asyncSelectedErosionModes;

        // Streaming system for massive terrain generation
        private Game.Editor.WorldEditor.Streaming.TiledHeightmapSystem _tiledHeightmapSystem;
        private System.Threading.CancellationTokenSource _terrainGenerationCTS;

        // Selective chunk generation
        private bool[,] _chunkMarkedForGeneration;          // Which chunks are marked to generate [x, z]
        private bool[,] _chunkGenerated;                     // Which chunks have been generated [x, z]
        private bool _visualizeGeneratedChunks = true;       // Toggle visualization of generated chunks (green)
        private bool _visualizeMarkedChunks = true;          // Toggle visualization of marked chunks (yellow)
        private bool _visualizeUnmarkedChunks = true;        // Toggle visualization of unmarked chunks (grey)
        private bool _selectiveGenerationMode = false;       // Enable/disable selective generation mode
        private int _selectionStartChunkX = 0;               // Start X for rect selection
        private int _selectionStartChunkZ = 0;               // Start Z for rect selection (vertical in preview)
        private int _selectionWidthChunks = 1;               // Width in chunks (X direction)
        private int _selectionHeightChunks = 1;              // Height in chunks (Z direction)




        // Iterative erosion state
        private List<(IErosionMode, ErosionSettings)> _iterativeErosionModes;
        private int _iterativeModeIndex = 0;
        private int _iterativeIteration = 0;
        private float[] _iterativeHeightmap;
        private int _iterativeTextureWidth;
        private int _iterativeTextureHeight;
        private int _iterativePreviewWorldWidth;
        private int _iterativePreviewWorldHeight;
        private string _iterativeProgressText;

        // River system removed

        private const float TOOLBAR_HEIGHT = 24f;
        private const float SIDE_PANEL_WIDTH = 300f;
        private const float PAN_SENSITIVITY = 5f;
        private const float ZOOM_SENSITIVITY = 0.1f;
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 5f;
        private const float SEA_FLOOR_WORLD_HEIGHT = -100f;
        private static readonly string SETTINGS_PATH = "Assets/Editor/WorldEditor/UI/WorldEditorSettings.json";

        [MenuItem("Tools/World Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<WorldEditorWindow>();
            window.titleContent = new GUIContent("World Editor");
            window.minSize = new Vector2(800f, 400f);
            window.Show();
        }

        private void OnGUI()
        {
            // Update iterative erosion if in progress
            if (_isGeneratingPreview && _iterativeHeightmap != null)
            {
                UpdateIterativeErosion();
                Repaint();
            }

            // Update async terrain generation if in progress
            if (_isGeneratingTerrain)
            {
                UpdateAsyncTerrainGeneration();
                Repaint();
            }

            DrawToolbar();
            HandlePanZoomInput();
            DrawMainLayout();
        }

        private void OnEnable()
        {
            InitializeErosionModes();
            InitializeSidebarModules();
            LoadSettings();
        }

        private void OnDestroy()
        {
            SaveSettings();
            CleanupTextures();
        }

        private void CleanupTextures()
        {
            if (_previewHeightmapTexture != null)
                Object.DestroyImmediate(_previewHeightmapTexture);
            if (_cachedCombinedHeightmapTexture != null)
                Object.DestroyImmediate(_cachedCombinedHeightmapTexture);
            if (_cachedBiomesVisualizationTexture != null)
                Object.DestroyImmediate(_cachedBiomesVisualizationTexture);
            if (_generatedHeightmapTexture != null)
                Object.DestroyImmediate(_generatedHeightmapTexture);
            // River visualization texture removed
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new WorldEditorSettings
                {
                    worldWidth = _worldWidth,
                    flipCoastlineMaskY = _flipCoastlineMaskY,
                    seaLevel = _seaLevel,
                    waterPlaneOffset = _waterPlaneOffset,
                    mountainLevel = _mountainLevel,
                    maxHeight = _maxHeight,
                    showPlateaus = _showPlateaus,
                    plateauCount = _plateauCount,
                    enablePlateauGeneration = _enablePlateauGeneration,
                    plateauSharpness = _plateauSharpness,
                    showPreview = _showPreview,
                    zoomLevel = _zoomLevel,
                    panOffsetX = _panOffset.x,
                    panOffsetY = _panOffset.y,
                    selectedSidebarTab = _selectedSidebarTab,
                    visualizationMode = (int)_visualizationMode,
                    enableErosion = _enableErosion,
                    erosionModeEnabled = _erosionModeEnabled,
                    visualizeGeneratedChunks = _visualizeGeneratedChunks,
                    visualizeMarkedChunks = _visualizeMarkedChunks,
                    visualizeUnmarkedChunks = _visualizeUnmarkedChunks,
                    selectiveGenerationMode = _selectiveGenerationMode
                };

                // Create directory if it doesn't exist
                string directoryPath = Path.GetDirectoryName(SETTINGS_PATH);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var data = new WorldEditorSettingsData { settings = settings };
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SETTINGS_PATH, json);
                Debug.Log($"Saved World Editor settings to {SETTINGS_PATH}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save World Editor settings: {e.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SETTINGS_PATH))
                    return;

                string json = File.ReadAllText(SETTINGS_PATH);
                var data = JsonUtility.FromJson<WorldEditorSettingsData>(json);
                var settings = data.settings;

                _worldWidth = settings.worldWidth;
                _flipCoastlineMaskY = settings.flipCoastlineMaskY;
                _seaLevel = settings.seaLevel;
                _waterPlaneOffset = settings.waterPlaneOffset;
                _mountainLevel = settings.mountainLevel;
                _maxHeight = settings.maxHeight;
                _showPlateaus = settings.showPlateaus;
                _plateauCount = settings.plateauCount;
                _enablePlateauGeneration = settings.enablePlateauGeneration;
                _plateauSharpness = settings.plateauSharpness;
                _showPreview = settings.showPreview;
                _zoomLevel = settings.zoomLevel;
                _panOffset = new Vector2(settings.panOffsetX, settings.panOffsetY);
                _selectedSidebarTab = settings.selectedSidebarTab;
                _visualizationMode = (VisualizationMode)settings.visualizationMode;
                _enableErosion = settings.enableErosion;
                _visualizeGeneratedChunks = settings.visualizeGeneratedChunks;
                _visualizeMarkedChunks = settings.visualizeMarkedChunks;
                _visualizeUnmarkedChunks = settings.visualizeUnmarkedChunks;
                _selectiveGenerationMode = settings.selectiveGenerationMode;

                // Restore erosion mode states if available
                if (settings.erosionModeEnabled != null && settings.erosionModeEnabled.Length > 0)
                {
                    System.Array.Copy(settings.erosionModeEnabled, _erosionModeEnabled,
                        Mathf.Min(settings.erosionModeEnabled.Length, _erosionModeEnabled.Length));
                }

                Debug.Log("Loaded World Editor settings");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load World Editor settings: {e.Message}");
            }
        }

        private void InitializeErosionModes()
        {
            // Get all available erosion modes
            var allModes = ErosionModeRegistry.GetAvailableModes();

            // Get all erosion modes
            _availableErosionModes = new List<IErosionMode>();

            foreach (var mode in allModes)
            {
                _availableErosionModes.Add(mode);
            }

            // Initialize global erosion mode arrays
            _erosionModeNames = new string[_availableErosionModes.Count];
            _erosionModeEnabled = new bool[_availableErosionModes.Count];

            for (int i = 0; i < _availableErosionModes.Count; i++)
            {
                _erosionModeNames[i] = _availableErosionModes[i].ModeName;
                // Disable all erosion modes by default
                _erosionModeEnabled[i] = false;
                _erosionModeSettings[_erosionModeNames[i]] = _availableErosionModes[i].GetDefaultSettings();
            }
        }

        private void InitializeSidebarModules()
        {
            _sidebarModules.Clear();
            _sidebarModules.Add(new WorldParametersModule());
            _sidebarModules.Add(new ErosionModule());

            if (_selectedSidebarTab >= _sidebarModules.Count)
                _selectedSidebarTab = 0;
        }

        // River registry initialization removed
    }
}

