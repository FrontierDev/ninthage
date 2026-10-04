using System;
using System.Collections.Generic;
using PurrNet;

namespace Game.Shared.Persistence
{
    [System.Serializable]
    public sealed class CharacterData
    {
        public string Name;
        public string Title;
        public string Guid;
        public int Level;
        public int Experience;
        public string ClassID;
        public string RaceID;
        public string PvPFactionID;
        public List<string> KnownSpellIDs = new List<string>();
        public List<SavedInventoryEntry> SavedInventory = new List<SavedInventoryEntry>();
        public List<SavedEquipmentEntry> SavedEquipment = new List<SavedEquipmentEntry>();
        public List<SavedWeaponSkill> SavedWeaponSkills = new List<SavedWeaponSkill>();
        public List<SavedTalentEntry> SavedTalents = new List<SavedTalentEntry>();
        public List<SavedReputationEntry> SavedReputations = new List<SavedReputationEntry>();
        public List<SavedQuestEntry> SavedQuests = new List<SavedQuestEntry>();

        /// <summary>
        /// Persisted resource current values (health, mana, etc.).
        /// Keyed by stat definition ID.
        /// Excluded from PurrNet serialization — this is server-only persistence data.
        /// </summary>
        [DontPack]
        public List<SavedResourceEntry> SavedResources = new List<SavedResourceEntry>();

        public CharacterData(string guid, string name, int level, int experience, string title, string classID, string raceID, string pvpFactionID)
        {
            Guid = guid;
            Name = name;
            Title = title;
            Level = level;
            PvPFactionID = pvpFactionID;
            Experience = experience;
            ClassID = classID;
            RaceID = raceID;
        }

        public CharacterData(string name, int level, string classID, string raceID, string pvpFactionID)
        {
            Name = name;
            ClassID = classID;
            RaceID = raceID;
            Level = level;
            PvPFactionID = pvpFactionID;

            if (string.IsNullOrEmpty(PvPFactionID))
            {
                var def = Game.Shared.Data.RaceDefinitionLibrary.Instance.GetDefinition(RaceID).DefaultPVPFaction;
                PvPFactionID = def != null ? def.DefinitionId : null;
            }

            // Generate GUID
            Guid = System.Guid.NewGuid().ToString();
        }
    }

    [System.Serializable]
    public struct SavedResourceEntry
    {
        public string StatId;
        public float Current;
    }

    [System.Serializable]
    public struct SavedInventoryEntry
    {
        public int SlotIndex;
        public string ItemGuid;
        public string ItemId;
        public int StackSize;
        public int Durability;
        public List<string> Modifications;
    }

    [System.Serializable]
    public struct SavedEquipmentEntry
    {
        public int SlotIndex;
        public string ItemGuid;
        public string ItemId;
        public int StackSize;
        public int Durability;
        public List<string> Modifications;
    }

    [System.Serializable]
    public struct SavedWeaponSkill
    {
        public int WeaponType;
        public int Level;
        public int Experience;
    }

    [System.Serializable]
    public struct SavedTalentEntry
    {
        public string TalentID;
        public int Rank;
    }

    [System.Serializable]
    public struct SavedReputationEntry
    {
        public string FactionID;
        public int Reputation;
    }

    [System.Serializable]
    public struct SavedQuestEntry
    {
        public string QuestID;
        public int[] Progress;
        public bool IsCompleted;
    }
}