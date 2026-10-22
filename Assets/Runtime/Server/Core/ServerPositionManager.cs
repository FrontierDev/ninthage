using System;
using System.Collections.Generic;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Describes the lifecycle state of a server physics frame.</summary>
    public enum ServerPhysicsFrameState
    {
        Reserved,
        Ready,
        Retiring,
    }

    /// <summary>
    /// Immutable identity and coordinate data for one context-qualified environment tile
    /// assigned to a server physics slot.
    /// </summary>
    public readonly struct ServerPhysicsFrame : IEquatable<ServerPhysicsFrame>
    {
        internal ServerPhysicsFrame(
            ActorSpatialContext context,
            Vector2Int tileCoordinate,
            int slotIndex,
            Vector2Int slotCoordinate,
            Vector3 slotAnchor,
            WorldPosition tileOrigin,
            int generation)
        {
            Context = context;
            TileCoordinate = tileCoordinate;
            SlotIndex = slotIndex;
            SlotCoordinate = slotCoordinate;
            SlotAnchor = slotAnchor;
            TileOrigin = tileOrigin;
            Generation = generation;
        }

        /// <summary>Gets the full authoritative context owning this tile.</summary>
        public ActorSpatialContext Context { get; }
        /// <summary>Gets the environment tile coordinate.</summary>
        public Vector2Int TileCoordinate { get; }
        /// <summary>Gets the stable index of the assigned physical slot.</summary>
        public int SlotIndex { get; }
        /// <summary>Gets the two-dimensional coordinate of the assigned slot.</summary>
        public Vector2Int SlotCoordinate { get; }
        /// <summary>Gets the bounded Unity-space anchor for the slot.</summary>
        public Vector3 SlotAnchor { get; }
        /// <summary>Gets the absolute double-precision origin of the tile.</summary>
        public WorldPosition TileOrigin { get; }

        // The generation prevents a stale frame token from becoming valid after slot reuse.
        internal int Generation { get; }

        /// <inheritdoc />
        public bool Equals(ServerPhysicsFrame other)
        {
            return Context == other.Context
                && TileCoordinate == other.TileCoordinate
                && SlotIndex == other.SlotIndex
                && Generation == other.Generation;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ServerPhysicsFrame other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Context.GetHashCode();
                hashCode = (hashCode * 397) ^ TileCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ SlotIndex;
                return (hashCode * 397) ^ Generation;
            }
        }

        /// <summary>Determines whether two frame tokens identify the same frame allocation.</summary>
        public static bool operator ==(ServerPhysicsFrame left, ServerPhysicsFrame right)
        {
            return left.Equals(right);
        }

        /// <summary>Determines whether two frame tokens identify different frame allocations.</summary>
        public static bool operator !=(ServerPhysicsFrame left, ServerPhysicsFrame right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Server-side position authority and owner of the bounded physics/NavMesh frame registry.
    /// Chunk/local conversion remains separate from the physical-slot coordinate system.
    /// </summary>
    public sealed class ServerPositionManager : MonoBehaviour
    {
        private const int DEFAULT_SLOT_GRID_WIDTH = 16;
        private const int DEFAULT_SLOT_GRID_HEIGHT = 16;
        private const float DEFAULT_SLOT_PITCH = 512f;
        private const double TILE_BOUNDARY_EPSILON = 0.000001d;

        private static ServerPositionManager _instance;
        /// <summary>Gets the active server position manager.</summary>
        public static ServerPositionManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the position manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        [SerializeField] private int _slotGridWidth = DEFAULT_SLOT_GRID_WIDTH;
        [SerializeField] private int _slotGridHeight = DEFAULT_SLOT_GRID_HEIGHT;
        [SerializeField] private float _slotPitch = DEFAULT_SLOT_PITCH;

        private int[] _slotGenerations;
        private readonly Dictionary<WorldChunkKey, FrameRecord> _framesByKey = new();
        private readonly Dictionary<int, FrameRecord> _framesBySlot = new();

        /// <summary>Gets the configured number of physical slots along the X axis.</summary>
        public int SlotGridWidth => _slotGridWidth;
        /// <summary>Gets the configured number of physical slots along the Z axis.</summary>
        public int SlotGridHeight => _slotGridHeight;
        /// <summary>Gets the total physical slot capacity.</summary>
        public int SlotCapacity => checked(_slotGridWidth * _slotGridHeight);
        /// <summary>Gets the distance between physical slot anchors in metres.</summary>
        public float SlotPitch => _slotPitch;
        /// <summary>Gets the canonical environment tile footprint in metres.</summary>
        public float EnvironmentTileSize => GetEnvironmentTileSize();
        /// <summary>Gets the guaranteed horizontal gap between full-size tile footprints.</summary>
        public float MinimumSlotGap => _slotPitch - EnvironmentTileSize;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerPositionManager is already initialized!");
                return;
            }

            ValidateLayout();
            _slotGenerations = new int[SlotCapacity];
            _instance = this;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_framesByKey.Count > 0)
            {
                Debug.Error(
                    $"ServerPositionManager is shutting down with {_framesByKey.Count} active physics frame(s). "
                    + "Retire and release all frame references before shutdown.");
            }

            _framesByKey.Clear();
            _framesBySlot.Clear();
            _slotGenerations = null;

            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// Reserves one unique physical slot for a context-qualified environment tile.
        /// The returned frame is not physics-visible until <see cref="MarkFrameReady"/> is called.
        /// </summary>
        /// <param name="context">The full authoritative world, map and instance context.</param>
        /// <param name="tileCoordinate">The canonical environment tile coordinate.</param>
        /// <returns>The immutable token for the reserved frame.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the tile is already registered or no physical slot is available.
        /// </exception>
        public ServerPhysicsFrame ReserveFrame(ActorSpatialContext context, Vector2Int tileCoordinate)
        {
            EnsureInitialized();
            WorldChunkKey key = CreateKey(context, tileCoordinate);

            if (_framesByKey.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"A physics frame is already registered for context '{context.MapID}' tile {tileCoordinate}.");
            }

            int slotIndex = FindFreeSlot();
            if (slotIndex < 0)
            {
                throw new InvalidOperationException(
                    $"The server physics frame pool is exhausted ({SlotCapacity} slots) while reserving "
                    + $"context '{context.MapID}' tile {tileCoordinate}.");
            }

            int generation = NextGeneration(slotIndex);
            Vector2Int slotCoordinate = new(slotIndex % _slotGridWidth, slotIndex / _slotGridWidth);
            Vector3 slotAnchor = new(
                slotCoordinate.x * _slotPitch,
                0f,
                slotCoordinate.y * _slotPitch);
            float environmentTileSize = EnvironmentTileSize;
            WorldPosition tileOrigin = new(
                (double)tileCoordinate.x * environmentTileSize,
                0d,
                (double)tileCoordinate.y * environmentTileSize);
            var frame = new ServerPhysicsFrame(
                context,
                tileCoordinate,
                slotIndex,
                slotCoordinate,
                slotAnchor,
                tileOrigin,
                generation);
            var record = new FrameRecord(frame);

            _framesByKey.Add(key, record);
            _framesBySlot.Add(slotIndex, record);
            return frame;
        }

        /// <summary>Looks up a currently reserved, ready or retiring frame.</summary>
        public bool TryGetFrame(
            ActorSpatialContext context,
            Vector2Int tileCoordinate,
            out ServerPhysicsFrame frame)
        {
            EnsureInitialized();
            WorldChunkKey key = CreateKey(context, tileCoordinate);
            if (_framesByKey.TryGetValue(key, out FrameRecord record))
            {
                frame = record.Frame;
                return true;
            }

            frame = default;
            return false;
        }

        /// <summary>Looks up a frame that is ready for physics/NavMesh references.</summary>
        public bool TryGetReadyFrame(
            ActorSpatialContext context,
            Vector2Int tileCoordinate,
            out ServerPhysicsFrame frame)
        {
            if (TryGetFrame(context, tileCoordinate, out frame)
                && GetFrameState(frame) == ServerPhysicsFrameState.Ready)
            {
                return true;
            }

            frame = default;
            return false;
        }

        /// <summary>Gets the current lifecycle state of a frame token.</summary>
        public ServerPhysicsFrameState GetFrameState(ServerPhysicsFrame frame)
        {
            return GetRecord(frame).State;
        }

        /// <summary>Gets the number of live geometry, actor, agent or physics references to a frame.</summary>
        public int GetFrameReferenceCount(ServerPhysicsFrame frame)
        {
            return GetRecord(frame).ReferenceCount;
        }

        /// <summary>Marks a reserved frame ready after its scene-owned content is bound to the slot.</summary>
        public void MarkFrameReady(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.State != ServerPhysicsFrameState.Reserved)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} cannot become ready from state {record.State}.");
            }

            record.State = ServerPhysicsFrameState.Ready;
        }

        /// <summary>
        /// Acquires a lifetime reference for geometry, NavMeshData, a collider, actor, agent or
        /// in-flight physics operation. Reserved frames accept only in-flight placement references;
        /// Ready frames accept normal physics references. Every acquisition must be paired with a release.
        /// </summary>
        public void AcquireFrameReference(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.State == ServerPhysicsFrameState.Retiring)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} cannot acquire a reference while retiring.");
            }

            record.ReferenceCount = checked(record.ReferenceCount + 1);
        }

        /// <summary>Releases one lifetime reference previously acquired from a reserved or ready frame.</summary>
        public void ReleaseFrameReference(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.ReferenceCount == 0)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} has no reference to release.");
            }

            record.ReferenceCount--;
        }

        /// <summary>
        /// Prevents new references while existing scene, actor and physics users detach.
        /// A retiring frame remains allocated until <see cref="ReleaseFrame"/> is called.
        /// </summary>
        public void BeginFrameRetirement(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.State == ServerPhysicsFrameState.Retiring)
                return;
            if (record.State != ServerPhysicsFrameState.Reserved
                && record.State != ServerPhysicsFrameState.Ready)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} cannot retire from state {record.State}.");
            }

            record.State = ServerPhysicsFrameState.Retiring;
        }

        /// <summary>
        /// Releases a retiring frame only after every geometry, actor, agent and physics reference
        /// has been detached. The physical slot then becomes available for a new frame.
        /// </summary>
        public void ReleaseFrame(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.State != ServerPhysicsFrameState.Retiring)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} cannot be released from state {record.State}.");
            }
            if (record.ReferenceCount != 0)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} still has {record.ReferenceCount} live reference(s).");
            }

            _framesByKey.Remove(new WorldChunkKey(frame.Context, frame.TileCoordinate));
            _framesBySlot.Remove(frame.SlotIndex);
        }

        /// <summary>
        /// Validates authored horizontal tile bounds against the canonical environment footprint
        /// and the positive inter-slot gap. Call this before making scene geometry physics-visible.
        /// </summary>
        /// <param name="tileMin">The tile-local minimum bounds.</param>
        /// <param name="tileMax">The tile-local maximum bounds.</param>
        public void ValidateEnvironmentTileExtents(Vector3 tileMin, Vector3 tileMax)
        {
            EnsureInitialized();
            if (!IsFinite(tileMin) || !IsFinite(tileMax))
                throw new ArgumentException("Environment tile bounds must be finite.");
            if (tileMax.x <= tileMin.x || tileMax.z <= tileMin.z)
                throw new ArgumentException("Environment tile bounds must have positive X and Z extents.");

            float environmentTileSize = EnvironmentTileSize;
            double width = (double)tileMax.x - tileMin.x;
            double depth = (double)tileMax.z - tileMin.z;
            if (tileMin.x < 0f || tileMin.z < 0f
                || tileMax.x > environmentTileSize || tileMax.z > environmentTileSize)
            {
                throw new InvalidOperationException(
                    $"Environment tile bounds must lie within the local [0, {environmentTileSize}] footprint.");
            }
            if (width > environmentTileSize || depth > environmentTileSize)
            {
                throw new InvalidOperationException(
                    $"Environment tile bounds ({width}m x {depth}m) exceed the configured "
                    + $"{environmentTileSize}m footprint.");
            }
            if (MinimumSlotGap <= 0f)
            {
                throw new InvalidOperationException(
                    $"The configured slot pitch {_slotPitch}m leaves no positive gap around the "
                    + $"{environmentTileSize}m environment tile footprint.");
            }
        }

        /// <summary>
        /// Converts an absolute world position into the compact local coordinates of a reserved
        /// or ready frame. All subtraction occurs in double precision before the float boundary.
        /// </summary>
        public Vector3 ToPhysicsLocal(ServerPhysicsFrame frame, WorldPosition worldPosition)
        {
            RequireConvertibleFrame(frame);
            ValidateWorldPosition(worldPosition);

            double offsetX = worldPosition.x - frame.TileOrigin.x;
            double offsetZ = worldPosition.z - frame.TileOrigin.z;
            ValidateTileOffset(frame, offsetX, offsetZ);

            double localX = (double)frame.SlotAnchor.x + offsetX;
            double localY = worldPosition.y;
            double localZ = (double)frame.SlotAnchor.z + offsetZ;
            return ToFiniteVector3(localX, localY, localZ);
        }

        /// <summary>
        /// Converts compact frame-local coordinates back to an absolute double-precision position.
        /// All addition occurs in double precision before constructing the world position.
        /// </summary>
        public WorldPosition ToWorld(ServerPhysicsFrame frame, Vector3 physicsLocalPosition)
        {
            RequireConvertibleFrame(frame);
            if (!IsFinite(physicsLocalPosition))
                throw new ArgumentException("Physics-local position must be finite.", nameof(physicsLocalPosition));

            double offsetX = (double)physicsLocalPosition.x - frame.SlotAnchor.x;
            double offsetZ = (double)physicsLocalPosition.z - frame.SlotAnchor.z;
            ValidateTileOffset(frame, offsetX, offsetZ);

            WorldPosition worldPosition = new(
                frame.TileOrigin.x + offsetX,
                frame.TileOrigin.y + ((double)physicsLocalPosition.y - frame.SlotAnchor.y),
                frame.TileOrigin.z + offsetZ);
            ValidateWorldPosition(worldPosition);
            return worldPosition;
        }

        /// <summary>Converts a chunk-relative position to an absolute WorldPosition for network encoding.</summary>
        public WorldPosition ToWorld(Vector2Int chunk, Vector3 localPos)
        {
            return WorldPosition.FromChunkLocal(chunk, localPos);
        }

        /// <summary>
        /// Converts an absolute WorldPosition to its existing chunk-relative network encoding.
        /// This is intentionally separate from physical frame conversion.
        /// </summary>
        public (Vector2Int chunk, Vector3 local) ToChunkLocal(WorldPosition worldPos)
        {
            return (worldPos.Chunk, worldPos.LocalPosition);
        }

        /// <summary>
        /// The old process-wide floating-origin API is intentionally unsupported. Server physics
        /// uses stable context-qualified frames; changing this metadata cannot rebase physics.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown; server-wide rebasing is not supported.</exception>
        public void RecentrePhysicsOrigin(Vector2Int newOriginChunk)
        {
            throw new NotSupportedException(
                "Server physics does not support process-wide origin rebasing. Reserve and bind a context-qualified frame instead.");
        }

        private static float GetEnvironmentTileSize()
        {
            Game.Shared.GameConfiguration configuration = Game.Shared.GameConfigurationManager.Config;
            if (configuration == null)
                throw new InvalidOperationException("Game configuration must be initialized before server physics frames.");
            if (configuration.ChunkSize <= 0)
                throw new InvalidOperationException("GameConfiguration.ChunkSize must be strictly positive.");

            return configuration.ChunkSize;
        }

        private void ValidateLayout()
        {
            if (_slotGridWidth <= 0 || _slotGridHeight <= 0)
                throw new InvalidOperationException("Server physics slot grid dimensions must be positive.");
            if (!IsFinite(_slotPitch))
                throw new InvalidOperationException("Server physics slot dimensions must be finite.");
            float environmentTileSize = EnvironmentTileSize;
            if (_slotPitch <= 0f || environmentTileSize <= 0f)
                throw new InvalidOperationException("Server physics slot dimensions must be positive.");
            if (_slotPitch <= environmentTileSize)
            {
                throw new InvalidOperationException(
                    $"Server physics slot pitch {_slotPitch}m must exceed the environment tile footprint "
                    + $"{environmentTileSize}m.");
            }
            double maximumSlotAnchor = Math.Max(_slotGridWidth - 1, _slotGridHeight - 1) * (double)_slotPitch;
            if (maximumSlotAnchor > float.MaxValue)
            {
                throw new InvalidOperationException(
                    "The configured server physics slot grid does not fit in Unity's float coordinate range.");
            }
            _ = SlotCapacity;
        }

        private void EnsureInitialized()
        {
            if (!_initialized || _instance != this)
                throw new InvalidOperationException("ServerPositionManager is not initialized.");
        }

        private WorldChunkKey CreateKey(ActorSpatialContext context, Vector2Int tileCoordinate)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));
            return new WorldChunkKey(context, tileCoordinate);
        }

        private int FindFreeSlot()
        {
            for (int slotIndex = 0; slotIndex < SlotCapacity; slotIndex++)
            {
                if (!_framesBySlot.ContainsKey(slotIndex))
                    return slotIndex;
            }

            return -1;
        }

        private int NextGeneration(int slotIndex)
        {
            int generation = _slotGenerations[slotIndex];
            if (generation == int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Physical slot {slotIndex} has exhausted its frame generation range and cannot be reused safely.");
            }

            generation++;
            _slotGenerations[slotIndex] = generation;
            return generation;
        }

        private FrameRecord GetRecord(ServerPhysicsFrame frame)
        {
            EnsureInitialized();
            WorldChunkKey key = CreateKey(frame.Context, frame.TileCoordinate);
            if (!_framesByKey.TryGetValue(key, out FrameRecord record)
                || record.Frame != frame)
            {
                throw new InvalidOperationException(
                    $"Physics frame token for context '{frame.Context.MapID}' tile {frame.TileCoordinate} "
                    + "is missing or stale.");
            }

            return record;
        }

        private FrameRecord RequireConvertibleFrame(ServerPhysicsFrame frame)
        {
            FrameRecord record = GetRecord(frame);
            if (record.State == ServerPhysicsFrameState.Retiring)
            {
                throw new InvalidOperationException(
                    $"Physics frame {frame.SlotIndex} is retiring and cannot perform new conversions.");
            }

            return record;
        }

        private void ValidateTileOffset(ServerPhysicsFrame frame, double offsetX, double offsetZ)
        {
            double tileSize = EnvironmentTileSize;
            if (offsetX < -TILE_BOUNDARY_EPSILON || offsetX > tileSize + TILE_BOUNDARY_EPSILON
                || offsetZ < -TILE_BOUNDARY_EPSILON || offsetZ > tileSize + TILE_BOUNDARY_EPSILON)
            {
                throw new ArgumentException(
                    $"Position does not belong to environment tile {frame.TileCoordinate}.");
            }
        }

        private static void ValidateWorldPosition(WorldPosition worldPosition)
        {
            if (!worldPosition.IsFinite)
                throw new ArgumentException("World position must be finite.", nameof(worldPosition));
            if (worldPosition.y < -float.MaxValue || worldPosition.y > float.MaxValue)
                throw new ArgumentException("World Y must fit in Unity's float physics coordinate.", nameof(worldPosition));
        }

        private static Vector3 ToFiniteVector3(double x, double y, double z)
        {
            if (x < -float.MaxValue || x > float.MaxValue
                || y < -float.MaxValue || y > float.MaxValue
                || z < -float.MaxValue || z > float.MaxValue)
            {
                throw new InvalidOperationException("The physics-local position does not fit in Unity's float range.");
            }

            return new Vector3((float)x, (float)y, (float)z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private sealed class FrameRecord
        {
            public FrameRecord(ServerPhysicsFrame frame)
            {
                Frame = frame;
                State = ServerPhysicsFrameState.Reserved;
            }

            public ServerPhysicsFrame Frame { get; }
            public ServerPhysicsFrameState State { get; set; }
            public int ReferenceCount { get; set; }
        }
    }
}
