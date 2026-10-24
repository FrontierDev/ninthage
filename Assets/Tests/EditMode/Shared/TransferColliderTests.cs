using Game.Shared.Runtime.World;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Game.Shared.Tests.Shared
{
    public sealed class TransferColliderTests
    {
        private static readonly FieldInfo INSTANCE_SCENE_ADDRESSES_FIELD =
            typeof(TransferCollider).GetField(
                "_instanceSceneAddresses",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private GameObject _gameObject;
        private Mesh _mesh;
        private TransferCollider _transferCollider;

        [SetUp]
        public void SetUp()
        {
            _mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-1f, 0f, -1f),
                    new Vector3(1f, 0f, -1f),
                    new Vector3(-1f, 0f, 1f)
                },
                triangles = new[] { 0, 1, 2 }
            };

            _gameObject = new GameObject("TransferColliderTest");
            _gameObject.transform.SetPositionAndRotation(
                new Vector3(4f, -2f, 7f),
                Quaternion.Euler(23f, 41f, 17f));
            _gameObject.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _transferCollider = _gameObject.AddComponent<TransferCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void CrossingUsesRotatedMeshPlaneAndActualTriangleShape()
        {
            Vector3 meshNormal = Vector3.down;
            Vector3 insidePoint = new(-0.5f, 0f, -0.5f);
            Vector3 previousInside = _gameObject.transform.TransformPoint(insidePoint - meshNormal);
            Vector3 currentInside = _gameObject.transform.TransformPoint(insidePoint + meshNormal);

            bool crossedInside = _transferCollider.TryGetCrossing(
                previousInside,
                currentInside,
                out TransferCollider.CrossingDirection intoDirection,
                out float crossingFraction);

            Assert.That(crossedInside, Is.True);
            Assert.That(intoDirection, Is.EqualTo(TransferCollider.CrossingDirection.IntoInstance));
            Assert.That(crossingFraction, Is.EqualTo(0.5f).Within(1e-6f));

            bool crossedOutside = _transferCollider.TryGetCrossing(
                _gameObject.transform.TransformPoint(new Vector3(0.75f, 1f, 0.75f)),
                _gameObject.transform.TransformPoint(new Vector3(0.75f, -1f, 0.75f)),
                out _,
                out _);

            Assert.That(crossedOutside, Is.False);

            bool crossedOut = _transferCollider.TryGetCrossing(
                currentInside,
                previousInside,
                out TransferCollider.CrossingDirection outDirection,
                out _);

            Assert.That(crossedOut, Is.True);
            Assert.That(outDirection, Is.EqualTo(TransferCollider.CrossingDirection.IntoPublic));
        }

        [Test]
        public void ConfigurationRequiresUniqueNonblankSceneAddressesUsingOrdinalComparison()
        {
            SetSceneAddresses("Scenes/Dungeon", "Scenes/dungeon");
            Assert.DoesNotThrow(_transferCollider.ValidateConfiguration);

            SetSceneAddresses("Scenes/Dungeon", "Scenes/Dungeon");
            Assert.Throws<System.InvalidOperationException>(_transferCollider.ValidateConfiguration);

            SetSceneAddresses("Scenes/Dungeon", " \t");
            Assert.Throws<System.InvalidOperationException>(_transferCollider.ValidateConfiguration);

            SetSceneAddresses();
            Assert.Throws<System.InvalidOperationException>(_transferCollider.ValidateConfiguration);
        }

        [Test]
        public void CrossingRejectsNonPlanarAndFullyDegenerateMeshes()
        {
            Mesh invalidMesh = new()
            {
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward
                },
                triangles = new[] { 0, 1, 2 }
            };
            _gameObject.GetComponent<MeshFilter>().sharedMesh = invalidMesh;

            Assert.Throws<System.InvalidOperationException>(() => _transferCollider.TryGetCrossing(
                Vector3.zero,
                Vector3.one,
                out _,
                out _));

            invalidMesh.vertices = new[]
            {
                Vector3.zero,
                Vector3.right,
                Vector3.right * 2f
            };
            invalidMesh.triangles = new[] { 0, 1, 2 };

            Assert.Throws<System.InvalidOperationException>(() => _transferCollider.TryGetCrossing(
                Vector3.zero,
                Vector3.one,
                out _,
                out _));

            Object.DestroyImmediate(invalidMesh);
        }

        private void SetSceneAddresses(params string[] addresses)
        {
            INSTANCE_SCENE_ADDRESSES_FIELD.SetValue(
                _transferCollider,
                new List<string>(addresses));
        }
    }
}
