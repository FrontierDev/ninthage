using Game.Server.Networking.Master;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Persistence
{
    internal sealed partial class WorldPersistenceCoordinator
    {
        private const int CHECKPOINT_INTERVAL_SECONDS = 30;
        private const int MAX_CHECKPOINT_BATCH_SIZE = 128;

        /// <summary>Queues or replaces the newest dirty checkpoint for a locally leased character.</summary>
        internal MasterPersistenceProtocol.PersistenceResult QueueCharacterCheckpoint(
            Guid characterID,
            WorldPersistenceCheckpoint checkpoint)
        {
            if (characterID == Guid.Empty || checkpoint == null)
                return MasterPersistenceProtocol.PersistenceResult.InvalidRequest;

            lock (_stateLock)
            {
                if (!_acceptingWork)
                    return MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable;
                if (!_characterLanes.TryGetValue(characterID, out CharacterOperationLane lane)
                    || !lane.HasOwnedLease)
                {
                    return MasterPersistenceProtocol.PersistenceResult.StaleLease;
                }
                if (!IsTransportSafeCheckpoint(checkpoint))
                    return MasterPersistenceProtocol.PersistenceResult.InvalidRequest;

                StorePendingCheckpoint(lane, checkpoint);
                return lane.BlockingResult
                    ?? MasterPersistenceProtocol.PersistenceResult.Accepted;
            }
        }

        /// <summary>Flushes the dirty checkpoint snapshot that exists when the cycle begins.</summary>
        internal async Task FlushPendingCheckpointsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCleanupOperation();
            try
            {
                List<Guid> characterIDs = SnapshotDirtyCharacterIDs();
                for (int startIndex = 0; startIndex < characterIDs.Count; startIndex += MAX_CHECKPOINT_BATCH_SIZE)
                {
                    int count = Math.Min(
                        MAX_CHECKPOINT_BATCH_SIZE,
                        characterIDs.Count - startIndex);
                    bool stopCycle = await FlushCheckpointBatchAsync(
                        characterIDs,
                        startIndex,
                        count,
                        cancellationToken).ConfigureAwait(false);
                    if (stopCycle)
                        break;
                }
            }
            finally
            {
                CompleteOperation();
            }
        }

        /// <summary>Runs periodic checkpoint cycles until caller cancellation.</summary>
        internal async Task RunCheckpointLoopAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(CHECKPOINT_INTERVAL_SECONDS),
                    cancellationToken).ConfigureAwait(false);
                await FlushPendingCheckpointsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> FlushCheckpointBatchAsync(
            List<Guid> characterIDs,
            int startIndex,
            int count,
            CancellationToken cancellationToken)
        {
            List<CharacterLaneHandle> laneHandles = new();
            List<CheckpointWork> workItems = new();
            try
            {
                for (int index = startIndex; index < startIndex + count; index++)
                {
                    Guid characterID = characterIDs[index];
                    CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                        characterID,
                        cancellationToken).ConfigureAwait(false);
                    laneHandles.Add(laneHandle);

                    if (TryGetCheckpointWork(characterID, laneHandle.Lane, out CheckpointWork work))
                        workItems.Add(work);
                }

                if (workItems.Count == 0)
                    return false;

                MasterPersistenceProtocol.CharacterCheckpointBatchRequest request =
                    BuildCheckpointRequest(workItems);
                MasterPersistenceProtocol.CharacterCheckpointBatchResponse response;
                try
                {
                    response = await _client.CheckpointCharactersAsync(
                        request,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
                catch (TimeoutException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                    return true;
                }

                if (response.Result == MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable)
                    return true;
                if (response.Result == MasterPersistenceProtocol.PersistenceResult.InvalidRequest)
                {
                    throw new InvalidOperationException(
                        "The coordinator-built checkpoint batch was rejected as invalid.");
                }
                if (IsWorldBlockingResult(response.Result))
                {
                    foreach (CheckpointWork work in workItems)
                        SetBlockingResult(work.Lane, response.Result);
                    return false;
                }
                if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    throw new InvalidOperationException("The Master checkpoint response had an unexpected result.");
                }

                SettleCheckpointBatch(workItems, response);
                return false;
            }
            finally
            {
                for (int index = laneHandles.Count - 1; index >= 0; index--)
                    laneHandles[index].Dispose();
            }
        }

        private List<Guid> SnapshotDirtyCharacterIDs()
        {
            lock (_stateLock)
            {
                List<Guid> characterIDs = new();
                foreach (KeyValuePair<Guid, CharacterOperationLane> pair in _characterLanes)
                {
                    if (pair.Value.HasOwnedLease
                        && pair.Value.HasPendingCheckpoint
                        && !pair.Value.BlockingResult.HasValue)
                    {
                        characterIDs.Add(pair.Key);
                    }
                }

                characterIDs.Sort();
                return characterIDs;
            }
        }

        private bool TryGetCheckpointWork(
            Guid characterID,
            CharacterOperationLane lane,
            out CheckpointWork work)
        {
            lock (_stateLock)
            {
                if (!lane.HasOwnedLease
                    || !lane.AccountID.HasValue
                    || !lane.CharacterPersistenceLeaseID.HasValue
                    || !lane.Revision.HasValue
                    || !lane.HasPendingCheckpoint
                    || lane.BlockingResult.HasValue)
                {
                    work = default;
                    return false;
                }

                work = new CheckpointWork(
                    characterID,
                    lane,
                    lane.PendingCheckpoint,
                    lane.PendingCheckpointGeneration,
                    lane.AccountID.Value,
                    lane.CharacterPersistenceLeaseID.Value,
                    lane.Revision.Value);
                return true;
            }
        }

        private MasterPersistenceProtocol.CharacterCheckpointBatchRequest BuildCheckpointRequest(
            List<CheckpointWork> workItems)
        {
            MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CheckpointEntry[] entries =
                new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CheckpointEntry[workItems.Count];
            for (int index = 0; index < workItems.Count; index++)
            {
                CheckpointWork work = workItems[index];
                entries[index] = new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CheckpointEntry
                {
                    AccountID = work.AccountID,
                    CharacterID = work.CharacterID,
                    CharacterPersistenceLeaseID = work.CharacterPersistenceLeaseID,
                    ExpectedRevision = work.Revision,
                    WorldState = MapWorldState(work.Checkpoint.WorldState),
                    Resources = MapResources(work.Checkpoint.Resources)
                };
            }

            return new MasterPersistenceProtocol.CharacterCheckpointBatchRequest
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                Checkpoints = entries
            };
        }

        private void SettleCheckpointBatch(
            List<CheckpointWork> workItems,
            MasterPersistenceProtocol.CharacterCheckpointBatchResponse response)
        {
            for (int index = 0; index < workItems.Count; index++)
            {
                CheckpointWork work = workItems[index];
                MasterPersistenceProtocol.CharacterCheckpointBatchResponse.CheckpointResult result =
                    response.Checkpoints[index];
                if (result.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    if (!result.Revision.HasValue
                        || work.Revision == long.MaxValue
                        || result.Revision.Value != work.Revision + 1)
                    {
                        throw new InvalidOperationException("The accepted checkpoint revision was not sequential.");
                    }

                    AdvanceLeaseRevision(work.Lane, result.Revision.Value);
                    ClearPendingCheckpointIfGeneration(work.Lane, work.Generation);
                }
                else if (result.Result != MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable)
                {
                    SetBlockingResult(work.Lane, result.Result);
                }
            }
        }

        private static bool IsWorldBlockingResult(MasterPersistenceProtocol.PersistenceResult result)
        {
            return result == MasterPersistenceProtocol.PersistenceResult.Unauthorized
                || result == MasterPersistenceProtocol.PersistenceResult.WorldNotFound
                || result == MasterPersistenceProtocol.PersistenceResult.WorldNotActive
                || result == MasterPersistenceProtocol.PersistenceResult.StaleWorldServerRun;
        }

        private static bool IsTransportSafeCheckpoint(WorldPersistenceCheckpoint checkpoint)
        {
            WorldPersistenceCheckpoint.Resource[] resources = checkpoint.Resources;
            if (checkpoint.WorldState == null && resources.Length == 0)
                return false;

            if (checkpoint.WorldState != null
                && (string.IsNullOrWhiteSpace(checkpoint.WorldState.MapID)
                    || !IsFinite(checkpoint.WorldState.PositionX)
                    || !IsFinite(checkpoint.WorldState.PositionY)
                    || !IsFinite(checkpoint.WorldState.PositionZ)
                    || checkpoint.WorldState.SavedAtUtc.Offset != TimeSpan.Zero))
            {
                return false;
            }

            HashSet<string> resourceIDs = new(StringComparer.Ordinal);
            foreach (WorldPersistenceCheckpoint.Resource resource in resources)
            {
                if (resource == null
                    || string.IsNullOrWhiteSpace(resource.ResourceDefinitionID)
                    || !IsFinite(resource.CurrentValue)
                    || resource.LastAuthoritativeUpdateUtc.Offset != TimeSpan.Zero
                    || !resourceIDs.Add(resource.ResourceDefinitionID))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static MasterPersistenceProtocol.CharacterCheckpointBatchRequest.WorldCheckpoint MapWorldState(
            WorldPersistenceCheckpoint.WorldStateSnapshot worldState)
        {
            if (worldState == null)
                return null;

            return new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.WorldCheckpoint
            {
                MapID = worldState.MapID,
                InstanceID = worldState.InstanceID,
                PositionX = worldState.PositionX,
                PositionY = worldState.PositionY,
                PositionZ = worldState.PositionZ,
                SafeReturnLocationID = worldState.SafeReturnLocationID,
                SavedAtUtc = worldState.SavedAtUtc
            };
        }

        private static MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ResourceCheckpoint[] MapResources(
            WorldPersistenceCheckpoint.Resource[] resources)
        {
            MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ResourceCheckpoint[] mapped =
                new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ResourceCheckpoint[resources.Length];
            for (int index = 0; index < resources.Length; index++)
            {
                WorldPersistenceCheckpoint.Resource resource = resources[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ResourceCheckpoint
                {
                    ResourceDefinitionID = resource.ResourceDefinitionID,
                    CurrentValue = resource.CurrentValue,
                    LastAuthoritativeUpdateUtc = resource.LastAuthoritativeUpdateUtc
                };
            }

            return mapped;
        }

        private void StorePendingCheckpoint(
            CharacterOperationLane lane,
            WorldPersistenceCheckpoint checkpoint)
        {
            long generation = checked(lane.PendingCheckpointGeneration + 1);
            lane.PendingCheckpoint = checkpoint;
            lane.PendingCheckpointGeneration = generation;
        }

        private void ClearPendingCheckpointIfGeneration(
            CharacterOperationLane lane,
            long generation)
        {
            lock (_stateLock)
            {
                if (lane.PendingCheckpointGeneration == generation)
                    lane.PendingCheckpoint = null;
            }
        }

        private readonly struct CheckpointWork
        {
            internal CheckpointWork(
                Guid characterID,
                CharacterOperationLane lane,
                WorldPersistenceCheckpoint checkpoint,
                long generation,
                Guid accountID,
                Guid characterPersistenceLeaseID,
                long revision)
            {
                CharacterID = characterID;
                Lane = lane;
                Checkpoint = checkpoint;
                Generation = generation;
                AccountID = accountID;
                CharacterPersistenceLeaseID = characterPersistenceLeaseID;
                Revision = revision;
            }

            internal Guid CharacterID { get; }
            internal CharacterOperationLane Lane { get; }
            internal WorldPersistenceCheckpoint Checkpoint { get; }
            internal long Generation { get; }
            internal Guid AccountID { get; }
            internal Guid CharacterPersistenceLeaseID { get; }
            internal long Revision { get; }
        }
    }
}
