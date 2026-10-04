using System;
using UnityEngine;

namespace Game.Shared
{
    public class ActorSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string guid;
        [Header("Spawn Point Settings")]
        [SerializeField] private Actor actor;
        [SerializeField] private float spawnCooldown = 10f;
        [SerializeField] private bool spawnOnStart = true;

        public Actor Actor => actor;
        public float SpawnCooldown => spawnCooldown;
        public bool SpawnOnStart => spawnOnStart;
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