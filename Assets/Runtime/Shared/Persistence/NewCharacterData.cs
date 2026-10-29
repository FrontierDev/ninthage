namespace Game.Shared.Persistence
{
    /// <summary>Contains the fields required to create a new character.</summary>
    public struct NewCharacterData
    {
        /// <summary>Gets or sets the character first name.</summary>
        public string FirstName;
        /// <summary>Gets or sets the character last name.</summary>
        public string LastName;
        /// <summary>Gets or sets the character class identifier.</summary>
        public string ClassID;
        /// <summary>Gets or sets the character race identifier.</summary>
        public string RaceID;
    }
}
