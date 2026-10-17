using Game.Server.Core;
using Game.Shared.Performance;
using System;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Advances active aura deadlines and queues aura events.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(CooldownTickSystem))]
    public partial struct AuraTickSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition AURA_TICK_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_AURA_TICK_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.aura_tick",
            "server_update");
        private static readonly PerformanceMetricDefinition AURA_ENTRY_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_AURA_ENTRY_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "ecs.aura_tick",
            "server_update");
        private static readonly PerformanceMarker AURA_TICK_MARKER = new(AURA_TICK_DURATION_METRIC, "Server.ECS.AuraTick");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(AURA_TICK_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(AURA_ENTRY_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Processes aura deadlines and queues ticks and expirations for server actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = AURA_TICK_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int auraEntryCount = 0;
            long currentTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentTickID <= 0)
                throw new InvalidOperationException("Aura timing requires an active authoritative simulation tick.");

            float fixedDeltaTime = SystemAPI.Time.DeltaTime;
            if (float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime) || fixedDeltaTime <= 0f)
                throw new InvalidOperationException("Aura timing requires a finite positive simulation tick duration.");

            foreach (var (_, actor, entity) in
                SystemAPI.Query<DynamicBuffer<AuraElement>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<AuraElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    if (entry.ExpiryTickID <= 0)
                        throw new InvalidOperationException(
                            $"Aura instance {entry.InstanceID} on actor {actor.ValueRO.ActorID} has no authoritative expiry deadline.");
                    if (entry.TickIntervalTicks < 0
                        || entry.TickIntervalTicks == 0 && entry.NextTickID != 0
                        || entry.TickIntervalTicks > 0 && entry.NextTickID <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Aura instance {entry.InstanceID} on actor {actor.ValueRO.ActorID} has invalid authoritative tick state.");
                    }

                    while (entry.TickIntervalTicks > 0
                        && entry.NextTickID <= currentTickID
                        && entry.NextTickID <= entry.ExpiryTickID)
                    {
                        ServerECSManager.QueueSimulationEvent(
                            ServerSimulationEventKind.AuraTick,
                            actor.ValueRO.ActorID,
                            entity,
                            entry.AuraDefinitionID,
                            entry.InstanceID);

                        entry.NextTickID = checked(entry.NextTickID + entry.TickIntervalTicks);
                    }

                    if (currentTickID >= entry.ExpiryTickID)
                    {
                        buffer.RemoveAt(i);
                        ServerECSManager.QueueSimulationEvent(
                            ServerSimulationEventKind.AuraExpired,
                            actor.ValueRO.ActorID,
                            entity,
                            entry.AuraDefinitionID,
                            entry.InstanceID);
                    }
                    else
                    {
                        long remainingTicks = checked(entry.ExpiryTickID - currentTickID);
                        entry.RemainingDuration = (float)(remainingTicks * (double)fixedDeltaTime);

                        if (entry.TickIntervalTicks == 0)
                        {
                            entry.TimeSinceLastTick = 0f;
                        }
                        else
                        {
                            long lastTickBoundary = checked(entry.NextTickID - entry.TickIntervalTicks);
                            long elapsedTicks = checked(currentTickID - lastTickBoundary);
                            if (elapsedTicks < 0 || elapsedTicks >= entry.TickIntervalTicks)
                            {
                                throw new InvalidOperationException(
                                    $"Aura instance {entry.InstanceID} on actor {actor.ValueRO.ActorID} has inconsistent tick progress.");
                            }

                            entry.TimeSinceLastTick = (float)(elapsedTicks * (double)fixedDeltaTime);
                        }

                        buffer[i] = entry;
                    }
                }
                if (telemetryEnabled)
                    auraEntryCount += buffer.Length;
            }
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(AURA_ENTRY_COUNT_METRIC, auraEntryCount);
        }
    }
}
