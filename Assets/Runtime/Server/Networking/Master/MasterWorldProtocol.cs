using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    /// <summary>Contains the transport-only JSON contract used by the Master World Server.</summary>
    internal static class MasterWorldProtocol
    {
        /// <summary>Canonical outcomes for a World Server registration request.</summary>
        internal enum RegistrationResult
        {
            Accepted = 0,
            InvalidWorldServerRunID = 1,
            InvalidWorldName = 2,
            InvalidBuildIdentity = 3,
            Unauthorized = 4,
            WorldNotFound = 5,
            WorldAlreadyActive = 6,
            StaleWorldServerRun = 7,
        }

        /// <summary>Canonical outcomes for a World Server lease request.</summary>
        internal enum LeaseResult
        {
            Accepted = 0,
            InvalidWorldID = 1,
            InvalidWorldServerRunID = 2,
            Unauthorized = 3,
            WorldNotFound = 4,
            StaleWorldServerRun = 5,
        }

        /// <summary>Wire request for registering a World Server with the Master.</summary>
        internal sealed class RegistrationRequest
        {
            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("worldName")]
            public string WorldName { get; set; }

            [JsonProperty("buildIdentity")]
            public string BuildIdentity { get; set; }
        }

        /// <summary>Wire response returned by a World Server registration request.</summary>
        internal sealed class RegistrationResponse
        {
            [JsonProperty("result")]
            public RegistrationResult Result { get; set; }

            [JsonProperty("worldID")]
            public Guid? WorldID { get; set; }

            [JsonProperty("heartbeatIntervalSeconds")]
            public int? HeartbeatIntervalSeconds { get; set; }

            [JsonProperty("leaseTimeoutSeconds")]
            public int? LeaseTimeoutSeconds { get; set; }
        }

        /// <summary>Wire request for renewing or releasing a World Server lease.</summary>
        internal sealed class LeaseRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }
        }

        /// <summary>Wire response returned by a World Server lease request.</summary>
        internal sealed class LeaseResponse
        {
            [JsonProperty("result")]
            public LeaseResult Result { get; set; }
        }
    }
}
