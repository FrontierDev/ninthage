using Game.Shared.Networking;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Advances active aura durations and invokes aura callbacks.</summary>
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

        /// <summary>Processes aura ticks and expirations for server actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = AURA_TICK_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int auraEntryCount = 0;
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (_, actor, entity) in
                SystemAPI.Query<DynamicBuffer<AuraElement>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<AuraElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.RemainingDuration -= dt;
                    entry.TimeSinceLastTick += dt;

                    if (entry.TickInterval > 0f && entry.TimeSinceLastTick >= entry.TickInterval)
                    {
                        entry.TimeSinceLastTick -= entry.TickInterval;

                        ActorEndpoint.onServerAuraTick?.Invoke(
                            actor.ValueRO.ActorID,
                            entry.AuraDefinitionID.ToString(),
                            entry.InstanceID);
                    }

                    if (entry.RemainingDuration <= 0f)
                    {
                        ActorEndpoint.onServerAuraExpired?.Invoke(
                            actor.ValueRO.ActorID,
                            entry.AuraDefinitionID.ToString(),
                            entry.InstanceID);

                        buffer.RemoveAt(i);
                    }
                    else
                    {
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
