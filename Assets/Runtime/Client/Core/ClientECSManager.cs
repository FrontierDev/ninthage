using UnityEngine;
using Unity.Entities;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client
{
    public static class ClientECSManager
    {
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        public static void ClientInitializeECSWorlds()
        {
            // Create the world.
            var defaultWorld = new World("Default");

            // Create the main simulation system group.
            var systemGroup = defaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Create and add systems to the world.
            var testSystem = defaultWorld.GetOrCreateSystem<TestClientSystem>();
            systemGroup.AddSystemToUpdateList(testSystem);

            _initialized = true;
        }
    }
}