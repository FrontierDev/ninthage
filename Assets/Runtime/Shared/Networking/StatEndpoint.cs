using System;

using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
namespace Game.Shared.Networking
{
    /// <summary>
    /// This manages all stat-related events and RPCs, 
    /// such as updating actor stats and notifying clients about stat changes. 
    /// It serves as a central hub for stat updates and synchronization in the networking layer.
    /// </summary>
    public static class StatEndpoint
    {
        internal static void RecordSnapshotRpc(int recipientCount = -1)
        {
            NetworkingPerformanceMetrics.RecordRpc(
                NetworkingPerformanceMetrics.RPC_STAT_SNAPSHOT_COUNT,
                recipientCount);
        }

        /// <summary>Raised when an actor's stat container is updated.</summary>
        public static Action<Actor, ActorStatContainer> onStatsUpdated;
    }
}
