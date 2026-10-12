using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentTreeViewerPanel</c> implementation.
    /// </summary>
    public sealed class UI_TalentTreeViewerPanel : UI_Panel
    {
        private static UI_TalentTreeViewerPanel instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_TalentTreeViewerPanel Instance => instance;

        [SerializeField] private Image graphics;
        [SerializeField] private TMP_Text talentNameText;
        [SerializeField] private TMP_Text talentDescriptionText;

        protected override void Awake()
        {
            base.Awake();

            instance = this;

            if (graphics.material != null)
                graphics.material = new Material(graphics.material);
        }

        /// <summary>
        /// Executes the client-side <c>ShowTalentInfo</c> operation.
        /// </summary>
        public void ShowTalentInfo(TalentDefinition talent)
        {
            if (talent == null)
            {
                Hide();
                return;
            }

            talentNameText.text = talent.DisplayName;
            talentDescriptionText.text = talent.Description;

            UIShaderProperties.SetTexture(graphics, "_IconTex", talent.Icon.texture);
            UIShaderProperties.SetFloat(graphics, "_IconAmount", 1f);

            Show();
        }
    }
}