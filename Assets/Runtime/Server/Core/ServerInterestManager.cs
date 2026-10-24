using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>
    /// Manages player interest (chunk subscriptions). Coordinates with
    /// ServerWorldManager for chunk loading/unloading.
    /// </summary>
    public sealed class ServerInterestManager : MonoBehaviour
    {
        private static readonly PerformanceMarker REQUEST_MARKER = new(
            NetworkingPerformanceMetrics.INTEREST_REQUEST_DURATION,
            "Server.InterestManager.OnInterestRequest");
        private static ServerInterestManager _instance;
        /// <summary>Gets the active server interest manager.</summary>
        public static ServerInterestManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the interest manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private NetworkManager _networkManager;

        private readonly Dictionary<Actor, HashSet<string>> _actorChunks = new();
        private readonly List<NetworkIdentity> _visibilityIdentityBuffer = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerInterestManager is already initialized!");
                return;
            }
            _instance = this;

            // cache a reference to the network manager.
            _networkManager = NetworkManager.main;

            // Listen for interest requests from the movement service.
            Game.Shared.Networking.MovementEndpoint.onInterestRequest += OnInterestRequest;
            _networkManager.onPlayerLeft += OnPlayerLeft;

            _initialized = true;
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.MovementEndpoint.onInterestRequest -= OnInterestRequest;

            if (_networkManager != null)
                _networkManager.onPlayerLeft -= OnPlayerLeft;

            _actorChunks.Clear();
            _visibilityIdentityBuffer.Clear();
            _instance = null;
            _initialized = false;
        }

        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            // Find the actor for this player from our own tracking, since session
            // data may already be removed by ServerConnectionManager.OnPlayerLeft.
            Actor playerActor = null;
            foreach (var kvp in _actorChunks)
            {
                if (kvp.Key.Owner == playerID)
                {
                    playerActor = kvp.Key;
                    break;
                }
            }

            if (playerActor == null)
                return;

            ActorSpatialContext actorContext = playerActor.SpatialContext;
            if (!actorContext.IsValid)
                throw new InvalidOperationException(
                    $"Player actor '{playerActor.ID}' has no valid server-owned spatial context.");

            // Remove from ChunkSubscriptions
            if (_actorChunks.TryGetValue(playerActor, out var scenes))
            {
                foreach (var sceneName in scenes)
                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(
                        actorContext,
                        sceneName,
                        playerActor);
            }

            Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(playerID);

            GetChunksByActor(playerActor, out var subscribedChunks);
            foreach (var chunk in subscribedChunks)
                UnsubscribePlayerFromChunk(playerActor, chunk);

            _actorChunks.Remove(playerActor);
        }

        private void OnInterestRequest(PlayerID playerID, Vector2Int centerChunk)
        {
            var session = ServerConnectionManager.Instance.GetSessionData(playerID);
            if (session?.PlayerActor == null) return;
            using var markerScope = REQUEST_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int visibilityEvaluationCount = 0;
            var actor = session.PlayerActor;
            ActorSpatialContext actorContext = actor.SpatialContext;
            if (!actorContext.IsValid)
                throw new InvalidOperationException($"Actor {actor} has no valid server-owned spatial context.");

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
                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(
                        actorContext,
                        sceneName,
                        actor);
            }
            foreach (var sceneName in newSet)
            {
                if (!oldSet.Contains(sceneName))
                    Game.Shared.Networking.ChunkSubscriptions.AddActorToScene(
                        actorContext,
                        sceneName,
                        actor);
            }

            // Subscribe/unsubscribe chunk scenes for terrain loading
            ProcessInterestChange(actor, toSubscribe, toUnsubscribe);
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.INTEREST_CHANGED_SUBSCRIPTION_COUNT,
                    toSubscribe.Count + toUnsubscribe.Count);

            // Targeted re-evaluation: only evaluate identities in chunks that changed,
            // rather than scanning every identity in every scene.
            var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
            if (NetworkManager.main.TryGetModule<HierarchyFactory>(true, out var factory)
                && factory.TryGetHierarchy(globalSceneID, out var hierarchy))
            {
                // Symmetric difference: chunks that were added or removed
                var changedChunks = new HashSet<string>(newSet);
                changedChunks.SymmetricExceptWith(oldSet);

                foreach (var chunk in changedChunks)
                {
                    _visibilityIdentityBuffer.Clear();
                    int identityCount = Game.Shared.Networking.ChunkSubscriptions.AppendIdentities(
                        actorContext,
                        chunk,
                        _visibilityIdentityBuffer);

                    for (int i = 0; i < identityCount; i++)
                    {
                        var id = _visibilityIdentityBuffer[i];
                        if (id != null)
                        {
                            if (telemetryEnabled)
                                visibilityEvaluationCount++;
                            hierarchy.EvaluateVisibility(playerID, id.transform);
                        }
                    }
                }
            }

            // Notify that the actor's scene (center chunk) changed.
            if (ServerWorldManager.Instance.TryGetChunkSceneID(actorContext, centerChunk, out var _centerSceneID))
            {
                Game.Shared.Networking.ActorEndpoint.onServerActorInterestChanged?.Invoke(actor, _centerSceneID);
            }
            if (telemetryEnabled)
            {
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.INTEREST_VISIBILITY_EVALUATION_COUNT,
                    visibilityEvaluationCount);
                NetworkingPerformanceMetrics.RecordVisibilityCandidates(visibilityEvaluationCount);
            }
        }

        private void ProcessInterestChange(Actor actor, List<Vector2Int> toSubscribe, List<Vector2Int> toUnsubscribe)
        {
            foreach (var chunk in toSubscribe)
                SubscribePlayerToChunk(actor, chunk);

            foreach (var chunk in toUnsubscribe)
                UnsubscribePlayerFromChunk(actor, chunk);
        }

        /// <summary>Subscribes a player's actor to a world chunk.</summary>
        /// <param name="actor">The actor to subscribe.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <param name="onSubscribed">An optional callback invoked after subscription.</param>
        public void SubscribePlayerToChunk(Actor actor, Vector2Int chunkCoord, Action onSubscribed = null)
        {
            ActorSpatialContext actorContext = actor.SpatialContext;
            ServerWorldManager.Instance.EnsureChunkLoaded(actorContext, chunkCoord, (succeeded, sceneID) =>
            {
                if (!succeeded)
                    return;

                // Guard: if the actor's interest changed while the chunk was loading, skip.
                var chunkScene = $"World_{chunkCoord.x}_{chunkCoord.y}";
                if (!_actorChunks.TryGetValue(actor, out var currentChunks) || !currentChunks.Contains(chunkScene))
                    return;

                if (actor.Owner != null)
                {
                    PlayerID playerID = actor.Owner.Value;
                    _networkManager.scenePlayersModule.AddPlayerToScene(playerID, sceneID);
                    Debug.Log($"Subscribed player {actor} to chunk {chunkCoord}.");
                }

                onSubscribed?.Invoke();
            });
        }

        /// <summary>Unsubscribes a player's actor from a world chunk.</summary>
        /// <param name="actor">The actor to unsubscribe.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        public void UnsubscribePlayerFromChunk(Actor actor, Vector2Int chunkCoord)
        {
            ActorSpatialContext actorContext = actor.SpatialContext;
            if (!ServerWorldManager.Instance.TryGetChunkSceneID(actorContext, chunkCoord, out SceneID sceneID))
                return;

            if (actor.Owner != null)
            {
                PlayerID playerID = actor.Owner.Value;
                _networkManager.scenePlayersModule.RemovePlayerFromScene(playerID, sceneID);
                Debug.Log($"Unsubscribed player {actor} from chunk {chunkCoord}.");
            }

            // If no players remain subscribed and no loads are in-flight, unload the chunk.
            if (ServerWorldManager.Instance.IsChunkLoadPending(actorContext, chunkCoord))
                return;

            if (!_networkManager.scenePlayersModule.TryGetPlayersInScene(sceneID, out var players) || players.Count == 0)
            {
                ServerWorldManager.Instance.UnloadChunk(actorContext, chunkCoord);
            }
        }

        /// <summary>
        /// Get all the chunks which the actor is subscribed to.
        /// This is useful if the actor disconnects.
        /// </summary>
        public void GetChunksByActor(Actor actor, out List<Vector2Int> subscribedChunks)
        {
            subscribedChunks = new();
            ActorSpatialContext actorContext = actor.SpatialContext;

            foreach (var kvp in ServerWorldManager.Instance.LoadedChunks)
            {
                if (kvp.Key.Context != actorContext)
                    continue;

                Vector2Int chunkCoord = kvp.Key.ChunkCoord;
                SceneID sceneID = kvp.Value;

                if (actor.Owner != null)
                {
                    PlayerID playerID = actor.Owner.Value;
                    if (_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
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
