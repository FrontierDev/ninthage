using System;
using System.Collections;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public abstract class UI_Window : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] internal string windowName; // Optional: Name of the window for identification purposes.
        public string WindowName => windowName; // Public getter for window name.

        [SerializeField] protected bool startVisible = false; // Optional: Whether the window should be visible at the start.

        [SerializeField] protected float fadeInDuration = 0f;
        [SerializeField] protected float fadeOutDuration = 0.3f;

        [Header("UI References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected UI_Panel[] panels;


        // Actions
        internal Action onShowWindow;
        internal Action onHideWindow;

        protected virtual void Start()
        {
            if (Game.Shared.Runtime.IsServer()) return;

            // Get panels in children.
            panels = GetComponentsInChildren<UI_Panel>(true);

            // Set callbacks.
            onShowWindow += OnShow;
            onHideWindow += OnHide;

            // Set initial visibility.
            if (startVisible) ShowImmediate();
            else HideImmediate();
        }

        #region Window Visibility
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

        private void OnShow()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            UI_Manager.Instance.OnShowWindow(this);
        }

        private void OnHide()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            UI_Manager.Instance.OnHideWindow(this);
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
        #endregion

        #region Panel Management
        public virtual bool TryGetPanel(string panelName, out UI_Panel panel)
        {
            panel = Array.Find(panels, p => p.PanelName == panelName);
            if (panel == null)
            {
                Debug.Error($"Panel with name '{panelName}' not found in window '{windowName}'.");
                return false;
            }
            return true;
        }

        public virtual void ShowPanel(string panelName)
        {
            if (TryGetPanel(panelName, out var panel)) panel.Show();
        }

        public virtual void HidePanel(string panelName)
        {
            if (TryGetPanel(panelName, out var panel)) panel.Hide();
        }

        public virtual void TogglePanel(string panelName)
        {
            if (TryGetPanel(panelName, out var panel)) panel.Toggle();
        }
        #endregion
    }
}