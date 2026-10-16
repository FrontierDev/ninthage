using Game.Shared.Networking;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Advances active spell casts and invokes spell callbacks.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(ResourceRegenSystem))]
    public partial struct SpellCastSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition SPELL_CAST_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_SPELL_CAST_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.spell_cast",
            "server_update");
        private static readonly PerformanceMetricDefinition ACTIVE_CAST_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_ACTIVE_CAST_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "ecs.spell_cast",
            "server_update");
        private static readonly PerformanceMarker SPELL_CAST_MARKER = new(SPELL_CAST_DURATION_METRIC, "Server.ECS.SpellCast");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(SPELL_CAST_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(ACTIVE_CAST_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Updates active spell casts for the current server tick.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = SPELL_CAST_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int activeCastCount = 0;
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (spellCast, actor, entity) in
                SystemAPI.Query<RefRW<SpellCastComponent>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                if (telemetryEnabled)
                    activeCastCount++;
                spellCast.ValueRW.RemainingCastTime -= dt;

                // Channel tick logic
                if (spellCast.ValueRO.TotalTicks > 0f)
                {
                    float tickInterval = spellCast.ValueRO.TotalCastTime / spellCast.ValueRO.TotalTicks;
                    spellCast.ValueRW.TimeSinceLastTick += dt;

                    if (spellCast.ValueRO.TimeSinceLastTick >= tickInterval)
                    {
                        spellCast.ValueRW.TimeSinceLastTick -= tickInterval;

                        ActorEndpoint.onServerSpellCastTick?.Invoke(
                            actor.ValueRO.ActorID,
                            spellCast.ValueRO.SpellID.ToString());
                    }
                }

                if (spellCast.ValueRO.RemainingCastTime <= 0f)
                {
                    // Disable the component (cast finished)
                    state.EntityManager.SetComponentEnabled<SpellCastComponent>(entity, false);

                    // Fire an event so the MonoBehaviour layer can resolve the spell
                    ActorEndpoint.onServerSpellCastCompleted?.Invoke(
                        actor.ValueRO.ActorID,
                        spellCast.ValueRO.SpellID.ToString());
                }
            }
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(ACTIVE_CAST_COUNT_METRIC, activeCastCount);
        }
    }
}
