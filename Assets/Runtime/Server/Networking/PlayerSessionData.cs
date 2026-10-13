using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Shared.Runtime.Actors;
using PurrNet;
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
        /// <summary>Gets or sets the selected character identifier.</summary>
        public string CharacterID;
        /// <summary>Gets or sets the actor spawned for the selected character.</summary>
        public Actor PlayerActor;

        /// <summary>Initializes a player session with the authenticated player and character details.</summary>
        /// <param name="playerID">The connected player's network identifier.</param>
        /// <param name="username">The authenticated username.</param>
        /// <param name="characterID">The selected character identifier.</param>
        public PlayerSessionData(PlayerID playerID, string username, string characterID)
        {
            PlayerID = playerID;
            Username = username;
            CharacterID = characterID;
        }

        /// <summary>Associates the spawned actor with this session.</summary>
        /// <param name="actor">The actor spawned for the player.</param>
        public void SetPlayerActor(Actor actor) { PlayerActor = actor; }
    }
}
