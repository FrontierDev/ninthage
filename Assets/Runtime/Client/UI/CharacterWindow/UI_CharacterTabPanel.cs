using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.UI.Configuration;
using Game.Client.UI.Skin;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("tabButtons")]
        [SerializeField] private Dictionary<UI_CharacterTabButton, UI_Panel> _tabButtons = new();
        [FormerlySerializedAs("tabButtonList")]
        [SerializeField] private List<UI_CharacterTabButton> _tabButtonList; // For inspector assignment
        private UI_Skin _skin;

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            _skin = GetComponentInParent<UI_Skin>(true);
            if (_skin == null)
                throw new InvalidOperationException("Character tab panel requires a parent UI_Skin.");
            if (_skin.Configuration == null)
                throw new InvalidOperationException("Character tab panel parent UI_Skin has no UIConfiguration.");

            foreach (UI_CharacterTabButton button in _tabButtonList)
            {
                _tabButtons.Add(button, button.AssociatedPanel);
                button.Register(this);
            }

            OnTabSelected(_tabButtonList[0]);

            base.Awake();
        }

        /// <summary>
        /// Handles the <c>OnTabSelected</c> callback.
        /// </summary>
        public void OnTabSelected(UI_CharacterTabButton selectedButton)
        {
            foreach (var kvp in _tabButtons)
            {
                bool isSelected = kvp.Key == selectedButton;
                kvp.Key.EnsureMaterialInstance();
                UIShaderProperties.SetTabActive(kvp.Key.TabGraphics, isSelected);

                if (isSelected)
                {
                    if (kvp.Value != null) kvp.Value.ShowImmediate();
                    kvp.Key.TabText.color = _skin.Configuration.GetColor(UITokens.Color.PrimaryText);
                }
                else
                {
                    if (kvp.Value != null) kvp.Value.HideImmediate();
                    kvp.Key.TabText.color = _skin.Configuration.GetColor(UITokens.Color.SecondaryText);
                }
            }
        }
    }
}
