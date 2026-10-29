using System;

namespace Game.Server.Networking
{
    /// <summary>Stores the Master-owned identity published after server authentication.</summary>
    internal sealed class ConnectedAccountContext
    {
        internal ConnectedAccountContext(string username, Guid accountID, Guid homeWorldID, long revision)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("A username is required.", nameof(username));
            if (accountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(accountID));
            if (homeWorldID == Guid.Empty)
                throw new ArgumentException("A nonempty home World ID is required.", nameof(homeWorldID));
            if (revision < 1)
                throw new ArgumentOutOfRangeException(nameof(revision), "The account revision must be positive.");

            Username = username;
            AccountID = accountID;
            HomeWorldID = homeWorldID;
            Revision = revision;
        }

        internal string Username { get; }
        internal Guid AccountID { get; }
        internal Guid HomeWorldID { get; }
        internal long Revision { get; }
    }
}
