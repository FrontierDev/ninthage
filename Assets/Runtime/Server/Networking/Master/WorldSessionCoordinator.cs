using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    /// <summary>Owns the immutable World identity used by gameplay-session requests.</summary>
    internal sealed class WorldSessionCoordinator
    {
        private const int MAX_REQUEST_ATTEMPTS = 4;
        private const int FIRST_RETRY_DELAY_MILLISECONDS = 500;
        private const int SECOND_RETRY_DELAY_MILLISECONDS = 1000;
        private const int THIRD_RETRY_DELAY_MILLISECONDS = 2000;

        private readonly MasterSessionClient _client;

        /// <summary>Creates a coordinator for one registered World Server run.</summary>
        internal WorldSessionCoordinator(
            MasterSessionClient client,
            Guid worldID,
            Guid worldServerRunID)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            if (worldID == Guid.Empty)
                throw new ArgumentException("A nonempty World ID is required.", nameof(worldID));
            if (worldServerRunID == Guid.Empty)
                throw new ArgumentException("A nonempty World Server run ID is required.", nameof(worldServerRunID));

            WorldID = worldID;
            WorldServerRunID = worldServerRunID;
        }

        /// <summary>Gets the immutable durable World ID stamped onto session requests.</summary>
        internal Guid WorldID { get; }

        /// <summary>Gets the immutable World Server run ID stamped onto session requests.</summary>
        internal Guid WorldServerRunID { get; }

        /// <summary>
        /// Authenticates one account through the Master and replays the exact request only for transient transport failures.
        /// </summary>
        internal Task<MasterSessionProtocol.AuthenticateResponse> AuthenticateAsync(
            string legacyUsername,
            string password,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(legacyUsername))
                throw new ArgumentException("A nonblank legacy username is required.", nameof(legacyUsername));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("A nonblank password is required.", nameof(password));

            MasterSessionProtocol.AuthenticateRequest request = new()
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                AuthenticationAttemptID = Guid.NewGuid(),
                LegacyUsername = legacyUsername,
                Password = password
            };
            return ExecuteWithTransientRetryAsync(
                token => _client.AuthenticateAsync(request, token),
                cancellationToken);
        }

        /// <summary>Disconnects one gameplay session through the Master, replaying only transient transport failures.</summary>
        internal Task<MasterSessionProtocol.SessionResponse> DisconnectAsync(
            Guid gameplaySessionID,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));

            MasterSessionProtocol.DisconnectRequest request = new()
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                GameplaySessionID = gameplaySessionID
            };
            return ExecuteWithTransientRetryAsync(
                token => _client.DisconnectAsync(request, token),
                cancellationToken);
        }

        /// <summary>Closes a disconnected gameplay session through the Master.</summary>
        internal Task<MasterSessionProtocol.SessionResponse> CloseGameplaySessionAsync(
            Guid gameplaySessionID,
            CancellationToken cancellationToken)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));

            MasterSessionProtocol.CloseRequest request = new()
            {
                WorldID = WorldID,
                WorldServerRunID = WorldServerRunID,
                GameplaySessionID = gameplaySessionID
            };
            return ExecuteWithTransientRetryAsync(
                token => _client.CloseAsync(request, token),
                cancellationToken);
        }

        private static async Task<TResponse> ExecuteWithTransientRetryAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            Exception finalTransientException = null;
            for (int attempt = 0; attempt < MAX_REQUEST_ATTEMPTS; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt > 0)
                {
                    int delayMilliseconds = attempt switch
                    {
                        1 => FIRST_RETRY_DELAY_MILLISECONDS,
                        2 => SECOND_RETRY_DELAY_MILLISECONDS,
                        3 => THIRD_RETRY_DELAY_MILLISECONDS,
                        _ => throw new InvalidOperationException("The session retry schedule is invalid.")
                    };
                    await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    return await operation(cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException exception)
                {
                    finalTransientException = exception;
                }
                catch (TimeoutException exception)
                {
                    finalTransientException = exception;
                }
                catch (OperationCanceledException exception)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    finalTransientException = exception;
                }
            }

            if (finalTransientException == null)
                throw new InvalidOperationException("The session retry loop completed without a result or transient failure.");

            throw finalTransientException;
        }
    }
}
