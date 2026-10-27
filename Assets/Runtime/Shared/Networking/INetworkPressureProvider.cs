using PurrNet;

namespace Game.Shared.Networking
{
    /// <summary>Provides a read-only query for the pressure state of a network player.</summary>
    public interface INetworkPressureProvider
    {
        /// <summary>Gets whether the supplied player currently has a pressured transport connection.</summary>
        bool IsPlayerPressured(PlayerID playerID);
    }
}
