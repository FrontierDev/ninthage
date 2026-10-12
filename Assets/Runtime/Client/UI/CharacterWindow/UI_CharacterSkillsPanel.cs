using Game.Shared;
using System.Collections.Generic;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterSkillsPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterSkillsPanel : UI_Panel
    {
        private static UI_CharacterSkillsPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterSkillsPanel Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        private List<UI_Panel> subPanels = new();

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

        /// <inheritdoc/>
        public override void OnShow()
        {
            base.OnShow();
            foreach (UI_Panel panel in subPanels)
            {
                if (panel != null) panel.ShowImmediate();
            }
        }

        /// <inheritdoc/>
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
