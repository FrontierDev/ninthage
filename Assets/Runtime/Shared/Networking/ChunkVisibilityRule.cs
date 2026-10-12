using PurrNet;
using UnityEngine;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Networking
{
    /// <summary>Determines visibility from the target actor's subscribed scene chunk.</summary>
    [CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/ChunkVisibilityRule")]
    public class ChunkVisibilityRule : NetworkVisibilityRule
    {
        /// <summary>Gets the relative complexity used by PurrNet for this rule.</summary>
        public override int complexity => 50;

        /// <summary>Returns whether a player can see a target identity in its chunk.</summary>
        public override bool CanSee(PlayerID player, NetworkIdentity target)
        {
            var targetActor = target.GetComponent<Actor>();
            if (targetActor == null || string.IsNullOrEmpty(targetActor.CurrentChunk))
                return false;

            if (!ChunkSubscriptions.SCENE_ACTORS.TryGetValue(targetActor.CurrentChunk, out var subscribers))
                return false;

            if (!ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(player, out var playerActor))
                return false;

            return subscribers.Contains(playerActor);
        }
    }
}
