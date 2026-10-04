using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using Game.Runtime.Shared;

namespace Game.Editor.WorldEditor
{
    public enum ErosionMode
    {
        Hydraulic,
        Wind,
        Terrace,
        Smooth,
        Sharpen
    }

    [System.Serializable]
    public class ErosionOperation
    {
        public ErosionMode mode = ErosionMode.Hydraulic;

        // Hydraulic
        public int h_Iterations = 100000;
        public int h_Resolution = 256;
        public int h_ErosionSteps = 3;

        // Wind
        public float w_Windiness = 0.5f;
        public Vector2 w_WindDirection = new Vector2(1, 2);
        public int w_Resolution = 257;

        // Terrace
        public float t_TerraceSpacing = 15;
        public float t_TerraceAngle = 20;

        // Smooth
        public int s_SmoothingWidth = 2;

        // Sharpen
        public float sh_SharpenStrength = 5;
        public int sh_SharpenIterations = 10;
        public float sh_PeakMixStrength = 0.7f;
    }

    [System.Serializable]
    public class ErosionProfile
    {
        public string name = "New Profile";
        public List<ErosionOperation> operations = new List<ErosionOperation>();
    }

    [System.Serializable]
    public class ErosionProfilesData
    {
        public List<ErosionProfile> profiles = new List<ErosionProfile>();
    }

    /// <summary>
    /// Window for eroding a 3x3 grid of terrain tiles with the selected tile at the center.
    /// Erosion flows naturally across tile boundaries.
    /// </summary>
    public class LocalErosionWindow : EditorWindow
    {
        private Terrain _centerTerrain;
        private Terrain _previousCenterTerrain;
        private Terrain[] _neighbors = new Terrain[8]; // NW, N, NE, W, E, SW, S, SE

        // Erosion mode
        private ErosionMode _erosionMode = ErosionMode.Hydraulic;

        // Hydraulic parameters
        private int _h_Iterations = 100000;
        private int _h_Resolution = 256;
        private int _h_ErosionSteps = 3;

        // Wind parameters
        private float _w_Windiness = 0.5f;
        private Vector2 _w_WindDirection = new Vector2(1, 2);
        private int _w_Resolution = 257;

        // Terrace parameters
        private float _t_TerraceSpacing = 15;
        private float _t_TerraceAngle = 20;

        // Smooth parameters
        private int _s_SmoothingWidth = 2;

        // Sharpen parameters
        private float _sh_SharpenStrength = 5;
        private int _sh_SharpenIterations = 10;
        private float _sh_PeakMixStrength = 0.7f;

        // Compute shaders
        private ComputeShader _hydraulicShader;
        private ComputeShader _windShader;
        private ComputeShader _terraceShader;
        private ComputeShader _smoothSharpenShader;

        // Profiles
        private List<ErosionProfile> _profiles = new List<ErosionProfile>();
        private int _selectedProfileIndex = -1;
        private string _newProfileName = "Mountain Peak";
        private Vector2 _profilesScrollPos = Vector2.zero;
        private Vector2 _operationsScrollPos = Vector2.zero;

        // Tile targeting
        private int _targetTileX = 0;
        private int _targetTileZ = 0;

        private Vector2 _scrollPos;
        private float _gridDisplaySize = 300f;

        // Profiles persistence
        private static readonly string PROFILES_PATH = "Assets/Editor/WorldEditor/Erosion/LocalTileErosion/profiles.json";

        [MenuItem("Window/Local Tile Erosion")]
        public static void ShowWindow()
        {
            GetWindow<LocalErosionWindow>("Local Erosion");
        }

        private void OnEnable()
        {
            LoadProfiles();
        }

        private void OnDisable()
        {
            SaveProfiles();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Local Tile Erosion", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Split view: profiles on left, main controls on right
            EditorGUILayout.BeginHorizontal();

            // LEFT: Profiles Panel
            DrawProfilesPanel();

            // RIGHT: Main Controls
            DrawMainControls();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawProfilesPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));

            EditorGUILayout.LabelField("Erosion Profiles", EditorStyles.boldLabel);

