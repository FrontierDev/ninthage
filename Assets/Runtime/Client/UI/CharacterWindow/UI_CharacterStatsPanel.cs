
using System;
using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterStatsPanel : UI_Panel
    {
        private static UI_CharacterStatsPanel _instance;
        public static UI_CharacterStatsPanel Instance => _instance;

        private List<UI_Panel> subPanels = new List<UI_Panel>();

        protected override void Awake()
        {
            _instance = this;

            foreach (UI_Panel panel in GetComponentsInChildren<UI_Panel>())
            {
                if (panel != this && !subPanels.Contains(panel))
                    subPanels.Add(panel);
            }
            base.Awake();
        }

        public override void OnShow()
        {
            base.OnShow();
            foreach (UI_Panel panel in subPanels)
            {
                if (panel != null) panel.ShowImmediate();
            }
        }

        public override void OnHide()
        {
            base.OnHide();
            foreach (UI_Panel panel in subPanels)
            {
                if (panel != null) panel.HideImmediate();
            }
        }

    }
}