using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    internal static partial class MasterPersistenceProtocol
    {
        /// <summary>Identifies the durable location relation for a character item.</summary>
        internal enum CharacterItemLocationKind : short
        {
            Inventory = 0,
            Equipment = 1,
        }

        /// <summary>Transport snapshot of the durable character state returned by the Master.</summary>
        internal sealed class CharacterPersistenceState
        {
            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

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

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("experience")]
            public int Experience { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("createdAtUtc")]
            public DateTimeOffset CreatedAtUtc { get; set; }

            [JsonProperty("revision")]
            public long Revision { get; set; }

            [JsonProperty("worldState")]
            public CharacterWorldState WorldState { get; set; }

            [JsonProperty("isDead")]
            public bool IsDead { get; set; }

            [JsonProperty("resources")]
            public CharacterResourceState[] Resources { get; set; }

            [JsonProperty("cooldowns")]
            public CharacterCooldownState[] Cooldowns { get; set; }

            [JsonProperty("persistentAuras")]
            public CharacterPersistentAuraState[] PersistentAuras { get; set; }

            [JsonProperty("abilities")]
            public CharacterAbilityState[] Abilities { get; set; }

            [JsonProperty("talents")]
            public CharacterTalentState[] Talents { get; set; }

            [JsonProperty("weaponSkills")]
            public CharacterWeaponSkillState[] WeaponSkills { get; set; }

            [JsonProperty("reputations")]
            public CharacterReputationState[] Reputations { get; set; }

            [JsonProperty("quests")]
            public CharacterQuestState[] Quests { get; set; }

            [JsonProperty("items")]
            public CharacterItemState[] Items { get; set; }
        }

        /// <summary>Persisted world location and recovery metadata for a character.</summary>
        internal sealed class CharacterWorldState
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

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

        /// <summary>Persisted current value for one character resource.</summary>
        internal sealed class CharacterResourceState
        {
            [JsonProperty("resourceDefinitionID")]
            public string ResourceDefinitionID { get; set; }

            [JsonProperty("currentValue")]
            public float CurrentValue { get; set; }

            [JsonProperty("lastAuthoritativeUpdateUtc")]
            public DateTimeOffset LastAuthoritativeUpdateUtc { get; set; }
        }

        /// <summary>Persisted expiry state for one character cooldown.</summary>
        internal sealed class CharacterCooldownState
        {
            [JsonProperty("cooldownDefinitionID")]
            public string CooldownDefinitionID { get; set; }

            [JsonProperty("chargeIndex")]
            public int ChargeIndex { get; set; }

            [JsonProperty("expiresAtUtc")]
            public DateTimeOffset ExpiresAtUtc { get; set; }
        }

        /// <summary>Persisted state for one character aura that survives actor lifetime.</summary>
        internal sealed class CharacterPersistentAuraState
        {
            [JsonProperty("persistentAuraID")]
            public Guid PersistentAuraID { get; set; }

            [JsonProperty("auraDefinitionID")]
            public string AuraDefinitionID { get; set; }

            [JsonProperty("sourceActorID")]
            public Guid? SourceActorID { get; set; }

            [JsonProperty("stacks")]
            public int Stacks { get; set; }

            [JsonProperty("expiresAtUtc")]
            public DateTimeOffset? ExpiresAtUtc { get; set; }

            [JsonProperty("nextTickAtUtc")]
            public DateTimeOffset? NextTickAtUtc { get; set; }

            [JsonProperty("tickIntervalSeconds")]
            public float? TickIntervalSeconds { get; set; }
        }

        /// <summary>Persisted learned rank for one character ability.</summary>
        internal sealed class CharacterAbilityState
        {
            [JsonProperty("abilityDefinitionID")]
            public string AbilityDefinitionID { get; set; }

            [JsonProperty("rank")]
            public int Rank { get; set; }
        }

        /// <summary>Persisted learned rank for one character talent.</summary>
        internal sealed class CharacterTalentState
        {
            [JsonProperty("talentDefinitionID")]
            public string TalentDefinitionID { get; set; }

            [JsonProperty("rank")]
            public int Rank { get; set; }
        }

        /// <summary>Persisted level and experience for one character weapon skill.</summary>
        internal sealed class CharacterWeaponSkillState
        {
            [JsonProperty("weaponType")]
            public int WeaponType { get; set; }

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("experience")]
            public int Experience { get; set; }
        }

        /// <summary>Persisted reputation value for one character faction.</summary>
        internal sealed class CharacterReputationState
        {
            [JsonProperty("factionDefinitionID")]
            public string FactionDefinitionID { get; set; }

            [JsonProperty("reputation")]
            public int Reputation { get; set; }
        }

        /// <summary>Persisted progress for one character quest.</summary>
        internal sealed class CharacterQuestState
        {
            [JsonProperty("questDefinitionID")]
            public string QuestDefinitionID { get; set; }

            [JsonProperty("progress")]
            public int[] Progress { get; set; }

            [JsonProperty("isCompleted")]
            public bool IsCompleted { get; set; }
        }

        /// <summary>Persisted item instance together with its authoritative character location.</summary>
        internal sealed class CharacterItemState
        {
            [JsonProperty("itemInstanceID")]
            public Guid ItemInstanceID { get; set; }

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
}
