using Game.Shared;
using Game.Shared.Utility;
using NUnit.Framework;
using UnityEngine;

namespace Game.Shared.Tests.Shared
{
    public sealed class CombatRatingHelperTests
    {
        [Test]
        public void RatingConversionIsDeterministic()
        {
            var first = CombatRatingHelper.RatingToPercent(250f, 30, 12f);
            var second = CombatRatingHelper.RatingToPercent(250f, 30, 12f);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void ZeroRatingReturnsZeroPercent()
        {
            Assert.That(CombatRatingHelper.RatingToPercent(0f, 30, 12f), Is.EqualTo(0f));
        }

        [Test]
        public void FixedRatingProducesLessPercentAtHigherLevel()
        {
            var lowerLevelPercent = CombatRatingHelper.RatingToPercent(100f, 10, 10f);
            var higherLevelPercent = CombatRatingHelper.RatingToPercent(100f, 60, 10f);

            Assert.That(higherLevelPercent, Is.LessThan(lowerLevelPercent));
        }

        [Test]
        public void BaseRatingPerPercentIsClampedToOne()
        {
            var percent = CombatRatingHelper.RatingToPercent(4f, 60, 0f);

            Assert.That(percent, Is.EqualTo(4f).Within(0.0001f));
        }
    }
}
