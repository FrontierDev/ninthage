using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    internal sealed partial class MasterPersistenceClient
    {
        /// <summary>Submits one checkpoint batch through the authenticated Master transport.</summary>
        internal Task<MasterPersistenceProtocol.CharacterCheckpointBatchResponse> CheckpointCharactersAsync(
            MasterPersistenceProtocol.CharacterCheckpointBatchRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync<MasterPersistenceProtocol.CharacterCheckpointBatchResponse>(
                MasterPersistenceProtocol.CHARACTER_CHECKPOINTS_PATH,
                request,
                response => response.Result,
                response => ValidateCheckpointResponse(response, request),
                cancellationToken);
        }

        private static void ValidateCheckpointResponse(
            MasterPersistenceProtocol.CharacterCheckpointBatchResponse response,
            MasterPersistenceProtocol.CharacterCheckpointBatchRequest request)
        {
            if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (response.Checkpoints != null)
                    throw new InvalidOperationException("The rejected Master checkpoint response contained settlement entries.");
                return;
            }

            if (request.Checkpoints == null
                || response.Checkpoints == null
                || response.Checkpoints.Length != request.Checkpoints.Length)
            {
                throw new InvalidOperationException("The accepted Master checkpoint response had an invalid settlement length.");
            }

            for (int index = 0; index < response.Checkpoints.Length; index++)
            {
                MasterPersistenceProtocol.CharacterCheckpointBatchResponse.CheckpointResult result =
                    response.Checkpoints[index];
                MasterPersistenceProtocol.CharacterCheckpointBatchRequest.CheckpointEntry requestEntry =
                    request.Checkpoints[index];
                if (result == null
                    || requestEntry == null
                    || result.CharacterID != requestEntry.CharacterID
                    || !Enum.IsDefined(typeof(MasterPersistenceProtocol.PersistenceResult), result.Result))
                {
                    throw new InvalidOperationException("The accepted Master checkpoint response had invalid settlement ordering.");
                }

                if (result.Result == MasterPersistenceProtocol.PersistenceResult.Accepted
                    && (!result.Revision.HasValue || result.Revision.Value < 1))
                {
                    throw new InvalidOperationException("The accepted Master checkpoint settlement had no valid revision.");
                }

                if (result.Result != MasterPersistenceProtocol.PersistenceResult.Accepted
                    && result.Revision.HasValue)
                {
                    throw new InvalidOperationException("The rejected Master checkpoint settlement contained a revision.");
                }
            }
        }
    }
}
