using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Requests graceful release of character persistence ownership.</summary>
        internal sealed class CharacterReleaseRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("accountID")]
            public Guid AccountID { get; set; }

            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("characterPersistenceLeaseID")]
            public Guid CharacterPersistenceLeaseID { get; set; }

            [JsonProperty("expectedRevision")]
            public long ExpectedRevision { get; set; }
        }
    }
}
