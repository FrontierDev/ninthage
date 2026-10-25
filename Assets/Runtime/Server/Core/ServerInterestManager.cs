using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
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
            "Server.InterestManager.UpdateAcceptedPositionInterest");
        private static ServerInterestManager _instance;
        /// <summary>Gets the active server interest manager.</summary>
        public static ServerInterestManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the interest manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private NetworkManager _networkManager;

        private sealed class PlayerInterestState
        {
            public ActorSpatialContext Context { get; }
            public Vector2Int CenterChunk { get; }
            public HashSet<string> SceneNames { get; }

            public PlayerInterestState(
                ActorSpatialContext context,
                Vector2Int centerChunk,
                HashSet<string> sceneNames)
            {
                Context = context;
                CenterChunk = centerChunk;
                SceneNames = sceneNames;
            }
        }

        private readonly Dictionary<Actor, PlayerInterestState> _actorInterest = new();
        private readonly List<Guid> _visibilityCandidateQueryBuffer = new();
        private readonly HashSet<Guid> _visibilityCandidateActorIDs = new();
        private readonly HashSet<(PlayerID Observer, Guid SubjectActorID)> _visibilityEvaluationKeys = new();
        private readonly Dictionary<PlayerID, WorldChunkKey> _retainedTransferEntranceChunks = new();
        private readonly Dictionary<WorldChunkKey, int> _environmentPreloadCounts = new();

        private sealed class PreparedTransferScenes
        {
            internal Actor Actor { get; }
            internal ActorSpatialContext Context { get; }
            internal List<string> Addresses { get; }
            internal bool Cancelled { get; set; }

            internal PreparedTransferScenes(Actor actor, ActorSpatialContext context, List<string> addresses)
            {
                Actor = actor;
                Context = context;
                Addresses = addresses;
            }
        }

        private readonly Dictionary<PlayerID, PreparedTransferScenes> _preparedTransferScenes = new();

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerInterestManager is already initialized.");
            _instance = this;

            // cache a reference to the network manager.
            _networkManager = NetworkManager.main;

            Game.Shared.Networking.ActorEndpoint.onServerAcceptedPlayerPositionChanged +=
                OnServerAcceptedPlayerPositionChanged;
            _networkManager.onPlayerLeft += OnPlayerLeft;

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            Game.Shared.Networking.ActorEndpoint.onServerAcceptedPlayerPositionChanged -=
                OnServerAcceptedPlayerPositionChanged;

            if (_networkManager != null)
                _networkManager.onPlayerLeft -= OnPlayerLeft;

            var transferPlayers = new List<PlayerID>(_preparedTransferScenes.Keys);
            foreach (PlayerID playerID in transferPlayers)
            {
                Actor actor = _preparedTransferScenes.TryGetValue(playerID, out PreparedTransferScenes prepared)
                    ? prepared.Actor
                    : null;
                try
                {
                    CleanupTransferPlayerState(playerID, actor, true);
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"[TransferInstanceScene] Server teardown cleanup failed for player {playerID}: {exception.Message}");
                }
            }

            _actorInterest.Clear();
            _retainedTransferEntranceChunks.Clear();
            _environmentPreloadCounts.Clear();
            _preparedTransferScenes.Clear();
            _visibilityCandidateQueryBuffer.Clear();
            _visibilityCandidateActorIDs.Clear();
            _visibilityEvaluationKeys.Clear();
            _instance = null;
            _initialized = false;
        }

        private void OnPlayerLeft(PlayerID playerID, bool asServer)
        {
            Actor playerActor = null;
            foreach (Actor actor in _actorInterest.Keys)
            {
                if (!ReferenceEquals(actor, null) && actor.Owner == playerID)
                {
                    playerActor = actor;
                    break;
                }
            }

            if (ReferenceEquals(playerActor, null)
                && Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                    playerID,
                    out Actor registeredActor))
                playerActor = registeredActor;

            RetirePlayerInterest(playerID, playerActor);
        }

        internal void UpdatePlayerInterestFromAcceptedPosition(
            Actor actor,
            WorldPosition acceptedPosition)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!actor.Owner.HasValue)
                throw new InvalidOperationException(
                    $"Actor '{actor.ID}' has no player owner for interest management.");

            PlayerID playerID = actor.Owner.Value;
            Vector2Int centerChunk = acceptedPosition.Chunk;
            using var markerScope = REQUEST_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            ActorSpatialContext actorContext = actor.SpatialContext;

            bool hasOldState = _actorInterest.TryGetValue(actor, out var oldState);
            if (hasOldState
                && oldState.Context == actorContext
                && oldState.CenterChunk == centerChunk)
            {
                return;
            }

            var newSet = GetSceneGrid(centerChunk);
            bool destinationIsTransferRuntime = ServerWorldManager.Instance.IsTransferRuntime(actorContext);

            var oldSet = hasOldState
                ? oldState.SceneNames
                : new HashSet<string>(StringComparer.Ordinal);
            bool contextChanged = hasOldState && !oldState.Context.Equals(actorContext);

            var toSubscribe = new List<Vector2Int>();
            var toUnsubscribe = new List<Vector2Int>();

            // Diff using the coordinate grid for subscribe/unsubscribe operations
            var newCoords = destinationIsTransferRuntime
                ? new HashSet<Vector2Int>()
                : GetGrid(centerChunk);
            var oldCoords = new HashSet<Vector2Int>();
            // Rebuild old coords from old scene names
            foreach (var sceneName in oldSet)
            {
                if (TryParseChunkCoord(sceneName, out var coord))
                    oldCoords.Add(coord);
            }

            if (contextChanged)
            {
                foreach (var coord in newCoords)
                    toSubscribe.Add(coord);
                foreach (var coord in oldCoords)
                    toUnsubscribe.Add(coord);
            }
            else
            {
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
            }

            if (contextChanged)
            {
                Game.Shared.Networking.ChunkSubscriptions.TransferActorSubscriptions(
                    oldState.Context,
                    oldState.SceneNames,
                    actorContext,
                    newSet,
                    actor);
            }
            else
            {
                ApplySameContextSubscriptionChange(actor, actorContext, oldSet, newSet);
            }

            _actorInterest[actor] = new PlayerInterestState(actorContext, centerChunk, newSet);

            // Subscribe/unsubscribe chunk scenes for terrain loading.
            ProcessInterestChange(
                actor,
                hasOldState ? oldState.Context : actorContext,
                actorContext,
                toSubscribe,
                toUnsubscribe);
            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(
                    NetworkingPerformanceMetrics.INTEREST_CHANGED_SUBSCRIPTION_COUNT,
                    toSubscribe.Count + toUnsubscribe.Count);

            // Notify that the actor's scene (center chunk) changed.
            if (ServerWorldManager.Instance.TryGetChunkSceneID(actorContext, centerChunk, out var _centerSceneID))
            {
                Game.Shared.Networking.ActorEndpoint.onServerActorInterestChanged?.Invoke(actor, _centerSceneID);
            }
        }

        internal void ReevaluateActorVisibility(
            Actor actor,
            ActorSpatialContext previousContext,
            WorldPosition previousPosition,
            ActorSpatialContext currentContext,
            WorldPosition currentPosition)
        {
            _visibilityEvaluationKeys.Clear();
            try
            {
                AppendVisibilityEvaluationKeys(
                    actor,
                    previousContext,
                    previousPosition,
                    currentContext,
                    currentPosition);
                EvaluateVisibilityKeys();
            }
            finally
            {
                _visibilityEvaluationKeys.Clear();
            }
        }

        internal void ReevaluateActorVisibilityBatch(
            IReadOnlyList<ServerActorManager.AcceptedVisibilityMovement> movements)
        {
            _visibilityEvaluationKeys.Clear();
            try
            {
                if (movements.Count == 0)
                    return;

                for (int i = 0; i < movements.Count; i++)
                {
                    var movement = movements[i];
                    AppendVisibilityEvaluationKeys(
                        movement.Actor,
                        movement.Context,
                        movement.PreviousPosition,
                        movement.Context,
                        movement.CurrentPosition);
                }

                EvaluateVisibilityKeys();
            }
            finally
            {
                _visibilityEvaluationKeys.Clear();
            }
        }

        private void AppendVisibilityEvaluationKeys(
            Actor actor,
            ActorSpatialContext previousContext,
            WorldPosition previousPosition,
            ActorSpatialContext currentContext,
            WorldPosition currentPosition)
        {
            var radius = GameConfigurationManager.Config.ActorVisibilityExitRadius;
            _visibilityCandidateActorIDs.Clear();

            _visibilityCandidateQueryBuffer.Clear();
            ServerActorManager.Instance.QuerySpatialCandidates(
                previousContext,
                previousPosition,
                radius,
                _visibilityCandidateQueryBuffer,
                out _);
            for (int i = 0; i < _visibilityCandidateQueryBuffer.Count; i++)
                _visibilityCandidateActorIDs.Add(_visibilityCandidateQueryBuffer[i]);

            var sameContextAndPosition = previousContext.Equals(currentContext)
                && previousPosition.x == currentPosition.x
                && previousPosition.y == currentPosition.y
                && previousPosition.z == currentPosition.z;
            if (!sameContextAndPosition)
            {
                _visibilityCandidateQueryBuffer.Clear();
                ServerActorManager.Instance.QuerySpatialCandidates(
                    currentContext,
                    currentPosition,
                    radius,
                    _visibilityCandidateQueryBuffer,
                    out _);
                for (int i = 0; i < _visibilityCandidateQueryBuffer.Count; i++)
                    _visibilityCandidateActorIDs.Add(_visibilityCandidateQueryBuffer[i]);
            }

            NetworkingPerformanceMetrics.RecordVisibilityCandidates(_visibilityCandidateActorIDs.Count);

            foreach (var candidateID in _visibilityCandidateActorIDs)
            {
                if (!ServerActorManager.Instance.TryGetActor(candidateID, out var candidateActor))
                {
                    throw new InvalidOperationException(
                        $"Spatial visibility candidate '{candidateID}' does not resolve to its live registered actor.");
                }

                if (candidateID == actor.ID)
                {
                    if (actor.Owner.HasValue)
                        _visibilityEvaluationKeys.Add((actor.Owner.Value, candidateID));

                    continue;
                }

                if (candidateActor.Owner.HasValue)
                    _visibilityEvaluationKeys.Add((candidateActor.Owner.Value, actor.ID));
                if (actor.Owner.HasValue)
                    _visibilityEvaluationKeys.Add((actor.Owner.Value, candidateID));
            }
        }

        private void EvaluateVisibilityKeys()
        {
            var telemetryEnabled = PerformanceTelemetry.IsEnabled;
            var visibilityEvaluationCount = 0;
            try
            {
                if (!_networkManager.TryGetModule<HierarchyFactory>(true, out var factory)
                    || !factory.TryGetHierarchy(ServerWorldManager.Instance.GlobalActorsSceneID, out var hierarchy))
                {
                    return;
                }

                foreach (var (observer, subjectActorID) in _visibilityEvaluationKeys)
                {
                    if (!ServerActorManager.Instance.TryGetActorIdentity(subjectActorID, out var subjectIdentity))
                    {
                        throw new InvalidOperationException(
                            $"Visibility subject '{subjectActorID}' does not resolve to its live registered NetworkIdentity.");
                    }

                    visibilityEvaluationCount++;
                    hierarchy.EvaluateVisibility(observer, subjectIdentity.transform);
                }
            }
            finally
            {
                if (telemetryEnabled)
                {
                    PerformanceTelemetry.TryRecord(
                        NetworkingPerformanceMetrics.INTEREST_VISIBILITY_EVALUATION_COUNT,
                        visibilityEvaluationCount);
                }
            }
        }

        private void OnServerAcceptedPlayerPositionChanged(
            Actor actor,
            WorldPosition acceptedPosition)
        {
            UpdatePlayerInterestFromAcceptedPosition(actor, acceptedPosition);
        }

        private static void ApplySameContextSubscriptionChange(
            Actor actor,
            ActorSpatialContext context,
            HashSet<string> oldSceneNames,
            HashSet<string> newSceneNames)
        {
            var removedSceneNames = new List<string>();
            var addedSceneNames = new List<string>();
            try
            {
                foreach (var sceneName in oldSceneNames)
                {
                    if (newSceneNames.Contains(sceneName)
                        || !Game.Shared.Networking.ChunkSubscriptions.ContainsActor(
                            context,
                            sceneName,
                            actor))
                    {
                        continue;
                    }

                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(
                        context,
                        sceneName,
                        actor);
                    removedSceneNames.Add(sceneName);
                }

                foreach (var sceneName in newSceneNames)
                {
                    if (oldSceneNames.Contains(sceneName))
                        continue;

                    bool wasRegistered = Game.Shared.Networking.ChunkSubscriptions.ContainsActor(
                        context,
                        sceneName,
                        actor);
                    Game.Shared.Networking.ChunkSubscriptions.AddActorToScene(
                        context,
                        sceneName,
                        actor);
                    if (!wasRegistered)
                        addedSceneNames.Add(sceneName);
                }
            }
            catch (Exception transferException)
            {
                try
                {
                    for (int i = addedSceneNames.Count - 1; i >= 0; i--)
                    {
                        Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(
                            context,
                            addedSceneNames[i],
                            actor);
                    }

                    foreach (var sceneName in removedSceneNames)
                    {
                        Game.Shared.Networking.ChunkSubscriptions.AddActorToScene(
                            context,
                            sceneName,
                            actor);
                    }
                }
                catch (Exception rollbackException)
                {
                    throw new InvalidOperationException(
                        $"Actor '{actor.ID}' same-context subscription update failed and rollback also failed.",
                        new AggregateException(transferException, rollbackException));
                }

                throw;
            }
        }

        private void ProcessInterestChange(
            Actor actor,
            ActorSpatialContext sourceContext,
            ActorSpatialContext destinationContext,
            List<Vector2Int> toSubscribe,
            List<Vector2Int> toUnsubscribe)
        {
            if (!ServerWorldManager.Instance.IsTransferRuntime(destinationContext))
            {
                foreach (var chunk in toSubscribe)
                    SubscribePlayerToChunk(actor, destinationContext, chunk);
            }

            foreach (var chunk in toUnsubscribe)
                UnsubscribePlayerFromChunk(actor, sourceContext, chunk);
        }

        /// <summary>Subscribes a player's actor to a world chunk.</summary>
        /// <param name="actor">The actor to subscribe.</param>
        /// <param name="context">The context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        /// <param name="onSubscribed">An optional callback invoked after subscription.</param>
        private void SubscribePlayerToChunk(
            Actor actor,
            ActorSpatialContext context,
            Vector2Int chunkCoord,
            Action onSubscribed = null)
        {
            ServerWorldManager.Instance.EnsureChunkLoaded(context, chunkCoord, (succeeded, sceneID) =>
            {
                if (!succeeded)
                    return;

                // Guard: if the actor's interest changed while the chunk was loading, skip.
                var chunkScene = $"World_{chunkCoord.x}_{chunkCoord.y}";
                if (!_actorInterest.TryGetValue(actor, out var currentState)
                    || !currentState.Context.Equals(context)
                    || !currentState.SceneNames.Contains(chunkScene))
                {
                    var chunkKey = new WorldChunkKey(context, chunkCoord);
                    UnloadChunkIfUnused(chunkKey, sceneID);
                    return;
                }

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
        /// <param name="context">The context that owns the chunk.</param>
        /// <param name="chunkCoord">The chunk coordinate.</param>
        private void UnsubscribePlayerFromChunk(
            Actor actor,
            ActorSpatialContext context,
            Vector2Int chunkCoord)
        {
            if (!context.IsValid)
                throw new ArgumentException(
                    "A valid actor spatial context is required.",
                    nameof(context));
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (actor.Owner.HasValue
                && _retainedTransferEntranceChunks.TryGetValue(actor.Owner.Value, out WorldChunkKey retainedChunk)
                && retainedChunk == new WorldChunkKey(context, chunkCoord))
                return;

            if (!ServerWorldManager.Instance.TryGetChunkSceneID(context, chunkCoord, out SceneID sceneID))
                return;

            if (actor.Owner != null)
            {
                PlayerID playerID = actor.Owner.Value;
                _networkManager.scenePlayersModule.RemovePlayerFromScene(playerID, sceneID);
                Debug.Log($"Unsubscribed player {actor} from chunk {chunkCoord}.");
            }

            UnloadChunkIfUnused(new WorldChunkKey(context, chunkCoord), sceneID);
        }

        /// <summary>
        /// Ensures that the explicit context-qualified destination interest grid is loaded
        /// without changing any player's current interest state.
        /// </summary>
        internal void EnsureInterestGridLoaded(
            ActorSpatialContext context,
            WorldPosition position,
            Action<bool> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            ActorSpatialContext runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            if (context.WorldID != runtimeContext.WorldID
                || !StringComparer.Ordinal.Equals(context.MapID, runtimeContext.MapID))
            {
                throw new InvalidOperationException(
                    "Interest grid preload context does not belong to the current server runtime.");
            }

            Vector2Int centerChunk = position.Chunk;
            var preloadChunks = new List<WorldChunkKey>();
            try
            {
                foreach (Vector2Int chunk in GetGrid(centerChunk))
                {
                    var chunkKey = new WorldChunkKey(context, chunk);
                    AddEnvironmentPreload(chunkKey);
                    preloadChunks.Add(chunkKey);
                }
            }
            catch
            {
                for (int i = preloadChunks.Count - 1; i >= 0; i--)
                    RemoveEnvironmentPreload(preloadChunks[i]);
                throw;
            }

            var pendingChunks = new List<WorldChunkKey>();
            foreach (WorldChunkKey chunkKey in preloadChunks)
            {
                if (!ServerWorldManager.Instance.IsChunkLoaded(context, chunkKey.ChunkCoord))
                    pendingChunks.Add(chunkKey);
            }

            bool terminalResultDelivered = false;
            void CompletePreload(bool succeeded)
            {
                if (terminalResultDelivered)
                    return;

                terminalResultDelivered = true;
                try
                {
                    onCompleted(succeeded);
                }
                finally
                {
                    for (int i = preloadChunks.Count - 1; i >= 0; i--)
                        RemoveEnvironmentPreload(preloadChunks[i]);

                    foreach (WorldChunkKey chunkKey in preloadChunks)
                    {
                        if (ServerWorldManager.Instance.TryGetChunkSceneID(
                                chunkKey.Context,
                                chunkKey.ChunkCoord,
                                out SceneID sceneID))
                            UnloadChunkIfUnused(chunkKey, sceneID);
                    }
                }
            }

            if (pendingChunks.Count == 0)
            {
                CompletePreload(true);
                return;
            }

            int remaining = pendingChunks.Count;

            void CompleteChunk(WorldChunkKey chunkKey, bool succeeded, SceneID sceneID)
            {
                if (terminalResultDelivered)
                {
                    if (succeeded)
                        UnloadChunkIfUnused(chunkKey, sceneID);
                    return;
                }

                if (!succeeded)
                {
                    CompletePreload(false);
                    return;
                }

                remaining--;
                if (remaining == 0)
                    CompletePreload(true);
            }

            foreach (WorldChunkKey chunkKey in pendingChunks)
            {
                try
                {
                    ServerWorldManager.Instance.EnsureChunkLoaded(
                        context,
                        chunkKey.ChunkCoord,
                        (succeeded, sceneID) => CompleteChunk(chunkKey, succeeded, sceneID));
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"[InstanceValidation] Destination chunk preload failed for {context} / {chunkKey.ChunkCoord}: {exception.Message}");
                    CompleteChunk(chunkKey, false, default);
                }
            }
        }

        internal void EnsureDestinationReadyForAdmission(
            ActorSpatialContext context,
            WorldPosition position,
            Action<bool> onCompleted)
        {
            if (!position.IsFinite)
                throw new ArgumentException(
                    "Destination position must be finite.",
                    nameof(position));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            Vector2Int centerChunk = position.Chunk;
            if (centerChunk.x == int.MinValue
                || centerChunk.x == int.MaxValue
                || centerChunk.y == int.MinValue
                || centerChunk.y == int.MaxValue)
                throw new InvalidOperationException(
                    "The destination interest grid would exceed the concrete chunk-coordinate range.");

            HashSet<Vector2Int> requiredChunks = GetGrid(centerChunk);

            bool completionDelivered = false;
            bool populationCompleted = false;
            bool populationSucceeded = false;
            bool environmentCompleted = false;
            bool environmentSucceeded = false;

            void TryComplete()
            {
                if (completionDelivered)
                    return;
                if ((populationCompleted && !populationSucceeded)
                    || (environmentCompleted && !environmentSucceeded))
                {
                    completionDelivered = true;
                    onCompleted(false);
                    return;
                }
                if (!populationCompleted || !environmentCompleted)
                    return;

                completionDelivered = true;
                onCompleted(true);
            }

            void CompletePopulationPreparation(bool succeeded)
            {
                populationCompleted = true;
                populationSucceeded = succeeded;
                TryComplete();
            }

            void CompleteEnvironmentPreload(bool succeeded)
            {
                if (!succeeded)
                {
                    ServerSpawnManager.Instance.FailInstancePopulationPreparation(
                        context,
                        "required destination environment preload failed");
                }

                environmentCompleted = true;
                environmentSucceeded = succeeded;
                TryComplete();
            }

            ServerSpawnManager.Instance.EnsureInstancePopulationReady(
                context,
                requiredChunks,
                CompletePopulationPreparation);

            try
            {
                EnsureInterestGridLoaded(
                    context,
                    position,
                    CompleteEnvironmentPreload);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"[InstanceReadiness] Destination environment preload could not start for {context}: {exception.Message}");
                CompleteEnvironmentPreload(false);
            }
        }

        /// <summary>
        /// Determines whether all nine context-qualified chunks around a position are loaded.
        /// </summary>
        /// <param name="context">The authoritative runtime context that owns the interest grid.</param>
        /// <param name="position">The finite world position at the grid centre.</param>
        /// <returns><see langword="true"/> when every chunk in the 3x3 grid is loaded.</returns>
        internal bool IsInterestGridLoaded(
            ActorSpatialContext context,
            WorldPosition position)
        {
            Vector2Int centerChunk = position.Chunk;
            foreach (var chunkCoord in GetGrid(centerChunk))
            {
                if (!ServerWorldManager.Instance.IsChunkLoaded(context, chunkCoord))
                    return false;
            }

            return true;
        }

        internal void PrepareTransferInstanceAdmission(
            Actor actor,
            ActorSpatialContext destinationContext,
            WorldChunkKey entranceChunk,
            IReadOnlyList<string> sceneAddresses,
            Action<bool> onCompleted)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!actor.Owner.HasValue)
                throw new InvalidOperationException("Transfer-instance admission requires an owned player actor.");
            if (sceneAddresses == null || sceneAddresses.Count == 0)
                throw new ArgumentException("At least one transfer scene address is required.", nameof(sceneAddresses));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            PlayerID playerID = actor.Owner.Value;
            ServerWorldManager worldManager = ServerWorldManager.Instance;
            if (!worldManager.TryGetTransferRuntime(destinationContext, out ServerWorldManager.TransferRuntime runtime)
                || runtime.EntranceChunk != entranceChunk
                || runtime.EntranceCollider == null)
                throw new InvalidOperationException("Transfer-instance admission does not match its registered entrance runtime.");
            if (_preparedTransferScenes.ContainsKey(playerID))
                throw new InvalidOperationException($"Player {playerID} already has prepared transfer-instance scenes.");

            var copiedAddresses = new List<string>(sceneAddresses);
            var preparation = new PreparedTransferScenes(actor, destinationContext, copiedAddresses);
            _preparedTransferScenes.Add(playerID, preparation);
            _retainedTransferEntranceChunks[playerID] = entranceChunk;

            bool completed = false;
            void Fail(string message)
            {
                if (completed)
                    return;
                completed = true;
                Debug.Error($"[TransferInstanceScene] Admission preparation failed for player {playerID}: {message}");
                try
                {
                    CleanupTransferPlayerState(playerID, actor, false);
                }
                catch (Exception cleanupException)
                {
                    Debug.Error(
                        $"[TransferInstanceScene] Admission rollback failed for player {playerID}: {cleanupException.Message}");
                }
                onCompleted(false);
            }

            try
            {
                worldManager.EnsureChunkLoaded(
                    entranceChunk.Context,
                    entranceChunk.ChunkCoord,
                    (entranceLoaded, entranceSceneID) =>
                    {
                        if (preparation.Cancelled)
                        {
                            UnloadChunkIfUnused(entranceChunk, entranceSceneID);
                            return;
                        }
                        if (completed)
                            return;
                        if (!entranceLoaded)
                        {
                            Fail($"public entrance chunk {entranceChunk} did not load");
                            return;
                        }

                        if (!_networkManager.scenePlayersModule.IsPlayerInScene(playerID, entranceSceneID))
                            _networkManager.scenePlayersModule.AddPlayerToScene(playerID, entranceSceneID);
                        if (!_networkManager.scenePlayersModule.IsPlayerInScene(playerID, entranceSceneID))
                        {
                            Fail($"player could not be subscribed to entrance chunk {entranceChunk}");
                            return;
                        }

                        LoadNextInstanceScene(0);
                    });
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }

            void LoadNextInstanceScene(int index)
            {
                if (completed)
                    return;
                if (index == copiedAddresses.Count)
                {
                    completed = true;
                    onCompleted(true);
                    return;
                }

                string address = copiedAddresses[index];
                try
                {
                    worldManager.EnsureInstanceSceneLoaded(
                        destinationContext,
                        address,
                        (sceneLoaded, sceneID) =>
                        {
                            if (preparation.Cancelled)
                            {
                                UnloadInstanceSceneIfUnused(destinationContext, address, sceneID);
                                return;
                            }
                            if (completed)
                                return;
                            if (!sceneLoaded)
                            {
                                Fail($"configured instance scene '{address}' did not load");
                                return;
                            }

                            try
                            {
                                if (!_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
                                    _networkManager.scenePlayersModule.AddPlayerToScene(playerID, sceneID);
                                if (!_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
                                {
                                    Fail($"player could not be subscribed to instance scene '{address}'");
                                    return;
                                }

                                LoadNextInstanceScene(index + 1);
                            }
                            catch (Exception exception)
                            {
                                Fail($"subscription to instance scene '{address}' threw: {exception.Message}");
                            }
                        });
                }
                catch (Exception exception)
                {
                    Fail($"load request for instance scene '{address}' threw: {exception.Message}");
                }
            }
        }

        internal bool IsPlayerDestinationEnvironmentReady(
            PlayerID playerID,
            ActorSpatialContext context,
            WorldPosition position)
        {
            if (ServerWorldManager.Instance.TryGetTransferRuntime(context, out ServerWorldManager.TransferRuntime runtime))
            {
                if (!_preparedTransferScenes.TryGetValue(playerID, out PreparedTransferScenes prepared)
                    || prepared.Context != context
                    || !_retainedTransferEntranceChunks.TryGetValue(playerID, out WorldChunkKey retained)
                    || retained != runtime.EntranceChunk
                    || !ServerWorldManager.Instance.TryGetChunkSceneID(
                        retained.Context,
                        retained.ChunkCoord,
                        out SceneID entranceSceneID)
                    || !_networkManager.scenePlayersModule.IsPlayerInScene(playerID, entranceSceneID))
                    return false;

                foreach (string address in runtime.SceneAddresses)
                {
                    if (!ServerWorldManager.Instance.TryGetInstanceSceneID(context, address, out SceneID sceneID)
                        || !_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
                        return false;
                }

                return true;
            }

            return IsInterestGridLoaded(context, position);
        }

        internal void CancelTransferInstanceAdmission(Actor actor, ActorSpatialContext context)
        {
            if (actor == null || !actor.Owner.HasValue)
                return;

            PlayerID playerID = actor.Owner.Value;
            if (_preparedTransferScenes.TryGetValue(playerID, out PreparedTransferScenes prepared)
                && prepared.Context == context)
                CleanupTransferPlayerState(playerID, actor, false);
            else
                ServerWorldManager.Instance.ReleaseTransferRuntimeUser(context, playerID);
        }

        internal void CleanupTransferInstanceAfterExit(Actor actor, ActorSpatialContext context)
        {
            if (actor == null || !actor.Owner.HasValue)
                return;

            PlayerID playerID = actor.Owner.Value;
            if (_preparedTransferScenes.TryGetValue(playerID, out PreparedTransferScenes prepared)
                && prepared.Context == context)
                CleanupTransferPlayerState(playerID, actor, false);
        }

        internal void OnPlayerActorRemoved(Actor actor)
        {
            if (actor == null || !actor.Owner.HasValue)
                return;
            RetirePlayerInterest(actor.Owner.Value, actor);
        }

        private void RetirePlayerInterest(PlayerID playerID, Actor expectedActor)
        {
            PlayerInterestState interestState = null;
            if (!ReferenceEquals(expectedActor, null))
                _actorInterest.TryGetValue(expectedActor, out interestState);

            if (interestState != null)
            {
                foreach (string sceneName in interestState.SceneNames)
                {
                    Game.Shared.Networking.ChunkSubscriptions.RemoveActorFromScene(
                        interestState.Context,
                        sceneName,
                        expectedActor);
                }

                foreach (string sceneName in interestState.SceneNames)
                {
                    if (TryParseChunkCoord(sceneName, out Vector2Int chunkCoord))
                        UnsubscribePlayerFromChunk(expectedActor, interestState.Context, chunkCoord);
                }

                _actorInterest.Remove(expectedActor);
            }

            if (!ReferenceEquals(expectedActor, null)
                && Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                    playerID,
                    out Actor registeredActor)
                && ReferenceEquals(registeredActor, expectedActor))
                Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(playerID);

            CleanupTransferPlayerState(playerID, expectedActor, true);
        }

        private void CleanupTransferPlayerState(PlayerID playerID, Actor actor, bool removeEntranceMembership)
        {
            if (_preparedTransferScenes.TryGetValue(playerID, out PreparedTransferScenes prepared))
            {
                prepared.Cancelled = true;
                foreach (string address in prepared.Addresses)
                    RemovePlayerFromInstanceScene(playerID, prepared.Context, address);

                _preparedTransferScenes.Remove(playerID);
                ServerWorldManager.Instance.ReleaseTransferRuntimeUser(prepared.Context, playerID);
            }

            ReleaseTransferEntranceRetention(playerID, actor, removeEntranceMembership);
        }

        private void ReleaseTransferEntranceRetention(
            PlayerID playerID,
            Actor actor,
            bool removeMembership)
        {
            if (!_retainedTransferEntranceChunks.Remove(playerID, out WorldChunkKey entranceChunk))
                return;

            bool remainsInPublicGrid = !removeMembership
                && actor != null
                && actor.SpatialContext == ServerWorldManager.Instance.CurrentRuntimeContext
                && _actorInterest.TryGetValue(actor, out PlayerInterestState state)
                && state.SceneNames.Contains($"World_{entranceChunk.ChunkCoord.x}_{entranceChunk.ChunkCoord.y}");
            if (!removeMembership && remainsInPublicGrid)
                return;

            if (!ServerWorldManager.Instance.TryGetChunkSceneID(
                    entranceChunk.Context,
                    entranceChunk.ChunkCoord,
                    out SceneID sceneID))
                return;

            if (_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
                _networkManager.scenePlayersModule.RemovePlayerFromScene(playerID, sceneID);
            UnloadChunkIfUnused(entranceChunk, sceneID);
        }

        private void RemovePlayerFromInstanceScene(
            PlayerID playerID,
            ActorSpatialContext context,
            string address)
        {
            if (!ServerWorldManager.Instance.TryGetInstanceSceneID(context, address, out SceneID sceneID))
                return;

            if (_networkManager.scenePlayersModule.IsPlayerInScene(playerID, sceneID))
                _networkManager.scenePlayersModule.RemovePlayerFromScene(playerID, sceneID);

            if (_networkManager.scenePlayersModule.TryGetPlayersAttachedToScene(sceneID, out var players)
                && players.Count == 0)
                ServerWorldManager.Instance.UnloadInstanceScene(context, address);
        }

        private void UnloadInstanceSceneIfUnused(
            ActorSpatialContext context,
            string address,
            SceneID sceneID)
        {
            if (sceneID != default
                && _networkManager.scenePlayersModule.TryGetPlayersAttachedToScene(sceneID, out var players)
                && players.Count == 0)
                ServerWorldManager.Instance.UnloadInstanceScene(context, address);
        }

        private void AddEnvironmentPreload(WorldChunkKey chunkKey)
        {
            if (_environmentPreloadCounts.TryGetValue(chunkKey, out int count))
            {
                _environmentPreloadCounts[chunkKey] = checked(count + 1);
                return;
            }

            _environmentPreloadCounts.Add(chunkKey, 1);
        }

        private void RemoveEnvironmentPreload(WorldChunkKey chunkKey)
        {
            if (!_environmentPreloadCounts.TryGetValue(chunkKey, out int count)
                || count <= 0)
            {
                throw new InvalidOperationException(
                    $"Environment preload bookkeeping is missing a positive count for {chunkKey}.");
            }

            if (count == 1)
            {
                _environmentPreloadCounts.Remove(chunkKey);
                return;
            }

            _environmentPreloadCounts[chunkKey] = count - 1;
        }

        private void UnloadChunkIfUnused(WorldChunkKey chunkKey, SceneID sceneID)
        {
            if (sceneID == default)
                return;
            if (_environmentPreloadCounts.TryGetValue(chunkKey, out int preloadCount)
                && preloadCount > 0)
                return;

            string chunkScene = $"World_{chunkKey.ChunkCoord.x}_{chunkKey.ChunkCoord.y}";
            foreach (PlayerInterestState state in _actorInterest.Values)
            {
                if (state.Context.Equals(chunkKey.Context)
                    && state.SceneNames.Contains(chunkScene))
                    return;
            }

            foreach (WorldChunkKey retainedChunk in _retainedTransferEntranceChunks.Values)
            {
                if (retainedChunk == chunkKey)
                    return;
            }

            if (_networkManager.scenePlayersModule.TryGetPlayersAttachedToScene(sceneID, out var players)
                && players.Count > 0)
                return;

            ServerWorldManager.Instance.UnloadChunk(chunkKey.Context, chunkKey.ChunkCoord);
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
