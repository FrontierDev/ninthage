using Game.Shared.Networking;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Processes time-based actor ticker callbacks.</summary>
    [DisableAutoCreation]
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
            PerformanceTelemetry.RegisterMetric(ACTOR_TICKER_DURATION_METRIC, 4096);
            PerformanceTelemetry.RegisterMetric(TICKER_ENTRY_COUNT_METRIC, 4096);
        }

        /// <summary>Advances active actor tickers and invokes their callbacks.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = ACTOR_TICKER_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int tickerEntryCount = 0;
            float dt = UnityEngine.Time.deltaTime;

            foreach (var (actor, entity) in
                SystemAPI.Query<RefRO<ActorComponent>>().WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<TickerElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.TimeSinceLastTick += dt;

                    if (entry.RemainingDuration != float.MaxValue)
                        entry.RemainingDuration -= dt;

                    if (entry.TickInterval > 0f && entry.TimeSinceLastTick >= entry.TickInterval)
                    {
                        entry.TimeSinceLastTick -= entry.TickInterval;

                        ActorEndpoint.onServerActorTick?.Invoke(
                            actor.ValueRO.ActorID,
                            entry.TickerID);
                    }

                    if (entry.RemainingDuration != float.MaxValue && entry.RemainingDuration <= 0f)
                    {
                        ActorEndpoint.onServerActorTickerExpired?.Invoke(
                            actor.ValueRO.ActorID,
                            entry.TickerID);

                        buffer.RemoveAt(i);
                    }
                    else
                    {
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
