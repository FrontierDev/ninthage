using System.Collections.Generic;
using PurrNet;

namespace Game.Shared.Networking
{
    public static class ChunkSubscriptions
    {
        public static readonly Dictionary<string, HashSet<Actor>> SceneActors = new();
        public static readonly Dictionary<PlayerID, Actor> PlayerActors = new();
        public static readonly Dictionary<string, HashSet<NetworkIdentity>> SceneIdentities = new();

        public static void AddActorToScene(string sceneName, Actor actor)
        {
            if (!SceneActors.TryGetValue(sceneName, out var set))
            {
                set = new HashSet<Actor>();
                SceneActors[sceneName] = set;
            }
            set.Add(actor);
        }

        public static void RemoveActorFromScene(string sceneName, Actor actor)
        {
            if (SceneActors.TryGetValue(sceneName, out var set))
            {
                set.Remove(actor);
                if (set.Count == 0)
                    SceneActors.Remove(sceneName);
            }
        }

        public static void RegisterPlayerActor(PlayerID playerId, Actor actor)
        {
            PlayerActors[playerId] = actor;
        }

        public static void UnregisterPlayerActor(PlayerID playerId)
        {
            PlayerActors.Remove(playerId);
        }

        public static void RegisterIdentity(string sceneName, NetworkIdentity identity)
        {
            if (!SceneIdentities.TryGetValue(sceneName, out var set))
            {
                set = new HashSet<NetworkIdentity>();
                SceneIdentities[sceneName] = set;
            }
            set.Add(identity);
        }

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