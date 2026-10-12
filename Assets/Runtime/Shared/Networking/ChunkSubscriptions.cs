using System.Collections.Generic;
using PurrNet;

namespace Game.Shared.Networking
{
    /// <summary>Tracks actor and identity subscriptions for streamed scene chunks.</summary>
    public static class ChunkSubscriptions
    {
        /// <summary>Gets actors registered for each scene chunk.</summary>
        public static readonly Dictionary<string, HashSet<Actor>> SceneActors = new();
        /// <summary>Gets the actor associated with each player.</summary>
        public static readonly Dictionary<PlayerID, Actor> PlayerActors = new();
        /// <summary>Gets identities registered for each scene chunk.</summary>
        public static readonly Dictionary<string, HashSet<NetworkIdentity>> SceneIdentities = new();

        /// <summary>Registers an actor in a scene chunk.</summary>
        public static void AddActorToScene(string sceneName, Actor actor)
        {
            if (!SceneActors.TryGetValue(sceneName, out var set))
            {
                set = new();
                SceneActors[sceneName] = set;
            }
            set.Add(actor);
        }

        /// <summary>Removes an actor from a scene chunk.</summary>
        public static void RemoveActorFromScene(string sceneName, Actor actor)
        {
            if (SceneActors.TryGetValue(sceneName, out var set))
            {
                set.Remove(actor);
                if (set.Count == 0)
                    SceneActors.Remove(sceneName);
            }
        }

        /// <summary>Associates a player with its actor.</summary>
        public static void RegisterPlayerActor(PlayerID playerId, Actor actor)
        {
            PlayerActors[playerId] = actor;
        }

        /// <summary>Removes a player's actor association.</summary>
        public static void UnregisterPlayerActor(PlayerID playerId)
        {
            PlayerActors.Remove(playerId);
        }

        /// <summary>Registers a network identity in a scene chunk.</summary>
        public static void RegisterIdentity(string sceneName, NetworkIdentity identity)
        {
            if (!SceneIdentities.TryGetValue(sceneName, out var set))
            {
                set = new();
                SceneIdentities[sceneName] = set;
            }
            set.Add(identity);
        }

        /// <summary>Removes a network identity from a scene chunk.</summary>
        public static void UnregisterIdentity(string sceneName, NetworkIdentity identity)
        {
            if (SceneIdentities.TryGetValue(sceneName, out var set))
            {
                set.Remove(identity);
                if (set.Count == 0)
                    SceneIdentities.Remove(sceneName);
            }
        }
    }
}
