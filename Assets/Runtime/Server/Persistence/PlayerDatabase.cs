using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Persistence;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Loads, migrates, and persists server player account data.</summary>
    public static class PlayerDatabase
    {
        private const int CurrentDataVersion = 1;
        private static readonly string _filePath;
        private static Dictionary<string, PlayerAccountData> playerAccounts = new();

        static PlayerDatabase()
        {
            _filePath = Path.Combine(Application.persistentDataPath, Game.Shared.GameConfigurationManager.Config.PlayerDataFilePath);
            LoadFromDisk();
        }

        /// <summary>Clears all accounts and writes an empty database to disk.</summary>
        public static void CleanDatabase()
        {
            playerAccounts.Clear();
            SaveToDisk();
            Debug.Warning("Player database has been cleared. This should only be used for testing purposes.");
        }

        /// <summary>Attempts to find an account by username.</summary>
        /// <param name="username">The account username.</param>
        /// <param name="accountData">Receives the account when found.</param>
        /// <returns><see langword="true"/> when an account exists for the username.</returns>
        public static bool TryGetPlayerAccount(string username, out PlayerAccountData accountData)
        {
            return playerAccounts.TryGetValue(username, out accountData);
        }

        /// <summary>Saves an account to the player database.</summary>
        /// <param name="accountData">The account data to save.</param>
        public static void SavePlayerAccount(PlayerAccountData accountData)
        {
            playerAccounts[accountData.Username] = accountData;
            SaveToDisk();
        }

        private static void LoadFromDisk()
        {
            if (!File.Exists(_filePath))
            {
                Debug.Warning($" - No player data file found at {_filePath}. Starting fresh.");
                return;
            }

            var json = File.ReadAllText(_filePath);
            var version = JsonUtility.FromJson<PersistenceFileVersion>(json);
            if (version == null || version.DataVersion < 0)
            {
                Debug.Error($" - Player data file at {_filePath} has an invalid persistence version.");
                return;
            }

            PlayerDatabaseWrapper wrapper;
            if (version.DataVersion == 0)
            {
                wrapper = MigrateLegacyData(JsonUtility.FromJson<LegacyPlayerDatabaseWrapper>(json));
            }
            else if (version.DataVersion == CurrentDataVersion)
            {
                wrapper = JsonUtility.FromJson<PlayerDatabaseWrapper>(json);
            }
            else
            {
                Debug.Error($" - Player data file at {_filePath} uses unsupported version {version.DataVersion}; expected {CurrentDataVersion}.");
                return;
            }

            if (wrapper?.Accounts != null)
            {
                playerAccounts.Clear();
                foreach (var account in wrapper.Accounts)
                    playerAccounts[account.Username] = account;
            }

            Debug.Log($" - Loaded {playerAccounts.Count} player accounts from {_filePath}.");
        }

        private static void SaveToDisk()
        {
            var wrapper = new PlayerDatabaseWrapper();
            wrapper.DataVersion = CurrentDataVersion;
            wrapper.Accounts = new List<PlayerAccountData>(playerAccounts.Values);
            var json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(_filePath, json);
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
                DataVersion = CurrentDataVersion,
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
