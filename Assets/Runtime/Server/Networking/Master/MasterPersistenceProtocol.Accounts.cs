using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Requests resolution of a temporary legacy account identity.</summary>
        internal sealed class AccountResolveRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("legacyUsername")]
            public string LegacyUsername { get; set; }
        }

        /// <summary>Requests idempotent creation of a temporary legacy account identity.</summary>
        internal sealed class AccountCreateRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("legacyUsername")]
            public string LegacyUsername { get; set; }
        }

        /// <summary>Wire response containing the Master-owned account identity.</summary>
        internal sealed class AccountIdentityResponse
        {
            [JsonProperty("result")]
            public PersistenceResult Result { get; set; }

            [JsonProperty("accountID")]
            public Guid? AccountID { get; set; }

            [JsonProperty("homeWorldID")]
            public Guid? HomeWorldID { get; set; }

            [JsonProperty("revision")]
            public long? Revision { get; set; }
        }
    }
}
