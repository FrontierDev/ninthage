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

        private void Awake()
        {
            _graphics.material = new Material(_graphics.material);
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(TalentDefinition definition, int rank = 0)
        {
            _talentDefinition = definition;
            this._rank = rank;

            UIShaderProperties.SetTexture(_graphics, "_IconTex", _talentDefinition.Icon.texture);
            UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1f);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            UI_TalentTreeViewerPanel.Instance.ShowTalentInfo(_talentDefinition);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
