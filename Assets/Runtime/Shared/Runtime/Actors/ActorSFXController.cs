using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Networking;
using PurrNet;
using System;
using UnityEngine;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>Plays networked spatial sound effects for an actor.</summary>
    [RequireComponent(typeof(Actor))]
    public sealed class ActorSFXController : NetworkBehaviour
    {
        private AudioSource _audioSource;

        private void Start()
        {
            // Initialize audio source for this actor
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f; // 3D audio
            }
        }

        /// <summary>Publishes a transient spatial sound effect to actor observers.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishSFX(string addressablePath)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish actor sound effects.");

            NetworkingPerformanceMetrics.RecordCosmeticObserverPublication(GetComponent<NetworkIdentity>());
            Observers_PlaySFX(addressablePath);
        }

        /// <summary>
        /// Play a spatial sound effect on this actor. Call via RPC from all clients.
        /// </summary>
        [ObserversRpc]
        public void Observers_PlaySFX(string addressablePath)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_SFX_HANDLER_INVOCATION_COUNT);
            PlaySFX(addressablePath);
        }

        /// <summary>
        /// Play a sound effect on this actor's audio source.
        /// Can be called directly or via RPC.
        /// </summary>
        public void PlaySFX(string addressablePath)
        {
            // Invoke the action to play audio on this actor's AudioSource
            ActorEndpoint.onClientPlaySFXOnAudioSource?.Invoke(_audioSource, addressablePath);
        }
    }
}
