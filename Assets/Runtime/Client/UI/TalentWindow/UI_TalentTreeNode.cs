using Game.Shared.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentTreeNode</c> implementation.
    /// </summary>
    public sealed class UI_TalentTreeNode : UI_Button
    {
        [SerializeField] private Image graphics;
        private TalentDefinition talentDefinition;
        private int rank;

        private void Awake()
        {
            graphics.material = new Material(graphics.material);
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(TalentDefinition definition, int rank = 0)
        {
            talentDefinition = definition;
            this.rank = rank;

            UIShaderProperties.SetTexture(graphics, "_IconTex", talentDefinition.Icon.texture);
            UIShaderProperties.SetFloat(graphics, "_IconAmount", 1f);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            UI_TalentTreeViewerPanel.Instance.ShowTalentInfo(talentDefinition);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}