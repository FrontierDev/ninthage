using Game.Shared.Utility;
using NUnit.Framework;
using System;

namespace Game.Shared.Tests.Shared
{
    public sealed class ExperienceCalculatorTests
    {
        [Test]
        public void LevelBoundsMatchTheCurrentFoundation()
        {
            Assert.That(ExperienceCalculator.MIN_LEVEL, Is.EqualTo(1));
            Assert.That(ExperienceCalculator.MAX_LEVEL, Is.EqualTo(60));
        }

        [Test]
        public void ExperienceRequirementsArePositiveBeforeLevelCap()
        {
            for (var level = ExperienceCalculator.MIN_LEVEL; level < ExperienceCalculator.MAX_LEVEL; level++)
                Assert.That(ExperienceCalculator.GetExpForLevel(level), Is.GreaterThan(0));
        }

        [Test]
        public void LevelCapRequiresNoFurtherExperience()
        {
            Assert.That(ExperienceCalculator.GetExpForLevel(ExperienceCalculator.MAX_LEVEL), Is.EqualTo(0));
        }

        [Test]
        public void GetExpForLevelRejectsLevelsOutsideRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceCalculator.GetExpForLevel(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExperienceCalculator.GetExpForLevel(61));
        }

        [Test]
        public void LevelProgressClampsAtBothBoundsAndLevelCap()
        {
            var requirement = ExperienceCalculator.GetExpForLevel(1);

            Assert.That(ExperienceCalculator.GetLevelProgress01(1, -1), Is.EqualTo(0f));
            Assert.That(ExperienceCalculator.GetLevelProgress01(1, requirement + 1), Is.EqualTo(1f));
            Assert.That(ExperienceCalculator.GetLevelProgress01(ExperienceCalculator.MAX_LEVEL, 0), Is.EqualTo(1f));
        }
    }
}
