using NUnit.Framework;
using Game.Server.Core;
using Game.Shared;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        public void TimedStateStressModeIsServerOnlyDataWithTheTrackedActorVariant()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateStress,
            };

            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(options.IsServerOnlyDataMode, Is.True);
            Assert.That(options.ActorCount, Is.EqualTo(100));
            Assert.That(options.Seed, Is.EqualTo(424242));
            Assert.That(options.WorkloadMode.ToString(), Is.EqualTo("EcsDataOnlyTimedStateStress"));
        }

        [Test]
        public void MinimalAndActivityModesRetainTheirExistingSelectionSemantics()
        {
            var minimal = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal,
            };
            var activity = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyActivity,
            };

            Assert.That(minimal.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(minimal.IsServerOnlyDataMode, Is.True);
            Assert.That(activity.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(activity.IsServerOnlyDataMode, Is.True);
        }

        [Test]
        public void TimedStateStressUsesTheRequiredBoundedEntryCounts()
        {
            const int actorCount = 100;

            Assert.That(
                actorCount * Test_PerformanceActivityController.TimedStressCooldownsPerEntity,
                Is.EqualTo(1600));
            Assert.That(
                actorCount * Test_PerformanceActivityController.TimedStressAurasPerEntity,
                Is.EqualTo(400));
            Assert.That(
                actorCount * Test_PerformanceActivityController.TimedStressTickersPerEntity,
                Is.EqualTo(400));
        }

        [Test]
        public void TimedStateStressPeriodicEventsRemainBelowTheServerEventBufferCapacity()
        {
            const int actorCount = 100;
            const int serverEventBufferCapacity = 4096;
            var synchronizedPeriodicEvents = actorCount * Test_PerformanceActivityController.TimedStressPeriodicEventsPerEntity;

            Assert.That(synchronizedPeriodicEvents, Is.EqualTo(800));
            Assert.That(synchronizedPeriodicEvents, Is.LessThan(serverEventBufferCapacity));
        }

        [Test]
        public void ResourceCeilingUsesPositiveGrowthFromTheWorkloadBaseline()
        {
            const long gibibyte = 1024L * 1024L * 1024L;
            var baseline = 5L * gibibyte;

            Assert.That(Test_PerformanceActivityController.GetAllocatedGrowthBytes(baseline, baseline), Is.EqualTo(0L));
            Assert.That(Test_PerformanceActivityController.GetAllocatedGrowthBytes(baseline, baseline + gibibyte / 2L), Is.EqualTo(gibibyte / 2L));
            Assert.That(Test_PerformanceActivityController.GetAllocatedGrowthBytes(baseline, baseline - gibibyte / 10L), Is.EqualTo(0L));
            Assert.That(
                Test_PerformanceActivityController.GetAllocatedGrowthBytes(baseline, 10L * gibibyte),
                Is.GreaterThan(Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES));
        }

        [Test]
        public void ResourceEvidenceReportsAbsoluteAndWorkloadRelativeMemory()
        {
            const long gibibyte = 1024L * 1024L * 1024L;
            var baseline = 5L * gibibyte;
            var total = baseline + gibibyte / 2L;
            var resources = Test_PerformanceRun.BuildResourceEvidenceJson(
                baseline,
                total,
                PerformanceRunStatus.CompleteMeasured);

            Assert.That(resources, Does.Contain($"\"baseline_total_allocated_bytes\":{baseline}"));
            Assert.That(resources, Does.Contain($"\"total_allocated_bytes\":{total}"));
            Assert.That(resources, Does.Contain($"\"allocated_growth_bytes\":{gibibyte / 2L}"));
            Assert.That(resources, Does.Contain($"\"growth_ceiling_bytes\":{Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES}"));
            Assert.That(resources, Does.Contain("\"ceiling_basis\":\"growth_from_pre_workload_baseline\""));
            Assert.That(resources, Does.Contain("\"ceiling_status\":\"not_exceeded\""));
        }

        [Test]
        public void ResourceEvidenceLeavesGrowthUnavailableWithoutAWorkloadBaseline()
        {
            var resources = Test_PerformanceRun.BuildResourceEvidenceJson(
                null,
                5L * 1024L * 1024L * 1024L,
                PerformanceRunStatus.BlockedPrerequisite);

            Assert.That(resources, Does.Contain("\"baseline_total_allocated_bytes\":null"));
            Assert.That(resources, Does.Contain("\"allocated_growth_bytes\":null"));
            Assert.That(resources, Does.Contain($"\"growth_ceiling_bytes\":{Test_PerformanceRun.MAX_WORKLOAD_ALLOCATED_GROWTH_BYTES}"));
            Assert.That(resources, Does.Contain("\"ceiling_status\":\"unavailable\""));
        }

        [Test]
        public void TimedStateStressStartsNewStateOnTheNextSimulationTick()
        {
            Assert.That(Test_PerformanceActivityController.GetFirstProgressTickID(0L), Is.EqualTo(1L));
            Assert.That(Test_PerformanceActivityController.GetFirstProgressTickID(100L), Is.EqualTo(101L));
        }

        [Test]
        public void ExistingDataOnlyModesRemainFixedAtTheCanonicalPopulation()
        {
            var options = new Test_PerformanceHarnessOptions { ActorCount = 101 };
            Assert.That(options.TryValidate(out var diagnostic, out _, out _), Is.False);
            Assert.That(diagnostic, Does.Contain("fixed population of 100"));
        }

        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2500)]
        [TestCase(5000)]
        public void TimedStateScaleAcceptsOnlyTheSupportedTiers(int actorCount)
        {
            var profile = Test_ActorFixtureLayout.BuildTimedStateScaleProfile(actorCount, Test_ActorFixtureLayout.DEFAULT_SEED);
            var scenario = Test_ActorFixtureLayout.BuildTimedStateScaleScenario(actorCount, Test_ActorFixtureLayout.DEFAULT_SEED);
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale,
                ActorCount = actorCount,
            };

            Assert.That(profile.population.npcs.requested, Is.EqualTo(actorCount));
            Assert.That(profile.population.accounting.ecs_data_only_npcs, Is.EqualTo(actorCount));
            Assert.That(options.IsTimedStateScale, Is.True);
            Assert.That(options.IsServerOnlyDataMode, Is.True);
            Assert.That(options.EffectiveProfileID, Is.EqualTo(Test_ActorFixtureLayout.TIMED_SCALE_PROFILE_ID));
            Assert.That(options.ActorVariantID, Is.EqualTo(Test_ActorFixtureLayout.TIMED_SCALE_VARIANT_ID));
            Assert.That(scenario.movement_routes, Is.Empty);
            Assert.That(scenario.ability_schedule, Is.Empty);
            Assert.That(scenario.events, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(101)]
        [TestCase(750)]
        [TestCase(5001)]
        public void TimedStateScaleRejectsUnsupportedTiers(int actorCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Test_ActorFixtureLayout.BuildTimedStateScaleProfile(actorCount, Test_ActorFixtureLayout.DEFAULT_SEED));
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale,
                ActorCount = actorCount,
            };
            Assert.That(options.TryValidate(out var diagnostic, out _, out _), Is.False);
            Assert.That(diagnostic, Does.Contain("100, 500, 1000, 2500 and 5000"));
        }

        [Test]
        public void TimedStateScaleKeepsTrackedSeedDensityAndOutdoorWorld()
        {
            var invalidDensity = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale,
                DensityTier = (Test_WorldFixtureDensityTier)999,
            };
            var invalidSeed = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale,
                Seed = 7,
            };
            var invalidWorld = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale,
                WorldFixture = Test_WorldFixtureVariant.Forest,
            };

            Assert.That(invalidDensity.TryValidate(out var densityDiagnostic, out _, out _), Is.False);
            Assert.That(densityDiagnostic, Does.Contain("D1_Default"));
            Assert.That(invalidSeed.TryValidate(out var seedDiagnostic, out _, out _), Is.False);
            Assert.That(seedDiagnostic, Does.Contain("seed 424242"));
            Assert.That(invalidWorld.TryValidate(out var worldDiagnostic, out _, out _), Is.False);
            Assert.That(worldDiagnostic, Does.Contain("Outdoor"));
        }

        [Test]
        public void TimedStateScalePlacementsAreDeterministicUniqueAndComplete()
        {
            var profile = Test_ActorFixtureLayout.BuildTimedStateScaleProfile(5000, Test_ActorFixtureLayout.DEFAULT_SEED);
            var first = Test_ActorFixtureLayout.BuildPlacements(profile);
            var second = Test_ActorFixtureLayout.BuildPlacements(profile);
            var ids = new System.Collections.Generic.HashSet<string>();

            Assert.That(first.Count, Is.EqualTo(5000));
            for (var index = 0; index < first.Count; index++)
            {
                Assert.That(ids.Add(first[index].actor_fixture_id), Is.True);
                Assert.That(first[index].actor_fixture_id, Does.Contain(Test_ActorFixtureLayout.TIMED_SCALE_VARIANT_ID));
                Assert.That(first[index].actor_fixture_id, Is.EqualTo(second[index].actor_fixture_id));
                Assert.That(first[index].world_x, Is.EqualTo(second[index].world_x));
                Assert.That(first[index].world_z, Is.EqualTo(second[index].world_z));
            }
        }

        [TestCase(100, 100, 1600, 400, 400)]
        [TestCase(500, 500, 8000, 2000, 2000)]
        [TestCase(1000, 1000, 16000, 4000, 4000)]
        [TestCase(2500, 2500, 40000, 10000, 10000)]
        [TestCase(5000, 5000, 80000, 20000, 20000)]
        public void TimedStateScaleEntryCountsPreserveThePerEntityLoad(int actorCount, int casts, int cooldowns, int auras, int tickers)
        {
            Assert.That(actorCount, Is.EqualTo(casts));
            Assert.That(actorCount * Test_PerformanceActivityController.TimedStressCooldownsPerEntity, Is.EqualTo(cooldowns));
            Assert.That(actorCount * Test_PerformanceActivityController.TimedStressAurasPerEntity, Is.EqualTo(auras));
            Assert.That(actorCount * Test_PerformanceActivityController.TimedStressTickersPerEntity, Is.EqualTo(tickers));
        }

        [Test]
        public void TimedStateScaleEventPeakUsesConfiguredTickDurationAndStaysBelowCapacity()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<GameConfiguration>("Assets/Resources/GameConfiguration.asset");
            Assert.That(configuration, Is.Not.Null);
            var castTicks = Test_PerformanceActivityController.GetNonShorteningTickCount(0.5f, configuration.ServerSimulationTickDuration);
            var intervalTicks = Test_PerformanceActivityController.GetNonShorteningTickCount(1f, configuration.ServerSimulationTickDuration);
            foreach (var actorCount in new[] { 100, 500, 1000, 2500, 5000 })
                Assert.That(Test_PerformanceActivityController.GetTimedStateScaleSyntheticEventPeak(actorCount, castTicks, intervalTicks), Is.LessThan(Test_PerformanceActivityController.TIMED_STATE_EVENT_BUFFER_CAPACITY));
            Assert.That(castTicks, Is.EqualTo(10));
            Assert.That(intervalTicks, Is.EqualTo(20));
            Assert.That(Test_PerformanceActivityController.GetTimedStateScaleSyntheticEventPeak(5000, castTicks, intervalTicks), Is.EqualTo(2500));
        }

        [Test]
        public void TimedStateStressRetainsFixedSynchronizedReferenceContract()
        {
            var options = new Test_PerformanceHarnessOptions { WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateStress };
            Assert.That(options.IsTimedStateStress, Is.True);
            Assert.That(options.IsTimedStateScale, Is.False);
            Assert.That(options.ActorCount, Is.EqualTo(Test_ActorFixtureLayout.NPC_COUNT));
            Assert.That(options.ActorVariantID, Is.EqualTo("S04-I-DATA"));
            Assert.That(100 * Test_PerformanceActivityController.TimedStressPeriodicEventsPerEntity, Is.EqualTo(800));
        }

        [Test]
        public void TimedStateScalingPresetDefinesFiveTiersAndTwentyFiveAttempts()
        {
            Assert.That(Test_PerformanceJobQueue.TIMED_STATE_SCALING_ACTOR_COUNTS, Is.EqualTo(new[] { 100, 500, 1000, 2500, 5000 }));
            var presetOptions = Test_PerformanceJobQueue.BuildTimedStateScalingPresetOptions();
            Assert.That(presetOptions.Count, Is.EqualTo(5));
            for (var index = 0; index < presetOptions.Count; index++)
            {
                Assert.That(presetOptions[index].WorkloadMode, Is.EqualTo(Test_PerformanceWorkloadMode.EcsDataOnlyTimedStateScale));
                Assert.That(presetOptions[index].ActorCount, Is.EqualTo(Test_PerformanceJobQueue.TIMED_STATE_SCALING_ACTOR_COUNTS[index]));
                Assert.That(presetOptions[index].WorldFixture, Is.EqualTo(Test_WorldFixtureVariant.Outdoor));
                Assert.That(presetOptions[index].DensityTier, Is.EqualTo(Test_WorldFixtureDensityTier.D1_Default));
                Assert.That(presetOptions[index].Seed, Is.EqualTo(Test_ActorFixtureLayout.DEFAULT_SEED));
            }
            Assert.That(Test_PerformanceJobQueue.TIMED_STATE_SCALING_ACTOR_COUNTS.Length * Test_PerformanceJobQueue.TIMED_STATE_SCALING_REPEAT_COUNT, Is.EqualTo(25));
        }

        [Test]
        public void Stage14ValidationDoesNotResolveAWorldFixture()
        {
            var options = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.FullNetworkedActors,
                WorldFixture = Test_WorldFixtureVariant.Forest,
            };

            Assert.That(options.TryValidate(out var diagnostic, out var worldPlan, out var actorProfile), Is.True, diagnostic);
            Assert.That(worldPlan, Is.Null);
            Assert.That(actorProfile, Is.Not.Null);
        }

        [Test]
        public void Stage14ExecutionDoesNotInjectAWorldFixtureScene()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var controllerPath = Path.Combine(
                projectRoot,
                "Assets/Tests/Performance/Harness/Editor/Test_PerformanceHarnessController.cs");
            var source = File.ReadAllText(controllerPath);
            var methodStart = source.IndexOf("private IEnumerator ExecuteStage14NPCActivityCore()", StringComparison.Ordinal);
            var methodEnd = source.IndexOf("private void ValidateStage14RuntimeAuthority()", methodStart, StringComparison.Ordinal);
            Assert.That(methodStart, Is.GreaterThanOrEqualTo(0), controllerPath);
            Assert.That(methodEnd, Is.GreaterThan(methodStart), controllerPath);
            var stage14Source = source.Substring(methodStart, methodEnd - methodStart);

            Assert.That(stage14Source, Does.Not.Contain("LoadSelectedWorldFixture"));
            Assert.That(stage14Source, Does.Not.Contain("EditorSceneManager.LoadSceneAsyncInPlayMode"));
            Assert.That(stage14Source, Does.Not.Contain("Test_WorldFixture_Outdoor"));
            Assert.That(stage14Source, Does.Contain("EnsureStage14PlacementChunks"));
            Assert.That(stage14Source, Does.Contain("ResolveAuthoritativeGround"));
            Assert.That(stage14Source, Does.Contain("StartFullActors"));
        }

        [Test]
        public void Stage14EngagementSentinelCompletesBeforeRamp()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var controllerPath = Path.Combine(
                projectRoot,
                "Assets/Tests/Performance/Harness/Editor/Test_PerformanceHarnessController.cs");
            var source = File.ReadAllText(controllerPath);
            var coreStart = source.IndexOf("private IEnumerator ExecuteStage14NPCActivityCore()", StringComparison.Ordinal);
            var coreEnd = source.IndexOf("private void ValidateStage14RuntimeAuthority()", coreStart, StringComparison.Ordinal);
            Assert.That(coreStart, Is.GreaterThanOrEqualTo(0), controllerPath);
            Assert.That(coreEnd, Is.GreaterThan(coreStart), controllerPath);
            var coreSource = source.Substring(coreStart, coreEnd - coreStart);
            var sentinelStart = coreSource.IndexOf("RunStage14EngagementSentinel", StringComparison.Ordinal);
            var rampStart = coreSource.IndexOf("_run.BeginPhase(PerformanceMeasurementPhase.Ramp)", StringComparison.Ordinal);

            Assert.That(coreSource, Does.Contain("StartEngagementForGroup(\"npc-engaged\", 1)"));
            Assert.That(sentinelStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(rampStart, Is.GreaterThan(sentinelStart));

            var sentinelMethodStart = source.IndexOf("private IEnumerator RunStage14EngagementSentinel", StringComparison.Ordinal);
            var sentinelMethodEnd = source.IndexOf("private void ValidateStage14RuntimeAuthority()", sentinelMethodStart, StringComparison.Ordinal);
            Assert.That(sentinelMethodStart, Is.GreaterThanOrEqualTo(0), controllerPath);
            Assert.That(sentinelMethodEnd, Is.GreaterThan(sentinelMethodStart), controllerPath);
            var sentinelSource = source.Substring(sentinelMethodStart, sentinelMethodEnd - sentinelMethodStart);
            Assert.That(sentinelSource, Does.Contain("STAGE14_ENGAGEMENT_SENTINEL_TIMEOUT_SECONDS"));
            Assert.That(sentinelSource, Does.Contain("finally"));
            Assert.That(sentinelSource, Does.Contain("StopEngagementForGroup(\"npc-engaged\")"));
            Assert.That(sentinelSource, Does.Contain("GetSentinelReacquisitionObservationSeconds"));
            Assert.That(sentinelSource, Does.Contain("observationDeadline"));
            Assert.That(sentinelSource, Does.Contain("ValidateEngagementStopped"));
            Assert.That(sentinelSource, Does.Contain("NPCAggroDecisionIntervalSeconds"));
            Assert.That(sentinelSource, Does.Contain("ServerSimulationTickDuration"));
            Assert.That(sentinelSource, Does.Contain("ValidatePlayerReferencesForSentinel"));
        }

        [Test]
        public void Stage14EngagementSentinelUsesProductionTargetAndCombatApis()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var populationPath = Path.Combine(
                projectRoot,
                "Assets/Tests/Performance/Fixtures/Actors/Test_ActorFixturePopulation.cs");
            var source = File.ReadAllText(populationPath);
            var startMethod = source.IndexOf("public IReadOnlyList<Actor> StartEngagementForGroup", StringComparison.Ordinal);
            var responsiveMethod = source.IndexOf("public bool IsEngagementResponsive", startMethod, StringComparison.Ordinal);
            var stopMethod = source.IndexOf("public void StopEngagementForGroup", responsiveMethod, StringComparison.Ordinal);
            var requestMethod = source.IndexOf("public void RequestAbilitySchedule", stopMethod, StringComparison.Ordinal);
            Assert.That(startMethod, Is.GreaterThanOrEqualTo(0), populationPath);
            Assert.That(responsiveMethod, Is.GreaterThan(startMethod), populationPath);
            Assert.That(stopMethod, Is.GreaterThan(responsiveMethod), populationPath);
            Assert.That(requestMethod, Is.GreaterThan(stopMethod), populationPath);

            var startSource = source.Substring(startMethod, responsiveMethod - startMethod);
            var stopSource = source.Substring(stopMethod, requestMethod - stopMethod);
            Assert.That(startSource, Does.Contain("SetTargetFromServer(_realPlayerTarget)"));
            Assert.That(startSource, Does.Contain("Server_StartAutoAttack()"));
            Assert.That(startSource, Does.Contain("ActorEndpoint.onServerStartAutoAttack +="));
            Assert.That(startSource, Does.Contain("ActorEndpoint.onServerStopAutoAttack +="));
            Assert.That(stopSource, Does.Contain("Server_StopAutoAttack()"));
            Assert.That(stopSource, Does.Not.Contain("SetTargetFromServer(null)"));
            Assert.That(stopSource, Does.Not.Contain("ActorEndpoint.onServerStartAutoAttack -="));
            Assert.That(stopSource, Does.Contain("TryGetActor(actor.ID"));
            Assert.That(stopSource, Does.Contain("ReferenceEquals(actor.Target, _realPlayerTarget)"));
            Assert.That(stopSource, Does.Contain("_restartedSentinelEngagementActorIDs"));
            Assert.That(source, Does.Contain("ActorEndpoint.onServerStopAutoAttack -="));
            Assert.That(startSource + stopSource, Does.Not.Contain("SetComponentData"));
            Assert.That(startSource + stopSource, Does.Not.Contain("EntityManager"));
        }

        [Test]
        public void Stage14PlayerReferenceValidationRemainsStrict()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var controllerPath = Path.Combine(
                projectRoot,
                "Assets/Tests/Performance/Harness/Editor/Test_NPCActivityScenarioController.cs");
            var source = File.ReadAllText(controllerPath);
            var validationStart = source.IndexOf("private void ValidatePlayerReferences()", StringComparison.Ordinal);
            var validationEnd = source.IndexOf("private static string FormatContext", validationStart, StringComparison.Ordinal);
            Assert.That(validationStart, Is.GreaterThanOrEqualTo(0), controllerPath);
            Assert.That(validationEnd, Is.GreaterThan(validationStart), controllerPath);
            var validationSource = source.Substring(validationStart, validationEnd - validationStart);

            Assert.That(validationSource, Does.Contain("player == null || player.isDead || player.Owner == null"));
            Assert.That(validationSource, Does.Contain("ChunkSubscriptions.PLAYER_ACTORS"));
            Assert.That(validationSource, Does.Contain("TryGetAcceptedPositionSnapshot"));
        }

        [Test]
        public void ExplicitEditorServerWorldNameRejectsBlankValues()
        {
            Assert.Throws<ArgumentException>(() => ServerInitialization.Begin(null, string.Empty));
            Assert.Throws<ArgumentException>(() => ServerInitialization.Begin(null, "   "));
        }

        [Test]
        public void ExplicitEditorServerWorldNameDoesNotUseCommandLineResolution()
        {
            Assert.Throws<ArgumentNullException>(() => ServerInitialization.Begin(null, "performance-harness"));
        }

        [Test]
        public void Stage14SpawnAreaGroundResolvesInOrdinaryRuntimeWhenServerIsRunning()
        {
            if (!GameConfigurationManager.IsInitialized
                || !ServerPhysicsManager.IsInitialized
                || !ServerWorldManager.IsInitialized
                || ServerWorldManager.Instance == null)
                Assert.Ignore("Authoritative ground resolution requires the running Stage 1.4 server scene.");

            if (!Test_NPCActivityScenarioController.TryCaptureRealPlayers(1, false, out var playerCapture, out var diagnostic))
                Assert.Ignore(diagnostic);
            Assert.That(playerCapture.Context, Is.EqualTo(ServerWorldManager.Instance.CurrentRuntimeContext));

            var profile = Test_ActorFixtureLayout.BuildStage14NPCActivityProfile(
                Test_NPCActivityScenario.ForcedFull,
                100,
                Test_ActorFixtureLayout.DEFAULT_SEED);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Test_ActorFixtureLayout.NPC_PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null, Test_ActorFixtureLayout.NPC_PREFAB_PATH);
            var passiveAggroRange = Test_ActorFixturePreflight.GetNPCPassiveAggroRange(prefab);
            var placements = Test_ActorFixtureLayout.BuildStage14NPCActivityPlacements(
                profile,
                Test_NPCActivityScenario.ForcedFull,
                playerCapture.AnchorPosition,
                passiveAggroRange);
            AssertRequiredChunksLoaded(placements, playerCapture.Context);
            AssertGroundResolutionPreservesHorizontalCoordinates(placements, Test_NPCActivityScenario.ForcedFull);

            var massWakePlacements = Test_ActorFixtureLayout.BuildStage14NPCActivityPlacements(
                profile,
                Test_NPCActivityScenario.MassWake,
                new WorldPosition(playerCapture.AnchorPosition.x + 140d, playerCapture.AnchorPosition.y, playerCapture.AnchorPosition.z),
                passiveAggroRange);
            AssertRequiredChunksLoaded(massWakePlacements, playerCapture.Context);
            AssertGroundResolutionPreservesHorizontalCoordinates(massWakePlacements, Test_NPCActivityScenario.MassWake);
        }

        private static void AssertRequiredChunksLoaded(
            IReadOnlyList<Test_ActorFixturePlacement> placements,
            ActorSpatialContext context)
        {
            foreach (var placement in placements)
            {
                var chunk = new WorldPosition(placement.world_x, placement.world_y, placement.world_z).Chunk;
                if (!ServerWorldManager.Instance.IsChunkLoaded(context, chunk))
                    Assert.Ignore($"The ordinary runtime chunk {chunk} is not loaded for the ground-resolution contract test.");
            }
        }

        private static void AssertGroundResolutionPreservesHorizontalCoordinates(
            IReadOnlyList<Test_ActorFixturePlacement> placements,
            Test_NPCActivityScenario scenario)
        {
            var originalX = placements.Select(placement => placement.world_x).ToArray();
            var originalZ = placements.Select(placement => placement.world_z).ToArray();
            var resolvedPlacements = Test_ActorFixturePreflight.ResolveAuthoritativeGround(placements, scenario);
            Assert.That(resolvedPlacements, Is.SameAs(placements));
            for (var index = 0; index < resolvedPlacements.Count; index++)
            {
                var placement = resolvedPlacements[index];
                Assert.That(placement.world_x, Is.EqualTo(originalX[index]));
                Assert.That(placement.world_z, Is.EqualTo(originalZ[index]));
                Assert.That(new WorldPosition(placement.world_x, placement.world_y, placement.world_z).IsFinite, Is.True);
            }
        }

        [Test]
        public void ActivityChurnIsMeasurementRelativeAndDeterministic()
        {
            var duration = Test_PerformanceRun.MEASUREMENT_SECONDS;
            var respawnThreshold = duration * 0.4f;

            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Warmup,
                    duration * 0.25f,
                    duration,
                    false,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    duration * 0.25f,
                    duration,
                    false,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.Despawn));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    respawnThreshold - 0.001f,
                    duration,
                    true,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    respawnThreshold,
                    duration,
                    true,
                    false),
                Is.EqualTo(Test_PerformanceActivityEvent.Respawn));
            Assert.That(
                Test_PerformanceActivityController.ResolveChurnEvent(
                    Test_PerformanceActivityPhase.Measurement,
                    duration,
                    duration,
                    true,
                    true),
                Is.EqualTo(Test_PerformanceActivityEvent.None));
        }

        [Test]
        public void ActivityEvidenceKeepsMeasuredSnapshotSeparateFromCumulativeEvents()
        {
            var snapshot = new Test_PerformanceActivitySnapshot(85, 4, 11, 10, 5, 5, 105, 5);
            var json = Test_PerformanceRun.BuildActivityJson(snapshot, "measurement_end_pre_cleanup");

            Assert.That(json, Does.Contain("\"snapshot_captured\":true"));
            Assert.That(json, Does.Contain("\"observation\":\"measurement_end_pre_cleanup\""));
            Assert.That(json, Does.Contain("\"idle\":85"));
            Assert.That(json, Does.Contain("\"combat\":11"));
            Assert.That(json, Does.Contain("\"wake_surge\":10"));
            Assert.That(json, Does.Contain("\"spawn_events\":5"));
            Assert.That(json, Does.Contain("\"spawn_events_total\":105"));
            Assert.That(json, Does.Contain("\"wake_surge_is_subset_of_combat\":true"));
        }

        [Test]
        public void InterruptedActivityEvidenceRemainsUnverified()
        {
            var json = Test_PerformanceRun.BuildActivityJson(null, null);

            Assert.That(json, Does.Contain("\"snapshot_captured\":false"));
            Assert.That(json, Does.Contain("\"observation\":\"unverified\""));
        }

        [Test]
        public void PerformanceSummaryUsesMeasurementSamplesOnly()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("phase-filter");
            var summary = CaptureTelemetrySummary(
                "performance-phase-filter",
                resultsDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Ramp);
                    PerformanceTelemetry.TryRecord(metric, 1000);
                    PerformanceTelemetry.TryRecord(metric, 2000);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 100; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Recovery);
                    PerformanceTelemetry.TryRecord(metric, 5000);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.phase-filter");

            Assert.That(metricJson, Does.Contain("\"phase\":\"measurement\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":100"));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":100"));
            Assert.That(metricJson, Does.Contain("\"min\":1"));
            Assert.That(metricJson, Does.Contain("\"max\":100"));
            Assert.That(metricJson, Does.Contain("\"mean\":50.5"));
            Assert.That(metricJson, Does.Contain("\"p50\":50"));
            Assert.That(metricJson, Does.Contain("\"p95\":95"));
            Assert.That(metricJson, Does.Contain("\"p99\":99"));
            Assert.That(metricJson, Does.Contain("\"retention_policy\":\"phase_reserved_exact\""));
        }

        [Test]
        public void ZeroMeasurementSamplesAreUnavailableForMeasurementPhase()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("no-measurement");
            var summary = CaptureTelemetrySummary(
                "performance-no-measurement",
                resultsDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Ramp);
                    PerformanceTelemetry.TryRecord(metric, 1000);
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.no-measurement");

            Assert.That(metricJson, Does.Contain("\"phase\":\"measurement\""));
            Assert.That(metricJson, Does.Contain("\"observation\":\"unavailable\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":0"));
            Assert.That(metricJson, Does.Contain("\"min\":null"));
            Assert.That(metricJson, Does.Contain("\"reason\":\"no_measurement_samples\""));
            Assert.That(metricJson, Does.Contain("\"raw_trace\":\"samples.ndjson\""));
        }

        [Test]
        public void MeasuredZeroRemainsMeasuredInsteadOfUnavailable()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("measured-zero");
            var summary = CaptureTelemetrySummary(
                "performance-measured-zero",
                resultsDirectory,
                128,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    Assert.That(PerformanceTelemetry.TryRecord(metric, 0d), Is.True);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.measured-zero");

            Assert.That(metricJson, Does.Contain("\"observation\":\"measured\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":1"));
            Assert.That(metricJson, Does.Contain("\"min\":0"));
            Assert.That(metricJson, Does.Contain("\"max\":0"));
            Assert.That(metricJson, Does.Contain("\"mean\":0"));
            Assert.That(metricJson, Does.Contain("\"measurement_coverage\":1"));
        }

        [Test]
        public void DeclaredUnavailableMetricDoesNotLookLikeMeasuredZero()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("declared-unavailable");
            var summary = CaptureTelemetrySummary(
                "performance-declared-unavailable",
                resultsDirectory,
                128,
                metric => PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement),
                PerformanceMetricObservation.Unavailable,
                "The controlled test intentionally has no source for this metric.");
            var metricJson = ExtractMetricJson(summary, "test.performance.declared-unavailable");

            Assert.That(metricJson, Does.Contain("\"observation\":\"unavailable\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":0"));
            Assert.That(metricJson, Does.Contain("\"min\":null"));
            Assert.That(metricJson, Does.Contain("\"mean\":null"));
            Assert.That(metricJson, Does.Contain("\"reason\":\"The controlled test intentionally has no source for this metric.\""));
            Assert.That(metricJson, Does.Contain("\"retention_policy\":\"not_collected\""));
        }

        [Test]
        public void PercentilesUseTheRetainedMeasurementSampleCount()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("small-percentiles");
            var summary = CaptureTelemetrySummary(
                "performance-small-percentiles",
                resultsDirectory,
                512,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 5; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.small-percentiles");

            Assert.That(metricJson, Does.Contain("\"sample_count\":5"));
            Assert.That(metricJson, Does.Contain("\"p50\":3"));
            Assert.That(metricJson, Does.Contain("\"p95\":5"));
            Assert.That(metricJson, Does.Contain("\"p99\":null"));
            Assert.That(metricJson, Does.Contain("\"reason\":\"insufficient_samples\""));
        }

        [Test]
        public void TelemetryLifecycleRejectsMeasurementBeforeWorkloadAndStopsCleanly()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("lifecycle-rejection");
            var metric = new PerformanceMetricDefinition(
                "test.performance.lifecycle-rejection",
                PerformanceMetricUnit.Count,
                PerformanceMetricSide.Server,
                "test",
                "lifecycle");
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext("performance-lifecycle-rejection", resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, 16);

                Assert.That(PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement), Is.False);
                Assert.That(PerformanceTelemetry.TryRecord(metric, 1d), Is.True);
                Assert.That(
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.CompleteMeasured,
                        "TEST_REJECTED",
                        "Measurement was rejected before workload application."),
                    Is.False);
                Assert.That(PerformanceTelemetry.IsEnabled, Is.False);
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        [Test]
        public void MetricRegistrationIsIdempotentButDimensionChangesFailExplicitly()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("metric-registration");
            var metric = new PerformanceMetricDefinition(
                "test.performance.metric-registration",
                PerformanceMetricUnit.Count,
                PerformanceMetricSide.Client,
                "test",
                "registration");
            var conflictingMetric = new PerformanceMetricDefinition(
                metric.MetricId,
                PerformanceMetricUnit.Bytes,
                metric.Side,
                metric.Subsystem,
                metric.Scope);
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext("performance-metric-registration", resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, 16);
                Assert.DoesNotThrow(() => PerformanceTelemetry.RegisterMetric(metric, 16));
                Assert.Throws<InvalidOperationException>(() => PerformanceTelemetry.RegisterMetric(conflictingMetric, 16));

                PerformanceTelemetry.MarkWorkloadApplied();
                Assert.That(PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement), Is.True);
                Assert.That(PerformanceTelemetry.TryRecord(metric, 0d), Is.True);
                Assert.That(
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.CompleteMeasured,
                        "TEST_COMPLETE",
                        "Metric registration contract test completed."),
                    Is.True);
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        [Test]
        public void ExportedTelemetryKeepsSchemaDimensionsAndRawSampleContract()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("export-schema");
            var metric = new PerformanceMetricDefinition(
                "test.performance.export-schema",
                PerformanceMetricUnit.Milliseconds,
                PerformanceMetricSide.Client,
                "test_exporter",
                "frame");
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext("performance-export-schema", resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, 16);
                PerformanceTelemetry.MarkWorkloadApplied();
                Assert.That(PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement), Is.True);
                Assert.That(PerformanceTelemetry.TryRecord(metric, 12.5d, 42, 0.5d), Is.True);
                Assert.That(
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.CompleteMeasured,
                        "TEST_COMPLETE",
                        "Telemetry exporter contract test completed."),
                    Is.True);

                var summary = File.ReadAllText(Path.Combine(resultsDirectory, "summary.json"));
                var sample = File.ReadAllText(Path.Combine(resultsDirectory, "samples.ndjson"));
                Assert.That(summary, Does.Contain("\"schema_version\":\"benchmark-summary/v1\""));
                Assert.That(summary, Does.Contain("\"run_id\":\"performance-export-schema\""));
                Assert.That(summary, Does.Contain("\"metric_id\":\"test.performance.export-schema\""));
                Assert.That(summary, Does.Contain("\"source\":\"client\""));
                Assert.That(summary, Does.Contain("\"unit\":\"ms\""));
                Assert.That(summary, Does.Contain("\"raw_files\":[\"samples.ndjson\"]"));
                Assert.That(sample, Does.Contain("\"schema_version\":\"benchmark-sample/v1\""));
                Assert.That(sample, Does.Contain("\"metric_id\":\"test.performance.export-schema\""));
                Assert.That(sample, Does.Contain("\"phase\":\"measurement\""));
                Assert.That(sample, Does.Contain("\"unit\":\"ms\""));
                Assert.That(sample, Does.Contain("\"frame_or_tick\":\"42\""));
                Assert.That(sample, Does.Contain("\"interval_seconds\":\"0.5\""));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        [Test]
        public void InvalidRunnerInputsAndPendingEvidenceFailClearly()
        {
            var invalidProfile = new Test_PerformanceHarnessOptions
            {
                ProfileID = "missing-profile",
            };
            Assert.That(invalidProfile.TryValidate(out var profileDiagnostic, out _, out _), Is.False);
            Assert.That(profileDiagnostic, Does.Contain("Only the tracked profile"));

            var missingWorld = new Test_PerformanceHarnessOptions
            {
                WorldFixture = (Test_WorldFixtureVariant)999,
            };
            Assert.That(missingWorld.TryValidate(out var worldDiagnostic, out _, out _), Is.False);
            Assert.That(worldDiagnostic, Does.Contain("World fixture variant"));

            var unsupportedMode = new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.StandaloneHeadlessServer,
            };
            Assert.That(unsupportedMode.TryValidate(out var modeDiagnostic, out _, out _), Is.False);
            Assert.That(modeDiagnostic, Does.Contain("blocked"));

            var incompleteState = new Test_PerformancePendingRunState();
            Assert.Throws<InvalidOperationException>(() => Test_PerformanceRun.Restore(incompleteState));
        }

        [Test]
        public void EvidenceFilesPreserveFixtureIdentityPopulationAndRunMetadata()
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions());
            try
            {
                var fixture = File.ReadAllText(run.FixturePath);
                var runJson = File.ReadAllText(run.RunPath);

                Assert.That(fixture, Does.Contain("\"schema_version\": \"benchmark-fixture/v1\""));
                Assert.That(fixture, Does.Contain("\"master_seed\": 424242"));
                Assert.That(fixture, Does.Contain("\"ecs_data_only_npcs\": 100"));
                Assert.That(fixture, Does.Contain("\"synthetic_clients\": 0"));
                Assert.That(fixture, Does.Contain("\"source_commit\":"));
                Assert.That(fixture, Does.Contain("\"source_sha256\":"));
                Assert.That(fixture, Does.Contain("\"fixture_scene_hash\":"));
                Assert.That(runJson, Does.Contain("\"schema_version\":\"benchmark-run/v1\""));
                Assert.That(runJson, Does.Contain("\"run_id\":\"" + run.RunID + "\""));
                Assert.That(runJson, Does.Contain("\"fixture\":{\"path\":\"fixture.json\",\"sha256\":\"" + run.FixtureHash + "\"}"));
                Assert.That(runJson, Does.Contain("\"standalone_qualified\":false"));
                Assert.That(runJson, Does.Contain("\"network_clients_supported\":false"));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        [Test]
        public void ProducedGraphicalEvidenceUsesCanonicalUnitsAndIsReadable()
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
            });
            var canonicalDefinitions = new[]
            {
                new PerformanceMetricDefinition("test.graphical.milliseconds", PerformanceMetricUnit.Milliseconds, PerformanceMetricSide.Client, "test", "interval"),
                new PerformanceMetricDefinition("test.graphical.bytes", PerformanceMetricUnit.Bytes, PerformanceMetricSide.Client, "test", "interval"),
                new PerformanceMetricDefinition("test.graphical.count", PerformanceMetricUnit.Count, PerformanceMetricSide.Client, "test", "interval"),
                new PerformanceMetricDefinition("test.graphical.ticks", PerformanceMetricUnit.Ticks, PerformanceMetricSide.Client, "test", "interval"),
                new PerformanceMetricDefinition("test.graphical.event_rate", PerformanceMetricUnit.EventsPerSecond, PerformanceMetricSide.Client, "test", "interval"),
                new PerformanceMetricDefinition("test.graphical.ratio", PerformanceMetricUnit.Ratio, PerformanceMetricSide.Client, "test", "interval"),
            };
            try
            {
                run.StartTelemetry();
                Test_PerformanceHarnessMetrics.RegisterGraphical();
                foreach (var definition in canonicalDefinitions)
                    PerformanceTelemetry.RegisterMetric(definition, 16);
                PerformanceTelemetry.MarkWorkloadApplied();
                Assert.That(PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement), Is.True);
                Assert.That(PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.GRAPHICAL_ROUTE_DISTANCE, 12.5d, 1), Is.True);
                foreach (var definition in canonicalDefinitions)
                    Assert.That(PerformanceTelemetry.TryRecord(definition, 1d, 1), Is.True);
                run.Complete(
                    PerformanceRunStatus.FailInstrumentation,
                    "TEST_CANONICAL_UNITS",
                    "The test intentionally finalized production graphical evidence.");

                var view = Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory);
                var summary = File.ReadAllText(run.SummaryPath);
                var runJson = File.ReadAllText(run.RunPath);
                var artifacts = File.ReadAllText(Path.Combine(run.ResultsDirectory, "artifacts.json"));

                Assert.That(view.IsVerified, Is.True, view.ErrorMessage);
                Assert.That(runJson, Does.Contain("\"branch\":"));
                Assert.That(runJson, Does.Contain("\"captured_utc\":"));
                Assert.That(artifacts, Does.Contain("\"status\":\"present\""));
                Assert.That(artifacts, Does.Contain("\"required\":true"));
                Assert.That(artifacts, Does.Contain("\"optional\":true"));
                Assert.That(artifacts, Does.Contain("\"sha256\":"));
                Assert.That(summary, Does.Contain("\"metric_id\":\"harness.graphical.route_distance\""));
                Assert.That(summary, Does.Contain("\"unit\":\"m\""));
                Assert.That(summary, Does.Contain("\"metric_id\":\"test.graphical.event_rate\""));
                Assert.That(summary, Does.Contain("\"unit\":\"events/s\""));
                Assert.That(summary, Does.Contain("\"unit\":\"ms\""));
                Assert.That(summary, Does.Contain("\"unit\":\"bytes\""));
                Assert.That(summary, Does.Contain("\"unit\":\"count\""));
                Assert.That(summary, Does.Contain("\"unit\":\"ticks\""));
                Assert.That(summary, Does.Contain("\"unit\":\"ratio\""));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        [Test]
        public void IncompleteAndTimeoutRunsAreMarkedInEvidence()
        {
            var incompleteSummary = CompleteCreatedRun(
                PerformanceRunStatus.CompleteMeasured,
                "TEST_COMPLETE",
                "The test intentionally omitted lifecycle completion.");
            Assert.That(incompleteSummary, Does.Contain("\"status\":\"FAIL-CORRECTNESS\""));
            Assert.That(incompleteSummary, Does.Contain("\"reason_code\":\"INCOMPLETE_LIFECYCLE\""));

            var timeoutSummary = CompleteCreatedRun(
                PerformanceRunStatus.FailTimeout,
                "PREFLIGHT_TIMEOUT",
                "The test intentionally exercised the timeout terminal state.");
            Assert.That(timeoutSummary, Does.Contain("\"status\":\"FAIL-TIMEOUT\""));
            Assert.That(timeoutSummary, Does.Contain("\"reason_code\":\"PREFLIGHT_TIMEOUT\""));
        }

        [Test]
        public void Stage14PreMeasurementFailurePreservesItsTerminalReasonWithoutTelemetry()
        {
            var summary = CompleteStage14CreatedRun(
                PerformanceRunStatus.FailProvision,
                "GROUND_PREFLIGHT_FAILED",
                "The test intentionally exercised a pre-measurement provisioning failure.");

            Assert.That(summary, Does.Contain("\"status\":\"FAIL-PROVISION\""));
            Assert.That(summary, Does.Contain("\"reason_code\":\"GROUND_PREFLIGHT_FAILED\""));
            Assert.That(summary, Does.Not.Contain("MISSING_REQUIRED_TELEMETRY"));
        }

        [Test]
        public void OtherwiseCompleteStage14RunRejectsMissingRequiredTelemetry()
        {
            var summary = CompleteStage14CreatedRun(
                PerformanceRunStatus.CompleteMeasured,
                "MEASUREMENT_COMPLETE",
                "The test intentionally omitted required Stage 1.4 telemetry.");

            Assert.That(summary, Does.Contain("\"status\":\"FAIL-INSTRUMENTATION\""));
            Assert.That(summary, Does.Contain("\"reason_code\":\"MISSING_REQUIRED_TELEMETRY\""));
        }

        [Test]
        public void TelemetryEnabledAndDisabledFakeSchedulesHaveEquivalentOutcomes()
        {
            var disabledOutcome = RunFakeSchedule(false);
            var enabledOutcome = RunFakeSchedule(true);

            Assert.That(enabledOutcome.ActorCount, Is.EqualTo(disabledOutcome.ActorCount));
            Assert.That(enabledOutcome.CastCount, Is.EqualTo(disabledOutcome.CastCount));
            Assert.That(enabledOutcome.TimerTicks, Is.EqualTo(disabledOutcome.TimerTicks));
            Assert.That(enabledOutcome.VisibleCount, Is.EqualTo(disabledOutcome.VisibleCount));
        }

        [Test]
        public void FakeMeasurementsCanBeConvertedAndComparedWithoutHardware()
        {
            var baseline = ReadFakeMeasurement(
                CaptureTelemetrySummary(
                    "performance-fake-baseline",
                    CreateTelemetryTestDirectory("fake-baseline"),
                    512,
                    metric =>
                    {
                        PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                        for (var value = 1; value <= 100; value++)
                            PerformanceTelemetry.TryRecord(metric, value);
                    }),
                "test.performance.fake-baseline");
            var candidate = ReadFakeMeasurement(
                CaptureTelemetrySummary(
                    "performance-fake-candidate",
                    CreateTelemetryTestDirectory("fake-candidate"),
                    512,
                    metric =>
                    {
                        PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                        for (var value = 1; value <= 94; value++)
                            PerformanceTelemetry.TryRecord(metric, value);
                        for (var value = 0; value < 6; value++)
                            PerformanceTelemetry.TryRecord(metric, 105d);
                    }),
                "test.performance.fake-candidate");

            Assert.That(candidate.Unit, Is.EqualTo(baseline.Unit));
            Assert.That(candidate.SampleCount, Is.EqualTo(baseline.SampleCount));
            Assert.That(candidate.P95, Is.GreaterThan(baseline.P95));
        }

        [Test]
        public void RepeatedTelemetrySessionsDoNotReusePreviousMeasurementSamples()
        {
            var firstDirectory = CreateTelemetryTestDirectory("repeated-first");
            var firstSummary = CaptureTelemetrySummary(
                "performance-repeated-first",
                firstDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 100; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });

            var secondDirectory = CreateTelemetryTestDirectory("repeated-second");
            var secondSummary = CaptureTelemetrySummary(
                "performance-repeated-second",
                secondDirectory,
                4096,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    PerformanceTelemetry.TryRecord(metric, 7);
                });

            Assert.That(ExtractMetricJson(firstSummary, "test.performance.repeated-first"), Does.Contain("\"sample_count\":100"));
            Assert.That(ExtractMetricJson(secondSummary, "test.performance.repeated-second"), Does.Contain("\"sample_count\":1"));
        }

        [Test]
        public void MeasurementRetentionAndRawTraceBudgetsAreExplicitlyQualified()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("retention-budget");
            var summary = CaptureTelemetrySummary(
                "performance-retention-budget",
                resultsDirectory,
                512,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 1000; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.retention-budget");

            Assert.That(summary, Does.Contain("\"status\":\"FAIL-INSTRUMENTATION\""));
            Assert.That(metricJson, Does.Contain("\"observation\":\"unavailable\""));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":1000"));
            Assert.That(metricJson, Does.Contain("\"sample_count\":0"));
            Assert.That(metricJson, Does.Contain("\"retained_sample_count\":384"));
            Assert.That(metricJson, Does.Contain("\"dropped_sample_count\":616"));
            Assert.That(metricJson, Does.Contain("\"raw_trace_sample_count\":256"));
            Assert.That(ExtractJsonNumber(metricJson, "measurement_coverage"), Is.EqualTo(0.384d).Within(0.000001d));
            Assert.That(ExtractJsonNumber(metricJson, "valid_sample_coverage"), Is.EqualTo(1d));
            Assert.That(metricJson, Does.Contain("\"rejected_sample_count\":0"));
            Assert.That(ExtractJsonNumber(metricJson, "capacity_overflow_count"), Is.EqualTo(616d));
            Assert.That(ExtractJsonNumber(metricJson, "reservoir_replacement_count"), Is.GreaterThan(0d).And.LessThan(616d));
            Assert.That(metricJson, Does.Contain("\"retention_policy\":\"phase_reserved_reservoir\""));
        }

        [Test]
        public void SufficientMeasurementCapacityRetainsAllSamplesAndStillBoundsRawTrace()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("retention-qualified");
            var summary = CaptureTelemetrySummary(
                "performance-retention-qualified",
                resultsDirectory,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    for (var value = 1; value <= 1000; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.retention-qualified");

            Assert.That(summary, Does.Contain("\"status\":\"COMPLETE-MEASURED\""));
            Assert.That(metricJson, Does.Contain("\"observation\":\"measured\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":1000"));
            Assert.That(metricJson, Does.Contain("\"retained_sample_count\":1000"));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":1000"));
            Assert.That(metricJson, Does.Contain("\"capacity_overflow_count\":0"));
            Assert.That(metricJson, Does.Contain("\"measurement_coverage\":1"));
            Assert.That(metricJson, Does.Contain("\"raw_trace_sample_count\":256"));
            Assert.That(metricJson, Does.Contain("\"max\":1000"));
        }

        [Test]
        public void InvalidMeasurementInputsAreCountedSeparatelyFromRetention()
        {
            var resultsDirectory = CreateTelemetryTestDirectory("invalid-measurement-input");
            var summary = CaptureTelemetrySummary(
                "performance-invalid-measurement-input",
                resultsDirectory,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC,
                metric =>
                {
                    PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                    Assert.That(PerformanceTelemetry.TryRecord(metric, double.NaN), Is.False);
                    for (var value = 1; value <= 99; value++)
                        PerformanceTelemetry.TryRecord(metric, value);
                });
            var metricJson = ExtractMetricJson(summary, "test.performance.invalid-measurement-input");

            Assert.That(summary, Does.Contain("\"status\":\"COMPLETE-MEASURED\""));
            Assert.That(metricJson, Does.Contain("\"sample_count\":99"));
            Assert.That(metricJson, Does.Contain("\"retained_sample_count\":99"));
            Assert.That(metricJson, Does.Contain("\"attempted_sample_count\":100"));
            Assert.That(metricJson, Does.Contain("\"rejected_sample_count\":1"));
            Assert.That(metricJson, Does.Contain("\"capacity_overflow_count\":0"));
            Assert.That(ExtractJsonNumber(metricJson, "valid_sample_coverage"), Is.EqualTo(0.99d).Within(0.000001d));
            Assert.That(ExtractJsonNumber(metricJson, "measurement_coverage"), Is.EqualTo(0.99d).Within(0.000001d));
        }

        [Test]
        public void FullStreamExtremaRemainVisibleWhenReservoirDoesNotRetainTheExtreme()
        {
            var bufferType = typeof(PerformanceTelemetry).Assembly.GetType("Game.Shared.Performance.PerformanceSampleBuffer");
            Assert.That(bufferType, Is.Not.Null);
            if (bufferType == null)
                return;

            var addMethod = bufferType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
            var snapshotMethod = bufferType.GetMethod("CreateSnapshot", BindingFlags.Instance | BindingFlags.Public);
            var randomStateField = bufferType.GetField("_measurementRandomState", BindingFlags.Instance | BindingFlags.NonPublic);
            var valuesField = bufferType.GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
            var capacityField = bufferType.GetField("_measurementCapacity", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(addMethod, Is.Not.Null);
            Assert.That(snapshotMethod, Is.Not.Null);
            Assert.That(randomStateField, Is.Not.Null);
            Assert.That(valuesField, Is.Not.Null);
            Assert.That(capacityField, Is.Not.Null);
            if (addMethod == null || snapshotMethod == null || randomStateField == null || valuesField == null || capacityField == null)
                return;

            const double extreme = 1000000d;
            object selectedBuffer = null;
            object selectedSnapshot = null;
            ulong selectedSeed = 0;
            var measurementPhase = PerformanceMeasurementPhase.Measurement;
            for (ulong seed = 1; seed <= 10000 && selectedBuffer == null; seed++)
            {
                var buffer = Activator.CreateInstance(bufferType, new object[] { 228 });
                for (var value = 1; value <= 100; value++)
                    addMethod.Invoke(buffer, new object[] { (double)value, value, value, -1d, measurementPhase });
                randomStateField.SetValue(buffer, seed);
                addMethod.Invoke(buffer, new object[] { extreme, 101L, 101L, -1d, measurementPhase });

                var values = (double[])valuesField.GetValue(buffer);
                var capacity = (int)capacityField.GetValue(buffer);
                var retainedExtreme = false;
                for (var index = 0; index < capacity; index++)
                    retainedExtreme |= values[index] == extreme;
                if (!retainedExtreme)
                {
                    selectedBuffer = buffer;
                    selectedSnapshot = snapshotMethod.Invoke(buffer, null);
                    selectedSeed = seed;
                }
            }

            Assert.That(selectedBuffer, Is.Not.Null, "The bounded reservoir did not expose a non-retained sample for the tested seeds.");
            Assert.That(selectedSnapshot, Is.Not.Null);
            if (selectedSnapshot == null)
                return;
            var maximum = Convert.ToDouble(selectedSnapshot.GetType().GetProperty("Maximum").GetValue(selectedSnapshot));
            var p99Value = selectedSnapshot.GetType().GetProperty("P99").GetValue(selectedSnapshot);
            Assert.That(maximum, Is.EqualTo(extreme));
            Assert.That(p99Value, Is.Not.Null);
            if (p99Value == null)
                return;
            Assert.That(Convert.ToDouble(p99Value), Is.LessThan(extreme));

            var resultsDirectory = CreateTelemetryTestDirectory("full-stream-extrema-export");
            var metric = new PerformanceMetricDefinition(
                "test.performance.full-stream-extrema-export",
                PerformanceMetricUnit.Milliseconds,
                PerformanceMetricSide.Server,
                "test",
                "test_run");
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext("performance-full-stream-extrema-export", resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, 228);
                PerformanceTelemetry.MarkWorkloadApplied();
                PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement);
                for (var value = 1; value <= 100; value++)
                    PerformanceTelemetry.TryRecord(metric, value);

                var sessionField = typeof(PerformanceTelemetry).GetField("_session", BindingFlags.Static | BindingFlags.NonPublic);
                var session = sessionField.GetValue(null);
                var registrationsField = session.GetType().GetField("_registrations", BindingFlags.Instance | BindingFlags.NonPublic);
                var registrations = (System.Collections.IDictionary)registrationsField.GetValue(session);
                var registration = registrations[metric.MetricId];
                var telemetryBuffer = registration.GetType().GetProperty("Buffer").GetValue(registration);
                randomStateField.SetValue(telemetryBuffer, selectedSeed);
                PerformanceTelemetry.TryRecord(metric, extreme);
                Assert.That(PerformanceTelemetry.StopSession(PerformanceRunStatus.CompleteMeasured, "TEST_COMPLETE", "Extrema export test completed."), Is.True);

                var exported = ExtractMetricJson(File.ReadAllText(Path.Combine(resultsDirectory, "summary.json")), metric.MetricId);
                Assert.That(exported, Does.Contain("\"observation\":\"measured\""));
                Assert.That(exported, Does.Contain("\"max\":1000000"));
                Assert.That(exported, Does.Contain("\"p99\":99"));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                    PerformanceTelemetry.StopSession(PerformanceRunStatus.FailInstrumentation, "TEST_CLEANUP", "Extrema export test cleanup.");
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
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
        public void FixtureScriptPrerequisitesAreBlockedInsteadOfRunnerErrors()
        {
            var classifyFailure = typeof(Test_PerformanceHarnessBehaviour).GetMethod(
                "ClassifyFailure",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(classifyFailure, Is.Not.Null);
            if (classifyFailure == null)
                return;

            var classification = classifyFailure.Invoke(
                null,
                new object[] { new Test_PerformancePrerequisiteException("fixture script is unavailable") });
            var classificationType = classification.GetType();

            Assert.That(classificationType.GetProperty("ReasonCode").GetValue(classification), Is.EqualTo("PREREQUISITE_BLOCKED"));
            Assert.That(classificationType.GetProperty("Status").GetValue(classification), Is.EqualTo(PerformanceRunStatus.BlockedPrerequisite));
        }

        [UnityTest]
        [Explicit("Requires the real Bootstrapper to MainMenu Play Mode transition; use the manual Issue #52 closure gate for the complete graphical workload.")]
        public IEnumerator RuntimeRunnerRootSurvivesBootstrapperTransition()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                Assert.Ignore("The Bootstrapper transition test must start from Edit Mode.");

            yield return new EnterPlayMode();

            GameObject runnerObject = null;
            var completed = false;
            try
            {
                runnerObject = Test_PerformanceHarnessController.CreateRuntimeRunnerObject();
                runnerObject.AddComponent<Test_PerformanceHarnessBehaviour>();
                var loadOperation = SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
                Assert.That(loadOperation, Is.Not.Null, "Unity did not create the Bootstrapper load operation.");
                if (loadOperation == null)
                    yield break;

                while (!loadOperation.isDone)
                    yield return null;

                var deadline = Time.realtimeSinceStartup + Test_PerformanceRun.PREFLIGHT_TIMEOUT_SECONDS;
                while (SceneManager.GetSceneByName("Bootstrapper").isLoaded
                    && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That(SceneManager.GetSceneByName("MainMenu").isLoaded, Is.True, "The ordinary client did not load MainMenu during Bootstrapper transition.");
                Assert.That(SceneManager.GetSceneByName("Bootstrapper").isLoaded, Is.False, "The ordinary client did not unload Bootstrapper during initialization.");
                Assert.That(runnerObject, Is.Not.Null, "The test runner root was destroyed during Bootstrapper unload.");
                completed = true;
            }
            finally
            {
                if (runnerObject != null)
                    UnityEngine.Object.Destroy(runnerObject);
                if (!completed && EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = false;
            }

            if (completed)
                yield return new ExitPlayMode();
        }

        [Test]
        public void GraphicalScenarioPlansAreDeterministicAndSeeded()
        {
            var first = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Forest,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);
            var second = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Forest,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(first.IsReady, Is.True);
            Assert.That(first.PlanHash, Is.EqualTo(second.PlanHash));
            Assert.That(first.Waypoints, Has.Length.EqualTo(4));
            Assert.That(first.RequestedObjectCount, Is.GreaterThan(200));
            Assert.That(first.RequiresWorldStreaming, Is.False);
            Test_GraphicalFixtureLayout.ValidatePlan(first);
        }

        [Test]
        public void GraphicalScenarioSeedAndActorDensityDoNotDowngradeSilently()
        {
            var changedSeed = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Outdoor,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED + 1);
            var actors = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.Outdoor,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.VisibleTen,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(changedSeed.Status, Is.EqualTo("Blocked"));
            Assert.That(changedSeed.BlockedReason, Does.Contain("seed"));
            Assert.That(actors.RequestedActorCount, Is.EqualTo(10));
            Assert.That(actors.RequiresRealClientSession, Is.True);
        }

        [Test]
        public void AdjacentGraphicalScenarioRequiresNormalStreamingPath()
        {
            var plan = Test_GraphicalFixtureLayout.BuildPlan(
                Test_GraphicalScenario.AdjacentTiles,
                Test_WorldFixtureDensityTier.D1_Default,
                Test_GraphicalActorDensity.None,
                Test_GraphicalFixtureLayout.DEFAULT_SEED);

            Assert.That(plan.RequiresWorldStreaming, Is.True);
            Assert.That(plan.FixtureVariants, Has.Length.EqualTo(2));
            Assert.That(plan.Waypoints[1].WorldTileX, Is.EqualTo(0));
            Assert.That(plan.Waypoints[2].WorldTileX, Is.EqualTo(1));
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

        [Test]
        public void RestoredFinalizedGraphicalRunPreservesWorkloadEvidence()
        {
            var originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            Test_PerformanceRun run = null;
            GameObject cameraObject = null;
            Scene fixtureScene = default;
            try
            {
                run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions
                {
                    WorkloadMode = Test_PerformanceWorkloadMode.GraphicalClient,
                    GraphicalScenario = Test_GraphicalScenario.Outdoor,
                });
                fixtureScene = EditorSceneManager.OpenScene(run.GraphicalPlan.FixtureScenePaths[0], OpenSceneMode.Additive);
                run.SetLoadedWorld(fixtureScene);
                var graphicalRuntime = run.CreateGraphicalRuntime();
                cameraObject = new GameObject("PerformanceHarnessRegressionCamera");
                graphicalRuntime.AttachCamera(cameraObject.AddComponent<Camera>());
                graphicalRuntime.ValidateLoadedScene(fixtureScene);
                graphicalRuntime.Advance(1f, 1f);
                var expectedObjectCount = graphicalRuntime.ActualObjectCount;
                var expectedActorCount = graphicalRuntime.ActualActorCount;
                var expectedRouteCoverage = graphicalRuntime.RouteWaypointsReached + "/" + run.GraphicalPlan.Waypoints.Length;
                graphicalRuntime.Dispose();
                run.MarkCleanupComplete();

                run.StartTelemetry();
                var testMetric = new PerformanceMetricDefinition(
                    "test.performance.finalized-reload",
                    PerformanceMetricUnit.Milliseconds,
                    PerformanceMetricSide.Shared,
                    "test_finalized_reload",
                    "sample");
                PerformanceTelemetry.RegisterMetric(testMetric, 16);
                run.BeginPhase(PerformanceMeasurementPhase.Ramp);
                run.MarkWorkloadApplied();
                run.MarkPhaseComplete(PerformanceMeasurementPhase.Ramp);
                run.BeginPhase(PerformanceMeasurementPhase.Warmup);
                run.MarkPhaseComplete(PerformanceMeasurementPhase.Warmup);
                run.BeginPhase(PerformanceMeasurementPhase.Measurement);
                Assert.That(PerformanceTelemetry.TryRecord(testMetric, 4d, 1, 0.1d), Is.True);
                run.RecordMeasurementFrame();
                run.RecordMeasurementSample();
                run.ObserveEntityCount(expectedObjectCount);
                run.MarkPhaseComplete(PerformanceMeasurementPhase.Measurement);
                run.BeginPhase(PerformanceMeasurementPhase.Recovery);
                run.MarkPhaseComplete(PerformanceMeasurementPhase.Recovery);
                run.MarkWorkloadFinalized();
                run.Complete(
                    PerformanceRunStatus.CompleteMeasured,
                    "MEASUREMENT_COMPLETE",
                    "The graphical reload regression fixture completed.");

                var originalSummary = File.ReadAllText(run.SummaryPath);
                var originalRawSamples = File.ReadAllBytes(Path.Combine(run.ResultsDirectory, "samples.ndjson"));
                var state = run.CreatePendingRunState(Array.Empty<SceneSetup>(), "finalized");
                var restored = Test_PerformanceRun.Restore(state);
                Assert.That(restored.GraphicalRuntime, Is.Null);

                restored.CompleteEditorLifecycle();

                var restoredSummary = File.ReadAllText(run.SummaryPath);
                var restoredReport = File.ReadAllText(run.ReportPath);
                Assert.That(restoredReport, Does.Contain($"- Achieved graphical objects: `{expectedObjectCount}`"));
                Assert.That(restoredReport, Does.Contain($"- Achieved graphical actors: `{expectedActorCount}`"));
                Assert.That(restoredReport, Does.Contain($"- Route coverage: `{expectedRouteCoverage}` waypoints"));
                Assert.That(restoredReport, Does.Not.Contain("Achieved graphical objects: `unverified`"));
                Assert.That(restoredReport, Does.Not.Contain("Achieved graphical actors: `unverified`"));
                Assert.That(restoredReport, Does.Not.Contain("Route coverage: `unverified`"));
                Assert.That(ExtractJsonValue(originalSummary, "status"), Is.EqualTo(ExtractJsonValue(restoredSummary, "status")));
                Assert.That(ExtractJsonValue(originalSummary, "achieved"), Is.EqualTo(ExtractJsonValue(restoredSummary, "achieved")));
                Assert.That(ExtractJsonValue(originalSummary, "stop"), Is.EqualTo(ExtractJsonValue(restoredSummary, "stop")));
                Assert.That(ExtractJsonValue(originalSummary, "metrics"), Is.EqualTo(ExtractJsonValue(restoredSummary, "metrics")));
                Assert.That(ExtractJsonValue(originalSummary, "raw_files"), Is.EqualTo(ExtractJsonValue(restoredSummary, "raw_files")));
                Assert.That(File.ReadAllBytes(Path.Combine(run.ResultsDirectory, "samples.ndjson")), Is.EqualTo(originalRawSamples));
                Assert.That(Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory).IsVerified, Is.True);

                var idempotentSummary = File.ReadAllText(run.SummaryPath);
                var idempotentReport = File.ReadAllText(run.ReportPath);
                var idempotentArtifacts = File.ReadAllText(Path.Combine(run.ResultsDirectory, "artifacts.json"));
                restored.CompleteEditorLifecycle();
                Assert.That(File.ReadAllText(run.SummaryPath), Is.EqualTo(idempotentSummary));
                Assert.That(File.ReadAllText(run.ReportPath), Is.EqualTo(idempotentReport));
                Assert.That(File.ReadAllText(Path.Combine(run.ResultsDirectory, "artifacts.json")), Is.EqualTo(idempotentArtifacts));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Finalized reload regression cleanup.");
                }
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                if (fixtureScene.IsValid() && fixtureScene.isLoaded)
                    EditorSceneManager.CloseScene(fixtureScene, true);
                if (run != null && Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
                if (originalSceneSetup != null && originalSceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSceneSetup);
            }
        }

        [Test]
        public void RestoredFinalizedLifecycleKeepsReloadTimingUnknownAndReportsSceneFailure()
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions());
            try
            {
                run.MarkWorkloadFinalized();
                run.Complete(
                    PerformanceRunStatus.FailCorrectness,
                    "TEST_FINALIZED",
                    "Finalized lifecycle regression fixture.");
                var restored = Test_PerformanceRun.Restore(run.CreatePendingRunState(Array.Empty<SceneSetup>(), "finalized"));
                restored.MarkPlayModeExited();
                restored.MarkSceneRestoreFailed("test scene restoration failure");
                restored.CompleteEditorLifecycle();

                var orchestration = ExtractJsonValue(File.ReadAllText(run.SummaryPath), "orchestration");
                Assert.That(orchestration, Does.Contain("\"play_mode_exited_utc\":\""));
                Assert.That(orchestration, Does.Contain("\"scene_restored_utc\":\"\""));
                Assert.That(orchestration, Does.Contain("\"workload_finalized_to_play_mode_exited_seconds\":null"));
                Assert.That(orchestration, Does.Contain("\"play_mode_exit_to_scene_restored_seconds\":null"));
                Assert.That(orchestration, Does.Contain("\"scene_restoration_status\":\"failed\""));
                Assert.That(orchestration, Does.Contain("test scene restoration failure"));
                Assert.That(Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory).IsVerified, Is.True);
            }
            finally
            {
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        [Test]
        public void NoReloadFinalizedLifecycleKeepsMeasuredEvidenceAndKnownTimings()
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions());
            try
            {
                run.MarkQueueDispatch(1, DateTime.UtcNow);
                run.MarkPlayModeEntered();
                run.MarkWorkloadFinalized();
                run.Complete(
                    PerformanceRunStatus.FailCorrectness,
                    "TEST_FINALIZED",
                    "No-reload lifecycle regression fixture.");
                var originalSummary = File.ReadAllText(run.SummaryPath);
                run.MarkPlayModeExited();
                run.MarkSceneRestored();
                run.CompleteEditorLifecycle();

                var summary = File.ReadAllText(run.SummaryPath);
                var orchestration = ExtractJsonValue(summary, "orchestration");
                Assert.That(orchestration, Does.Contain("\"queue_dispatch_to_play_mode_entered_seconds\":").And.Not.Contain("\"queue_dispatch_to_play_mode_entered_seconds\":null"));
                Assert.That(orchestration, Does.Contain("\"workload_finalized_to_play_mode_exited_seconds\":").And.Not.Contain("\"workload_finalized_to_play_mode_exited_seconds\":null"));
                Assert.That(orchestration, Does.Contain("\"play_mode_exit_to_scene_restored_seconds\":").And.Not.Contain("\"play_mode_exit_to_scene_restored_seconds\":null"));
                Assert.That(ExtractJsonValue(originalSummary, "status"), Is.EqualTo(ExtractJsonValue(summary, "status")));
                Assert.That(ExtractJsonValue(originalSummary, "stop"), Is.EqualTo(ExtractJsonValue(summary, "stop")));
                Assert.That(ExtractJsonValue(originalSummary, "metrics"), Is.EqualTo(ExtractJsonValue(summary, "metrics")));
                Assert.That(File.ReadAllText(run.ReportPath), Does.Contain("- Requested ECS entities:"));
                Assert.That(Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory).IsVerified, Is.True);
            }
            finally
            {
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        [Test]
        public void RestoredFinalizedServerRunPreservesPersistedActivityEvidence()
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions());
            try
            {
                run.MarkWorkloadFinalized();
                run.Complete(
                    PerformanceRunStatus.FailCorrectness,
                    "TEST_FINALIZED",
                    "Server reload regression fixture.");
                var report = File.ReadAllText(run.ReportPath)
                    .Replace("- Ramp-completed ECS entities: `0`", "- Ramp-completed ECS entities: `100`", StringComparison.Ordinal)
                    .Replace("- Measurement-start ECS entities: `0`", "- Measurement-start ECS entities: `100`", StringComparison.Ordinal)
                    .Replace("- Measurement-end ECS entities: `0`", "- Measurement-end ECS entities: `100`", StringComparison.Ordinal)
                    .Replace("- Peak ECS entities observed: `0`", "- Peak ECS entities observed: `100`", StringComparison.Ordinal)
                    .Replace(
                        "- Measured activity snapshot: `unverified` (measurement did not start or no pre-cleanup snapshot was captured)",
                        "- Measured activity snapshot (`measurement`, before teardown): idle=`7`, patrolling=`11`, combat=`82`, wake surge=`3`",
                        StringComparison.Ordinal);
                File.WriteAllText(run.ReportPath, report);

                var restored = Test_PerformanceRun.Restore(run.CreatePendingRunState(Array.Empty<SceneSetup>(), "finalized"));
                restored.CompleteEditorLifecycle();

                var restoredReport = File.ReadAllText(run.ReportPath);
                Assert.That(restoredReport, Does.Contain("- Ramp-completed ECS entities: `100`"));
                Assert.That(restoredReport, Does.Contain("- Measurement-start ECS entities: `100`"));
                Assert.That(restoredReport, Does.Contain("- Measurement-end ECS entities: `100`"));
                Assert.That(restoredReport, Does.Contain("- Peak ECS entities observed: `100`"));
                Assert.That(restoredReport, Does.Contain("idle=`7`, patrolling=`11`, combat=`82`, wake surge=`3`"));
                Assert.That(Test_PerformanceEvidence.ReadFinalizedResult(run.ResultsDirectory).IsVerified, Is.True);
            }
            finally
            {
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        private static string CompleteCreatedRun(
            PerformanceRunStatus status,
            string reasonCode,
            string message)
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions());
            try
            {
                run.Complete(status, reasonCode, message);
                return File.ReadAllText(run.SummaryPath);
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        private static string CompleteStage14CreatedRun(
            PerformanceRunStatus status,
            string reasonCode,
            string message)
        {
            var run = Test_PerformanceRun.Create(new Test_PerformanceHarnessOptions
            {
                WorkloadMode = Test_PerformanceWorkloadMode.FullNetworkedActors,
            });
            try
            {
                run.Complete(status, reasonCode, message);
                return File.ReadAllText(run.SummaryPath);
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(run.ResultsDirectory))
                    Directory.Delete(run.ResultsDirectory, true);
            }
        }

        private static Test_FakeScheduleOutcome RunFakeSchedule(bool telemetryEnabled)
        {
            var resultsDirectory = CreateTelemetryTestDirectory(telemetryEnabled ? "fake-enabled" : "fake-disabled");
            var metric = new PerformanceMetricDefinition(
                "test.performance.fake-schedule",
                PerformanceMetricUnit.Count,
                PerformanceMetricSide.Shared,
                "test_fake_schedule",
                "step");
            try
            {
                if (telemetryEnabled)
                {
                    PerformanceTelemetry.StartSession(
                        new PerformanceSessionContext(
                            telemetryEnabled ? "performance-fake-schedule-enabled" : "performance-fake-schedule-disabled",
                            resultsDirectory)
                        {
                            HostId = "test-host",
                            ProcessId = "test-process",
                        });
                    PerformanceTelemetry.RegisterMetric(metric, 16);
                    PerformanceTelemetry.MarkWorkloadApplied();
                    Assert.That(PerformanceTelemetry.BeginPhase(PerformanceMeasurementPhase.Measurement), Is.True);
                }

                var outcome = new Test_FakeScheduleOutcome();
                for (var step = 0; step < 4; step++)
                {
                    outcome.ActorCount += step == 0 ? 4 : 0;
                    outcome.CastCount += step == 1 || step == 3 ? 2 : 0;
                    outcome.TimerTicks += 1;
                    outcome.VisibleCount = outcome.ActorCount;
                    if (telemetryEnabled)
                        Assert.That(PerformanceTelemetry.TryRecord(metric, outcome.ActorCount, step, 0.25d), Is.True);
                }

                if (telemetryEnabled)
                {
                    Assert.That(
                        PerformanceTelemetry.StopSession(
                            PerformanceRunStatus.CompleteMeasured,
                            "TEST_COMPLETE",
                            "Fake schedule completed."),
                        Is.True);
                }

                return outcome;
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        private static Test_FakeMeasurement ReadFakeMeasurement(string summary, string metricID)
        {
            var metricJson = ExtractMetricJson(summary, metricID);
            return new Test_FakeMeasurement
            {
                Unit = ExtractJsonString(metricJson, "unit"),
                SampleCount = (int)ExtractJsonNumber(metricJson, "sample_count"),
                P95 = ExtractJsonNumber(metricJson, "p95"),
            };
        }

        private static string ExtractJsonString(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\":\"";
            var start = json.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "JSON property was not present: " + propertyName);
            start += marker.Length;
            var end = json.IndexOf('"', start);
            Assert.That(end, Is.GreaterThan(start), "JSON string property was not terminated: " + propertyName);
            return json.Substring(start, end - start);
        }

        private static double ExtractJsonNumber(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\":";
            var start = json.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "JSON property was not present: " + propertyName);
            start += marker.Length;
            var end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}')
                end++;
            return double.Parse(json.Substring(start, end - start), CultureInfo.InvariantCulture);
        }

        private sealed class Test_FakeScheduleOutcome
        {
            public int ActorCount;
            public int CastCount;
            public int TimerTicks;
            public int VisibleCount;
        }

        private sealed class Test_FakeMeasurement
        {
            public string Unit;
            public int SampleCount;
            public double P95;
        }

        private static string CaptureTelemetrySummary(
            string runID,
            string resultsDirectory,
            int capacity,
            Action<PerformanceMetricDefinition> recordSamples,
            PerformanceMetricObservation observation = PerformanceMetricObservation.Measured,
            string reason = null)
        {
            var metricID = "test.performance." + runID.Substring("performance-".Length);
            var metric = new PerformanceMetricDefinition(
                metricID,
                PerformanceMetricUnit.Milliseconds,
                PerformanceMetricSide.Server,
                "test",
                "test_run");
            try
            {
                PerformanceTelemetry.StartSession(
                    new PerformanceSessionContext(runID, resultsDirectory)
                    {
                        HostId = "test-host",
                        ProcessId = "test-process",
                    });
                PerformanceTelemetry.RegisterMetric(metric, capacity, observation, reason);
                PerformanceTelemetry.MarkWorkloadApplied();
                recordSamples(metric);
                Assert.That(
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.CompleteMeasured,
                        "TEST_COMPLETE",
                        "Telemetry contract test completed."),
                    Is.True);
                return File.ReadAllText(Path.Combine(resultsDirectory, "summary.json"));
            }
            finally
            {
                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopSession(
                        PerformanceRunStatus.FailInstrumentation,
                        "TEST_CLEANUP",
                        "Telemetry contract test cleanup.");
                }
                if (Directory.Exists(resultsDirectory))
                    Directory.Delete(resultsDirectory, true);
            }
        }

        private static string CreateTelemetryTestDirectory(string name)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "NinthAgePerformanceTests",
                name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string ExtractMetricJson(string summary, string metricID)
        {
            var marker = "\"metric_id\":\"" + metricID + "\"";
            var start = summary.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "Metric was not present in summary.json.");
            var end = summary.IndexOf('}', start);
            Assert.That(end, Is.GreaterThan(start), "Metric JSON was not terminated.");
            return summary.Substring(start, end - start + 1);
        }

        private static string ExtractJsonValue(string json, string propertyName)
        {
            var marker = "\"" + propertyName + "\":";
            var propertyIndex = json.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(propertyIndex, Is.GreaterThanOrEqualTo(0), "JSON property was not present: " + propertyName);
            var start = propertyIndex + marker.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            Assert.That(start, Is.LessThan(json.Length), "JSON property had no value: " + propertyName);
            if (json[start] == '"')
            {
                var end = start + 1;
                var escaped = false;
                for (; end < json.Length; end++)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (json[end] == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (json[end] == '"')
                        return json.Substring(start, end - start + 1);
                }
            }

            Assert.That(json[start] == '{' || json[start] == '[', Is.True, "JSON property was not an object or array: " + propertyName);
            var open = json[start];
            var close = open == '{' ? '}' : ']';
            var depth = 0;
            var inString = false;
            var escapedValue = false;
            for (var index = start; index < json.Length; index++)
            {
                var character = json[index];
                if (inString)
                {
                    if (escapedValue)
                        escapedValue = false;
                    else if (character == '\\')
                        escapedValue = true;
                    else if (character == '"')
                        inString = false;
                    continue;
                }
                if (character == '"')
                    inString = true;
                else if (character == open)
                    depth++;
                else if (character == close && --depth == 0)
                    return json.Substring(start, index - start + 1);
            }

            Assert.Fail("JSON property was not terminated: " + propertyName);
            return null;
        }
    }
}
