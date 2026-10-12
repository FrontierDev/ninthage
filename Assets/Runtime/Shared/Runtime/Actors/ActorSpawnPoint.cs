using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared
{
    /// <summary>Defines an actor spawn location and spawn settings.</summary>
    public class ActorSpawnPoint : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("guid")] private string _guid;
        [Header("Spawn Point Settings")]
        [SerializeField, FormerlySerializedAs("actor")] private Actor _actor;
        [SerializeField, FormerlySerializedAs("spawnCooldown")] private float _spawnCooldown = 10f;
        [SerializeField, FormerlySerializedAs("spawnOnStart")] private bool _spawnOnStart = true;

        /// <summary>Gets the actor prefab to spawn.</summary>
        public Actor Actor => _actor;
        /// <summary>Gets the delay between spawns.</summary>
        public float SpawnCooldown => _spawnCooldown;
        /// <summary>Gets whether spawning begins automatically.</summary>
        public bool SpawnOnStart => _spawnOnStart;
        /// <summary>Gets the spawn point GUID.</summary>
        public Guid Guid => string.IsNullOrEmpty(_guid) ? Guid.Empty : System.Guid.Parse(_guid);


#if UNITY_EDITOR
        [ContextMenu("Regenerate GUID")]
        private void RegenerateGuid()
        {
            _guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(_guid))
            {
                _guid = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
