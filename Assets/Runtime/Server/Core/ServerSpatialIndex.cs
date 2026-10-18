using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Game.Server.Core
{
    /// <summary>
    /// Maintains context-qualified spatial cell membership for live server actors.
    /// </summary>
    public sealed class ServerSpatialIndex
    {
        private const double MAX_CELL_COORDINATE_EXCLUSIVE = 9223372036854775808d;

        private readonly Dictionary<SpatialCellKey, HashSet<Guid>> _actorIDsByCell = new();
        private readonly Dictionary<Guid, SpatialCellKey> _cellByActorID = new();

        /// <summary>
        /// Initializes a server spatial index with the configured horizontal cell width.
        /// </summary>
        /// <param name="cellWidth">The positive, finite cell width in metres.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cellWidth"/> is non-finite or not strictly positive.
        /// </exception>
        public ServerSpatialIndex(double cellWidth)
        {
            if (double.IsNaN(cellWidth) || double.IsInfinity(cellWidth) || cellWidth <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellWidth),
                    cellWidth,
                    "Spatial index cell width must be finite and strictly positive.");
            }

            CellWidth = cellWidth;
        }

        /// <summary>Gets the horizontal spatial cell width in metres.</summary>
        public double CellWidth { get; }

        /// <summary>
        /// Adds an actor to the cell projected from its accepted absolute position.
        /// </summary>
        /// <param name="actorID">The live actor identifier.</param>
        /// <param name="context">The actor's authoritative world, map and instance context.</param>
        /// <param name="acceptedPosition">The accepted double-precision absolute position.</param>
        /// <exception cref="ArgumentException">Thrown when the actor ID, context or position is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the position cannot be represented by the index's integral cell coordinates.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the actor already has a membership.</exception>
        public void Insert(Guid actorID, ActorSpatialContext context, WorldPosition acceptedPosition)
        {
            ValidateActorID(actorID);
            var cell = CreateCellKey(context, acceptedPosition);

            if (_cellByActorID.ContainsKey(actorID))
            {
                throw new InvalidOperationException(
                    $"Actor '{actorID}' already has a spatial membership; use Move to update it.");
            }

            AddActorToCell(actorID, cell);
            _cellByActorID.Add(actorID, cell);
        }

        /// <summary>
        /// Moves an indexed actor to the cell projected from its accepted absolute position.
        /// </summary>
        /// <param name="actorID">The indexed actor identifier.</param>
        /// <param name="context">The actor's authoritative world, map and instance context.</param>
        /// <param name="acceptedPosition">The accepted double-precision absolute position.</param>
        /// <exception cref="ArgumentException">Thrown when the actor ID, context or position is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the position cannot be represented by the index's integral cell coordinates.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown when the actor is not currently indexed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the supplied context differs from the existing context, or when the existing membership
        /// is inconsistent with its cell bucket.
        /// </exception>
        public void Move(Guid actorID, ActorSpatialContext context, WorldPosition acceptedPosition)
        {
            ValidateActorID(actorID);
            var destinationCell = CreateCellKey(context, acceptedPosition);

            if (!_cellByActorID.TryGetValue(actorID, out var currentCell))
            {
                throw new KeyNotFoundException($"Actor '{actorID}' is not registered in the spatial index.");
            }

            if (!currentCell.Context.Equals(context))
            {
                throw new InvalidOperationException(
                    $"Actor '{actorID}' cannot move to a different spatial context; "
                    + "context transfers require the dedicated transfer operation.");
            }

            if (currentCell.Equals(destinationCell))
                return;

            var currentBucket = GetExistingBucket(currentCell, actorID);
            AddActorToCell(actorID, destinationCell);
            RemoveActorFromBucket(currentCell, currentBucket, actorID);
            _cellByActorID[actorID] = destinationCell;
        }

        /// <summary>Removes an actor and releases its empty spatial cell bucket.</summary>
        /// <param name="actorID">The indexed actor identifier.</param>
        /// <exception cref="ArgumentException">Thrown when the actor ID is empty.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the actor is not currently indexed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the existing membership is inconsistent with its cell bucket.
        /// </exception>
        public void Remove(Guid actorID)
        {
            ValidateActorID(actorID);

            if (!_cellByActorID.TryGetValue(actorID, out var cell))
            {
                throw new KeyNotFoundException($"Actor '{actorID}' is not registered in the spatial index.");
            }

            var bucket = GetExistingBucket(cell, actorID);
            RemoveActorFromBucket(cell, bucket, actorID);
            _cellByActorID.Remove(actorID);
        }

        private static void ValidateActorID(Guid actorID)
        {
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
        }

        private SpatialCellKey CreateCellKey(ActorSpatialContext context, WorldPosition position)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));

            ValidatePosition(position);

            return new SpatialCellKey(
                context,
                GetCellCoordinate(position.x, nameof(position.x)),
                GetCellCoordinate(position.z, nameof(position.z)));
        }

        private static void ValidatePosition(WorldPosition position)
        {
            if (double.IsNaN(position.x) || double.IsInfinity(position.x)
                || double.IsNaN(position.y) || double.IsInfinity(position.y)
                || double.IsNaN(position.z) || double.IsInfinity(position.z))
            {
                throw new ArgumentException(
                    "An accepted position must have finite x, y and z coordinates.",
                    nameof(position));
            }
        }

        private long GetCellCoordinate(double coordinate, string coordinateName)
        {
            double cellCoordinate = Math.Floor(coordinate / CellWidth);
            if (cellCoordinate < long.MinValue || cellCoordinate >= MAX_CELL_COORDINATE_EXCLUSIVE)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coordinate),
                    coordinate,
                    $"The {coordinateName} coordinate is outside the supported spatial cell range.");
            }

            return checked((long)cellCoordinate);
        }

        private void AddActorToCell(Guid actorID, SpatialCellKey cell)
        {
            if (!_actorIDsByCell.TryGetValue(cell, out var bucket))
            {
                bucket = new HashSet<Guid>();
                _actorIDsByCell.Add(cell, bucket);
            }

            if (!bucket.Add(actorID))
            {
                throw new InvalidOperationException(
                    $"Actor '{actorID}' already exists in spatial cell ({cell.CellX}, {cell.CellZ}).");
            }
        }

        private HashSet<Guid> GetExistingBucket(SpatialCellKey cell, Guid actorID)
        {
            if (!_actorIDsByCell.TryGetValue(cell, out var bucket) || !bucket.Contains(actorID))
            {
                throw new InvalidOperationException(
                    $"Spatial membership for actor '{actorID}' is missing from its cell bucket.");
            }

            return bucket;
        }

        private void RemoveActorFromBucket(SpatialCellKey cell, HashSet<Guid> bucket, Guid actorID)
        {
            if (!bucket.Remove(actorID))
            {
                throw new InvalidOperationException(
                    $"Spatial membership for actor '{actorID}' could not be removed from its cell bucket.");
            }

            if (bucket.Count == 0)
                _actorIDsByCell.Remove(cell);
        }

        private readonly struct SpatialCellKey : IEquatable<SpatialCellKey>
        {
            public SpatialCellKey(ActorSpatialContext context, long cellX, long cellZ)
            {
                Context = context;
                CellX = cellX;
                CellZ = cellZ;
            }

            public ActorSpatialContext Context { get; }
            public long CellX { get; }
            public long CellZ { get; }

            public bool Equals(SpatialCellKey other)
            {
                return Context.Equals(other.Context)
                    && CellX == other.CellX
                    && CellZ == other.CellZ;
            }

            public override bool Equals(object obj)
            {
                return obj is SpatialCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Context.GetHashCode();
                    hashCode = (hashCode * 397) ^ CellX.GetHashCode();
                    hashCode = (hashCode * 397) ^ CellZ.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