            // Create new profile
            EditorGUILayout.BeginHorizontal();
            _newProfileName = EditorGUILayout.TextField(_newProfileName, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                var newProfile = new ErosionProfile { name = _newProfileName };
                newProfile.operations.Add(new ErosionOperation());
                _profiles.Add(newProfile);
                _selectedProfileIndex = _profiles.Count - 1;
                SaveProfiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Profile list
            EditorGUILayout.LabelField("Saved Profiles", EditorStyles.miniLabel);
            _profilesScrollPos = EditorGUILayout.BeginScrollView(_profilesScrollPos, GUILayout.Height(150));

            for (int i = 0; i < _profiles.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);

                if (GUILayout.Button(_profiles[i].name, _selectedProfileIndex == i ? EditorStyles.toolbarButton : EditorStyles.label))
                {
                    _selectedProfileIndex = i;
                }

                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    _profiles.RemoveAt(i);
                    if (_selectedProfileIndex == i) _selectedProfileIndex = -1;
                    SaveProfiles();
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Operations in selected profile
            if (_selectedProfileIndex >= 0 && _selectedProfileIndex < _profiles.Count)
            {
                EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);
                var profile = _profiles[_selectedProfileIndex];

                _operationsScrollPos = EditorGUILayout.BeginScrollView(_operationsScrollPos, GUILayout.Height(250));

                for (int i = 0; i < profile.operations.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    EditorGUI.BeginChangeCheck();
                    profile.operations[i].mode = (ErosionMode)EditorGUILayout.EnumPopup(profile.operations[i].mode, GUILayout.Width(100));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveProfiles();
                    }

                    if (GUILayout.Button("−", GUILayout.Width(25)))
                    {
                        profile.operations.RemoveAt(i);
                        SaveProfiles();
                        continue;
                    }

                    EditorGUILayout.EndHorizontal();

                    // Draw parameters for this operation
                    DrawOperationParameters(profile.operations[i]);
                }

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Add Operation"))
                {
                    profile.operations.Add(new ErosionOperation());
                    SaveProfiles();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Apply Profile", GUILayout.Height(40)))
                {
                    ApplyProfile(profile);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMainControls()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.HelpBox("Select a terrain tile to erode it along with its 8 neighbors.", MessageType.Info);
            EditorGUILayout.Space();

            // Target tile
            EditorGUILayout.LabelField("Target Tile", EditorStyles.boldLabel);
            Terrain newSelection = EditorGUILayout.ObjectField("Center Tile", _centerTerrain, typeof(Terrain), true) as Terrain;

            if (newSelection != _previousCenterTerrain)
            {
                _centerTerrain = newSelection;
                _previousCenterTerrain = newSelection;

                if (_centerTerrain != null)
                {
                    FindNeighbors();
                }
                else
                {
                    System.Array.Clear(_neighbors, 0, _neighbors.Length);
                }
            }

            if (_centerTerrain != null && GUILayout.Button("Refresh Neighbors", GUILayout.Height(25)))
            {
                FindNeighbors();
            }

            EditorGUILayout.Space();

            // Target tile by coordinates
            EditorGUILayout.LabelField("Target Tile by Coordinates", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _targetTileX = EditorGUILayout.IntField("Tile X", _targetTileX, GUILayout.ExpandWidth(true));
            _targetTileZ = EditorGUILayout.IntField("Tile Z", _targetTileZ, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Target Tile", GUILayout.Height(25)))
            {
                TargetTileByCoordinates(_targetTileX, _targetTileZ);
            }

            EditorGUILayout.Space();

            // Grid display
            if (_centerTerrain != null)
            {
                DrawGrid();
                EditorGUILayout.Space();
            }

            // Compute shaders setup
            EditorGUILayout.LabelField("Compute Shaders", EditorStyles.boldLabel);
            _hydraulicShader = EditorGUILayout.ObjectField("Hydraulic", _hydraulicShader, typeof(ComputeShader), false) as ComputeShader;
            _windShader = EditorGUILayout.ObjectField("Wind", _windShader, typeof(ComputeShader), false) as ComputeShader;
            _terraceShader = EditorGUILayout.ObjectField("Terrace", _terraceShader, typeof(ComputeShader), false) as ComputeShader;
            _smoothSharpenShader = EditorGUILayout.ObjectField("Smooth/Sharpen", _smoothSharpenShader, typeof(ComputeShader), false) as ComputeShader;

            EditorGUILayout.Space();

            // Quick erosion mode selector
            EditorGUILayout.LabelField("Quick Erosion", EditorStyles.boldLabel);
            _erosionMode = (ErosionMode)EditorGUILayout.EnumPopup("Mode", _erosionMode);

            DrawParametersForMode();

            EditorGUILayout.Space();

            bool canErode = _centerTerrain != null;
            if (_erosionMode == ErosionMode.Hydraulic) canErode = canErode && _hydraulicShader != null;
            else if (_erosionMode == ErosionMode.Wind) canErode = canErode && _windShader != null;
            else if (_erosionMode == ErosionMode.Terrace) canErode = canErode && _terraceShader != null;
            else if (_erosionMode == ErosionMode.Smooth || _erosionMode == ErosionMode.Sharpen) canErode = canErode && _smoothSharpenShader != null;

            GUI.enabled = canErode;
            if (GUILayout.Button("Apply Single Mode", GUILayout.Height(40)))
            {
                ErodeGrid();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawOperationParameters(ErosionOperation op)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            switch (op.mode)
            {
                case ErosionMode.Hydraulic:
                    op.h_Iterations = EditorGUILayout.IntField("Iterations", op.h_Iterations);
                    op.h_Resolution = EditorGUILayout.IntField("Resolution", op.h_Resolution);
                    op.h_ErosionSteps = EditorGUILayout.IntSlider("Steps", op.h_ErosionSteps, 1, 8);
                    break;
                case ErosionMode.Wind:
                    op.w_Windiness = EditorGUILayout.Slider("Windiness", op.w_Windiness, 0, 5);
                    op.w_WindDirection = EditorGUILayout.Vector2Field("Direction", op.w_WindDirection);
                    op.w_Resolution = EditorGUILayout.IntField("Resolution", op.w_Resolution);
                    break;
                case ErosionMode.Terrace:
                    op.t_TerraceSpacing = EditorGUILayout.FloatField("Spacing", op.t_TerraceSpacing);
                    op.t_TerraceAngle = EditorGUILayout.Slider("Angle", op.t_TerraceAngle, 0, 90);
                    break;
                case ErosionMode.Smooth:
                    op.s_SmoothingWidth = EditorGUILayout.IntField("Width", op.s_SmoothingWidth);
                    break;
                case ErosionMode.Sharpen:
                    op.sh_SharpenStrength = EditorGUILayout.FloatField("Strength", op.sh_SharpenStrength);
                    op.sh_SharpenIterations = EditorGUILayout.IntField("Iterations", op.sh_SharpenIterations);
                    op.sh_PeakMixStrength = EditorGUILayout.Slider("Peak Mix", op.sh_PeakMixStrength, 0, 1);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveProfiles();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void ApplyProfile(ErosionProfile profile)
        {
            if (_centerTerrain == null || profile.operations.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Select a terrain and add operations to the profile", "OK");
                return;
            }

            List<Terrain> gridTerrains = new List<Terrain> { _centerTerrain };
            for (int i = 0; i < 8; i++)
            {
                if (_neighbors[i] != null)
                    gridTerrains.Add(_neighbors[i]);
            }

            // Apply each operation in sequence
            foreach (var operation in profile.operations)
            {
                ComputeShader shader = GetShaderForMode(operation.mode);
                if (shader == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Shader not assigned for {operation.mode}", "OK");
                    return;
                }

                LocalTileErosion.ErodeLocalGrid(
                    gridTerrains,
                    _centerTerrain,
                    operation.mode,
                    shader,
                    _smoothSharpenShader,
                    operation.h_Iterations, operation.h_Resolution, operation.h_ErosionSteps,
                    operation.w_Windiness, operation.w_WindDirection, operation.w_Resolution,
                    operation.t_TerraceSpacing, operation.t_TerraceAngle,
                    operation.s_SmoothingWidth,
                    operation.sh_SharpenStrength, operation.sh_SharpenIterations, operation.sh_PeakMixStrength);
            }

            // Mark center terrain and neighbors as eroded
#if UNITY_EDITOR
            foreach (var terrain in gridTerrains)
            {
                if (terrain != null)
                {
                    var tileComponent = terrain.transform.parent?.GetComponent<TerrainTileComponent>();
                    if (tileComponent != null)
                    {
                        tileComponent.HasBeenEroded = true;
                        tileComponent.LastErosionProfileUsed = profile.name;
                        EditorUtility.SetDirty(tileComponent);
                    }
                }
            }
#endif

            EditorUtility.DisplayDialog("Success", $"Applied profile: {profile.name}", "OK");
        }

        private ComputeShader GetShaderForMode(ErosionMode mode)
        {
            return mode switch
            {
                ErosionMode.Hydraulic => _hydraulicShader,
                ErosionMode.Wind => _windShader,
                ErosionMode.Terrace => _terraceShader,
                ErosionMode.Smooth or ErosionMode.Sharpen => _smoothSharpenShader,
                _ => null
            };
        }

        private void DrawParametersForMode()
        {
            switch (_erosionMode)
            {
                case ErosionMode.Hydraulic:
                    _h_Iterations = EditorGUILayout.IntField("Iterations", _h_Iterations);
                    _h_Resolution = EditorGUILayout.IntField("Resolution", _h_Resolution);
                    _h_ErosionSteps = EditorGUILayout.IntSlider("Erosion Steps", _h_ErosionSteps, 1, 8);
                    break;

                case ErosionMode.Wind:
                    _w_Windiness = EditorGUILayout.Slider("Windiness", _w_Windiness, 0, 5);
                    _w_WindDirection = EditorGUILayout.Vector2Field("Wind Direction", _w_WindDirection);
                    _w_Resolution = EditorGUILayout.IntField("Resolution", _w_Resolution);
                    break;

                case ErosionMode.Terrace:
                    _t_TerraceSpacing = EditorGUILayout.FloatField("Terrace Spacing", _t_TerraceSpacing);
                    _t_TerraceAngle = EditorGUILayout.Slider("Terrace Angle", _t_TerraceAngle, 0, 90);
                    break;

                case ErosionMode.Smooth:
                    _s_SmoothingWidth = EditorGUILayout.IntField("Smoothing Width", _s_SmoothingWidth);
                    break;

                case ErosionMode.Sharpen:
                    _sh_SharpenStrength = EditorGUILayout.FloatField("Sharpen Strength", _sh_SharpenStrength);
                    _sh_SharpenIterations = EditorGUILayout.IntField("Sharpen Iterations", _sh_SharpenIterations);
                    _sh_PeakMixStrength = EditorGUILayout.Slider("Peak Mix Strength", _sh_PeakMixStrength, 0, 1);
                    break;
            }
        }

        private void FindNeighbors()
        {
            // Parse center terrain name to get grid coordinates
            if (!ParseTerrainName(_centerTerrain.name, out int centerX, out int centerY))
            {
                Debug.LogError($"Failed to parse terrain name: {_centerTerrain.name}. Expected format: Terrain_X_Y or LOD0_Terrain_X_Y");
                return;
            }

            // Extract LOD prefix if it exists (e.g., "LOD0_" from "LOD0_Terrain_29_24")
            string lodPrefix = "";
            if (_centerTerrain.name.Contains("LOD"))
            {
                int terrainIndex = _centerTerrain.name.IndexOf("Terrain");
                if (terrainIndex > 0)
                    lodPrefix = _centerTerrain.name.Substring(0, terrainIndex);
            }

            Debug.Log($"Center terrain: {_centerTerrain.name} at coordinates ({centerX}, {centerY}), LOD prefix: '{lodPrefix}'");

            Terrain[] allTerrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            var activeScene = EditorSceneManager.GetActiveScene();

            // Create a lookup dictionary for quick terrain access
            Dictionary<string, Terrain> terrainLookup = new Dictionary<string, Terrain>();
            foreach (var terrain in allTerrains)
            {
                if (terrain.gameObject.scene == activeScene)
                    terrainLookup[terrain.name] = terrain;
            }

            Debug.Log($"Found {terrainLookup.Count} terrains in scene");

            // Neighbor offsets: NW, N, NE, W, E, SW, S, SE
            int[,] offsets = new int[,]
            {
                { -1, -1 },   // NW
                { 0, -1 },    // N
                { 1, -1 },    // NE
                { -1, 0 },    // W
                { 1, 0 },     // E
                { -1, 1 },    // SW
                { 0, 1 },     // S
                { 1, 1 }      // SE
            };

            string[] neighborNames = { "NW", "N", "NE", "W", "E", "SW", "S", "SE" };

            for (int i = 0; i < 8; i++)
            {
                int neighborX = centerX + offsets[i, 0];
                int neighborY = centerY + offsets[i, 1];
                string neighborName = $"{lodPrefix}Terrain_{neighborX}_{neighborY}";

                if (terrainLookup.ContainsKey(neighborName))
                {
                    _neighbors[i] = terrainLookup[neighborName];
                    Debug.Log($"✓ Found {neighborNames[i]} ({neighborName})");
                }
                else
                {
                    _neighbors[i] = null;
                    Debug.LogWarning($"✗ Missing {neighborNames[i]} ({neighborName})");
                }
            }
        }

        private bool ParseTerrainName(string name, out int x, out int y)
        {
            x = 0;
            y = 0;

            // Handle LOD prefix: LOD0_Terrain_X_Y or Terrain_X_Y
            string[] parts = name.Split('_');

            // Need at least 3 parts (Terrain, X, Y) or 4 parts (LOD0, Terrain, X, Y)
            if (parts.Length < 3)
                return false;

            // Check if it has LOD prefix
            int xIndex = parts.Length - 2;
            int yIndex = parts.Length - 1;

            return int.TryParse(parts[xIndex], out x) && int.TryParse(parts[yIndex], out y);
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("3x3 Tile Grid", EditorStyles.boldLabel);

            // Simple grid display
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50);
            EditorGUILayout.BeginVertical(GUILayout.Width(_gridDisplaySize));

            // Row 1: NW, N, NE
            EditorGUILayout.BeginHorizontal();
            DrawGridTile(_neighbors[0], "NW");
            DrawGridTile(_neighbors[1], "N");
            DrawGridTile(_neighbors[2], "NE");
            EditorGUILayout.EndHorizontal();

            // Row 2: W, CENTER, E
            EditorGUILayout.BeginHorizontal();
            DrawGridTile(_neighbors[3], "W");
            DrawGridTile(_centerTerrain, "CENTER", true);
            DrawGridTile(_neighbors[4], "E");
            EditorGUILayout.EndHorizontal();

            // Row 3: SW, S, SE
            EditorGUILayout.BeginHorizontal();
            DrawGridTile(_neighbors[5], "SW");
            DrawGridTile(_neighbors[6], "S");
            DrawGridTile(_neighbors[7], "SE");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridTile(Terrain terrain, string label, bool isCenter = false)
        {
            GUI.color = isCenter ? Color.cyan : (terrain != null ? Color.green : Color.red);
            string buttonText = terrain != null ? (isCenter ? "CENTER" : label) : "MISSING";

            if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Width(60), GUILayout.Height(60)), buttonText, EditorStyles.miniButton))
            {
                // Button clicked
                if (terrain != null)
                {
                    // Select in scene hierarchy and inspector
                    Selection.activeGameObject = terrain.gameObject;
                    EditorGUIUtility.PingObject(terrain.gameObject);

                    // Set as active tile if not already the center
                    if (!isCenter)
                    {
                        _centerTerrain = terrain;
                        _previousCenterTerrain = terrain;
                        FindNeighbors();
                    }
                }
            }

            GUI.color = Color.white;
        }

        private void ErodeGrid()
        {
            if (_centerTerrain == null)
                return;

            // Verify required shader for selected mode
            ComputeShader shader = null;
            switch (_erosionMode)
            {
                case ErosionMode.Hydraulic:
                    if (_hydraulicShader == null) { Debug.LogError("Hydraulic shader not assigned"); return; }
                    shader = _hydraulicShader;
                    break;
                case ErosionMode.Wind:
                    if (_windShader == null) { Debug.LogError("Wind shader not assigned"); return; }
                    shader = _windShader;
                    break;
                case ErosionMode.Terrace:
                    if (_terraceShader == null) { Debug.LogError("Terrace shader not assigned"); return; }
                    shader = _terraceShader;
                    break;
                case ErosionMode.Smooth:
                case ErosionMode.Sharpen:
                    if (_smoothSharpenShader == null) { Debug.LogError("Smooth/Sharpen shader not assigned"); return; }
                    shader = _smoothSharpenShader;
                    break;
            }

            // Collect all 9 terrains (including center and neighbors)
            List<Terrain> gridTerrains = new List<Terrain> { _centerTerrain };
            for (int i = 0; i < 8; i++)
            {
                if (_neighbors[i] != null)
                    gridTerrains.Add(_neighbors[i]);
            }

            LocalTileErosion.ErodeLocalGrid(
                gridTerrains,
                _centerTerrain,
                _erosionMode,
                shader,
                _smoothSharpenShader,
                _h_Iterations, _h_Resolution, _h_ErosionSteps,
                _w_Windiness, _w_WindDirection, _w_Resolution,
                _t_TerraceSpacing, _t_TerraceAngle,
                _s_SmoothingWidth,
                _sh_SharpenStrength, _sh_SharpenIterations, _sh_PeakMixStrength);

            // Mark center terrain and neighbors as eroded
#if UNITY_EDITOR
            foreach (var terrain in gridTerrains)
            {
                if (terrain != null)
                {
                    var tileComponent = terrain.transform.parent?.GetComponent<TerrainTileComponent>();
                    if (tileComponent != null)
                    {
                        tileComponent.HasBeenEroded = true;
                        tileComponent.LastErosionProfileUsed = _erosionMode.ToString();
                        EditorUtility.SetDirty(tileComponent);
                    }
                }
            }
#endif

            EditorUtility.DisplayDialog("Success", "Erosion completed on 3x3 grid.", "OK");
        }

        private void SaveProfiles()
        {
            try
            {
                // Create directory if it doesn't exist
                string directoryPath = Path.GetDirectoryName(PROFILES_PATH);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Wrap profiles in serializable container
                var data = new ErosionProfilesData { profiles = _profiles };
                string json = JsonUtility.ToJson(data, true);

                File.WriteAllText(PROFILES_PATH, json);
                Debug.Log($"Saved {_profiles.Count} erosion profiles to {PROFILES_PATH}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save erosion profiles: {e.Message}");
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(PROFILES_PATH))
                {
                    string json = File.ReadAllText(PROFILES_PATH);
                    var data = JsonUtility.FromJson<ErosionProfilesData>(json);
                    _profiles = data.profiles ?? new List<ErosionProfile>();
                    Debug.Log($"Loaded {_profiles.Count} erosion profiles from {PROFILES_PATH}");
                }
                else
                {
                    _profiles = new List<ErosionProfile>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load erosion profiles: {e.Message}");
                _profiles = new List<ErosionProfile>();
            }
        }

        private void TargetTileByCoordinates(int targetX, int targetZ)
        {
            Terrain[] allTerrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            var activeScene = EditorSceneManager.GetActiveScene();

            foreach (var terrain in allTerrains)
            {
                if (terrain.gameObject.scene != activeScene)
                    continue;

#if UNITY_EDITOR
                // Check for tile component on the terrain's parent (chunkRoot)
                var tileComponent = terrain.transform.parent?.GetComponent<Game.Runtime.Shared.TerrainTileComponent>();
                if (tileComponent != null && tileComponent.TileX == targetX && tileComponent.TileZ == targetZ)
                {
                    _centerTerrain = terrain;
                    _previousCenterTerrain = terrain;
                    FindNeighbors();
                    Selection.activeGameObject = terrain.gameObject;
                    EditorGUIUtility.PingObject(terrain.gameObject);
                    Debug.Log($"Targeted tile at ({targetX}, {targetZ})");
                    return;
                }
#endif
            }

            Debug.LogWarning($"No terrain found with coordinates ({targetX}, {targetZ})");
        }
    }

    /// <summary>
    /// Performs erosion on a 3x3 tile grid with natural flow across boundaries.
    /// </summary>
    public static class LocalTileErosion
    {
        public static void ErodeLocalGrid(
            List<Terrain> gridTerrains,
            Terrain centerTerrain,
            ErosionMode mode,
            ComputeShader shader,
            ComputeShader smoothSharpenShader,
            int h_iterations, int h_resolution, int h_erosionSteps,
            float w_windiness, Vector2 w_windDirection, int w_resolution,
            float t_terraceSpacing, float t_terraceAngle,
            int s_smoothingWidth,
            float sh_sharpenStrength, int sh_sharpenIterations, float sh_peakMixStrength)
        {
            if (gridTerrains.Count == 0 || centerTerrain == null || shader == null)
                return;

            int tileResolution = centerTerrain.terrainData.heightmapResolution;

            // Load all heightmaps
            Dictionary<Terrain, float[,]> originalHeightmaps = new Dictionary<Terrain, float[,]>();
            foreach (var terrain in gridTerrains)
            {
                if (terrain?.terrainData != null)
                {
                    originalHeightmaps[terrain] = terrain.terrainData.GetHeights(0, 0, tileResolution, tileResolution);
                }
            }

            // Stitch the 3x3 tiles into a unified heightmap
            float[,] unifiedHeightmap = StitchGridToUnified(originalHeightmaps, centerTerrain, tileResolution);

            // Apply erosion to unified heightmap based on selected mode
            float[,] erodedUnified = unifiedHeightmap;

            switch (mode)
            {
                case ErosionMode.Hydraulic:
                    erodedUnified = ApplyHydraulicErosion(shader, erodedUnified, tileResolution, h_iterations, h_resolution, h_erosionSteps);
                    break;
                case ErosionMode.Wind:
                    erodedUnified = ApplyWindErosion(shader, erodedUnified, tileResolution, w_windiness, w_windDirection, w_resolution, smoothSharpenShader);
                    break;
                case ErosionMode.Terrace:
                    erodedUnified = ApplyTerraceErosion(shader, erodedUnified, tileResolution, t_terraceSpacing, t_terraceAngle, centerTerrain, smoothSharpenShader);
                    break;
                case ErosionMode.Smooth:
                    erodedUnified = ApplySmoothErosion(shader, erodedUnified, tileResolution, s_smoothingWidth);
                    break;
                case ErosionMode.Sharpen:
                    erodedUnified = ApplySharpenErosion(shader, erodedUnified, tileResolution, sh_sharpenStrength, sh_sharpenIterations, sh_peakMixStrength);
                    break;
            }

            // Extract and apply center tile
            float[,] erodedCenter = ExtractCenterTile(erodedUnified, tileResolution);
            if (centerTerrain?.terrainData != null)
            {
                EditorUtility.DisplayProgressBar("Erosion", "Applying results to tiles...", 0.3f);
                Undo.RecordObject(centerTerrain.terrainData, "Local Erosion");
                centerTerrain.terrainData.SetHeights(0, 0, erodedCenter);
                EditorUtility.SetDirty(centerTerrain.terrainData);
            }

            // Extract and apply eroded edges to neighboring tiles
            ApplyErodedEdgesToNeighbors(erodedUnified, originalHeightmaps, centerTerrain, gridTerrains, tileResolution);

            EditorUtility.ClearProgressBar();
        }

        private static float[,] ApplyHydraulicErosion(ComputeShader shader, float[,] heightmap, int tileResolution, int iterations, int resolution, int erosionSteps)
        {
            float[,] result = heightmap;
            int currentRes = tileResolution;
            int currentIterations = iterations;

            for (int step = 0; step < erosionSteps; step++)
            {
                result = Hydraulics.Erode(
                    shader,
                    currentIterations,
                    result,
                    result.GetLength(0),
                    currentRes);

                currentIterations *= 2;
                currentRes *= 2;
            }

            return result;
        }

        private static float[,] ApplyWindErosion(ComputeShader shader, float[,] heightmap, int tileResolution, float windiness, Vector2 windDirection, int resolution, ComputeShader smoothShader)
        {
            // Wind erosion using EDEN_Wind
            Vector2 normalizedDir = windDirection.normalized;
            int iterations = (int)(600000 * windiness);
            int unifiedSize = tileResolution * 3;

            float[,] result = GapperGames.EDEN_Wind.Erode(
                shader,
                iterations,
                heightmap,
                unifiedSize,
                resolution,
                normalizedDir);

            // Apply smoothing to reduce wind erosion intensity
            result = ApplySmoothPass(smoothShader, result, unifiedSize, 4);
            result = ApplySmoothPass(smoothShader, result, unifiedSize, 2);

            return result;
        }

        private static float[,] ApplyTerraceErosion(ComputeShader shader, float[,] heightmap, int tileResolution, float terraceSpacing, float terraceAngle, Terrain centerTerrain, ComputeShader smoothShader)
        {
            // Terrace erosion using EDEN compute shader
            int unifiedSize = tileResolution * 3;
            Texture2D heightmapTex = ConvertHeightmapToTexture(heightmap, unifiedSize);
            RenderTexture rt = CreateRT(unifiedSize);

            // Get actual terrain dimensions from center terrain
            float terrainHeight = centerTerrain?.terrainData?.size.y ?? 1.0f;
            float terrainWidth = centerTerrain?.terrainData?.size.x ?? 1.0f;

            // Setup Compute Shader
            shader.SetTexture(shader.FindKernel("Terrace"), "Result", rt);
            shader.SetTexture(shader.FindKernel("Terrace"), "heightMap", heightmapTex);
            shader.SetTexture(shader.FindKernel("Terrace"), "normalHeightmap", heightmapTex);
            shader.SetInt("heightmapResolution", unifiedSize);
            shader.SetFloat("terraceSpacing", terraceSpacing);
            shader.SetFloat("angle", terraceAngle);
            shader.SetFloat("terrainHeight", terrainHeight);
            shader.SetFloat("terrainWidth", terrainWidth);

            shader.Dispatch(shader.FindKernel("Terrace"), rt.width / 8, rt.height / 8, 1);

            float[,] result = ReadRenderTextureToCPU(rt, unifiedSize);

            rt.Release();
            Object.DestroyImmediate(heightmapTex);
            Object.DestroyImmediate(rt);

            // Apply smoothing to blend terrace effects
            result = ApplySmoothPass(smoothShader, result, unifiedSize, 1);

            return result;
        }

        private static float[,] ApplySmoothErosion(ComputeShader shader, float[,] heightmap, int tileResolution, int smoothingWidth)
        {
            // Smooth erosion using EDEN compute shader
            int unifiedSize = tileResolution * 3;
            Texture2D heightmapTex = ConvertHeightmapToTexture(heightmap, unifiedSize);
            RenderTexture rt = CreateRT(unifiedSize);

            // Setup Compute Shader
            shader.SetTexture(shader.FindKernel("Smooth"), "Result", rt);
            shader.SetTexture(shader.FindKernel("Smooth"), "heightMap", heightmapTex);
            shader.SetInt("heightSize", unifiedSize);
            shader.SetInt("sWidth", smoothingWidth);
            shader.Dispatch(shader.FindKernel("Smooth"), rt.width / 8, rt.height / 8, 1);

            float[,] result = ReadRenderTextureToCPU(rt, unifiedSize);

            rt.Release();
            Object.DestroyImmediate(heightmapTex);
            Object.DestroyImmediate(rt);

            return result;
        }

        private static float[,] ApplySharpenErosion(ComputeShader shader, float[,] heightmap, int tileResolution, float sharpenStrength, int sharpenIterations, float peakMixStrength)
        {
            // Sharpen erosion using EDEN compute shader
            int unifiedSize = tileResolution * 3;
            float[,] result = (float[,])heightmap.Clone();

            for (int i = 0; i < sharpenIterations; i++)
            {
                Texture2D heightmapTex = ConvertHeightmapToTexture(result, unifiedSize);
                RenderTexture rt = CreateRT(unifiedSize);

                // Setup Compute Shader
                shader.SetTexture(shader.FindKernel("Sharpen"), "Result", rt);
                shader.SetTexture(shader.FindKernel("Sharpen"), "heightMap", heightmapTex);
                shader.SetInt("heightSize", unifiedSize);
                shader.SetFloat("shstr", sharpenStrength);
                shader.SetFloat("pmstr", peakMixStrength);
                shader.Dispatch(shader.FindKernel("Sharpen"), rt.width / 8, rt.height / 8, 1);

                result = ReadRenderTextureToCPU(rt, unifiedSize);

                rt.Release();
                Object.DestroyImmediate(heightmapTex);
                Object.DestroyImmediate(rt);
            }

            return result;
        }

        private static float[,] StitchGridToUnified(Dictionary<Terrain, float[,]> heightmaps, Terrain centerTerrain, int tileResolution)
        {
            int unifiedSize = tileResolution * 3;
            float[,] unified = new float[unifiedSize, unifiedSize];

            // Parse center terrain name to get grid coordinates
            if (!ParseTerrainCoords(centerTerrain.name, out int centerX, out int centerY))
                return unified;

            // Extract LOD prefix if it exists
            string lodPrefix = "";
            if (centerTerrain.name.Contains("LOD"))
            {
                int terrainIndex = centerTerrain.name.IndexOf("Terrain");
                if (terrainIndex > 0)
                    lodPrefix = centerTerrain.name.Substring(0, terrainIndex);
            }

            // Create a reverse lookup: name -> terrain
            Dictionary<string, Terrain> nameToTerrain = new Dictionary<string, Terrain>();
            foreach (var kvp in heightmaps)
            {
                nameToTerrain[kvp.Key.name] = kvp.Key;
            }

            // Neighbor offsets and their unified grid positions
            int[,] offsets = new int[,]
            {
                { -1, -1, 0, 0 },   // NW = grid (0,0)
                { 0, -1, 1, 0 },    // N = grid (1,0)
                { 1, -1, 2, 0 },    // NE = grid (2,0)
                { -1, 0, 0, 1 },    // W = grid (0,1)
                { 0, 0, 1, 1 },     // CENTER = grid (1,1)
                { 1, 0, 2, 1 },     // E = grid (2,1)
                { -1, 1, 0, 2 },    // SW = grid (0,2)
                { 0, 1, 1, 2 },     // S = grid (1,2)
                { 1, 1, 2, 2 }      // SE = grid (2,2)
            };

            for (int idx = 0; idx < 9; idx++)
            {
                int neighborX = centerX + offsets[idx, 0];
                int neighborY = centerY + offsets[idx, 1];
                int gridX = offsets[idx, 2];
                int gridY = offsets[idx, 3];

                string neighborName = $"{lodPrefix}Terrain_{neighborX}_{neighborY}";

                if (nameToTerrain.TryGetValue(neighborName, out Terrain terrain) && heightmaps.TryGetValue(terrain, out var heightmap))
                {
                    int baseX = gridX * tileResolution;
                    int baseZ = gridY * tileResolution;

                    for (int z = 0; z < tileResolution; z++)
                    {
                        for (int x = 0; x < tileResolution; x++)
                        {
                            unified[baseZ + z, baseX + x] = heightmap[z, x];
                        }
                    }
                }
            }

            return unified;
        }

        private static void ApplyErodedEdgesToNeighbors(float[,] erodedUnified, Dictionary<Terrain, float[,]> originalHeightmaps, Terrain centerTerrain, List<Terrain> gridTerrains, int tileResolution)
        {
            // Parse center terrain name to get grid coordinates
            if (!ParseTerrainCoords(centerTerrain.name, out int centerX, out int centerY))
                return;

            // Extract LOD prefix if it exists
            string lodPrefix = "";
            if (centerTerrain.name.Contains("LOD"))
            {
                int terrainIndex = centerTerrain.name.IndexOf("Terrain");
                if (terrainIndex > 0)
                    lodPrefix = centerTerrain.name.Substring(0, terrainIndex);
            }

            // Neighbor offsets: NW, N, NE, W, E, SW, S, SE
            int[,] offsets = new int[,]
            {
                { -1, -1 },   // NW = 0
                { 0, -1 },    // N = 1
                { 1, -1 },    // NE = 2
                { -1, 0 },    // W = 3
                { 1, 0 },     // E = 4
                { -1, 1 },    // SW = 5
                { 0, 1 },     // S = 6
                { 1, 1 }      // SE = 7
            };

            // Map neighbor indices to grid positions in unified heightmap
            int[,] gridPositions = new int[,]
            {
                { 0, 0 },  // NW -> (0,0)
                { 1, 0 },  // N -> (1,0)
                { 2, 0 },  // NE -> (2,0)
                { 0, 1 },  // W -> (0,1)
                { 2, 1 },  // E -> (2,1)
                { 0, 2 },  // SW -> (0,2)
                { 1, 2 },  // S -> (1,2)
                { 2, 2 }   // SE -> (2,2)
            };

            for (int i = 0; i < gridTerrains.Count; i++)
            {
                Terrain neighbor = gridTerrains[i];
                if (neighbor == null || neighbor == centerTerrain || !originalHeightmaps.ContainsKey(neighbor))
                    continue;

                // Parse neighbor terrain name
                if (!ParseTerrainCoords(neighbor.name, out int neighborX, out int neighborY))
                    continue;

                int neighborIdx = -1;

                // Find which neighbor this is based on coordinate offsets
                for (int j = 0; j < 8; j++)
                {
                    if (neighborX == centerX + offsets[j, 0] && neighborY == centerY + offsets[j, 1])
                    {
                        neighborIdx = j;
                        break;
                    }
                }

                if (neighborIdx < 0)
                    continue;

                // Get neighbor's position in unified grid using lookup table
                int gridX = gridPositions[neighborIdx, 0];
                int gridZ = gridPositions[neighborIdx, 1];

                // Extract and apply entire eroded tile with falloff based on distance from center
                float[,] neighborCopy = (float[,])originalHeightmaps[neighbor].Clone();
                float[,] erodedNeighbor = ExtractNeighborTile(erodedUnified, gridX, gridZ, tileResolution);
                ApplyWithDistanceFalloff(neighborCopy, erodedNeighbor, gridX, gridZ, tileResolution);

                // Apply back to terrain
                EditorUtility.DisplayProgressBar("Erosion", $"Applying erosion to neighbor...", 0.5f);
                Undo.RecordObject(neighbor.terrainData, "Local Erosion");
                neighbor.terrainData.SetHeights(0, 0, neighborCopy);
                EditorUtility.SetDirty(neighbor.terrainData);
            }
        }

        private static float[,] ExtractNeighborTile(float[,] unified, int gridX, int gridZ, int tileResolution)
        {
            float[,] tile = new float[tileResolution, tileResolution];
            int baseX = gridX * tileResolution;
            int baseZ = gridZ * tileResolution;

            for (int z = 0; z < tileResolution; z++)
            {
                for (int x = 0; x < tileResolution; x++)
                {
                    tile[z, x] = unified[baseZ + z, baseX + x];
                }
            }

            return tile;
        }

        private static void ApplyWithDistanceFalloff(float[,] original, float[,] eroded, int gridX, int gridZ, int tileResolution)
        {
            // Apply eroded heightmap with sharp falloff based on distance from the shared edge with center tile
            // Strength is 100% at edge and drops to 0% at 25% into the tile
            // Center tile is at grid (1,1)

            float falloffRange = tileResolution * 0.25f; // 25% of tile width/height

            for (int z = 0; z < tileResolution; z++)
            {
                for (int x = 0; x < tileResolution; x++)
                {
                    float falloff = 1.0f;

                    // Calculate falloff based on which neighbor this is
                    if (gridX == 0) // West neighbor - distance from right edge (x=tileResolution-1)
                    {
                        float distance = tileResolution - 1 - x;
                        falloff = Mathf.Max(0, 1.0f - (distance / falloffRange));
                    }
                    else if (gridX == 2) // East neighbor - distance from left edge (x=0)
                    {
                        float distance = x;
                        falloff = Mathf.Max(0, 1.0f - (distance / falloffRange));
                    }

                    if (gridZ == 0) // North neighbor - distance from bottom edge (z=tileResolution-1)
                    {
                        float distance = tileResolution - 1 - z;
                        float zFalloff = Mathf.Max(0, 1.0f - (distance / falloffRange));
                        if (gridX != 1) falloff *= zFalloff; // For corners, multiply both axes
                        else falloff = zFalloff; // For pure north/south, use Z only
                    }
                    else if (gridZ == 2) // South neighbor - distance from top edge (z=0)
                    {
                        float distance = z;
                        float zFalloff = Mathf.Max(0, 1.0f - (distance / falloffRange));
                        if (gridX != 1) falloff *= zFalloff; // For corners, multiply both axes
                        else falloff = zFalloff; // For pure north/south, use Z only
                    }

                    // Blend original and eroded using falloff
                    original[z, x] = Mathf.Lerp(original[z, x], eroded[z, x], falloff);
                }
            }
        }

        private static bool ParseTerrainCoords(string name, out int x, out int y)
        {
            x = 0;
            y = 0;

            // Handle LOD prefix: LOD0_Terrain_X_Y or Terrain_X_Y
            string[] parts = name.Split('_');

            // Need at least 3 parts (Terrain, X, Y) or 4 parts (LOD0, Terrain, X, Y)
            if (parts.Length < 3)
                return false;

            // Extract last two parts as X and Y
            int xIndex = parts.Length - 2;
            int yIndex = parts.Length - 1;

            return int.TryParse(parts[xIndex], out x) && int.TryParse(parts[yIndex], out y);
        }


        private static float[,] ExtractCenterTile(float[,] unifiedHeightmap, int tileResolution)
        {
            float[,] centerTile = new float[tileResolution, tileResolution];
            int baseX = tileResolution;
            int baseZ = tileResolution;

            for (int z = 0; z < tileResolution; z++)
            {
                for (int x = 0; x < tileResolution; x++)
                {
                    centerTile[z, x] = unifiedHeightmap[baseZ + z, baseX + x];
                }
            }

            return centerTile;
        }

        private static float[,] ApplySmoothPass(ComputeShader shader, float[,] heightmap, int resolution, int smoothingWidth)
        {
            // Apply a single smooth pass
            Texture2D heightmapTex = ConvertHeightmapToTexture(heightmap, resolution);
            RenderTexture rt = CreateRT(resolution);

            shader.SetTexture(shader.FindKernel("Smooth"), "Result", rt);
            shader.SetTexture(shader.FindKernel("Smooth"), "heightMap", heightmapTex);
            shader.SetInt("heightSize", resolution);
            shader.SetInt("sWidth", smoothingWidth);
            shader.Dispatch(shader.FindKernel("Smooth"), rt.width / 8, rt.height / 8, 1);

            float[,] result = ReadRenderTextureToCPU(rt, resolution);

            rt.Release();
            Object.DestroyImmediate(heightmapTex);
            Object.DestroyImmediate(rt);

            return result;
        }

        private static RenderTexture CreateRT(int size)
        {
            RenderTexture rt = new RenderTexture(size, size, 256, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        private static Texture2D ConvertHeightmapToTexture(float[,] heightMap, int resolution)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    int pixelX = (int)(((float)x / resolution) * tex.width);
                    int pixelY = (int)(((float)y / resolution) * tex.height);
                    float heightValue = heightMap[x, y];
                    tex.SetPixel(pixelX, pixelY, Color.white * heightValue);
                }
            }

            tex.Apply();
            return tex;
        }

        private static float[,] ReadRenderTextureToCPU(RenderTexture rt, int resolution)
        {
            Rect rectReadPicture = new Rect(0, 0, rt.width, rt.height);
            RenderTexture.active = rt;

            Texture2D rtTex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            rtTex2d.filterMode = FilterMode.Point;
            rtTex2d.ReadPixels(rectReadPicture, 0, 0);
            rtTex2d.Apply();

            RenderTexture.active = null;

            float[,] heightMap = new float[resolution, resolution];
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    int pixelX = (int)(((float)x / resolution) * rtTex2d.width);
                    int pixelY = (int)(((float)y / resolution) * rtTex2d.height);
                    heightMap[x, y] = rtTex2d.GetPixel(pixelX, pixelY).r;
                }
            }

            Object.DestroyImmediate(rtTex2d);
            return heightMap;
        }
    }
}
