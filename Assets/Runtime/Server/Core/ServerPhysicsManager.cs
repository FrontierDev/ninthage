using System;
using Game.Shared.Utility;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>
    /// Owns continuous server physics and NavMesh mechanics for the active runtime context.
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

            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                throw new InvalidOperationException("Server physics manager requires initialized server world manager.");

            if (!ServerWorldManager.Instance.CurrentRuntimeContext.IsValid)
                throw new InvalidOperationException("Server physics manager requires a valid server runtime context.");

            _instance = this;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                DisposeContinuousNavigation();
                _instance = null;
                _initialized = false;
            }
        }

        /// <summary>Converts a canonical world position directly into the continuous Unity frame.</summary>
        internal Vector3 ToPhysicalPosition(WorldPosition worldPosition)
        {
            EnsureInitialized();
            if (!worldPosition.IsFinite)
                throw new ArgumentException("The world position must be finite.", nameof(worldPosition));
            if (!FitsContinuousFloat(worldPosition.x)
                || !FitsContinuousFloat(worldPosition.y)
                || !FitsContinuousFloat(worldPosition.z))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldPosition),
                    "The world position must fit within finite Unity float coordinates.");
            }

            return new Vector3(
                (float)worldPosition.x,
                (float)worldPosition.y,
                (float)worldPosition.z);
        }

        /// <summary>Reconstructs a canonical world position from the continuous Unity frame.</summary>
        internal WorldPosition ToWorldPosition(Vector3 physicalPosition)
        {
            EnsureInitialized();
            if (!IsContinuousFinite(physicalPosition))
                throw new ArgumentException("The physical position must be finite.", nameof(physicalPosition));

            var worldPosition = new WorldPosition(
                physicalPosition.x,
                physicalPosition.y,
                physicalPosition.z);
            if (!worldPosition.IsFinite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicalPosition),
                    "The reconstructed world position must be finite.");
            }

            return worldPosition;
        }

        private static bool IsContinuousFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static bool FitsContinuousFloat(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && value >= -float.MaxValue && value <= float.MaxValue;
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
