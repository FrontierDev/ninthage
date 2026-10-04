using PurrNet;
using UnityEngine;

namespace Game.Shared.Networking
{
    [CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/ChunkVisibilityRule")]
    public class ChunkVisibilityRule : NetworkVisibilityRule
    {
        public override int complexity => 50;

        public override bool CanSee(PlayerID player, NetworkIdentity target)
        {
            var targetActor = target.GetComponent<Actor>();
            if (targetActor == null || string.IsNullOrEmpty(targetActor.CurrentChunk))
                return false;

            if (!ChunkSubscriptions.SceneActors.TryGetValue(targetActor.CurrentChunk, out var subscribers))
                return false;

            if (!ChunkSubscriptions.PlayerActors.TryGetValue(player, out var playerActor))
                return false;

            return subscribers.Contains(playerActor);
        }
    }
}