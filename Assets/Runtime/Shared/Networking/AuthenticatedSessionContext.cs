using System;

namespace Game.Shared.Networking
{
    /// <summary>Contains the immutable identity established by an accepted Master gameplay session.</summary>
    public sealed class AuthenticatedSessionContext
    {
        /// <summary>Creates an accepted authentication context for one gameplay session.</summary>
        public AuthenticatedSessionContext(
            string username,
            Guid gameplaySessionID,
            Guid accountID,
            Guid homeWorldID,
            long revision,
            bool wasReclaimed,
            Guid? selectedCharacterID)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("A nonblank username is required.", nameof(username));
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

        /// <summary>Gets the legacy username supplied for authentication.</summary>
        public string Username { get; }

        /// <summary>Gets the Master-owned gameplay session ID.</summary>
        public Guid GameplaySessionID { get; }

        /// <summary>Gets the Master-owned account ID.</summary>
        public Guid AccountID { get; }

        /// <summary>Gets the account's home World ID.</summary>
        public Guid HomeWorldID { get; }

        /// <summary>Gets the Master account revision associated with the session.</summary>
        public long Revision { get; }

        /// <summary>Gets whether the Master reclaimed an existing gameplay session.</summary>
        public bool WasReclaimed { get; }

        /// <summary>Gets the selected character ID, when the Master supplied one.</summary>
        public Guid? SelectedCharacterID { get; }
    }
}
