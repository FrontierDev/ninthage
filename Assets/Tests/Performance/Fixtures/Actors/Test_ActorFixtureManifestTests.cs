using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Performance.Fixtures.Actors
{
    /// <summary>Regression coverage for deterministic actor manifests and explicit prerequisites.</summary>
    public sealed class Test_ActorFixtureManifestTests
    {
        [Test]
        public void SameSeedProducesIdenticalCountsPlacementsAndActivitySchedule()
        {
            var first = Test_ActorFixtureLayout.BuildCatalog(Test_ActorFixtureLayout.DEFAULT_SEED);
            var second = Test_ActorFixtureLayout.BuildCatalog(Test_ActorFixtureLayout.DEFAULT_SEED);
            var firstScenario = Test_ActorFixtureLayout.BuildScenarioCatalog(Test_ActorFixtureLayout.DEFAULT_SEED);
            var secondScenario = Test_ActorFixtureLayout.BuildScenarioCatalog(Test_ActorFixtureLayout.DEFAULT_SEED);

            Assert.That(first.catalog_sha256, Is.EqualTo(second.catalog_sha256));
            Assert.That(firstScenario.scenario_catalog_sha256, Is.EqualTo(secondScenario.scenario_catalog_sha256));
            Assert.That(PlacementSignature(Test_ActorFixtureLayout.BuildPlacements(first.profiles[1])),
                Is.EqualTo(PlacementSignature(Test_ActorFixtureLayout.BuildPlacements(second.profiles[1]))));
            Assert.That(JsonUtility.ToJson(firstScenario.profiles[1].ability_schedule),
                Is.EqualTo(JsonUtility.ToJson(secondScenario.profiles[1].ability_schedule)));
            Assert.That(JsonUtility.ToJson(firstScenario.profiles[1].events),
                Is.EqualTo(JsonUtility.ToJson(secondScenario.profiles[1].events)));
        }

        [Test]
        public void DifferentSeedChangesDeterministicPlacementInput()
        {
            var first = Test_ActorFixtureLayout.BuildCatalog(Test_ActorFixtureLayout.DEFAULT_SEED);
            var second = Test_ActorFixtureLayout.BuildCatalog(Test_ActorFixtureLayout.DEFAULT_SEED + 1);
            var firstPlacements = Test_ActorFixtureLayout.BuildPlacements(first.profiles[1]);
            var secondPlacements = Test_ActorFixtureLayout.BuildPlacements(second.profiles[1]);

            Assert.That(first.catalog_sha256, Is.Not.EqualTo(second.catalog_sha256));
            Assert.That(firstPlacements, Has.Count.EqualTo(Test_ActorFixtureLayout.NPC_COUNT));
            Assert.That(secondPlacements, Has.Count.EqualTo(Test_ActorFixtureLayout.NPC_COUNT));
            Assert.That(firstPlacements[0].world_x, Is.Not.EqualTo(secondPlacements[0].world_x));
            Assert.That(firstPlacements[0].world_z, Is.Not.EqualTo(secondPlacements[0].world_z));
        }

        [Test]
        public void ProfilesKeepDataOnlyAndFullActorAccountingSeparate()
        {
            var catalog = Test_ActorFixtureLayout.BuildCatalog();
            var dataOnly = catalog.profiles[0];
            var full = catalog.profiles[1];

            Assert.That(dataOnly.representation, Is.EqualTo("ecs_data_only"));
            Assert.That(dataOnly.population.accounting.ecs_data_only_npcs, Is.EqualTo(100));
            Assert.That(dataOnly.population.accounting.full_gameobject_purrnet_npcs, Is.EqualTo(0));
            Assert.That(dataOnly.population.accounting.real_player_connections, Is.EqualTo(0));
            Assert.That(dataOnly.population.accounting.synthetic_network_players, Is.EqualTo(0));

            Assert.That(full.representation, Is.EqualTo("networked_gameobject_ecs"));
            Assert.That(full.population.accounting.ecs_data_only_npcs, Is.EqualTo(0));
            Assert.That(full.population.accounting.full_gameobject_purrnet_npcs, Is.EqualTo(100));
            Assert.That(full.population.accounting.real_player_connections, Is.EqualTo(1));
            Assert.That(full.population.accounting.synthetic_network_players, Is.EqualTo(0));
        }

        [Test]
        public void PopulationUsesRequiredActivityAndSpatialMix()
        {
            var profile = Test_ActorFixtureLayout.BuildCatalog().profiles[1];
            var placements = Test_ActorFixtureLayout.BuildPlacements(profile);

            Assert.That(placements, Has.Count.EqualTo(100));
            Assert.That(Count(placements, "npc-idle"), Is.EqualTo(95));
            Assert.That(Count(placements, "npc-patrol"), Is.EqualTo(4));
            Assert.That(Count(placements, "npc-engaged"), Is.EqualTo(1));
            Assert.That(Count(placements, "distant"), Is.EqualTo(80));
            Assert.That(Count(placements, "outer"), Is.EqualTo(15));
            Assert.That(Count(placements, "player-start"), Is.EqualTo(5));

            var scenario = Test_ActorFixtureLayout.BuildScenarioCatalog().profiles[1];
            Assert.That(scenario.load_ramp_rate_actors_per_s, Is.EqualTo(100d / 120d));
            Assert.That(scenario.measurement_s, Is.EqualTo(300d));
            Assert.That(scenario.sample_interval_ms, Is.EqualTo(1000));
            Assert.That(FindEvent(scenario, "wake-up-surge-01").offset_s, Is.EqualTo(150d));
            Assert.That(FindEvent(scenario, "wake-up-surge-01").count, Is.EqualTo(10));
            Assert.That(FindAbility(scenario, "engaged-fireball").aura_definition_id, Is.EqualTo("burning"));
            Assert.That(FindAbility(scenario, "engaged-cleave").aura_definition_id, Is.EqualTo("bleed"));
        }

        [Test]
        public void MissingRunningServerIsReportedAsPreconditionFailure()
        {
            var profile = Test_ActorFixtureLayout.BuildCatalog().profiles[1];
            var exception = Assert.Throws<Test_ActorFixturePreconditionException>(() =>
                Test_ActorFixturePreflight.ValidateFull(profile, 0, default));

            Assert.That(exception.Message, Does.Contain("running PurrNet server"));
        }

        [Test]
        public void TrackedManifestsMatchDeterministicPlans()
        {
            Assert.That(File.Exists(Test_ActorFixtureLayout.CATALOG_PATH), Is.True,
                "The tracked actor catalog must be generated before running manifest regression tests.");
            Assert.That(File.Exists(Test_ActorFixtureLayout.SCENARIO_PATH), Is.True,
                "The tracked scenario catalog must be generated before running manifest regression tests.");

            var catalog = JsonUtility.FromJson<Test_ActorFixtureCatalog>(File.ReadAllText(Test_ActorFixtureLayout.CATALOG_PATH));
            var scenarios = JsonUtility.FromJson<Test_ActorScenarioCatalog>(File.ReadAllText(Test_ActorFixtureLayout.SCENARIO_PATH));
            var expectedCatalog = Test_ActorFixtureLayout.BuildCatalog();
            var expectedScenarios = Test_ActorFixtureLayout.BuildScenarioCatalog();

            Assert.That(catalog.catalog_sha256, Is.EqualTo(expectedCatalog.catalog_sha256));
            Assert.That(scenarios.scenario_catalog_sha256, Is.EqualTo(expectedScenarios.scenario_catalog_sha256));
            Assert.That(catalog.profiles, Has.Length.EqualTo(3));
            Assert.That(scenarios.profiles, Has.Length.EqualTo(3));
            Assert.That(AssetDatabase.AssetPathToGUID(Test_ActorFixtureLayout.NPC_PREFAB_PATH), Does.Match("^[0-9a-f]{32}$"));
        }

        private static int Count(System.Collections.Generic.IEnumerable<Test_ActorFixturePlacement> placements, string value)
        {
            var count = 0;
            foreach (var placement in placements)
                if (placement.cluster_id == value || placement.group_id == value)
                    count++;
            return count;
        }

        private static string PlacementSignature(IEnumerable<Test_ActorFixturePlacement> placements)
        {
            var builder = new StringBuilder();
            foreach (var placement in placements)
            {
                builder.Append(placement.actor_fixture_id).Append('|')
                    .Append(placement.group_id).Append('|')
                    .Append(placement.level.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.class_id).Append('|')
                    .Append(placement.cluster_id).Append('|')
                    .Append(placement.world_x.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.world_y.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.world_z.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.origin_cell_x.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.origin_cell_z.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.world_tile_x.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(placement.world_tile_z.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            return builder.ToString();
        }

        private static Test_ActorScenarioEvent FindEvent(Test_ActorScenarioProfile profile, string eventID)
        {
            foreach (var item in profile.events)
                if (item.event_id == eventID)
                    return item;
            Assert.Fail($"Scenario event '{eventID}' is missing.");
            return null;
        }

        private static Test_ActorAbilitySchedule FindAbility(Test_ActorScenarioProfile profile, string actionID)
        {
            foreach (var item in profile.ability_schedule)
                if (item.action_id == actionID)
                    return item;
            Assert.Fail($"Ability schedule '{actionID}' is missing.");
            return null;
        }
    }
}
