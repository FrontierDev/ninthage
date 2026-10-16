using Game.Shared;
using Game.Shared.Runtime.Actors;
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
    }
}
