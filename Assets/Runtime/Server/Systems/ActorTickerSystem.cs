using Game.Server.Core;
using Game.Shared.Performance;
using System;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Processes authoritative actor ticker deadlines and queues callbacks.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(AuraTickSystem))]
    public partial struct ActorTickerSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition ACTOR_TICKER_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_ACTOR_TICKER_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.actor_ticker",
            "server_update");
        private static readonly PerformanceMetricDefinition TICKER_ENTRY_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_TICKER_ENTRY_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "ecs.actor_ticker",
            "server_update");
        private static readonly PerformanceMarker ACTOR_TICKER_MARKER = new(ACTOR_TICKER_DURATION_METRIC, "Server.ECS.ActorTicker");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(ACTOR_TICKER_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(TICKER_ENTRY_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Advances active actor ticker deadlines and queues their callbacks.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = ACTOR_TICKER_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int tickerEntryCount = 0;
            long currentTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentTickID <= 0)
                throw new InvalidOperationException("Actor ticker timing requires an active authoritative simulation tick.");

            float fixedDeltaTime = SystemAPI.Time.DeltaTime;
            if (float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime) || fixedDeltaTime <= 0f)
                throw new InvalidOperationException("Actor ticker timing requires a finite positive simulation tick duration.");

            foreach (var (actor, entity) in
                SystemAPI.Query<RefRO<ActorComponent>>().WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<TickerElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    if (entry.RemainingDuration == float.MaxValue)
                    {
                        if (entry.ExpiryTickID != 0)
                            throw new InvalidOperationException(
                                $"Ticker {entry.TickerID} on actor {actor.ValueRO.ActorID} has an expiry deadline despite being indefinite.");
                    }
                    else if (entry.ExpiryTickID <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Ticker {entry.TickerID} on actor {actor.ValueRO.ActorID} has no authoritative expiry deadline.");
                    }
                    if (entry.TickIntervalTicks < 0
                        || entry.TickIntervalTicks == 0 && entry.NextTickID != 0
                        || entry.TickIntervalTicks > 0 && entry.NextTickID <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Ticker {entry.TickerID} on actor {actor.ValueRO.ActorID} has invalid authoritative tick state.");
                    }

                    while (entry.TickIntervalTicks > 0
                        && entry.NextTickID <= currentTickID
                        && (entry.ExpiryTickID == 0 || entry.NextTickID <= entry.ExpiryTickID))
                    {
                        ServerECSManager.QueueSimulationEvent(
                            ServerSimulationEventKind.ActorTickerTick,
                            actor.ValueRO.ActorID,
                            entity,
                            operationID: entry.TickerID);

                        entry.NextTickID = checked(entry.NextTickID + entry.TickIntervalTicks);
                    }

                    if (entry.ExpiryTickID != 0 && currentTickID >= entry.ExpiryTickID)
                    {
                        buffer.RemoveAt(i);
                        ServerECSManager.QueueSimulationEvent(
                            ServerSimulationEventKind.ActorTickerExpired,
                            actor.ValueRO.ActorID,
                            entity,
                            operationID: entry.TickerID);
                    }
                    else
                    {
                        if (entry.RemainingDuration != float.MaxValue)
                        {
                            long remainingTicks = checked(entry.ExpiryTickID - currentTickID);
                            entry.RemainingDuration = (float)(remainingTicks * (double)fixedDeltaTime);
                        }

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
                                    $"Ticker {entry.TickerID} on actor {actor.ValueRO.ActorID} has inconsistent tick progress.");
                            }

                            entry.TimeSinceLastTick = (float)(elapsedTicks * (double)fixedDeltaTime);
                        }

                        buffer[i] = entry;
                    }
                }
                if (telemetryEnabled)
                    tickerEntryCount += buffer.Length;
            }
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(TICKER_ENTRY_COUNT_METRIC, tickerEntryCount);
        }
    }
}
