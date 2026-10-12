using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ReputationRewardEntry</c> implementation.
    /// </summary>
    public sealed class UI_ReputationRewardEntry : UI_ListEntry<FactionRewardDescriptionEntry>
    {
        private int _index;
        private FactionRewardDescriptionEntry _data;

        [FormerlySerializedAs("header")]
        [SerializeField] private TMP_Text _header;
        [FormerlySerializedAs("entryText")]
        [SerializeField] private TMP_Text _entryText;
        [FormerlySerializedAs("thresholdText")]
        [SerializeField] private TMP_Text _thresholdText;
        [FormerlySerializedAs("thresholdIcon")]
        [SerializeField] private Image _thresholdIcon;
        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;

        private void Awake()
        {
            _graphics.material = new Material(_graphics.material);
        }

        /// <inheritdoc/>
        public override void Initialize(FactionRewardDescriptionEntry data, int index)
        {
            this._data = data;
            this._index = index;

            _header.text = data.title;
            _thresholdIcon.sprite = data.icon;

            _header.color = data.isPositive ? new Color(0.22f, 0.725f, 0.525f) : new Color(0.91f, 0.541f, 0.322f);
            _entryText.text = string.Empty;

            foreach (var benefit in data.benefits)
            {
                _entryText.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.22f, 0.725f, 0.525f))}>- {benefit}</color>\n";
            }

            foreach (var penalty in data.penalties)
            {
                _entryText.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.91f, 0.541f, 0.322f))}>- {penalty}</color>\n";
            }
        }

        /// <summary>
        /// Sets the value managed by <c>SetThresholdLevels</c>.
        /// </summary>
        public void SetThresholdLevels(int minRep, int maxRep)
        {
            _thresholdText.text = $"{minRep} to {maxRep}";
        }

        /// <summary>
        /// Sets the value managed by <c>SetBorderColor</c>.
        /// </summary>
        public void SetBorderColor(bool isPositive)
        {
            if (_graphics != null)
            {
                // Use hex colors: 38B986 for positive (green), E88A52 for negative (orange)
                Color borderColor = isPositive
                    ? new Color(0x38 / 255f, 0xB9 / 255f, 0x86 / 255f)  // #38B986
                    : new Color(0xE8 / 255f, 0x8A / 255f, 0x52 / 255f); // #E88A52

                UIShaderProperties.SetColor(_graphics, "_BorderColor", borderColor);
            }
        }

        /// <summary>
        /// Executes the client-side <c>ClearBorderColor</c> operation.
        /// </summary>
        public void ClearBorderColor()
        {
            if (_graphics != null)
            {
                // Reset to default border color
                var defaultColor = new Color(0x4B / 255f, 0x55 / 255f, 0x66 / 255f); // #4B5566
                UIShaderProperties.SetColor(_graphics, "_BorderColor", defaultColor);
            }
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
