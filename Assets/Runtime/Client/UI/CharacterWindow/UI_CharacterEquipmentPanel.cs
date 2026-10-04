using System.Collections.Generic;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterEquipmentPanel : UI_Panel
    {
        private static UI_CharacterEquipmentPanel _instance;
        public static UI_CharacterEquipmentPanel Instance => _instance;

        [Header("Sub-panels")]
        [SerializeField] private List<UI_Panel> subPanels;

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
            base.OnShow();
        }

        public override void OnHide()
        {

            foreach (UI_Panel panel in subPanels)
            {
                if (panel != null) panel.HideImmediate();
            }
            base.OnHide();
        }
    }
}