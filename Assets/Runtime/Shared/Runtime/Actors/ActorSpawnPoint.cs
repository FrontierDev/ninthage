using System;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Defines an actor spawn location and spawn settings.</summary>
    public class ActorSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string guid;
        [Header("Spawn Point Settings")]
        [SerializeField] private Actor actor;
        [SerializeField] private float spawnCooldown = 10f;
        [SerializeField] private bool spawnOnStart = true;

        /// <summary>Gets the actor prefab to spawn.</summary>
        public Actor Actor => actor;
        /// <summary>Gets the delay between spawns.</summary>
        public float SpawnCooldown => spawnCooldown;
        /// <summary>Gets whether spawning begins automatically.</summary>
        public bool SpawnOnStart => spawnOnStart;
        /// <summary>Gets the spawn point GUID.</summary>
        public Guid Guid => string.IsNullOrEmpty(guid) ? Guid.Empty : System.Guid.Parse(guid);


#if UNITY_EDITOR
        [ContextMenu("Regenerate GUID")]
        private void RegenerateGuid()
        {
            guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
