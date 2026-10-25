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
        private int _activeQueryCount;

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
        /// <exception cref="InvalidOperationException">
        /// Thrown when the actor already has a membership or a query is active.
        /// </exception>
        public void Insert(Guid actorID, ActorSpatialContext context, WorldPosition acceptedPosition)
        {
            EnsureNoActiveQuery();
            ValidateActorID(actorID);
            if (!context.IsValid)
                throw new ArgumentException(
                    "A valid world, map and instance context is required.",
                    nameof(context));
            ValidatePosition(acceptedPosition);
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
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the position cannot be represented by the index's integral cell coordinates.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown when the actor is not currently indexed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the supplied context differs from the existing context, or when the existing membership
        /// is inconsistent with its cell bucket, or when a query is active.
        /// </exception>
        public void Move(Guid actorID, ActorSpatialContext context, WorldPosition acceptedPosition)
        {
            EnsureNoActiveQuery();
            if (!_cellByActorID.TryGetValue(actorID, out var currentCell))
            {
                throw new KeyNotFoundException($"Actor '{actorID}' is not registered in the spatial index.");
            }

            // Ensure the actor is not attempting to move to a different spatial context.
            if (!currentCell.Context.Equals(context))
            {
                throw new InvalidOperationException(
                    $"Actor '{actorID}' cannot move to a different spatial context; "
                    + "context transfers require the dedicated transfer operation.");
            }

            var destinationCell = CreateCellKey(context, acceptedPosition);

            // If the actor is already in the destination cell, no movement is necessary.
            if (currentCell.Equals(destinationCell))
                return;

            var currentBucket = GetExistingBucket(currentCell, actorID);
            AddActorToCell(actorID, destinationCell);
            RemoveActorFromBucket(currentCell, currentBucket, actorID);
            _cellByActorID[actorID] = destinationCell;
        }

        /// <summary>
        /// Atomically transfers an indexed actor to the cell projected from a destination context
        /// and accepted absolute position.
        /// </summary>
        /// <param name="actorID">The indexed live actor identifier.</param>
        /// <param name="destinationContext">The actor's destination world, map and instance context.</param>
        /// <param name="destinationPosition">The destination accepted double-precision absolute position.</param>
        /// <exception cref="ArgumentException">Thrown when the destination context is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the destination position cannot be represented by the index's integral cell coordinates.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown when the actor is not currently indexed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the existing membership is inconsistent, destination insertion fails, or a query is active.
        /// </exception>
        public void Transfer(
            Guid actorID,
            ActorSpatialContext destinationContext,
            WorldPosition destinationPosition)
        {
            EnsureNoActiveQuery();
            if (!destinationContext.IsValid)
                throw new ArgumentException(
                    "A valid destination actor spatial context is required.",
                    nameof(destinationContext));

            if (!_cellByActorID.TryGetValue(actorID, out var currentCell))
            {
                throw new KeyNotFoundException($"Actor '{actorID}' is not registered in the spatial index.");
            }

            var destinationCell = CreateCellKey(destinationContext, destinationPosition);
            var currentBucket = GetExistingBucket(currentCell, actorID);
            if (currentCell.Equals(destinationCell))
                return;

            AddActorToCell(actorID, destinationCell);
            RemoveActorFromBucket(currentCell, currentBucket, actorID);
            _cellByActorID[actorID] = destinationCell;
        }

        /// <summary>
        /// Removes all derived spatial memberships belonging to an authoritative context.
        /// </summary>
        /// <param name="context">The world, map and instance context to remove.</param>
        /// <param name="removedActorIDs">
        /// A caller-owned list to which removed actor identifiers are appended; existing entries are preserved.
        /// </param>
        /// <returns>The number of actor memberships removed from the context.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="removedActorIDs"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a query is active or a context membership is internally inconsistent.
        /// </exception>
        public int ClearContext(
            ActorSpatialContext context,
            List<Guid> removedActorIDs)
        {
            EnsureNoActiveQuery();
            if (!context.IsValid)
                throw new ArgumentException(
                    "A valid world, map and instance context is required.",
                    nameof(context));
            if (removedActorIDs == null)
                throw new ArgumentNullException(nameof(removedActorIDs));

            var memberships = new List<KeyValuePair<Guid, SpatialCellKey>>();
            foreach (var pair in _cellByActorID)
            {
                if (!pair.Value.Context.Equals(context))
                    continue;

                memberships.Add(new KeyValuePair<Guid, SpatialCellKey>(pair.Key, pair.Value));
            }

            foreach (var membership in memberships)
            {
                var bucket = GetExistingBucket(membership.Value, membership.Key);
                RemoveActorFromBucket(membership.Value, bucket, membership.Key);
                _cellByActorID.Remove(membership.Key);
                removedActorIDs.Add(membership.Key);
            }

            return memberships.Count;
        }

        /// <summary>Removes an actor and releases its empty spatial cell bucket.</summary>
        /// <param name="actorID">The indexed actor identifier.</param>
        /// <exception cref="KeyNotFoundException">Thrown when the actor is not currently indexed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the existing membership is inconsistent with its cell bucket or a query is active.
        /// </exception>
        public void Remove(Guid actorID)
        {
            EnsureNoActiveQuery();

            if (!_cellByActorID.TryGetValue(actorID, out var cell))
            {
                throw new KeyNotFoundException($"Actor '{actorID}' is not registered in the spatial index.");
            }

            var bucket = GetExistingBucket(cell, actorID);
            RemoveActorFromBucket(cell, bucket, actorID);
            _cellByActorID.Remove(actorID);
        }

        /// <summary>
        /// Appends actor IDs from context-qualified cells intersecting the horizontal query radius.
        /// </summary>
        /// <param name="context">The authoritative world, map and instance context to query.</param>
        /// <param name="center">The double-precision absolute query centre.</param>
        /// <param name="radius">The non-negative horizontal broad-phase radius in metres.</param>
        /// <param name="resultBuffer">
        /// A caller-owned reusable list. This method appends to it and does not clear it; entries are actor IDs,
        /// not Unity objects.
        /// </param>
        /// <param name="examinedCandidateCount">Receives the number of actor IDs examined in matching buckets.</param>
        /// <param name="predicate">
        /// An optional caller-owned filter applied after context and cell screening. Query result order is unspecified.
        /// </param>
        /// <returns>The number of actor IDs added to <paramref name="resultBuffer"/> by this invocation.</returns>
        /// <remarks>
        /// Call this synchronously at the authoritative tick boundary. Membership mutations are rejected while the
        /// query is active; consumers requiring deterministic order must sort by actor ID.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the context or centre is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resultBuffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the radius is invalid or too large to index.</exception>
        public int QueryCandidates(
            ActorSpatialContext context,
            WorldPosition center,
            double radius,
            List<Guid> resultBuffer,
            out int examinedCandidateCount,
            Predicate<Guid> predicate = null)
        {
            return QueryBuckets(
                context,
                center,
                radius,
                resultBuffer,
                null,
                false,
                predicate,
                false,
                out examinedCandidateCount);
        }

        /// <summary>
        /// Appends actor IDs within an exact double-precision radius after broad-phase cell screening.
        /// </summary>
        /// <param name="context">The authoritative world, map and instance context to query.</param>
        /// <param name="center">The double-precision absolute query centre.</param>
        /// <param name="radius">The non-negative exact range in metres.</param>
        /// <param name="resultBuffer">
        /// A caller-owned reusable list. This method appends to it and does not clear it; entries are actor IDs,
        /// not Unity objects.
        /// </param>
        /// <param name="acceptedPositionResolver">
        /// Resolves each candidate's accepted position from the same authoritative tick snapshot as the query.
        /// </param>
        /// <param name="examinedCandidateCount">Receives the number of actor IDs examined after bucket screening.</param>
        /// <param name="includeVerticalDistance">
        /// When true, applies a 3D squared-distance check; when false, applies horizontal X/Z squared distance only.
        /// </param>
        /// <param name="predicate">An optional caller-owned filter applied after the exact distance check.</param>
        /// <returns>The number of actor IDs added to <paramref name="resultBuffer"/> by this invocation.</returns>
        /// <remarks>
        /// Call this synchronously at the authoritative tick boundary. Membership mutations are rejected while any
        /// query is active, and result ordering is unspecified; consumers requiring deterministic order must sort by actor ID.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the context or centre is invalid.</exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="resultBuffer"/> or <paramref name="acceptedPositionResolver"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the radius is invalid or too large to index.</exception>
        public int QueryInRange(
            ActorSpatialContext context,
            WorldPosition center,
            double radius,
            List<Guid> resultBuffer,
            Func<Guid, WorldPosition> acceptedPositionResolver,
            out int examinedCandidateCount,
            bool includeVerticalDistance = true,
            Predicate<Guid> predicate = null)
        {
            if (acceptedPositionResolver == null)
                throw new ArgumentNullException(nameof(acceptedPositionResolver));

            return QueryBuckets(
                context,
                center,
                radius,
                resultBuffer,
                acceptedPositionResolver,
                includeVerticalDistance,
                predicate,
                true,
                out examinedCandidateCount);
        }

        private static void ValidateActorID(Guid actorID)
        {
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
        }

        private SpatialCellKey CreateCellKey(ActorSpatialContext context, WorldPosition position)
        {
            return new SpatialCellKey(
                context,
                GetCellCoordinate(position.x, nameof(position.x)),
                GetCellCoordinate(position.z, nameof(position.z)));
        }

        private static void ValidatePosition(WorldPosition position)
        {
            if (!position.IsFinite)
                throw new ArgumentException(
                    "An accepted position must have finite x, y and z coordinates.",
                    nameof(position));
        }

        private int QueryBuckets(
            ActorSpatialContext context,
            WorldPosition center,
            double radius,
            List<Guid> resultBuffer,
            Func<Guid, WorldPosition> acceptedPositionResolver,
            bool includeVerticalDistance,
            Predicate<Guid> predicate,
            bool exactRange,
            out int examinedCandidateCount)
        {
            if (resultBuffer == null)
                throw new ArgumentNullException(nameof(resultBuffer));

            var bounds = GetQueryBounds(context, center, radius);
            double radiusSquared = 0d;
            if (exactRange)
            {
                radiusSquared = radius * radius;
                if (double.IsInfinity(radiusSquared))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(radius),
                        radius,
                        "Query radius is too large for a finite squared-distance comparison.");
                }
            }

            examinedCandidateCount = 0;
            int returnedCount = 0;
            _activeQueryCount++;
            try
            {
                for (long cellX = bounds.MinCellX; ; cellX++)
                {
                    for (long cellZ = bounds.MinCellZ; ; cellZ++)
                    {
                        var cell = new SpatialCellKey(context, cellX, cellZ);
                        if (_actorIDsByCell.TryGetValue(cell, out var bucket))
                        {
                            foreach (var actorID in bucket)
                            {
                                examinedCandidateCount++;

                                if (exactRange
                                    && !IsWithinRange(
                                        center,
                                        acceptedPositionResolver(actorID),
                                        radiusSquared,
                                        includeVerticalDistance))
                                {
                                    continue;
                                }

                                if (predicate != null && !predicate(actorID))
                                    continue;

                                resultBuffer.Add(actorID);
                                returnedCount++;
                            }
                        }

                        if (cellZ == bounds.MaxCellZ)
                            break;
                    }

                    if (cellX == bounds.MaxCellX)
                        break;
                }
            }
            finally
            {
                _activeQueryCount--;
            }

            return returnedCount;
        }

        private CellBounds GetQueryBounds(ActorSpatialContext context, WorldPosition center, double radius)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));

            ValidatePosition(center);
            ValidateRadius(radius);

            return new CellBounds(
                GetCellCoordinate(center.x - radius, nameof(center.x)),
                GetCellCoordinate(center.x + radius, nameof(center.x)),
                GetCellCoordinate(center.z - radius, nameof(center.z)),
                GetCellCoordinate(center.z + radius, nameof(center.z)));
        }

        private static void ValidateRadius(double radius)
        {
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    "Query radius must be finite and non-negative.");
            }
        }

        private static bool IsWithinRange(
            WorldPosition center,
            WorldPosition candidate,
            double radiusSquared,
            bool includeVerticalDistance)
        {
            double deltaX = candidate.x - center.x;
            double deltaZ = candidate.z - center.z;
            double distanceSquared = deltaX * deltaX + deltaZ * deltaZ;
            if (includeVerticalDistance)
            {
                double deltaY = candidate.y - center.y;
                distanceSquared += deltaY * deltaY;
            }

            return distanceSquared <= radiusSquared;
        }

        /// <summary>
        /// Ensures that there are no active spatial queries.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void EnsureNoActiveQuery()
        {
            if (_activeQueryCount > 0)
            {
                throw new InvalidOperationException(
                    "Spatial membership cannot change while a spatial query is active.");
            }
        }

        private long GetCellCoordinate(double coordinate, string coordinateName)
        {
            double cellCoordinate = Math.Floor(coordinate / CellWidth);
            if (double.IsNaN(cellCoordinate) || double.IsInfinity(cellCoordinate)
                || cellCoordinate < long.MinValue || cellCoordinate >= MAX_CELL_COORDINATE_EXCLUSIVE)
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
            bucket.Remove(actorID);
            if (bucket.Count == 0)
                _actorIDsByCell.Remove(cell);
        }

        private readonly struct CellBounds
        {
            public CellBounds(long minCellX, long maxCellX, long minCellZ, long maxCellZ)
            {
                MinCellX = minCellX;
                MaxCellX = maxCellX;
                MinCellZ = minCellZ;
                MaxCellZ = maxCellZ;
            }

            public long MinCellX { get; }
            public long MaxCellX { get; }
            public long MinCellZ { get; }
            public long MaxCellZ { get; }
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
