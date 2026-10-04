using System.Collections;
using Game.Shared;
using UnityEngine;

namespace Game.Client.VFX
{
    /// <summary>
    /// Base class for impact effects. Handles lifetime and cleanup of the effect after a certain duration.
    /// </summary>
    public class VFX_ImpactEffect : MonoBehaviour, IVFXFadeIn, IVFXFadeOut
    {
        private Coroutine _lifetimeCoroutine;

        [SerializeField] private float lifetime = 2f;

        private void Start()
        {
            // Additional initialization for impact effect if needed
            _lifetimeCoroutine = StartCoroutine(DestroyAfterLifetime());
        }

        private IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }

        public void VFXFadeIn()
        {
            // nyi
        }

        public void VFXFadeOut()
        {
            // NYI
        }
    }
}