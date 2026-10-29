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
        /// Validates the supplied credentials against the authoritative account service.
        /// </summary>
        Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);

        /// <summary>Records a validated connection's username for unauthentication lookup.</summary>
        void TrackPendingConnection(object connection, string username);

        /// <summary>Removes and returns the pending username for the given connection.</summary>
        bool TryRemovePendingConnection(object connection, out string username);

        /// <summary>Dequeues the next validated username. Returns "Unknown" if the queue is empty.</summary>
        string GetNextValidatedUsername();

    }
}
