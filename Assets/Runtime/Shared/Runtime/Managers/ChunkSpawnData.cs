using UnityEngine;
using System.Collections.Generic;

namespace Game.Shared
{
    /// <summary>
    /// NPC spawning data for a single chunk.
    /// </summary>
    public class ChunkSpawnData : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private GameObject prefab;
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
        public GameObject Prefab => prefab;

    }
}