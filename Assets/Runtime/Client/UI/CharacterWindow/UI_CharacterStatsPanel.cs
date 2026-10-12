
using Game.Shared;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterStatsPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterStatsPanel : UI_Panel
    {
        private static UI_CharacterStatsPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterStatsPanel Instance => _instance;

        private List<UI_Panel> _subPanels = new();

        protected override void Awake()
        {
            _instance = this;

            foreach (UI_Panel panel in GetComponentsInChildren<UI_Panel>())
            {
                if (panel != this && !_subPanels.Contains(panel))
                    _subPanels.Add(panel);
            }
            base.Awake();
        }

        /// <inheritdoc/>
        public override void OnShow()
        {
            base.OnShow();
            foreach (UI_Panel panel in _subPanels)
            {
                if (panel != null) panel.ShowImmediate();
            }
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            base.OnHide();
            foreach (UI_Panel panel in _subPanels)
            {
                if (panel != null) panel.HideImmediate();
            }
        }

    }
}
