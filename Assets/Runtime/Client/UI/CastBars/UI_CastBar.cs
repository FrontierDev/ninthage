using System.Collections;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CastBar : UI_ProgressBar
    {
        private string parentWindowName;
        private Coroutine _activeCoroutine;

        [SerializeField] private Material defaultMaterial;

        private void Awake()
        {
            var parentWindow = GetComponentInParent<UI_Window>(true);
            parentWindowName = parentWindow.WindowName;

            SetMaterial(defaultMaterial);
        }

        public void StartCast(float duration, bool reverse = false)
        {
            StopActiveCast();
            UI_Manager.Instance.ShowWindow(parentWindowName);
            Show();
            _activeCoroutine = StartCoroutine(CastBarCoroutine(duration, reverse));
        }

        public void InterruptCast()
        {
            StopActiveCast();
            Hide();
            UI_Manager.Instance.HideWindow(parentWindowName);
        }

        public void FinishCast()
        {
            StopActiveCast();
            Hide();
            UI_Manager.Instance.HideWindow(parentWindowName);
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