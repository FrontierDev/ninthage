using System;
using UnityEngine;

namespace Game.Shared.Runtime
{
    /// <summary>Identifies the selected runtime role for the current process/play session.</summary>
    public enum RuntimeRole
    {
        Uninitialized = 0,
        Client = 1,
        Server = 2,
    }

    /// <summary>Provides the bootstrap-owned runtime role authority.</summary>
    public static class Runtime
    {
        private static RuntimeRole _role;

        /// <summary>Gets the selected runtime role for the current process/play session.</summary>
        public static RuntimeRole Role => _role;

        /// <summary>Initializes the runtime role selected by the process bootstrapper.</summary>
        public static void Initialize(RuntimeRole role)
        {
            if (role == RuntimeRole.Uninitialized)
                throw new ArgumentException("The runtime role must be Client or Server.", nameof(role));
            if (_role != RuntimeRole.Uninitialized && _role != role)
                throw new InvalidOperationException($"The runtime role is already initialized as {_role} and cannot be changed to {role}.");

            _role = role;
        }

        /// <summary>Gets whether the selected runtime role is the authoritative server.</summary>
        public static bool IsServer()
        {
            return _role == RuntimeRole.Server;
        }

        /// <summary>Gets whether the selected runtime role is the graphical client.</summary>
        public static bool IsClient()
        {
            return _role == RuntimeRole.Client;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _role = RuntimeRole.Uninitialized;
        }
    }
}
