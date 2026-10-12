using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CastBar</c> implementation.
    /// </summary>
    public sealed class UI_CastBar : UI_ProgressBar
    {
        private string _parentWindowName;
        private Coroutine _activeCoroutine;

        [FormerlySerializedAs("defaultMaterial")]
        [SerializeField] private Material _defaultMaterial;

        private void Awake()
        {
            var parentWindow = GetComponentInParent<UI_Window>(true);
            _parentWindowName = parentWindow.WindowName;

            SetMaterial(_defaultMaterial);
        }

        /// <summary>
        /// Executes the client-side <c>StartCast</c> operation.
        /// </summary>
        public void StartCast(float duration, bool reverse = false)
        {
            StopActiveCast();
            UI_Manager.Instance.ShowWindow(_parentWindowName);
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
            UI_Manager.Instance.HideWindow(_parentWindowName);
        }

        /// <summary>
        /// Executes the client-side <c>FinishCast</c> operation.
        /// </summary>
        public void FinishCast()
        {
            StopActiveCast();
            Hide();
            UI_Manager.Instance.HideWindow(_parentWindowName);
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
