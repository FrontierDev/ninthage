using UnityEngine;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Serializable settings for the World Editor window.
    /// </summary>
    [System.Serializable]
    public class WorldEditorSettings
    {
        // World parameters
        public int worldWidth = 256;
        public bool flipCoastlineMaskY = false;
        public int seaLevel = 10;
        public float waterPlaneOffset = -2f;
        public float mountainLevel = 0.7f;
        public float maxHeight = 100f;

        // Plateau mode
        public bool showPlateaus = false;
        public int plateauCount = 5;
        public bool enablePlateauGeneration = false;
        public float plateauSharpness = 2.0f;

        // View and interaction
        public bool showPreview = true;
        public float zoomLevel = 1f;
        public float panOffsetX = 0f;
        public float panOffsetY = 0f;
        public int selectedSidebarTab = 0;
        public int visualizationMode = 0; // VisualizationMode.Heightmap

        // Erosion settings
        public bool enableErosion = false;
        public bool[] erosionModeEnabled = new bool[0];

        // Selective generation
        public bool visualizeGeneratedChunks = true;
        public bool visualizeMarkedChunks = true;
        public bool visualizeUnmarkedChunks = true;
        public bool selectiveGenerationMode = false;
    }

    /// <summary>
    /// Wrapper for JSON serialization of World Editor settings.
    /// </summary>
    [System.Serializable]
    public class WorldEditorSettingsData
    {
        public WorldEditorSettings settings = new WorldEditorSettings();
    }
}
