using Game.Shared.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentTreeNode</c> implementation.
    /// </summary>
    public sealed class UI_TalentTreeNode : UI_Button
    {
        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;
        private TalentDefinition _talentDefinition;
        private int _rank;
        private Material _materialInstance;

        private void Awake()
        {
            if (_graphics != null && _graphics.material != null)
            {
                _materialInstance = new Material(_graphics.material);
                _graphics.material = _materialInstance;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
                Destroy(_materialInstance);
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(TalentDefinition definition, int rank = 0)
        {
            _talentDefinition = definition;
            this._rank = rank;

            UIShaderProperties.SetIcon(_graphics, _talentDefinition.Icon.texture);
            UIShaderProperties.SetIconVisible(_graphics, true);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            UI_TalentTreeViewerPanel.Instance.ShowTalentInfo(_talentDefinition);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
