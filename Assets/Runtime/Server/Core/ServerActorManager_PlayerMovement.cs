using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Server.Core
{
    public sealed partial class ServerActorManager
    {
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
        private static readonly PerformanceMarker POSITION_VALIDATION_MARKER = new(POSITION_VALIDATION_DURATION_METRIC, "Server.ActorManager.ValidatePlayerPositions");
        private static readonly PerformanceMarker PLAYER_ECS_SYNC_MARKER = new(PLAYER_ECS_SYNC_DURATION_METRIC, "Server.ActorManager.SyncPlayerECS");

        // Player tracking for validation.
        private readonly Dictionary<PlayerID, Actor> _playerActors = new();
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
            // Instance validation rechecks the captured canonical position after async preload.
            // Do not admit movement proposals that could invalidate that transaction.
            if (_pendingInstanceValidationToggles.Contains(sender))
                return;

            if (!_initialized
                || ownershipEpoch <= 0
                || proposalSequence <= 0
                || !position.IsFinite)
            {
                return;
            }

            if (!_playerActors.TryGetValue(sender, out var actor)
                || actor == null
                || !actor.Owner.HasValue
                || actor.Owner.Value != sender
                || !actor.SpatialContext.IsValid
                || !_actorEntities.TryGetValue(actor.ID, out var entity)
                || !_entityManager.Exists(entity))
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
            foreach (var kvp in _playerActors)
            {
                var playerID = kvp.Key;
                var actor = kvp.Value;
                if (actor == null) continue;

                if (!_nextValidationTickIDs.TryGetValue(playerID, out var nextValidationTickID))
                    continue;
                if (!_lastValidationTickIDs.TryGetValue(playerID, out var lastValidationTickID))
                    continue;

                bool validationDue = tick.TickID >= nextValidationTickID;
                long validationIntervalTicks = 0;
                long elapsedTickCount = 0;
                if (validationDue)
                {
                    validationIntervalTicks = checked(nextValidationTickID - lastValidationTickID);
                    elapsedTickCount = checked(tick.TickID - lastValidationTickID);
                    if (validationIntervalTicks <= 0 || elapsedTickCount <= 0)
                        throw new InvalidOperationException("Player position validation deadline state is invalid.");
                }

                if (!_actorEntities.TryGetValue(actor.ID, out var entity)
                    || !_entityManager.Exists(entity))
                {
                    RemovePendingPositionProposal(playerID);
                    continue;
                }

                var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                var currentAcceptedPosition = ToWorldPosition(component.WorldPositionD);
                if (!currentAcceptedPosition.IsFinite)
                    throw new InvalidOperationException(
                        $"Player '{playerID}' has an invalid canonical ECS position.");

                bool hasEligibleProposal = false;
                WorldPosition candidatePosition = default;
                if (_pendingPositionProposals.TryGetValue(playerID, out var proposal))
                {
                    _pendingPositionProposals.Remove(playerID);
                    if (proposal.ActorID == actor.ID
                        && proposal.SpatialContext.IsCompatibleWith(actor.SpatialContext)
                        && proposal.OwnershipEpoch > 0
                        && proposal.ProposalSequence > 0
                        && proposal.Position.IsFinite)
                    {
                        hasEligibleProposal = true;
                        candidatePosition = proposal.Position;
                    }
                }

                if (!validationDue)
                {
                    if (hasEligibleProposal)
                    {
                        CommitPlayerPosition(entity, component, actor, candidatePosition);
                        physicalTransformsChanged = true;
                    }
                    continue;
                }

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

                AdvanceValidationDeadline(playerID, tick.TickID, validationIntervalTicks);
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
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                throw new InvalidOperationException("Player position commit requires initialized server physics manager.");

            var physicalPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(position);
            actor.transform.position = physicalPosition;
            component.WorldPositionD = new Unity.Mathematics.double3(position.x, position.y, position.z);
            _entityManager.SetComponentData(entity, component);
            GetSpatialIndex().Move(component.ActorID, actor.SpatialContext, position);
            ActorEndpoint.onServerAcceptedPlayerPositionChanged?.Invoke(
                actor,
                position);
        }

        private static void ProjectPlayerPhysicalPosition(
            Actor actor,
            WorldPosition position)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!position.IsFinite)
                throw new ArgumentException("The player physical position must be finite.", nameof(position));
            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                throw new InvalidOperationException("Player physical projection requires initialized server physics manager.");

            actor.transform.position = ServerPhysicsManager.Instance.ToPhysicalPosition(position);
        }

        /// <summary>Sends a client correction and projects it into the continuous physical frame.</summary>
        private static void SendPlayerCorrection(
            PlayerID playerID,
            Actor actor,
            WorldPosition correctionPosition)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!ServerPositionManager.IsInitialized || ServerPositionManager.Instance == null)
                throw new InvalidOperationException("Player correction requires initialized server position manager.");

            var (chunk, localPos) = ServerPositionManager.Instance.ToChunkLocal(correctionPosition);
            MovementEndpoint.Client_ServerCorrection(playerID, chunk, localPos);

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                throw new InvalidOperationException("Player correction requires initialized server physics manager.");
            ProjectPlayerPhysicalPosition(actor, correctionPosition);
        }

        /// <summary>Moves a player's periodic validation window to the current authoritative tick.</summary>
        /// <param name="playerID">The player whose deadline is advancing.</param>
        /// <param name="tickID">The current authoritative tick.</param>
        /// <param name="validationIntervalTicks">The configured validation interval in ticks.</param>
        private void AdvanceValidationDeadline(PlayerID playerID, long tickID, long validationIntervalTicks)
        {
            _lastValidationTickIDs[playerID] = tickID;
            _nextValidationTickIDs[playerID] = checked(tickID + validationIntervalTicks);
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
            long validationIntervalTicks = GetValidationIntervalTicks();
            RemovePendingPositionProposal(owner);
            _playerActors[owner] = actor;
            _periodicValidationBaselines[owner] = acceptedPosition;
            _lastValidationTickIDs[owner] = currentTickID;
            _nextValidationTickIDs[owner] = checked(currentTickID + validationIntervalTicks);
            _violationCounters[owner] = 0;
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

        private static long GetValidationIntervalTicks()
        {
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException("Player position validation requires initialized server simulation configuration.");

            ServerSimulationClock.ValidateConfiguration(GameConfigurationManager.Config);
            float fixedDeltaTime = GameConfigurationManager.Config.ServerSimulationTickDuration;
            double tickCountEstimate = VALIDATION_INTERVAL / (double)fixedDeltaTime;
            if (double.IsInfinity(tickCountEstimate) || tickCountEstimate > long.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(VALIDATION_INTERVAL),
                    "The player position validation interval cannot be represented by an authoritative tick deadline.");

            return ServerSimulationClock.GetNonShorteningTickCount(VALIDATION_INTERVAL, fixedDeltaTime);
        }
    }
}
