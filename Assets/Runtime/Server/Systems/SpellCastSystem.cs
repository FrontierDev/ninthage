using Game.Server.Core;
using Game.Shared.Performance;
using System;
using System.Globalization;
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
            float fixedDeltaTime = SystemAPI.Time.DeltaTime;
            long currentTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentTickID <= 0)
                throw new InvalidOperationException("Spell casts require an active authoritative simulation tick.");

            foreach (var (spellCast, actor, entity) in
                SystemAPI.Query<RefRW<SpellCastComponent>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                if (telemetryEnabled)
                    activeCastCount++;

                long completionTickID = spellCast.ValueRO.CompletionTickID;
                if (completionTickID == 0)
                {
                    long durationTicks = GetNonShorteningDurationTicks(
                        spellCast.ValueRO.TotalCastTime,
                        fixedDeltaTime);
                    completionTickID = checked(currentTickID + durationTicks - 1L);
                    spellCast.ValueRW.CompletionTickID = completionTickID;
                }

                float remainingCastTimeBeforeUpdate = spellCast.ValueRO.RemainingCastTime;
                float remainingCastTime = remainingCastTimeBeforeUpdate - fixedDeltaTime;
                spellCast.ValueRW.RemainingCastTime = remainingCastTime;

                // Channel tick logic
                if (spellCast.ValueRO.TotalTicks > 0f)
                {
                    float tickInterval = spellCast.ValueRO.TotalCastTime / spellCast.ValueRO.TotalTicks;
                    if (tickInterval > 0f)
                    {
                        float channelDelta = remainingCastTimeBeforeUpdate > 0f
                            ? remainingCastTimeBeforeUpdate < fixedDeltaTime
                                ? remainingCastTimeBeforeUpdate
                                : fixedDeltaTime
                            : 0f;
                        float timeSinceLastTick = spellCast.ValueRO.TimeSinceLastTick + channelDelta;

                        while (timeSinceLastTick >= tickInterval)
                        {
                            timeSinceLastTick -= tickInterval;

                            ServerECSManager.QueueSimulationEvent(
                                ServerSimulationEventKind.SpellCastTick,
                                actor.ValueRO.ActorID,
                                entity,
                                spellCast.ValueRO.SpellID,
                                castID: spellCast.ValueRO.CastID);
                        }

                        spellCast.ValueRW.TimeSinceLastTick = timeSinceLastTick;
                    }
                }

                if (currentTickID >= completionTickID)
                {
                    spellCast.ValueRW.RemainingCastTime = 0f;

                    // Disable the component (cast finished)
                    state.EntityManager.SetComponentEnabled<SpellCastComponent>(entity, false);

                    // Queue an event so the MonoBehaviour layer can resolve the spell after this traversal.
                    ServerECSManager.QueueSimulationEvent(
                        ServerSimulationEventKind.SpellCastCompleted,
                        actor.ValueRO.ActorID,
                        entity,
                        spellCast.ValueRO.SpellID,
                        castID: spellCast.ValueRO.CastID);
                }
            }
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(ACTIVE_CAST_COUNT_METRIC, activeCastCount);
        }

        private static long GetNonShorteningDurationTicks(float duration, float fixedDeltaTime)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new InvalidOperationException("An active spell cast requires a finite positive duration.");
            if (float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime) || fixedDeltaTime <= 0f)
                throw new InvalidOperationException("Spell casts require a finite positive simulation tick duration.");

            // Round-trip float values through decimal before ceiling so a nominal multiple is not
            // pushed to a later tick by binary noise, while a just-above-boundary duration is not shortened.
            decimal durationSeconds = decimal.Parse(
                duration.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            decimal tickDurationSeconds = decimal.Parse(
                fixedDeltaTime.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            long durationTicks = checked((long)Math.Ceiling(durationSeconds / tickDurationSeconds));
            return Math.Max(1L, durationTicks);
        }
    }
}
