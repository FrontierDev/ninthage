using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_ReputationRewardEntry : UI_ListEntry<FactionRewardDescriptionEntry>
    {
        private int index;
        private FactionRewardDescriptionEntry data;

        [SerializeField] private TMP_Text header;
        [SerializeField] private TMP_Text entryText;
        [SerializeField] private TMP_Text thresholdText;
        [SerializeField] private Image thresholdIcon;
        [SerializeField] private Image graphics;

        private void Awake()
        {
            graphics.material = new Material(graphics.material);
        }

        public override void Initialize(FactionRewardDescriptionEntry data, int index)
        {
            this.data = data;
            this.index = index;

            header.text = data.title;
            thresholdIcon.sprite = data.icon;

            header.color = data.isPositive ? new Color(0.22f, 0.725f, 0.525f) : new Color(0.91f, 0.541f, 0.322f);
            entryText.text = string.Empty;

            foreach (var benefit in data.benefits)
            {
                entryText.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.22f, 0.725f, 0.525f))}>- {benefit}</color>\n";
            }

            foreach (var penalty in data.penalties)
            {
                entryText.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.91f, 0.541f, 0.322f))}>- {penalty}</color>\n";
            }
        }

        public void SetThresholdLevels(int minRep, int maxRep)
        {
            thresholdText.text = $"{minRep} to {maxRep}";
        }

        public void SetBorderColor(bool isPositive)
        {
            if (graphics != null)
            {
                // Use hex colors: 38B986 for positive (green), E88A52 for negative (orange)
                Color borderColor = isPositive
                    ? new Color(0x38 / 255f, 0xB9 / 255f, 0x86 / 255f)  // #38B986
                    : new Color(0xE8 / 255f, 0x8A / 255f, 0x52 / 255f); // #E88A52

                UIShaderProperties.SetColor(graphics, "_BorderColor", borderColor);
            }
        }

        public void ClearBorderColor()
        {
            if (graphics != null)
            {
                // Reset to default border color
                Color defaultColor = new Color(0x4B / 255f, 0x55 / 255f, 0x66 / 255f); // #4B5566
                UIShaderProperties.SetColor(graphics, "_BorderColor", defaultColor);
            }
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}