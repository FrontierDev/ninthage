using Game.Shared;
using Game.Shared.Utility;
using NUnit.Framework;
using UnityEngine;

namespace Game.Shared.Tests.Shared
{
    public sealed class WorldPositionTests
    {
        [SetUp]
        public void SetUp()
        {
            GameConfigurationManager.Initialize();
            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
        }

        [Test]
        public void FromChunkLocalUsesConfiguredChunkSize()
        {
            var chunkSize = GameConfigurationManager.Config.ChunkSize;
            var chunk = new Vector2Int(2, -3);
            var local = new Vector3(12.5f, 7.25f, 8.75f);

            var position = WorldPosition.FromChunkLocal(chunk, local);

            Assert.That(position.x, Is.EqualTo((double)chunk.x * chunkSize + local.x).Within(0.000000001d));
            Assert.That(position.y, Is.EqualTo(local.y).Within(0.000000001d));
            Assert.That(position.z, Is.EqualTo((double)chunk.y * chunkSize + local.z).Within(0.000000001d));
        }

        [Test]
        public void PositiveWorldPositionReturnsExpectedChunkAndLocalPosition()
        {
            var chunkSize = GameConfigurationManager.Config.ChunkSize;
            var position = new WorldPosition(chunkSize * 2.5d, 3.5d, chunkSize * 0.75d);

            Assert.That(position.Chunk, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(position.LocalPosition.x, Is.EqualTo(chunkSize * 0.5f).Within(0.0001f));
            Assert.That(position.LocalPosition.y, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(position.LocalPosition.z, Is.EqualTo(chunkSize * 0.75f).Within(0.0001f));
        }

        [Test]
        public void NegativeWorldPositionUsesFloorChunkSemantics()
        {
            var chunkSize = GameConfigurationManager.Config.ChunkSize;
            var position = new WorldPosition(-0.1d, 5d, -chunkSize - 0.1d);

            Assert.That(position.Chunk, Is.EqualTo(new Vector2Int(-1, -2)));
            Assert.That(position.LocalPosition.x, Is.EqualTo(chunkSize - 0.1f).Within(0.0001f));
            Assert.That(position.LocalPosition.y, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(position.LocalPosition.z, Is.EqualTo(chunkSize - 0.1f).Within(0.0001f));
        }

        [Test]
        public void ExactChunkBoundariesResolveToExpectedChunks()
        {
            var chunkSize = GameConfigurationManager.Config.ChunkSize;

            var positiveBoundary = new WorldPosition(chunkSize, 4d, -chunkSize);
            var negativeBoundary = new WorldPosition(-chunkSize, 4d, -chunkSize);

            Assert.That(positiveBoundary.Chunk, Is.EqualTo(new Vector2Int(1, -1)));
            Assert.That(positiveBoundary.LocalPosition, Is.EqualTo(new Vector3(0f, 4f, 0f)));
            Assert.That(negativeBoundary.Chunk, Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(negativeBoundary.LocalPosition, Is.EqualTo(new Vector3(0f, 4f, 0f)));
        }

        [Test]
        public void ToLocalReturnsPositionRelativeToOrigin()
        {
            var position = new WorldPosition(250.5d, 7.25d, -110.75d);
            var origin = new WorldPosition(200d, 2d, -100.5d);

            var local = position.ToLocal(origin);

            Assert.That(local.x, Is.EqualTo(50.5f).Within(0.0001f));
            Assert.That(local.y, Is.EqualTo(5.25f).Within(0.0001f));
            Assert.That(local.z, Is.EqualTo(-10.25f).Within(0.0001f));
        }

        [Test]
        public void AdditionAndSubtractionPreserveDoublePrecisionCoordinates()
        {
            var first = new WorldPosition(1_000_000_000.125d, -2.5d, -2_000_000_000.875d);
            var second = new WorldPosition(12_345.75d, 6.25d, -9_876.125d);

            var sum = first + second;
            var difference = sum - second;

            Assert.That(sum.x, Is.EqualTo(1_000_012_345.875d).Within(0.000000001d));
            Assert.That(sum.y, Is.EqualTo(3.75d).Within(0.000000001d));
            Assert.That(sum.z, Is.EqualTo(-2_000_009_877d).Within(0.000000001d));
            Assert.That(difference.x, Is.EqualTo(first.x).Within(0.000000001d));
            Assert.That(difference.y, Is.EqualTo(first.y).Within(0.000000001d));
            Assert.That(difference.z, Is.EqualTo(first.z).Within(0.000000001d));
        }
    }
}
