using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentTreeViewerPanel</c> implementation.
    /// </summary>
    public sealed class UI_TalentTreeViewerPanel : UI_Panel
    {
        private static UI_TalentTreeViewerPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_TalentTreeViewerPanel Instance => _instance;

        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;
        [FormerlySerializedAs("talentNameText")]
        [SerializeField] private TMP_Text _talentNameText;
        [FormerlySerializedAs("talentDescriptionText")]
        [SerializeField] private TMP_Text _talentDescriptionText;

        protected override void Awake()
        {
            base.Awake();

            _instance = this;

            if (_graphics.material != null)
                _graphics.material = new Material(_graphics.material);
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

            _talentNameText.text = talent.DisplayName;
            _talentDescriptionText.text = talent.Description;

            UIShaderProperties.SetTexture(_graphics, "_IconTex", talent.Icon.texture);
            UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1f);

            Show();
        }
    }
}
