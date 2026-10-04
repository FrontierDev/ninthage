using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public enum ContextMenuPivot
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public sealed class UI_ContextMenuWindow : UI_Window, IPointerClickHandler
    {
        private static UI_ContextMenuWindow _instance;
        public static UI_ContextMenuWindow Instance => _instance;
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        [SerializeField] private UI_ContextMenuPanel contentsPanel;
        [SerializeField] private Image backdropBlocker;

        private RectTransform panelRect;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            _initialized = true;
            panelRect = contentsPanel.GetComponent<RectTransform>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, eventData.position))
                Hide();
        }

        public void Show(Vector2 position, ContextMenuPivot pivot, GameObject entryPrefab, Action<UI_ContextMenuPanel> populate)
        {
            panelRect.pivot = pivot switch
            {
                ContextMenuPivot.TopLeft => new Vector2(0f, 1f),
                ContextMenuPivot.TopRight => new Vector2(1f, 1f),
                ContextMenuPivot.BottomLeft => new Vector2(0f, 0f),
                ContextMenuPivot.BottomRight => new Vector2(1f, 0f),
                _ => new Vector2(0f, 1f)
            };
            contentsPanel.Populate(entryPrefab, populate);
            panelRect.position = position;
            Show();
        }

        public override void Hide()
        {
            contentsPanel.PopulateList(true);
            base.Hide();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }
    }
}