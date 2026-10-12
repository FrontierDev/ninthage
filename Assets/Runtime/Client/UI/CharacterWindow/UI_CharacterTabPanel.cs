using Debug = Game.Shared.Utility.FormattedDebug;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterTabPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterTabPanel : UI_Panel
    {
        private static UI_CharacterTabPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterTabPanel Instance => _instance;

        [SerializeField] private Dictionary<UI_CharacterTabButton, UI_Panel> tabButtons = new();
        [SerializeField] private List<UI_CharacterTabButton> tabButtonList; // For inspector assignment

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            foreach (UI_CharacterTabButton button in tabButtonList)
            {
                tabButtons.Add(button, button.AssociatedPanel);
                button.Register(this);
            }

            OnTabSelected(tabButtonList[0]);

            base.Awake();
        }

        /// <summary>
        /// Handles the <c>OnTabSelected</c> callback.
        /// </summary>
        public void OnTabSelected(UI_CharacterTabButton selectedButton)
        {
            foreach (var kvp in tabButtons)
            {
                bool isSelected = kvp.Key == selectedButton;
                UIShaderProperties.SetFloat(kvp.Key.TabGraphics, "_IsActive", isSelected ? 1f : 0f);

                if (isSelected)
                {
                    if (kvp.Value != null) kvp.Value.ShowImmediate();
                    kvp.Key.TabText.color = Utility.UIConstants.PrimaryText; // Set active tab text color
                }
                else
                {
                    if (kvp.Value != null) kvp.Value.HideImmediate();
                    kvp.Key.TabText.color = Utility.UIConstants.SecondaryText; // Set inactive tab text color
                }
            }
        }
    }
}
