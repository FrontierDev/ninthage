namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Configuration parameters for erosion algorithms.
    /// Serializable for persistence in editor UI.
    /// </summary>
    public struct ErosionSettings
    {
        /// <summary>
        /// Overall erosion strength [0, 1]. Lower = subtle, 1 = aggressive.
        /// </summary>
        public float intensity;

        /// <summary>
        /// Number of erosion simulation iterations. More = stronger effect.
        /// </summary>
        public int iterations;

        /// <summary>
        /// Per-iteration erosion strength multiplier. Mode-specific interpretation.
        /// </summary>
        public float strength;

        /// <summary>
        /// Erosion radius/kernel size in pixels. Mode-specific interpretation.
        /// </summary>
        public float radius;

        /// <summary>
        /// Random seed for deterministic erosion with pseudo-randomness.
        /// Same seed + heightmap + mode + other params = identical output.
        /// </summary>
        public int randomSeed;

        /// <summary>
        /// Sea level threshold [0, 1]. Below this height, only wave erosion applies.
        /// </summary>
        public float seaLevel;

        /// <summary>
        /// Total world width in world units. Used to calculate proper cell size for heightmap scaling.
        /// </summary>
        public float worldWidth;

        /// <summary>
        /// Total world height in world units. Used to calculate proper cell size for heightmap scaling.
        /// </summary>
        public float worldHeight;

        /// <summary>
        /// Creates default erosion settings with reasonable values.
        /// </summary>
        public static ErosionSettings Default()
        {
            return new ErosionSettings
            {
                intensity = 0.5f,
                iterations = 10,
                strength = 0.1f,
                radius = 1f,
                randomSeed = 12345,
                seaLevel = 0.3f,
                worldWidth = 256f,  // Default single chunk
                worldHeight = 256f   // Default single chunk
            };
        }
    }
}
