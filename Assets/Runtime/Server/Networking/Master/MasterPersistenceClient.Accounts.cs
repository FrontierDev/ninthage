using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    internal sealed partial class MasterPersistenceClient
    {
        /// <summary>Resolves a temporary account identity through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.AccountIdentityResponse> ResolveAccountAsync(
            MasterPersistenceProtocol.AccountResolveRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.AccountIdentityResponse>(
                MasterPersistenceProtocol.ACCOUNT_RESOLVE_PATH,
                request,
                response => response.Result,
                ValidateAccountIdentityResponse,
                cancellationToken);
        }

        /// <summary>Creates a temporary account identity through the Master Server.</summary>
        internal Task<MasterPersistenceProtocol.AccountIdentityResponse> CreateAccountAsync(
            MasterPersistenceProtocol.AccountCreateRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.AccountIdentityResponse>(
                MasterPersistenceProtocol.ACCOUNT_CREATE_PATH,
                request,
                response => response.Result,
                response => ValidateCreatedAccountResponse(response, request.WorldID),
                cancellationToken);
        }

        /// <summary>Loads the active and deleted character roster for an account.</summary>
        internal Task<MasterPersistenceProtocol.CharacterRosterResponse> GetCharacterRosterAsync(
            MasterPersistenceProtocol.CharacterRosterRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterRosterResponse>(
                MasterPersistenceProtocol.CHARACTER_ROSTER_PATH,
                request,
                response => response.Result,
                response => ValidateCharacterRosterResponse(response, request.AccountID),
                cancellationToken);
        }

        private static void ValidateAccountIdentityResponse(
            MasterPersistenceProtocol.AccountIdentityResponse response)
        {
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (!response.AccountID.HasValue
                    || response.AccountID.Value == Guid.Empty
                    || !response.HomeWorldID.HasValue
                    || response.HomeWorldID.Value == Guid.Empty
                    || !response.Revision.HasValue
                    || response.Revision.Value < 1)
                {
                    throw new InvalidOperationException("The accepted Master account response was contradictory.");
                }

                return;
            }

            if (response.AccountID.HasValue
                || response.HomeWorldID.HasValue
                || response.Revision.HasValue)
            {
                throw new InvalidOperationException("The rejected Master account response contained success fields.");
            }
        }

        private static void ValidateCreatedAccountResponse(
            MasterPersistenceProtocol.AccountIdentityResponse response,
            Guid requestedWorldID)
        {
            ValidateAccountIdentityResponse(response);
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted
                && response.HomeWorldID.Value != requestedWorldID)
            {
                throw new InvalidOperationException("The created Master account response had a different home world.");
            }
        }

        private static void ValidateCharacterRosterResponse(
            MasterPersistenceProtocol.CharacterRosterResponse response,
            Guid requestedAccountID)
        {
            if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (response.AccountID.HasValue
                    || response.AccountRevision.HasValue
                    || response.ActiveCharacters != null
                    || response.DeletedCharacters != null)
                {
                    throw new InvalidOperationException("The rejected Master roster response contained success fields.");
                }

                return;
            }

            if (!response.AccountID.HasValue
                || response.AccountID.Value == Guid.Empty
                || response.AccountID.Value != requestedAccountID
                || !response.AccountRevision.HasValue
                || response.AccountRevision.Value < 1
                || response.ActiveCharacters == null
                || response.DeletedCharacters == null)
            {
                throw new InvalidOperationException("The accepted Master roster response was contradictory.");
            }

            HashSet<Guid> characterIDs = new();
            ValidateRosterEntries(response.ActiveCharacters, false, characterIDs);
            ValidateRosterEntries(response.DeletedCharacters, true, characterIDs);
        }

        private static void ValidateRosterEntries(
            MasterPersistenceProtocol.CharacterRosterEntry[] entries,
            bool deleted,
            HashSet<Guid> characterIDs)
        {
            foreach (MasterPersistenceProtocol.CharacterRosterEntry entry in entries)
            {
                if (entry == null
                    || entry.CharacterID == Guid.Empty
                    || entry.Revision < 1
                    || entry.DeletedAtUtc.HasValue != deleted
                    || !characterIDs.Add(entry.CharacterID))
                {
                    throw new InvalidOperationException("The Master roster response contained an invalid character entry.");
                }
            }
        }
    }
}
