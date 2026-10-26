using Game.Tests.Performance.Fixtures;
using Game.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Fixtures.Editor
{
    /// <summary>
    /// Generates and validates the tracked Stage 1.0 world performance fixtures.
    /// The generator creates only test-owned scenes, materials and manifest data.
    /// </summary>
    public static class Test_WorldFixtureGenerator
    {
        private const string FIXTURE_ROOT = "Assets/Tests/Performance/Fixtures";
        private const string WORLD_ROOT = FIXTURE_ROOT + "/Worlds";
        private const string MATERIAL_ROOT = WORLD_ROOT + "/Materials";
        private const string MANIFEST_PATH = WORLD_ROOT + "/world-fixture-manifest.json";
        private const string MATERIAL_SHADER = "Universal Render Pipeline/Lit";

        private static readonly Color[] MATERIAL_COLORS =
        {
            new(0.30f, 0.52f, 0.28f, 1f),
            new(0.18f, 0.36f, 0.16f, 1f),
            new(0.42f, 0.30f, 0.18f, 1f),
            new(0.48f, 0.48f, 0.46f, 1f),
        };

        /// <summary>Regenerates the default D1 world fixture tier from the canonical seed.</summary>
        [MenuItem("Tools/Ninth Age/Tests/Performance/Regenerate World Fixtures (D1 Default)", priority = 300)]
        public static void RegenerateDefault()
        {
            Regenerate(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
        }

        /// <summary>Regenerates the minimal D0 world fixture tier from the canonical seed.</summary>
        [MenuItem("Tools/Ninth Age/Tests/Performance/Regenerate World Fixtures (D0 Minimal)", priority = 301)]
        public static void RegenerateMinimal()
        {
            Regenerate(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D0_Minimal,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
        }

        /// <summary>
        /// Regenerates all fixture scenes and the manifest for the supplied deterministic inputs.
        /// Existing fixture asset paths are overwritten only after their current metadata identity is captured.
        /// </summary>
        public static Test_WorldFixtureManifest Regenerate(Test_WorldFixtureOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ValidateDeterminism(options);
            EnsureFixtureFolders();
            EnsureFixtureMaterials();

            var manifest = Test_WorldFixtureLayout.BuildManifest(options);
            foreach (var plan in manifest.Scenes)
            {
                GenerateScene(plan, options.Seed);
                plan.MetaGUID = AssetDatabase.AssetPathToGUID(plan.AssetPath);
                if (string.IsNullOrEmpty(plan.MetaGUID))
                    throw new InvalidOperationException($"Unity did not import generated fixture scene '{plan.AssetPath}'.");
            }

            manifest.ManifestHash = Test_WorldFixtureLayout.Hash(Test_WorldFixtureLayout.CanonicalManifest(manifest));
            WriteManifest(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateManifestReferences(manifest);
            return manifest;
        }

        /// <summary>Builds deterministic metadata for tests without modifying project assets.</summary>
        public static Test_WorldFixtureManifest BuildManifest(Test_WorldFixtureOptions options)
        {
            return Test_WorldFixtureLayout.BuildManifest(options);
        }

        private static void ValidateDeterminism(Test_WorldFixtureOptions options)
        {
            var first = Test_WorldFixtureLayout.BuildManifest(options);
            var second = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = options.Seed,
                DensityTier = options.DensityTier,
                FixtureVersion = options.FixtureVersion,
            });

            if (first.ManifestHash != second.ManifestHash)
                throw new InvalidOperationException("World fixture generation is not deterministic for the same seed and inputs.");

            var changedSeed = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = unchecked(options.Seed + 1),
                DensityTier = options.DensityTier,
                FixtureVersion = options.FixtureVersion,
            });

            if (first.ManifestHash == changedSeed.ManifestHash)
                throw new InvalidOperationException("Changing the fixture seed did not change procedural world metadata.");

            if (!Test_WorldFixtureLayout.ValidateCoordinateContinuity(first.Scenes))
                throw new InvalidOperationException("Adjacent fixture tile bounds are not coordinate-continuous.");
        }

        private static void GenerateScene(Test_WorldFixturePlan plan, int seed)
        {
            var previousGUID = AssetDatabase.AssetPathToGUID(plan.AssetPath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var fixtureRoot = new GameObject("FixtureRoot");
                var metadata = fixtureRoot.AddComponent<Test_WorldFixtureMetadata>();
                metadata.Set(plan, seed, Test_WorldFixtureLayout.FIXTURE_VERSION);

                var configuredGroundLayer = -1;
                foreach (var spawn in plan.Spawns)
                {
                    if (spawn.Kind == "Ground" && configuredGroundLayer < 0)
                        configuredGroundLayer = ResolveConfiguredGroundLayer();
                    CreateSpawn(fixtureRoot.transform, spawn, configuredGroundLayer);
                }

                if (plan.IsNavigable)
                    AddNavigationSurface(fixtureRoot, plan);

                if (!EditorSceneManager.SaveScene(scene, plan.AssetPath, false))
                    throw new InvalidOperationException($"Could not save generated fixture scene '{plan.AssetPath}'.");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.ImportAsset(plan.AssetPath, ImportAssetOptions.ForceSynchronousImport);
            var currentGUID = AssetDatabase.AssetPathToGUID(plan.AssetPath);
            if (!string.IsNullOrEmpty(previousGUID) && previousGUID != currentGUID)
            {
                throw new InvalidOperationException(
                    $"Regeneration changed the fixture scene GUID for '{plan.AssetPath}' from '{previousGUID}' to '{currentGUID}'.");
            }
        }

        private static void AddNavigationSurface(GameObject fixtureRoot, Test_WorldFixturePlan plan)
        {
            var navigationSurface = fixtureRoot.AddComponent<NavMeshSurface>();
            navigationSurface.collectObjects = CollectObjects.Volume;
            navigationSurface.center = new Vector3(
                128f + plan.WorldTileX * Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS,
                0f,
                128f + plan.WorldTileZ * Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS);
            navigationSurface.size = new Vector3(Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS, 32f, Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS);
            navigationSurface.layerMask = ~0;
            navigationSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navigationSurface.overrideTileSize = true;
            navigationSurface.tileSize = Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS;
            navigationSurface.BuildNavMesh();
        }

        private static void CreateSpawn(Transform parent, Test_WorldFixtureSpawn spawn, int configuredGroundLayer)
        {
            var worldPosition = new Vector3((float)spawn.WorldX, (float)spawn.WorldY, (float)spawn.WorldZ);
            var scale = new Vector3(spawn.ScaleX, spawn.ScaleY, spawn.ScaleZ);
            if (spawn.Kind == "Ground")
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = spawn.SpawnID.Replace('/', '_');
                ground.transform.SetParent(parent, true);
                ground.transform.position = worldPosition;
                ground.transform.localScale = scale;
                if (configuredGroundLayer < 0)
                    throw new InvalidOperationException($"Ground spawn '{spawn.SpawnID}' has no resolved configured ground layer.");
                ground.layer = configuredGroundLayer;
                ground.GetComponent<Renderer>().sharedMaterial = GetMaterial(spawn.MaterialVariant);
                return;
            }

            var root = new GameObject(spawn.SpawnID.Replace('/', '_'));
            root.transform.SetParent(parent, true);
            root.transform.position = worldPosition;
            var lodGroup = root.AddComponent<LODGroup>();
            var lods = new List<LOD>();
            for (var lodIndex = 0; lodIndex < spawn.LODLevels; lodIndex++)
            {
                var lodObject = GameObject.CreatePrimitive(ToPrimitiveType(spawn.Primitive));
                lodObject.name = $"{root.name}_LOD{lodIndex}";
                lodObject.transform.SetParent(root.transform, false);
                lodObject.transform.localScale = scale * (1f - lodIndex * 0.2f);
                lodObject.GetComponent<Renderer>().sharedMaterial = GetMaterial(spawn.MaterialVariant);

                var collider = lodObject.GetComponent<Collider>();
                if (lodIndex > 0 || !spawn.HasCollision)
                    UnityEngine.Object.DestroyImmediate(collider);

                lods.Add(new LOD(GetLODTransition(lodIndex), lodObject.GetComponentsInChildren<Renderer>()));
            }

            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();

            var metadata = root.AddComponent<Test_WorldFixtureSpawnMetadata>();
            metadata.Set(spawn);
        }

        private static PrimitiveType ToPrimitiveType(string primitive)
        {
            switch (primitive)
            {
                case "Cube":
                    return PrimitiveType.Cube;
                case "Capsule":
                    return PrimitiveType.Capsule;
                case "Cylinder":
                    return PrimitiveType.Cylinder;
                case "Sphere":
                    return PrimitiveType.Sphere;
                default:
                    throw new InvalidOperationException($"Unsupported deterministic fixture primitive '{primitive}'.");
            }
        }

        private static float GetLODTransition(int lodIndex)
        {
            switch (lodIndex)
            {
                case 0:
                    return 0.6f;
                case 1:
                    return 0.2f;
                default:
                    return 0.05f;
            }
        }

        private static Material GetMaterial(int materialVariant)
        {
            var clampedVariant = Mathf.Clamp(materialVariant, 0, MATERIAL_COLORS.Length - 1);
            var path = $"{MATERIAL_ROOT}/Test_WorldFixture_Material_{clampedVariant}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(MATERIAL_SHADER);
                if (shader == null)
                    throw new InvalidOperationException($"Required fixture shader '{MATERIAL_SHADER}' is unavailable.");

                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    color = MATERIAL_COLORS[clampedVariant],
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.color = MATERIAL_COLORS[clampedVariant];
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static int ResolveConfiguredGroundLayer()
        {
            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            var configuration = GameConfigurationManager.Config;
            if (configuration == null)
                throw new InvalidOperationException("Could not generate ground fixture geometry because GameConfigurationManager.Config is unavailable.");

            var layerName = configuration.GroundDetectionMaskName;
            if (string.IsNullOrWhiteSpace(layerName))
                throw new InvalidOperationException("Could not generate ground fixture geometry because the configured ground detection layer name is empty.");

            var layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex < 0)
                throw new InvalidOperationException(
                    $"Could not generate ground fixture geometry because configured ground detection layer '{layerName}' does not exist in the Unity layer configuration.");

            return layerIndex;
        }

        private static void EnsureFixtureMaterials()
        {
            EnsureFolder(MATERIAL_ROOT);
            for (var index = 0; index < MATERIAL_COLORS.Length; index++)
                GetMaterial(index);
        }

        private static void EnsureFixtureFolders()
        {
            EnsureFolder("Assets/Tests/Performance");
            EnsureFolder(FIXTURE_ROOT);
            EnsureFolder(WORLD_ROOT);
            EnsureFolder(MATERIAL_ROOT);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void WriteManifest(Test_WorldFixtureManifest manifest)
        {
            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(MANIFEST_PATH, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(MANIFEST_PATH, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ValidateManifestReferences(Test_WorldFixtureManifest manifest)
        {
            foreach (var plan in manifest.Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(plan.AssetPath) == null)
                    throw new InvalidOperationException($"Generated manifest references missing scene '{plan.AssetPath}'.");

                var importedGUID = AssetDatabase.AssetPathToGUID(plan.AssetPath);
                if (importedGUID != plan.MetaGUID)
                {
                    throw new InvalidOperationException(
                        $"Generated manifest GUID mismatch for '{plan.AssetPath}': '{plan.MetaGUID}' versus '{importedGUID}'.");
                }
            }
        }
    }
}
