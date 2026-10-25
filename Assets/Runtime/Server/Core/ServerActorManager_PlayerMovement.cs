using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.World;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerActorManager
    {
        private readonly List<ServerWorldManager.TransferColliderCandidate> _transferColliderBuffer = new();

        private static readonly PerformanceMetricDefinition POSITION_VALIDATION_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_POSITION_VALIDATION_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition PLAYER_ECS_SYNC_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_MANAGER_PLAYER_ECS_SYNC_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition PLAYER_MOVEMENT_WORK_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_PLAYER_MOVEMENT_WORK_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private static readonly PerformanceMetricDefinition PENDING_POSITION_PROPOSAL_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_PENDING_POSITION_PROPOSAL_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private static readonly PerformanceMetricDefinition DUE_POSITION_VALIDATION_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_DUE_POSITION_VALIDATION_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private static readonly PerformanceMarker POSITION_VALIDATION_MARKER = new(POSITION_VALIDATION_DURATION_METRIC, "Server.ActorManager.ValidatePlayerPositions");
        private static readonly PerformanceMarker PLAYER_ECS_SYNC_MARKER = new(PLAYER_ECS_SYNC_DURATION_METRIC, "Server.ActorManager.SyncPlayerECS");

        // Player tracking for validation.
        private readonly Dictionary<PlayerID, Actor> _playerActors = new();
        private long _playerValidationIntervalTicks;
        private List<PlayerID>[] _playerValidationBuckets;
        private readonly List<PlayerID> _playerMovementWork = new();
        private readonly HashSet<PlayerID> _playerMovementWorkSet = new();
        // This is an anti-cheat snapshot only; ActorComponent.WorldPositionD remains the
        // authoritative accepted position and is updated on every authoritative tick.
        private readonly Dictionary<PlayerID, WorldPosition> _periodicValidationBaselines = new();
        private readonly Dictionary<PlayerID, long> _lastValidationTickIDs = new();
        private readonly Dictionary<PlayerID, long> _nextValidationTickIDs = new();
        private readonly Dictionary<PlayerID, int> _violationCounters = new();

        private readonly Dictionary<PlayerID, PendingPositionProposal> _pendingPositionProposals = new();
        private readonly Dictionary<PlayerID, ProposalOrdering> _lastProposalOrder = new();

        /// <summary>Stores the newest authenticated proposal waiting for the next authoritative tick.</summary>
        private readonly struct PendingPositionProposal
        {
            public PendingPositionProposal(
                Guid actorID,
                ActorSpatialContext spatialContext,
                WorldPosition position,
                long ownershipEpoch,
                long proposalSequence)
            {
                ActorID = actorID;
                SpatialContext = spatialContext;
                Position = position;
                OwnershipEpoch = ownershipEpoch;
                ProposalSequence = proposalSequence;
            }

            public Guid ActorID { get; }
            public ActorSpatialContext SpatialContext { get; }
            public WorldPosition Position { get; }
            public long OwnershipEpoch { get; }
            public long ProposalSequence { get; }
        }

        /// <summary>Identifies the latest proposal accepted for a player ownership epoch.</summary>
        private readonly struct ProposalOrdering
        {
            public ProposalOrdering(long ownershipEpoch, long proposalSequence)
            {
                OwnershipEpoch = ownershipEpoch;
                ProposalSequence = proposalSequence;
            }

            public long OwnershipEpoch { get; }
            public long ProposalSequence { get; }
        }

        // Generous speed cap: moveSpeed(5) * sqrt(2) diagonal * 2 headroom for latency bursts.
        private const float MAX_ALLOWED_SPEED = 15f;
        // Require sustained violations before correcting to tolerate network jitter.
        private const int VIOLATION_THRESHOLD = 3;
        private const float VALIDATION_INTERVAL = 0.5f;

        private void InitializePlayerValidationSchedule()
        {
            _playerValidationIntervalTicks = ServerSimulationClock.GetNonShorteningTickCount(
                VALIDATION_INTERVAL,
                GameConfigurationManager.Config.ServerSimulationTickDuration);
            if (_playerValidationIntervalTicks <= 0 || _playerValidationIntervalTicks > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "The player validation interval must be positive and representable as a validation bucket array length.");
            }

            int bucketCount = checked((int)_playerValidationIntervalTicks);
            _playerValidationBuckets = new List<PlayerID>[bucketCount];
            for (int i = 0; i < _playerValidationBuckets.Length; i++)
                _playerValidationBuckets[i] = new List<PlayerID>();
        }

        private void SchedulePlayerValidation(
            PlayerID playerID,
            long dueTickID)
        {
            if (_playerValidationIntervalTicks <= 0 || _playerValidationBuckets == null)
            {
                throw new InvalidOperationException(
                    "Player validation scheduling has not been initialized.");
            }

            if (ServerECSManager.CurrentSimulationTickID > 0)
            {
                if (dueTickID <= ServerECSManager.CurrentSimulationTickID)
                {
                    throw new InvalidOperationException(
                        $"Player validation deadline {dueTickID} must be after active simulation tick "
                        + $"{ServerECSManager.CurrentSimulationTickID}.");
                }
            }
            else if (dueTickID <= ServerSimulationClock.CurrentTickID)
            {
                throw new InvalidOperationException(
                    $"Player validation deadline {dueTickID} must be after current simulation tick "
                    + $"{ServerSimulationClock.CurrentTickID}.");
            }

            int bucketIndex = (int)(dueTickID % _playerValidationIntervalTicks);
            _playerValidationBuckets[bucketIndex].Add(playerID);
        }

        /// <summary>
        /// Authenticates and coalesces the newest owner proposal for consumption by the next
        /// authoritative tick; validation and ECS commitment occur at the simulation boundary.
        /// </summary>
        /// <param name="sender">The player that submitted the proposal.</param>
        /// <param name="position">The proposed absolute world position.</param>
        /// <param name="ownershipEpoch">The sender's current ownership epoch.</param>
        /// <param name="proposalSequence">The monotonically increasing proposal sequence.</param>
        private void OnServerPositionProposal(
            PlayerID sender,
            WorldPosition position,
            long ownershipEpoch,
            long proposalSequence)
        {
            // Spatial-context transfers recheck the captured canonical position after async preload.
            // Do not admit movement proposals that could invalidate that transaction.
            if (_pendingSpatialContextTransfers.Contains(sender))
                return;

            if (ownershipEpoch <= 0
                || proposalSequence <= 0
                || !position.IsFinite)
            {
                return;
            }

            if (!_playerActors.TryGetValue(sender, out var actor)
                || actor == null
                || !actor.Owner.HasValue
                || actor.Owner.Value != sender)
            {
                return;
            }

            if (_lastProposalOrder.TryGetValue(sender, out var lastOrder)
                && (ownershipEpoch < lastOrder.OwnershipEpoch
                    || ownershipEpoch == lastOrder.OwnershipEpoch
                    && proposalSequence <= lastOrder.ProposalSequence))
            {
                return;
            }

            _lastProposalOrder[sender] = new ProposalOrdering(ownershipEpoch, proposalSequence);
            _pendingPositionProposals[sender] = new PendingPositionProposal(
                actor.ID,
                actor.SpatialContext,
                position,
                ownershipEpoch,
                proposalSequence);
        }

        /// <summary>
        /// Admits the latest authenticated owner proposal for this tick and independently performs
        /// any due periodic speed check against the committed ECS position. The networked transform
        /// is never treated as an absolute coordinate source.
        /// </summary>
        private bool ValidatePlayerPositions(ServerSimulationTick tick)
        {
            using var markerScope = POSITION_VALIDATION_MARKER.Begin();
            bool physicalTransformsChanged = false;
            _playerMovementWork.Clear();
            _playerMovementWorkSet.Clear();

            int pendingProposalCount = _pendingPositionProposals.Count;
            foreach (PlayerID playerID in _pendingPositionProposals.Keys)
            {
                if (_playerMovementWorkSet.Add(playerID))
                    _playerMovementWork.Add(playerID);
            }

            if (_playerValidationIntervalTicks <= 0 || _playerValidationBuckets == null)
            {
                throw new InvalidOperationException(
                    "Player validation scheduling has not been initialized.");
            }

            int bucketIndex = (int)(tick.TickID % _playerValidationIntervalTicks);
            List<PlayerID> validationBucket = _playerValidationBuckets[bucketIndex];
            int dueValidationCount = 0;
            foreach (PlayerID playerID in validationBucket)
            {
                if (!_nextValidationTickIDs.TryGetValue(playerID, out long dueTickID)
                    || dueTickID > tick.TickID)
                {
                    continue;
                }

                dueValidationCount++;
                if (_playerMovementWorkSet.Add(playerID))
                    _playerMovementWork.Add(playerID);
            }
            validationBucket.Clear();

            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(
                    PENDING_POSITION_PROPOSAL_COUNT_METRIC,
                    pendingProposalCount,
                    tick.TickID);
                PerformanceTelemetry.TryRecord(
                    DUE_POSITION_VALIDATION_COUNT_METRIC,
                    dueValidationCount,
                    tick.TickID);
                PerformanceTelemetry.TryRecord(
                    PLAYER_MOVEMENT_WORK_COUNT_METRIC,
                    _playerMovementWork.Count,
                    tick.TickID);
            }

            foreach (PlayerID playerID in _playerMovementWork)
            {
                if (!_playerActors.TryGetValue(playerID, out var actor))
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no registered actor.");
                }

                if (actor == null)
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no registered actor.");
                }

                if (!_nextValidationTickIDs.TryGetValue(playerID, out var nextValidationTickID))
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no validation deadline.");
                }

                bool validationDue = tick.TickID >= nextValidationTickID;

                bool hasEligibleProposal = false;
                WorldPosition candidatePosition = default;
                if (_pendingPositionProposals.TryGetValue(playerID, out var proposal))
                {
                    _pendingPositionProposals.Remove(playerID);
                    if (proposal.ActorID == actor.ID
                        && proposal.SpatialContext.IsCompatibleWith(actor.SpatialContext))
                    {
                        hasEligibleProposal = true;
                        candidatePosition = proposal.Position;
                    }
                }

                if (!validationDue && !hasEligibleProposal)
                    continue;

                if (!TryGetActorEntity(actor.ID, out var entity))
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no corresponding ECS entity.");
                }

                if (!_entityManager.Exists(entity))
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' maps to a destroyed ECS entity.");
                }

                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                var currentAcceptedPosition = ToWorldPosition(component.WorldPositionD);

                if (!validationDue)
                {
                    CommitPlayerPosition(entity, component, actor, candidatePosition);
                    physicalTransformsChanged = true;
                    continue;
                }

                if (!_lastValidationTickIDs.TryGetValue(playerID, out var lastValidationTickID))
                {
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no validation baseline tick.");
                }

                long elapsedTickCount = checked(tick.TickID - lastValidationTickID);
                if (nextValidationTickID <= lastValidationTickID || elapsedTickCount <= 0)
                    throw new InvalidOperationException("Player position validation deadline state is invalid.");

                if (!_periodicValidationBaselines.TryGetValue(playerID, out var baselinePosition))
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has no periodic position validation baseline.");
                if (!baselinePosition.IsFinite)
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has an invalid periodic position validation baseline.");

                if (!hasEligibleProposal)
                {
                    // A missing or rejected proposal does not bypass the due check; validate the
                    // position already admitted to the canonical ECS component.
                    candidatePosition = currentAcceptedPosition;
                }

                double elapsed = elapsedTickCount * (double)tick.DeltaTime;
                if (elapsed <= 0d || double.IsNaN(elapsed) || double.IsInfinity(elapsed))
                    throw new InvalidOperationException("Player position validation elapsed time is invalid.");

                double deltaX = candidatePosition.x - baselinePosition.x;
                double deltaZ = candidatePosition.z - baselinePosition.z;
                double speed = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ) / elapsed;

                if (speed > MAX_ALLOWED_SPEED)
                {
                    _violationCounters[playerID] = _violationCounters.GetValueOrDefault(playerID) + 1;

                    if (_violationCounters[playerID] >= VIOLATION_THRESHOLD)
                    {
                        Debug.Warning("Player " + playerID + " exceeded max speed (" + speed.ToString("F1") + " m/s) " + _violationCounters[playerID] + " times. Correcting.");
                        double acceptedDeltaX = currentAcceptedPosition.x - baselinePosition.x;
                        double acceptedDeltaZ = currentAcceptedPosition.z - baselinePosition.z;
                        double acceptedSpeed = Math.Sqrt(
                            acceptedDeltaX * acceptedDeltaX
                            + acceptedDeltaZ * acceptedDeltaZ) / elapsed;

                        var correctionPosition = acceptedSpeed <= MAX_ALLOWED_SPEED
                            ? currentAcceptedPosition
                            : baselinePosition;
                        // Ensure ECS is corrected when an earlier tick already admitted illegal movement.
                        CommitPlayerPosition(entity, component, actor, correctionPosition);
                        physicalTransformsChanged = true;
                        SendPlayerCorrection(playerID, actor, correctionPosition);
                        _periodicValidationBaselines[playerID] = correctionPosition;
                        _violationCounters[playerID] = 0;
                    }
                    else
                    {
                        if (hasEligibleProposal)
                        {
                            CommitPlayerPosition(entity, component, actor, candidatePosition);
                            physicalTransformsChanged = true;
                        }
                        _periodicValidationBaselines[playerID] = candidatePosition;
                    }
                }
                else
                {
                    _violationCounters[playerID] = 0;
                    if (hasEligibleProposal)
                    {
                        CommitPlayerPosition(entity, component, actor, candidatePosition);
                        physicalTransformsChanged = true;
                    }
                    _periodicValidationBaselines[playerID] = candidatePosition;
                }

                AdvanceValidationDeadline(playerID, tick.TickID);
            }

            return physicalTransformsChanged;
        }

        /// <summary>Writes one accepted absolute player position to the authoritative ECS component.</summary>
        /// <param name="entity">The player's authoritative ECS entity.</param>
        /// <param name="component">The current actor component value.</param>
        /// <param name="actor">The actor whose physical transform is updated.</param>
        /// <param name="position">The absolute position to commit.</param>
        private void CommitPlayerPosition(
            Entity entity,
            Systems.ActorComponent component,
            Actor actor,
            WorldPosition position)
        {
            using var markerScope = PLAYER_ECS_SYNC_MARKER.Begin();

            WorldPosition previousPosition = ToWorldPosition(component.WorldPositionD);
            bool positionChanged = !AreWorldPositionsEqual(previousPosition, position);
            var physicalPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(position);
            actor.transform.position = physicalPosition;
            component.WorldPositionD = new Unity.Mathematics.double3(position.x, position.y, position.z);
            _entityManager.SetComponentData(entity, component);
            if (positionChanged)
                _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity, true);
            _spatialIndex.Move(component.ActorID, actor.SpatialContext, position);
            EvaluateTransferColliderCrossing(actor, previousPosition, position);
            ActorEndpoint.onServerAcceptedPlayerPositionChanged?.Invoke(
                actor,
                position);
        }

        private void EvaluateTransferColliderCrossing(
            Actor actor,
            WorldPosition previousPosition,
            WorldPosition currentPosition)
        {
            if (_pendingSpatialContextTransfers.Contains(actor.Owner.Value)
                || AreWorldPositionsEqual(previousPosition, currentPosition))
            {
                return;
            }

            _transferColliderBuffer.Clear();
            ServerWorldManager worldManager = ServerWorldManager.Instance;
            worldManager.AppendTransferColliders(
                actor.SpatialContext,
                previousPosition,
                currentPosition,
                _transferColliderBuffer);
            if (_transferColliderBuffer.Count == 0)
                return;

            Vector3 previousPhysicalPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(previousPosition);
            Vector3 currentPhysicalPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(currentPosition);
            ActorSpatialContext publicContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            TransferCollider selectedCollider = null;
            WorldChunkKey selectedEntranceChunk = default;
            TransferCollider.CrossingDirection selectedDirection = default;
            int eligibleCrossingCount = 0;

            foreach (ServerWorldManager.TransferColliderCandidate candidate in _transferColliderBuffer)
            {
                TransferCollider collider = candidate.Collider;
                if (collider == null
                    || !collider.TryGetCrossing(
                        previousPhysicalPosition,
                        currentPhysicalPosition,
                        out TransferCollider.CrossingDirection direction,
                        out _))
                {
                    continue;
                }

                bool eligible;
                if (direction == TransferCollider.CrossingDirection.IntoInstance)
                {
                    eligible = actor.SpatialContext == publicContext;
                }
                else
                {
                    eligible = worldManager.TryGetTransferRuntime(actor.SpatialContext, out var runtime)
                        && runtime.EntranceCollider == collider;
                }
                if (!eligible)
                    continue;

                eligibleCrossingCount++;
                if (eligibleCrossingCount == 1)
                {
                    selectedCollider = collider;
                    selectedEntranceChunk = candidate.ChunkKey;
                    selectedDirection = direction;
                }
            }

            if (eligibleCrossingCount > 1)
            {
                Debug.Error(
                    $"[TransferCollider] Actor {actor.ID} crossed multiple eligible planes in one accepted segment; "
                    + "correct the environment authoring; no transfer was started.");
                return;
            }
            if (eligibleCrossingCount == 0)
                return;

            ActorSpatialContext destinationContext;
            try
            {
                destinationContext = selectedDirection == TransferCollider.CrossingDirection.IntoInstance
                    ? worldManager.CreateTransferRuntime(selectedEntranceChunk, selectedCollider)
                    : publicContext;
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"[TransferCollider] Player {actor.ID} direction={selectedDirection} "
                    + $"transfer failed: {exception.Message}");
                return;
            }

            bool inward = selectedDirection == TransferCollider.CrossingDirection.IntoInstance;
            ActorSpatialContext sourceContext = actor.SpatialContext;
            if (inward)
                worldManager.AddTransferRuntimeUser(destinationContext, actor.Owner.Value);
            Action<Action<bool>> prepareDestination = inward
                ? callback => ServerInterestManager.Instance.PrepareTransferInstanceAdmission(
                    actor,
                    destinationContext,
                    selectedEntranceChunk,
                    worldManager.TryGetTransferRuntime(destinationContext, out var transferRuntime)
                        ? transferRuntime.SceneAddresses
                        : throw new InvalidOperationException($"Transfer runtime {destinationContext} was not registered."),
                    callback)
                : callback => ServerInterestManager.Instance.EnsureInterestGridLoaded(
                    destinationContext,
                    currentPosition,
                    callback);

            bool started = TryBeginPlayerSpatialContextTransfer(
                actor,
                destinationContext,
                prepareDestination,
                (succeeded, reason) =>
                {
                    if (!succeeded)
                    {
                        if (inward)
                            ServerInterestManager.Instance.CancelTransferInstanceAdmission(actor, destinationContext);
                        Debug.Error(
                            $"[TransferCollider] Player {actor.ID} direction={selectedDirection} "
                            + $"transfer failed: {reason}");
                        return;
                    }

                    if (selectedDirection == TransferCollider.CrossingDirection.IntoInstance)
                    {
                        Debug.Log(
                            $"[TransferCollider] Player {actor.ID} entered "
                            + $"instance={destinationContext.InstanceID} "
                            + $"position=({currentPosition.x:F3},{currentPosition.y:F3},{currentPosition.z:F3})");
                    }
                    else
                    {
                        Debug.Log(
                            $"[TransferCollider] Player {actor.ID} returned to public "
                            + $"position=({currentPosition.x:F3},{currentPosition.y:F3},{currentPosition.z:F3})");
                        ServerInterestManager.Instance.CleanupTransferInstanceAfterExit(actor, sourceContext);
                    }
                });

            if (!started)
            {
                if (inward)
                    worldManager.ReleaseTransferRuntimeUser(destinationContext, actor.Owner.Value);
                Debug.Error(
                    $"[TransferCollider] Player {actor.ID} direction={selectedDirection} "
                    + "transfer failed: another spatial-context transfer is already pending");
            }
        }

        /// <summary>Sends a client correction and projects it into the continuous physical frame.</summary>
        private static void SendPlayerCorrection(
            PlayerID playerID,
            Actor actor,
            WorldPosition correctionPosition)
        {
            Vector3 physicalCorrectionPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(correctionPosition);

            var (chunk, localPos) = ServerPositionManager.Instance.ToChunkLocal(correctionPosition);
            MovementEndpoint.Client_ServerCorrection(playerID, chunk, localPos);

            actor.transform.position = physicalCorrectionPosition;
        }

        /// <summary>Moves a player's periodic validation window to the current authoritative tick.</summary>
        /// <param name="playerID">The player whose deadline is advancing.</param>
        /// <param name="tickID">The current authoritative tick.</param>
        private void AdvanceValidationDeadline(PlayerID playerID, long tickID)
        {
            _lastValidationTickIDs[playerID] = tickID;
            long dueTickID = checked(tickID + _playerValidationIntervalTicks);
            _nextValidationTickIDs[playerID] = dueTickID;
            SchedulePlayerValidation(playerID, dueTickID);
        }

        /// <summary>
        /// Initializes owner tracking, proposal ordering and the periodic anti-cheat baseline
        /// from the already accepted ECS spawn or ownership-transition position.
        /// </summary>
        /// <param name="owner">The owning player.</param>
        /// <param name="actor">The actor owned by the player.</param>
        /// <param name="acceptedPosition">The current accepted absolute position.</param>
        private void InitializePlayerValidationState(PlayerID owner, Actor actor, WorldPosition acceptedPosition)
        {
            long currentTickID = ServerSimulationClock.CurrentTickID;
            long dueTickID = checked(currentTickID + _playerValidationIntervalTicks);
            RemovePendingPositionProposal(owner);
            _playerActors[owner] = actor;
            _periodicValidationBaselines[owner] = acceptedPosition;
            _lastValidationTickIDs[owner] = currentTickID;
            _nextValidationTickIDs[owner] = dueTickID;
            _violationCounters[owner] = 0;
            SchedulePlayerValidation(owner, dueTickID);
        }

        /// <summary>Removes all authoritative movement state associated with a player owner.</summary>
        /// <param name="owner">The player whose movement state is being removed.</param>
        private void RemovePlayerValidationState(PlayerID owner)
        {
            _playerActors.Remove(owner);
            _periodicValidationBaselines.Remove(owner);
            _lastValidationTickIDs.Remove(owner);
            _nextValidationTickIDs.Remove(owner);
            _violationCounters.Remove(owner);
            RemovePendingPositionProposal(owner);
        }

        /// <summary>Discards a player's pending proposal and ordering state.</summary>
        /// <param name="owner">The player whose proposal state is being removed.</param>
        private void RemovePendingPositionProposal(PlayerID owner)
        {
            _pendingPositionProposals.Remove(owner);
            _lastProposalOrder.Remove(owner);
        }

        /// <summary>Converts an ECS double-precision position to the shared world-position value.</summary>
        /// <param name="position">The ECS position to convert.</param>
        /// <returns>The equivalent absolute world position.</returns>
        private static WorldPosition ToWorldPosition(Unity.Mathematics.double3 position)
        {
            return new WorldPosition(position.x, position.y, position.z);
        }

    }
}
