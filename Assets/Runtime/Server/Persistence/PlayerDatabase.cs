using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.Persistence
{
    public static class PlayerDatabase
    {
        private static readonly string _filePath;
        private static Dictionary<string, PlayerAccountData> playerAccounts = new Dictionary<string, PlayerAccountData>();

        static PlayerDatabase()
        {
            _filePath = Path.Combine(Application.persistentDataPath, Game.Shared.GameConfigurationManager.Config.PlayerDataFilePath);
            LoadFromDisk();
        }

        public static void CleanDatabase()
        {
            playerAccounts.Clear();
            SaveToDisk();
            Debug.Warning("Player database has been cleared. This should only be used for testing purposes.");
        }

        public static bool TryGetPlayerAccount(string username, out PlayerAccountData accountData)
        {
            return playerAccounts.TryGetValue(username, out accountData);
        }

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
            var wrapper = JsonUtility.FromJson<PlayerDatabaseWrapper>(json);
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
            wrapper.Accounts = new List<PlayerAccountData>(playerAccounts.Values);
            var json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(_filePath, json);
        }

        [System.Serializable]
        private class PlayerDatabaseWrapper
        {
            public List<PlayerAccountData> Accounts = new List<PlayerAccountData>();
        }
    }
}