using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    internal sealed partial class MasterPersistenceClient
    {
        /// <summary>Creates a character through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> CreateCharacterAsync(
            MasterPersistenceProtocol.CharacterCreateRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterMutationResponse>(
                MasterPersistenceProtocol.CHARACTER_CREATE_PATH,
                request,
                response => response.Result,
                ValidateCharacterMutationResponse,
                cancellationToken);
        }

        /// <summary>Soft-deletes a character through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> DeleteCharacterAsync(
            MasterPersistenceProtocol.CharacterDeleteRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterMutationResponse>(
                MasterPersistenceProtocol.CHARACTER_DELETE_PATH,
                request,
                response => response.Result,
                response => ValidateExistingCharacterMutationResponse(response, request.CharacterID),
                cancellationToken);
        }

        /// <summary>Restores a soft-deleted character through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.CharacterMutationResponse> RestoreCharacterAsync(
            MasterPersistenceProtocol.CharacterRestoreRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterMutationResponse>(
                MasterPersistenceProtocol.CHARACTER_RESTORE_PATH,
                request,
                response => response.Result,
                response => ValidateExistingCharacterMutationResponse(response, request.CharacterID),
                cancellationToken);
        }

        /// <summary>Acquires a character persistence lease and loads its durable state.</summary>
        internal Task<MasterPersistenceProtocol.CharacterAcquireResponse> AcquireCharacterAsync(
            MasterPersistenceProtocol.CharacterAcquireRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterAcquireResponse>(
                MasterPersistenceProtocol.CHARACTER_ACQUIRE_PATH,
                request,
                response => response.Result,
                response => ValidateCharacterAcquireResponse(response, request),
                cancellationToken);
        }

        private static void ValidateCharacterMutationResponse(
            MasterPersistenceProtocol.CharacterMutationResponse response)
        {
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (!response.CharacterID.HasValue
                    || response.CharacterID.Value == Guid.Empty
                    || !response.Revision.HasValue
                    || response.Revision.Value < 1)
                {
                    throw new InvalidOperationException("The accepted Master character mutation response was contradictory.");
                }

                return;
            }

            if (response.CharacterID.HasValue || response.Revision.HasValue)
                throw new InvalidOperationException("The rejected Master character mutation response contained success fields.");
        }

        private static void ValidateExistingCharacterMutationResponse(
            MasterPersistenceProtocol.CharacterMutationResponse response,
            Guid requestedCharacterID)
        {
            ValidateCharacterMutationResponse(response);
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted
                && response.CharacterID.Value != requestedCharacterID)
            {
                throw new InvalidOperationException("The Master character mutation response had a different character.");
            }
        }

        private static void ValidateCharacterAcquireResponse(
            MasterPersistenceProtocol.CharacterAcquireResponse response,
            MasterPersistenceProtocol.CharacterAcquireRequest request)
        {
            if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (response.CharacterPersistenceLeaseID.HasValue
                    || response.Revision.HasValue
                    || response.Character != null)
                {
                    throw new InvalidOperationException("The rejected Master character acquire response contained success fields.");
                }

                return;
            }

            if (!response.CharacterPersistenceLeaseID.HasValue
                || response.CharacterPersistenceLeaseID.Value == Guid.Empty
                || !response.Revision.HasValue
                || response.Revision.Value < 1
                || response.Character == null
                || response.Character.CharacterID != request.CharacterID
                || response.Character.AccountID != request.AccountID
                || response.Character.Revision != response.Revision.Value
                || response.Character.Resources == null
                || response.Character.Cooldowns == null
                || response.Character.PersistentAuras == null
                || response.Character.Abilities == null
                || response.Character.Talents == null
                || response.Character.WeaponSkills == null
                || response.Character.Reputations == null
                || response.Character.Quests == null
                || response.Character.Items == null
                || !HasValidWorldState(response.Character.WorldState))
            {
                throw new InvalidOperationException("The accepted Master character acquire response was contradictory.");
            }
        }

        private static bool HasValidWorldState(MasterPersistenceProtocol.CharacterWorldState worldState)
        {
            return worldState == null
                || (worldState.WorldID != Guid.Empty
                    && (!worldState.InstanceID.HasValue || worldState.InstanceID.Value != Guid.Empty));
        }
    }
}
