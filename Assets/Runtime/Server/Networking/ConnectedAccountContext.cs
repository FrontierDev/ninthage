using System;

namespace Game.Server.Networking
{
    /// <summary>Stores the Master-owned identity published after server authentication.</summary>
    internal sealed class ConnectedAccountContext
    {
        /// <summary>Creates the local identity binding for one published gameplay session.</summary>
        internal ConnectedAccountContext(
            string username,
            Guid gameplaySessionID,
            Guid accountID,
            Guid homeWorldID,
            long revision,
            bool wasReclaimed,
            Guid? selectedCharacterID)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("A username is required.", nameof(username));
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException("A nonempty gameplay session ID is required.", nameof(gameplaySessionID));
            if (accountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(accountID));
            if (homeWorldID == Guid.Empty)
                throw new ArgumentException("A nonempty home World ID is required.", nameof(homeWorldID));
            if (revision < 1)
                throw new ArgumentOutOfRangeException(nameof(revision), "The account revision must be positive.");
            if (selectedCharacterID.HasValue && selectedCharacterID.Value == Guid.Empty)
                throw new ArgumentException(
                    "A selected character ID must be nonempty when present.",
                    nameof(selectedCharacterID));

            Username = username;
            GameplaySessionID = gameplaySessionID;
            AccountID = accountID;
            HomeWorldID = homeWorldID;
            Revision = revision;
            WasReclaimed = wasReclaimed;
            SelectedCharacterID = selectedCharacterID;
        }

        /// <summary>Gets the legacy username associated with the session.</summary>
        internal string Username { get; }

        /// <summary>Gets the Master-owned gameplay session ID.</summary>
        internal Guid GameplaySessionID { get; }

        /// <summary>Gets the Master-owned account ID.</summary>
        internal Guid AccountID { get; }

        /// <summary>Gets the account's home World ID.</summary>
        internal Guid HomeWorldID { get; }

        /// <summary>Gets the Master account revision.</summary>
        internal long Revision { get; }

        /// <summary>Gets whether the Master reclaimed the gameplay session.</summary>
        internal bool WasReclaimed { get; }

        /// <summary>Gets the selected character ID, when one was supplied.</summary>
        internal Guid? SelectedCharacterID { get; }
    }
}
