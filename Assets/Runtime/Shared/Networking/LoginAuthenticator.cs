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
    /// Ninth Age authenticator for client credentials and gameplay-session admission.
    /// Handles client-side authentication payload generation and server-side validation.
    /// Stores accepted gameplay-session identity for the exact server connection.
    /// </summary>
    [RegisterNetworkType(typeof(AuthenticationRequest<AuthenticationPayload>))]
    [RegisterNetworkType(typeof(LoginDenialReason))]
    public class LoginAuthenticator :
        AuthenticationBehaviour<AuthenticationPayload, LoginDenialReason>
    {
        #region Fields - Client Side
        private static AuthenticationPayload _clientPayload;
        #endregion

        #region Fields - Server Side
        private static IPlayerAuthService _authService;
        private static ClientCompatibilityIdentity _clientCompatibilityRequirements;
        private static bool _relaxClientBuildAndCatalog;
        #endregion

        #region Public API - Client Setup
        /// <summary>
        /// Set the credentials to be sent during authentication.
        /// Called by MainMenuController before connecting.
        /// </summary>
        public static void SetCredentials(string username, string password)
        {
            var compatibilityIdentity = ClientCompatibilityIdentity.LoadCurrent();
            _clientPayload = new AuthenticationPayload(username, password, compatibilityIdentity);
            Debug.Log($"Credentials set for: {username}");
        }
        #endregion

        #region Public API - Service Registration
        /// <summary>
        /// Registers the server-side auth service implementation.
        /// Called by ServerInitialization (Step 7) after Master gameplay-session coordination is available.
        /// </summary>
        public static void SetAuthService(IPlayerAuthService authService) { _authService = authService; }

        /// <summary>Registers or clears the World-pinned client compatibility requirements.</summary>
        public static void SetClientCompatibilityRequirements(
            ClientCompatibilityIdentity compatibilityRequirements,
            bool relaxClientBuildAndCatalog)
        {
            if (compatibilityRequirements == null && relaxClientBuildAndCatalog)
            {
                throw new ArgumentException(
                    "A client build/catalog relaxation policy requires compatibility requirements.",
                    nameof(relaxClientBuildAndCatalog));
            }

            if (compatibilityRequirements == null || !relaxClientBuildAndCatalog)
            {
                _relaxClientBuildAndCatalog = false;
                _clientCompatibilityRequirements = compatibilityRequirements;
                return;
            }

            _clientCompatibilityRequirements = compatibilityRequirements;
            _relaxClientBuildAndCatalog = true;
        }
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
            if (_clientPayload == null)
            {
                throw new InvalidOperationException(
                    "Client authentication payload was not prepared before authentication started.");
            }

            Debug.Log($"Sending authentication payload for: {_clientPayload.Username}");
            return Task.FromResult(new AuthenticationRequest<AuthenticationPayload>(_clientPayload));
        }

        /// <summary>
        /// Server: Validate the client's credentials through Master account authentication.
        /// </summary>
        protected override async Task<AuthenticationResponse<LoginDenialReason>> ValidateClientPayload(
            Connection connection,
            AuthenticationPayload payload)
        {
            var expectedCompatibility = _clientCompatibilityRequirements;
            bool relaxClientBuildAndCatalog = _relaxClientBuildAndCatalog;
            if (expectedCompatibility == null || !connection.isValid)
            {
                Debug.Error(
                    "Client compatibility requirements or the client connection is unavailable. "
                    + "Cannot authenticate the client.");
                return AuthenticationResponse<LoginDenialReason>.Deny(
                    LoginDenialReason.AuthenticationRejected);
            }

            var compatibilityDenial = GetCompatibilityDenial(
                payload,
                expectedCompatibility,
                relaxClientBuildAndCatalog);
            if (compatibilityDenial.HasValue)
            {
                Debug.Warning($"Client compatibility was rejected: {compatibilityDenial.Value}.");
                return AuthenticationResponse<LoginDenialReason>.Deny(compatibilityDenial.Value);
            }

            var authService = _authService;
            if (authService == null || !connection.isValid)
            {
                Debug.Error("No valid authentication service or connection is available. Cannot validate credentials.");
                return AuthenticationResponse<LoginDenialReason>.Deny(
                    LoginDenialReason.AuthenticationRejected);
            }

            var authenticated = await authService.AuthenticateConnectionAsync(
                connection,
                payload.Username,
                payload.Password,
                CancellationToken.None);

            if (!authenticated)
            {
                Debug.Warning("Authentication failed for the supplied credentials.");
                return AuthenticationResponse<LoginDenialReason>.Deny(
                    LoginDenialReason.AuthenticationRejected);
            }
            if (!ReferenceEquals(authService, _authService)
                || !ReferenceEquals(expectedCompatibility, _clientCompatibilityRequirements)
                || relaxClientBuildAndCatalog != _relaxClientBuildAndCatalog
                || !connection.isValid)
            {
                Debug.Warning("Authentication completed after service teardown or connection loss.");
                await authService.AbandonPendingAuthenticationAsync(connection, CancellationToken.None);
                return AuthenticationResponse<LoginDenialReason>.Deny(
                    LoginDenialReason.AuthenticationRejected);
            }

            return AuthenticationResponse<LoginDenialReason>.Accept();
        }

        /// <summary>Reports the typed denial reason received from the World Server.</summary>
        protected override void OnDeniedByServer(
            DenialKind kind,
            LoginDenialReason? reason)
        {
            if (kind == DenialKind.AuthenticatorRejected && reason.HasValue)
            {
                Debug.Error($"Authentication denied: {reason.Value}.");
                return;
            }

            Debug.Error($"Authentication denied by PurrNet: {kind}.");
        }

        /// <summary>
        /// Called when a client is unauthenticated.
        /// </summary>
        protected override void UnAuthenticateClient(Connection connection)
        {
            var authService = _authService;
            if (authService == null)
                return;

            var cleanupTask = authService.AbandonPendingAuthenticationAsync(
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

        private static LoginDenialReason? GetCompatibilityDenial(
            AuthenticationPayload payload,
            ClientCompatibilityIdentity expectedCompatibility,
            bool relaxClientBuildAndCatalog)
        {
            if (payload == null)
                return LoginDenialReason.ClientUpdateRequired;

            if (!relaxClientBuildAndCatalog
                && (!string.Equals(
                    payload.ClientBuildVersion,
                    expectedCompatibility.ClientBuildVersion,
                    StringComparison.Ordinal)
                || !string.Equals(
                    payload.ClientBuildID,
                    expectedCompatibility.ClientBuildID,
                    StringComparison.Ordinal)))
            {
                return LoginDenialReason.ClientUpdateRequired;
            }

            if (payload.ProtocolVersion != expectedCompatibility.ProtocolVersion)
                return LoginDenialReason.ProtocolMismatch;

            if (payload.GameplayContentRevision != expectedCompatibility.GameplayContentRevision)
                return LoginDenialReason.GameplayContentUpdateRequired;

            if (!string.Equals(
                    payload.GameplayContentManifestHash,
                    expectedCompatibility.GameplayContentManifestHash,
                    StringComparison.Ordinal))
            {
                return LoginDenialReason.GameplayContentVerificationFailed;
            }

            if (!string.Equals(
                    payload.BuildTarget,
                    expectedCompatibility.BuildTarget,
                    StringComparison.Ordinal))
            {
                return LoginDenialReason.UnsupportedBuildTarget;
            }

            if (!relaxClientBuildAndCatalog
                && payload.CatalogRevision != expectedCompatibility.CatalogRevision)
                return LoginDenialReason.AddressablesCatalogUpdateRequired;

            if (!relaxClientBuildAndCatalog
                && !string.Equals(
                    payload.CatalogHash,
                    expectedCompatibility.CatalogHash,
                    StringComparison.Ordinal))
            {
                return LoginDenialReason.AddressablesCatalogVerificationFailed;
            }

            return null;
        }
        #endregion
    }
}
