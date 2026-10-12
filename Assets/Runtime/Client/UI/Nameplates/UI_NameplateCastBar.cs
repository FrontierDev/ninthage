using System.Collections;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_NameplateCastBar</c> implementation.
    /// </summary>
    public sealed class UI_NameplateCastBar : UI_ProgressBar
    {
        private Coroutine _activeCoroutine;

        private void Awake() => SetMaterial(graphics.material);

        /// <summary>
        /// Executes the client-side <c>StartCast</c> operation.
        /// </summary>
        public void StartCast(float duration, bool reverse = false)
        {
            StopActiveCast();
            Show();
            _activeCoroutine = StartCoroutine(CastBarCoroutine(duration, reverse));
        }

        /// <summary>
        /// Executes the client-side <c>InterruptCast</c> operation.
        /// </summary>
        public void InterruptCast()
        {
            StopActiveCast();
            Hide();
        }

        /// <summary>
        /// Executes the client-side <c>FinishCast</c> operation.
        /// </summary>
        public void FinishCast()
        {
            StopActiveCast();
            Hide();
        }

        private void StopActiveCast()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
        }

        private IEnumerator CastBarCoroutine(float duration, bool reverse)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                SetProgress(reverse ? 1f - t : t);
                yield return null;
            }
            SetProgress(reverse ? 0f : 1f);
            _activeCoroutine = null;
        }
    }
}
