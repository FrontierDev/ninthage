using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Contains the validated spatial state used to enter a character into the world.</summary>
    internal readonly struct CharacterWorldEntryLocation
    {
        /// <summary>Gets the authoritative runtime context for the character.</summary>
        internal ActorSpatialContext Context { get; }
        /// <summary>Gets the saved absolute world position for the character.</summary>
        internal WorldPosition Position { get; }
        /// <summary>Gets the normalized saved facing for the character.</summary>
        internal Quaternion Facing { get; }

        /// <summary>Creates a validated world-entry location.</summary>
        /// <param name="context">The authoritative runtime context.</param>
        /// <param name="position">The saved absolute world position.</param>
        /// <param name="facing">The saved character facing.</param>
        /// <exception cref="ArgumentException">Thrown when any spatial value is invalid.</exception>
        internal CharacterWorldEntryLocation(
            ActorSpatialContext context,
            WorldPosition position,
            Quaternion facing)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
            if (!position.IsFinite)
                throw new ArgumentException("A finite world position is required.", nameof(position));

            double facingLengthSquared = (double)facing.x * facing.x
                + (double)facing.y * facing.y
                + (double)facing.z * facing.z
                + (double)facing.w * facing.w;
            if (float.IsNaN(facing.x)
                || float.IsInfinity(facing.x)
                || float.IsNaN(facing.y)
                || float.IsInfinity(facing.y)
                || float.IsNaN(facing.z)
                || float.IsInfinity(facing.z)
                || float.IsNaN(facing.w)
                || float.IsInfinity(facing.w)
                || Math.Abs(facingLengthSquared - 1d) > 0.001d)
            {
                throw new ArgumentException("A finite unit quaternion is required.", nameof(facing));
            }

            Context = context;
            Position = position;
            Facing = facing.normalized;
        }
    }
}
