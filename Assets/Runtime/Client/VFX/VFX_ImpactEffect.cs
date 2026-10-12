using Game.Shared;
using Game.Shared.Runtime.VFX;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.VFX
{
    /// <summary>
    /// Base class for impact effects. Handles lifetime and cleanup of the effect after a certain duration.
    /// </summary>
    public class VFX_ImpactEffect : MonoBehaviour, IVFXFadeIn, IVFXFadeOut
    {
        private Coroutine _lifetimeCoroutine;

        [FormerlySerializedAs("lifetime")]
        [SerializeField] private float _lifetime = 2f;

        private void Start()
        {
            // Additional initialization for impact effect if needed
            _lifetimeCoroutine = StartCoroutine(DestroyAfterLifetime());
        }

        private IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(_lifetime);
            Destroy(gameObject);
        }

        /// <summary>
        /// Executes the client-side <c>VFXFadeIn</c> operation.
        /// </summary>
        public void VFXFadeIn()
        {
            // nyi
        }

        /// <summary>
        /// Executes the client-side <c>VFXFadeOut</c> operation.
        /// </summary>
        public void VFXFadeOut()
        {
            // NYI
        }
    }
}
