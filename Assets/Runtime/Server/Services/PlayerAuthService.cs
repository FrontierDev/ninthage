using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Game.Shared.Authentication;
using Debug = Game.Shared.FormattedDebug;
using PlayerDatabase = Game.Server.Persistence.PlayerDatabase;

namespace Game.Server.Persistence
{
    /// <summary>
    /// Server-side implementation of IPlayerAuthService.
    /// Validates credentials against the PlayerDatabase, creating new accounts as needed.
    /// Owns all server-side session state: pending connections and validated queues.
    /// Registered with LoginAuthenticator during server initialization (Step 7).
    /// </summary>
    public class PlayerAuthService : IPlayerAuthService
    {
        private readonly Queue<string> _validatedUsernamesQueue = new Queue<string>();
        private readonly Queue<bool> _newPlayerQueue = new Queue<bool>();
        private readonly Dictionary<object, string> _pendingConnections = new Dictionary<object, string>();

        /// <summary>
        /// Validates the supplied credentials against the player database using PBKDF2-SHA256.
        /// Creates a new account if the username does not yet exist - this should be replaced for production.
        /// </summary>
        public bool ValidateCredentials(string username, string password, out bool isNewPlayer)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                isNewPlayer = false;
                return false;
            }

            if (PlayerDatabase.TryGetPlayerAccount(username, out var accountData))
            {
                isNewPlayer = false;
                bool passwordMatch = VerifyPassword(password, accountData.PasswordHash, accountData.PasswordSalt);
                if (!passwordMatch)
                    Debug.Warning($"Password mismatch for existing player: {username}");
                return passwordMatch;
            }
            else
            {
                isNewPlayer = true;

                var salt = GenerateSalt();
                var hash = HashPassword(password, salt);

                PlayerDatabase.SavePlayerAccount(new PlayerAccountData
                {
                    Username = username,
                    PasswordHash = hash,
                    PasswordSalt = salt
                });

                Debug.Warning($"New player account created for: {username}. Player account creation should not be handled by the authentication service in a production implementation.");
                return true;
            }
        }

        public void TrackPendingConnection(object connection, string username)
        {
            _pendingConnections[connection] = username;
            _validatedUsernamesQueue.Enqueue(username);
        }

        public bool TryRemovePendingConnection(object connection, out string username)
        {
            if (_pendingConnections.TryGetValue(connection, out username))
            {
                _pendingConnections.Remove(connection);
                return true;
            }
            return false;
        }

        public string GetNextValidatedUsername()
        {
            return _validatedUsernamesQueue.Count > 0 ? _validatedUsernamesQueue.Dequeue() : "Unknown";
        }

        public bool GetNextPlayerIsNew()
        {
            return _newPlayerQueue.Count > 0 && _newPlayerQueue.Dequeue();
        }

        public void EnqueueNewPlayerFlag(bool isNewPlayer)
        {
            _newPlayerQueue.Enqueue(isNewPlayer);
        }

        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        private static string GenerateSalt()
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        private static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(pbkdf2.GetBytes(HashSize));
        }

        private static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var computedHash = HashPassword(password, storedSalt);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedHash),
                Convert.FromBase64String(storedHash));
        }
    }
}
