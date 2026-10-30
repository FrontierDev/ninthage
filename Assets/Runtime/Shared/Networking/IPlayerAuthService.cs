using System.Threading;
using System.Threading.Tasks;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Abstraction over the server's player authentication and persistence layer.
    /// Defined in Shared so LoginAuthenticator can use it without referencing Game.Server directly.
    /// Implemented and registered by the server during initialization.
    /// </summary>
    public interface IPlayerAuthService
    {
        /// <summary>
        /// Authenticates the exact connection and stores its accepted Master gameplay-session identity.
        /// </summary>
        Task<bool> AuthenticateConnectionAsync(
            object connection,
            string username,
            string password,
            CancellationToken cancellationToken);

        /// <summary>Takes and removes the accepted session context for the exact connection.</summary>
        bool TryTakeAuthenticatedSession(
            object connection,
            out AuthenticatedSessionContext sessionContext);

        /// <summary>Removes without publication the pending context for the exact connection.</summary>
        bool TryRemovePendingAuthentication(
            object connection,
            out AuthenticatedSessionContext sessionContext);

        /// <summary>Removes pending authentication and compensates the Master session when required.</summary>
        Task AbandonPendingAuthenticationAsync(
            object connection,
            CancellationToken cancellationToken);

    }
}
