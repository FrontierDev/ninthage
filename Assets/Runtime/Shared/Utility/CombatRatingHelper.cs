using UnityEngine;

namespace Game.Shared.Utility
{
    /// <summary>Converts combat ratings into level-scaled percentages.</summary>
    public static class CombatRatingHelper
    {
        private const int REFERENCE_LEVEL = 60;
        private const float SCALING_EXPONENT = 1.5f;

        /// <summary>
        /// Converts a flat rating value to a percentage, scaled by character level.
        /// Higher levels require more rating for the same percentage.
        /// </summary>
        /// <param name="rating">The raw rating value.</param>
        /// <param name="level">The character's level.</param>
        /// <param name="baseRatingPerPercent">How much rating equals 1% at the reference level.</param>
        /// <summary>Converts a rating to a percentage for a character level.</summary>
        public static float RatingToPercent(float rating, int level, float baseRatingPerPercent)
        {
            float ratingPerPercent = Mathf.Max(1f, baseRatingPerPercent
                * Mathf.Pow((float)level / REFERENCE_LEVEL, SCALING_EXPONENT));
            return rating / ratingPerPercent;
        }
    }
}
