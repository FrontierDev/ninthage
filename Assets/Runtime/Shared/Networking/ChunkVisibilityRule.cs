using PurrNet;
using UnityEngine;

using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
namespace Game.Shared.Networking
{
    /// <summary>Determines visibility from the target actor's subscribed scene chunk.</summary>
    [CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/ChunkVisibilityRule")]
    public class ChunkVisibilityRule : NetworkVisibilityRule
    {
        private static readonly PerformanceMarker VISIBILITY_MARKER = new(
            NetworkingPerformanceMetrics.VISIBILITY_EVALUATION_DURATION,
            "Network.ChunkVisibilityRule.CanSee");

        /// <summary>Gets the relative complexity used by PurrNet for this rule.</summary>
        public override int complexity => 50;

        /// <summary>Returns whether a player can see a target identity in its chunk.</summary>
        public override bool CanSee(PlayerID player, NetworkIdentity target)
        {
            using var markerScope = VISIBILITY_MARKER.Begin();
            var targetActor = target.GetComponent<Actor>();
            if (targetActor == null || string.IsNullOrEmpty(targetActor.CurrentChunk))
                return RecordVisibility(false);

            if (!ChunkSubscriptions.SCENE_ACTORS.TryGetValue(targetActor.CurrentChunk, out var subscribers))
                return RecordVisibility(false);

            if (!ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(player, out var playerActor))
                return RecordVisibility(false);

            return RecordVisibility(subscribers.Contains(playerActor));
        }

        private static bool RecordVisibility(bool visible)
        {
            NetworkingPerformanceMetrics.RecordVisibility(visible);
            return visible;
        }
    }
}
