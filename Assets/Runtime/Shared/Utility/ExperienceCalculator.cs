using System;
using System.Collections.Generic;

namespace Game.Shared.Utility
{
    /// <summary>
    /// Stores per-level XP requirements for a WoW-like levelling curve.
    /// The table is generated once when the class is first accessed.
    ///
    /// LevelXpRequirements[level] = XP required to go from that level to the next.
    /// Example: LevelXpRequirements[10] = XP required for level 10 -> 11.
    /// </summary>
    public static class ExperienceCalculator
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 60;

        /// <summary>
        /// XP required to go from level N to level N+1.
        /// </summary>
        public static readonly Dictionary<int, int> LevelXpRequirements;

        static ExperienceCalculator()
        {
            LevelXpRequirements = new Dictionary<int, int>(MaxLevel);
            Initialize();
        }

        private static void Initialize()
        {
            for (int level = MinLevel; level < MaxLevel; level++)
            {
                LevelXpRequirements[level] = CalculateRequiredExp(level);
            }

            // At level cap, no further XP is required.
            LevelXpRequirements[MaxLevel] = 0;
        }

        /// <summary>
        /// Returns the XP needed to go from the given level to the next level.
        /// Example: level 1 returns the XP needed for 1 -> 2.
        /// </summary>
        private static int CalculateRequiredExp(int level)
        {
            if (level < MinLevel)
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be >= {MinLevel}.");

            if (level >= MaxLevel)
                return 0;

            float t = (level - MinLevel) / (float)(MaxLevel - MinLevel);

            float xp =
                400f +
                (level * 120f) +
                (t * t * 2200f) +
                (t * t * t * 4800f);

            return (int)MathF.Round(xp);
        }

        public static int GetExpForLevel(int level)
        {
            if (!LevelXpRequirements.TryGetValue(level, out int xp))
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be between {MinLevel} and {MaxLevel}.");

            return xp;
        }

        public static float GetLevelProgress01(int currentLevel, int currentExp)
        {
            if (currentLevel < MinLevel || currentLevel > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(currentLevel), $"Current level must be between {MinLevel} and {MaxLevel}.");

            if (currentLevel == MaxLevel)
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