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
        private readonly Dictionary<Guid, CharacterOperationLane> _characterLanes = new();

        /// <summary>Admits a character through its serialized session-authorized character lane.</summary>
        internal Task<MasterSessionProtocol.CharacterAdmissionResponse> AdmitCharacterAsync(
            Guid gameplaySessionID,
            Guid expectedAccountID,
            Guid characterID,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (expectedAccountID == Guid.Empty)
                throw new ArgumentException("A nonempty expected Account ID is required.", nameof(expectedAccountID));
            if (characterID == Guid.Empty)
                throw new ArgumentException("A nonempty Character ID is required.", nameof(characterID));
            if (!TryStartOperation())
                return Task.FromResult(CreateUnavailableAdmissionResponse());

            return AdmitCharacterCoreAsync(
                gameplaySessionID,
                expectedAccountID,
                characterID,
                cancellationToken);
        }

        /// <summary>Leaves a session-authorized character while preserving the local lease on uncertainty.</summary>
        internal Task<MasterSessionProtocol.CharacterLeaveResponse> LeaveAdmittedCharacterAsync(
            Guid gameplaySessionID,
            Guid characterID,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (characterID == Guid.Empty)
                throw new ArgumentException("A nonempty Character ID is required.", nameof(characterID));
            if (!TryStartOperation())
                return Task.FromResult(CreateUnavailableLeaveResponse());

            return LeaveAdmittedCharacterCoreAsync(gameplaySessionID, characterID, cancellationToken);
        }

        /// <summary>Releases a locally owned character persistence lease with one Master attempt.</summary>
        internal Task<MasterPersistenceProtocol.PersistenceResponse> ReleaseCharacterAsync(
            Guid characterID,
            CancellationToken cancellationToken)
        {
            if (characterID == Guid.Empty)
                throw new ArgumentException("A nonempty Character ID is required.", nameof(characterID));
            if (!TryStartOperation())
                return Task.FromResult(CreateUnavailablePersistenceResponse());

            return ReleaseCharacterTrackedAsync(characterID, cancellationToken);
        }

        /// <summary>Releases all locally owned character leases in ascending CharacterID order.</summary>
        internal async Task ReleaseAllCharactersAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCleanupOperation();
            try
            {
                List<Guid> characterIDs = SnapshotOwnedCharacterIDs();
                foreach (Guid characterID in characterIDs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    MasterPersistenceProtocol.PersistenceResponse response =
                        await ReleaseCharacterCoreAsync(characterID, cancellationToken).ConfigureAwait(false);
                    if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                    {
                        Debug.Warning(
                            $"Persistence character lease release for {characterID} was rejected with result {response.Result}.");
                    }
                }
            }
            finally
            {
                CompleteOperation();
            }
        }

        /// <summary>Gets a locally stored blocking persistence failure for an owned character, if any.</summary>
        internal bool TryGetCharacterPersistenceFailure(
            Guid characterID,
            out MasterPersistenceProtocol.PersistenceResult result)
        {
            result = default;
            if (characterID == Guid.Empty)
                return false;

            lock (_stateLock)
            {
                if (!_characterLanes.TryGetValue(characterID, out CharacterOperationLane lane)
                    || !lane.HasOwnedLease
                    || !lane.BlockingResult.HasValue)
                {
                    return false;
                }

                result = lane.BlockingResult.Value;
                return true;
            }
        }

        private async Task<MasterSessionProtocol.CharacterAdmissionResponse> AdmitCharacterCoreAsync(
            Guid gameplaySessionID,
            Guid expectedAccountID,
            Guid characterID,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    characterID,
                    cancellationToken).ConfigureAwait(false);

                if (HasOwnedLease(laneHandle.Lane))
                    return CreateLeaseConflictAdmissionResponse();

                MasterSessionProtocol.CharacterAdmissionRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    GameplaySessionID = gameplaySessionID,
                    CharacterID = characterID
                };
                MasterSessionProtocol.CharacterAdmissionResponse response =
                    await _sessionClient.AdmitCharacterAsync(
                        request,
                        expectedAccountID,
                        cancellationToken).ConfigureAwait(false);

                if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted
                    && response.PersistenceResult == MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    StoreOwnedLease(
                        laneHandle.Lane,
                        expectedAccountID,
                        response.CharacterPersistenceLeaseID.Value,
                        response.Revision.Value);
                }
                else
                {
                    ClearLeaseState(laneHandle.Lane);
                }

                return response;
            }
            finally
            {
                CompleteOperation();
            }
        }

        private async Task<MasterSessionProtocol.CharacterLeaveResponse> LeaveAdmittedCharacterCoreAsync(
            Guid gameplaySessionID,
            Guid characterID,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    characterID,
                    cancellationToken).ConfigureAwait(false);

                if (!HasOwnedLease(laneHandle.Lane))
                    return CreateStaleLeaseLeaveResponse();

                CharacterLeaseSnapshot lease = GetCurrentLeaseSnapshot(laneHandle.Lane);
                MasterSessionProtocol.CharacterLeaveRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    GameplaySessionID = gameplaySessionID,
                    CharacterID = characterID,
                    CharacterPersistenceLeaseID = lease.CharacterPersistenceLeaseID,
                    ExpectedRevision = lease.Revision
                };
                MasterSessionProtocol.CharacterLeaveResponse response =
                    await _sessionClient.LeaveCharacterAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted
                    && response.PersistenceResult == MasterPersistenceProtocol.PersistenceResult.Accepted)
                {
                    ClearLeaseState(laneHandle.Lane);
                }

                return response;
            }
            finally
            {
                CompleteOperation();
            }
        }

        private async Task<MasterPersistenceProtocol.PersistenceResponse> ReleaseCharacterTrackedAsync(
            Guid characterID,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ReleaseCharacterCoreAsync(characterID, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteOperation();
            }
        }

        private async Task<MasterPersistenceProtocol.PersistenceResponse> ReleaseCharacterCoreAsync(
            Guid characterID,
            CancellationToken cancellationToken)
        {
            using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                characterID,
                cancellationToken).ConfigureAwait(false);

            if (!HasOwnedLease(laneHandle.Lane))
                return CreateStaleLeaseResponse();

            CharacterLeaseSnapshot lease = GetCurrentLeaseSnapshot(laneHandle.Lane);
            MasterPersistenceProtocol.CharacterReleaseRequest request = new()
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                AccountID = lease.AccountID,
                CharacterID = characterID,
                CharacterPersistenceLeaseID = lease.CharacterPersistenceLeaseID,
                ExpectedRevision = lease.Revision
            };

            MasterPersistenceProtocol.PersistenceResponse response;
            try
            {
                response = await _client.ReleaseCharacterAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return CreateUnavailablePersistenceResponse();
            }
            catch (TimeoutException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return CreateUnavailablePersistenceResponse();
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return CreateUnavailablePersistenceResponse();
            }

            switch (response.Result)
            {
                case MasterPersistenceProtocol.PersistenceResult.Accepted:
                    ClearLeaseState(laneHandle.Lane);
                    return response;

                case MasterPersistenceProtocol.PersistenceResult.StaleLease:
                case MasterPersistenceProtocol.PersistenceResult.StaleWorldServerRun:
                case MasterPersistenceProtocol.PersistenceResult.WorldNotActive:
                case MasterPersistenceProtocol.PersistenceResult.WorldNotFound:
                case MasterPersistenceProtocol.PersistenceResult.Unauthorized:
                    ClearLeaseState(laneHandle.Lane);
                    return response;

                case MasterPersistenceProtocol.PersistenceResult.RevisionConflict:
                    SetBlockingResult(laneHandle.Lane, MasterPersistenceProtocol.PersistenceResult.RevisionConflict);
                    return response;

                case MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable:
                    return CreateUnavailablePersistenceResponse();

                default:
                    return response;
            }
        }

        private async Task<CharacterLaneHandle> AcquireCharacterLaneAsync(
            Guid characterID,
            CancellationToken cancellationToken)
        {
            CharacterOperationLane lane;
            lock (_stateLock)
            {
                if (!_characterLanes.TryGetValue(characterID, out lane))
                {
                    lane = new CharacterOperationLane();
                    _characterLanes.Add(characterID, lane);
                }

                lane.ReferenceCount++;
            }

            bool gateAcquired = false;
            try
            {
                await lane.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                gateAcquired = true;
                return new CharacterLaneHandle(this, characterID, lane);
            }
            catch
            {
                if (gateAcquired)
                    lane.Gate.Release();
                ReleaseLaneReference(characterID, lane);
                throw;
            }
        }

        private void ReleaseLaneReference(Guid characterID, CharacterOperationLane lane)
        {
            lock (_stateLock)
            {
                lane.ReferenceCount--;
                if (lane.ReferenceCount < 0)
                    throw new InvalidOperationException("The character lane reference count became negative.");
                if (lane.ReferenceCount != 0 || lane.HasOwnedLease || lane.HasPendingCheckpoint)
                    return;

                if (!_characterLanes.Remove(characterID))
                    throw new InvalidOperationException("The character lane was removed unexpectedly.");
                lane.Dispose();
            }
        }

        private List<Guid> SnapshotOwnedCharacterIDs()
        {
            lock (_stateLock)
            {
                List<Guid> characterIDs = new();
                foreach (KeyValuePair<Guid, CharacterOperationLane> pair in _characterLanes)
                {
                    if (pair.Value.HasOwnedLease)
                        characterIDs.Add(pair.Key);
                }

                characterIDs.Sort();
                return characterIDs;
            }
        }

        private CharacterLeaseSnapshot GetCurrentLeaseSnapshot(CharacterOperationLane lane)
        {
            lock (_stateLock)
            {
                if (!lane.HasOwnedLease
                    || !lane.AccountID.HasValue
                    || !lane.Revision.HasValue)
                {
                    throw new InvalidOperationException("The character lane has no complete owned lease.");
                }

                return new CharacterLeaseSnapshot(
                    lane.AccountID.Value,
                    lane.CharacterPersistenceLeaseID.Value,
                    lane.Revision.Value,
                    lane.BlockingResult);
            }
        }

        private void AdvanceLeaseRevision(CharacterOperationLane lane, long revision)
        {
            lock (_stateLock)
            {
                if (!lane.Revision.HasValue
                    || lane.Revision.Value == long.MaxValue
                    || revision != lane.Revision.Value + 1)
                {
                    throw new InvalidOperationException("The accepted persistence revision was not sequential.");
                }

                lane.Revision = revision;
            }
        }

        private void SetBlockingResult(
            CharacterOperationLane lane,
            MasterPersistenceProtocol.PersistenceResult? result)
        {
            lock (_stateLock)
                lane.BlockingResult = result;
        }

        private void ClearLeaseState(CharacterOperationLane lane)
        {
            lock (_stateLock)
            {
                lane.AccountID = null;
                lane.CharacterPersistenceLeaseID = null;
                lane.Revision = null;
                lane.BlockingResult = null;
            }
        }

        private void StoreOwnedLease(
            CharacterOperationLane lane,
            Guid accountID,
            Guid characterPersistenceLeaseID,
            long revision)
        {
            if (accountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(accountID));
            if (characterPersistenceLeaseID == Guid.Empty)
            {
                throw new ArgumentException(
                    "A nonempty character persistence lease ID is required.",
                    nameof(characterPersistenceLeaseID));
            }
            if (revision < 1)
                throw new ArgumentOutOfRangeException(nameof(revision), "Revision must be at least 1.");

            lock (_stateLock)
            {
                if (lane.HasOwnedLease)
                    throw new InvalidOperationException("The character lane already owns a persistence lease.");

                lane.AccountID = accountID;
                lane.CharacterPersistenceLeaseID = characterPersistenceLeaseID;
                lane.Revision = revision;
                lane.BlockingResult = null;
            }
        }

        private bool HasOwnedLease(CharacterOperationLane lane)
        {
            lock (_stateLock)
                return lane.HasOwnedLease;
        }

        private void StartCleanupOperation()
        {
            lock (_stateLock)
                _activeOperationCount++;
        }

        private static MasterSessionProtocol.CharacterAdmissionResponse CreateLeaseConflictAdmissionResponse()
        {
            return new MasterSessionProtocol.CharacterAdmissionResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.Accepted,
                PersistenceResult = MasterPersistenceProtocol.PersistenceResult.LeaseConflict
            };
        }

        private static MasterSessionProtocol.CharacterAdmissionResponse CreateUnavailableAdmissionResponse()
        {
            return new MasterSessionProtocol.CharacterAdmissionResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.Accepted,
                PersistenceResult = MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable
            };
        }

        private static MasterSessionProtocol.CharacterLeaveResponse CreateStaleLeaseLeaveResponse()
        {
            return new MasterSessionProtocol.CharacterLeaveResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.Accepted,
                PersistenceResult = MasterPersistenceProtocol.PersistenceResult.StaleLease
            };
        }

        private static MasterSessionProtocol.CharacterLeaveResponse CreateUnavailableLeaveResponse()
        {
            return new MasterSessionProtocol.CharacterLeaveResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.Accepted,
                PersistenceResult = MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable
            };
        }

        private static MasterPersistenceProtocol.PersistenceResponse CreateStaleLeaseResponse()
        {
            return new MasterPersistenceProtocol.PersistenceResponse
            {
                Result = MasterPersistenceProtocol.PersistenceResult.StaleLease
            };
        }

        private static MasterPersistenceProtocol.PersistenceResponse CreateUnavailablePersistenceResponse()
        {
            return new MasterPersistenceProtocol.PersistenceResponse
            {
                Result = MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable
            };
        }

        private sealed class CharacterOperationLane : IDisposable
        {
            internal readonly SemaphoreSlim Gate = new(1, 1);
            internal int ReferenceCount;
            internal Guid? AccountID;
            internal Guid? CharacterPersistenceLeaseID;
            internal long? Revision;
            internal MasterPersistenceProtocol.PersistenceResult? BlockingResult;
            internal WorldPersistenceCheckpoint PendingCheckpoint;
            internal long PendingCheckpointGeneration;

            internal bool HasOwnedLease => CharacterPersistenceLeaseID.HasValue;
            internal bool HasPendingCheckpoint => PendingCheckpoint != null;

            public void Dispose()
            {
                Gate.Dispose();
            }
        }

        private sealed class CharacterLaneHandle : IDisposable
        {
            private readonly WorldPersistenceCoordinator _coordinator;
            private readonly Guid _characterID;
            private readonly CharacterOperationLane _lane;
            private bool _disposed;

            internal CharacterLaneHandle(
                WorldPersistenceCoordinator coordinator,
                Guid characterID,
                CharacterOperationLane lane)
            {
                _coordinator = coordinator;
                _characterID = characterID;
                _lane = lane;
            }

            internal CharacterOperationLane Lane => _lane;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _lane.Gate.Release();
                _coordinator.ReleaseLaneReference(_characterID, _lane);
            }
        }

        private readonly struct CharacterLeaseSnapshot
        {
            internal CharacterLeaseSnapshot(
                Guid accountID,
                Guid characterPersistenceLeaseID,
                long revision,
                MasterPersistenceProtocol.PersistenceResult? blockingResult)
            {
                AccountID = accountID;
                CharacterPersistenceLeaseID = characterPersistenceLeaseID;
                Revision = revision;
                BlockingResult = blockingResult;
            }

            internal Guid AccountID { get; }
            internal Guid CharacterPersistenceLeaseID { get; }
            internal long Revision { get; }
            internal MasterPersistenceProtocol.PersistenceResult? BlockingResult { get; }
        }
    }
}
