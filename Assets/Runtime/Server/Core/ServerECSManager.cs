using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Game.Server.Systems;
using System;
using Unity.Core;
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
        private static readonly PerformanceMetricDefinition SIMULATION_TICKS_SCHEDULED_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_TICKS_SCHEDULED,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMetricDefinition SIMULATION_TICKS_EXECUTED_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_TICKS_EXECUTED,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMetricDefinition SIMULATION_DEADLINE_MISS_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_DEADLINE_MISS_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMetricDefinition SIMULATION_CATCH_UP_CAP_HIT_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SIMULATION_CATCH_UP_CAP_HIT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "simulation",
            "fixed_tick");
        private static readonly PerformanceMarker ECS_INITIALIZATION_MARKER = new(ECS_INITIALIZATION_DURATION_METRIC, "Server.ECS.Initialization");
        private static SimulationSystemGroup _simulationSystemGroup;
        private static bool _initialized = false;
        /// <summary>Gets whether the server ECS world has been initialized.</summary>
        public static bool Initialized => _initialized;

        /// <summary>Gets the server's default ECS world.</summary>
        public static World DefaultWorld { get; private set; }

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(ECS_INITIALIZATION_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(
                SIMULATION_TICK_DURATION_METRIC,
                PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SIMULATION_TICK_BACKLOG_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SIMULATION_TICKS_SCHEDULED_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SIMULATION_TICKS_EXECUTED_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SIMULATION_DEADLINE_MISS_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SIMULATION_CATCH_UP_CAP_HIT_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Creates the server ECS world and registers its systems.</summary>
        public static void ServerInitializeECSWorlds()
        {
            if (_initialized || DefaultWorld != null)
                throw new InvalidOperationException("The server ECS world is already initialized.");
            if (!Game.Shared.GameConfigurationManager.IsInitialized)
                throw new InvalidOperationException("The game configuration must be initialized before the server ECS world.");

            ServerSimulationClock.ValidateConfiguration(Game.Shared.GameConfigurationManager.Config);

            using var markerScope = ECS_INITIALIZATION_MARKER.Begin();
            World world = null;
            try
            {
                world = new World("Default", WorldFlags.GameServer);

                // Create the main simulation system group.
                var systemGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();

                // Create and add systems to the world.
                var testSystem = world.GetOrCreateSystem<WorldSpawnSystem>();
                systemGroup.AddSystemToUpdateList(testSystem);

                var actorUpdateSystem = world.GetOrCreateSystem<ActorUpdateSystem>();
                systemGroup.AddSystemToUpdateList(actorUpdateSystem);

                var resourceRegenSystem = world.GetOrCreateSystem<ResourceRegenSystem>();
                systemGroup.AddSystemToUpdateList(resourceRegenSystem);

                var spellCastSystem = world.GetOrCreateSystem<SpellCastSystem>();
                systemGroup.AddSystemToUpdateList(spellCastSystem);

                var cooldownTickSystem = world.GetOrCreateSystem<CooldownTickSystem>();
                systemGroup.AddSystemToUpdateList(cooldownTickSystem);

                var auraTickSystem = world.GetOrCreateSystem<AuraTickSystem>();
                systemGroup.AddSystemToUpdateList(auraTickSystem);

                var actorTickerSystem = world.GetOrCreateSystem<ActorTickerSystem>();
                systemGroup.AddSystemToUpdateList(actorTickerSystem);

                DefaultWorld = world;
                _simulationSystemGroup = systemGroup;
                _initialized = true;
            }
            catch
            {
                if (world != null && world.IsCreated)
                    world.Dispose();
                throw;
            }
        }

        /// <summary>Runs the owned simulation group once for one authoritative tick.</summary>
        internal static void UpdateSimulation(ServerSimulationTick tick)
        {
            if (!_initialized || DefaultWorld == null || _simulationSystemGroup == null)
                throw new InvalidOperationException("The server ECS world is not ready for simulation.");

            DefaultWorld.SetTime(new TimeData(tick.ElapsedTime, tick.DeltaTime));
            _simulationSystemGroup.Update();
        }

        /// <summary>Records the measured CPU duration of one complete authoritative tick.</summary>
        internal static void RecordSimulationTickDuration(long tickID, double milliseconds, double intervalSeconds)
        {
            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(
                    SIMULATION_TICK_DURATION_METRIC,
                    milliseconds,
                    tickID,
                    intervalSeconds);
            }
        }

        /// <summary>Records per-frame scheduling, execution, backlog and overload observations.</summary>
        internal static void RecordSimulationFrameMetrics(
            long scheduledTicks,
            long executedTicks,
            long backlogTicks,
            long deadlineMisses,
            bool catchUpCapHit)
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(SIMULATION_TICK_BACKLOG_METRIC, backlogTicks);
            PerformanceTelemetry.TryRecord(SIMULATION_TICKS_SCHEDULED_METRIC, scheduledTicks);
            PerformanceTelemetry.TryRecord(SIMULATION_TICKS_EXECUTED_METRIC, executedTicks);
            PerformanceTelemetry.TryRecord(SIMULATION_DEADLINE_MISS_COUNT_METRIC, deadlineMisses);
            PerformanceTelemetry.TryRecord(SIMULATION_CATCH_UP_CAP_HIT_COUNT_METRIC, catchUpCapHit ? 1 : 0);
        }

        /// <summary>Stops the simulation clock owner and disposes the dedicated server ECS world.</summary>
        internal static void Shutdown()
        {
            World world = DefaultWorld;
            DefaultWorld = null;
            _simulationSystemGroup = null;
            _initialized = false;

            if (world == null || !world.IsCreated)
                return;

            if (ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(world))
                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);

            world.Dispose();
        }
    }
}
