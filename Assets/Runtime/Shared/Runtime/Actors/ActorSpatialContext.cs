using System;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>
    /// Identifies the authoritative world, logical map and runtime simulation context for an actor or spatial query.
    /// </summary>
    public readonly struct ActorSpatialContext : IEquatable<ActorSpatialContext>
    {
        private readonly Guid _worldID;
        private readonly string _mapID;
        private readonly Guid _instanceID;

        private ActorSpatialContext(Guid worldID, string mapID, Guid instanceID)
        {
            _worldID = worldID;
            _mapID = mapID;
            _instanceID = instanceID;
        }

        /// <summary>Gets the stable identity of the player-selectable world.</summary>
        public Guid WorldID => _worldID;

        /// <summary>Gets the stable, case-sensitive identity of the logical map.</summary>
        public string MapID => _mapID;

        /// <summary>
        /// Gets the private runtime instance identity, or <see cref="Guid.Empty"/> for the public context.
        /// </summary>
        public Guid InstanceID => _instanceID;

        /// <summary>
        /// Gets whether this context has a valid world and map identity.
        /// </summary>
        public bool IsValid => _worldID != Guid.Empty && !string.IsNullOrWhiteSpace(_mapID);

        /// <summary>
        /// Creates the public runtime context for a world and logical map.
        /// </summary>
        /// <param name="worldID">The stable identity of the player-selectable world.</param>
        /// <param name="mapID">The canonical, case-sensitive logical map identity.</param>
        /// <returns>A valid public spatial context.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="worldID"/> is empty or <paramref name="mapID"/> is null, empty or whitespace.
        /// </exception>
        public static ActorSpatialContext CreatePublic(Guid worldID, string mapID)
        {
            ValidateWorldAndMap(worldID, mapID);
            return new ActorSpatialContext(worldID, mapID, Guid.Empty);
        }

        /// <summary>
        /// Creates a private runtime context for a world, logical map and trusted instance identity.
        /// </summary>
        /// <param name="worldID">The stable identity of the player-selectable world.</param>
        /// <param name="mapID">The canonical, case-sensitive logical map identity.</param>
        /// <param name="instanceID">The nonempty identity of the private runtime instance.</param>
        /// <returns>A valid private spatial context.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the world or map is invalid, or when <paramref name="instanceID"/> is empty.
        /// </exception>
        public static ActorSpatialContext CreateInstanced(Guid worldID, string mapID, Guid instanceID)
        {
            ValidateWorldAndMap(worldID, mapID);
            if (instanceID == Guid.Empty)
                throw new ArgumentException("A private runtime instance ID is required.", nameof(instanceID));

            return new ActorSpatialContext(worldID, mapID, instanceID);
        }

        /// <summary>
        /// Determines whether another context is valid and identifies exactly the same world, map and instance.
        /// </summary>
        /// <param name="other">The context to compare with this context.</param>
        /// <returns>
        /// <see langword="true"/> only when both contexts are valid and all three identifiers are equal.
        /// </returns>
        public bool IsCompatibleWith(ActorSpatialContext other)
        {
            return IsValid && other.IsValid && Equals(other);
        }

        /// <summary>Determines whether this context has the same three identifiers as another context.</summary>
        /// <param name="other">The context to compare with this context.</param>
        /// <returns><see langword="true"/> when all three identifiers are equal.</returns>
        public bool Equals(ActorSpatialContext other)
        {
            return _worldID == other._worldID
                && StringComparer.Ordinal.Equals(_mapID, other._mapID)
                && _instanceID == other._instanceID;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ActorSpatialContext other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _worldID.GetHashCode();
                hashCode = (hashCode * 397) ^ (_mapID == null ? 0 : StringComparer.Ordinal.GetHashCode(_mapID));
                hashCode = (hashCode * 397) ^ _instanceID.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>Determines whether two contexts have the same three identifiers.</summary>
        public static bool operator ==(ActorSpatialContext left, ActorSpatialContext right)
        {
            return left.Equals(right);
        }

        /// <summary>Determines whether two contexts do not have the same three identifiers.</summary>
        public static bool operator !=(ActorSpatialContext left, ActorSpatialContext right)
        {
            return !left.Equals(right);
        }

        private static void ValidateWorldAndMap(Guid worldID, string mapID)
        {
            if (worldID == Guid.Empty)
                throw new ArgumentException("A world ID is required.", nameof(worldID));
            if (string.IsNullOrWhiteSpace(mapID))
                throw new ArgumentException("A map ID is required.", nameof(mapID));
        }
    }
}
