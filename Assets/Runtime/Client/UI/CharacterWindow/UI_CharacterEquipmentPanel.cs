using Game.Shared.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterEquipmentPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterEquipmentPanel : UI_Panel
    {
        private static UI_CharacterEquipmentPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterEquipmentPanel Instance => _instance;

        [Header("Sub-panels")]
        [FormerlySerializedAs("subPanels")]
        [SerializeField] private List<UI_Panel> _subPanels;

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
            base.OnShow();
        }

        /// <inheritdoc/>
        public override void OnHide()
        {

            foreach (UI_Panel panel in _subPanels)
            {
                if (panel != null) panel.HideImmediate();
            }
            base.OnHide();
        }
    }
}
