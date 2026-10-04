using System.Collections;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using CharacterData = Game.Shared.Persistence.CharacterData;
using PurrNet;
using Game.Server.Networking;

namespace Game.Server
{
    public static class ServerHooksManager
    {
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        public static void ServerInitializeRPCHooks()
        {
            AccountServiceHooks.RegisterHooks();
            ActorDeathHooks.RegisterHooks();

            _initialized = true;
        }
    }
}