using NUnit.Framework;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System.IO;

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
    }
}
