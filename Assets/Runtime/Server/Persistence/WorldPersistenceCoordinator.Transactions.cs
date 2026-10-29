using Game.Server.Networking.Master;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Persistence
{
    internal sealed partial class WorldPersistenceCoordinator
    {
        private const int MAX_PENDING_PERSISTENCE_TRANSACTIONS = 4096;

        private int _pendingPersistenceTransactionCount;

        /// <summary>Creates a temporary account identity with one retry-safe operation ID.</summary>
        internal Task<MasterPersistenceProtocol.AccountIdentityResponse> CreateAccountAsync(
            string legacyUsername,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(legacyUsername))
                throw new ArgumentException("A nonempty legacy username is required.", nameof(legacyUsername));
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableAccountResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterPersistenceProtocol.AccountCreateRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    OperationID = operationID,
                    LegacyUsername = legacyUsername
                };
                return CreateAccountCoreAsync(request, cancellationToken);
            }
            catch
            {
                CompleteTransaction();
                throw;
            }
        }

        /// <summary>Creates a durable character with one retry-safe operation ID.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> CreateCharacterAsync(
            CharacterCreationPersistenceCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.AccountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(command));
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterPersistenceProtocol.CharacterCreateRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    OperationID = operationID,
                    AccountID = command.AccountID,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    RaceDefinitionID = command.RaceDefinitionID,
                    ClassDefinitionID = command.ClassDefinitionID,
                    FactionDefinitionID = command.FactionDefinitionID,
                    WorldState = MapWorldState(command.WorldState),
                    Resources = MapResources(command.Resources),
                    Reputations = MapReputations(command.Reputations),
                    Items = MapItems(command.Items)
                };
                return CreateCharacterCoreAsync(request, cancellationToken);
            }
            catch
            {
                CompleteTransaction();
                throw;
            }
        }

        /// <summary>Deletes a character with one retry-safe operation ID.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> DeleteCharacterAsync(
            Guid accountID,
            Guid characterID,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            ValidateLifecycleArguments(accountID, characterID, expectedRevision);
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterPersistenceProtocol.CharacterDeleteRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    OperationID = operationID,
                    AccountID = accountID,
                    CharacterID = characterID,
                    ExpectedRevision = expectedRevision
                };
                return DeleteCharacterCoreAsync(request, cancellationToken);
            }
            catch
            {
                CompleteTransaction();
                throw;
            }
        }

        /// <summary>Restores a character with one retry-safe operation ID.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> RestoreCharacterAsync(
            Guid accountID,
            Guid characterID,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            ValidateLifecycleArguments(accountID, characterID, expectedRevision);
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterPersistenceProtocol.CharacterRestoreRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    OperationID = operationID,
                    AccountID = accountID,
                    CharacterID = characterID,
                    ExpectedRevision = expectedRevision
                };
                return RestoreCharacterCoreAsync(request, cancellationToken);
            }
            catch
            {
                CompleteTransaction();
                throw;
            }
        }

        private async Task<MasterPersistenceProtocol.AccountIdentityResponse> CreateAccountCoreAsync(
            MasterPersistenceProtocol.AccountCreateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteWithRetryAsync(
                    token => _client.CreateAccountAsync(request, token),
                    response => response.Result,
                    CreateUnavailableAccountResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private async Task<MasterPersistenceProtocol.CharacterMutationResponse> CreateCharacterCoreAsync(
            MasterPersistenceProtocol.CharacterCreateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteWithRetryAsync(
                    token => _client.CreateCharacterAsync(request, token),
                    response => response.Result,
                    CreateUnavailableMutationResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private async Task<MasterPersistenceProtocol.CharacterMutationResponse> DeleteCharacterCoreAsync(
            MasterPersistenceProtocol.CharacterDeleteRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    request.CharacterID,
                    cancellationToken).ConfigureAwait(false);
                if (HasOwnedLease(laneHandle.Lane))
                    return CreateLeaseConflictMutationResponse();

                return await ExecuteWithRetryAsync(
                    token => _client.DeleteCharacterAsync(request, token),
                    response => response.Result,
                    CreateUnavailableMutationResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private async Task<MasterPersistenceProtocol.CharacterMutationResponse> RestoreCharacterCoreAsync(
            MasterPersistenceProtocol.CharacterRestoreRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    request.CharacterID,
                    cancellationToken).ConfigureAwait(false);
                if (HasOwnedLease(laneHandle.Lane))
                    return CreateLeaseConflictMutationResponse();

                return await ExecuteWithRetryAsync(
                    token => _client.RestoreCharacterAsync(request, token),
                    response => response.Result,
                    CreateUnavailableMutationResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private bool TryAdmitTransaction()
        {
            lock (_stateLock)
            {
                if (!_acceptingWork
                    || _pendingPersistenceTransactionCount >= MAX_PENDING_PERSISTENCE_TRANSACTIONS)
                {
                    return false;
                }

                _pendingPersistenceTransactionCount++;
                _activeOperationCount++;
                return true;
            }
        }

        private void CompleteTransaction()
        {
            lock (_stateLock)
            {
                _pendingPersistenceTransactionCount--;
                _activeOperationCount--;
                if (_pendingPersistenceTransactionCount < 0 || _activeOperationCount < 0)
                    throw new InvalidOperationException("The persistence transaction count became negative.");
            }
        }

        private static Guid GenerateOperationID()
        {
            Guid operationID = Guid.NewGuid();
            if (operationID == Guid.Empty)
                throw new InvalidOperationException("The generated persistence OperationID was empty.");
            return operationID;
        }

        private static void ValidateLifecycleArguments(
            Guid accountID,
            Guid characterID,
            long expectedRevision)
        {
            if (accountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(accountID));
            if (characterID == Guid.Empty)
                throw new ArgumentException("A nonempty Character ID is required.", nameof(characterID));
            if (expectedRevision < 1)
                throw new ArgumentOutOfRangeException(nameof(expectedRevision), "Expected revision must be at least 1.");
        }

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialWorldState MapWorldState(
            CharacterCreationPersistenceCommand.InitialWorldState worldState)
        {
            if (worldState == null)
                return null;

            return new MasterPersistenceProtocol.CharacterCreateRequest.InitialWorldState
            {
                MapID = worldState.MapID,
                InstanceID = worldState.InstanceID,
                PositionX = worldState.PositionX,
                PositionY = worldState.PositionY,
                PositionZ = worldState.PositionZ,
                SafeReturnLocationID = worldState.SafeReturnLocationID
            };
        }

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialResource[] MapResources(
            CharacterCreationPersistenceCommand.InitialResource[] resources)
        {
            MasterPersistenceProtocol.CharacterCreateRequest.InitialResource[] mapped =
                new MasterPersistenceProtocol.CharacterCreateRequest.InitialResource[resources.Length];
            for (int index = 0; index < resources.Length; index++)
            {
                CharacterCreationPersistenceCommand.InitialResource resource = resources[index];
                mapped[index] = resource == null
                    ? null
                    : new MasterPersistenceProtocol.CharacterCreateRequest.InitialResource
                    {
                        ResourceDefinitionID = resource.ResourceDefinitionID,
                        CurrentValue = resource.CurrentValue
                    };
            }

            return mapped;
        }

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialReputation[] MapReputations(
            CharacterCreationPersistenceCommand.InitialReputation[] reputations)
        {
            MasterPersistenceProtocol.CharacterCreateRequest.InitialReputation[] mapped =
                new MasterPersistenceProtocol.CharacterCreateRequest.InitialReputation[reputations.Length];
            for (int index = 0; index < reputations.Length; index++)
            {
                CharacterCreationPersistenceCommand.InitialReputation reputation = reputations[index];
                mapped[index] = reputation == null
                    ? null
                    : new MasterPersistenceProtocol.CharacterCreateRequest.InitialReputation
                    {
                        FactionDefinitionID = reputation.FactionDefinitionID,
                        Reputation = reputation.Reputation
                    };
            }

            return mapped;
        }

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialItem[] MapItems(
            CharacterCreationPersistenceCommand.InitialItem[] items)
        {
            MasterPersistenceProtocol.CharacterCreateRequest.InitialItem[] mapped =
                new MasterPersistenceProtocol.CharacterCreateRequest.InitialItem[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                CharacterCreationPersistenceCommand.InitialItem item = items[index];
                mapped[index] = item == null
                    ? null
                    : new MasterPersistenceProtocol.CharacterCreateRequest.InitialItem
                    {
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

        private static MasterPersistenceProtocol.CharacterMutationResponse CreateLeaseConflictMutationResponse()
        {
            return new MasterPersistenceProtocol.CharacterMutationResponse
            {
                Result = MasterPersistenceProtocol.PersistenceResult.LeaseConflict
            };
        }

        private static MasterPersistenceProtocol.CharacterMutationResponse CreateUnavailableMutationResponse()
        {
            return new MasterPersistenceProtocol.CharacterMutationResponse
            {
                Result = MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable
            };
        }
    }
}
