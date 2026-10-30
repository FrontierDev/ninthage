using Debug = Game.Shared.Utility.FormattedDebug;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Transports;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Game-specific authenticator for SpaceSim.
    /// Handles client-side authentication payload generation and server-side validation.
    /// Stores accepted gameplay-session identity for the exact server connection.
    /// </summary>
    [RegisterNetworkType(typeof(AuthenticationRequest<AuthenticationPayload>))]
    public class LoginAuthenticator : AuthenticationBehaviour<AuthenticationPayload>
    {
        #region Fields - Client Side
        private static AuthenticationPayload clientPayload;
        #endregion

        #region Fields - Server Side
        private static IPlayerAuthService _authService;
        #endregion

        #region Public API - Client Setup
        /// <summary>
        /// Set the credentials to be sent during authentication.
        /// Called by MainMenuController before connecting.
        /// </summary>
        public static void SetCredentials(string username, string password)
        {
            clientPayload = new AuthenticationPayload(username, password);
            Debug.Log($"Credentials set for: {username}");
        }
        #endregion

        #region Public API - Service Registration
        /// <summary>
        /// Registers the server-side auth service implementation.
        /// Called by ServerInitialization (Step 7) after Master gameplay-session coordination is available.
        /// </summary>
        public static void SetAuthService(IPlayerAuthService authService) { _authService = authService; }
        #endregion

        #region Public API - Server Session (delegated to IPlayerAuthService)
        /// <summary>Takes the accepted gameplay-session context for the exact PurrNet connection.</summary>
        public static bool TryTakeAuthenticatedSession(
            Connection connection,
            out AuthenticatedSessionContext sessionContext)
        {
            if (_authService == null)
            {
                sessionContext = null;
                return false;
            }

            return _authService.TryTakeAuthenticatedSession(connection, out sessionContext);
        }
        #endregion

        #region Protected Overrides
        /// <summary>
        /// Client: Return the credentials to send to the server.
        /// </summary>
        protected override Task<AuthenticationRequest<AuthenticationPayload>> GetClientPayload()
        {
            if (clientPayload == null)
            {
                Debug.Warning("Client payload not set! Using empty credentials.");
                clientPayload = new AuthenticationPayload("", "");
            }

            Debug.Log($"Sending authentication payload for: {clientPayload.Username}");
            return Task.FromResult(new AuthenticationRequest<AuthenticationPayload>(clientPayload));
        }

        /// <summary>
        /// Server: Validate the client's credentials through Master account authentication.
        /// </summary>
        protected override async Task<AuthenticationResponse> ValidateClientPayload(
            Connection connection,
            AuthenticationPayload payload)
        {
            IPlayerAuthService authService = _authService;
            if (authService == null || !connection.isValid)
            {
                Debug.Error("No valid authentication service or connection is available. Cannot validate credentials.");
                return new AuthenticationResponse { success = false };
            }

            bool authenticated = await authService.AuthenticateConnectionAsync(
                connection,
                payload.Username,
                payload.Password,
                CancellationToken.None);

            if (!authenticated)
            {
                Debug.Warning("Authentication failed for the supplied credentials.");
                return new AuthenticationResponse { success = false };
            }
            if (!ReferenceEquals(authService, _authService) || !connection.isValid)
            {
                Debug.Warning("Authentication completed after service teardown or connection loss.");
                await authService.AbandonPendingAuthenticationAsync(connection, CancellationToken.None);
                return new AuthenticationResponse { success = false };
            }

            return new AuthenticationResponse { success = true };
        }

        /// <summary>
        /// Called when a client is unauthenticated.
        /// </summary>
        protected override void UnAuthenticateClient(Connection connection)
        {
            IPlayerAuthService authService = _authService;
            if (authService == null)
                return;

            Task cleanupTask = authService.AbandonPendingAuthenticationAsync(
                connection,
                CancellationToken.None);

            ObservePendingAuthenticationCleanup(cleanupTask, connection);
        }

        private static void ObservePendingAuthenticationCleanup(
            Task cleanupTask,
            Connection connection)
        {
            if (cleanupTask == null)
            {
                Debug.Error(
                    $"Pending authentication cleanup returned no task for connection {connection}.");
                return;
            }

            var awaiter = cleanupTask.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                CompletePendingAuthenticationCleanup(cleanupTask, connection);
                return;
            }

            awaiter.OnCompleted(
                () => CompletePendingAuthenticationCleanup(cleanupTask, connection));
        }

        private static void CompletePendingAuthenticationCleanup(
            Task cleanupTask,
            Connection connection)
        {
            try
            {
                cleanupTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Pending authentication cleanup failed for connection {connection}: "
                    + exception.Message);
            }
        }
        #endregion
    }
}
