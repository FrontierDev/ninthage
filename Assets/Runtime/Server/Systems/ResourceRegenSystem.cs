using Unity.Entities;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.ECS
{
    [DisableAutoCreation]
    public partial struct ResourceRegenSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ResourceRegenSystem.");
        }

        public readonly void OnUpdate(ref SystemState state)
        {
            // Main update logic for the system.
        }

        public readonly void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed ResourceRegenSystem.");
        }
    }
}