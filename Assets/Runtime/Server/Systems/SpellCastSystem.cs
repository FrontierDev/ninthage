using Game.Server.Core;
using Game.Shared.Performance;
using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Systems
{
    /// <summary>Advances active spell casts and invokes spell callbacks.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(ActorUpdateSystem))]
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
            var telemetryEnabled = PerformanceTelemetry.IsEnabled;
            var activeCastCount = 0;
            var fixedDeltaTime = SystemAPI.Time.DeltaTime;
            var currentTickID = ServerECSManager.CurrentSimulationTickID;

            foreach (var (spellCast, actor, entity) in
                SystemAPI.Query<RefRW<SpellCastComponent>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                if (telemetryEnabled)
                    activeCastCount++;

                var completionTickID = spellCast.ValueRO.CompletionTickID;

                // Channel tick logic
                if (spellCast.ValueRO.TotalTicks > 0f)
                {
                    float tickInterval = spellCast.ValueRO.TotalCastTime / spellCast.ValueRO.TotalTicks;
                    if (tickInterval > 0f)
                    {
                        var completedBeforeCurrent = Math.Max(
                            0L,
                            currentTickID - spellCast.ValueRO.FirstProgressTickID);
                        var elapsedCastSeconds = completedBeforeCurrent
                            * (double)fixedDeltaTime;
                        var remainingBeforeCurrent = Mathf.Max(
                            0f,
                            spellCast.ValueRO.TotalCastTime - (float)elapsedCastSeconds);
                        var channelDelta = Mathf.Min(remainingBeforeCurrent, fixedDeltaTime);
                        var timeSinceLastTick = spellCast.ValueRO.TimeSinceLastTick + channelDelta;

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

    }
}
