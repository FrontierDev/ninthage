using System;

namespace Game.Shared.Networking
{
    /// <summary>
    /// This manages all stat-related events and RPCs, 
    /// such as updating actor stats and notifying clients about stat changes. 
    /// It serves as a central hub for stat updates and synchronization in the networking layer.
    /// </summary>
    public static class StatService
    {
        public static Action<Actor, ActorStatContainer> onStatsUpdated;
    }
}