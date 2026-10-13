using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Converts pending spawn points into server spawn commands.</summary>
    [DisableAutoCreation]
    public partial struct WorldSpawnSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition WORLD_SPAWN_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_WORLD_SPAWN_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.world_spawn",
            "server_update");
        private static readonly PerformanceMetricDefinition SPAWN_COMMAND_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_SPAWN_COMMAND_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "ecs.world_spawn",
            "server_update");
        private static readonly PerformanceMarker WORLD_SPAWN_MARKER = new(WORLD_SPAWN_DURATION_METRIC, "Server.ECS.WorldSpawn");

        /// <summary>Initializes world-spawn system state.</summary>
        public void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created WorldSpawnSystem.");
        }

        /// <summary>Creates spawn commands for pending spawn points.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = WORLD_SPAWN_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int spawnCommandCount = 0;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (spawnPoint, entity) in
                SystemAPI.Query<RefRW<SpawnPointComponent>>().WithEntityAccess())
            {
                if (spawnPoint.ValueRO.Pending)
                {
                    if (telemetryEnabled)
                        spawnCommandCount++;
                    // Create a spawn command entity for the bridge to consume
                    var cmd = ecb.CreateEntity();
                    ecb.AddComponent(cmd, new SpawnCommand
                    {
                        Position = spawnPoint.ValueRO.Position,
                        PrefabIndex = spawnPoint.ValueRO.PrefabIndex,
                        SceneIndex = spawnPoint.ValueRO.SceneIndex,
                        SceneName = spawnPoint.ValueRO.SceneName,
                    });

                    spawnPoint.ValueRW.Pending = false;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, spawnCommandCount);
        }

        /// <summary>Releases world-spawn system state.</summary>
        public void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed WorldSpawnSystem.");
        }
    }
}
