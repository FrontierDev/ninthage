using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking.Master;
using Game.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Services
{
    /// <summary>
    /// Server-side implementation of IPlayerAuthService.
    /// Authenticates through Master and owns the exact-connection session handoff.
    /// Registered with LoginAuthenticator during server initialization (Step 7).
    /// </summary>
    public class PlayerAuthService : IPlayerAuthService
    {
        private readonly Dictionary<object, AuthenticatedSessionContext> _pendingAuthentications = new();
        private readonly object _pendingAuthenticationLock = new();
        private readonly WorldSessionCoordinator _sessionCoordinator;

        internal PlayerAuthService(WorldSessionCoordinator sessionCoordinator)
        {
            _sessionCoordinator = sessionCoordinator
                ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        }

        /// <summary>
        /// Authenticates one exact connection through Master and stores its accepted session context.
        /// </summary>
        public async Task<bool> AuthenticateConnectionAsync(
            object connection,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            MasterSessionProtocol.AuthenticateResponse response;
            try
            {
                response = await _sessionCoordinator.AuthenticateAsync(
                    username,
                    password,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.Error($"Master gameplay-session authentication was unavailable for username '{username}': {exception.Message}");
                return false;
            }

            if (response.Result != MasterSessionProtocol.GameplaySessionResult.Accepted)
            {
                if (response.Result == MasterSessionProtocol.GameplaySessionResult.InvalidCredentials)
                {
                    Debug.Warning($"Master rejected credentials for username '{username}'.");
                    return false;
                }

                if (response.Result == MasterSessionProtocol.GameplaySessionResult.SessionAlreadyActive)
                {
                    Debug.Warning($"Master rejected gameplay-session authentication for username '{username}' because the session is already active.");
                    return false;
                }

                Debug.Error(
                    $"Master gameplay-session authentication for username '{username}' returned {response.Result}; authentication failed closed.");
                return false;
            }

            AuthenticatedSessionContext sessionContext = new(
                username,
                response.GameplaySessionID.Value,
                response.AccountID.Value,
                response.HomeWorldID.Value,
                response.Revision.Value,
                response.Disposition.Value == MasterSessionProtocol.GameplaySessionDisposition.Reclaimed,
                response.SelectedCharacterID);

            lock (_pendingAuthenticationLock)
            {
                if (_pendingAuthentications.TryGetValue(connection, out _))
                {
                    throw new InvalidOperationException(
                        "The connection already has a pending authenticated session.");
                }

                _pendingAuthentications.Add(connection, sessionContext);
            }

            return true;
        }

        /// <summary>Takes and removes the pending context for the exact connection.</summary>
        public bool TryTakeAuthenticatedSession(
            object connection,
            out AuthenticatedSessionContext sessionContext)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            lock (_pendingAuthenticationLock)
            {
                if (!_pendingAuthentications.TryGetValue(connection, out sessionContext))
                {
                    sessionContext = null;
                    return false;
                }

                if (!_pendingAuthentications.Remove(connection))
                    throw new InvalidOperationException(
                        "The pending authenticated session disappeared during removal.");

                return true;
            }
        }

        /// <summary>Removes the pending context without publishing it to the server session owner.</summary>
        public bool TryRemovePendingAuthentication(
            object connection,
            out AuthenticatedSessionContext sessionContext)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            lock (_pendingAuthenticationLock)
            {
                if (!_pendingAuthentications.TryGetValue(connection, out sessionContext))
                {
                    sessionContext = null;
                    return false;
                }

                if (!_pendingAuthentications.Remove(connection))
                    throw new InvalidOperationException(
                        "The pending authenticated session disappeared during removal.");

                return true;
            }
        }

        /// <summary>Removes pending authentication and compensates the Master session when required.</summary>
        public async Task AbandonPendingAuthenticationAsync(
            object connection,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (!TryRemovePendingAuthentication(connection, out AuthenticatedSessionContext sessionContext))
                return;

            try
            {
                MasterSessionProtocol.SessionResponse response =
                    await _sessionCoordinator.DisconnectAsync(
                        sessionContext.GameplaySessionID,
                        cancellationToken);

                if (response.Result == MasterSessionProtocol.GameplaySessionResult.Accepted)
                    return;

                if (response.Result == MasterSessionProtocol.GameplaySessionResult.StaleSession)
                {
                    Debug.Warning(
                        $"Master no longer had pending gameplay session {sessionContext.GameplaySessionID} during authentication cleanup.");
                    return;
                }

                Debug.Error(
                    $"Master gameplay-session cleanup for {sessionContext.GameplaySessionID} returned {response.Result}; cleanup was not accepted.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                Debug.Error(
                    $"Master gameplay-session cleanup for {sessionContext.GameplaySessionID} was canceled unexpectedly: {exception.Message}");
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Master gameplay-session cleanup for {sessionContext.GameplaySessionID} failed: {exception.Message}");
            }
        }
    }
}
