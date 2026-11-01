using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterTabButton</c> implementation.
    /// </summary>
    public sealed class UI_CharacterTabButton : UI_Button
    {
        [FormerlySerializedAs("associatedPanel")]
        [SerializeField] private UI_Panel _associatedPanel;
        [FormerlySerializedAs("tabText")]
        [SerializeField] private TMP_Text _tabText;
        [FormerlySerializedAs("tabGraphics")]
        [SerializeField] private Image _tabGraphics;
        /// <summary>
        /// Gets the <c>TabGraphics</c> value.
        /// </summary>
        public Image TabGraphics => _tabGraphics;
        /// <summary>
        /// Gets the <c>AssociatedPanel</c> value.
        /// </summary>
        public UI_Panel AssociatedPanel => _associatedPanel;
        /// <summary>
        /// Gets the <c>TabText</c> value.
        /// </summary>
        public TMP_Text TabText => _tabText;

        private UI_CharacterTabPanel _characterTabPanel;
        private Material _materialInstance;

        private void Awake()
        {
            EnsureMaterialInstance();
        }

        internal void EnsureMaterialInstance()
        {
            if (_materialInstance != null || _tabGraphics == null || _tabGraphics.material == null)
                return;

            _materialInstance = new Material(_tabGraphics.material);
            _tabGraphics.material = _materialInstance;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
                Destroy(_materialInstance);
        }

        /// <summary>
        /// Executes the client-side <c>Register</c> operation.
        /// </summary>
        public void Register(UI_CharacterTabPanel panel)
        {
            _characterTabPanel = panel;
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            _characterTabPanel.OnTabSelected(this);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
