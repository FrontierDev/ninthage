using System;

namespace Game.Shared.Authentication
{
    /// <summary>
    /// Payload sent from client to server during authentication.
    /// Contains username and password credentials.
    /// </summary>
    [Serializable]
    public class AuthenticationPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>Initializes credentials with the supplied username and password.</summary>
        public AuthenticationPayload(string username, string password)
        {
            Username = username;
            Password = password;
        }

        /// <summary>Initializes an empty authentication payload.</summary>
        public AuthenticationPayload()
        {
            Username = string.Empty;
            Password = string.Empty;
        }
    }
}
