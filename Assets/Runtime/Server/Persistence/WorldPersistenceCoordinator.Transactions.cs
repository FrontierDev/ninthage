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

        /// <summary>Creates a durable character with one retry-safe operation ID.</summary>
        internal Task<MasterSessionProtocol.CharacterMutationResponse> CreateCharacterAsync(
            Guid gameplaySessionID,
            CharacterCreationPersistenceCommand command,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterSessionProtocol.CharacterCreateRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    GameplaySessionID = gameplaySessionID,
                    OperationID = operationID,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    RaceDefinitionID = command.RaceDefinitionID,
                    ClassDefinitionID = command.ClassDefinitionID,
                    FactionDefinitionID = command.FactionDefinitionID,
                    WorldState = MapWorldState(command.WorldState),
                    Resources = MapResources(command.Resources),
                    Abilities = MapAbilities(command.Abilities),
                    WeaponSkills = MapWeaponSkills(command.WeaponSkills),
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
        internal Task<MasterSessionProtocol.CharacterMutationResponse> DeleteCharacterAsync(
            Guid gameplaySessionID,
            Guid characterID,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            ValidateLifecycleArguments(gameplaySessionID, characterID, expectedRevision);
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterSessionProtocol.CharacterDeleteRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    GameplaySessionID = gameplaySessionID,
                    OperationID = operationID,
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
        internal Task<MasterSessionProtocol.CharacterMutationResponse> RestoreCharacterAsync(
            Guid gameplaySessionID,
            Guid characterID,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            ValidateLifecycleArguments(gameplaySessionID, characterID, expectedRevision);
            if (!TryAdmitTransaction())
                return Task.FromResult(CreateUnavailableMutationResponse());

            try
            {
                Guid operationID = GenerateOperationID();
                MasterSessionProtocol.CharacterRestoreRequest request = new()
                {
                    WorldID = WorldID,
                    WorldServerRunID = WorldServerRunID,
                    GameplaySessionID = gameplaySessionID,
                    OperationID = operationID,
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

        private async Task<MasterSessionProtocol.CharacterMutationResponse> CreateCharacterCoreAsync(
            MasterSessionProtocol.CharacterCreateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteSessionPersistenceWithRetryAsync(
                    token => _sessionClient.CreateCharacterAsync(request, token),
                    response => response.SessionResult,
                    response => response.PersistenceResponse?.Result,
                    CreateUnavailableMutationResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private async Task<MasterSessionProtocol.CharacterMutationResponse> DeleteCharacterCoreAsync(
            MasterSessionProtocol.CharacterDeleteRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    request.CharacterID,
                    cancellationToken).ConfigureAwait(false);
                if (HasOwnedLease(laneHandle.Lane))
                    return CreateLeaseConflictMutationResponse();

                return await ExecuteSessionPersistenceWithRetryAsync(
                    token => _sessionClient.DeleteCharacterAsync(request, token),
                    response => response.SessionResult,
                    response => response.PersistenceResponse?.Result,
                    CreateUnavailableMutationResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteTransaction();
            }
        }

        private async Task<MasterSessionProtocol.CharacterMutationResponse> RestoreCharacterCoreAsync(
            MasterSessionProtocol.CharacterRestoreRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using CharacterLaneHandle laneHandle = await AcquireCharacterLaneAsync(
                    request.CharacterID,
                    cancellationToken).ConfigureAwait(false);
                if (HasOwnedLease(laneHandle.Lane))
                    return CreateLeaseConflictMutationResponse();

                return await ExecuteSessionPersistenceWithRetryAsync(
                    token => _sessionClient.RestoreCharacterAsync(request, token),
                    response => response.SessionResult,
                    response => response.PersistenceResponse?.Result,
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
            Guid gameplaySessionID,
            Guid characterID,
            long expectedRevision)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
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
                FacingX = worldState.FacingX,
                FacingY = worldState.FacingY,
                FacingZ = worldState.FacingZ,
                FacingW = worldState.FacingW,
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

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialAbility[] MapAbilities(
            CharacterCreationPersistenceCommand.InitialAbility[] abilities)
        {
            MasterPersistenceProtocol.CharacterCreateRequest.InitialAbility[] mapped =
                new MasterPersistenceProtocol.CharacterCreateRequest.InitialAbility[abilities.Length];
            for (int index = 0; index < abilities.Length; index++)
            {
                CharacterCreationPersistenceCommand.InitialAbility ability = abilities[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCreateRequest.InitialAbility
                {
                    AbilityDefinitionID = ability.AbilityDefinitionID,
                    Rank = ability.Rank
                };
            }

            return mapped;
        }

        private static MasterPersistenceProtocol.CharacterCreateRequest.InitialWeaponSkill[] MapWeaponSkills(
            CharacterCreationPersistenceCommand.InitialWeaponSkill[] weaponSkills)
        {
            MasterPersistenceProtocol.CharacterCreateRequest.InitialWeaponSkill[] mapped =
                new MasterPersistenceProtocol.CharacterCreateRequest.InitialWeaponSkill[weaponSkills.Length];
            for (int index = 0; index < weaponSkills.Length; index++)
            {
                CharacterCreationPersistenceCommand.InitialWeaponSkill weaponSkill = weaponSkills[index];
                mapped[index] = new MasterPersistenceProtocol.CharacterCreateRequest.InitialWeaponSkill
                {
                    WeaponType = weaponSkill.WeaponType,
                    Level = weaponSkill.Level,
                    Experience = weaponSkill.Experience
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

        private static MasterSessionProtocol.CharacterMutationResponse CreateLeaseConflictMutationResponse()
        {
            return new MasterSessionProtocol.CharacterMutationResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.Accepted,
                PersistenceResponse = new MasterPersistenceProtocol.CharacterMutationResponse
                {
                    Result = MasterPersistenceProtocol.PersistenceResult.LeaseConflict
                }
            };
        }

        private static MasterSessionProtocol.CharacterMutationResponse CreateUnavailableMutationResponse()
        {
            return new MasterSessionProtocol.CharacterMutationResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.PersistenceUnavailable
            };
        }
    }
}
