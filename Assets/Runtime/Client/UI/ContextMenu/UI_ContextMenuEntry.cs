using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Game.Client.UI
{
    public sealed class UI_ContextMenuEntry : UI_ListEntry
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        private Action onClick;

        public void Initialize(string displayName, Sprite icon, Action onClickCallback)
        {
            nameText.text = displayName;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            onClick = onClickCallback;
        }

        public override void OnClick(PointerEventData eventData)
        {
            onClick?.Invoke();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
