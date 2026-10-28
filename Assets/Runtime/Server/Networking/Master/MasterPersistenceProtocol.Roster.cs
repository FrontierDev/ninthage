using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Requests the active and soft-deleted roster for one account.</summary>
        internal sealed class CharacterRosterRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("accountID")]
            public Guid AccountID { get; set; }
        }

        /// <summary>Detached character-selection state returned in a roster.</summary>
        internal sealed class CharacterRosterEntry
        {
            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("firstName")]
            public string FirstName { get; set; }

            [JsonProperty("lastName")]
            public string LastName { get; set; }

            [JsonProperty("raceDefinitionID")]
            public string RaceDefinitionID { get; set; }

            [JsonProperty("classDefinitionID")]
            public string ClassDefinitionID { get; set; }

            [JsonProperty("factionDefinitionID")]
            public string FactionDefinitionID { get; set; }

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("experience")]
            public int Experience { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("createdAtUtc")]
            public DateTimeOffset CreatedAtUtc { get; set; }

            [JsonProperty("deletedAtUtc")]
            public DateTimeOffset? DeletedAtUtc { get; set; }

            [JsonProperty("revision")]
            public long Revision { get; set; }
        }

        /// <summary>Wire response containing the active and soft-deleted character roster.</summary>
        internal sealed class CharacterRosterResponse
        {
            [JsonProperty("result")]
            public PersistenceResult Result { get; set; }

            [JsonProperty("accountID")]
            public Guid? AccountID { get; set; }

            [JsonProperty("accountRevision")]
            public long? AccountRevision { get; set; }

            [JsonProperty("activeCharacters")]
            public CharacterRosterEntry[] ActiveCharacters { get; set; }

            [JsonProperty("deletedCharacters")]
            public CharacterRosterEntry[] DeletedCharacters { get; set; }
        }
    }
}
