using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_SpecialResourceCounter : UI_ListEntry<Sprite>
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;

        private int index;

        public override void Initialize(Sprite sprite, int slotId)
        {
            background.sprite = sprite;
            icon.sprite = sprite;
            index = slotId;
        }

        public void SetFilled(bool filled)
        {
            if (icon != null)
                icon.enabled = filled;
        }

        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
        }

        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
        }
    }
}