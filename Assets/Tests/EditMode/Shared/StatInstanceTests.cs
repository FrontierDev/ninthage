using Game.Shared;
using NUnit.Framework;

namespace Game.Shared.Tests.Shared
{
    public sealed class StatInstanceTests
    {
        [Test]
        public void RecalculateEffectiveMaxUsesCurrentFormula()
        {
            var stat = new StatInstance(100f, 20f, 0.25f, 0f, 50f);

            stat.RecalculateEffectiveMax();

            Assert.That(stat.EffectiveMaximum, Is.EqualTo(150f).Within(0.0001f));
        }

        [Test]
        public void CurrentValueIsClampedToEffectiveMaximum()
        {
            var belowMinimum = new StatInstance(0f, 0f, 0f, 100f, -10f);
            var aboveMaximum = new StatInstance(0f, 0f, 0f, 100f, 150f);

            Assert.That(belowMinimum.CurrentValue, Is.EqualTo(0f));
            Assert.That(aboveMaximum.CurrentValue, Is.EqualTo(100f));
        }

        [Test]
        public void SetterMethodsOnlyChangeTheirBackingValueBeforeRecalculation()
        {
            var stat = new StatInstance(10f, 2f, 0.5f, 18f, 7f);

            stat.SetBaseValue(20f);
            stat.SetFlatModifier(3f);
            stat.SetPercentModifier(0.25f);

            Assert.That(stat.BaseValue, Is.EqualTo(20f));
            Assert.That(stat.FlatModifier, Is.EqualTo(3f));
            Assert.That(stat.PercentModifier, Is.EqualTo(0.25f));
            Assert.That(stat.EffectiveMaximum, Is.EqualTo(18f));
            Assert.That(stat.CurrentValue, Is.EqualTo(7f));
        }
    }
}
