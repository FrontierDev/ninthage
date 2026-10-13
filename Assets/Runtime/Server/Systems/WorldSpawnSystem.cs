using Debug = Game.Shared.Utility.FormattedDebug;
using Unity.Entities;

namespace Game.Server.ECS
{
    /// <summary>Converts pending spawn points into server spawn commands.</summary>
    [DisableAutoCreation]
    public partial struct WorldSpawnSystem : ISystem
    {
        /// <summary>Initializes world-spawn system state.</summary>
        public void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created WorldSpawnSystem.");
        }

        /// <summary>Creates spawn commands for pending spawn points.</summary>
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (spawnPoint, entity) in
                SystemAPI.Query<RefRW<SpawnPointComponent>>().WithEntityAccess())
            {
                if (spawnPoint.ValueRO.Pending)
                {
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
        }

        /// <summary>Releases world-spawn system state.</summary>
        public void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed WorldSpawnSystem.");
        }
    }
}
