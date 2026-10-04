using System.Collections.Generic;
using CharacterData = Game.Shared.Persistence.CharacterData;

namespace Game.Server
{
    [System.Serializable]
    public class PlayerAccountData
    {
        public string Username;
        public string PasswordHash;
        public string PasswordSalt;
        public List<CharacterData> Characters = new List<CharacterData>();
    }
}