using System;
using System.Collections.Generic;

namespace Game.Shared.Utility
{
    /// <summary>
    /// Stores per-level XP requirements for a WoW-like levelling curve.
    /// The table is generated once when the class is first accessed.
    ///
    /// LEVEL_XP_REQUIREMENTS[level] = XP required to go from that level to the next.
    /// Example: LEVEL_XP_REQUIREMENTS[10] = XP required for level 10 -> 11.
    /// </summary>
    public static class ExperienceCalculator
    {
        public const int MIN_LEVEL = 1;
        public const int MAX_LEVEL = 60;

        /// <summary>
        /// XP required to go from level N to level N+1.
        /// </summary>
        public static readonly Dictionary<int, int> LEVEL_XP_REQUIREMENTS;

        static ExperienceCalculator()
        {
            LEVEL_XP_REQUIREMENTS = new(MAX_LEVEL);
            Initialize();
        }

        private static void Initialize()
        {
            for (int level = MIN_LEVEL; level < MAX_LEVEL; level++)
            {
                LEVEL_XP_REQUIREMENTS[level] = CalculateRequiredExp(level);
            }

            // At level cap, no further XP is required.
            LEVEL_XP_REQUIREMENTS[MAX_LEVEL] = 0;
        }

        /// <summary>
        /// Returns the XP needed to go from the given level to the next level.
        /// Example: level 1 returns the XP needed for 1 -> 2.
        /// </summary>
        private static int CalculateRequiredExp(int level)
        {
            if (level < MIN_LEVEL)
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be >= {MIN_LEVEL}.");

            if (level >= MAX_LEVEL)
                return 0;

            float t = (level - MIN_LEVEL) / (float)(MAX_LEVEL - MIN_LEVEL);

            float xp =
                400f +
                (level * 120f) +
                (t * t * 2200f) +
                (t * t * t * 4800f);

            return (int)MathF.Round(xp);
        }

        /// <summary>Gets the experience required to advance from a level.</summary>
        public static int GetExpForLevel(int level)
        {
            if (!LEVEL_XP_REQUIREMENTS.TryGetValue(level, out int xp))
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be between {MIN_LEVEL} and {MAX_LEVEL}.");

            return xp;
        }

        /// <summary>Gets current level progress as a value from zero to one.</summary>
        public static float GetLevelProgress01(int currentLevel, int currentExp)
        {
            if (currentLevel < MIN_LEVEL || currentLevel > MAX_LEVEL)
                throw new ArgumentOutOfRangeException(nameof(currentLevel), $"Current level must be between {MIN_LEVEL} and {MAX_LEVEL}.");

            if (currentLevel == MAX_LEVEL)
                return 1f;

            int expRequired = GetExpForLevel(currentLevel);

            if (expRequired <= 0)
                return 1f;

            float progress = currentExp / (float)expRequired;

            if (progress < 0f)
                return 0f;

            if (progress > 1f)
                return 1f;

            return progress;
        }
    }
}
