using System;

namespace Game.Server.Persistence
{
    /// <summary>Immutable server-domain snapshot of checkpointable character state.</summary>
    internal sealed class WorldPersistenceCheckpoint
    {
        private readonly Resource[] _resources;

        /// <summary>Creates a checkpoint snapshot without capturing runtime actor state.</summary>
        internal WorldPersistenceCheckpoint(WorldStateSnapshot worldState, Resource[] resources, bool? isDead)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));

            WorldState = worldState;
            _resources = (Resource[])resources.Clone();
            IsDead = isDead;
        }

        internal WorldStateSnapshot WorldState { get; }
        internal Resource[] Resources => (Resource[])_resources.Clone();
        internal bool? IsDead { get; }

        /// <summary>Optional persisted world location for a checkpoint.</summary>
        internal sealed class WorldStateSnapshot
        {
            internal WorldStateSnapshot(
                string mapID,
                Guid? instanceID,
                double positionX,
                double positionY,
                double positionZ,
                float facingX,
                float facingY,
                float facingZ,
                float facingW,
                string safeReturnLocationID,
                DateTimeOffset savedAtUtc)
            {
                MapID = mapID;
                InstanceID = instanceID;
                PositionX = positionX;
                PositionY = positionY;
                PositionZ = positionZ;
                FacingX = facingX;
                FacingY = facingY;
                FacingZ = facingZ;
                FacingW = facingW;
                SafeReturnLocationID = safeReturnLocationID;
                SavedAtUtc = savedAtUtc;
            }

            internal string MapID { get; }
            internal Guid? InstanceID { get; }
            internal double PositionX { get; }
            internal double PositionY { get; }
            internal double PositionZ { get; }
            internal float FacingX { get; }
            internal float FacingY { get; }
            internal float FacingZ { get; }
            internal float FacingW { get; }
            internal string SafeReturnLocationID { get; }
            internal DateTimeOffset SavedAtUtc { get; }
        }

        /// <summary>Current persisted value for one resource.</summary>
        internal sealed class Resource
        {
            internal Resource(
                string resourceDefinitionID,
                float currentValue,
                DateTimeOffset lastAuthoritativeUpdateUtc)
            {
                ResourceDefinitionID = resourceDefinitionID;
                CurrentValue = currentValue;
                LastAuthoritativeUpdateUtc = lastAuthoritativeUpdateUtc;
            }

            internal string ResourceDefinitionID { get; }
            internal float CurrentValue { get; }
            internal DateTimeOffset LastAuthoritativeUpdateUtc { get; }
        }
    }
}
