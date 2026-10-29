using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Contains field-scoped checkpoint entries for multiple characters.</summary>
        internal sealed class CharacterCheckpointBatchRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("checkpoints")]
            public CheckpointEntry[] Checkpoints { get; set; }

            /// <summary>One independently revisioned field-scoped character checkpoint.</summary>
            internal sealed class CheckpointEntry
            {
                [JsonProperty("accountID")]
                public Guid AccountID { get; set; }

                [JsonProperty("characterID")]
                public Guid CharacterID { get; set; }

                [JsonProperty("characterPersistenceLeaseID")]
                public Guid CharacterPersistenceLeaseID { get; set; }

                [JsonProperty("expectedRevision")]
                public long ExpectedRevision { get; set; }

                [JsonProperty("worldState")]
                public WorldCheckpoint WorldState { get; set; }

                [JsonProperty("resources")]
                public ResourceCheckpoint[] Resources { get; set; }

                [JsonProperty("isDead")]
                public bool? IsDead { get; set; }
            }

            /// <summary>Field-scoped persisted world location for a checkpoint.</summary>
            internal sealed class WorldCheckpoint
            {
                [JsonProperty("mapID")]
                public string MapID { get; set; }

                [JsonProperty("instanceID")]
                public Guid? InstanceID { get; set; }

                [JsonProperty("positionX")]
                public double PositionX { get; set; }

                [JsonProperty("positionY")]
                public double PositionY { get; set; }

                [JsonProperty("positionZ")]
                public double PositionZ { get; set; }

                [JsonProperty("facingX")]
                public float FacingX { get; set; }

                [JsonProperty("facingY")]
                public float FacingY { get; set; }

                [JsonProperty("facingZ")]
                public float FacingZ { get; set; }

                [JsonProperty("facingW")]
                public float FacingW { get; set; }

                [JsonProperty("safeReturnLocationID")]
                public string SafeReturnLocationID { get; set; }

                [JsonProperty("savedAtUtc")]
                public DateTimeOffset SavedAtUtc { get; set; }
            }

            /// <summary>Field-scoped persisted current value for one resource.</summary>
            internal sealed class ResourceCheckpoint
            {
                [JsonProperty("resourceDefinitionID")]
                public string ResourceDefinitionID { get; set; }

                [JsonProperty("currentValue")]
                public float CurrentValue { get; set; }

                [JsonProperty("lastAuthoritativeUpdateUtc")]
                public DateTimeOffset LastAuthoritativeUpdateUtc { get; set; }
            }
        }

        /// <summary>Wire response containing the batch settlement and per-character results.</summary>
        internal sealed class CharacterCheckpointBatchResponse
        {
            [JsonProperty("result")]
            public PersistenceResult Result { get; set; }

            [JsonProperty("checkpoints")]
            public CheckpointResult[] Checkpoints { get; set; }

            /// <summary>Settlement for one character checkpoint entry.</summary>
            internal sealed class CheckpointResult
            {
                [JsonProperty("characterID")]
                public Guid CharacterID { get; set; }

                [JsonProperty("result")]
                public PersistenceResult Result { get; set; }

                [JsonProperty("revision")]
                public long? Revision { get; set; }
            }
        }
    }
}
