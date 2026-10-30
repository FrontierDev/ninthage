using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Networking.Master
{
    /// <summary>Owns the immutable World identity used by gameplay-session requests.</summary>
    internal sealed class WorldSessionCoordinator
    {
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

        /// <summary>Authenticates one account through the Master without replaying the request.</summary>
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
                LegacyUsername = legacyUsername,
                Password = password
            };
            return _client.AuthenticateAsync(request, cancellationToken);
        }

        /// <summary>Disconnects one gameplay session through the Master.</summary>
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
            return _client.DisconnectAsync(request, cancellationToken);
        }
    }
}
