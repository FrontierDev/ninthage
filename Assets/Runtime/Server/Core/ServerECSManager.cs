using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Server.Systems;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Creates and owns the dedicated server ECS world.</summary>
    public static class ServerECSManager
    {
        private static readonly PerformanceMetricDefinition ECS_INITIALIZATION_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_INITIALIZATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.initialization",
            "server_update");
        private static readonly PerformanceMetricDefinition SIMULATION_TICK_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_TICK_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMetricDefinition SIMULATION_TICK_BACKLOG_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_TICK_BACKLOG,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMarker ECS_INITIALIZATION_MARKER = new(ECS_INITIALIZATION_DURATION_METRIC, "Server.ECS.Initialization");
        private static bool _initialized = false;
        /// <summary>Gets whether the server ECS world has been initialized.</summary>
        public static bool Initialized => _initialized;

        /// <summary>Gets the server's default ECS world.</summary>
        public static World DefaultWorld { get; private set; }

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(ECS_INITIALIZATION_DURATION_METRIC, 4096);
            PerformanceTelemetry.RegisterMetric(
                SIMULATION_TICK_DURATION_METRIC,
                0,
                PerformanceMetricObservation.Unavailable,
                "The server has no project-owned fixed simulation tick timing probe.");
            PerformanceTelemetry.RegisterMetric(
                SIMULATION_TICK_BACKLOG_METRIC,
                0,
                PerformanceMetricObservation.Unavailable,
                "The server has no project-owned fixed simulation tick backlog probe.");
        }

        /// <summary>Creates the server ECS world and registers its systems.</summary>
        public static void ServerInitializeECSWorlds()
        {
            using var markerScope = ECS_INITIALIZATION_MARKER.Begin();
            // Create the world.
            DefaultWorld = new World("Default");

            // Create the main simulation system group.
            var systemGroup = DefaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Create and add systems to the world.
            var testSystem = DefaultWorld.GetOrCreateSystem<WorldSpawnSystem>();
            systemGroup.AddSystemToUpdateList(testSystem);

            var actorUpdateSystem = DefaultWorld.GetOrCreateSystem<ActorUpdateSystem>();
            systemGroup.AddSystemToUpdateList(actorUpdateSystem);

            var resourceRegenSystem = DefaultWorld.GetOrCreateSystem<ResourceRegenSystem>();
            systemGroup.AddSystemToUpdateList(resourceRegenSystem);

            var spellCastSystem = DefaultWorld.GetOrCreateSystem<SpellCastSystem>();
            systemGroup.AddSystemToUpdateList(spellCastSystem);

            var cooldownTickSystem = DefaultWorld.GetOrCreateSystem<CooldownTickSystem>();
            systemGroup.AddSystemToUpdateList(cooldownTickSystem);

            var auraTickSystem = DefaultWorld.GetOrCreateSystem<AuraTickSystem>();
            systemGroup.AddSystemToUpdateList(auraTickSystem);

            var actorTickerSystem = DefaultWorld.GetOrCreateSystem<ActorTickerSystem>();
            systemGroup.AddSystemToUpdateList(actorTickerSystem);

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(DefaultWorld);

            _initialized = true;
        }
    }
}
