using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_SpecialResourceCounter</c> implementation.
    /// </summary>
    public sealed class UI_SpecialResourceCounter : UI_ListEntry<Sprite>
    {
        [FormerlySerializedAs("background")]
        [SerializeField] private Image _background;
        [FormerlySerializedAs("icon")]
        [SerializeField] private Image _icon;

        private int _index;

        /// <inheritdoc/>
        public override void Initialize(Sprite sprite, int slotID)
        {
            _background.sprite = sprite;
            _icon.sprite = sprite;
            _index = slotID;
        }

        /// <summary>
        /// Sets the value managed by <c>SetFilled</c>.
        /// </summary>
        public void SetFilled(bool filled)
        {
            if (_icon != null)
                _icon.enabled = filled;
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
