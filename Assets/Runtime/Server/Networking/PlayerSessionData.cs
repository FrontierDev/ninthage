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
        public PlayerID PlayerId;
        /// <summary>Gets or sets the authenticated username.</summary>
        public string Username;
        /// <summary>Gets or sets the selected character identifier.</summary>
        public string CharacterId;
        /// <summary>Gets or sets the actor spawned for the selected character.</summary>
        public Actor PlayerActor;

        /// <summary>Initializes a player session with the authenticated player and character details.</summary>
        /// <param name="playerId">The connected player's network identifier.</param>
        /// <param name="username">The authenticated username.</param>
        /// <param name="characterId">The selected character identifier.</param>
        public PlayerSessionData(PlayerID playerId, string username, string characterId)
        {
            PlayerId = playerId;
            Username = username;
            CharacterId = characterId;
        }

        /// <summary>Associates the spawned actor with this session.</summary>
        /// <param name="actor">The actor spawned for the player.</param>
        public void SetPlayerActor(Actor actor) { PlayerActor = actor; }
    }
}
