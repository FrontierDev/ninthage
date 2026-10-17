using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Systems;
using Game.Shared.Networking;
using Game.Shared.Performance;
using System;
using Unity.Collections;
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
        private static readonly ServerSimulationEventBuffer SIMULATION_EVENT_BUFFER = new();
        private static bool _initialized = false;
        private static bool _mutationCommitActive;
        private static long _currentSimulationTickID;
        private static bool _hasCurrentSimulationEvent;
        private static ServerSimulationEvent _currentSimulationEvent;
        /// <summary>Gets whether the server ECS world has been initialized.</summary>
        public static bool Initialized => _initialized;

        /// <summary>Gets the authoritative tick currently being simulated.</summary>
        internal static long CurrentSimulationTickID => _currentSimulationTickID;

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
            SIMULATION_EVENT_BUFFER.Clear();
            _mutationCommitActive = false;
            _currentSimulationTickID = 0;
            _hasCurrentSimulationEvent = false;
            _currentSimulationEvent = default;

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

                // Apply the explicit phase dependencies before the first simulation tick.
                systemGroup.SortSystems();

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

            SIMULATION_EVENT_BUFFER.BeginTick(tick.TickID);
            _currentSimulationTickID = tick.TickID;
            bool tickCompleted = false;
            try
            {
                CommitPendingMutations();
                ServerActorManager.Instance?.ProcessAuthoritativeTick(tick);
                DefaultWorld.SetTime(new TimeData(tick.ElapsedTime, tick.DeltaTime));
                _simulationSystemGroup.Update();
                ServerActorManager.Instance?.ProcessAuthoritativeTickCompleted();
                // ECS structural work is complete; domain lifecycle work is committed once
                // from this tick-owned boundary before deferred event delivery.
                CommitSpawnCommands();
                SIMULATION_EVENT_BUFFER.Dispatch(DispatchSimulationEvent);
                tickCompleted = true;
            }
            finally
            {
                if (tickCompleted)
                    SIMULATION_EVENT_BUFFER.EndTick();
                else
                    SIMULATION_EVENT_BUFFER.Clear();
                _mutationCommitActive = false;
                _currentSimulationTickID = 0;
                _hasCurrentSimulationEvent = false;
                _currentSimulationEvent = default;
            }
        }

        /// <summary>Queues one immutable ECS-to-domain event for the current authoritative tick.</summary>
        internal static void QueueSimulationEvent(
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID = default,
            int operationID = 0,
            long castID = 0)
        {
            if (!_initialized || _currentSimulationTickID <= 0)
                throw new InvalidOperationException("Server simulation events require an active authoritative tick.");

            if (_mutationCommitActive)
            {
                SIMULATION_EVENT_BUFFER.EnqueueForNextTick(
                    kind,
                    actorID,
                    sourceEntity,
                    definitionID,
                    operationID,
                    castID);
            }
            else
            {
                SIMULATION_EVENT_BUFFER.Enqueue(kind, actorID, sourceEntity, definitionID, operationID, castID);
            }
        }

        /// <summary>Gets whether callbacks must hand off ECS mutations to the next commit boundary.</summary>
        internal static bool IsMutationBoundaryActive => _mutationCommitActive || SIMULATION_EVENT_BUFFER.IsDispatching;

        /// <summary>Gets the event currently being delivered, when domain delivery is active.</summary>
        internal static bool TryGetCurrentSimulationEvent(out ServerSimulationEvent simulationEvent)
        {
            simulationEvent = _currentSimulationEvent;
            return _hasCurrentSimulationEvent;
        }

        /// <summary>Rejects buffered events for an actor whose authoritative lifetime ended.</summary>
        internal static void InvalidateActorSimulationEvents(Guid actorID)
        {
            SIMULATION_EVENT_BUFFER.CancelActor(actorID);
        }

        /// <summary>Allows a newly authoritative actor identity to use the event buffer again.</summary>
        internal static void ClearActorSimulationEventCancellation(Guid actorID)
        {
            SIMULATION_EVENT_BUFFER.ClearActorCancellation(actorID);
        }

        private static void CommitPendingMutations()
        {
            _mutationCommitActive = true;
            try
            {
                ServerSpellCastManager.Instance?.PreparePendingMutations();
                ServerAuraManager.Instance?.PreparePendingMutations();
                ServerTickerManager.Instance?.PreparePendingMutations();
                ServerCooldownManager.Instance?.PreparePendingMutations();
                ServerCooldownManager.Instance?.CommitPendingMutations();
                ServerSpellCastManager.Instance?.CommitPendingMutations();
                ServerAuraManager.Instance?.CommitPendingMutations();
                ServerTickerManager.Instance?.CommitPendingMutations();
            }
            finally
            {
                _mutationCommitActive = false;
            }
        }

        private static void CommitSpawnCommands()
        {
            _mutationCommitActive = true;
            try
            {
                ServerSpawnManager.Instance?.CommitSpawnCommands();
            }
            finally
            {
                _mutationCommitActive = false;
            }
        }

        private static void DispatchSimulationEvent(ServerSimulationEvent simulationEvent)
        {
            if (!IsLiveSimulationEvent(simulationEvent))
                return;

            _currentSimulationEvent = simulationEvent;
            _hasCurrentSimulationEvent = true;
            try
            {
                switch (simulationEvent.Kind)
                {
                    case ServerSimulationEventKind.SpellCastTick:
                        ActorEndpoint.onServerSpellCastTick?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.DefinitionID.ToString());
                        break;
                    case ServerSimulationEventKind.SpellCastCompleted:
                        ActorEndpoint.onServerSpellCastCompleted?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.DefinitionID.ToString());
                        break;
                    case ServerSimulationEventKind.AuraTick:
                        ActorEndpoint.onServerAuraTick?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.DefinitionID.ToString(),
                            simulationEvent.OperationID);
                        break;
                    case ServerSimulationEventKind.AuraExpired:
                        ActorEndpoint.onServerAuraExpired?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.DefinitionID.ToString(),
                            simulationEvent.OperationID);
                        break;
                    case ServerSimulationEventKind.ActorTickerTick:
                        ActorEndpoint.onServerActorTick?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.OperationID);
                        break;
                    case ServerSimulationEventKind.ActorTickerExpired:
                        ActorEndpoint.onServerActorTickerExpired?.Invoke(
                            simulationEvent.ActorID,
                            simulationEvent.OperationID);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(simulationEvent.Kind),
                            simulationEvent.Kind,
                            "Unknown server simulation event kind.");
                }
            }
            finally
            {
                _hasCurrentSimulationEvent = false;
                _currentSimulationEvent = default;
            }
        }

        private static bool IsLiveSimulationEvent(ServerSimulationEvent simulationEvent)
        {
            if (DefaultWorld == null || !DefaultWorld.IsCreated)
                return false;

            if (ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActor(simulationEvent.ActorID, out var actor)
                || actor == null
                || actor.ID != simulationEvent.ActorID
                || !ServerActorManager.Instance.TryGetActorEntity(simulationEvent.ActorID, out Entity authoritativeEntity)
                || authoritativeEntity != simulationEvent.SourceEntity)
            {
                return false;
            }

            EntityManager entityManager = DefaultWorld.EntityManager;
            if (!entityManager.Exists(simulationEvent.SourceEntity)
                || !entityManager.HasComponent<ActorComponent>(simulationEvent.SourceEntity))
            {
                return false;
            }

            if (entityManager.GetComponentData<ActorComponent>(simulationEvent.SourceEntity).ActorID != simulationEvent.ActorID)
                return false;

            if (simulationEvent.Kind == ServerSimulationEventKind.SpellCastTick
                || simulationEvent.Kind == ServerSimulationEventKind.SpellCastCompleted)
            {
                if (!entityManager.HasComponent<SpellCastComponent>(simulationEvent.SourceEntity))
                    return false;

                var spellCast = entityManager.GetComponentData<SpellCastComponent>(simulationEvent.SourceEntity);
                return spellCast.CastID == simulationEvent.CastID
                    && spellCast.SpellID == simulationEvent.DefinitionID;
            }

            return true;
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
            SIMULATION_EVENT_BUFFER.Clear();
            _mutationCommitActive = false;
            _currentSimulationTickID = 0;
            _hasCurrentSimulationEvent = false;
            _currentSimulationEvent = default;
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
