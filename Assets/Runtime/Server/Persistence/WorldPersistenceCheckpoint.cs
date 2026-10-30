using System;

namespace Game.Server.Persistence
{
    /// <summary>Immutable server-domain snapshot of checkpointable character state.</summary>
    internal sealed class WorldPersistenceCheckpoint
    {
        private readonly Resource[] _resources;
        private readonly Cooldown[] _cooldowns;
        private readonly PersistentAura[] _persistentAuras;

        /// <summary>Creates a checkpoint snapshot without capturing runtime actor state.</summary>
        internal WorldPersistenceCheckpoint(
            WorldStateSnapshot worldState,
            Resource[] resources,
            bool? isDead,
            Cooldown[] cooldowns = null,
            PersistentAura[] persistentAuras = null)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));

            WorldState = worldState;
            _resources = (Resource[])resources.Clone();
            _cooldowns = cooldowns == null ? null : (Cooldown[])cooldowns.Clone();
            _persistentAuras = persistentAuras == null ? null : (PersistentAura[])persistentAuras.Clone();
            IsDead = isDead;
        }

        internal WorldStateSnapshot WorldState { get; }
        internal Resource[] Resources => (Resource[])_resources.Clone();
        internal Cooldown[] Cooldowns => _cooldowns == null ? null : (Cooldown[])_cooldowns.Clone();
        internal PersistentAura[] PersistentAuras =>
            _persistentAuras == null ? null : (PersistentAura[])_persistentAuras.Clone();
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

        /// <summary>Persisted cooldown state for one character ability.</summary>
        internal sealed class Cooldown
        {
            internal Cooldown(
                string cooldownDefinitionID,
                int chargeIndex,
                DateTimeOffset expiresAtUtc)
            {
                CooldownDefinitionID = cooldownDefinitionID;
                ChargeIndex = chargeIndex;
                ExpiresAtUtc = expiresAtUtc;
            }

            internal string CooldownDefinitionID { get; }
            internal int ChargeIndex { get; }
            internal DateTimeOffset ExpiresAtUtc { get; }
        }

        /// <summary>Persisted state for one character aura that survives actor lifetime.</summary>
        internal sealed class PersistentAura
        {
            internal PersistentAura(
                Guid persistentAuraID,
                string auraDefinitionID,
                Guid? sourceActorID,
                int stacks,
                DateTimeOffset? expiresAtUtc,
                DateTimeOffset? nextTickAtUtc,
                float? tickIntervalSeconds)
            {
                PersistentAuraID = persistentAuraID;
                AuraDefinitionID = auraDefinitionID;
                SourceActorID = sourceActorID;
                Stacks = stacks;
                ExpiresAtUtc = expiresAtUtc;
                NextTickAtUtc = nextTickAtUtc;
                TickIntervalSeconds = tickIntervalSeconds;
            }

            internal Guid PersistentAuraID { get; }
            internal string AuraDefinitionID { get; }
            internal Guid? SourceActorID { get; }
            internal int Stacks { get; }
            internal DateTimeOffset? ExpiresAtUtc { get; }
            internal DateTimeOffset? NextTickAtUtc { get; }
            internal float? TickIntervalSeconds { get; }
        }
    }
}
