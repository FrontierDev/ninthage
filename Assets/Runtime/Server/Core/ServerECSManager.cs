using UnityEngine;
using Unity.Entities;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.ECS
{
    public static class ServerECSManager
    {
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        public static World DefaultWorld { get; private set; }

        public static void ServerInitializeECSWorlds()
        {
            // Create the world.
            DefaultWorld = new World("Default");

            // Create the main simulation system group.
            var systemGroup = DefaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Create and add systems to the world.
            var testSystem = DefaultWorld.GetOrCreateSystem<WorldSpawnSystem>();
            systemGroup.AddSystemToUpdateList(testSystem);

            var actorUpdateSystem = DefaultWorld.GetOrCreateSystem<ActorUpdateSystem>();
            systemGroup.AddSystemToUpdateList(actorUpdateSystem);

            var resourceRegenSystem = DefaultWorld.GetOrCreateSystem<ResourceRegenSystem>();
            systemGroup.AddSystemToUpdateList(resourceRegenSystem);

            var spellCastSystem = DefaultWorld.GetOrCreateSystem<SpellCastSystem>();
            systemGroup.AddSystemToUpdateList(spellCastSystem);

            var cooldownTickSystem = DefaultWorld.GetOrCreateSystem<CooldownTickSystem>();
            systemGroup.AddSystemToUpdateList(cooldownTickSystem);

            var auraTickSystem = DefaultWorld.GetOrCreateSystem<AuraTickSystem>();
            systemGroup.AddSystemToUpdateList(auraTickSystem);

            var actorTickerSystem = DefaultWorld.GetOrCreateSystem<ActorTickerSystem>();
            systemGroup.AddSystemToUpdateList(actorTickerSystem);

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(DefaultWorld);

            _initialized = true;
        }
    }
}