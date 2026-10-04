using System.Threading.Tasks;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Transports;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Authentication
{
    /// <summary>
    /// Game-specific authenticator for SpaceSim.
    /// Handles client-side authentication payload generation and server-side validation.
    /// Stores authenticated usernames for server-side retrieval.
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
        /// Called by ServerInitialization (Step 7) after the player database is loaded.
        /// </summary>
        public static void SetAuthService(IPlayerAuthService authService) { _authService = authService; }
        #endregion

        #region Public API - Server Session (delegated to IPlayerAuthService)
        public static string GetNextValidatedUsername() => _authService?.GetNextValidatedUsername() ?? "Unknown";
        public static bool GetNextPlayerIsNew() => _authService?.GetNextPlayerIsNew() ?? false;
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
        /// Server: Validate the client's credentials.
        /// Delegates to the registered IPlayerAuthService; creates new accounts on first login.
        /// </summary>
        protected override Task<AuthenticationResponse> ValidateClientPayload(Connection connection, AuthenticationPayload payload)
        {
            if (_authService == null)
            {
                Debug.Error("No IPlayerAuthService registered. Cannot validate credentials.");
                return Task.FromResult(new AuthenticationResponse { success = false });
            }

            bool isValid = _authService.ValidateCredentials(payload.Username, payload.Password, out bool isNewPlayer);

            if (isValid)
            {
                _authService.TrackPendingConnection(connection, payload.Username);
                _authService.EnqueueNewPlayerFlag(isNewPlayer);
            }
            else
            {
                Debug.Warning($"✗ Authentication failed for: {payload.Username}");
            }

            return Task.FromResult(new AuthenticationResponse { success = isValid });
        }

        /// <summary>
        /// Called when a client is unauthenticated.
        /// </summary>
        protected override void UnAuthenticateClient(Connection connection)
        {
            if (_authService != null && _authService.TryRemovePendingConnection(connection, out var username)) { }
        }
        #endregion
    }
}
