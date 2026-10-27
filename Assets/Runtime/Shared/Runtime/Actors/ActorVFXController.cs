using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Networking;
using Game.Shared.Utility;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>Coordinates networked actor visual effects.</summary>
    [RequireComponent(typeof(Actor))]
    public sealed class ActorVFXController : NetworkBehaviour
    {
        [SerializeField, FormerlySerializedAs("spellEffectSpawnPoint")] private Transform _spellEffectSpawnPoint;
        /// <summary>Gets the spell effect spawn point.</summary>
        public Transform SpellEffectSpawnPoint => _spellEffectSpawnPoint;

        [SerializeField, FormerlySerializedAs("activeVFX")] private Dictionary<int, GameObject> _activeVFX = new();
        /// <summary>Gets active visual effects by identifier.</summary>
        public IReadOnlyDictionary<int, GameObject> ActiveEffects => _activeVFX;

        /// <summary>Registers a visual effect by identifier.</summary>
        public void RegisterEffect(int id, GameObject go)
        {
            _activeVFX[id] = go;
        }

        /// <summary>Publishes a transient effect at an authoritative world position to actor observers.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishEffectAtPosition(int id, string addressablePath, WorldPosition position, float lifetime)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish actor visual effects.");

            NetworkingPerformanceMetrics.RecordCosmeticObserverPublication(GetComponent<NetworkIdentity>());
            Observers_SpawnEffectAtPosition(id, addressablePath, position, lifetime);
        }

        /// <summary>Publishes a transient projectile effect to actor observers.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishProjectile(Actor target, string addressablePath, float speed)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish actor visual effects.");

            NetworkingPerformanceMetrics.RecordCosmeticObserverPublication(GetComponent<NetworkIdentity>());
            Observers_SpawnProjectile(target, addressablePath, speed);
        }

        /// <summary>Publishes a transient trigger effect to actor observers.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishTriggerEffect(int id)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish actor visual effects.");

            NetworkingPerformanceMetrics.RecordCosmeticObserverPublication(GetComponent<NetworkIdentity>());
            Observers_TriggerEffect(id);
        }

        /// <summary>
        /// Spawns a prefab on all clients at the given position. Reusable for any
        /// ground-placed visual effect (zones, portals, AoE indicators, etc.).
        /// </summary>
        /// <param name="addressablePath">Addressable key for the prefab to instantiate.</param>
        /// <param name="position">Authoritative absolute world position.</param>
        /// <param name="lifetime">Seconds until the object is auto-destroyed. 0 = prefab manages its own lifecycle.</param>
        [ObserversRpc(channel: Channel.UnreliableSequenced)]
        public void Observers_SpawnEffectAtPosition(int id, string addressablePath, WorldPosition position, float lifetime)
        {
            if (!position.IsFinite)
            {
                Debug.Error($"Cannot spawn effect {id} ('{addressablePath}') at a non-finite world position.");
                return;
            }

            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT);
            ActorEndpoint.onClientSpawnVFXAtPosition?.Invoke(this, id, addressablePath, position, lifetime);
        }

        /// <summary>
        /// <summary>
        /// Spawns a projectile on clients for spell effects that use projectiles.
        /// The projectile is initialized to move towards the target's projectile target transform.
        /// Trajectory properties (arc, deviation) are configured on the projectile prefab.
        /// </summary>
        [ObserversRpc(channel: Channel.UnreliableSequenced)]
        public void Observers_SpawnProjectile(Actor target, string addressablePath, float speed)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT);
            ActorEndpoint.onClientSpawnProjectile?.Invoke(this, target, addressablePath, speed);
        }

        /// <summary>
        /// Tells all clients to fire the one-shot trigger effect on the VFX
        /// registered under <paramref name="id"/>. Only that specific effect is triggered.
        /// </summary>
        [ObserversRpc(channel: Channel.UnreliableSequenced)]
        public void Observers_TriggerEffect(int id)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_VFX_HANDLER_INVOCATION_COUNT);
            ActorEndpoint.onClientTriggerEffect?.Invoke(this, id);
        }
    }
}
