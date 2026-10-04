namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Interface for erosion algorithm implementations.
    /// All erosion modes must implement this contract to be used in the erosion pipeline.
    /// </summary>
    public interface IErosionMode
    {
        /// <summary>
        /// Gets the display name of this erosion mode.
        /// </summary>
        string ModeName { get; }

        /// <summary>
        /// Gets a description of what this erosion mode does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the default settings for this erosion mode.
        /// </summary>
        ErosionSettings GetDefaultSettings();

        /// <summary>
        /// Applies erosion to the provided heightmap.
        /// Returns a new erosion-modified heightmap; input is not modified.
        /// </summary>
        /// <param name="heightmap">Normalized heightmap array [0, 1]. Not modified by this method.</param>
        /// <param name="width">Width of the heightmap in pixels.</param>
        /// <param name="height">Height of the heightmap in pixels.</param>
        /// <param name="settings">Erosion parameters.</param>
        /// <returns>New float array with erosion applied. Same dimensions as input.</returns>
        float[] Erode(float[] heightmap, int width, int height, ErosionSettings settings);
    }
}
