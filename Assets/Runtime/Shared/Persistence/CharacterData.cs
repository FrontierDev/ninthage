using System;
using System.Collections.Generic;
using PurrNet;

namespace Game.Shared.Persistence
{
    /// <summary>Persisted character state shared by client and server.</summary>
    [System.Serializable]
    public sealed class CharacterData
    {
        /// <summary>Gets or sets the character name.</summary>
        public string Name;
        /// <summary>Gets or sets the character title.</summary>
        public string Title;
        /// <summary>Gets or sets the character GUID.</summary>
        public string GUID;
        /// <summary>Gets or sets the character level.</summary>
        public int Level;
        /// <summary>Gets or sets the character experience.</summary>
        public int Experience;
        /// <summary>Gets or sets the class identifier.</summary>
        public string ClassID;
        /// <summary>Gets or sets the race identifier.</summary>
        public string RaceID;
        /// <summary>Gets or sets the player-versus-player faction identifier.</summary>
        public string PVPFactionID;
        /// <summary>Gets or sets known spell identifiers.</summary>
        public List<string> KnownSpellIDs = new();
        /// <summary>Gets or sets saved inventory entries.</summary>
        public List<SavedInventoryEntry> SavedInventory = new();
        /// <summary>Gets or sets saved equipment entries.</summary>
        public List<SavedEquipmentEntry> SavedEquipment = new();
        /// <summary>Gets or sets saved weapon skills.</summary>
        public List<SavedWeaponSkill> SavedWeaponSkills = new();
        /// <summary>Gets or sets saved talents.</summary>
        public List<SavedTalentEntry> SavedTalents = new();
        /// <summary>Gets or sets saved reputations.</summary>
        public List<SavedReputationEntry> SavedReputations = new();
        /// <summary>Gets or sets saved quests.</summary>
        public List<SavedQuestEntry> SavedQuests = new();

        /// <summary>
        /// Persisted resource current values (health, mana, etc.).
        /// Keyed by stat definition ID.
        /// Excluded from PurrNet serialization — this is server-only persistence data.
        /// </summary>
        [DontPack]
        public List<SavedResourceEntry> SavedResources = new();

        /// <summary>Initializes persisted character state with all saved identity values.</summary>
        public CharacterData(string guid, string name, int level, int experience, string title, string classID, string raceID, string pvpFactionID)
        {
            GUID = guid;
            Name = name;
            Title = title;
            Level = level;
            PVPFactionID = pvpFactionID;
            Experience = experience;
            ClassID = classID;
            RaceID = raceID;
        }

        /// <summary>Initializes a new character with its starting identity values.</summary>
        public CharacterData(string name, int level, string classID, string raceID, string pvpFactionID)
        {
            Name = name;
            ClassID = classID;
            RaceID = raceID;
            Level = level;
            PVPFactionID = pvpFactionID;

            if (string.IsNullOrEmpty(PVPFactionID))
            {
                var def = Game.Shared.Data.RaceDefinitionLibrary.Instance.GetDefinition(RaceID).DefaultPVPFaction;
                PVPFactionID = def != null ? def.DefinitionID : null;
            }

            // Generate GUID
            GUID = System.Guid.NewGuid().ToString();
        }
    }

    [System.Serializable]
    /// <summary>Stores a persisted resource value.</summary>
    public struct SavedResourceEntry
    {
        /// <summary>Gets or sets the resource stat identifier.</summary>
        public string StatID;
        /// <summary>Gets or sets the current resource value.</summary>
        public float Current;
    }

    [System.Serializable]
    /// <summary>Stores a persisted inventory entry.</summary>
    public struct SavedInventoryEntry
    {
        /// <summary>Gets or sets the inventory slot index.</summary>
        public int SlotIndex;
        /// <summary>Gets or sets the item GUID.</summary>
        public string ItemGUID;
        /// <summary>Gets or sets the item identifier.</summary>
        public string ItemID;
        /// <summary>Gets or sets the stack size.</summary>
        public int StackSize;
        /// <summary>Gets or sets the durability.</summary>
        public int Durability;
        /// <summary>Gets or sets item modifications.</summary>
        public List<string> Modifications;
    }

    [System.Serializable]
    /// <summary>Stores a persisted equipment entry.</summary>
    public struct SavedEquipmentEntry
    {
        public int SlotIndex;
        public string ItemGUID;
        public string ItemID;
        public int StackSize;
        public int Durability;
        public List<string> Modifications;
    }

    [System.Serializable]
    /// <summary>Stores a persisted weapon skill.</summary>
    public struct SavedWeaponSkill
    {
        public int WeaponType;
        public int Level;
        public int Experience;
    }

    [System.Serializable]
    /// <summary>Stores a persisted talent entry.</summary>
    public struct SavedTalentEntry
    {
        public string TalentID;
        public int Rank;
    }

    [System.Serializable]
    /// <summary>Stores a persisted reputation entry.</summary>
    public struct SavedReputationEntry
    {
        public string FactionID;
        public int Reputation;
    }

    [System.Serializable]
    /// <summary>Stores a persisted quest entry.</summary>
    public struct SavedQuestEntry
    {
        public string QuestID;
        public int[] Progress;
        public bool IsCompleted;
    }
}
