using Game.Shared.Persistence;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Legacy player-data serializer retained for the explicit Stage-1.7 Step-13 importer. It is not runtime character/account authority and performs no filesystem persistence.</summary>
    public static class PlayerDatabase
    {
        private const int CURRENT_DATA_VERSION = 1;

        /// <summary>Deserializes versioned player data and applies legacy migration without filesystem access.</summary>
        /// <param name="json">The versioned player database JSON.</param>
        /// <param name="accounts">Receives the deserialized accounts when the version is supported.</param>
        /// <returns><see langword="true"/> when the JSON uses a supported persistence version.</returns>
        public static bool TryDeserialize(string json, out List<PlayerAccountData> accounts)
        {
            accounts = null;

            var version = JsonUtility.FromJson<PersistenceFileVersion>(json);
            if (version == null || version.DataVersion < 0)
                return false;

            PlayerDatabaseWrapper wrapper;
            if (version.DataVersion == 0)
                wrapper = MigrateLegacyData(JsonUtility.FromJson<LegacyPlayerDatabaseWrapper>(json));
            else if (version.DataVersion == CURRENT_DATA_VERSION)
                wrapper = JsonUtility.FromJson<PlayerDatabaseWrapper>(json);
            else
                return false;

            if (wrapper != null)
                accounts = wrapper.Accounts;

            return true;
        }

        /// <summary>Serializes player accounts using the current persistence version without filesystem access.</summary>
        /// <param name="accounts">The player accounts to serialize.</param>
        /// <returns>Current-version player database JSON.</returns>
        public static string Serialize(IEnumerable<PlayerAccountData> accounts)
        {
            var wrapper = new PlayerDatabaseWrapper
            {
                DataVersion = CURRENT_DATA_VERSION,
                Accounts = new List<PlayerAccountData>(accounts)
            };

            return JsonUtility.ToJson(wrapper, true);
        }

        [System.Serializable]
        private class PlayerDatabaseWrapper
        {
            public int DataVersion;
            public List<PlayerAccountData> Accounts = new();
        }

        #pragma warning disable 0649

        [System.Serializable]
        private class PersistenceFileVersion
        {
            public int DataVersion;
        }

        private static PlayerDatabaseWrapper MigrateLegacyData(LegacyPlayerDatabaseWrapper legacyWrapper)
        {
            if (legacyWrapper == null)
                return null;

            var migratedWrapper = new PlayerDatabaseWrapper
            {
                DataVersion = CURRENT_DATA_VERSION,
                Accounts = new()
            };

            if (legacyWrapper.Accounts == null)
                return migratedWrapper;

            foreach (var legacyAccount in legacyWrapper.Accounts)
            {
                if (legacyAccount == null)
                {
                    migratedWrapper.Accounts.Add(null);
                    continue;
                }

                var account = new PlayerAccountData
                {
                    Username = legacyAccount.Username,
                    PasswordHash = legacyAccount.PasswordHash,
                    PasswordSalt = legacyAccount.PasswordSalt,
                    Characters = new()
                };

                if (legacyAccount.Characters != null)
                {
                    foreach (var legacyCharacter in legacyAccount.Characters)
                    {
                        account.Characters.Add(legacyCharacter == null ? null : MigrateLegacyCharacter(legacyCharacter));
                    }
                }

                migratedWrapper.Accounts.Add(account);
            }

            return migratedWrapper;
        }

        private static CharacterData MigrateLegacyCharacter(LegacyCharacterData legacyCharacter)
        {
            var character = new CharacterData(
                legacyCharacter.Guid,
                legacyCharacter.Name,
                legacyCharacter.Level,
                legacyCharacter.Experience,
                legacyCharacter.Title,
                legacyCharacter.ClassID,
                legacyCharacter.RaceID,
                legacyCharacter.PvPFactionID)
            {
                KnownSpellIDs = legacyCharacter.KnownSpellIDs,
                SavedInventory = MigrateLegacyInventory(legacyCharacter.SavedInventory),
                SavedEquipment = MigrateLegacyEquipment(legacyCharacter.SavedEquipment),
                SavedWeaponSkills = legacyCharacter.SavedWeaponSkills,
                SavedTalents = legacyCharacter.SavedTalents,
                SavedReputations = legacyCharacter.SavedReputations,
                SavedQuests = legacyCharacter.SavedQuests,
                SavedResources = MigrateLegacyResources(legacyCharacter.SavedResources)
            };

            return character;
        }

        private static List<SavedInventoryEntry> MigrateLegacyInventory(List<LegacySavedInventoryEntry> entries)
        {
            if (entries == null)
                return null;

            var migratedEntries = new List<SavedInventoryEntry>(entries.Count);
            foreach (var entry in entries)
            {
                migratedEntries.Add(new SavedInventoryEntry
                {
                    SlotIndex = entry.SlotIndex,
                    ItemGUID = entry.ItemGuid,
                    ItemID = entry.ItemId,
                    StackSize = entry.StackSize,
                    Durability = entry.Durability,
                    Modifications = entry.Modifications
                });
            }

            return migratedEntries;
        }

        private static List<SavedEquipmentEntry> MigrateLegacyEquipment(List<LegacySavedEquipmentEntry> entries)
        {
            if (entries == null)
                return null;

            var migratedEntries = new List<SavedEquipmentEntry>(entries.Count);
            foreach (var entry in entries)
            {
                migratedEntries.Add(new SavedEquipmentEntry
                {
                    SlotIndex = entry.SlotIndex,
                    ItemGUID = entry.ItemGuid,
                    ItemID = entry.ItemId,
                    StackSize = entry.StackSize,
                    Durability = entry.Durability,
                    Modifications = entry.Modifications
                });
            }

            return migratedEntries;
        }

        private static List<SavedResourceEntry> MigrateLegacyResources(List<LegacySavedResourceEntry> entries)
        {
            if (entries == null)
                return null;

            var migratedEntries = new List<SavedResourceEntry>(entries.Count);
            foreach (var entry in entries)
            {
                migratedEntries.Add(new SavedResourceEntry
                {
                    StatID = entry.StatId,
                    Current = entry.Current
                });
            }

            return migratedEntries;
        }

        [System.Serializable]
        private class LegacyPlayerDatabaseWrapper
        {
            public List<LegacyPlayerAccountData> Accounts;
        }

        [System.Serializable]
        private class LegacyPlayerAccountData
        {
            public string Username;
            public string PasswordHash;
            public string PasswordSalt;
            public List<LegacyCharacterData> Characters;
        }

        [System.Serializable]
        private class LegacyCharacterData
        {
            public string Name;
            public string Title;
            public string Guid;
            public int Level;
            public int Experience;
            public string ClassID;
            public string RaceID;
            public string PvPFactionID;
            public List<string> KnownSpellIDs;
            public List<LegacySavedInventoryEntry> SavedInventory;
            public List<LegacySavedEquipmentEntry> SavedEquipment;
            public List<SavedWeaponSkill> SavedWeaponSkills;
            public List<SavedTalentEntry> SavedTalents;
            public List<SavedReputationEntry> SavedReputations;
            public List<SavedQuestEntry> SavedQuests;
            public List<LegacySavedResourceEntry> SavedResources;
        }

        [System.Serializable]
        private struct LegacySavedInventoryEntry
        {
            public int SlotIndex;
            public string ItemGuid;
            public string ItemId;
            public int StackSize;
            public int Durability;
            public List<string> Modifications;
        }

        [System.Serializable]
        private struct LegacySavedEquipmentEntry
        {
            public int SlotIndex;
            public string ItemGuid;
            public string ItemId;
            public int StackSize;
            public int Durability;
            public List<string> Modifications;
        }

        [System.Serializable]
        private struct LegacySavedResourceEntry
        {
            public string StatId;
            public float Current;
        }

        #pragma warning restore 0649
    }
}
