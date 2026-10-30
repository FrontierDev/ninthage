using Debug = Game.Shared.Utility.FormattedDebug;
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
        internal Task FlushPendingCheckpointsAsync(CancellationToken cancellationToken)
        {
            return FlushPendingCheckpointsCoreAsync(cancellationToken, false);
        }

        /// <summary>Flushes dirty checkpoints during graceful shutdown and logs cleanup failures.</summary>
        internal Task FlushPendingCheckpointsForShutdownAsync(CancellationToken cancellationToken)
        {
            return FlushPendingCheckpointsCoreAsync(cancellationToken, true);
        }

        /// <summary>Gets the pending and blocking state for an owned character checkpoint lane.</summary>
        internal bool TryGetPendingCheckpointState(
            Guid characterID,
            out bool hasPendingCheckpoint,
            out MasterPersistenceProtocol.PersistenceResult? blockingResult)
        {
            hasPendingCheckpoint = false;
            blockingResult = null;
            if (characterID == Guid.Empty)
                return false;

            lock (_stateLock)
            {
                if (!_characterLanes.TryGetValue(characterID, out CharacterOperationLane lane)
                    || !lane.HasOwnedLease)
                {
                    return false;
                }

                hasPendingCheckpoint = lane.HasPendingCheckpoint;
                blockingResult = lane.BlockingResult;
                return true;
            }
        }

        /// <summary>Gets the owned lease revision and checkpoint settlement state for one character.</summary>
        internal bool TryGetCharacterCheckpointSettlement(
            Guid characterID,
            out Guid accountID,
            out long revision,
            out bool hasPendingCheckpoint,
            out MasterPersistenceProtocol.PersistenceResult? blockingResult)
        {
            accountID = Guid.Empty;
            revision = 0;
            hasPendingCheckpoint = false;
            blockingResult = null;
            if (characterID == Guid.Empty)
                return false;

            lock (_stateLock)
            {
                if (!_characterLanes.TryGetValue(characterID, out CharacterOperationLane lane)
                    || !lane.HasOwnedLease
                    || !lane.AccountID.HasValue
                    || !lane.Revision.HasValue)
                {
                    return false;
                }

                accountID = lane.AccountID.Value;
                revision = lane.Revision.Value;
                hasPendingCheckpoint = lane.HasPendingCheckpoint;
                blockingResult = lane.BlockingResult;
                return true;
            }
        }

        /// <summary>Gets whether any owned character lane has an unsettled checkpoint or blocking result.</summary>
        internal bool HasUnsettledCheckpoints()
        {
            lock (_stateLock)
            {
                foreach (CharacterOperationLane lane in _characterLanes.Values)
                {
                    if (lane.HasOwnedLease
                        && (lane.HasPendingCheckpoint || lane.BlockingResult.HasValue))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Gets owned character IDs whose checkpoint state remains unsettled.</summary>
        internal List<Guid> GetUnsettledCheckpointCharacterIDs()
        {
            lock (_stateLock)
            {
                List<Guid> characterIDs = new();
                foreach (KeyValuePair<Guid, CharacterOperationLane> pair in _characterLanes)
                {
                    if (pair.Value.HasOwnedLease
                        && (pair.Value.HasPendingCheckpoint || pair.Value.BlockingResult.HasValue))
                    {
                        characterIDs.Add(pair.Key);
                    }
                }

                characterIDs.Sort();
                return characterIDs;
            }
        }

        /// <summary>Blocks an owned character lane after a final checkpoint could not be captured or queued.</summary>
        internal bool MarkCheckpointFailure(
            Guid characterID,
            MasterPersistenceProtocol.PersistenceResult result)
        {
            if (characterID == Guid.Empty
                || result == MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                return false;
            }

            lock (_stateLock)
            {
                if (!_characterLanes.TryGetValue(characterID, out CharacterOperationLane lane)
                    || !lane.HasOwnedLease)
                {
                    return false;
                }

                lane.BlockingResult = result;
                return true;
            }
        }

        private async Task FlushPendingCheckpointsCoreAsync(
            CancellationToken cancellationToken,
            bool logCleanupFailures)
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
                        cancellationToken,
                        logCleanupFailures).ConfigureAwait(false);
                    if (stopCycle)
                        break;
                }
            }
            finally
            {
                CompleteOperation();
            }
        }

        private async Task<bool> FlushCheckpointBatchAsync(
            List<Guid> characterIDs,
            int startIndex,
            int count,
            CancellationToken cancellationToken,
            bool logCleanupFailures)
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
                catch (HttpRequestException exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            $"Final checkpoint batch could not be confirmed during shutdown: {exception.Message}");
                    }
                    return true;
                }
                catch (TimeoutException exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            $"Final checkpoint batch could not be confirmed during shutdown: {exception.Message}");
                    }
                    return true;
                }
                catch (OperationCanceledException exception)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            $"Final checkpoint batch could not be confirmed during shutdown: {exception.Message}");
                    }
                    return true;
                }

                if (response.Result == MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable)
                {
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            "Final checkpoint batch was rejected during shutdown with result "
                            + response.Result
                            + ".");
                    }
                    return true;
                }
                if (response.Result == MasterPersistenceProtocol.PersistenceResult.InvalidRequest)
                {
                    throw new InvalidOperationException(
                        "The coordinator-built checkpoint batch was rejected as invalid.");
                }
                if (IsWorldBlockingResult(response.Result))
                {
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            "Final checkpoint batch was rejected during shutdown with result "
                            + response.Result
                            + ".");
                    }
                    foreach (CheckpointWork work in workItems)
                        SetBlockingResult(work.Lane, response.Result);
                    return false;
                }
                if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    throw new InvalidOperationException("The Master checkpoint response had an unexpected result.");
                }

                SettleCheckpointBatch(workItems, response, logCleanupFailures);
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
                    Resources = MapResources(work.Checkpoint.Resources),
                    IsDead = work.Checkpoint.IsDead,
                    Cooldowns = MapCooldowns(work.Checkpoint.Cooldowns),
                    PersistentAuras = MapPersistentAuras(work.Checkpoint.PersistentAuras),
                    Items = MapItems(work.Checkpoint.Items)
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
            MasterPersistenceProtocol.CharacterCheckpointBatchResponse response,
            bool logCleanupFailures)
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
                else if (result.Result == MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable)
                {
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            $"final checkpoint for character {work.CharacterID} was rejected with result {result.Result}.");
                    }
                }
                else
                {
                    SetBlockingResult(work.Lane, result.Result);
                    if (logCleanupFailures)
                    {
                        Debug.Warning(
                            $"final checkpoint for character {work.CharacterID} was rejected with result {result.Result}.");
                    }
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
            WorldPersistenceCheckpoint.Cooldown[] cooldowns = checkpoint.Cooldowns;
            WorldPersistenceCheckpoint.PersistentAura[] persistentAuras = checkpoint.PersistentAuras;
            WorldPersistenceCheckpoint.Item[] items = checkpoint.Items;
            if (checkpoint.WorldState == null
                && resources.Length == 0
                && cooldowns == null
                && persistentAuras == null
                && items == null
                && !checkpoint.IsDead.HasValue)
                return false;

            if (checkpoint.WorldState != null
                && (string.IsNullOrWhiteSpace(checkpoint.WorldState.MapID)
                    || checkpoint.WorldState.InstanceID == Guid.Empty
                    || !IsFinite(checkpoint.WorldState.PositionX)
                    || !IsFinite(checkpoint.WorldState.PositionY)
                    || !IsFinite(checkpoint.WorldState.PositionZ)
                    || !IsFinite(checkpoint.WorldState.FacingX)
                    || !IsFinite(checkpoint.WorldState.FacingY)
                    || !IsFinite(checkpoint.WorldState.FacingZ)
                    || !IsFinite(checkpoint.WorldState.FacingW)
                    || Math.Abs(
                        (double)checkpoint.WorldState.FacingX * checkpoint.WorldState.FacingX
                        + (double)checkpoint.WorldState.FacingY * checkpoint.WorldState.FacingY
                        + (double)checkpoint.WorldState.FacingZ * checkpoint.WorldState.FacingZ
                        + (double)checkpoint.WorldState.FacingW * checkpoint.WorldState.FacingW
                        - 1d) > 0.001d
                    || (checkpoint.WorldState.SafeReturnLocationID != null
                        && string.IsNullOrWhiteSpace(checkpoint.WorldState.SafeReturnLocationID))
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

            if (cooldowns != null)
            {
                HashSet<(string CooldownDefinitionID, int ChargeIndex)> cooldownKeys = new();
                foreach (WorldPersistenceCheckpoint.Cooldown cooldown in cooldowns)
                {
                    if (cooldown == null
                        || string.IsNullOrWhiteSpace(cooldown.CooldownDefinitionID)
                        || cooldown.ChargeIndex < 0
                        || cooldown.ExpiresAtUtc.Offset != TimeSpan.Zero
                        || !cooldownKeys.Add((cooldown.CooldownDefinitionID, cooldown.ChargeIndex)))
                    {
                        return false;
                    }
                }
            }

            if (persistentAuras != null)
            {
                HashSet<Guid> persistentAuraIDs = new();
                foreach (WorldPersistenceCheckpoint.PersistentAura persistentAura in persistentAuras)
                {
                    if (persistentAura == null
                        || persistentAura.PersistentAuraID == Guid.Empty
                        || !persistentAuraIDs.Add(persistentAura.PersistentAuraID)
                        || string.IsNullOrWhiteSpace(persistentAura.AuraDefinitionID)
                        || (persistentAura.SourceActorID.HasValue
                            && persistentAura.SourceActorID.Value == Guid.Empty)
                        || persistentAura.Stacks <= 0
                        || (persistentAura.ExpiresAtUtc.HasValue
                            && persistentAura.ExpiresAtUtc.Value.Offset != TimeSpan.Zero)
                        || (persistentAura.NextTickAtUtc.HasValue
                            && persistentAura.NextTickAtUtc.Value.Offset != TimeSpan.Zero)
                        || (persistentAura.TickIntervalSeconds.HasValue
                            && (!IsFinite(persistentAura.TickIntervalSeconds.Value)
                                || persistentAura.TickIntervalSeconds.Value <= 0f))
                        || persistentAura.NextTickAtUtc.HasValue
                            != persistentAura.TickIntervalSeconds.HasValue)
                    {
                        return false;
                    }
                }
            }

            if (items != null)
            {
                HashSet<Guid> itemInstanceIDs = new();
                HashSet<(MasterPersistenceProtocol.CharacterItemLocationKind LocationKind, int SlotIndex)> itemLocationKeys = new();
                foreach (WorldPersistenceCheckpoint.Item item in items)
                {
                    if (item == null
                        || item.ItemInstanceID == Guid.Empty
                        || string.IsNullOrWhiteSpace(item.ItemDefinitionID)
                        || item.StackSize <= 0
                        || (item.Durability.HasValue && item.Durability.Value < 0)
                        || !Enum.IsDefined(typeof(MasterPersistenceProtocol.CharacterItemLocationKind), item.LocationKind)
                        || item.SlotIndex < 0
                        || !itemInstanceIDs.Add(item.ItemInstanceID)
                        || !itemLocationKeys.Add((item.LocationKind, item.SlotIndex)))
                    {
                        return false;
                    }

                    string[] modificationIDs = item.ModificationIDs;
                    if (modificationIDs == null)
                        return false;
                    foreach (string modificationID in modificationIDs)
                    {
                        if (string.IsNullOrWhiteSpace(modificationID))
                            return false;
                    }
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
                FacingX = worldState.FacingX,
                FacingY = worldState.FacingY,
                FacingZ = worldState.FacingZ,
                FacingW = worldState.FacingW,
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

        private static MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CooldownCheckpoint[] MapCooldowns(
            WorldPersistenceCheckpoint.Cooldown[] cooldowns)
        {
            if (cooldowns == null)
                return null;

            MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CooldownCheckpoint[] mapped =
                new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CooldownCheckpoint[cooldowns.Length];
            for (int index = 0; index < cooldowns.Length; index++)
            {
                WorldPersistenceCheckpoint.Cooldown cooldown = cooldowns[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CooldownCheckpoint
                {
                    CooldownDefinitionID = cooldown.CooldownDefinitionID,
                    ChargeIndex = cooldown.ChargeIndex,
                    ExpiresAtUtc = cooldown.ExpiresAtUtc
                };
            }

            return mapped;
        }

        private static MasterPersistenceProtocol.CharacterCheckpointBatchRequest.PersistentAuraCheckpoint[] MapPersistentAuras(
            WorldPersistenceCheckpoint.PersistentAura[] persistentAuras)
        {
            if (persistentAuras == null)
                return null;

            MasterPersistenceProtocol.CharacterCheckpointBatchRequest.PersistentAuraCheckpoint[] mapped =
                new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.PersistentAuraCheckpoint[persistentAuras.Length];
            for (int index = 0; index < persistentAuras.Length; index++)
            {
                WorldPersistenceCheckpoint.PersistentAura persistentAura = persistentAuras[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.PersistentAuraCheckpoint
                {
                    PersistentAuraID = persistentAura.PersistentAuraID,
                    AuraDefinitionID = persistentAura.AuraDefinitionID,
                    SourceActorID = persistentAura.SourceActorID,
                    Stacks = persistentAura.Stacks,
                    ExpiresAtUtc = persistentAura.ExpiresAtUtc,
                    NextTickAtUtc = persistentAura.NextTickAtUtc,
                    TickIntervalSeconds = persistentAura.TickIntervalSeconds
                };
            }

            return mapped;
        }

        private static MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ItemCheckpoint[] MapItems(
            WorldPersistenceCheckpoint.Item[] items)
        {
            if (items == null)
                return null;

            MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ItemCheckpoint[] mapped =
                new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ItemCheckpoint[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                WorldPersistenceCheckpoint.Item item = items[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCheckpointBatchRequest.ItemCheckpoint
                {
                    ItemInstanceID = item.ItemInstanceID,
                    ItemDefinitionID = item.ItemDefinitionID,
                    StackSize = item.StackSize,
                    Durability = item.Durability,
                    ModificationIDs = item.ModificationIDs,
                    LocationKind = item.LocationKind,
                    SlotIndex = item.SlotIndex
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
