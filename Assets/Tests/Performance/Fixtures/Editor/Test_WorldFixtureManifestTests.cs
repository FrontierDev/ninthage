using Game.Tests.Performance.Fixtures;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Performance.Fixtures.Editor
{
    /// <summary>Regression coverage for deterministic world fixture metadata and asset references.</summary>
    public sealed class Test_WorldFixtureManifestTests
    {
        private const string MANIFEST_PATH = "Assets/Tests/Performance/Fixtures/Worlds/world-fixture-manifest.json";

        [Test]
        public void SameSeedAndInputsProduceEquivalentMetadata()
        {
            var options = new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            };

            var first = Test_WorldFixtureLayout.BuildManifest(options);
            var second = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = options.Seed,
                DensityTier = options.DensityTier,
                FixtureVersion = options.FixtureVersion,
            });

            Assert.That(first.ManifestHash, Is.EqualTo(second.ManifestHash));
            Assert.That(first.Scenes.Length, Is.EqualTo(second.Scenes.Length));
            for (var index = 0; index < first.Scenes.Length; index++)
            {
                Assert.That(first.Scenes[index].SceneHash, Is.EqualTo(second.Scenes[index].SceneHash));
                Assert.That(first.Scenes[index].SpawnMetadataHash, Is.EqualTo(second.Scenes[index].SpawnMetadataHash));
            }
        }

        [Test]
        public void DifferentSeedChangesProceduralMetadata()
        {
            var first = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
            });
            var second = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED + 1,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
            });

            Assert.That(first.ManifestHash, Is.Not.EqualTo(second.ManifestHash));
            Assert.That(first.Scenes[1].SpawnMetadataHash, Is.Not.EqualTo(second.Scenes[1].SpawnMetadataHash));
        }

        [Test]
        public void OriginCellsAndWorldTilesRemainSeparateContracts()
        {
            Assert.That(Test_WorldFixtureLayout.ORIGIN_CELL_SIZE_METERS, Is.EqualTo(100));
            Assert.That(Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS, Is.EqualTo(256));

            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
            });
            var eastTile = FindScene(manifest, "AdjacentTileEast");
            var boundarySpawn = eastTile.Spawns[1];

            Assert.That(boundarySpawn.WorldTileX, Is.EqualTo(1));
            Assert.That(boundarySpawn.OriginCellX, Is.EqualTo(2));
            Assert.That(boundarySpawn.OriginCellX, Is.Not.EqualTo(boundarySpawn.WorldTileX));
        }

        [Test]
        public void AdjacentTileBoundsAreContinuous()
        {
            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D0_Minimal,
            });

            Assert.That(Test_WorldFixtureLayout.ValidateCoordinateContinuity(manifest.Scenes), Is.True);
            var west = FindScene(manifest, "AdjacentTileWest");
            var east = FindScene(manifest, "AdjacentTileEast");
            Assert.That(west.BoundsMaxX, Is.EqualTo(east.BoundsMinX));
        }

        [Test]
        public void UnimplementedRuntimeInstanceCapabilityIsBlocked()
        {
            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions());
            var blocked = false;
            foreach (var variant in manifest.BlockedVariants)
            {
                if (variant.Variant == "PrivateRuntimeInstanceIsolation")
                    blocked = variant.Status == "Blocked" && variant.Reason.Contains("not implemented");
            }

            Assert.That(blocked, Is.True);
        }

        [Test]
        public void DefaultTierContainsRepeatableGeometryCollisionAndLODVariation()
        {
            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
            });
            var forest = FindScene(manifest, "Forest");
            var settlement = FindScene(manifest, "Settlement");

            Assert.That(forest.ObjectCount, Is.GreaterThan(200));
            Assert.That(forest.GeometryVariantCount, Is.GreaterThan(1));
            Assert.That(forest.MaterialVariantCount, Is.EqualTo(4));
            Assert.That(forest.LODLevelCount, Is.EqualTo(3));
            Assert.That(settlement.CollisionObjectCount, Is.EqualTo(settlement.ObjectCount));
            Assert.That(settlement.LODLevelCount, Is.EqualTo(3));
        }

        [Test]
        public void TrackedManifestReferencesImportedScenesWithStableMetaGUIDs()
        {
            if (!File.Exists(MANIFEST_PATH))
                Assert.Ignore("Tracked fixture manifest has not been generated in this checkout.");

            var manifest = JsonUtility.FromJson<Test_WorldFixtureManifest>(File.ReadAllText(MANIFEST_PATH));
            Assert.That(manifest, Is.Not.Null);
            var expected = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            Assert.That(manifest.SchemaVersion, Is.EqualTo(expected.SchemaVersion));
            Assert.That(manifest.ManifestHash, Is.EqualTo(expected.ManifestHash));
            Assert.That(manifest.Scenes.Length, Is.EqualTo(expected.Scenes.Length));
            for (var index = 0; index < manifest.Scenes.Length; index++)
            {
                Assert.That(manifest.Scenes[index].SceneHash, Is.EqualTo(expected.Scenes[index].SceneHash));
                Assert.That(manifest.Scenes[index].ContentHash, Is.EqualTo(expected.Scenes[index].ContentHash));
                Assert.That(manifest.Scenes[index].SpawnMetadataHash, Is.EqualTo(expected.Scenes[index].SpawnMetadataHash));
            }

            foreach (var scene in manifest.Scenes)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.AssetPath), Is.Not.Null, scene.AssetPath);
                var importedGUID = AssetDatabase.AssetPathToGUID(scene.AssetPath);
                Assert.That(importedGUID, Is.EqualTo(scene.MetaGUID), scene.AssetPath);
                Assert.That(importedGUID, Does.Match("^[0-9a-f]{32}$"), scene.AssetPath);
            }
        }

        private static Test_WorldFixturePlan FindScene(Test_WorldFixtureManifest manifest, string variant)
        {
            foreach (var scene in manifest.Scenes)
            {
                if (scene.Variant == variant)
                    return scene;
            }

            Assert.Fail($"Manifest does not contain fixture variant '{variant}'.");
            return null;
        }
    }
}
