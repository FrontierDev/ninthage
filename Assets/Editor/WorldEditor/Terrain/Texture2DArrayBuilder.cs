using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// EditorWindow that builds a Texture2DArray asset from an ordered list of
    /// Texture2D assets. Slices can be added from the Project window selection,
    /// individually assigned, or reordered with Up/Down buttons before building.
    ///
    /// Open via  Window → NinthAge → Texture2D Array Builder.
    /// </summary>
    public class Texture2DArrayBuilder : EditorWindow
    {
        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<Texture2D> _slices = new List<Texture2D>();
        private bool _linear = false;
        private string _outputPath = "Assets/";
        private string _outputName = "NewAlbedoArray";
        private Vector2 _scroll;
        private string _validationError = string.Empty;

        // ── Window entry point ────────────────────────────────────────────────────

        [MenuItem("Window/NinthAge/Texture2D Array Builder", priority = 200)]
        public static void Open() => GetWindow<Texture2DArrayBuilder>("Texture2D Array Builder");

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            // ── Output settings ───────────────────────────────────────────────
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            _outputName = EditorGUILayout.TextField(
                new GUIContent("Name", "Asset filename (no extension)."), _outputName);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(
                    new GUIContent("Folder", "Folder inside Assets/ where the array will be saved."));
                EditorGUILayout.LabelField(_outputPath, EditorStyles.miniLabel);
                if (GUILayout.Button("Browse\u2026", GUILayout.Width(70)))
                {
                    string abs = EditorUtility.OpenFolderPanel("Save Texture2DArray in\u2026",
                        Path.GetFullPath(Path.Combine(Application.dataPath, "..", _outputPath)), "");
                    if (!string.IsNullOrEmpty(abs))
                    {
                        string rel = "Assets" + abs.Replace(Application.dataPath, string.Empty).Replace('\\', '/');
                        _outputPath = rel.TrimEnd('/') + "/";
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            bool newLinear = EditorGUILayout.Toggle(
                new GUIContent("Linear", "Enable for normal maps. Disable for albedo/diffuse (sRGB)."),
                _linear);
            if (EditorGUI.EndChangeCheck())
            {
                _linear = newLinear;
                _outputName = _linear ? "NewNormalArray" : "NewAlbedoArray";
            }

            EditorGUILayout.Space(6);

            // ── Slice list ────────────────────────────────────────────────────
            EditorGUILayout.LabelField($"Slices  ({_slices.Count})", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            for (int i = 0; i < _slices.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(24));

                    EditorGUI.BeginChangeCheck();
                    Texture2D next = (Texture2D)EditorGUILayout.ObjectField(
                        _slices[i], typeof(Texture2D), allowSceneObjects: false,
                        GUILayout.Height(48), GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                        _slices[i] = next;

                    using (new EditorGUI.DisabledGroupScope(i == 0))
                        if (GUILayout.Button("\u25b2", GUILayout.Width(22), GUILayout.Height(48)))
                            (_slices[i], _slices[i - 1]) = (_slices[i - 1], _slices[i]);

                    using (new EditorGUI.DisabledGroupScope(i == _slices.Count - 1))
                        if (GUILayout.Button("\u25bc", GUILayout.Width(22), GUILayout.Height(48)))
                            (_slices[i], _slices[i + 1]) = (_slices[i + 1], _slices[i]);

                    if (GUILayout.Button("\u2715", GUILayout.Width(22), GUILayout.Height(48)))
                    {
                        _slices.RemoveAt(i);
                        break;
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            // ── Add buttons ───────────────────────────────────────────────────
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Slot"))
                    _slices.Add(null);

                Texture2D[] sel = Selection.objects.OfType<Texture2D>().ToArray();
                using (new EditorGUI.DisabledGroupScope(sel.Length == 0))
                {
                    string addLabel = sel.Length > 0
                        ? $"Add from Selection  ({sel.Length})"
                        : "Add from Selection";
                    if (GUILayout.Button(addLabel))
                        foreach (Texture2D t in sel)
                            _slices.Add(t);
                }

                if (GUILayout.Button("Clear All"))
                    _slices.Clear();
            }

            EditorGUILayout.Space(6);

            // ── Validation + Build ────────────────────────────────────────────
            RefreshValidation();
            if (!string.IsNullOrEmpty(_validationError))
                EditorGUILayout.HelpBox(_validationError, MessageType.Error);

            using (new EditorGUI.DisabledGroupScope(!string.IsNullOrEmpty(_validationError)))
            {
                if (GUILayout.Button("Build Texture2D Array", GUILayout.Height(32)))
                    DoBuild();
            }

            EditorGUILayout.Space(4);
        }

        // ── Validation ────────────────────────────────────────────────────────────

        private void RefreshValidation()
        {
            Texture2D[] nonNull = _slices.Where(t => t != null).ToArray();

            if (nonNull.Length == 0)
            {
                _validationError = "Add at least one texture slice.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_outputName))
            {
                _validationError = "Enter an output asset name.";
                return;
            }

            int w = nonNull[0].width;
            int h = nonNull[0].height;
            foreach (Texture2D t in nonNull.Skip(1))
            {
                if (t.width != w || t.height != h)
                {
                    _validationError =
                        $"All textures must be the same size. " +
                        $"Expected {w}\u00d7{h}, but \u2018{t.name}\u2019 is {t.width}\u00d7{t.height}.";
                    return;
                }
            }

            _validationError = string.Empty;
        }

        // ── Builder ───────────────────────────────────────────────────────────────

        private void DoBuild()
        {
            Texture2D[] sources = _slices.ToArray();
            int w = sources.First(t => t != null).width;
            int h = sources.First(t => t != null).height;

            string savePath = _outputPath.TrimEnd('/') + "/" + _outputName + ".asset";

            // Blit each source through a RenderTexture so compressed formats
            // (DXT/BC3) are GPU-decompressed — no Read/Write flag required.
            //
            // Color space: in linear rendering (URP), sampling an sRGB texture
            // converts it to linear. By creating the RT as sRGB, the hardware
            // applies a linear→sRGB conversion on write, which round-trips the
            // values back to their original encoded form. For linear data
            // (normal maps) both RT and readback stay in linear.
            RenderTextureReadWrite rtReadWrite = _linear
                ? RenderTextureReadWrite.Linear
                : RenderTextureReadWrite.sRGB;
            RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, rtReadWrite);
            Texture2D readback = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false, linear: _linear);
            Texture2DArray array = new Texture2DArray(w, h, sources.Length,
                TextureFormat.RGBA32, mipChain: true, linear: _linear);
            array.filterMode = FilterMode.Trilinear; // interpolates between mip levels — eliminates mip-snap seams
            array.anisoLevel = 8;                    // sharp at oblique angles
            array.wrapMode = TextureWrapMode.Repeat;

            rt.Create();
            RenderTexture prev = RenderTexture.active;

            // Normal-map slices need a decode step: Unity compresses normal maps
            // to a platform-specific format (BC5 on PC: X in R, Y in G; DXT5nm on
            // mobile: X in A, Y in G). The default blit stores raw encoded bytes,
            // which when re-imported as NormalMap would double-process. Instead we
            // blit through TerrainNormalDecoder which calls UnpackNormal and outputs
            // XYZ in [0, 1], ready for a clean NormalMap reimport.
            const string normalDecoderShaderPath = "Assets/Shaders/TerrainNormalDecoder.shader";
            Material normalDecodeMat = null;
            if (_linear)
            {
                Shader dec = AssetDatabase.LoadAssetAtPath<Shader>(normalDecoderShaderPath);
                if (dec != null)
                    normalDecodeMat = new Material(dec);
                else
                    Debug.LogWarning($"[Texture2DArrayBuilder] Normal decoder shader not found at '{normalDecoderShaderPath}'. "
                        + "Normal maps may be encoded incorrectly in the array.");
            }

            try
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Building Texture2D Array",
                        sources[i] != null
                            ? $"Slice {i}: \u2018{sources[i].name}\u2019  ({i + 1}/{sources.Length})"
                            : $"Slice {i}: (empty)  ({i + 1}/{sources.Length})",
                        (float)i / sources.Length);

                    Texture2D src = sources[i] != null ? sources[i] : Texture2D.blackTexture;
                    if (normalDecodeMat != null)
                        Graphics.Blit(src, rt, normalDecodeMat);
                    else
                        Graphics.Blit(src, rt);
                    RenderTexture.active = rt;
                    readback.ReadPixels(new Rect(0, 0, w, h), 0, 0, recalculateMipMaps: false);
                    readback.Apply(updateMipmaps: false);
                    array.SetPixels(readback.GetPixels(), i);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RenderTexture.active = prev;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(readback);
                if (normalDecodeMat != null) Object.DestroyImmediate(normalDecodeMat);
            }

            array.Apply(updateMipmaps: true);

            AssetDatabase.CreateAsset(array, savePath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(array);

            Debug.Log($"[Texture2DArrayBuilder] Created {sources.Length}-slice " +
                      $"{w}\u00d7{h} {(_linear ? "Linear" : "sRGB")} array \u2192 {savePath}");
        }
    }
}
