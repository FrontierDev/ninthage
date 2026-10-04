using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_CharacterTabButton : UI_Button
    {
        [SerializeField] private UI_Panel associatedPanel;
        [SerializeField] private TMP_Text tabText;
        [SerializeField] private Image tabGraphics;
        public Image TabGraphics => tabGraphics;
        public UI_Panel AssociatedPanel => associatedPanel;
        public TMP_Text TabText => tabText;

        private UI_CharacterTabPanel characterTabPanel;

        private void Awake()
        {
            if (tabGraphics != null && tabGraphics.material != null)
                tabGraphics.material = new Material(tabGraphics.material);
        }

        public void Register(UI_CharacterTabPanel panel)
        {
            characterTabPanel = panel;
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            characterTabPanel.OnTabSelected(this);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}