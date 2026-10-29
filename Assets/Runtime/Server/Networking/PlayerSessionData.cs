using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Server.Networking.Master;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using UnityEngine;

namespace Game.Server.Networking
{
    /// <summary>
    /// Stores the server-side session state associated with a connected player.
    /// </summary>
    public class PlayerSessionData
    {
        /// <summary>Gets or sets the connected player's network identifier.</summary>
        public PlayerID PlayerID;
        /// <summary>Gets or sets the authenticated username.</summary>
        public string Username;
        /// <summary>Gets or sets the authenticated account identifier.</summary>
        public Guid AccountID;
        /// <summary>Gets or sets the selected character identifier.</summary>
        public Guid CharacterID;
        /// <summary>Gets the compatibility projection of the accepted Master character state.</summary>
        public CharacterData CharacterData;
        /// <summary>Gets the unmodified accepted Master character state.</summary>
        internal MasterPersistenceProtocol.CharacterPersistenceState PersistenceState { get; }
        /// <summary>Gets or sets the actor spawned for the selected character.</summary>
        public Actor PlayerActor;

        /// <summary>Initializes a player session with the authenticated player and character details.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="username">The authenticated username.</param>
        /// <param name="accountID">The Master-owned account identifier.</param>
        /// <param name="characterID">The selected character identifier.</param>
        /// <param name="characterData">The compatibility projection used by the spawn path.</param>
        /// <param name="persistenceState">The accepted Master character state.</param>
        internal PlayerSessionData(
            PlayerID playerID,
            string username,
            Guid accountID,
            Guid characterID,
            CharacterData characterData,
            MasterPersistenceProtocol.CharacterPersistenceState persistenceState)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("A username is required.", nameof(username));
            if (accountID == Guid.Empty)
                throw new ArgumentException("A nonempty Account ID is required.", nameof(accountID));
            if (characterID == Guid.Empty)
                throw new ArgumentException("A nonempty Character ID is required.", nameof(characterID));
            if (characterData == null)
                throw new ArgumentNullException(nameof(characterData));
            if (persistenceState == null)
                throw new ArgumentNullException(nameof(persistenceState));
            if (persistenceState.AccountID == Guid.Empty)
                throw new ArgumentException("The persistence state must contain an Account ID.", nameof(persistenceState));
            if (persistenceState.CharacterID == Guid.Empty)
                throw new ArgumentException("The persistence state must contain a Character ID.", nameof(persistenceState));
            if (persistenceState.AccountID != accountID)
                throw new ArgumentException("The persistence state account does not match the session account.", nameof(persistenceState));
            if (persistenceState.CharacterID != characterID)
                throw new ArgumentException("The persistence state character does not match the session character.", nameof(persistenceState));

            PlayerID = playerID;
            Username = username;
            AccountID = accountID;
            CharacterID = characterID;
            CharacterData = characterData;
            PersistenceState = persistenceState;
        }

        /// <summary>Associates the spawned actor with this session.</summary>
        /// <param name="actor">The actor spawned for the player.</param>
        public void SetPlayerActor(Actor actor) { PlayerActor = actor; }
    }
}
