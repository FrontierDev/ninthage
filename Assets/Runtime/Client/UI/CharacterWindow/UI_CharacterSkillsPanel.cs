using System.Collections.Generic;
using Game.Shared;

namespace Game.Client.UI
{
    public sealed class UI_CharacterSkillsPanel : UI_Panel
    {
        private static UI_CharacterSkillsPanel _instance;
        public static UI_CharacterSkillsPanel Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

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