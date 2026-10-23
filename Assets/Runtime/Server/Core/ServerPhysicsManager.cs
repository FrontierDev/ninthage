using System;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>
    /// Owns process-level server simulation-region physical state.
    /// Region functionality is introduced by subsequent Stage 1.2 work.
    /// </summary>
    public sealed partial class ServerPhysicsManager : MonoBehaviour
    {
        private static ServerPhysicsManager _instance;
        private static bool _initialized;

        /// <summary>Gets the active server physics manager.</summary>
        public static ServerPhysicsManager Instance => _instance;

        /// <summary>Gets whether the server physics manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerPhysicsManager is already initialized.");

            if (!Game.Shared.GameConfigurationManager.IsInitialized
                || Game.Shared.GameConfigurationManager.Config == null)
            {
                throw new InvalidOperationException(
                    "Server physics manager requires initialized game configuration.");
            }

            // Validate the simulation region configuration from the game configuration.
            Game.Shared.GameConfigurationManager.Config.ValidateSimulationRegionConfiguration();

            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                throw new InvalidOperationException("Server physics manager requires initialized server world manager.");

            if (!ServerWorldManager.Instance.CurrentRuntimeContext.IsValid)
                throw new InvalidOperationException("Server physics manager requires a valid server runtime context.");

            if (!ServerPositionManager.IsInitialized || ServerPositionManager.Instance == null)
                throw new InvalidOperationException("Server physics manager requires initialized server position manager.");

            _instance = this;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _environmentRegionBindings.Clear();
                _activeRegions.Clear();
                _instance = null;
                _initialized = false;
            }
        }

        private void EnsureInitialized()
        {
            if (!_initialized || _instance != this)
            {
                throw new InvalidOperationException(
                    "ServerPhysicsManager is not initialized.");
            }
        }
    }
}
