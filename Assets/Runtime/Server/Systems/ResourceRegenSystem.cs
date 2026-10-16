using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Processes server-side resource regeneration.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(ActorUpdateSystem))]
    public partial struct ResourceRegenSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition RESOURCE_REGEN_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_RESOURCE_REGEN_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.resource_regen",
            "server_update");
        private static readonly PerformanceMarker RESOURCE_REGEN_MARKER = new(RESOURCE_REGEN_DURATION_METRIC, "Server.ECS.ResourceRegen");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(RESOURCE_REGEN_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        /// <summary>Initializes resource-regeneration system state.</summary>
        public readonly void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ResourceRegenSystem.");
        }

        /// <summary>Updates resource regeneration for the current server tick.</summary>
        public readonly void OnUpdate(ref SystemState state)
        {
            using var markerScope = RESOURCE_REGEN_MARKER.Begin();
            // Main update logic for the system.
        }

        /// <summary>Releases resource-regeneration system state.</summary>
        public readonly void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed ResourceRegenSystem.");
        }
    }
}
