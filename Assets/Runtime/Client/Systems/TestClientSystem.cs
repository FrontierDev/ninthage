using Unity.Entities;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client
{
    [DisableAutoCreation]
    public partial struct TestClientSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created Test Client System.");
        }

        public readonly void OnUpdate(ref SystemState state)
        {
            // Main update logic for the system.
        }

        public readonly void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed Test Client System.");
        }
    }
}