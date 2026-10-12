using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ContextMenuEntry</c> implementation.
    /// </summary>
    public sealed class UI_ContextMenuEntry : UI_ListEntry
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        private Action onClick;

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(string displayName, Sprite icon, Action onClickCallback)
        {
            nameText.text = displayName;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            onClick = onClickCallback;
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            onClick?.Invoke();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
