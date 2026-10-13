using Game.Server.Networking;

namespace Game.Server.Core
{
    /// <summary>
    /// Registers server bindings for shared endpoint callbacks.
    /// </summary>
    public static class ServerBindings
    {
        private static bool _initialized = false;

        /// <summary>
        /// Gets whether server bindings have completed registration.
        /// </summary>
        public static bool Initialized => _initialized;

        /// <summary>
        /// Registers each server binding once.
        /// </summary>
        public static void Initialize()
        {
            if (Initialized)
                return;

            AccountBindings.Register();
            ActorDeathBindings.Register();

            _initialized = true;
        }
    }
}
