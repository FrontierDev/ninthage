using Debug = Game.Shared.Utility.FormattedDebug;
using Unity.Entities;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientECSManager</c> implementation.
    /// </summary>
    public static class ClientECSManager
    {
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        /// <summary>
        /// Executes the client-side <c>ClientInitializeECSWorlds</c> operation.
        /// </summary>
        public static void ClientInitializeECSWorlds()
        {
            // Create the world.
            var defaultWorld = new World("Default");

            // Create the main simulation system group.
            var systemGroup = defaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Create and add systems to the world.
            var testSystem = defaultWorld.GetOrCreateSystem<Test_ClientSystem>();
            systemGroup.AddSystemToUpdateList(testSystem);

            _initialized = true;
        }
    }
}
