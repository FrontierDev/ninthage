namespace Game.Shared.Persistence
{
    /// <summary>Contains the fields required to create a new character.</summary>
    public struct NewCharacterData
    {
        /// <summary>Gets or sets the character name.</summary>
        public string Name;
        /// <summary>Gets or sets the character class identifier.</summary>
        public string ClassID;
        /// <summary>Gets or sets the character race identifier.</summary>
        public string RaceID;
    }
}
