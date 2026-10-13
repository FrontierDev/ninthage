using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Performance.Fixtures.Actors
{
    /// <summary>Writes the tracked actor and scenario manifests from deterministic plans.</summary>
    public static class Test_ActorFixtureGenerator
    {
        [MenuItem("Tools/Ninth Age/Tests/Performance/Regenerate Actor Fixtures", priority = 310)]
        public static void Regenerate()
        {
            ValidateDeterminism();
            var catalog = Test_ActorFixtureLayout.BuildCatalog();
            var scenarios = Test_ActorFixtureLayout.BuildScenarioCatalog();
            File.WriteAllText(Test_ActorFixtureLayout.CATALOG_PATH, JsonUtility.ToJson(catalog, true));
            File.WriteAllText(Test_ActorFixtureLayout.SCENARIO_PATH, JsonUtility.ToJson(scenarios, true));
            AssetDatabase.ImportAsset(Test_ActorFixtureLayout.CATALOG_PATH, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Test_ActorFixtureLayout.SCENARIO_PATH, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Generated actor fixture catalog {catalog.catalog_sha256} and scenario catalog {scenarios.scenario_catalog_sha256}.");
        }

        private static void ValidateDeterminism()
        {
            var firstCatalog = Test_ActorFixtureLayout.BuildCatalog();
            var secondCatalog = Test_ActorFixtureLayout.BuildCatalog();
            if (firstCatalog.catalog_sha256 != secondCatalog.catalog_sha256)
                throw new InvalidOperationException("Actor fixture catalog is not deterministic for the same seed and inputs.");

            var changedSeedCatalog = Test_ActorFixtureLayout.BuildCatalog(Test_ActorFixtureLayout.DEFAULT_SEED + 1);
            if (firstCatalog.catalog_sha256 == changedSeedCatalog.catalog_sha256)
                throw new InvalidOperationException("Changing the actor fixture seed did not change the population catalog.");

            var firstScenario = Test_ActorFixtureLayout.BuildScenarioCatalog();
            var secondScenario = Test_ActorFixtureLayout.BuildScenarioCatalog();
            if (firstScenario.scenario_catalog_sha256 != secondScenario.scenario_catalog_sha256)
                throw new InvalidOperationException("Actor scenario catalog is not deterministic for the same seed and inputs.");
        }
    }
}
