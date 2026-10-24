using PurrNet;
using UnityEngine;

using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
namespace Game.Shared.Networking
{
    /// <summary>Determines visibility from the target actor's subscribed scene chunk and authoritative range.</summary>
    [CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/ChunkVisibilityRule")]
    public class ChunkVisibilityRule : NetworkVisibilityRule
    {
        private static readonly PerformanceMarker VISIBILITY_MARKER = new(
            NetworkingPerformanceMetrics.VISIBILITY_EVALUATION_DURATION,
            "Network.ChunkVisibilityRule.CanSee");

        /// <summary>Gets the relative complexity used by PurrNet for this rule.</summary>
        public override int complexity => 50;

        /// <summary>Returns whether a player can see a target identity in its context, chunk and visibility range.</summary>
        public override bool CanSee(PlayerID player, NetworkIdentity target)
        {
            using var markerScope = VISIBILITY_MARKER.Begin();
            var targetActor = target == null ? null : target.GetComponent<Actor>();
            if (targetActor == null || string.IsNullOrWhiteSpace(targetActor.CurrentChunk))
                return RecordVisibility(false);

            if (!ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(player, out var playerActor)
                || playerActor == null)
                return RecordVisibility(false);

            if (!targetActor.SpatialContext.IsValid
                || !playerActor.SpatialContext.IsValid
                || !targetActor.SpatialContext.IsCompatibleWith(playerActor.SpatialContext))
            {
                return RecordVisibility(false);
            }

            if (!ChunkSubscriptions.ContainsActor(
                targetActor.SpatialContext,
                targetActor.CurrentChunk,
                playerActor))
            {
                return RecordVisibility(false);
            }

            if (!playerActor.TryGetAcceptedPositionSnapshot(out ActorPositionSnapshot playerSnapshot)
                || !targetActor.TryGetAcceptedPositionSnapshot(out ActorPositionSnapshot targetSnapshot))
            {
                return RecordVisibility(false);
            }

            bool wasPreviouslyVisible = target.IsObserver(player);
            double radius = wasPreviouslyVisible
                ? GameConfigurationManager.Config.ActorVisibilityExitRadius
                : GameConfigurationManager.Config.ActorVisibilityEntryRadius;
            double dx = targetSnapshot.Position.x - playerSnapshot.Position.x;
            double dy = targetSnapshot.Position.y - playerSnapshot.Position.y;
            double dz = targetSnapshot.Position.z - playerSnapshot.Position.z;
            double distanceSquared = dx * dx + dy * dy + dz * dz;
            double radiusSquared = radius * radius;

            return RecordVisibility(distanceSquared <= radiusSquared);
        }

        private static bool RecordVisibility(bool visible)
        {
            NetworkingPerformanceMetrics.RecordVisibility(visible);
            return visible;
        }
    }
}
