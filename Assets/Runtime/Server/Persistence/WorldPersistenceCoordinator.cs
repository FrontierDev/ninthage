using Game.Server.Networking.Master;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Persistence
{
    /// <summary>Coordinates bounded-retry Master persistence reads for one World Server run.</summary>
    internal sealed partial class WorldPersistenceCoordinator
    {
        private const int FIRST_RETRY_DELAY_MILLISECONDS = 500;
        private const int SECOND_RETRY_DELAY_MILLISECONDS = 1000;
        private const int THIRD_RETRY_DELAY_MILLISECONDS = 2000;

        private readonly object _stateLock = new();
        private readonly MasterPersistenceClient _client;
        private readonly MasterSessionClient _sessionClient;
        private bool _acceptingWork = true;
        private int _activeOperationCount;

        /// <summary>Creates a coordinator for one immutable World Server identity.</summary>
        internal WorldPersistenceCoordinator(
            MasterPersistenceClient client,
            MasterSessionClient sessionClient,
            Guid worldID,
            Guid worldServerRunID)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _sessionClient = sessionClient ?? throw new ArgumentNullException(nameof(sessionClient));
            if (worldID == Guid.Empty)
                throw new ArgumentException("A nonempty World ID is required.", nameof(worldID));
            if (worldServerRunID == Guid.Empty)
                throw new ArgumentException("A nonempty World Server run ID is required.", nameof(worldServerRunID));

            WorldID = worldID;
            WorldServerRunID = worldServerRunID;
        }

        /// <summary>Gets the durable World ID stamped onto persistence requests.</summary>
        internal Guid WorldID { get; }

        /// <summary>Gets the immutable World Server run ID stamped onto persistence requests.</summary>
        internal Guid WorldServerRunID { get; }

        /// <summary>Gets whether new ordinary persistence work may be submitted.</summary>
        internal bool IsAcceptingWork
        {
            get
            {
                lock (_stateLock)
                    return _acceptingWork;
            }
        }

        /// <summary>Gets whether no coordinator persistence operation is currently active.</summary>
        internal bool IsIdle
        {
            get
            {
                lock (_stateLock)
                    return _activeOperationCount == 0;
            }
        }

        /// <summary>Prevents new ordinary persistence work from being submitted.</summary>
        internal void StopAcceptingWork()
        {
            lock (_stateLock)
                _acceptingWork = false;
        }

        /// <summary>Loads an account's character roster with the coordinator-owned world context.</summary>
        internal Task<MasterSessionProtocol.CharacterRosterResponse> GetCharacterRosterAsync(
            Guid gameplaySessionID,
            Guid expectedAccountID,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (expectedAccountID == Guid.Empty)
                throw new ArgumentException("A nonempty expected Account ID is required.", nameof(expectedAccountID));
            if (!TryStartOperation())
                return Task.FromResult(CreateUnavailableRosterResponse());

            MasterSessionProtocol.CharacterRosterRequest request = new()
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                GameplaySessionID = gameplaySessionID
            };
            return GetCharacterRosterCoreAsync(request, expectedAccountID, cancellationToken);
        }

        private async Task<MasterSessionProtocol.CharacterRosterResponse> GetCharacterRosterCoreAsync(
            MasterSessionProtocol.CharacterRosterRequest request,
            Guid expectedAccountID,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteSessionPersistenceWithRetryAsync(
                    token => _sessionClient.GetCharacterRosterAsync(request, expectedAccountID, token),
                    response => response.SessionResult,
                    response => response.PersistenceResponse?.Result,
                    CreateUnavailableRosterResponse,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteOperation();
            }
        }

        private async Task<TResponse> ExecuteSessionPersistenceWithRetryAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            Func<TResponse, MasterSessionProtocol.GameplaySessionResult> sessionResultSelector,
            Func<TResponse, MasterPersistenceProtocol.PersistenceResult?> persistenceResultSelector,
            Func<TResponse> unavailableResponseFactory,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (sessionResultSelector == null)
                throw new ArgumentNullException(nameof(sessionResultSelector));
            if (persistenceResultSelector == null)
                throw new ArgumentNullException(nameof(persistenceResultSelector));
            if (unavailableResponseFactory == null)
                throw new ArgumentNullException(nameof(unavailableResponseFactory));

            for (int attempt = 0; attempt < 4; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt > 0)
                {
                    int delayMilliseconds = attempt switch
                    {
                        1 => FIRST_RETRY_DELAY_MILLISECONDS,
                        2 => SECOND_RETRY_DELAY_MILLISECONDS,
                        3 => THIRD_RETRY_DELAY_MILLISECONDS,
                        _ => throw new InvalidOperationException("The persistence retry schedule is invalid.")
                    };
                    await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    TResponse response = await operation(cancellationToken).ConfigureAwait(false);
                    MasterSessionProtocol.GameplaySessionResult sessionResult = sessionResultSelector(response);
                    MasterPersistenceProtocol.PersistenceResult? persistenceResult = persistenceResultSelector(response);
                    if (sessionResult != MasterSessionProtocol.GameplaySessionResult.PersistenceUnavailable
                        && !(sessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted
                            && persistenceResult == MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable))
                    {
                        return response;
                    }
                }
                catch (HttpRequestException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (TimeoutException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (attempt == 3)
                    return unavailableResponseFactory();
            }

            throw new InvalidOperationException("The persistence retry loop completed unexpectedly.");
        }

        private async Task<TResponse> ExecuteWithRetryAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            Func<TResponse, MasterPersistenceProtocol.PersistenceResult> resultSelector,
            Func<TResponse> unavailableResponseFactory,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));
            if (unavailableResponseFactory == null)
                throw new ArgumentNullException(nameof(unavailableResponseFactory));

            for (int attempt = 0; attempt < 4; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt > 0)
                {
                    int delayMilliseconds = attempt switch
                    {
                        1 => FIRST_RETRY_DELAY_MILLISECONDS,
                        2 => SECOND_RETRY_DELAY_MILLISECONDS,
                        3 => THIRD_RETRY_DELAY_MILLISECONDS,
                        _ => throw new InvalidOperationException("The persistence retry schedule is invalid.")
                    };
                    await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    TResponse response = await operation(cancellationToken).ConfigureAwait(false);
                    if (resultSelector(response) != MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable)
                        return response;
                }
                catch (HttpRequestException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (TimeoutException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (attempt == 3)
                    return unavailableResponseFactory();
            }

            throw new InvalidOperationException("The persistence retry loop completed unexpectedly.");
        }

        private bool TryStartOperation()
        {
            lock (_stateLock)
            {
                if (!_acceptingWork)
                    return false;

                _activeOperationCount++;
                return true;
            }
        }

        private void CompleteOperation()
        {
            lock (_stateLock)
                _activeOperationCount--;
        }

        private static MasterSessionProtocol.CharacterRosterResponse CreateUnavailableRosterResponse()
        {
            return new MasterSessionProtocol.CharacterRosterResponse
            {
                SessionResult = MasterSessionProtocol.GameplaySessionResult.PersistenceUnavailable
            };
        }
    }
}
