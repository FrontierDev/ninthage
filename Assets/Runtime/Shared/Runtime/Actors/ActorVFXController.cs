using System.Collections;
using System.Collections.Generic;
using Game.Shared.Networking;
using PurrNet;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    [RequireComponent(typeof(Actor))]
    public sealed class ActorVFXController : NetworkBehaviour
    {
        [SerializeField] private Transform spellEffectSpawnPoint;
        public Transform SpellEffectSpawnPoint => spellEffectSpawnPoint;

        [SerializeField] private Dictionary<int, GameObject> activeVFX = new();
        public IReadOnlyDictionary<int, GameObject> ActiveEffects => activeVFX;

        public void RegisterEffect(int id, GameObject go)
        {
            activeVFX[id] = go;
        }

        /// <summary>
        /// Spawns a prefab on all clients at the given position. Reusable for any
        /// ground-placed visual effect (zones, portals, AoE indicators, etc.).
        /// </summary>
        /// <param name="addressablePath">Addressable key for the prefab to instantiate.</param>
        /// <param name="position">Physics-space world position.</param>
        /// <param name="lifetime">Seconds until the object is auto-destroyed. 0 = prefab manages its own lifecycle.</param>
        [ObserversRpc]
        public void Observer_SpawnEffectAtPosition(int id, string addressablePath, Vector3 position, float lifetime)
        {
            ActorService.onClientSpawnVFXAtPosition?.Invoke(this, id, addressablePath, position, lifetime);
        }

        /// <summary>
        /// <summary>
        /// Spawns a projectile on clients for spell effects that use projectiles.
        /// The projectile is initialized to move towards the target's projectile target transform.
        /// Trajectory properties (arc, deviation) are configured on the projectile prefab.
        /// </summary>
        [ObserversRpc]
        public void Observer_SpawnProjectile(Actor target, string addressablePath, float speed)
        {
            ActorService.onClientSpawnProjectile?.Invoke(this, target, addressablePath, speed);
        }

        /// <summary>
        /// Tells all clients to fire the one-shot trigger effect on the VFX
        /// registered under <paramref name="id"/>. Only that specific effect is triggered.
        /// </summary>
        [ObserversRpc]
        public void Observer_TriggerEffect(int id)
        {
            ActorService.onClientTriggerEffect?.Invoke(this, id);
        }
    }
}
