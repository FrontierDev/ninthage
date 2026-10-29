using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Requests creation of a character and its initial durable state.</summary>
        internal sealed class CharacterCreateRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("accountID")]
            public Guid AccountID { get; set; }

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

            [JsonProperty("worldState")]
            public InitialWorldState WorldState { get; set; }

            [JsonProperty("resources")]
            public InitialResource[] Resources { get; set; }

            [JsonProperty("reputations")]
            public InitialReputation[] Reputations { get; set; }

            [JsonProperty("items")]
            public InitialItem[] Items { get; set; }

            /// <summary>Initial map and position supplied for the new character.</summary>
            internal sealed class InitialWorldState
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
            }

            /// <summary>Initial current value for one character resource.</summary>
            internal sealed class InitialResource
            {
                [JsonProperty("resourceDefinitionID")]
                public string ResourceDefinitionID { get; set; }

                [JsonProperty("currentValue")]
                public float CurrentValue { get; set; }
            }

            /// <summary>Initial reputation value for one character faction.</summary>
            internal sealed class InitialReputation
            {
                [JsonProperty("factionDefinitionID")]
                public string FactionDefinitionID { get; set; }

                [JsonProperty("reputation")]
                public int Reputation { get; set; }
            }

            /// <summary>Initial item state and durable character location.</summary>
            internal sealed class InitialItem
            {
                [JsonProperty("itemDefinitionID")]
                public string ItemDefinitionID { get; set; }

                [JsonProperty("stackSize")]
                public int StackSize { get; set; }

                [JsonProperty("durability")]
                public int? Durability { get; set; }

                [JsonProperty("modificationIDs")]
                public string[] ModificationIDs { get; set; }

                [JsonProperty("locationKind")]
                public CharacterItemLocationKind LocationKind { get; set; }

                [JsonProperty("slotIndex")]
                public int SlotIndex { get; set; }
            }
        }

        /// <summary>Requests soft deletion of an owned character.</summary>
        internal sealed class CharacterDeleteRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("accountID")]
            public Guid AccountID { get; set; }

            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("expectedRevision")]
            public long ExpectedRevision { get; set; }
        }

        /// <summary>Requests restoration of an owned soft-deleted character.</summary>
        internal sealed class CharacterRestoreRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("accountID")]
            public Guid AccountID { get; set; }

            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("expectedRevision")]
            public long ExpectedRevision { get; set; }
        }

        /// <summary>Wire response returned by a character lifecycle mutation.</summary>
        internal sealed class CharacterMutationResponse
        {
            [JsonProperty("result")]
            public PersistenceResult Result { get; set; }

            [JsonProperty("characterID")]
            public Guid? CharacterID { get; set; }

            [JsonProperty("revision")]
            public long? Revision { get; set; }
        }
    }
}
