using System.Collections.Generic;
using PurrNet;

namespace Game.Shared.Networking
{
    /// <summary>Tracks actor and identity subscriptions for streamed scene chunks.</summary>
    public static class ChunkSubscriptions
    {
        /// <summary>Gets actors registered for each scene chunk.</summary>
        public static readonly Dictionary<string, HashSet<Actor>> SCENE_ACTORS = new();
        /// <summary>Gets the actor associated with each player.</summary>
        public static readonly Dictionary<PlayerID, Actor> PLAYER_ACTORS = new();
        /// <summary>Gets identities registered for each scene chunk.</summary>
        public static readonly Dictionary<string, HashSet<NetworkIdentity>> SCENE_IDENTITIES = new();

        /// <summary>Registers an actor in a scene chunk.</summary>
        public static void AddActorToScene(string sceneName, Actor actor)
        {
            if (!SCENE_ACTORS.TryGetValue(sceneName, out var set))
            {
                set = new();
                SCENE_ACTORS[sceneName] = set;
            }
            set.Add(actor);
        }

        /// <summary>Removes an actor from a scene chunk.</summary>
        public static void RemoveActorFromScene(string sceneName, Actor actor)
        {
            if (SCENE_ACTORS.TryGetValue(sceneName, out var set))
            {
                set.Remove(actor);
                if (set.Count == 0)
                    SCENE_ACTORS.Remove(sceneName);
            }
        }

        /// <summary>Associates a player with its actor.</summary>
        public static void RegisterPlayerActor(PlayerID playerId, Actor actor)
        {
            PLAYER_ACTORS[playerId] = actor;
        }

        /// <summary>Removes a player's actor association.</summary>
        public static void UnregisterPlayerActor(PlayerID playerId)
        {
            PLAYER_ACTORS.Remove(playerId);
        }

        /// <summary>Registers a network identity in a scene chunk.</summary>
        public static void RegisterIdentity(string sceneName, NetworkIdentity identity)
        {
            if (!SCENE_IDENTITIES.TryGetValue(sceneName, out var set))
            {
                set = new();
                SCENE_IDENTITIES[sceneName] = set;
            }
            set.Add(identity);
        }

        /// <summary>Removes a network identity from a scene chunk.</summary>
        public static void UnregisterIdentity(string sceneName, NetworkIdentity identity)
        {
            if (SCENE_IDENTITIES.TryGetValue(sceneName, out var set))
            {
                set.Remove(identity);
                if (set.Count == 0)
                    SCENE_IDENTITIES.Remove(sceneName);
            }
        }
    }
}
