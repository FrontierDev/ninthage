using Game.Tests.Performance.Fixtures;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Test]
        public void TrackedScenesExposeRuntimeFixtureMetadataOnTheirRoot()
        {
            Assert.That(typeof(Test_WorldFixtureMetadata).Assembly.GetName().Name, Is.EqualTo("Game.WorldFixtures.Runtime"));

            var expectedManifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });

            foreach (var expectedPlan in expectedManifest.Scenes)
            {
                Assert.That(File.Exists(expectedPlan.AssetPath), Is.True, expectedPlan.AssetPath);
                var existingScene = SceneManager.GetSceneByPath(expectedPlan.AssetPath);
                var openedByTest = !(existingScene.IsValid() && existingScene.isLoaded);
                var scene = openedByTest
                    ? EditorSceneManager.OpenScene(expectedPlan.AssetPath, OpenSceneMode.Additive)
                    : existingScene;
                try
                {
                    Assert.That(scene.IsValid() && scene.isLoaded, Is.True, expectedPlan.AssetPath);
                    var fixtureRoot = FindRoot(scene, "FixtureRoot");
                    Assert.That(fixtureRoot, Is.Not.Null, expectedPlan.AssetPath);
                    AssertNoMissingScripts(scene, expectedPlan.AssetPath);
                    Assert.That(
                        fixtureRoot.transform.childCount,
                        Is.EqualTo(expectedPlan.ObjectCount),
                        $"Scene '{expectedPlan.AssetPath}' contains a different number of direct spawn objects than its manifest.");

                    var metadata = fixtureRoot.GetComponent<Test_WorldFixtureMetadata>();
                    Assert.That(metadata, Is.Not.Null, expectedPlan.AssetPath);
                    Assert.That(metadata.FixtureVersion, Is.EqualTo(Test_WorldFixtureLayout.FIXTURE_VERSION), expectedPlan.AssetPath);
                    Assert.That(metadata.FixtureID, Is.EqualTo(expectedPlan.FixtureID), expectedPlan.AssetPath);
                    Assert.That(metadata.Variant, Is.EqualTo(expectedPlan.Variant), expectedPlan.AssetPath);
                    Assert.That(metadata.Status, Is.EqualTo(expectedPlan.Status), expectedPlan.AssetPath);
                    Assert.That(metadata.Seed, Is.EqualTo(Test_WorldFixtureLayout.DEFAULT_SEED), expectedPlan.AssetPath);
                    AssertSpawnMetadata(expectedPlan, fixtureRoot);
                }
                finally
                {
                    if (openedByTest && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return root;
            }

            return null;
        }

        private static void AssertNoMissingScripts(Scene scene, string scenePath)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var missingScript = FindMissingScript(root, root.name);
                Assert.That(missingScript, Is.Null, $"Scene '{scenePath}' contains an unresolved script reference: {missingScript}");
            }
        }

        private static string FindMissingScript(GameObject root, string hierarchyPath)
        {
            var components = root.GetComponents<Component>();
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] == null)
                    return $"GameObject '{hierarchyPath}' component slot {index} has no imported script.";
            }

            for (var index = 0; index < root.transform.childCount; index++)
            {
                var child = root.transform.GetChild(index).gameObject;
                var missingScript = FindMissingScript(child, hierarchyPath + "/" + child.name);
                if (missingScript != null)
                    return missingScript;
            }

            return null;
        }

        private static void AssertSpawnMetadata(Test_WorldFixturePlan expectedPlan, GameObject fixtureRoot)
        {
            Assert.That(expectedPlan.Spawns, Is.Not.Null, expectedPlan.AssetPath);
            foreach (var expectedSpawn in expectedPlan.Spawns)
            {
                var spawnName = expectedSpawn.SpawnID.Replace('/', '_');
                var spawnRoot = FindDirectChild(fixtureRoot.transform, spawnName);
                Assert.That(spawnRoot, Is.Not.Null, $"Scene '{expectedPlan.AssetPath}' is missing spawn '{expectedSpawn.SpawnID}'.");
                if (spawnRoot == null)
                    continue;

                var metadata = spawnRoot.GetComponent<Test_WorldFixtureSpawnMetadata>();
                if (expectedSpawn.Kind == "Ground")
                {
                    Assert.That(metadata, Is.Null, $"Ground spawn '{expectedSpawn.SpawnID}' unexpectedly has spawn metadata.");
                    continue;
                }

                Assert.That(metadata, Is.Not.Null, $"Spawn '{expectedSpawn.SpawnID}' is missing Test_WorldFixtureSpawnMetadata.");
                if (metadata == null)
                    continue;

                Assert.That(metadata.SpawnID, Is.EqualTo(expectedSpawn.SpawnID));
                Assert.That(metadata.Kind, Is.EqualTo(expectedSpawn.Kind));
                Assert.That(metadata.Primitive, Is.EqualTo(expectedSpawn.Primitive));
                Assert.That(metadata.WorldX, Is.EqualTo(expectedSpawn.WorldX));
                Assert.That(metadata.WorldY, Is.EqualTo(expectedSpawn.WorldY));
                Assert.That(metadata.WorldZ, Is.EqualTo(expectedSpawn.WorldZ));
                Assert.That(metadata.OriginCellX, Is.EqualTo(expectedSpawn.OriginCellX));
                Assert.That(metadata.OriginCellZ, Is.EqualTo(expectedSpawn.OriginCellZ));
                Assert.That(metadata.WorldTileX, Is.EqualTo(expectedSpawn.WorldTileX));
                Assert.That(metadata.WorldTileZ, Is.EqualTo(expectedSpawn.WorldTileZ));
                Assert.That(metadata.MaterialVariant, Is.EqualTo(expectedSpawn.MaterialVariant));
                Assert.That(metadata.LODLevels, Is.EqualTo(expectedSpawn.LODLevels));
                Assert.That(metadata.HasCollision, Is.EqualTo(expectedSpawn.HasCollision));
            }
        }

        private static GameObject FindDirectChild(Transform parent, string childName)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index).gameObject;
                if (child.name == childName)
                    return child;
            }

            return null;
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
