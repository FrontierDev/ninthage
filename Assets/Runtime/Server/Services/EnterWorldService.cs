using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Services
{
    /// <summary>Acquires Master character persistence and enters the player into the world.</summary>
    public sealed class EnterWorldService
    {
        private const string STAT_HEALTH = "health";

        /// <summary>Executes one Master-backed world-entry request.</summary>
        internal IEnumerator Execute(PlayerID playerID, string characterIDValue)
        {
            if (!Guid.TryParse(characterIDValue, out Guid characterID) || characterID == Guid.Empty)
            {
                Debug.Error($"Player {playerID} provided an invalid character GUID.");
                yield break;
            }

            if (!ServerConnectionManager.Instance.TryGetAccountContext(playerID, out ConnectedAccountContext accountContext))
            {
                Debug.Error($"No authenticated account context exists for player {playerID}.");
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not access persistence coordinator for player {playerID}: {exception.Message}");
                yield break;
            }

            if (coordinator == null)
            {
                Debug.Error($"Persistence coordinator is unavailable for player {playerID}.");
                yield break;
            }

            Task<MasterPersistenceProtocol.CharacterAcquireResponse> acquireTask;
            try
            {
                acquireTask = coordinator.AcquireCharacterAsync(
                    accountContext.AccountID,
                    characterID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not start character acquisition for player {playerID}: {exception.Message}");
                yield break;
            }

            while (!acquireTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.CharacterAcquireResponse acquireResponse;
            try
            {
                acquireResponse = acquireTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Character acquisition failed for player {playerID}: {exception.Message}");
                yield break;
            }

            if (acquireResponse == null
                || acquireResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error($"Character acquisition for player {playerID} returned {acquireResponse?.Result}; world entry failed closed.");
                yield break;
            }

            if (acquireResponse.Character == null)
            {
                Debug.Error($"Character acquisition for player {playerID} returned no character state.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            if (acquireResponse.Character.CharacterID != characterID
                || acquireResponse.Character.AccountID != accountContext.AccountID)
            {
                Debug.Error(
                    $"Character acquisition for player {playerID} returned state for character "
                    + $"{acquireResponse.Character.CharacterID} or account {acquireResponse.Character.AccountID}; "
                    + $"requested character {characterID} for account {accountContext.AccountID}.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            CharacterData characterData = null;
            Exception projectionException = null;
            try
            {
                characterData = CharacterPersistenceProjection.ToCharacterData(acquireResponse.Character);
                CharacterPersistenceDefinitionValidator.ValidateForWorldEntry(
                    characterData,
                    acquireResponse.Character);
                ValidatePersistedResources(acquireResponse.Character);
            }
            catch (Exception exception)
            {
                projectionException = exception;
            }

            if (projectionException != null)
            {
                Debug.Error($"Acquired character {characterID} failed definition or state validation: {projectionException.Message}");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            if (!ServerConnectionManager.Instance.IsPlayerConnected(playerID)
                || !ServerConnectionManager.Instance.TryGetAccountContext(
                    playerID,
                    out ConnectedAccountContext currentAccountContext)
                || currentAccountContext.AccountID != accountContext.AccountID
                || !StringComparer.Ordinal.Equals(currentAccountContext.Username, accountContext.Username))
            {
                Debug.Warning(
                    $"Player {playerID} disconnected or changed account while acquiring character {characterID}.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            ServerWorldManager worldManager = ServerWorldManager.Instance;
            ActorSpatialContext currentPublicContext = worldManager != null
                ? worldManager.CurrentRuntimeContext
                : default;
            if (worldManager == null)
            {
                Debug.Error($"The server world manager is unavailable for player {playerID}.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }
            if (currentAccountContext.HomeWorldID != currentPublicContext.WorldID)
            {
                Debug.Error(
                    $"Account home WorldID {currentAccountContext.HomeWorldID} does not match current WorldID "
                    + $"{currentPublicContext.WorldID} for player {playerID}.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            IReadOnlyList<CharacterWorldEntryLocationResolver.RecoveryCandidate> candidates =
                CharacterWorldEntryLocationResolver.BuildCandidates(
                    acquireResponse.Character,
                    currentPublicContext,
                    worldManager);
            CharacterWorldEntryLocationResolver.RecoveryCandidate selectedCandidate = default;
            string selectedSafeReturnLocationID = acquireResponse.Character.WorldState.SafeReturnLocationID;
            bool candidateSelected = false;
            for (int index = 0; index < candidates.Count; index++)
            {
                CharacterWorldEntryLocationResolver.RecoveryCandidate candidate = candidates[index];
                if (!IsAdmissionStillValid(
                    playerID,
                    characterID,
                    accountContext,
                    coordinator,
                    worldManager,
                    currentPublicContext,
                    out string admissionFailureReason))
                {
                    Debug.Error(
                        $"Character admission for {characterID} became invalid before recovery tier "
                        + $"{candidate.Tier}: {admissionFailureReason}");
                    yield return ReleaseCharacter(coordinator, characterID);
                    yield break;
                }

                ServerPhysicsManager physicsManager = ServerPhysicsManager.Instance;
                string chunkValidationFailureReason = null;
                UnityEngine.Vector2Int arrivalChunk = default;
                bool arrivalChunkValid = physicsManager != null
                    && physicsManager.TryGetCharacterArrivalChunk(
                        candidate.EntryLocation.Position,
                        out arrivalChunk,
                        out chunkValidationFailureReason);
                if (!arrivalChunkValid)
                {
                    Debug.Warning(
                        $"Character {characterID} skipped recovery tier {candidate.Tier}: "
                        + (chunkValidationFailureReason ?? "server physics manager is unavailable"));
                    continue;
                }

                bool chunkLoadCompleted = false;
                bool chunkLoadSucceeded = false;
                string chunkLoadFailureReason = null;
                try
                {
                    worldManager.EnsureChunkLoaded(
                        candidate.EntryLocation.Context,
                        arrivalChunk,
                        (succeeded, _) =>
                        {
                            chunkLoadSucceeded = succeeded;
                            chunkLoadCompleted = true;
                            if (!succeeded)
                                chunkLoadFailureReason = "the destination chunk did not load";
                        });
                }
                catch (Exception exception)
                {
                    chunkLoadCompleted = true;
                    chunkLoadFailureReason = exception.Message;
                }

                while (!chunkLoadCompleted)
                    yield return null;

                if (!chunkLoadSucceeded)
                {
                    Debug.Warning(
                        $"Character {characterID} skipped recovery tier {candidate.Tier}: "
                        + chunkLoadFailureReason);
                    continue;
                }

                if (!IsAdmissionStillValid(
                    playerID,
                    characterID,
                    accountContext,
                    coordinator,
                    worldManager,
                    currentPublicContext,
                    out admissionFailureReason))
                {
                    Debug.Error(
                        $"Character admission for {characterID} became invalid after recovery tier "
                        + $"{candidate.Tier} streamed: {admissionFailureReason}");
                    yield return ReleaseCharacter(coordinator, characterID);
                    yield break;
                }

                string arrivalFailureReason = null;
                bool arrivalSafe = physicsManager != null
                    && physicsManager.IsCharacterArrivalSafe(
                        candidate.EntryLocation.Context,
                        candidate.EntryLocation.Position,
                        candidate.Tier >= LocationRecoveryTier.SafeReturnAnchor,
                        out arrivalFailureReason);
                if (!arrivalSafe)
                {
                    Debug.Warning(
                        $"Character {characterID} skipped recovery tier {candidate.Tier}: "
                        + (arrivalFailureReason ?? "server physics manager is unavailable"));
                    continue;
                }

                if (candidate.RequiresDurableRelocation)
                {
                    selectedSafeReturnLocationID = ResolveRecoverySafeReturnLocationID(
                        acquireResponse.Character,
                        candidate,
                        currentPublicContext);
                    bool durableRelocationSucceeded = false;
                    string durableRelocationFailureReason = null;
                    yield return QueueDurableRecoveryCheckpoint(
                        coordinator,
                        characterID,
                        acquireResponse.Character,
                        accountContext.AccountID,
                        candidate,
                        selectedSafeReturnLocationID,
                        playerID,
                        worldManager,
                        currentPublicContext,
                        (succeeded, reason) =>
                        {
                            durableRelocationSucceeded = succeeded;
                            durableRelocationFailureReason = reason;
                        });
                    if (!durableRelocationSucceeded)
                    {
                        Debug.Warning(
                            $"Character {characterID} skipped recovery tier {candidate.Tier}: "
                            + durableRelocationFailureReason);
                        yield return ReleaseCharacter(coordinator, characterID);
                        yield break;
                    }
                }

                selectedCandidate = candidate;
                candidateSelected = true;
                break;
            }

            if (!candidateSelected)
            {
                Debug.Error(
                    $"Acquired character {characterID} has no safe, streamable recovery location; entry denied.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            Exception sessionException = null;
            try
            {
                PlayerSessionData session = new(
                    playerID,
                    accountContext.GameplaySessionID,
                    accountContext.Username,
                    accountContext.AccountID,
                    characterID,
                    characterData,
                    acquireResponse.Character,
                    selectedCandidate.EntryLocation);

                ServerConnectionManager.Instance.SetSessionData(
                    playerID,
                    session);
            }
            catch (Exception exception)
            {
                sessionException = exception;
            }

            if (sessionException != null)
            {
                Debug.Error($"Could not establish world-entry session for player {playerID}: {sessionException.Message}");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            yield return SpawnAndReleaseOnFailure(coordinator, playerID, characterID);
        }

        private static bool IsAdmissionStillValid(
            PlayerID playerID,
            Guid characterID,
            ConnectedAccountContext expectedAccountContext,
            WorldPersistenceCoordinator coordinator,
            ServerWorldManager worldManager,
            ActorSpatialContext expectedPublicContext,
            out string failureReason)
        {
            failureReason = null;
            if (!ServerConnectionManager.Instance.IsPlayerConnected(playerID))
                return FailAdmission("the player is no longer connected", out failureReason);
            if (!ServerConnectionManager.Instance.TryGetAccountContext(
                playerID,
                out ConnectedAccountContext currentAccountContext))
            {
                return FailAdmission("the account context is no longer present", out failureReason);
            }
            if (currentAccountContext.AccountID != expectedAccountContext.AccountID
                || !StringComparer.Ordinal.Equals(
                    currentAccountContext.Username,
                    expectedAccountContext.Username))
            {
                return FailAdmission("the authenticated account changed", out failureReason);
            }
            if (worldManager == null
                || worldManager.CurrentRuntimeContext != expectedPublicContext
                || !expectedPublicContext.IsValid
                || expectedPublicContext.InstanceID != Guid.Empty
                || coordinator == null
                || coordinator.WorldID != expectedPublicContext.WorldID)
            {
                return FailAdmission("the authoritative world context changed", out failureReason);
            }
            if (!coordinator.TryGetCharacterCheckpointSettlement(
                characterID,
                out Guid leasedAccountID,
                out _,
                out bool hasPendingCheckpoint,
                out MasterPersistenceProtocol.PersistenceResult? blockingResult))
            {
                return FailAdmission("the acquired character lease is no longer owned", out failureReason);
            }
            if (leasedAccountID != expectedAccountContext.AccountID)
                return FailAdmission("the acquired lease belongs to another account", out failureReason);
            if (blockingResult.HasValue)
            {
                return FailAdmission(
                    $"the acquired character lease is blocked with result {blockingResult.Value}",
                    out failureReason);
            }
            if (hasPendingCheckpoint)
                return FailAdmission("the acquired character already has a pending checkpoint", out failureReason);

            return true;
        }

        private static IEnumerator QueueDurableRecoveryCheckpoint(
            WorldPersistenceCoordinator coordinator,
            Guid characterID,
            MasterPersistenceProtocol.CharacterPersistenceState acquiredState,
            Guid accountID,
            CharacterWorldEntryLocationResolver.RecoveryCandidate candidate,
            string safeReturnLocationID,
            PlayerID playerID,
            ServerWorldManager worldManager,
            ActorSpatialContext expectedPublicContext,
            Action<bool, string> completed)
        {
            bool succeeded = false;
            string failureReason = null;
            Task flushTask = null;
            WorldPersistenceCheckpoint.WorldStateSnapshot queuedWorldState = null;
            try
            {
                bool preparationSucceeded = false;
                ConnectedAccountContext accountContext = null;
                long expectedRevision = 0;
                do
                {
                    try
                    {
                        if (!ServerConnectionManager.Instance.TryGetAccountContext(
                            playerID,
                            out accountContext))
                        {
                            failureReason = "the authenticated account context is unavailable";
                            break;
                        }

                        if (!IsAdmissionStillValid(
                            playerID,
                            characterID,
                            accountContext,
                            coordinator,
                            worldManager,
                            expectedPublicContext,
                            out failureReason))
                        {
                            break;
                        }

                        if (!coordinator.TryGetCharacterCheckpointSettlement(
                            characterID,
                            out Guid leasedAccountID,
                            out expectedRevision,
                            out bool hasPendingCheckpoint,
                            out MasterPersistenceProtocol.PersistenceResult? blockingResult))
                        {
                            failureReason = "the acquired character lease state is unavailable";
                            break;
                        }
                        if (leasedAccountID != accountID)
                        {
                            failureReason = "the acquired character lease account does not match admission";
                            break;
                        }
                        if (hasPendingCheckpoint || blockingResult.HasValue)
                        {
                            failureReason = "the acquired character lease is not clean for relocation";
                            break;
                        }
                        if (expectedRevision == long.MaxValue)
                        {
                            failureReason = "the acquired character revision cannot advance";
                            break;
                        }

                        ServerTimeAuthority timeAuthority = ServerInitialization.TimeAuthority;
                        if (timeAuthority == null)
                        {
                            failureReason = "the server time authority is unavailable";
                            break;
                        }

                        ServerUtcObservation observation = timeAuthority.ReadObservation();
                        WorldPersistenceCheckpoint.WorldStateSnapshot worldState =
                            new(
                                candidate.EntryLocation.Context.MapID,
                                null,
                                candidate.EntryLocation.Position.x,
                                candidate.EntryLocation.Position.y,
                                candidate.EntryLocation.Position.z,
                                candidate.EntryLocation.Facing.x,
                                candidate.EntryLocation.Facing.y,
                                candidate.EntryLocation.Facing.z,
                                candidate.EntryLocation.Facing.w,
                                safeReturnLocationID,
                                observation.UtcInstant);
                        WorldPersistenceCheckpoint checkpoint = new(
                            worldState,
                            Array.Empty<WorldPersistenceCheckpoint.Resource>(),
                            null);
                        MasterPersistenceProtocol.PersistenceResult queueResult =
                            coordinator.QueueCharacterCheckpoint(characterID, checkpoint);
                        if (queueResult != MasterPersistenceProtocol.PersistenceResult.Accepted)
                        {
                            failureReason = $"checkpoint queue returned {queueResult}";
                            break;
                        }

                        queuedWorldState = worldState;
                        flushTask = coordinator.FlushPendingCheckpointsAsync(CancellationToken.None);
                        preparationSucceeded = true;
                    }
                    catch (Exception exception)
                    {
                        failureReason = exception.Message;
                    }
                }
                while (false);

                if (!preparationSucceeded)
                    yield break;

                while (!flushTask.IsCompleted)
                    yield return null;

                bool settlementSucceeded = false;
                try
                {
                    flushTask.GetAwaiter().GetResult();

                    if (!IsAdmissionStillValid(
                        playerID,
                        characterID,
                        accountContext,
                        coordinator,
                        worldManager,
                        expectedPublicContext,
                        out failureReason))
                    {
                        settlementSucceeded = false;
                    }
                    else if (!coordinator.TryGetCharacterCheckpointSettlement(
                        characterID,
                        out Guid settledAccountID,
                        out long settledRevision,
                        out bool hasPendingAfterFlush,
                        out MasterPersistenceProtocol.PersistenceResult? blockingAfterFlush))
                    {
                        failureReason = "checkpoint settlement state is unavailable";
                    }
                    else if (settledAccountID != accountID
                        || settledRevision != expectedRevision + 1
                        || hasPendingAfterFlush
                        || blockingAfterFlush.HasValue)
                    {
                        failureReason = "checkpoint settlement was not an accepted single revision advance";
                    }
                    else
                    {
                        MasterPersistenceProtocol.CharacterWorldState acceptedWorldState =
                            acquiredState.WorldState;

                        acceptedWorldState.MapID = queuedWorldState.MapID;
                        acceptedWorldState.InstanceID = queuedWorldState.InstanceID;
                        acceptedWorldState.PositionX = queuedWorldState.PositionX;
                        acceptedWorldState.PositionY = queuedWorldState.PositionY;
                        acceptedWorldState.PositionZ = queuedWorldState.PositionZ;
                        acceptedWorldState.FacingX = queuedWorldState.FacingX;
                        acceptedWorldState.FacingY = queuedWorldState.FacingY;
                        acceptedWorldState.FacingZ = queuedWorldState.FacingZ;
                        acceptedWorldState.FacingW = queuedWorldState.FacingW;
                        acceptedWorldState.SafeReturnLocationID = queuedWorldState.SafeReturnLocationID;
                        acceptedWorldState.SavedAtUtc = queuedWorldState.SavedAtUtc;

                        acquiredState.Revision = settledRevision;
                        succeeded = true;
                        settlementSucceeded = true;
                    }
                }
                catch (Exception exception)
                {
                    failureReason = exception.Message;
                }

                if (!settlementSucceeded)
                    yield break;
            }
            finally
            {
                completed?.Invoke(succeeded, failureReason);
            }
        }

        private static string ResolveRecoverySafeReturnLocationID(
            MasterPersistenceProtocol.CharacterPersistenceState state,
            CharacterWorldEntryLocationResolver.RecoveryCandidate candidate,
            ActorSpatialContext currentPublicContext)
        {
            WorldLocationDefinitionLibrary locationLibrary = WorldLocationDefinitionLibrary.Instance;
            if (locationLibrary == null || !locationLibrary.ValidateLibrary())
                return null;

            WorldLocationDefinition savedSafeReturnLocation = null;
            if (!string.IsNullOrWhiteSpace(state.WorldState.SafeReturnLocationID))
            {
                foreach (WorldLocationDefinition definition in locationLibrary.GetAllDefinitions())
                {
                    if (definition != null
                        && StringComparer.Ordinal.Equals(
                            definition.DefinitionID,
                            state.WorldState.SafeReturnLocationID))
                    {
                        savedSafeReturnLocation = definition;
                        break;
                    }
                }
            }

            if (CharacterWorldEntryLocationResolver.TryResolveAuthoredLocation(
                savedSafeReturnLocation,
                WorldLocationPurpose.SafeReturn,
                currentPublicContext,
                out _,
                out _))
            {
                return state.WorldState.SafeReturnLocationID;
            }

            WorldLocationDefinition candidateLocation = null;
            if (!string.IsNullOrWhiteSpace(candidate.LocationDefinitionID))
            {
                foreach (WorldLocationDefinition definition in locationLibrary.GetAllDefinitions())
                {
                    if (definition != null
                        && StringComparer.Ordinal.Equals(
                            definition.DefinitionID,
                            candidate.LocationDefinitionID))
                    {
                        candidateLocation = definition;
                        break;
                    }
                }
            }

            if (candidateLocation != null
                && CharacterWorldEntryLocationResolver.TryResolveAuthoredLocation(
                    candidateLocation,
                    WorldLocationPurpose.SafeReturn,
                    currentPublicContext,
                    out _,
                    out _))
            {
                return candidate.LocationDefinitionID;
            }

            return null;
        }

        private static bool FailAdmission(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }

        private static void ValidatePersistedResources(
            MasterPersistenceProtocol.CharacterPersistenceState state)
        {
            if (state.Resources == null)
                throw new InvalidOperationException("Character persistence state has no resources array.");

            ActorStatDefinitionLibrary library = ActorStatDefinitionLibrary.Instance;
            ActorStatDefinition healthDefinition = library?.GetDefinition(STAT_HEALTH);
            if (healthDefinition == null || healthDefinition.Category != ActorStatCategory.Resource)
            {
                throw new InvalidOperationException(
                    $"Character '{state.CharacterID}' has no valid '{STAT_HEALTH}' resource definition.");
            }

            HashSet<string> resourceIDs = new(StringComparer.Ordinal);
            bool hasHealth = false;
            foreach (MasterPersistenceProtocol.CharacterResourceState resource in state.Resources)
            {
                if (resource == null
                    || string.IsNullOrWhiteSpace(resource.ResourceDefinitionID)
                    || !resourceIDs.Add(resource.ResourceDefinitionID))
                {
                    throw new InvalidOperationException(
                        $"Character '{state.CharacterID}' contains an invalid or duplicate resource ID.");
                }
                if (float.IsNaN(resource.CurrentValue)
                    || float.IsInfinity(resource.CurrentValue)
                    || resource.CurrentValue < 0f)
                {
                    throw new InvalidOperationException(
                        $"Character '{state.CharacterID}' contains invalid current value "
                        + $"{resource.CurrentValue} for resource '{resource.ResourceDefinitionID}'.");
                }

                ActorStatDefinition definition = library?.GetDefinition(resource.ResourceDefinitionID);
                if (definition == null || definition.Category != ActorStatCategory.Resource)
                {
                    throw new InvalidOperationException(
                        $"Character '{state.CharacterID}' references unknown or non-resource definition "
                        + $"'{resource.ResourceDefinitionID}'.");
                }

                if (StringComparer.Ordinal.Equals(resource.ResourceDefinitionID, STAT_HEALTH))
                    hasHealth = true;
            }

            if (!hasHealth
                && (state.Resources.Length > 0
                    || state.Revision != 1
                    || state.IsDead))
            {
                throw new InvalidOperationException(
                    $"Character '{state.CharacterID}' has no persisted '{STAT_HEALTH}' resource.");
            }
        }

        private IEnumerator SpawnAndReleaseOnFailure(
            WorldPersistenceCoordinator coordinator,
            PlayerID playerID,
            Guid characterID)
        {
            bool spawnSucceeded = false;
            IEnumerator spawnCoroutine = null;
            Exception spawnStartException = null;
            try
            {
                spawnCoroutine = ServerSpawnManager.Instance.SpawnPlayerCoroutine(
                    playerID,
                    succeeded => spawnSucceeded = succeeded);
            }
            catch (Exception exception)
            {
                spawnStartException = exception;
            }

            if (spawnStartException != null)
            {
                Debug.Error($"Could not start player spawn for {playerID}: {spawnStartException.Message}");
                ServerConnectionManager.Instance.ClearSessionData(playerID);
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            while (true)
            {
                bool hasNext;
                object yieldedValue = null;
                try
                {
                    hasNext = spawnCoroutine.MoveNext();
                    if (hasNext)
                        yieldedValue = spawnCoroutine.Current;
                }
                catch (Exception exception)
                {
                    Debug.Error($"Player spawn failed for {playerID}: {exception.Message}");
                    break;
                }

                if (!hasNext)
                    break;

                yield return yieldedValue;
            }

            if (spawnSucceeded)
                yield break;

            ServerConnectionManager.Instance.ClearSessionData(playerID);
            yield return ReleaseCharacter(coordinator, characterID);
            Debug.Error($"Player spawn failed for {playerID}; character persistence lease {characterID} was released.");
        }

        private static IEnumerator ReleaseCharacter(
            WorldPersistenceCoordinator coordinator,
            Guid characterID)
        {
            Task<MasterPersistenceProtocol.PersistenceResponse> releaseTask;
            try
            {
                releaseTask = coordinator.ReleaseCharacterAsync(characterID, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Warning($"Could not start persistence release for character {characterID}: {exception.Message}");
                yield break;
            }

            while (!releaseTask.IsCompleted)
                yield return null;

            try
            {
                MasterPersistenceProtocol.PersistenceResponse response = releaseTask.GetAwaiter().GetResult();
                if (response == null || response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                    Debug.Error($"Persistence release for character {characterID} returned {response?.Result}.");
            }
            catch (Exception exception)
            {
                Debug.Error($"Persistence release failed for character {characterID}: {exception.Message}");
            }
        }
    }
}
