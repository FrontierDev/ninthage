using UnityEngine;

namespace Game.Tests.Performance.Fixtures
{
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
