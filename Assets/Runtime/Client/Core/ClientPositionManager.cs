using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client
{
    /// <summary>
    /// Manages the client's floating origin. When the player's chunk interest
    /// changes, all loaded scene roots are shifted so the current chunk origin
    /// sits at (0, 0, 0) in Unity space, keeping floats precise.
    /// </summary>
    public class ClientPositionManager : MonoBehaviour
    {
        private static readonly PerformanceMarker ORIGIN_SHIFT_MARKER = new(
            ClientPerformanceMetrics.ORIGIN_SHIFT_DURATION,
            "Client.PositionManager.OriginShift");
        private static ClientPositionManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientPositionManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        /// <summary> The chunk the client currently considers as origin. </summary>
        public Vector2Int OriginChunk { get; private set; }

        private int _chunkSize;

        private struct RemoteActorPositionState
        {
            public WorldPosition StartAbsolute;
            public WorldPosition TargetAbsolute;
            public long TargetTickID;
            public float StartTime;
            public float Duration;
            public bool Initialized;
        }

        private readonly Dictionary<Actor, RemoteActorPositionState> _remoteActorPositions = new();

        internal bool HasPresentationState(Actor actor)
        {
            if (actor == null)
                return false;

            return actor == ClientAccountManager.PlayerActor
                || _remoteActorPositions.ContainsKey(actor);
        }

        private void Awake()
        {
            _instance = this;
            _chunkSize = Game.Shared.GameConfigurationManager.Config.ChunkSize;
            _initialized = true;
            ActorEndpoint.onClientAcceptedPositionChanged += OnClientAcceptedPositionChanged;
            ActorEndpoint.onActorDespawned += OnActorDespawned;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onClientAcceptedPositionChanged -= OnClientAcceptedPositionChanged;
            ActorEndpoint.onActorDespawned -= OnActorDespawned;
            _remoteActorPositions.Clear();

            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// Offsets a newly loaded scene's root objects so they align with the
        /// current floating origin. Call this once immediately after a world
        /// scene finishes loading.
        /// </summary>
        public void ApplyOriginToScene(Scene scene)
        {
            using var markerScope = ORIGIN_SHIFT_MARKER.Begin();
            var originOffset = new Vector3(OriginChunk.x * _chunkSize, 0f, OriginChunk.y * _chunkSize);
            int rootCount = ShiftAllSceneRoots(scene, originOffset);

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_ROOT_COUNT, rootCount);
        }

        /// <summary>
        /// Call this when the player crosses a chunk boundary.
        /// Shifts every root object in every loaded scene (including global_actors)
        /// so the new chunk origin sits at world-space (0, 0, 0).
        /// </summary>
        public void SetOriginChunk(Vector2Int newChunk)
        {
            Vector2Int previousOrigin = OriginChunk;
            Vector2Int delta = newChunk - OriginChunk;
            if (delta == Vector2Int.zero)
                return;

            var playerActor = ClientAccountManager.PlayerActor;
            bool hasPlayerActor = playerActor != null;
            WorldPosition playerAbsolute = default;

            if (hasPlayerActor)
            {
                var (playerChunk, playerLocal) =
                    LocalToChunkRelative(playerActor.transform.position);

                playerAbsolute = WorldPosition.FromChunkLocal(
                    playerChunk,
                    playerLocal);

                if (!playerAbsolute.IsFinite)
                    throw new InvalidOperationException(
                        "Cannot rebase the client origin from a non-finite local player position.");
            }

            using var markerScope = ORIGIN_SHIFT_MARKER.Begin();

            var shift = new Vector3(delta.x * _chunkSize, 0f, delta.y * _chunkSize);
            int rootCount = 0;

            // Shift world chunk scenes.
            var worldScenes = new List<Scene>();
            ClientWorldManager.Instance.GetWorldScenes(worldScenes);

            foreach (var scene in worldScenes)
            {
                if (!scene.isLoaded) continue;
                rootCount += ShiftAllSceneRoots(scene, shift);
            }

            // Shift the global_actors scene so networked actors stay aligned.
            var globalActors = SceneManager.GetSceneByName("global_actors");
            if (globalActors.isLoaded)
                rootCount += ShiftAllSceneRoots(globalActors, shift);

            OriginChunk = newChunk;

            if (hasPlayerActor)
            {
                var previousLocalPosition = playerActor.transform.position;
                var newLocalPosition = NetworkToLocal(
                    playerAbsolute.Chunk,
                    playerAbsolute.LocalPosition);
                playerActor.transform.position = newLocalPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    $"[Movement/Origin] actor='{playerActor.Name}' ({playerActor.ID}) "
                    + $"oldLocal={previousLocalPosition} newLocal={newLocalPosition}.");
#endif
            }

            float rebaseTime = Time.unscaledTime;
            foreach (var pair in _remoteActorPositions)
            {
                var actor = pair.Key;
                if (actor == null
                    || actor == playerActor)
                {
                    continue;
                }

                var previousLocalPosition = actor.transform.position;
                WorldPosition presentationAbsolute = EvaluateRemotePosition(pair.Value, rebaseTime);
                var newLocalPosition = NetworkToLocal(
                    presentationAbsolute.Chunk,
                    presentationAbsolute.LocalPosition);
                actor.transform.position = newLocalPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    $"[Movement/Origin] actor='{actor.Name}' ({actor.ID}) "
                    + $"oldLocal={previousLocalPosition} newLocal={newLocalPosition}.");
#endif
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var appliedLocalShift = -shift;
            Debug.Log(
                $"[Movement/Origin] old=({previousOrigin.x},{previousOrigin.y}) "
                + $"new=({newChunk.x},{newChunk.y}) "
                + $"localShift=({appliedLocalShift.x:F3},{appliedLocalShift.y:F3},{appliedLocalShift.z:F3}).");
#endif

            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_COUNT, 1);
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.ORIGIN_SHIFT_ROOT_COUNT, rootCount);
            }
        }

        private void OnClientAcceptedPositionChanged(
            Actor actor,
            ActorPositionSnapshot snapshot)
        {
            if (actor == null)
                throw new InvalidOperationException("Cannot project an accepted position for a null actor.");
            if (!snapshot.IsValid || snapshot.TickID < 0 || !snapshot.Position.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} received an invalid accepted position snapshot.");
            }

            if (actor == ClientAccountManager.PlayerActor)
                return;

            float snapshotTime = Time.unscaledTime;
            if (!_remoteActorPositions.TryGetValue(actor, out var state)
                || !state.Initialized)
            {
                state = new RemoteActorPositionState
                {
                    StartAbsolute = snapshot.Position,
                    TargetAbsolute = snapshot.Position,
                    TargetTickID = snapshot.TickID,
                    StartTime = snapshotTime,
                    Duration = 0f,
                    Initialized = true
                };
                _remoteActorPositions[actor] = state;
                actor.transform.position = NetworkToLocal(
                    snapshot.Position.Chunk,
                    snapshot.Position.LocalPosition);
                return;
            }

            if (snapshot.TickID <= state.TargetTickID)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} received accepted position tick {snapshot.TickID} "
                    + $"after {state.TargetTickID}.");
            }

            WorldPosition currentInterpolatedAbsolute = EvaluateRemotePosition(state, snapshotTime);
            long tickDelta = checked(snapshot.TickID - state.TargetTickID);
            if (tickDelta <= 0)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} received a nonpositive accepted position tick delta.");
            }

            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            if (!IsFinite(tickDuration) || tickDuration <= 0f)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameConfiguration.ServerSimulationTickDuration)} must be finite and positive.");
            }

            float duration = (float)(tickDelta * (double)tickDuration);
            if (!IsFinite(duration) || duration <= 0f)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} received an invalid interpolation duration.");
            }

            state = new RemoteActorPositionState
            {
                StartAbsolute = currentInterpolatedAbsolute,
                TargetAbsolute = snapshot.Position,
                TargetTickID = snapshot.TickID,
                StartTime = snapshotTime,
                Duration = duration,
                Initialized = true
            };
            _remoteActorPositions[actor] = state;
        }

        private void Update()
        {
            float presentationTime = Time.unscaledTime;
            List<Actor> destroyedActors = null;

            foreach (var pair in _remoteActorPositions)
            {
                var actor = pair.Key;
                if (actor == null)
                {
                    (destroyedActors ??= new List<Actor>()).Add(actor);
                    continue;
                }

                if (actor == ClientAccountManager.PlayerActor)
                    continue;

                WorldPosition presentationAbsolute = EvaluateRemotePosition(pair.Value, presentationTime);
                actor.transform.position = NetworkToLocal(
                    presentationAbsolute.Chunk,
                    presentationAbsolute.LocalPosition);
            }

            if (destroyedActors == null)
                return;

            foreach (var actor in destroyedActors)
                _remoteActorPositions.Remove(actor);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            _remoteActorPositions.Remove(actor);
        }

        private static WorldPosition InterpolateAbsolute(
            WorldPosition start,
            WorldPosition target,
            double t)
        {
            if (!start.IsFinite || !target.IsFinite)
                throw new InvalidOperationException("Cannot interpolate non-finite world positions.");
            if (double.IsNaN(t) || double.IsInfinity(t))
                throw new InvalidOperationException("Cannot interpolate with a non-finite interpolation factor.");

            t = Math.Max(0d, Math.Min(1d, t));

            var result = new WorldPosition(
                start.x + (target.x - start.x) * t,
                start.y + (target.y - start.y) * t,
                start.z + (target.z - start.z) * t);
            if (!result.IsFinite)
                throw new InvalidOperationException("World position interpolation produced a non-finite result.");

            return result;
        }

        private static WorldPosition EvaluateRemotePosition(
            RemoteActorPositionState state,
            float time)
        {
            if (!state.Initialized)
                throw new InvalidOperationException("Cannot evaluate an uninitialized remote actor position.");
            if (!state.StartAbsolute.IsFinite || !state.TargetAbsolute.IsFinite)
                throw new InvalidOperationException("Cannot evaluate a remote actor from non-finite positions.");
            if (float.IsNaN(state.Duration) || float.IsInfinity(state.Duration))
                throw new InvalidOperationException("Cannot evaluate a remote actor with a non-finite duration.");
            if (state.Duration <= 0f)
                return state.TargetAbsolute;

            double t = (time - state.StartTime) / state.Duration;
            return InterpolateAbsolute(state.StartAbsolute, state.TargetAbsolute, t);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int ShiftAllSceneRoots(Scene scene, Vector3 shift)
        {
            int rootCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                root.transform.position -= shift;
                rootCount++;
            }

            return rootCount;
        }

        /// <summary>
        /// Converts a chunk-coordinate world position to Unity local space
        /// accounting for the current floating origin.
        /// </summary>
        public Vector3 ChunkToLocal(Vector2Int chunk, Vector3 positionInChunk)
        {
            Vector2Int offset = chunk - OriginChunk;
            return new Vector3(
                offset.x * _chunkSize + positionInChunk.x,
                positionInChunk.y,
                offset.y * _chunkSize + positionInChunk.z
            );
        }

        /// <summary>
        /// Single entry point for converting a server-sent chunk-relative position
        /// into Unity local space. All incoming network position handlers should
        /// call this instead of using raw floats.
        /// </summary>
        public Vector3 NetworkToLocal(Vector2Int chunk, Vector3 localPosition)
        {
            return ChunkToLocal(chunk, localPosition);
        }

        /// <summary>
        /// Converts a Unity local-space position back to chunk-relative format
        /// suitable for sending to the server.
        /// </summary>
        public (Vector2Int chunk, Vector3 local) LocalToChunkRelative(Vector3 unityPosition)
        {
            float halfCell = _chunkSize * 0.5f;
            int cx = Mathf.FloorToInt(
                (unityPosition.x + halfCell) / _chunkSize)
                + OriginChunk.x;
            int cz = Mathf.FloorToInt(
                (unityPosition.z + halfCell) / _chunkSize)
                + OriginChunk.y;
            var chunk = new Vector2Int(cx, cz);

            Vector2Int offset = chunk - OriginChunk;
            var local = new Vector3(
                unityPosition.x - offset.x * _chunkSize,
                unityPosition.y,
                unityPosition.z - offset.y * _chunkSize
            );
            return (chunk, local);
        }
    }
}
