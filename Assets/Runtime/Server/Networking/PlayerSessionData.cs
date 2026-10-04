using PurrNet;
using UnityEngine;
using Actor = Game.Shared.Actor;

namespace Game.Server.Networking
{
    public class PlayerSessionData
    {
        public PlayerID PlayerId;
        public string Username;
        public string CharacterId;
        public Actor PlayerActor;

        public PlayerSessionData(PlayerID playerId, string username, string characterId)
        {
            PlayerId = playerId;
            Username = username;
            CharacterId = characterId;
        }

        public void SetPlayerActor(Actor actor) { PlayerActor = actor; }
    }
}