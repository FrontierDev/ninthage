using Game.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Stores temporary Stage 1.7 authentication credentials separately from legacy player state.</summary>
    internal static class TemporaryCredentialStore
    {
        private const int CURRENT_DATA_VERSION = 1;
        private const string CREDENTIAL_FILE_NAME = "player_credentials.json";
        private const int SALT_SIZE = 16;
        private const int HASH_SIZE = 32;

        private static Dictionary<string, CredentialRecord> _credentials;
        private static string _credentialFilePath;
        private static bool _initialized;

        /// <summary>Initializes the temporary credential store once for the current server lifetime.</summary>
        internal static void Initialize()
        {
            if (_initialized)
                return;

            _credentialFilePath = Path.Combine(Application.persistentDataPath, CREDENTIAL_FILE_NAME);
            Dictionary<string, CredentialRecord> credentials;
            if (File.Exists(_credentialFilePath))
            {
                credentials = LoadCredentialFile();
            }
            else
            {
                credentials = LoadLegacyCredentials();
                _credentials = credentials;
                SaveToDisk();
            }

            _credentials = credentials;
            _initialized = true;
        }

        /// <summary>Clears temporary credentials and rewrites only the credential store.</summary>
        internal static void Clear()
        {
            EnsureInitialized();

            Dictionary<string, CredentialRecord> previousCredentials = _credentials;
            _credentials = new Dictionary<string, CredentialRecord>(StringComparer.Ordinal);
            try
            {
                SaveToDisk();
            }
            catch
            {
                _credentials = previousCredentials;
                throw;
            }
        }

        /// <summary>Looks up a temporary credential without changing its username.</summary>
        internal static bool TryGetCredential(
            string username,
            out string passwordHash,
            out string passwordSalt,
            out bool masterProvisioningPending)
        {
            EnsureInitialized();
            passwordHash = null;
            passwordSalt = null;
            masterProvisioningPending = false;

            if (string.IsNullOrEmpty(username)
                || !_credentials.TryGetValue(username, out CredentialRecord credential))
            {
                return false;
            }

            passwordHash = credential.PasswordHash;
            passwordSalt = credential.PasswordSalt;
            masterProvisioningPending = credential.MasterProvisioningPending;
            return true;
        }

        /// <summary>Creates a pending temporary credential and persists the updated credential store.</summary>
        internal static bool TryCreateCredential(
            string username,
            string passwordHash,
            string passwordSalt)
        {
            EnsureInitialized();
            if (!IsValidCredential(username, passwordHash, passwordSalt)
                || _credentials.ContainsKey(username))
            {
                return false;
            }

            CredentialRecord credential = new()
            {
                Username = username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                MasterProvisioningPending = true
            };
            _credentials.Add(username, credential);
            try
            {
                SaveToDisk();
            }
            catch
            {
                _credentials.Remove(username);
                throw;
            }

            return true;
        }

        /// <summary>Marks an existing temporary credential as provisioned by the Master.</summary>
        internal static bool MarkMasterProvisioned(string username)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(username)
                || !_credentials.TryGetValue(username, out CredentialRecord credential))
            {
                return false;
            }
            if (!credential.MasterProvisioningPending)
                return true;

            credential.MasterProvisioningPending = false;
            try
            {
                SaveToDisk();
            }
            catch
            {
                credential.MasterProvisioningPending = true;
                throw;
            }

            return true;
        }

        /// <summary>Removes an existing temporary credential and persists the updated credential store.</summary>
        internal static bool RemoveCredential(string username)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(username)
                || !_credentials.TryGetValue(username, out CredentialRecord credential))
            {
                return false;
            }

            _credentials.Remove(username);
            try
            {
                SaveToDisk();
            }
            catch
            {
                _credentials.Add(username, credential);
                throw;
            }

            return true;
        }

        private static Dictionary<string, CredentialRecord> LoadCredentialFile()
        {
            CredentialFile file = Deserialize<CredentialFile>(File.ReadAllText(_credentialFilePath));
            if (file == null || file.DataVersion != CURRENT_DATA_VERSION || file.Credentials == null)
            {
                throw new InvalidOperationException(
                    $"The temporary credential file at {_credentialFilePath} has an invalid data version or record list.");
            }

            return ValidateRecords(file.Credentials);
        }

        private static Dictionary<string, CredentialRecord> LoadLegacyCredentials()
        {
            if (GameConfigurationManager.Config == null)
            {
                throw new InvalidOperationException(
                    "Game configuration must be initialized before loading temporary credentials.");
            }

            string legacyPath = Path.Combine(
                Application.persistentDataPath,
                GameConfigurationManager.Config.PlayerDataFilePath);
            if (!File.Exists(legacyPath))
                return new Dictionary<string, CredentialRecord>(StringComparer.Ordinal);

            LegacyCredentialFile file = Deserialize<LegacyCredentialFile>(File.ReadAllText(legacyPath));
            if (file == null || file.DataVersion < 0 || file.DataVersion > CURRENT_DATA_VERSION)
            {
                throw new InvalidOperationException(
                    $"The legacy player data file at {legacyPath} has an unsupported data version.");
            }

            List<CredentialRecord> credentials = new();
            if (file.Accounts != null)
            {
                foreach (LegacyCredentialRecord account in file.Accounts)
                {
                    if (account == null)
                    {
                        throw new InvalidOperationException(
                            $"The legacy player data file at {legacyPath} contains an invalid credential record.");
                    }

                    credentials.Add(new CredentialRecord
                    {
                        Username = account.Username,
                        PasswordHash = account.PasswordHash,
                        PasswordSalt = account.PasswordSalt,
                        MasterProvisioningPending = false
                    });
                }
            }

            return ValidateRecords(credentials);
        }

        private static Dictionary<string, CredentialRecord> ValidateRecords(
            IEnumerable<CredentialRecord> records)
        {
            Dictionary<string, CredentialRecord> credentials = new(StringComparer.Ordinal);
            foreach (CredentialRecord credential in records)
            {
                if (credential == null
                    || !IsValidCredential(credential.Username, credential.PasswordHash, credential.PasswordSalt))
                {
                    throw new InvalidOperationException("The credential store contains an invalid credential record.");
                }
                if (!credentials.TryAdd(credential.Username, credential))
                {
                    throw new InvalidOperationException(
                        "The credential store contains duplicate usernames.");
                }
            }

            return credentials;
        }

        private static bool IsValidCredential(
            string username,
            string passwordHash,
            string passwordSalt)
        {
            return !string.IsNullOrEmpty(username)
                && IsValidBase64(passwordSalt, SALT_SIZE)
                && IsValidBase64(passwordHash, HASH_SIZE);
        }

        private static bool IsValidBase64(string value, int expectedByteCount)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                return Convert.FromBase64String(value).Length == expectedByteCount;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void SaveToDisk()
        {
            CredentialFile file = new()
            {
                DataVersion = CURRENT_DATA_VERSION,
                Credentials = new List<CredentialRecord>(_credentials.Values)
            };
            File.WriteAllText(_credentialFilePath, JsonUtility.ToJson(file, true));
        }

        private static T Deserialize<T>(string json)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The credential file contains malformed JSON.", exception);
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("The temporary credential store has not been initialized.");
        }

        [Serializable]
        private class CredentialFile
        {
            public int DataVersion;
            public List<CredentialRecord> Credentials;
        }

        [Serializable]
        private class CredentialRecord
        {
            public string Username;
            public string PasswordHash;
            public string PasswordSalt;
            public bool MasterProvisioningPending;
        }

        #pragma warning disable 0649

        [Serializable]
        private class LegacyCredentialFile
        {
            public int DataVersion;
            public List<LegacyCredentialRecord> Accounts;
        }

        [Serializable]
        private class LegacyCredentialRecord
        {
            public string Username;
            public string PasswordHash;
            public string PasswordSalt;
        }

        #pragma warning restore 0649
    }
}
