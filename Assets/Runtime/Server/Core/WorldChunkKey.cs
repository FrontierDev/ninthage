using Game.Shared.Runtime.Actors;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Identifies an environment tile within an authoritative runtime context.</summary>
    public readonly struct WorldChunkKey : IEquatable<WorldChunkKey>
    {
        /// <summary>Gets the authoritative world, map and runtime instance context.</summary>
        public ActorSpatialContext Context { get; }
        /// <summary>Gets the environment tile coordinate.</summary>
        public Vector2Int ChunkCoord { get; }

        /// <summary>Creates a context-qualified environment tile key.</summary>
        /// <param name="context">The authoritative runtime context.</param>
        /// <param name="chunkCoord">The environment tile coordinate.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        public WorldChunkKey(ActorSpatialContext context, Vector2Int chunkCoord)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));

            Context = context;
            ChunkCoord = chunkCoord;
        }

        /// <inheritdoc />
        public bool Equals(WorldChunkKey other)
        {
            return Context == other.Context && ChunkCoord == other.ChunkCoord;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is WorldChunkKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Context.GetHashCode() * 397) ^ ChunkCoord.GetHashCode();
            }
        }

        /// <summary>Determines whether two keys identify the same context and tile.</summary>
        public static bool operator ==(WorldChunkKey left, WorldChunkKey right)
        {
            return left.Equals(right);
        }

        /// <summary>Determines whether two keys identify different contexts or tiles.</summary>
        public static bool operator !=(WorldChunkKey left, WorldChunkKey right)
        {
            return !left.Equals(right);
        }
    }
}
