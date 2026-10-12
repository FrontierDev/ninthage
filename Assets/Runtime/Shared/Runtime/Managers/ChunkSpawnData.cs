using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>
    /// NPC spawning data for a single chunk.
    /// </summary>
    public class ChunkSpawnData : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private GameObject prefab;
        /// <summary>Gets the configured spawn points.</summary>
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
        /// <summary>Gets the prefab spawned by this chunk.</summary>
        public GameObject Prefab => prefab;

    }
}
