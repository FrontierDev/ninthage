using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor
{
    public partial class WorldEditorWindow : EditorWindow
    {
        private void GenerateErosionPreview()
        {
            if (_heightmapTexture == null)
                return;

            List<(IErosionMode, ErosionSettings)> selectedModes = new List<(IErosionMode, ErosionSettings)>();
            for (int i = 0; i < _availableErosionModes.Count; i++)
            {
                if (_erosionModeEnabled[i])
                {
                    IErosionMode mode = _availableErosionModes[i];
                    ErosionSettings settings = _erosionModeSettings[mode.ModeName];
                    settings.seaLevel = _seaLevel;
                    selectedModes.Add((mode, settings));
                }
            }

            if (selectedModes.Count == 0)
                return;

            try
            {
                Color[] pixels = _heightmapTexture.GetPixels();
                float[] heightmap = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    heightmap[i] = pixels[i].grayscale;
                }

                float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
                int worldDepth = Mathf.RoundToInt(_worldWidth * aspectRatio);

                _iterativeTextureWidth = _heightmapTexture.width;
                _iterativeTextureHeight = _heightmapTexture.height;
                _iterativePreviewWorldWidth = _worldWidth;
                _iterativePreviewWorldHeight = worldDepth;

                _iterativeErosionModes = selectedModes;
                _iterativeHeightmap = heightmap;
                _iterativeModeIndex = 0;
                _iterativeIteration = 0;
                _isGeneratingPreview = true;

                int previewWidth = _iterativeTextureWidth;
                int previewHeight = _iterativeTextureHeight;

                if (_previewHeightmapTexture != null)
                {
                    Object.DestroyImmediate(_previewHeightmapTexture);
                }
                _previewHeightmapTexture = new Texture2D(previewWidth, previewHeight, TextureFormat.RGB24, false);

                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Erosion preview failed: {ex.Message}", "OK");
                Debug.LogException(ex);
                _isGeneratingPreview = false;
            }
        }

        private void UpdateIterativeErosion()
        {
            if (_iterativeHeightmap == null || _iterativeErosionModes == null || _iterativeModeIndex >= _iterativeErosionModes.Count)
            {
                _isGeneratingPreview = false;
                return;
            }

            var (currentMode, settings) = _iterativeErosionModes[_iterativeModeIndex];

            if (_iterativeIteration < settings.iterations)
            {
                string modeName = currentMode.ModeName;
                _iterativeProgressText = $"({modeName} {_iterativeIteration + 1}/{settings.iterations})";

                _iterativeHeightmap = currentMode.Erode(
                    _iterativeHeightmap,
                    _iterativeTextureWidth,
                    _iterativeTextureHeight,
                    new ErosionSettings
                    {
                        intensity = settings.intensity,
                        iterations = 1,
                        strength = settings.strength,
                        radius = settings.radius,
                        randomSeed = settings.randomSeed + _iterativeIteration,
                        seaLevel = settings.seaLevel
                    });

                _iterativeIteration++;

                if (_iterativeIteration >= settings.iterations)
                {
                    _iterativeModeIndex++;
                    _iterativeIteration = 0;
                }

                UpdatePreviewTexture();
                _previewHeightmap = _iterativeHeightmap;
            }
            else
            {
                _isGeneratingPreview = false;
            }
        }

        private void UpdatePreviewTexture()
        {
            int previewWidth = _previewHeightmapTexture.width;
            int previewHeight = _previewHeightmapTexture.height;
            Color[] previewPixels = new Color[previewWidth * previewHeight];

            for (int py = 0; py < previewHeight; py++)
            {
                for (int px = 0; px < previewWidth; px++)
                {
                    int hmx = Mathf.Clamp(px, 0, _iterativeTextureWidth - 1);
                    int hmz = Mathf.Clamp(py, 0, _iterativeTextureHeight - 1);
                    int hmIdx = hmz * _iterativeTextureWidth + hmx;

                    float height = Mathf.Clamp01(_iterativeHeightmap[hmIdx]);
                    int pIdx = py * previewWidth + px;
                    previewPixels[pIdx] = new Color(height, height, height, 1f);
                }
            }

            _previewHeightmapTexture.SetPixels(previewPixels);
            _previewHeightmapTexture.Apply();
        }

        private void ResetErosionPreview()
        {
            _isGeneratingPreview = false;
            _iterativeHeightmap = null;
            _iterativeErosionModes = null;
            _iterativeModeIndex = 0;
            _iterativeIteration = 0;

            if (_previewHeightmapTexture != null)
            {
                Object.DestroyImmediate(_previewHeightmapTexture);
                _previewHeightmapTexture = null;
            }
            _previewHeightmap = null;
            Repaint();
        }


    }
}
