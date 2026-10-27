using PurrNet;
using System;

namespace Game.Shared.Networking
{
    /// <summary>Provides the shared read-only network-pressure query boundary.</summary>
    public static class NetworkPressureQuery
    {
        private static INetworkPressureProvider _provider;

        /// <summary>Registers the sole network-pressure provider.</summary>
        public static void Register(INetworkPressureProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (_provider == null)
            {
                _provider = provider;
                return;
            }
            if (ReferenceEquals(_provider, provider))
                return;

            throw new InvalidOperationException("A different network-pressure provider is already registered.");
        }

        /// <summary>Unregisters the currently registered network-pressure provider.</summary>
        public static void Unregister(INetworkPressureProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (_provider == null)
                return;
            if (!ReferenceEquals(_provider, provider))
                throw new InvalidOperationException("Only the registered network-pressure provider may be unregistered.");

            _provider = null;
        }

        /// <summary>Gets whether the supplied player is currently under network pressure.</summary>
        public static bool IsPlayerPressured(PlayerID playerID)
        {
            return _provider != null && _provider.IsPlayerPressured(playerID);
        }
    }
}
