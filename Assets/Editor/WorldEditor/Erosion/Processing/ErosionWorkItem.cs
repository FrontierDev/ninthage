namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Immutable work unit for erosion processing.
    /// Contains all data needed to apply erosion to a heightmap.
    /// </summary>
    public struct ErosionWorkItem
    {
        /// <summary>
        /// The heightmap to erode (normalized [0, 1]).
        /// </summary>
        public float[] heightmap;

        /// <summary>
        /// Width of the heightmap in pixels.
        /// </summary>
        public int width;

        /// <summary>
        /// Height of the heightmap in pixels.
        /// </summary>
        public int height;

        /// <summary>
        /// The erosion mode to apply.
        /// </summary>
        public IErosionMode mode;

        /// <summary>
        /// Erosion configuration parameters.
        /// </summary>
        public ErosionSettings settings;

        /// <summary>
        /// Optional work item identifier for tracking.
        /// </summary>
        public int id;

        /// <summary>
        /// Creates a new erosion work item.
        /// </summary>
        public ErosionWorkItem(float[] heightmap, int width, int height, IErosionMode mode, ErosionSettings settings, int id = 0)
        {
            this.heightmap = heightmap;
            this.width = width;
            this.height = height;
            this.mode = mode;
            this.settings = settings;
            this.id = id;
        }

        /// <summary>
        /// Validates work item integrity.
        /// </summary>
        public bool IsValid()
        {
            return heightmap != null
                && height > 0
                && width > 0
                && heightmap.Length == width * height
                && mode != null;
        }
    }
}
