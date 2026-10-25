using Game.Server.Core;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Advances and removes server actor cooldown entries.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(SpellCastSystem))]
    public partial struct CooldownTickSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition COOLDOWN_TICK_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_COOLDOWN_TICK_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.cooldown_tick",
            "server_update");
        private static readonly PerformanceMetricDefinition COOLDOWN_ENTRY_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_COOLDOWN_ENTRY_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "ecs.cooldown_tick",
            "server_update");
        private static readonly PerformanceMarker COOLDOWN_TICK_MARKER = new(COOLDOWN_TICK_DURATION_METRIC, "Server.ECS.CooldownTick");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(COOLDOWN_TICK_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(COOLDOWN_ENTRY_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Updates active cooldown durations for all server actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = COOLDOWN_TICK_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int cooldownEntryCount = 0;
            float fixedDeltaTime = SystemAPI.Time.DeltaTime;
            ServerActorManager actorManager = ServerActorManager.Instance;
            if (actorManager == null)
            {
                if (telemetryEnabled)
                    PerformanceTelemetry.TryRecord(COOLDOWN_ENTRY_COUNT_METRIC, 0);
                return;
            }

            foreach (var (queriedBuffer, actor, entity) in
                SystemAPI.Query<DynamicBuffer<CooldownElement>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                DynamicBuffer<CooldownElement> buffer = queriedBuffer;
                if (!actorManager.TryGetActorEntity(actor.ValueRO.ActorID, out Entity authoritativeEntity)
                    || authoritativeEntity != entity)
                {
                    continue;
                }

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.RemainingCooldown -= fixedDeltaTime;

                    if (entry.RemainingCooldown <= 0f)
                        buffer.RemoveAt(i);
                    else
                        buffer[i] = entry;
                }
                if (telemetryEnabled)
                    cooldownEntryCount += buffer.Length;
            }
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(COOLDOWN_ENTRY_COUNT_METRIC, cooldownEntryCount);
        }
    }
}
