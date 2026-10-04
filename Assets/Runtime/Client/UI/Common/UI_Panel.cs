using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

namespace Game.Client.UI
{

    /// <summary>
    /// Base class for UI panels. Panels are components that can be shown or hidden within a UI window. 
    /// They can contain various UI elements such as buttons, text, images, etc. 
    /// This class can be extended to create specific types of panels with custom behavior and appearance.
    /// </summary>
    public abstract class UI_Panel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] protected string panelName; // Optional: Name of the panel for identification purposes.
        public string PanelName => panelName; // Public getter for panel name.
        [SerializeField] protected bool startVisible = false; // Optional: Whether the panel should be visible at the start.
        [SerializeField] protected float fadeInDuration = 0f;
        [SerializeField] protected float fadeOutDuration = 0.3f;

        [Header("UI References")]
        [SerializeField] protected CanvasGroup canvasGroup;

        // Actions
        private Action onShowWindow;
        private Action onHideWindow;

        protected virtual void Awake()
        {
            if (Game.Shared.Runtime.IsServer()) return;

            onShowWindow += OnShow;
            onHideWindow += OnHide;

            if (startVisible) ShowImmediate();
            else HideImmediate();
        }

        public virtual void Show()
        {
            StartCoroutine(Fade(1f, fadeInDuration));
            onShowWindow?.Invoke();
        }

        public virtual void ShowImmediate()
        {
            canvasGroup.alpha = 1f;
            onShowWindow?.Invoke();
        }

        public virtual void Hide()
        {
            StartCoroutine(Fade(0f, fadeOutDuration));
            onHideWindow?.Invoke();
        }

        public virtual void HideImmediate()
        {
            canvasGroup.alpha = 0f;
            onHideWindow?.Invoke();
        }

        public virtual void Toggle()
        {
            if (canvasGroup.interactable)
                Hide();
            else
                Show();
        }

        private IEnumerator Fade(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        public virtual void OnShow()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public virtual void OnHide()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public virtual void Refresh()
        {
            // Optional: Override in derived classes to refresh panel content when shown.
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}