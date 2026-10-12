using Game.Shared;
using Game.Shared.Movement;
using NUnit.Framework;
using UnityEngine;

namespace Game.Shared.Tests.Shared
{
    public sealed class MovementSimulationTests
    {
        [Test]
        public void ForwardInputAtZeroYawMovesAlongPositiveZ()
        {
            var delta = MovementSimulation.ComputeMoveDelta(
                new Vector2(0f, 1f), 0f, 5f, 0f, 0.2f, true, out var horizontalVelocity);

            Assert.That(delta, Is.EqualTo(new Vector3(0f, 0f, 1f)));
            Assert.That(horizontalVelocity, Is.EqualTo(new Vector3(0f, 0f, 5f)));
        }

        [Test]
        public void RightInputAtZeroYawMovesAlongPositiveX()
        {
            var delta = MovementSimulation.ComputeMoveDelta(
                new Vector2(1f, 0f), 0f, 5f, 0f, 0.2f, true, out var horizontalVelocity);

            Assert.That(delta, Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Assert.That(horizontalVelocity, Is.EqualTo(new Vector3(5f, 0f, 0f)));
        }

        [Test]
        public void DiagonalInputIsNormalized()
        {
            var delta = MovementSimulation.ComputeMoveDelta(
                Vector2.one, 0f, 10f, 0f, 1f, true, out var horizontalVelocity);

            Assert.That(horizontalVelocity.magnitude, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(delta.x, Is.EqualTo(delta.z).Within(0.0001f));
        }

        [Test]
        public void GroundedMovementEmitsHorizontalVelocityAndAirborneMovementDoesNot()
        {
            MovementSimulation.ComputeMoveDelta(
                new Vector2(1f, 0f), 0f, 5f, 0f, 0.2f, true, out var groundedVelocity);
            MovementSimulation.ComputeMoveDelta(
                new Vector2(1f, 0f), 0f, 5f, 0f, 0.2f, false, out var airborneVelocity);

            Assert.That(groundedVelocity, Is.EqualTo(new Vector3(5f, 0f, 0f)));
            Assert.That(airborneVelocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void GravityAppliesOnlyWhileAirborne()
        {
            var groundedVelocity = MovementSimulation.ApplyGravity(-2f, -10f, 50f, 0.5f, true);
            var airborneVelocity = MovementSimulation.ApplyGravity(-2f, -10f, 50f, 0.5f, false);

            Assert.That(groundedVelocity, Is.EqualTo(-2f));
            Assert.That(airborneVelocity, Is.EqualTo(-7f));
        }

        [Test]
        public void FallSpeedIsClampedToMaximum()
        {
            var velocity = MovementSimulation.ApplyGravity(-10f, -10f, 15f, 1f, false);

            Assert.That(velocity, Is.EqualTo(-15f));
        }
    }
}
