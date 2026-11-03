using System.Threading;
using System.Threading.Tasks;

namespace Game.Shared.Networking
{
    /// <summary>Authenticates a client connection with a Master-issued World-entry handoff token.</summary>
    public interface IWorldEntryAuthService
    {
        /// <summary>Authenticates the exact connection using the opaque World-entry handoff token.</summary>
        Task<bool> AuthenticateWorldEntryConnectionAsync(
            object connection,
            string handoffToken,
            CancellationToken cancellationToken);
    }
}
