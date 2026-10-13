using UnityEngine;

namespace Game.Tests.Performance.Fixtures
{
    /// <summary>Stores the deterministic fixture contract on each generated scene root.</summary>
    public sealed class Test_WorldFixtureMetadata : MonoBehaviour
    {
        [SerializeField] private string _fixtureVersion;
        [SerializeField] private string _fixtureID;
        [SerializeField] private string _variant;
        [SerializeField] private string _status;
        [SerializeField] private int _seed;
        [SerializeField] private int _originCellSizeMeters;
        [SerializeField] private int _worldTileSizeMeters;
        [SerializeField] private int _worldTileX;
        [SerializeField] private int _worldTileZ;
        [SerializeField] private int _objectCount;
        [SerializeField] private bool _isNavigable;

        public string FixtureVersion => _fixtureVersion;

        public string FixtureID => _fixtureID;

        public string Variant => _variant;

        public string Status => _status;

        public int Seed => _seed;

        public int OriginCellSizeMeters => _originCellSizeMeters;

        public int WorldTileSizeMeters => _worldTileSizeMeters;

        public int WorldTileX => _worldTileX;

        public int WorldTileZ => _worldTileZ;

        public int ObjectCount => _objectCount;

        public bool IsNavigable => _isNavigable;

        /// <summary>Copies manifest identity and coordinate-contract data to the scene root.</summary>
        public void Set(Test_WorldFixturePlan plan, int seed, string fixtureVersion)
        {
            _fixtureVersion = fixtureVersion;
            _fixtureID = plan.FixtureID;
            _variant = plan.Variant;
            _status = plan.Status;
            _seed = seed;
            _originCellSizeMeters = Test_WorldFixtureLayout.ORIGIN_CELL_SIZE_METERS;
            _worldTileSizeMeters = Test_WorldFixtureLayout.WORLD_TILE_SIZE_METERS;
            _worldTileX = plan.WorldTileX;
            _worldTileZ = plan.WorldTileZ;
            _objectCount = plan.ObjectCount;
            _isNavigable = plan.IsNavigable;
        }
    }

    /// <summary>Stores deterministic spawn metadata on generated geometry roots.</summary>
    public sealed class Test_WorldFixtureSpawnMetadata : MonoBehaviour
    {
        [SerializeField] private string _spawnID;
        [SerializeField] private string _kind;
        [SerializeField] private string _primitive;
        [SerializeField] private double _worldX;
        [SerializeField] private double _worldY;
        [SerializeField] private double _worldZ;
        [SerializeField] private int _originCellX;
        [SerializeField] private int _originCellZ;
        [SerializeField] private int _worldTileX;
        [SerializeField] private int _worldTileZ;
        [SerializeField] private int _materialVariant;
        [SerializeField] private int _lodLevels;
        [SerializeField] private bool _hasCollision;

        public string SpawnID => _spawnID;

        public string Kind => _kind;

        public string Primitive => _primitive;

        public double WorldX => _worldX;

        public double WorldY => _worldY;

        public double WorldZ => _worldZ;

        public int OriginCellX => _originCellX;

        public int OriginCellZ => _originCellZ;

        public int WorldTileX => _worldTileX;

        public int WorldTileZ => _worldTileZ;

        public int MaterialVariant => _materialVariant;

        public int LODLevels => _lodLevels;

        public bool HasCollision => _hasCollision;

        /// <summary>Copies canonical spawn metadata to a generated geometry root.</summary>
        public void Set(Test_WorldFixtureSpawn spawn)
        {
            _spawnID = spawn.SpawnID;
            _kind = spawn.Kind;
            _primitive = spawn.Primitive;
            _worldX = spawn.WorldX;
            _worldY = spawn.WorldY;
            _worldZ = spawn.WorldZ;
            _originCellX = spawn.OriginCellX;
            _originCellZ = spawn.OriginCellZ;
            _worldTileX = spawn.WorldTileX;
            _worldTileZ = spawn.WorldTileZ;
            _materialVariant = spawn.MaterialVariant;
            _lodLevels = spawn.LODLevels;
            _hasCollision = spawn.HasCollision;
        }
    }
}
