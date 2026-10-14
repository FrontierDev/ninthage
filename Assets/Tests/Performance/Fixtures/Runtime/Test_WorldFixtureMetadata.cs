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

}
