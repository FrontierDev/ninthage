using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared
{
    /// <summary>
    /// NPC spawning data for a single chunk.
    /// </summary>
    public class ChunkSpawnData : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("spawnPoints")] private List<Transform> _spawnPoints;
        [SerializeField, FormerlySerializedAs("prefab")] private GameObject _prefab;
        /// <summary>Gets the configured spawn points.</summary>
        public IReadOnlyList<Transform> SpawnPoints => _spawnPoints;
        /// <summary>Gets the prefab spawned by this chunk.</summary>
        public GameObject Prefab => _prefab;

    }
}
