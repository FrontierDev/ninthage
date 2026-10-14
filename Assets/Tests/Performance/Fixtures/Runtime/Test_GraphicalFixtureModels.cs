using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Tests.Performance.Fixtures
{
    /// <summary>Identifies the graphical world scenarios exposed by the test-only runner.</summary>
    public enum Test_GraphicalScenario
    {
        Empty,
        Outdoor,
        Forest,
        Settlement,
        AdjacentTiles,
    }

    /// <summary>Identifies the requested rendered actor density.</summary>
    public enum Test_GraphicalActorDensity
    {
        None,
        VisibleTen,
        VisibleOneHundred,
    }

    /// <summary>Describes one deterministic camera route waypoint in authored world coordinates.</summary>
    [Serializable]
    public sealed class Test_GraphicalRouteWaypoint
    {
        public string WaypointID;
        public string FixtureVariant;
        public float WorldX;
        public float WorldY;
        public float WorldZ;
        public float HeadingDegrees;
        public int OriginCellX;
        public int OriginCellZ;
        public int WorldTileX;
        public int WorldTileZ;
    }

    /// <summary>Contains the deterministic fixture and route contract for one graphical scenario.</summary>
    [Serializable]
    public sealed class Test_GraphicalScenarioPlan
    {
        public string SchemaVersion;
        public string ProfileID;
        public string ProfileRevision;
        public string ScenarioID;
        public string Status;
        public string BlockedReason;
        public int Seed;
        public string ObjectDensityTier;
        public string ActorDensity;
        public string[] FixtureVariants;
        public string[] FixtureScenePaths;
        public string RouteID;
        public bool RequiresWorldStreaming;
        public bool RequiresRealClientSession;
        public int RequestedObjectCount;
        public int RequestedActorCount;
        public int RequestedNameplateCount;
        public int RequestedVFXCount;
        public float RouteDistanceMeters;
        public Test_GraphicalRouteWaypoint[] Waypoints;
        public string PlanHash;

        public bool IsReady => string.Equals(Status, "Ready", StringComparison.Ordinal);

        /// <summary>Returns the deterministic identity string used for the scenario plan hash.</summary>
        public string CanonicalIdentity()
        {
            var builder = new StringBuilder();
            builder.Append(SchemaVersion).Append('|')
                .Append(ProfileID).Append('|')
                .Append(ProfileRevision).Append('|')
                .Append(ScenarioID).Append('|')
                .Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(ObjectDensityTier).Append('|')
                .Append(ActorDensity).Append('|')
                .Append(RouteID).Append('|')
                .Append(RequiresWorldStreaming ? '1' : '0').Append('|')
                .Append(RequiresRealClientSession ? '1' : '0').Append('|')
                .Append(RequestedObjectCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(RequestedActorCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(RequestedNameplateCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(RequestedVFXCount.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(RouteDistanceMeters.ToString("R", CultureInfo.InvariantCulture));

            foreach (var variant in FixtureVariants)
                builder.Append('|').Append(variant);
            foreach (var path in FixtureScenePaths)
                builder.Append('|').Append(path);
            foreach (var waypoint in Waypoints)
            {
                builder.Append('|').Append(waypoint.WaypointID)
                    .Append('|').Append(waypoint.FixtureVariant)
                    .Append('|').Append(waypoint.WorldX.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.WorldY.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.WorldZ.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.HeadingDegrees.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.OriginCellX.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.OriginCellZ.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.WorldTileX.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(waypoint.WorldTileZ.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }

    /// <summary>Builds deterministic graphical scenario and route plans from the tracked world fixtures.</summary>
    public static class Test_GraphicalFixtureLayout
    {
        public const string SCHEMA_VERSION = "graphical-fixture/v1";
        public const string PROFILE_ID = "P1-S0-06";
        public const string PROFILE_REVISION = "1.0.0";
        public const string SPEC_REVISION = "P1-STRESS-1.0.0";
        public const int DEFAULT_SEED = Test_WorldFixtureLayout.DEFAULT_SEED;
        public const Test_WorldFixtureDensityTier DEFAULT_OBJECT_DENSITY = Test_WorldFixtureDensityTier.D1_Default;
        public const float DEFAULT_WARMUP_SECONDS = 2f;
        public const float DEFAULT_MEASUREMENT_SECONDS = 5f;
        public const string ROUTE_GENERATOR = "Game.Tests.Performance.Fixtures.Test_GraphicalFixtureLayout";

        /// <summary>Builds the requested deterministic graphical scenario without loading or modifying assets.</summary>
        public static Test_GraphicalScenarioPlan BuildPlan(
            Test_GraphicalScenario scenario,
            Test_WorldFixtureDensityTier objectDensity,
            Test_GraphicalActorDensity actorDensity,
            int seed)
        {
            var worldManifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = seed,
                DensityTier = objectDensity,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            var plan = new Test_GraphicalScenarioPlan
            {
                SchemaVersion = SCHEMA_VERSION,
                ProfileID = PROFILE_ID,
                ProfileRevision = PROFILE_REVISION,
                ScenarioID = scenario.ToString(),
                Status = "Ready",
                BlockedReason = string.Empty,
                Seed = seed,
                ObjectDensityTier = objectDensity.ToString(),
                ActorDensity = actorDensity.ToString(),
                RouteID = "graphical-" + scenario.ToString().ToLowerInvariant() + "-route",
                RequiresWorldStreaming = scenario == Test_GraphicalScenario.AdjacentTiles,
                RequiresRealClientSession = actorDensity != Test_GraphicalActorDensity.None,
                RequestedActorCount = GetActorCount(actorDensity),
                RequestedNameplateCount = GetActorCount(actorDensity),
                RequestedVFXCount = actorDensity == Test_GraphicalActorDensity.VisibleOneHundred ? 10 : 0,
            };

            var variants = GetVariants(scenario);
            plan.FixtureVariants = new string[variants.Length];
            plan.FixtureScenePaths = new string[variants.Length];
            var waypoints = BuildWaypoints(scenario);
            plan.Waypoints = waypoints.ToArray();
            for (var index = 0; index < variants.Length; index++)
            {
                plan.FixtureVariants[index] = variants[index].ToString();
                plan.FixtureScenePaths[index] = FindWorldPlan(worldManifest, variants[index]).AssetPath;
            }

            plan.RequestedObjectCount = 0;
            foreach (var variant in variants)
                plan.RequestedObjectCount += FindWorldPlan(worldManifest, variant).ObjectCount;
            plan.RouteDistanceMeters = CalculateRouteDistance(waypoints);
            plan.PlanHash = Hash(plan.CanonicalIdentity());

            if (seed != DEFAULT_SEED)
            {
                plan.Status = "Blocked";
                plan.BlockedReason = "Tracked graphical world scenes are authored for seed 424242; another seed requires regenerated test-owned scenes.";
            }
            else if (objectDensity != DEFAULT_OBJECT_DENSITY)
            {
                plan.Status = "Blocked";
                plan.BlockedReason = "Tracked graphical world scenes are available only at density tier D1_Default; another tier requires regenerated test-owned scenes.";
            }
            return plan;
        }

        /// <summary>Validates route references and coordinate contracts without Unity scene access.</summary>
        public static void ValidatePlan(Test_GraphicalScenarioPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (plan.Waypoints == null || plan.Waypoints.Length == 0)
                throw new InvalidOperationException("A graphical scenario must contain at least one route waypoint.");
            if (plan.FixtureVariants == null || plan.FixtureVariants.Length == 0)
                throw new InvalidOperationException("A graphical scenario must reference at least one world fixture variant.");
            if (plan.FixtureScenePaths == null || plan.FixtureScenePaths.Length != plan.FixtureVariants.Length)
                throw new InvalidOperationException("Graphical fixture scene references do not match fixture variants.");
            if (plan.RequiresWorldStreaming && plan.FixtureVariants.Length < 2)
                throw new InvalidOperationException("An adjacent-tile graphical scenario must reference both tile fixtures.");
            if (plan.RouteDistanceMeters < 0f)
                throw new InvalidOperationException("A graphical route cannot have a negative distance.");
            if (string.IsNullOrEmpty(plan.PlanHash) || plan.PlanHash != Hash(plan.CanonicalIdentity()))
                throw new InvalidOperationException("Graphical scenario plan hash does not match its deterministic inputs.");
        }

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

        private static int GetActorCount(Test_GraphicalActorDensity actorDensity)
        {
            switch (actorDensity)
            {
                case Test_GraphicalActorDensity.None:
                    return 0;
                case Test_GraphicalActorDensity.VisibleTen:
                    return 10;
                case Test_GraphicalActorDensity.VisibleOneHundred:
                    return 100;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actorDensity), actorDensity, null);
            }
        }

        private static Test_WorldFixtureVariant[] GetVariants(Test_GraphicalScenario scenario)
        {
            switch (scenario)
            {
                case Test_GraphicalScenario.Empty:
                    return new[] { Test_WorldFixtureVariant.Empty };
                case Test_GraphicalScenario.Outdoor:
                    return new[] { Test_WorldFixtureVariant.Outdoor };
                case Test_GraphicalScenario.Forest:
                    return new[] { Test_WorldFixtureVariant.Forest };
                case Test_GraphicalScenario.Settlement:
                    return new[] { Test_WorldFixtureVariant.Settlement };
                case Test_GraphicalScenario.AdjacentTiles:
                    return new[] { Test_WorldFixtureVariant.AdjacentTileWest, Test_WorldFixtureVariant.AdjacentTileEast };
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        private static List<Test_GraphicalRouteWaypoint> BuildWaypoints(Test_GraphicalScenario scenario)
        {
            switch (scenario)
            {
                case Test_GraphicalScenario.Empty:
                    return new List<Test_GraphicalRouteWaypoint> { Waypoint("empty-baseline", "Empty", 128f, 8f, 128f, 0f) };
                case Test_GraphicalScenario.Outdoor:
                    return BuildTileRoute("Outdoor", 0, 0, 0f);
                case Test_GraphicalScenario.Forest:
                    return BuildTileRoute("Forest", 0, 1, 25f);
                case Test_GraphicalScenario.Settlement:
                    return BuildTileRoute("Settlement", 1, 1, 45f);
                case Test_GraphicalScenario.AdjacentTiles:
                    return new List<Test_GraphicalRouteWaypoint>
                    {
                        Waypoint("adjacent-west", "AdjacentTileWest", 224f, 8f, 128f, 90f),
                        Waypoint("adjacent-boundary", "AdjacentTileWest", 248f, 8f, 128f, 90f),
                        Waypoint("adjacent-east", "AdjacentTileEast", 264f, 8f, 128f, 90f),
                        Waypoint("adjacent-east-end", "AdjacentTileEast", 320f, 8f, 128f, 90f),
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        private static List<Test_GraphicalRouteWaypoint> BuildTileRoute(
            string variant,
            int tileX,
            int tileZ,
            float heading)
        {
            var offsetX = tileX * Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS;
            var offsetZ = tileZ * Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS;
            return new List<Test_GraphicalRouteWaypoint>
            {
                Waypoint(variant.ToLowerInvariant() + "-start", variant, offsetX + 24f, 8f, offsetZ + 24f, heading),
                Waypoint(variant.ToLowerInvariant() + "-near", variant, offsetX + 88f, 8f, offsetZ + 88f, heading + 30f),
                Waypoint(variant.ToLowerInvariant() + "-far", variant, offsetX + 168f, 8f, offsetZ + 168f, heading + 60f),
                Waypoint(variant.ToLowerInvariant() + "-end", variant, offsetX + 232f, 8f, offsetZ + 232f, heading + 90f),
            };
        }

        private static Test_GraphicalRouteWaypoint Waypoint(
            string waypointID,
            string fixtureVariant,
            float worldX,
            float worldY,
            float worldZ,
            float headingDegrees)
        {
            return new Test_GraphicalRouteWaypoint
            {
                WaypointID = waypointID,
                FixtureVariant = fixtureVariant,
                WorldX = worldX,
                WorldY = worldY,
                WorldZ = worldZ,
                HeadingDegrees = headingDegrees,
                OriginCellX = Mathf.FloorToInt(worldX / Test_WorldFixtureLayout.ORIGIN_CELL_SIZE_METERS),
                OriginCellZ = Mathf.FloorToInt(worldZ / Test_WorldFixtureLayout.ORIGIN_CELL_SIZE_METERS),
                WorldTileX = Mathf.FloorToInt(worldX / Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS),
                WorldTileZ = Mathf.FloorToInt(worldZ / Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS),
            };
        }

        private static float CalculateRouteDistance(IReadOnlyList<Test_GraphicalRouteWaypoint> waypoints)
        {
            var distance = 0f;
            for (var index = 1; index < waypoints.Count; index++)
            {
                var previous = new Vector3(waypoints[index - 1].WorldX, waypoints[index - 1].WorldY, waypoints[index - 1].WorldZ);
                var current = new Vector3(waypoints[index].WorldX, waypoints[index].WorldY, waypoints[index].WorldZ);
                distance += Vector3.Distance(previous, current);
            }

            return distance;
        }

        private static Test_WorldFixturePlan FindWorldPlan(Test_WorldFixtureManifest manifest, Test_WorldFixtureVariant variant)
        {
            foreach (var plan in manifest.Scenes)
            {
                if (plan.Variant == variant.ToString())
                    return plan;
            }

            throw new InvalidOperationException("World fixture variant is missing from the deterministic manifest: " + variant);
        }
    }
}
