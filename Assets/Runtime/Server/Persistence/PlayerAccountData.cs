using CharacterData = Game.Shared.Persistence.CharacterData;
using System.Collections.Generic;

namespace Game.Server
{
    /// <summary>Serializable account data used by the server persistence layer.</summary>
    [System.Serializable]
    public class PlayerAccountData
    {
        /// <summary>The account username.</summary>
        public string Username;
        /// <summary>The stored password hash.</summary>
        public string PasswordHash;
        /// <summary>The stored password salt.</summary>
        public string PasswordSalt;
        /// <summary>The characters owned by the account.</summary>
        public List<CharacterData> Characters = new();
    }
}
