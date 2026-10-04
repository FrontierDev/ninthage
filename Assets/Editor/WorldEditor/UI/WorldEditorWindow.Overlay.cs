using UnityEditor;
using UnityEngine;

namespace Game.Editor.WorldEditor
{
    public partial class WorldEditorWindow : EditorWindow
    {
        private void DrawGridOverlay(Rect displayRect)
        {
            if (_heightmapTexture == null)
                return;

            float aspectRatio = (float)_heightmapTexture.height / _heightmapTexture.width;
            int chunksX = _worldWidth / 256;
            int chunksZ = Mathf.RoundToInt(_worldWidth * aspectRatio) / 256;

            // Initialize chunk tracking on first run
            if (_chunkMarkedForGeneration == null || _chunkMarkedForGeneration.GetLength(0) != chunksX || _chunkMarkedForGeneration.GetLength(1) != chunksZ)
            {
                _chunkMarkedForGeneration = new bool[chunksX, chunksZ];
                _chunkGenerated = new bool[chunksX, chunksZ];
            }

            float chunkWidthPixels = displayRect.width / chunksX;
            float chunkHeightPixels = displayRect.height / chunksZ;

            // Draw chunk visualization if in selective mode
            if (_selectiveGenerationMode)
            {
                int totalChunks = chunksX * chunksZ;

                // Performance optimization: Skip chunk visualization only if chunks are extremely tiny
                bool chunksVisible = chunkWidthPixels >= 0.5f && chunkHeightPixels >= 0.5f;

                // Adaptive detail for selection mode performance
                int drawStep = 1;
                if (totalChunks > 1000) drawStep = 2;      // Skip every other chunk for high count
                if (totalChunks > 2500) drawStep = 4;      // Skip 3 out of 4 chunks for very high count

                if (chunksVisible)
                {
                    for (int z = 0; z < chunksZ; z += drawStep)
                    {
                        for (int x = 0; x < chunksX; x += drawStep)
                        {
                            // Invert Z so that visual (0,0) at bottom-left matches terrain generation coordinates
                            int visualZ = chunksZ - 1 - z;
                            float xPos = displayRect.x + (x * chunkWidthPixels);
                            float zPos = displayRect.y + (visualZ * chunkHeightPixels);
                            Rect chunkRect = new Rect(xPos, zPos, chunkWidthPixels * drawStep, chunkHeightPixels * drawStep);

                            // Determine chunk state color with higher opacity for visibility
                            Color chunkColor = Color.clear;
                            bool drawChunk = false;
                            bool hasImportantState = false;

                            if (_chunkGenerated[x, z] && _visualizeGeneratedChunks)
                            {
                                chunkColor = new Color(0f, 1f, 0f, 0.35f); // Generated (green) - more opaque
                                drawChunk = true;
                                hasImportantState = true;
                            }
                            else if (_chunkMarkedForGeneration[x, z] && _visualizeMarkedChunks)
                            {
                                chunkColor = new Color(1f, 1f, 0f, 0.35f); // Marked for generation (yellow) - more opaque
                                drawChunk = true;
                                hasImportantState = true;
                            }
                            else if (!_chunkMarkedForGeneration[x, z] && !_chunkGenerated[x, z] && _visualizeUnmarkedChunks)
                            {
                                chunkColor = new Color(0.5f, 0.5f, 0.5f, 0.15f); // Not marked (grey) - subtle
                                drawChunk = true;
                            }

                            // Draw chunk background
                            if (drawChunk)
                                EditorGUI.DrawRect(chunkRect, chunkColor);

                            // Draw different types of borders based on chunk size
                            if (hasImportantState)
                            {
                                Color borderColor = _chunkGenerated[x, z] ? new Color(0f, 1f, 0f, 0.8f) : new Color(1f, 1f, 0f, 0.8f);
                                Handles.color = borderColor;

                                // For very small chunks, draw as dots or simple rectangles
                                if (chunkWidthPixels < 2f || chunkHeightPixels < 2f)
                                {
                                    // Draw as a single dot for tiny chunks
                                    float xCenter = chunkRect.center.x;
                                    float zCenter = chunkRect.center.y;
                                    EditorGUI.DrawRect(new Rect(xCenter - 0.5f, zCenter - 0.5f, 1f, 1f), borderColor);
                                }
                                else if (chunkWidthPixels < 8f || chunkHeightPixels < 8f)
                                {
                                    // Draw as filled rectangle for small chunks
                                    EditorGUI.DrawRect(chunkRect, new Color(borderColor.r, borderColor.g, borderColor.b, 0.5f));
                                }
                                else
                                {
                                    // Draw border as wireframe for larger chunks
                                    Vector3[] borderPoints = new Vector3[5] {
                                    new Vector3(xPos, zPos, 0f),
                                    new Vector3(xPos + chunkRect.width, zPos, 0f),
                                    new Vector3(xPos + chunkRect.width, zPos + chunkRect.height, 0f),
                                    new Vector3(xPos, zPos + chunkRect.height, 0f),
                                    new Vector3(xPos, zPos, 0f)
                                };
                                    Handles.DrawPolyLine(borderPoints);
                                }
                            }
                        }
                    }
                }
            }
        }


        private void CreateDisplayTextureWithWorldAspect()
        {
            if (_heightmapTexture == null)
                return;

            float worldAspectRatio = (float)_iterativePreviewWorldHeight / _iterativePreviewWorldWidth;

            int displayWidth = Mathf.Min(1024, _iterativePreviewWorldWidth);
            int displayHeight = Mathf.Max(1, (int)(displayWidth * worldAspectRatio));

            if (_previewHeightmapTexture != null)
                Object.DestroyImmediate(_previewHeightmapTexture);

            _previewHeightmapTexture = new Texture2D(displayWidth, displayHeight, TextureFormat.RGB24, false);

            Color[] displayPixels = new Color[displayWidth * displayHeight];
            for (int y = 0; y < displayHeight; y++)
            {
                for (int x = 0; x < displayWidth; x++)
                {
                    float u = x / (float)displayWidth;
                    float v = y / (float)displayHeight;

                    int sourceX = (int)(u * _heightmapTexture.width);
                    int sourceY = (int)(v * _heightmapTexture.height);
                    sourceX = Mathf.Clamp(sourceX, 0, _heightmapTexture.width - 1);
                    sourceY = Mathf.Clamp(sourceY, 0, _heightmapTexture.height - 1);

                    Color sample = _heightmapTexture.GetPixel(sourceX, sourceY);
                    displayPixels[y * displayWidth + x] = sample;
                }
            }

            _previewHeightmapTexture.SetPixels(displayPixels);
            _previewHeightmapTexture.Apply();
        }
    }
}
