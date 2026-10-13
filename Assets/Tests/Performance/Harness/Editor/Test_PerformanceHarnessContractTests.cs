using NUnit.Framework;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Regression coverage for deterministic runner selection and explicit capability boundaries.</summary>
    public sealed class Test_PerformanceHarnessContractTests
    {
        [Test]
        public void DataOnlyModesUseTheEcsFixtureWithoutRealPlayers()
        {
            var options = new Test_PerformanceHarnessOptions();

            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(options.IsServerOnlyDataMode, Is.True);
            Assert.That(options.ActorCount, Is.EqualTo(100));
            Assert.That(options.Seed, Is.EqualTo(424242));
        }

        [Test]
        public void ActivityModeKeepsTheSameDeterministicActorVariant()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
            };

            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(options.IsServerOnlyDataMode, Is.True);
        }

        [Test]
        public void UnsupportedExecutionModesHaveExplicitDiagnostics()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.StandaloneHeadlessServer,
            };
            Assert.That(options.GetUnsupportedModeDiagnostic(), Does.Contain("blocked"));

            options.WorkloadMode = Test_PerformanceWorkloadMode.NetworkedClients;
            Assert.That(options.GetUnsupportedModeDiagnostic(), Does.Contain("real-client"));
        }

        [Test]
        public void TrackedHarnessSceneAndFixtureInputsRemainPresent()
        {
            Assert.That(File.Exists(Test_PerformanceRun.HARNESS_SCENE_PATH), Is.True);
            Assert.That(File.Exists(Test_ActorFixtureLayout.CATALOG_PATH), Is.True);
            Assert.That(File.Exists(Test_ActorFixtureLayout.SCENARIO_PATH), Is.True);
        }

        [Test]
        public void PendingRunStatePreservesRunIdentityAndValidatedOptions()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorldFixture = Test_WorldFixtureVariant.Forest,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
            };
            var state = new Test_PerformancePendingRunState
            {
                runID = "run-domain-reload-regression",
                resultsDirectory = "C:/project/Builds/Benchmarks/Results/run-domain-reload-regression",
                fixturePath = "C:/project/Builds/Benchmarks/Results/run-domain-reload-regression/fixture.json",
                options = Test_PerformancePendingRunOptions.Capture(options),
                lifecycle = "starting",
            };

            var restoredState = JsonUtility.FromJson<Test_PerformancePendingRunState>(JsonUtility.ToJson(state));
            var restoredOptions = restoredState.options.ToOptions();
            Assert.That(restoredState.runID, Is.EqualTo(state.runID));
            Assert.That(restoredState.resultsDirectory, Is.EqualTo(state.resultsDirectory));
            Assert.That(restoredState.fixturePath, Is.EqualTo(state.fixturePath));
            Assert.That(restoredState.lifecycle, Is.EqualTo("starting"));
            Assert.That(restoredOptions.WorldFixture, Is.EqualTo(options.WorldFixture));
            Assert.That(restoredOptions.WorkloadMode, Is.EqualTo(options.WorkloadMode));
            Assert.That(restoredOptions.Seed, Is.EqualTo(options.Seed));
        }

        [Test]
        public void PreviousSceneSetupStatePreservesRestoreMarkers()
        {
            var original = new[]
            {
                new SceneSetup
                {
                    path = "Assets/Tests/Performance/Harness/Editor/PerformanceHarnessServer.unity",
                    isLoaded = true,
                    isActive = true,
                    isSubScene = false,
                },
            };

            var state = Test_PerformanceSceneSetupState.Capture(original);
            var restored = state[0].ToSceneSetup();
            Assert.That(restored.path, Is.EqualTo(original[0].path));
            Assert.That(restored.isLoaded, Is.EqualTo(original[0].isLoaded));
            Assert.That(restored.isActive, Is.EqualTo(original[0].isActive));
            Assert.That(restored.isSubScene, Is.EqualTo(original[0].isSubScene));
        }
    }
}
