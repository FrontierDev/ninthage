using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterTabButton</c> implementation.
    /// </summary>
    public sealed class UI_CharacterTabButton : UI_Button
    {
        [SerializeField] private UI_Panel associatedPanel;
        [SerializeField] private TMP_Text tabText;
        [SerializeField] private Image tabGraphics;
        /// <summary>
        /// Gets the <c>TabGraphics</c> value.
        /// </summary>
        public Image TabGraphics => tabGraphics;
        /// <summary>
        /// Gets the <c>AssociatedPanel</c> value.
        /// </summary>
        public UI_Panel AssociatedPanel => associatedPanel;
        /// <summary>
        /// Gets the <c>TabText</c> value.
        /// </summary>
        public TMP_Text TabText => tabText;

        private UI_CharacterTabPanel characterTabPanel;

        private void Awake()
        {
            if (tabGraphics != null && tabGraphics.material != null)
                tabGraphics.material = new Material(tabGraphics.material);
        }

        /// <summary>
        /// Executes the client-side <c>Register</c> operation.
        /// </summary>
        public void Register(UI_CharacterTabPanel panel)
        {
            characterTabPanel = panel;
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            characterTabPanel.OnTabSelected(this);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}