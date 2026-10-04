using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using UnityEngine;
using System;
using System.Collections.Generic;
using PurrNet.Modules;
using Unity.Entities.UniversalDelegates;
using Game.Shared;
using Game.Server.Networking;

namespace Game.Server
{
    /// <summary>
    /// Manages player interest (chunk subscriptions). Coordinates with
    /// ServerWorldManager for chunk loading/unloading.
    /// </summary>
    public sealed class ServerInterestManager : MonoBehaviour
    {
        private static ServerInterestManager _instance;
        public static ServerInterestManager Instance => _instance;
        private static bool _initialized = false;
        public static bool IsInitialized => _initialized;

        private NetworkManager networkManager;

        private readonly Dictionary<Actor, HashSet<string>> _actorChunks = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerInterestManager is already initialized!");
                return;
            }
            _instance = this;

            // cache a reference to the network manager.
            networkManager = NetworkManager.main;

            // Listen for interest requests from the movement service.
            Game.Shared.Networking.MovementService.onInterestRequest += OnInterestRequest;
            networkManager.onPlayerLeft += OnPlayerLeft;

            _initialized = true;
        }

        private void OnPlayerLeft(PlayerID playerId, bool asServer)
        {
            // Find the actor for this player from our own tracking, since session
            // data may already be removed by ServerConnectionManager.OnPlayerLeft.
            Actor playerActor = null;
            foreach (var kvp in _actorChunks)
            {
                if (kvp.Key.Owner == playerId)
                {
                    playerActor = kvp.Key;
                    break;
                }
            }

            if (playerActor == null)
                return;

            // Remove from ChunkSubscriptions
            if (_actorChunks.TryGetValue(playerActor, out var scenes))
            {
                foreach (var sceneName in scenes)
                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(sceneName, playerActor);
            }

            Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(playerId);

            GetChunksByActor(playerActor, out var subscribedChunks);
            foreach (var chunk in subscribedChunks)
                UnsubscribePlayerFromChunk(playerActor, chunk);

            _actorChunks.Remove(playerActor);
        }

        private void OnInterestRequest(PlayerID playerId, Vector2Int centerChunk)
        {
            var session = ServerConnectionManager.Instance.GetSessionData(playerId);
            if (session?.PlayerActor == null) return;
            var actor = session.PlayerActor;

            var newSet = GetSceneGrid(centerChunk);

            if (!_actorChunks.TryGetValue(actor, out var oldSet))
                oldSet = new HashSet<string>();

            var toSubscribe = new List<Vector2Int>();
            var toUnsubscribe = new List<Vector2Int>();

            // Diff using the coordinate grid for subscribe/unsubscribe operations
            var newCoords = GetGrid(centerChunk);
            var oldCoords = new HashSet<Vector2Int>();
            // Rebuild old coords from old scene names
            foreach (var sceneName in oldSet)
            {
                if (TryParseChunkCoord(sceneName, out var coord))
                    oldCoords.Add(coord);
            }

            foreach (var coord in newCoords)
            {
                if (!oldCoords.Contains(coord))
                    toSubscribe.Add(coord);
            }

            foreach (var coord in oldCoords)
            {
                if (!newCoords.Contains(coord))
                    toUnsubscribe.Add(coord);
            }

            // Update actor's subscribed scene set and current chunk
            _actorChunks[actor] = newSet;
            var centerScene = $"World_{centerChunk.x}_{centerChunk.y}";
            actor.CurrentChunk = centerScene;

            // Update ChunkSubscriptions: remove actor from old scenes, add to new
            foreach (var sceneName in oldSet)
            {
                if (!newSet.Contains(sceneName))
                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(sceneName, actor);
            }
            foreach (var sceneName in newSet)
            {
                if (!oldSet.Contains(sceneName))
                    Game.Shared.Networking.ChunkSubscriptions.AddActorToScene(sceneName, actor);
            }

            // Subscribe/unsubscribe chunk scenes for terrain loading
            ProcessInterestChange(actor, toSubscribe, toUnsubscribe);

            // Targeted re-evaluation: only evaluate identities in chunks that changed,
            // rather than scanning every identity in every scene.
            var globalSceneId = ServerWorldManager.Instance.GlobalActorsSceneId;
            if (NetworkManager.main.TryGetModule<HierarchyFactory>(true, out var factory)
                && factory.TryGetHierarchy(globalSceneId, out var hierarchy))
            {
                // Symmetric difference: chunks that were added or removed
                var changedChunks = new HashSet<string>(newSet);
                changedChunks.SymmetricExceptWith(oldSet);

                foreach (var chunk in changedChunks)
                {
                    if (!Game.Shared.Networking.ChunkSubscriptions.SceneIdentities.TryGetValue(chunk, out var identities))
                        continue;

                    foreach (var id in identities)
                    {
                        if (id != null)
                            hierarchy.EvaluateVisibility(playerId, id.transform);
                    }
                }
            }

            // Notify that the actor's scene (center chunk) changed.
            if (ServerWorldManager.Instance.TryGetChunkSceneId(centerChunk, out var _centerSceneId))
            {
                Game.Shared.Networking.ActorService.onServerActorInterestChanged?.Invoke(actor, _centerSceneId);
            }
        }

        private void ProcessInterestChange(Actor actor, List<Vector2Int> toSubscribe, List<Vector2Int> toUnsubscribe)
        {
            foreach (var chunk in toSubscribe)
                SubscribePlayerToChunk(actor, chunk);

            foreach (var chunk in toUnsubscribe)
                UnsubscribePlayerFromChunk(actor, chunk);
        }

        public void SubscribePlayerToChunk(Actor actor, Vector2Int chunkCoord, Action onSubscribed = null)
        {
            ServerWorldManager.Instance.EnsureChunkLoaded(actor, chunkCoord, (sceneId) =>
            {
                // Guard: if the actor's interest changed while the chunk was loading, skip.
                var chunkScene = $"World_{chunkCoord.x}_{chunkCoord.y}";
                if (!_actorChunks.TryGetValue(actor, out var currentChunks) || !currentChunks.Contains(chunkScene))
                    return;

                if (actor.Owner != null)
                {
                    PlayerID playerID = actor.Owner.Value;
                    networkManager.scenePlayersModule.AddPlayerToScene(playerID, sceneId);
                    Debug.Log($"Subscribed player {actor} to chunk {chunkCoord}.");
                }

                onSubscribed?.Invoke();
            });
        }

        public void UnsubscribePlayerFromChunk(Actor actor, Vector2Int chunkCoord)
        {
            if (!ServerWorldManager.Instance.TryGetChunkSceneId(chunkCoord, out SceneID sceneId))
                return;

            if (actor.Owner != null)
            {
                PlayerID playerID = actor.Owner.Value;
                networkManager.scenePlayersModule.RemovePlayerFromScene(playerID, sceneId);
                Debug.Log($"Unsubscribed player {actor} from chunk {chunkCoord}.");
            }

            // If no players remain subscribed and no loads are in-flight, unload the chunk.
            if (ServerWorldManager.Instance.IsChunkLoadPending(chunkCoord))
                return;

            if (!networkManager.scenePlayersModule.TryGetPlayersInScene(sceneId, out var players) || players.Count == 0)
            {
                ServerWorldManager.Instance.UnloadChunk(chunkCoord);
            }
        }

        /// <summary>
        /// Get all the chunks which the actor is subscribed to.
        /// This is useful if the actor disconnects.
        /// </summary>
        public void GetChunksByActor(Actor actor, out List<Vector2Int> subscribedChunks)
        {
            subscribedChunks = new List<Vector2Int>();

            foreach (var kvp in ServerWorldManager.Instance.LoadedChunks)
            {
                Vector2Int chunkCoord = kvp.Key;
                SceneID sceneId = kvp.Value;

                if (actor.Owner != null)
                {
                    PlayerID playerID = actor.Owner.Value;
                    if (networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneId))
                        subscribedChunks.Add(chunkCoord);
                }
            }
        }

        private static HashSet<Vector2Int> GetGrid(Vector2Int center)
        {
            var set = new HashSet<Vector2Int>();
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    set.Add(new Vector2Int(center.x + x, center.y + y));
            return set;
        }

        private static HashSet<string> GetSceneGrid(Vector2Int center)
        {
            var set = new HashSet<string>();
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    set.Add($"World_{center.x + x}_{center.y + y}");
            return set;
        }

        private static bool TryParseChunkCoord(string sceneName, out Vector2Int coord)
        {
            coord = default;
            if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("World_"))
                return false;
            var parts = sceneName.Substring(6).Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                coord = new Vector2Int(x, y);
                return true;
            }
            return false;
        }
    }
}
