using Game.Runtime.Shared;
using Game.Editor.WorldEditor;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Components
{
    internal static class TerrainTileGizmoDrawer
    {
        private static GUIStyle _labelStyle;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(TerrainTileComponent tile, GizmoType gizmoType)
        {
            if (!tile.ShowPoiMarker)
                return;

            bool selected = (gizmoType & (GizmoType.Selected | GizmoType.InSelectionHierarchy)) != 0;

            if (string.IsNullOrWhiteSpace(tile.PoiLabel))
                return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            _labelStyle.normal.textColor = selected
                ? tile.PoiColor
                : new Color(tile.PoiColor.r, tile.PoiColor.g, tile.PoiColor.b, 0.7f);
            _labelStyle.fontSize = Mathf.Max(1, tile.PoiTextSize);

            // Use terrain bounds center so the label is above the actual terrain surface,
            // regardless of where the GameObject pivot sits.
            Terrain terrain = tile.GetComponentInChildren<Terrain>();
            Vector3 origin;
            if (terrain != null)
            {
                Bounds b = terrain.terrainData.bounds;
                // bounds are in terrain-local space; transform to world space
                origin = terrain.transform.TransformPoint(b.center);
                origin.y = terrain.transform.position.y + b.max.y;
            }
            else
            {
                origin = tile.transform.position;
            }

            Vector3 markerPos = origin + Vector3.up * 100f;

            // Line from terrain surface to label
            Color lineColor = tile.PoiColor;
            lineColor.a = selected ? 0.8f : 0.3f;
            using (new Handles.DrawingScope(lineColor))
                Handles.DrawLine(origin, markerPos, selected ? 2f : 1f);

            Handles.Label(markerPos, tile.PoiLabel, _labelStyle);
        }
    }

    [CustomEditor(typeof(TerrainTileComponent))]
    public class TerrainTileComponentEditor : UnityEditor.Editor
    {
        // Persisted via EditorPrefs (GUID) so the assignment survives domain
        // reloads and inspector selection changes without requiring re-assignment.
        private const string PrefKey = "NinthAge.TerrainTile.PainterShaderGUID";
        private const string PrefKeyCliffThresh = "NinthAge.TerrainTile.CliffThreshold";
        private const string PrefKeySlopeThresh = "NinthAge.TerrainTile.SlopeThreshold";
        private const string PrefKeyTalusThresh = "NinthAge.TerrainTile.TalusThreshold";
        private const string PrefKeyBlendWidth = "NinthAge.TerrainTile.BlendWidth";
        private const string PrefKeyAlphamapRes = "NinthAge.TerrainTile.AlphamapResolution";
        private const string PrefKeySnowElevation = "NinthAge.TerrainTile.SnowElevation";
        private const string PrefKeySnowHBlend = "NinthAge.TerrainTile.SnowHeightBlend";
        private const string PrefKeySnowSlopeLimit = "NinthAge.TerrainTile.SnowSlopeLimit";
        private const string PrefKeySnowSlopeBlend = "NinthAge.TerrainTile.SnowSlopeBlend";
        private const string PrefKeyIceElevation = "NinthAge.TerrainTile.IceElevation";
        private const string PrefKeyIceHBlend = "NinthAge.TerrainTile.IceHeightBlend";
        private const string PrefKeyIceSlopeLimit = "NinthAge.TerrainTile.IceSlopeLimit";
        private const string PrefKeyIceSlopeBlend = "NinthAge.TerrainTile.IceSlopeBlend";
        private const string PrefKeyTileSize = "NinthAge.TerrainTile.TextureTileSize";

        private static readonly int[] AlphamapResOptions = { 512, 1024, 2048, 4096 };
        private static readonly GUIContent[] AlphamapResLabels =
        {
            new GUIContent("512"),
            new GUIContent("1024"),
            new GUIContent("2048"),
            new GUIContent("4096"),
        };

        private ComputeShader _painterShader;
        private float _slopeThreshold;
        private float _talusThreshold;
        private float _cliffThreshold;
        private float _blendWidth;
        private int _alphamapResolution;
        private float _snowElevation;
        private float _snowHeightBlend;
        private float _snowSlopeLimit;
        private float _snowSlopeBlend;
        private float _iceElevation;
        private float _iceHeightBlend;
        private float _iceSlopeLimit;
        private float _iceSlopeBlend;
        private float _textureTileSize;

        private void OnEnable()
        {
            // Restore the shader reference from the saved GUID.
            string guid = EditorPrefs.GetString(PrefKey, string.Empty);
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    _painterShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            }

            _slopeThreshold = EditorPrefs.GetFloat(PrefKeySlopeThresh, TerrainAutoPainterHelper.DefaultSlopeThreshold);
            _talusThreshold = EditorPrefs.GetFloat(PrefKeyTalusThresh, TerrainAutoPainterHelper.DefaultTalusThreshold);
            _cliffThreshold = EditorPrefs.GetFloat(PrefKeyCliffThresh, TerrainAutoPainterHelper.DefaultCliffThreshold);
            _blendWidth = EditorPrefs.GetFloat(PrefKeyBlendWidth, TerrainAutoPainterHelper.DefaultBlendWidth);
            _alphamapResolution = EditorPrefs.GetInt(PrefKeyAlphamapRes, TerrainAutoPainterHelper.DefaultAlphamapResolution);
            _snowElevation = EditorPrefs.GetFloat(PrefKeySnowElevation, TerrainAutoPainterHelper.DefaultSnowElevation);
            _snowHeightBlend = EditorPrefs.GetFloat(PrefKeySnowHBlend, TerrainAutoPainterHelper.DefaultSnowHeightBlend);
            _snowSlopeLimit = EditorPrefs.GetFloat(PrefKeySnowSlopeLimit, TerrainAutoPainterHelper.DefaultSnowSlopeLimit);
            _snowSlopeBlend = EditorPrefs.GetFloat(PrefKeySnowSlopeBlend, TerrainAutoPainterHelper.DefaultSnowSlopeBlend);
            _iceElevation = EditorPrefs.GetFloat(PrefKeyIceElevation, TerrainAutoPainterHelper.DefaultIceElevation);
            _iceHeightBlend = EditorPrefs.GetFloat(PrefKeyIceHBlend, TerrainAutoPainterHelper.DefaultIceHeightBlend);
            _iceSlopeLimit = EditorPrefs.GetFloat(PrefKeyIceSlopeLimit, TerrainAutoPainterHelper.DefaultIceSlopeLimit);
            _iceSlopeBlend = EditorPrefs.GetFloat(PrefKeyIceSlopeBlend, TerrainAutoPainterHelper.DefaultIceSlopeBlend);
            _textureTileSize = EditorPrefs.GetFloat(PrefKeyTileSize, TerrainAutoPainterHelper.DefaultTextureTileSize);
        }

        public override void OnInspectorGUI()
        {
            TerrainTileComponent terrainTile = (TerrainTileComponent)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Tile Coordinates", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Display coordinates in a nice formatted box
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.LabelField("Tile Position", GUILayout.Width(80));
            EditorGUILayout.LabelField($"({terrainTile.TileX}, {terrainTile.TileZ})", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Erosion State", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Display erosion status
            GUI.color = terrainTile.HasBeenEroded ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.LabelField("Eroded", GUILayout.Width(80));
            EditorGUILayout.LabelField(terrainTile.HasBeenEroded ? "Yes" : "No", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            // Display erosion profile if available
            if (terrainTile.HasBeenEroded && !string.IsNullOrEmpty(terrainTile.LastErosionProfileUsed))
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                EditorGUILayout.LabelField("Last Profile", GUILayout.Width(80));
                EditorGUILayout.LabelField(terrainTile.LastErosionProfileUsed, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This component is only present in edit mode for debugging and organization purposes.", MessageType.Info);

            // ── Zone & Texturing ──────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Zone & Texturing", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Zone ID
            EditorGUI.BeginChangeCheck();
            int newZoneID = EditorGUILayout.IntField("Zone ID", terrainTile.ZoneID);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(terrainTile, "Set Zone ID");
                terrainTile.ZoneID = newZoneID;
                EditorUtility.SetDirty(terrainTile);
            }

            // Zone Definition asset reference
            EditorGUI.BeginChangeCheck();
            ZoneDefinition newZone = (ZoneDefinition)EditorGUILayout.ObjectField(
                "Zone Definition",
                terrainTile.ZoneDefinition,
                typeof(ZoneDefinition),
                allowSceneObjects: false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(terrainTile, "Set Zone Definition");
                terrainTile.ZoneDefinition = newZone;
                EditorUtility.SetDirty(terrainTile);
            }

            // Compute shader — persisted across domain reloads via EditorPrefs GUID
            EditorGUI.BeginChangeCheck();
            ComputeShader newShader = (ComputeShader)EditorGUILayout.ObjectField(
                new GUIContent("Auto-Paint Shader", "Assign the TerrainAutoPainter compute shader asset."),
                _painterShader, typeof(ComputeShader), allowSceneObjects: false);
            if (EditorGUI.EndChangeCheck())
            {
                _painterShader = newShader;
                string newGuid = newShader != null
                    ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newShader))
                    : string.Empty;
                EditorPrefs.SetString(PrefKey, newGuid);
            }

            // ── Classification thresholds ─────────────────────────────────────
            EditorGUILayout.LabelField("Classification Thresholds", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float newTextureTileSize = EditorGUILayout.FloatField(
                new GUIContent("Texture Tile Size (m)", "World-space size of one texture tile in metres. Controls how large each texture appears on the terrain. Default 15 m."),
                _textureTileSize);
            newTextureTileSize = Mathf.Max(0.1f, newTextureTileSize); // prevent zero/negative
            float newSlopeThreshold = EditorGUILayout.Slider(
                new GUIContent("Slope Threshold", "Slope angle in degrees above which a texel becomes mid-slope (layer 1)."),
                _slopeThreshold, 0f, 90f);
            float newTalusThreshold = EditorGUILayout.Slider(
                new GUIContent("Talus Threshold", "Slope angle in degrees above which a texel becomes talus/scree (layer 2). Should be between Slope and Cliff thresholds."),
                _talusThreshold, 0f, 90f);
            float newCliffThreshold = EditorGUILayout.Slider(
                new GUIContent("Cliff Threshold", "Slope angle in degrees above which a texel is classified as cliff (layer 3). Should be > Talus Threshold."),
                _cliffThreshold, 0f, 90f);
            float newBlendWidth = EditorGUILayout.Slider(
                new GUIContent("Blend Width", "Half-width in degrees of the smoothstep transition band between layers. Larger values give softer edges."),
                _blendWidth, 0f, 20f);

            // Alphamap resolution — set before painting; higher = sharper layer boundaries.
            int currentResIdx = System.Array.IndexOf(AlphamapResOptions, _alphamapResolution);
            if (currentResIdx < 0) currentResIdx = 1; // fallback to 1024
            int newResIdx = EditorGUILayout.Popup(
                new GUIContent("Alphamap Resolution", "Splatmap resolution. Higher values give sharper texture boundaries but use more memory."),
                currentResIdx, AlphamapResLabels);
            int newAlphamapResolution = AlphamapResOptions[newResIdx];

            // ── Snow layer (5) ──────────────────────────────────────────────
            EditorGUILayout.LabelField("Snow (Layer 4)", EditorStyles.boldLabel);
            float newSnowElevation = EditorGUILayout.FloatField(
                new GUIContent("Elevation (m)", "World-space height in metres at which snow begins to appear."),
                _snowElevation);
            float newSnowHeightBlend = EditorGUILayout.FloatField(
                new GUIContent("Height Blend (m)", "Half-width of the height fade-in/out in metres."),
                _snowHeightBlend);
            float newSnowSlopeLimit = EditorGUILayout.Slider(
                new GUIContent("Slope Limit", "Snow only appears on slopes below this angle (degrees). Steeper faces show slope bands instead."),
                _snowSlopeLimit, 0f, 90f);
            float newSnowSlopeBlend = EditorGUILayout.Slider(
                new GUIContent("Slope Blend", "Half-width in degrees of the snow slope suppression fade."),
                _snowSlopeBlend, 0f, 20f);

            // ── Ice layer (6) ───────────────────────────────────────────────
            EditorGUILayout.LabelField("Ice (Layer 5)", EditorStyles.boldLabel);
            float newIceElevation = EditorGUILayout.FloatField(
                new GUIContent("Elevation (m)", "World-space height in metres at which ice begins to appear. Takes priority over snow."),
                _iceElevation);
            float newIceHeightBlend = EditorGUILayout.FloatField(
                new GUIContent("Height Blend (m)", "Half-width of the height fade-in/out in metres."),
                _iceHeightBlend);
            float newIceSlopeLimit = EditorGUILayout.Slider(
                new GUIContent("Slope Limit", "Ice only appears on slopes below this angle (degrees). Steeper faces show slope bands instead."),
                _iceSlopeLimit, 0f, 90f);
            float newIceSlopeBlend = EditorGUILayout.Slider(
                new GUIContent("Slope Blend", "Half-width in degrees of the ice slope suppression fade."),
                _iceSlopeBlend, 0f, 20f);

            if (EditorGUI.EndChangeCheck())
            {
                _slopeThreshold = newSlopeThreshold;
                _talusThreshold = newTalusThreshold;
                _cliffThreshold = newCliffThreshold;
                _blendWidth = newBlendWidth;
                _alphamapResolution = newAlphamapResolution;
                _snowElevation = newSnowElevation;
                _snowHeightBlend = newSnowHeightBlend;
                _snowSlopeLimit = newSnowSlopeLimit;
                _snowSlopeBlend = newSnowSlopeBlend;
                _iceElevation = newIceElevation;
                _iceHeightBlend = newIceHeightBlend;
                _iceSlopeLimit = newIceSlopeLimit;
                _iceSlopeBlend = newIceSlopeBlend;
                _textureTileSize = newTextureTileSize;
                EditorPrefs.SetFloat(PrefKeySlopeThresh, _slopeThreshold);
                EditorPrefs.SetFloat(PrefKeyTalusThresh, _talusThreshold);
                EditorPrefs.SetFloat(PrefKeyCliffThresh, _cliffThreshold);
                EditorPrefs.SetFloat(PrefKeyBlendWidth, _blendWidth);
                EditorPrefs.SetInt(PrefKeyAlphamapRes, _alphamapResolution);
                EditorPrefs.SetFloat(PrefKeySnowElevation, _snowElevation);
                EditorPrefs.SetFloat(PrefKeySnowHBlend, _snowHeightBlend);
                EditorPrefs.SetFloat(PrefKeySnowSlopeLimit, _snowSlopeLimit);
                EditorPrefs.SetFloat(PrefKeySnowSlopeBlend, _snowSlopeBlend);
                EditorPrefs.SetFloat(PrefKeyIceElevation, _iceElevation);
                EditorPrefs.SetFloat(PrefKeyIceHBlend, _iceHeightBlend);
                EditorPrefs.SetFloat(PrefKeyIceSlopeLimit, _iceSlopeLimit);
                EditorPrefs.SetFloat(PrefKeyIceSlopeBlend, _iceSlopeBlend);
                EditorPrefs.SetFloat(PrefKeyTileSize, _textureTileSize);
            }

            EditorGUILayout.Space();

            // ── Auto-Paint button ─────────────────────────────────────────────
            bool canPaint = terrainTile.ZoneDefinition != null
                         && _painterShader != null
                         && terrainTile.GetComponentInChildren<Terrain>() != null;

            if (!canPaint)
            {
                if (terrainTile.GetComponentInChildren<Terrain>() == null)
                    EditorGUILayout.HelpBox("A Terrain component is required on this GameObject or a child.", MessageType.Warning);
                else if (terrainTile.ZoneDefinition == null)
                    EditorGUILayout.HelpBox("Assign a Zone Definition to enable auto-painting.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Assign the TerrainAutoPainter compute shader to enable auto-painting.", MessageType.Warning);
            }

            // ── Map Marker ────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Marker", EditorStyles.boldLabel);

            SerializedObject so = serializedObject;
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("_showPoiMarker"), new GUIContent("Show Marker"));
            using (new EditorGUI.DisabledGroupScope(!terrainTile.ShowPoiMarker))
            {
                EditorGUILayout.PropertyField(so.FindProperty("_poiLabel"), new GUIContent("Label"));
                EditorGUILayout.PropertyField(so.FindProperty("_poiTextSize"), new GUIContent("Text Size"));
                EditorGUILayout.PropertyField(so.FindProperty("_poiColor"), new GUIContent("Color"));

                if (GUILayout.Button("Rename GameObject to Label"))
                {
                    if (!string.IsNullOrWhiteSpace(terrainTile.PoiLabel))
                    {
                        Undo.RecordObject(terrainTile.gameObject, "Rename to POI Label");
                        terrainTile.gameObject.name = terrainTile.PoiLabel;
                    }
                }
            }
            so.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(!canPaint))
            {
                if (GUILayout.Button("Auto-Paint Terrain", GUILayout.Height(30)))
                {
                    try
                    {
                        // Record the TerrainData for undo before we overwrite the alphamaps.
                        Terrain terrain = terrainTile.GetComponentInChildren<Terrain>();
                        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Auto-Paint Terrain");

                        EditorUtility.DisplayProgressBar("Auto-Paint Terrain", "Running compute shader passes...", 0.0f);
                        TerrainAutoPainterHelper.PaintTile(
                            terrainTile, _painterShader,
                            _slopeThreshold, _talusThreshold, _cliffThreshold, _blendWidth, _alphamapResolution,
                            _textureTileSize,
                            _snowElevation, _snowHeightBlend, _snowSlopeLimit, _snowSlopeBlend,
                            _iceElevation, _iceHeightBlend, _iceSlopeLimit, _iceSlopeBlend);
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog(
                            "Auto-Paint Complete",
                            $"Splatmaps updated for tile ({terrainTile.TileX}, {terrainTile.TileZ}) " +
                            $"using zone '{terrainTile.ZoneDefinition.zoneName}'.",
                            "OK");
                    }
                    catch (System.Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogException(ex, terrainTile);
                        EditorUtility.DisplayDialog(
                            "Auto-Paint Failed",
                            $"An error occurred during auto-painting. See the Console for details.\n\n{ex.Message}",
                            "OK");
                    }
                }
            }
        }


    }
}
