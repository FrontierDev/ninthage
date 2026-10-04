using UnityEngine;

namespace Game.Shared
{
    public static class CombatRatingHelper
    {
        private const int ReferenceLevel = 60;
        private const float ScalingExponent = 1.5f;

        /// <summary>
        /// Converts a flat rating value to a percentage, scaled by character level.
        /// Higher levels require more rating for the same percentage.
        /// </summary>
        /// <param name="rating">The raw rating value.</param>
        /// <param name="level">The character's level.</param>
        /// <param name="baseRatingPerPercent">How much rating equals 1% at the reference level.</param>
        public static float RatingToPercent(float rating, int level, float baseRatingPerPercent)
        {
            float ratingPerPercent = Mathf.Max(1f, baseRatingPerPercent
                * Mathf.Pow((float)level / ReferenceLevel, ScalingExponent));
            return rating / ratingPerPercent;
        }
    }
}