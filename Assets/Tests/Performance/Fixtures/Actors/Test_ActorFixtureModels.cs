using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Game.Tests.Performance.Fixtures.Actors
{
    /// <summary>Defines the two actor representations covered by the fixture contract.</summary>
    public enum Test_ActorFixtureRepresentation
    {
        ecs_data_only,
        networked_gameobject_ecs,
    }

    /// <summary>Builds deterministic seeded actor and activity manifests.</summary>
    public static class Test_ActorFixtureLayout
    {
        public const int DEFAULT_SEED = 424242;
        public const string SCHEMA_VERSION = "actor-fixture-catalog/v1";
        public const string SCENARIO_SCHEMA_VERSION = "actor-scenario/v1";
        public const string PROFILE_ID = "P1-S0-04";
        public const string PROFILE_REVISION = "1.0.0";
        public const string SPEC_REVISION = "P1-STRESS-1.0.0";
        public const string GENERATOR_REVISION = "1.0.0";
        public const string ASSET_MANIFEST_SHA256 = "ad8a7c5ddced960ac7e028b5f75ff2e6f04ba6cf7ee6e2df946ef90d08e26842";
        public const int NPC_COUNT = 100;
        public const int IDLE_COUNT = 95;
        public const int PATROL_COUNT = 4;
        public const int ENGAGED_COUNT = 1;
        public const string CATALOG_PATH = "Assets/Tests/Performance/Fixtures/Actors/actor-population-manifest.json";
        public const string SCENARIO_PATH = "Assets/Tests/Performance/Fixtures/Scenarios/scenario-manifest.json";
        public const string ASSET_MANIFEST_PATH = "Assets/Tests/Performance/Fixtures/Actors/actor-asset-manifest.json";
        public const string NPC_PREFAB_PATH = "Assets/Prefabs/Test/[PH] NPC Unit.prefab";
        public const string PLAYER_PREFAB_PATH = "Assets/Prefabs/PlayerPrefab.prefab";
        public const string GLOBAL_ACTOR_SCENE_PATH = "Assets/Scenes/global_actors.unity";

        /// <summary>Builds the tracked actor population catalog.</summary>
        public static Test_ActorFixtureCatalog BuildCatalog(int seed = DEFAULT_SEED)
        {
            var catalog = new Test_ActorFixtureCatalog
            {
                schema_version = SCHEMA_VERSION,
                profile_id = PROFILE_ID,
                profile_revision = PROFILE_REVISION,
                spec_revision = SPEC_REVISION,
                generator_revision = GENERATOR_REVISION,
                master_seed = seed,
                derivation = "sha256-u32le-v1",
                streams = new[] { "actors", "movement", "abilities", "lifecycle" },
                profiles = new[]
                {
                    BuildProfile("S04-I-DATA", Test_ActorFixtureRepresentation.ecs_data_only, 0, seed),
                    BuildProfile("S04-I-FULL", Test_ActorFixtureRepresentation.networked_gameobject_ecs, 1, seed),
                    BuildProfile("S04-C-WAKE", Test_ActorFixtureRepresentation.networked_gameobject_ecs, 1, seed),
                },
                blocked_capabilities = new[]
                {
                    new Test_ActorFixtureBlockedCapability
                    {
                        capability = "true_dormant_sleep_wake",
                        status = "Blocked",
                        reason = "Dormant/sleep simulation LOD belongs to Stage 1.4 and is not claimed by this fixture.",
                    },
                    new Test_ActorFixtureBlockedCapability
                    {
                        capability = "synthetic_player_authentication",
                        status = "Blocked",
                        reason = "Player entry/authentication requires a real PurrNet client; synthetic actors are never counted as network players.",
                    },
                },
                limitation = "The current prototype player entry path hardcodes World_0_0 and position (0,0,0) before the initial height offset. This fixture does not claim to exercise connection or authentication when no real client is present.",
            };

            catalog.catalog_sha256 = Hash(CanonicalCatalog(catalog));
            return catalog;
        }

        /// <summary>Builds the scenario schedule manifest shared by the three actor profiles.</summary>
        public static Test_ActorScenarioCatalog BuildScenarioCatalog(int seed = DEFAULT_SEED)
        {
            var catalog = new Test_ActorScenarioCatalog
            {
                schema_version = SCENARIO_SCHEMA_VERSION,
                profile_id = PROFILE_ID,
                profile_revision = PROFILE_REVISION,
                spec_revision = SPEC_REVISION,
                master_seed = seed,
                profiles = new[]
                {
                    BuildScenario("S04-I-DATA", Test_ActorFixtureRepresentation.ecs_data_only, false),
                    BuildScenario("S04-I-FULL", Test_ActorFixtureRepresentation.networked_gameobject_ecs, true),
                    BuildScenario("S04-C-WAKE", Test_ActorFixtureRepresentation.networked_gameobject_ecs, true),
                },
            };

            catalog.scenario_catalog_sha256 = Hash(CanonicalScenarios(catalog));
            return catalog;
        }

        /// <summary>Returns deterministic placement records for one profile.</summary>
        public static List<Test_ActorFixturePlacement> BuildPlacements(Test_ActorFixtureProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var placements = new List<Test_ActorFixturePlacement>(NPC_COUNT);
            var random = new Test_ActorFixtureRandom(profile.replicate_seed, profile.variant_id, "actors");
            // P1-S0-04 spatial mix: 80% distant, 15% outer, 5% beside the player.
            // The activity mix remains 95% idle, 4% patrolling and 1% engaged.
            AddPlacements(placements, profile, "npc-idle", 80, 5, 10, "fighter", "distant", random, 0);
            AddPlacements(placements, profile, "npc-idle", 11, 5, 10, "fighter", "outer", random, 80);
            AddPlacements(placements, profile, "npc-patrol", PATROL_COUNT, 10, 15, "ranger", "outer", random, 0);
            AddPlacements(placements, profile, "npc-idle", 4, 5, 10, "fighter", "player-start", random, 91);
            AddPlacements(placements, profile, "npc-engaged", ENGAGED_COUNT, 15, 20, "sorcerer", "player-start", random, 0);
            return placements;
        }

        /// <summary>Checks the required population invariants before any runtime is started.</summary>
        public static void ValidatePopulation(Test_ActorFixtureProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (profile.population == null || profile.population.players == null || profile.population.npcs == null || profile.population.accounting == null)
                throw new InvalidOperationException($"{profile.variant_id} is missing population accounting data.");
            if (profile.population.npcs.groups == null || profile.population.npcs.groups.Length != 3)
                throw new InvalidOperationException($"{profile.variant_id} must define exactly three NPC activity groups.");
            if (profile.population.npcs.requested != NPC_COUNT)
                throw new InvalidOperationException($"{profile.variant_id} must request exactly {NPC_COUNT} NPCs.");
            if (profile.population.npcs.groups[0].count + profile.population.npcs.groups[1].count + profile.population.npcs.groups[2].count != NPC_COUNT)
                throw new InvalidOperationException($"{profile.variant_id} NPC group counts do not sum to {NPC_COUNT}.");
            if (profile.population.accounting.ecs_data_only_npcs + profile.population.accounting.full_gameobject_purrnet_npcs != NPC_COUNT)
                throw new InvalidOperationException($"{profile.variant_id} representation accounting does not sum to {NPC_COUNT}.");
            if (profile.population.accounting.real_player_connections != profile.population.players.requested)
                throw new InvalidOperationException($"{profile.variant_id} real-player accounting does not match requested players.");
        }

        /// <summary>Returns stable canonical input for the catalog hash.</summary>
        public static string CanonicalCatalog(Test_ActorFixtureCatalog catalog)
        {
            var builder = new StringBuilder();
            builder.Append(catalog.schema_version).Append('|')
                .Append(catalog.profile_id).Append('|')
                .Append(catalog.profile_revision).Append('|')
                .Append(catalog.spec_revision).Append('|')
                .Append(catalog.generator_revision).Append('|')
                .Append(catalog.master_seed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(catalog.derivation);

            foreach (var profile in catalog.profiles)
            {
                builder.Append('|').Append(profile.variant_id)
                    .Append('|').Append(profile.representation)
                    .Append('|').Append(profile.replicate_seed.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(profile.population.players.requested.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(profile.population.npcs.requested.ToString(CultureInfo.InvariantCulture));

                foreach (var group in profile.population.npcs.groups)
                {
                    builder.Append('|').Append(group.group_id)
                        .Append('|').Append(group.role)
                        .Append('|').Append(group.count.ToString(CultureInfo.InvariantCulture))
                        .Append('|').Append(group.level_min.ToString(CultureInfo.InvariantCulture))
                        .Append('|').Append(group.level_max.ToString(CultureInfo.InvariantCulture))
                        .Append('|').Append(group.class_id)
                        .Append('|').Append(group.cluster_id);
                }

                foreach (var cluster in profile.space.clusters)
                {
                    builder.Append('|').Append(cluster.cluster_id)
                        .Append('|').Append(cluster.center_world_m[0].ToString("R", CultureInfo.InvariantCulture))
                        .Append('|').Append(cluster.center_world_m[1].ToString("R", CultureInfo.InvariantCulture))
                        .Append('|').Append(cluster.center_world_m[2].ToString("R", CultureInfo.InvariantCulture))
                        .Append('|').Append(cluster.radius_m.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            foreach (var blocked in catalog.blocked_capabilities)
                builder.Append('|').Append(blocked.capability).Append('|').Append(blocked.status).Append('|').Append(blocked.reason);

            return builder.ToString();
        }

        /// <summary>Returns stable canonical input for the scenario catalog hash.</summary>
        public static string CanonicalScenarios(Test_ActorScenarioCatalog catalog)
        {
            var builder = new StringBuilder();
            builder.Append(catalog.schema_version).Append('|')
                .Append(catalog.profile_id).Append('|')
                .Append(catalog.profile_revision).Append('|')
                .Append(catalog.spec_revision).Append('|')
                .Append(catalog.master_seed.ToString(CultureInfo.InvariantCulture));

            foreach (var profile in catalog.profiles)
            {
                builder.Append('|').Append(profile.variant_id)
                    .Append('|').Append(profile.representation)
                    .Append('|').Append(profile.requires_real_client ? '1' : '0')
                    .Append('|').Append(profile.load_ramp_rate_actors_per_s.ToString("R", CultureInfo.InvariantCulture));

                foreach (var route in profile.movement_routes)
                    builder.Append('|').Append(route.route_id).Append('|').Append(route.actor_group).Append('|').Append(route.loop ? '1' : '0');
                foreach (var action in profile.ability_schedule)
                    builder.Append('|').Append(action.action_id).Append('|').Append(action.actor_group).Append('|').Append(action.ability_definition_id).Append('|').Append(action.effect_kind).Append('|').Append(action.attempts_per_actor_per_s.ToString("R", CultureInfo.InvariantCulture));
                foreach (var evt in profile.events)
                    builder.Append('|').Append(evt.event_id).Append('|').Append(evt.kind).Append('|').Append(evt.offset_s.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(evt.count.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>Computes a stable lowercase SHA-256 hash.</summary>
        public static string Hash(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var valueByte in bytes)
                    builder.Append(valueByte.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static Test_ActorFixtureProfile BuildProfile(string variantID, Test_ActorFixtureRepresentation representation, int realPlayers, int seed)
        {
            var isDataOnly = representation == Test_ActorFixtureRepresentation.ecs_data_only;
            var profile = new Test_ActorFixtureProfile
            {
                profile_id = PROFILE_ID,
                variant_id = variantID,
                revision = PROFILE_REVISION,
                tier = "T1",
                replicate_seed = seed,
                representation = representation.ToString(),
                population = new Test_ActorFixturePopulation
                {
                    players = new Test_ActorFixturePlayerPopulation
                    {
                        requested = realPlayers,
                        connection_kind = realPlayers == 0 ? "none_data_only" : "real_purrnet",
                        groups = realPlayers == 0 ? Array.Empty<Test_ActorFixturePlayerGroup>() : new[]
                        {
                            new Test_ActorFixturePlayerGroup
                            {
                                group_id = "player-route",
                                class_id = "fighter",
                                role = "traveller",
                                count = 1,
                                cluster_id = "player-start",
                                activity = new Test_ActorFixtureActivityMix { idle = 0.5d, travelling = 0.5d, combat = 0d },
                            },
                        },
                    },
                    npcs = new Test_ActorFixtureNPCPopulation
                    {
                        requested = NPC_COUNT,
                        representation = representation.ToString(),
                        groups = BuildNPCGroups(representation),
                    },
                    accounting = new Test_ActorFixtureAccounting
                    {
                        ecs_data_only_npcs = isDataOnly ? NPC_COUNT : 0,
                        full_gameobject_purrnet_npcs = isDataOnly ? 0 : NPC_COUNT,
                        real_player_connections = realPlayers,
                        synthetic_network_players = 0,
                    },
                },
                space = BuildSpace(realPlayers),
                environment = new Test_ActorFixtureEnvironment
                {
                    world_id = "fixture-world",
                    map_id = "fixture-open-world",
                    instance_id = null,
                    origin_cell_size_m = 100,
                    world_tile_size_m = 256,
                    authored_grid_columns = 5,
                    authored_grid_rows = 5,
                    collision_enabled = true,
                },
                provenance = BuildProvenance(),
                required_features = isDataOnly
                    ? new[] { "seeded_actor_placement", "ecs_data_only_actor_state", "deterministic_lifecycle_ledger" }
                    : new[] { "seeded_actor_placement", "full_networked_npc_spawn", "server_actor_lifecycle", "legal_scripted_activity" },
            };

            ValidatePopulation(profile);
            return profile;
        }

        private static Test_ActorFixtureNPCGroup[] BuildNPCGroups(Test_ActorFixtureRepresentation representation)
        {
            return new[]
            {
                new Test_ActorFixtureNPCGroup
                {
                    group_id = "npc-idle",
                    definition_id = "6e50fb82-1724-4040-afec-88fabc925ae0",
                    prefab_path = representation == Test_ActorFixtureRepresentation.networked_gameobject_ecs ? NPC_PREFAB_PATH : string.Empty,
                    role = "idle",
                    count = IDLE_COUNT,
                    level_min = 5,
                    level_max = 10,
                    class_id = "fighter",
                    cluster_id = "distant",
                    route_id = string.Empty,
                    activity = new Test_ActorFixtureNPCActivityMix { idle = 1d, patrolling = 0d, engaged = 0d },
                },
                new Test_ActorFixtureNPCGroup
                {
                    group_id = "npc-patrol",
                    definition_id = "6e50fb82-1724-4040-afec-88fabc925ae0",
                    prefab_path = representation == Test_ActorFixtureRepresentation.networked_gameobject_ecs ? NPC_PREFAB_PATH : string.Empty,
                    role = "patrolling",
                    count = PATROL_COUNT,
                    level_min = 10,
                    level_max = 15,
                    class_id = "ranger",
                    cluster_id = "outer",
                    route_id = "patrol-route-alpha",
                    activity = new Test_ActorFixtureNPCActivityMix { idle = 0d, patrolling = 1d, engaged = 0d },
                },
                new Test_ActorFixtureNPCGroup
                {
                    group_id = "npc-engaged",
                    definition_id = "6e50fb82-1724-4040-afec-88fabc925ae0",
                    prefab_path = representation == Test_ActorFixtureRepresentation.networked_gameobject_ecs ? NPC_PREFAB_PATH : string.Empty,
                    role = "engaged",
                    count = ENGAGED_COUNT,
                    level_min = 15,
                    level_max = 20,
                    class_id = "sorcerer",
                    cluster_id = "player-start",
                    route_id = "engaged-ring",
                    activity = new Test_ActorFixtureNPCActivityMix { idle = 0d, patrolling = 0d, engaged = 1d },
                },
            };
        }

        private static Test_ActorFixtureSpace BuildSpace(int realPlayers)
        {
            return new Test_ActorFixtureSpace
            {
                world_id = "fixture-world",
                map_id = "fixture-open-world",
                instance_id = null,
                clusters = new[]
                {
                    new Test_ActorFixtureCluster { cluster_id = "distant", center_world_m = new[] { -512d, 0d, -512d }, radius_m = 64d, players = 0, npcs = 80 },
                    new Test_ActorFixtureCluster { cluster_id = "outer", center_world_m = new[] { 128d, 0d, 128d }, radius_m = 64d, players = 0, npcs = 15 },
                    new Test_ActorFixtureCluster { cluster_id = "player-start", center_world_m = new[] { 0d, 0d, 0d }, radius_m = 32d, players = realPlayers, npcs = 5 },
                },
            };
        }

        private static Test_ActorFixtureProvenance BuildProvenance()
        {
            return new Test_ActorFixtureProvenance
            {
                fixture_generator = "Game.Tests.Performance.Fixtures.Actors.Test_ActorFixtureGenerator",
                generator_revision = GENERATOR_REVISION,
                input_path = CATALOG_PATH,
                asset_manifest_path = "Assets/Tests/Performance/Fixtures/Actors/actor-asset-manifest.json",
                source_commit = "UNRESOLVED_UNTIL_COMMIT",
                source_sha256 = string.Empty,
                asset_manifest_sha256 = ASSET_MANIFEST_SHA256,
            };
        }

        private static Test_ActorScenarioProfile BuildScenario(string variantID, Test_ActorFixtureRepresentation representation, bool requiresRealClient)
        {
            return new Test_ActorScenarioProfile
            {
                variant_id = variantID,
                representation = representation.ToString(),
                requires_real_client = requiresRealClient,
                load_ramp_rate_actors_per_s = NPC_COUNT / 120d,
                preflight_timeout_s = 30d,
                ramp_s = 120d,
                warmup_s = 60d,
                measurement_s = 300d,
                recovery_s = 60d,
                sample_interval_ms = 1000,
                movement_routes = new[]
                {
                    new Test_ActorMovementRoute
                    {
                        route_id = "patrol-route-alpha",
                        actor_group = "npc-patrol",
                        loop = true,
                        start_phase = "warmup",
                        waypoints_world_m = new[]
                        {
                            new Test_ActorWorldPoint { x = 96d, y = 0d, z = 96d },
                            new Test_ActorWorldPoint { x = 160d, y = 0d, z = 96d },
                            new Test_ActorWorldPoint { x = 160d, y = 0d, z = 160d },
                            new Test_ActorWorldPoint { x = 96d, y = 0d, z = 160d },
                        },
                    },
                    new Test_ActorMovementRoute
                    {
                        route_id = "player-entry-route",
                        actor_group = "player-route",
                        loop = true,
                        start_phase = "measurement",
                        waypoints_world_m = new[]
                        {
                            new Test_ActorWorldPoint { x = 0d, y = 5d, z = 0d },
                            new Test_ActorWorldPoint { x = 20d, y = 5d, z = 0d },
                            new Test_ActorWorldPoint { x = 120d, y = 5d, z = 0d },
                            new Test_ActorWorldPoint { x = -120d, y = 5d, z = 0d },
                        },
                    },
                },
                ability_schedule = new[]
                {
                    new Test_ActorAbilitySchedule { action_id = "engaged-auto-attack", actor_group = "npc-engaged", ability_definition_id = "auto_attack", target_group = "player-route", effect_kind = "auto_attack", attempts_per_actor_per_s = 0.5d, start_phase = "measurement" },
                    new Test_ActorAbilitySchedule { action_id = "engaged-fireball", actor_group = "npc-engaged", ability_definition_id = "fireball", target_group = "player-route", effect_kind = "single_target_cast_with_aura", aura_definition_id = "burning", attempts_per_actor_per_s = 0.1d, start_phase = "measurement" },
                    new Test_ActorAbilitySchedule { action_id = "engaged-cleave", actor_group = "npc-engaged", ability_definition_id = "cleave", target_group = "player-route", effect_kind = "aoe_cone_with_aura", aura_definition_id = "bleed", attempts_per_actor_per_s = 0.1d, start_phase = "measurement" },
                },
                events = new[]
                {
                    new Test_ActorScenarioEvent { event_id = "respawn-churn-01", kind = "respawn_churn", actor_group = "npc-patrol", offset_s = 90d, count = 5, from_activity = "patrolling", to_activity = "patrolling" },
                    new Test_ActorScenarioEvent { event_id = "wake-up-surge-01", kind = "wake_up_surge", actor_group = "npc-idle", offset_s = 150d, count = 10, from_activity = "idle", to_activity = "engaged" },
                    new Test_ActorScenarioEvent { event_id = "engagement-reset-01", kind = "reset_engagement", actor_group = "npc-idle", offset_s = 240d, count = 10, from_activity = "engaged", to_activity = "idle" },
                },
                deterministic_boundaries = new Test_ActorScenarioBoundaries
                {
                    preflight_start_s = 0d,
                    ramp_start_s = 0d,
                    warmup_start_s = 120d,
                    measurement_start_s = 180d,
                    measurement_end_s = 480d,
                    recovery_end_s = 540d,
                },
            };
        }

        private static void AddPlacements(
            ICollection<Test_ActorFixturePlacement> placements,
            Test_ActorFixtureProfile profile,
            string groupID,
            int count,
            int levelMin,
            int levelMax,
            string classID,
            string clusterID,
            Test_ActorFixtureRandom random,
            int indexOffset)
        {
            var cluster = FindCluster(profile.space, clusterID);
            for (var index = 0; index < count; index++)
            {
                var angle = random.NextDouble() * Math.PI * 2d;
                var radius = Math.Sqrt(random.NextDouble()) * cluster.radius_m;
                var x = cluster.center_world_m[0] + Math.Cos(angle) * radius;
                var z = cluster.center_world_m[2] + Math.Sin(angle) * radius;
                var level = levelMin + random.NextInt(levelMax - levelMin + 1);
                placements.Add(new Test_ActorFixturePlacement
                {
                    actor_fixture_id = $"{profile.variant_id}/{groupID}/{index + indexOffset:D4}",
                    group_id = groupID,
                    representation = profile.representation,
                    level = level,
                    class_id = classID,
                    cluster_id = clusterID,
                    world_x = x,
                    world_y = 0d,
                    world_z = z,
                    origin_cell_x = (int)Math.Floor(x / 100d),
                    origin_cell_z = (int)Math.Floor(z / 100d),
                    world_tile_x = (int)Math.Floor(x / 256d),
                    world_tile_z = (int)Math.Floor(z / 256d),
                });
            }
        }

        private static Test_ActorFixtureCluster FindCluster(Test_ActorFixtureSpace space, string clusterID)
        {
            foreach (var cluster in space.clusters)
                if (cluster.cluster_id == clusterID)
                    return cluster;
            throw new InvalidOperationException($"Actor fixture cluster '{clusterID}' is not defined.");
        }
    }

    /// <summary>Catalog of actor representation variants.</summary>
    [Serializable]
    public sealed class Test_ActorFixtureCatalog
    {
        public string schema_version;
        public string profile_id;
        public string profile_revision;
        public string spec_revision;
        public string generator_revision;
        public int master_seed;
        public string derivation;
        public string[] streams;
        public Test_ActorFixtureProfile[] profiles;
        public Test_ActorFixtureBlockedCapability[] blocked_capabilities;
        public string limitation;
        public string catalog_sha256;
    }

    /// <summary>One complete actor-population variant.</summary>
    [Serializable]
    public sealed class Test_ActorFixtureProfile
    {
        public string profile_id;
        public string variant_id;
        public string revision;
        public string tier;
        public int replicate_seed;
        public string representation;
        public string[] required_features;
        public Test_ActorFixturePopulation population;
        public Test_ActorFixtureSpace space;
        public Test_ActorFixtureEnvironment environment;
        public Test_ActorFixtureProvenance provenance;
    }

    [Serializable]
    public sealed class Test_ActorFixturePopulation
    {
        public Test_ActorFixturePlayerPopulation players;
        public Test_ActorFixtureNPCPopulation npcs;
        public Test_ActorFixtureAccounting accounting;
    }

    [Serializable]
    public sealed class Test_ActorFixturePlayerPopulation
    {
        public int requested;
        public string connection_kind;
        public Test_ActorFixturePlayerGroup[] groups;
    }

    [Serializable]
    public sealed class Test_ActorFixturePlayerGroup
    {
        public string group_id;
        public string class_id;
        public string role;
        public int count;
        public string cluster_id;
        public Test_ActorFixtureActivityMix activity;
    }

    [Serializable]
    public sealed class Test_ActorFixtureNPCPopulation
    {
        public int requested;
        public string representation;
        public Test_ActorFixtureNPCGroup[] groups;
    }

    [Serializable]
    public sealed class Test_ActorFixtureNPCGroup
    {
        public string group_id;
        public string definition_id;
        public string prefab_path;
        public string role;
        public int count;
        public int level_min;
        public int level_max;
        public string class_id;
        public string cluster_id;
        public string route_id;
        public Test_ActorFixtureNPCActivityMix activity;
    }

    [Serializable]
    public sealed class Test_ActorFixtureActivityMix
    {
        public double idle;
        public double travelling;
        public double combat;
    }

    [Serializable]
    public sealed class Test_ActorFixtureNPCActivityMix
    {
        public double idle;
        public double patrolling;
        public double engaged;
    }

    [Serializable]
    public sealed class Test_ActorFixtureAccounting
    {
        public int ecs_data_only_npcs;
        public int full_gameobject_purrnet_npcs;
        public int real_player_connections;
        public int synthetic_network_players;
    }

    [Serializable]
    public sealed class Test_ActorFixtureSpace
    {
        public string world_id;
        public string map_id;
        public string instance_id;
        public Test_ActorFixtureCluster[] clusters;
    }

    [Serializable]
    public sealed class Test_ActorFixtureCluster
    {
        public string cluster_id;
        public double[] center_world_m;
        public double radius_m;
        public int players;
        public int npcs;
    }

    [Serializable]
    public sealed class Test_ActorFixtureEnvironment
    {
        public string world_id;
        public string map_id;
        public string instance_id;
        public int origin_cell_size_m;
        public int world_tile_size_m;
        public int authored_grid_columns;
        public int authored_grid_rows;
        public bool collision_enabled;
    }

    [Serializable]
    public sealed class Test_ActorFixtureProvenance
    {
        public string fixture_generator;
        public string generator_revision;
        public string input_path;
        public string asset_manifest_path;
        public string source_commit;
        public string source_sha256;
        public string asset_manifest_sha256;
    }

    [Serializable]
    public sealed class Test_ActorFixtureBlockedCapability
    {
        public string capability;
        public string status;
        public string reason;
    }

    /// <summary>Records the imported project assets required by the actor fixture.</summary>
    [Serializable]
    public sealed class Test_ActorAssetManifest
    {
        public string schema_version;
        public string profile_id;
        public string source_status;
        public Test_ActorAssetEntry[] assets;
        public string[] limitations;
    }

    [Serializable]
    public sealed class Test_ActorAssetEntry
    {
        public string path;
        public string guid;
        public string kind;
        public string definition_id;
        public string[] required_for;
    }

    [Serializable]
    public sealed class Test_ActorFixturePlacement
    {
        public string actor_fixture_id;
        public string group_id;
        public string representation;
        public int level;
        public string class_id;
        public string cluster_id;
        public double world_x;
        public double world_y;
        public double world_z;
        public int origin_cell_x;
        public int origin_cell_z;
        public int world_tile_x;
        public int world_tile_z;
    }

    [Serializable]
    public sealed class Test_ActorScenarioCatalog
    {
        public string schema_version;
        public string profile_id;
        public string profile_revision;
        public string spec_revision;
        public int master_seed;
        public Test_ActorScenarioProfile[] profiles;
        public string scenario_catalog_sha256;
    }

    [Serializable]
    public sealed class Test_ActorScenarioProfile
    {
        public string variant_id;
        public string representation;
        public bool requires_real_client;
        public double load_ramp_rate_actors_per_s;
        public double preflight_timeout_s;
        public double ramp_s;
        public double warmup_s;
        public double measurement_s;
        public double recovery_s;
        public int sample_interval_ms;
        public Test_ActorMovementRoute[] movement_routes;
        public Test_ActorAbilitySchedule[] ability_schedule;
        public Test_ActorScenarioEvent[] events;
        public Test_ActorScenarioBoundaries deterministic_boundaries;
    }

    [Serializable]
    public sealed class Test_ActorMovementRoute
    {
        public string route_id;
        public string actor_group;
        public bool loop;
        public string start_phase;
        public Test_ActorWorldPoint[] waypoints_world_m;
    }

    [Serializable]
    public sealed class Test_ActorWorldPoint
    {
        public double x;
        public double y;
        public double z;
    }

    [Serializable]
    public sealed class Test_ActorAbilitySchedule
    {
        public string action_id;
        public string actor_group;
        public string ability_definition_id;
        public string target_group;
        public string effect_kind;
        public string aura_definition_id;
        public double attempts_per_actor_per_s;
        public string start_phase;
    }

    [Serializable]
    public sealed class Test_ActorScenarioEvent
    {
        public string event_id;
        public string kind;
        public string actor_group;
        public double offset_s;
        public int count;
        public string from_activity;
        public string to_activity;
    }

    [Serializable]
    public sealed class Test_ActorScenarioBoundaries
    {
        public double preflight_start_s;
        public double ramp_start_s;
        public double warmup_start_s;
        public double measurement_start_s;
        public double measurement_end_s;
        public double recovery_end_s;
    }

    /// <summary>Versioned deterministic PRNG for actor placement input data.</summary>
    internal sealed class Test_ActorFixtureRandom
    {
        private uint _state;

        public Test_ActorFixtureRandom(int replicateSeed, string profileID, string streamName)
        {
            var seedText = $"ninthage-bench-seed-v1\n{Test_ActorFixtureLayout.PROFILE_ID}\n{Test_ActorFixtureLayout.PROFILE_REVISION}\n{replicateSeed}\n{streamName}:{profileID}";
            using (var sha256 = SHA256.Create())
            {
                var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(seedText));
                _state = (uint)(digest[0] | digest[1] << 8 | digest[2] << 16 | digest[3] << 24);
            }

            if (_state == 0)
                _state = 0x6D2B79F5u;
        }

        public double NextDouble()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state / 4294967296d;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return (int)(NextDouble() * exclusiveMax);
        }
    }
}
