using Game.Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        private readonly Dictionary<SimulationRegionKey, ActiveSimulationRegion> _activeRegions = new();

        /// <summary>Gets the descriptor for an active region without creating a new region.</summary>
        /// <param name="key">The context-qualified simulation-region key.</param>
        /// <param name="descriptor">The active region descriptor when found; otherwise the default value.</param>
        /// <returns><see langword="true"/> when the region is active; otherwise <see langword="false"/>.</returns>
        public bool TryGetActiveRegionDescriptor(
            SimulationRegionKey key,
            out SimulationRegionDescriptor descriptor)
        {
            EnsureInitialized();
            if (_activeRegions.TryGetValue(key, out var activeRegion))
            {
                descriptor = activeRegion.Descriptor;
                return true;
            }

            descriptor = default;
            return false;
        }

        private ActiveSimulationRegion GetOrCreateActiveRegion(SimulationRegionKey key)
        {
            EnsureInitialized();
            if (_activeRegions.TryGetValue(key, out var activeRegion))
                return activeRegion;

            int configuredCapacity = GameConfigurationManager.Config.ServerSimulationRegionCapacity;
            if (_activeRegions.Count >= configuredCapacity)
            {
                throw new InvalidOperationException(
                    $"Cannot activate simulation region for context {key.Context.WorldID}/{key.Context.MapID}/{key.Context.InstanceID} "
                    + $"at coordinate ({key.RegionCoordinate.x}, {key.RegionCoordinate.y}): configured capacity is {configuredCapacity}.");
            }

            int slotIndex = FindAvailableRegionSlot();
            Vector3 localAnchor = GetRegionLocalAnchor(slotIndex);
            var descriptor = CreateRegionDescriptor(key, localAnchor);
            activeRegion = new ActiveSimulationRegion(descriptor, slotIndex);
            _activeRegions.Add(key, activeRegion);
            return activeRegion;
        }

        private int FindAvailableRegionSlot()
        {
            int configuredCapacity = GameConfigurationManager.Config.ServerSimulationRegionCapacity;
            for (int slotIndex = 0; slotIndex < configuredCapacity; slotIndex++)
            {
                bool occupied = false;
                foreach (var activeRegion in _activeRegions.Values)
                {
                    if (activeRegion.SlotIndex == slotIndex)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                    return slotIndex;
            }

            throw new InvalidOperationException(
                $"Simulation-region registry state is inconsistent: no available slot exists below configured capacity {configuredCapacity}.");
        }

        private Vector3 GetRegionLocalAnchor(int slotIndex)
        {
            double regionWidth = (double)GameConfigurationManager.Config.ServerSimulationRegionTilesX
                * GameConfigurationManager.Config.ChunkSize;
            double anchorPitchX = regionWidth * 2d;
            double anchorX = slotIndex * anchorPitchX;
            if (!FitsFiniteFloat(anchorX))
            {
                throw new InvalidOperationException(
                    $"Simulation-region slot {slotIndex} produces a local anchor outside the finite Unity float range.");
            }

            return new Vector3((float)anchorX, 0f, 0f);
        }

        private void RetireActiveRegion(SimulationRegionKey key)
        {
            if (!_activeRegions.TryGetValue(key, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Cannot retire missing active simulation region {key}.");
            }
            if (activeRegion.BoundEnvironmentTiles.Count != 0
                || activeRegion.BoundActors.Count != 0)
            {
                throw new InvalidOperationException(
                    $"Cannot retire active simulation region {key} while environment tiles or actors remain bound.");
            }

            _activeRegions.Remove(key);
        }

        private sealed class ActiveSimulationRegion
        {
            internal ActiveSimulationRegion(
                SimulationRegionDescriptor descriptor,
                int slotIndex)
            {
                Descriptor = descriptor;
                SlotIndex = slotIndex;
            }

            internal readonly SimulationRegionDescriptor Descriptor;
            internal readonly int SlotIndex;
            public readonly HashSet<WorldChunkKey> BoundEnvironmentTiles = new();
            internal readonly HashSet<Guid> BoundActors = new();
        }
    }
}
