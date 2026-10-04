using System.Collections.Generic;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public sealed class UI_CharacterTabPanel : UI_Panel
    {
        private static UI_CharacterTabPanel _instance;
        public static UI_CharacterTabPanel Instance => _instance;

        [SerializeField] private Dictionary<UI_CharacterTabButton, UI_Panel> tabButtons = new Dictionary<UI_CharacterTabButton, UI_Panel>();
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