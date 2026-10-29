using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Services
{
    /// <summary>
    /// Server-side implementation of IPlayerAuthService.
    /// Validates credentials through Master and owns server-side pending authentication state.
    /// Registered with LoginAuthenticator during server initialization (Step 7).
    /// </summary>
    public class PlayerAuthService : IPlayerAuthService
    {
        private readonly WorldPersistenceCoordinator _persistenceCoordinator;
        private readonly Queue<PendingConnection> _validatedConnectionsQueue = new();
        private readonly Dictionary<object, string> _pendingConnections = new();

        internal PlayerAuthService(WorldPersistenceCoordinator persistenceCoordinator)
        {
            _persistenceCoordinator = persistenceCoordinator
                ?? throw new ArgumentNullException(nameof(persistenceCoordinator));
        }

        /// <summary>
        /// Validates the supplied credentials through the authoritative Master account service.
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync(
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            try
            {
                MasterPersistenceProtocol.AccountAuthenticateResponse response =
                    await _persistenceCoordinator.AuthenticateAccountAsync(
                        username,
                        password,
                        cancellationToken);

                if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
                    return true;

                if (response.Result == MasterPersistenceProtocol.PersistenceResult.InvalidCredentials)
                {
                    Debug.Warning($"Master rejected credentials for username '{username}'.");
                    return false;
                }

                Debug.Error(
                    $"Master account authentication for username '{username}' returned {response.Result}; authentication failed closed.");
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Master account authentication was unavailable for username '{username}': {exception.Message}");
                return false;
            }
        }

        /// <summary>Associates a pending connection with the username being authenticated.</summary>
        /// <param name="connection">The pending connection handle.</param>
        /// <param name="username">The username being authenticated.</param>
        public void TrackPendingConnection(object connection, string username)
        {
            _pendingConnections[connection] = username;
            _validatedConnectionsQueue.Enqueue(new PendingConnection(connection, username));
        }

        /// <summary>Removes and returns the username associated with a pending connection.</summary>
        /// <param name="connection">The pending connection handle.</param>
        /// <param name="username">Receives the associated username when found.</param>
        /// <returns><see langword="true"/> when a pending connection was removed.</returns>
        public bool TryRemovePendingConnection(object connection, out string username)
        {
            if (_pendingConnections.TryGetValue(connection, out username))
            {
                _pendingConnections.Remove(connection);
                return true;
            }
            return false;
        }

        /// <summary>Dequeues the next username that passed authentication.</summary>
        /// <returns>The next validated username, or <c>Unknown</c> when the queue is empty.</returns>
        public string GetNextValidatedUsername()
        {
            while (_validatedConnectionsQueue.Count > 0)
            {
                PendingConnection pendingConnection = _validatedConnectionsQueue.Dequeue();
                if (_pendingConnections.TryGetValue(pendingConnection.Connection, out string username)
                    && username == pendingConnection.Username)
                {
                    return username;
                }
            }

            return "Unknown";
        }

        private readonly struct PendingConnection
        {
            public PendingConnection(object connection, string username)
            {
                Connection = connection;
                Username = username;
            }

            public object Connection { get; }
            public string Username { get; }
        }
    }
}
