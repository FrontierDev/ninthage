namespace Game.Shared.Authentication
{
    /// <summary>
    /// Abstraction over the server's player authentication and persistence layer.
    /// Defined in Shared so LoginAuthenticator can use it without referencing Game.Server directly.
    /// Implemented and registered by the server during initialization.
    /// </summary>
    public interface IPlayerAuthService
    {
        /// <summary>
        /// Validates the supplied credentials against the player database.
        /// Creates a new account if the username does not yet exist.
        /// Enqueues the username and isNewPlayer flag for retrieval by the server after onPlayerJoined fires.
        /// </summary>
        bool ValidateCredentials(string username, string password, out bool isNewPlayer);

        /// <summary>Records a validated connection's username for unauthentication lookup.</summary>
        void TrackPendingConnection(object connection, string username);

        /// <summary>Removes and returns the pending username for the given connection.</summary>
        bool TryRemovePendingConnection(object connection, out string username);

        /// <summary>Dequeues the next validated username. Returns "Unknown" if the queue is empty.</summary>
        string GetNextValidatedUsername();

        /// <summary>Dequeues whether the next player is new. Returns false if the queue is empty.</summary>
        bool GetNextPlayerIsNew();

        /// <summary>Enqueues the isNewPlayer flag after a successful validation, paired with TrackPendingConnection.</summary>
        void EnqueueNewPlayerFlag(bool isNewPlayer);
    }
}
