using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
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
        /// <summary>Gets the Master-owned gameplay-session identity for this admitted character.</summary>
        public Guid GameplaySessionID;
        /// <summary>Gets or sets the authenticated username.</summary>
        public string Username;
        /// <summary>Gets or sets the authenticated account identifier.</summary>
        public Guid AccountID;
        /// <summary>Gets or sets the selected character identifier.</summary>
        public Guid CharacterID;
        /// <summary>Gets the compatibility projection of the accepted Master character state.</summary>
        public CharacterData CharacterData;
        /// <summary>
        /// Gets the accepted Master character state, advanced to a confirmed recovered location and revision
        /// when admission recovery commits a checkpoint.
        /// </summary>
        internal MasterPersistenceProtocol.CharacterPersistenceState PersistenceState { get; }
        /// <summary>Gets the validated accepted world-entry location.</summary>
        internal CharacterWorldEntryLocation EntryLocation { get; }
        private string _currentSafeReturnLocationID;
        /// <summary>Gets the current live authored safe-return location.</summary>
        internal string CurrentSafeReturnLocationID => _currentSafeReturnLocationID;
        /// <summary>Gets or sets the actor spawned for the selected character.</summary>
        public Actor PlayerActor;

        /// <summary>Initializes a player session with the authenticated player and character details.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="gameplaySessionID">The Master-owned gameplay-session identity.</param>
        /// <param name="username">The authenticated username.</param>
        /// <param name="accountID">The Master-owned account identifier.</param>
        /// <param name="characterID">The selected character identifier.</param>
        /// <param name="characterData">The compatibility projection used by the spawn path.</param>
        /// <param name="persistenceState">
        /// The accepted Master character state, advanced to a confirmed recovered location and revision when
        /// admission recovery commits a checkpoint.
        /// </param>
        /// <param name="entryLocation">The validated accepted world-entry location.</param>
        internal PlayerSessionData(
            PlayerID playerID,
            Guid gameplaySessionID,
            string username,
            Guid accountID,
            Guid characterID,
            CharacterData characterData,
            MasterPersistenceProtocol.CharacterPersistenceState persistenceState,
            CharacterWorldEntryLocation entryLocation)
        {
            if (gameplaySessionID == Guid.Empty)
                throw new ArgumentException(
                    "A nonempty gameplay session ID is required.",
                    nameof(gameplaySessionID));
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
            if (persistenceState.WorldState == null)
                throw new ArgumentException("The persistence state must contain world state.", nameof(persistenceState));
            if (!entryLocation.Context.IsValid)
                throw new ArgumentException("The entry location context must be valid.", nameof(entryLocation));
            if (!entryLocation.Position.IsFinite)
                throw new ArgumentException("The entry location position must be finite.", nameof(entryLocation));
            double facingLengthSquared = (double)entryLocation.Facing.x * entryLocation.Facing.x
                + (double)entryLocation.Facing.y * entryLocation.Facing.y
                + (double)entryLocation.Facing.z * entryLocation.Facing.z
                + (double)entryLocation.Facing.w * entryLocation.Facing.w;
            if (float.IsNaN(entryLocation.Facing.x)
                || float.IsInfinity(entryLocation.Facing.x)
                || float.IsNaN(entryLocation.Facing.y)
                || float.IsInfinity(entryLocation.Facing.y)
                || float.IsNaN(entryLocation.Facing.z)
                || float.IsInfinity(entryLocation.Facing.z)
                || float.IsNaN(entryLocation.Facing.w)
                || float.IsInfinity(entryLocation.Facing.w)
                || Math.Abs(facingLengthSquared - 1d) > 0.001d)
            {
                throw new ArgumentException("The entry location facing must be a finite unit quaternion.", nameof(entryLocation));
            }
            if (persistenceState.WorldState.WorldID != entryLocation.Context.WorldID
                || !StringComparer.Ordinal.Equals(
                    persistenceState.WorldState.MapID,
                    entryLocation.Context.MapID))
            {
                throw new ArgumentException(
                    "The persistence state world does not match the accepted entry location.",
                    nameof(entryLocation));
            }
            Guid persistedInstanceID = persistenceState.WorldState.InstanceID ?? Guid.Empty;
            if (persistedInstanceID != entryLocation.Context.InstanceID)
            {
                throw new ArgumentException(
                    "The persistence state instance does not match the accepted entry location.",
                    nameof(entryLocation));
            }
            if (persistenceState.WorldState.SafeReturnLocationID != null
                && string.IsNullOrWhiteSpace(persistenceState.WorldState.SafeReturnLocationID))
            {
                throw new ArgumentException(
                    "The persistence state safe-return location ID cannot be blank.",
                    nameof(persistenceState));
            }

            PlayerID = playerID;
            GameplaySessionID = gameplaySessionID;
            Username = username;
            AccountID = accountID;
            CharacterID = characterID;
            CharacterData = characterData;
            PersistenceState = persistenceState;
            EntryLocation = entryLocation;
            _currentSafeReturnLocationID = persistenceState.WorldState.SafeReturnLocationID;
        }

        /// <summary>Associates the spawned actor with this session.</summary>
        /// <param name="actor">The actor spawned for the player.</param>
        public void SetPlayerActor(Actor actor) { PlayerActor = actor; }

        /// <summary>Updates the server-owned live safe-return location.</summary>
        /// <param name="locationDefinitionID">The verified authored location ID, or null to explicitly clear it.</param>
        /// <exception cref="ArgumentException">Thrown when a non-null ID is blank or whitespace.</exception>
        internal void SetCurrentSafeReturnLocationID(string locationDefinitionID)
        {
            if (locationDefinitionID != null && string.IsNullOrWhiteSpace(locationDefinitionID))
            {
                throw new ArgumentException(
                    "A non-null safe-return location ID cannot be blank.",
                    nameof(locationDefinitionID));
            }

            _currentSafeReturnLocationID = locationDefinitionID;
        }
    }
}
