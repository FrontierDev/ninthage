using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_SpecialResourceCounter</c> implementation.
    /// </summary>
    public sealed class UI_SpecialResourceCounter : UI_ListEntry<Sprite>
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;

        private int index;

        /// <inheritdoc/>
        public override void Initialize(Sprite sprite, int slotId)
        {
            background.sprite = sprite;
            icon.sprite = sprite;
            index = slotId;
        }

        /// <summary>
        /// Sets the value managed by <c>SetFilled</c>.
        /// </summary>
        public void SetFilled(bool filled)
        {
            if (icon != null)
                icon.enabled = filled;
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
        }
    }
}