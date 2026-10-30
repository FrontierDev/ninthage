using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    /// <summary>Contains the transport-only JSON contracts used by the Master persistence API.</summary>
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Canonical outcomes for a persistence request.</summary>
        internal enum PersistenceResult
        {
            Accepted = 0,
            InvalidRequest = 1,
            Unauthorized = 2,
            WorldNotFound = 3,
            WorldNotActive = 4,
            StaleWorldServerRun = 5,
            AccountNotFound = 6,
            AccountAlreadyExists = 7,
            CharacterNotFound = 8,
            CharacterNotOwned = 9,
            CharacterDeleted = 10,
            CharacterLimitReached = 11,
            NameConflict = 12,
            LeaseConflict = 13,
            StaleLease = 14,
            RevisionConflict = 15,
            PersistenceUnavailable = 16,
            InvalidCredentials = 17,
        }

        internal const string CHARACTER_RELEASE_PATH = "/world/v1/characters/release";
        internal const string CHARACTER_CHECKPOINTS_PATH = "/world/v1/characters/checkpoints";

        /// <summary>Wire response returned by a persistence operation without an additional payload.</summary>
        internal sealed class PersistenceResponse
        {
            [JsonProperty("result")]
            public PersistenceResult Result { get; set; }
        }
    }
}
