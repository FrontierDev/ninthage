using Game.Shared;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_NameplateHealthBar</c> implementation.
    /// </summary>
    public sealed class UI_NameplateHealthBar : UI_ProgressBar
    {
        [FormerlySerializedAs("nameText")]
        [SerializeField] private TMP_Text _nameText;
        [FormerlySerializedAs("percentText")]
        [SerializeField] private TMP_Text _percentText;
        [FormerlySerializedAs("levelText")]
        [SerializeField] private TMP_Text _levelText;

        [FormerlySerializedAs("hostileColor")]
        [SerializeField]
        private Color _hostileColor = new(0.878f, 0.749f, 0.447f);
        [FormerlySerializedAs("alliedColor")]
        [SerializeField]
        private Color _alliedColor = new(0.878f, 0.749f, 0.447f);
        [FormerlySerializedAs("neutralColor")]
        [SerializeField]
        private Color _neutralColor = new(0.118f, 0.133f, 0.165f);

        private void Awake()
        {
            graphics.material = new Material(graphics.material);
        }

        /// <summary>
        /// Sets the value managed by <c>SetRelation</c>.
        /// </summary>
        public void SetRelation(FactionRelationState state)
        {
            switch (state)
            {
                case FactionRelationState.Hostile:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.red);
                    _nameText.color = _hostileColor;
                    _percentText.color = _hostileColor;
                    break;
                case FactionRelationState.Allied:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.green);
                    _nameText.color = _alliedColor;
                    _percentText.color = _alliedColor;
                    break;
                case FactionRelationState.Neutral:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.softYellow);
                    _nameText.color = _neutralColor;
                    _percentText.color = _neutralColor;
                    break;
            }
        }

        /// <summary>
        /// Sets the value managed by <c>SetName</c>.
        /// </summary>
        public void SetName(string name)
        {
            if (_nameText != null)
                _nameText.text = name;
        }

        /// <summary>
        /// Sets the value managed by <c>SetLevel</c>.
        /// </summary>
        public void SetLevel(int level)
        {
            if (_levelText != null)
                _levelText.text = level.ToString();
        }

        /// <summary>
        /// Sets the value managed by <c>SetHealthPercent</c>.
        /// </summary>
        public void SetHealthPercent(float percent)
        {
            UIShaderProperties.SetFloat(graphics, "_FillAmount", percent);
            _percentText.text = $"{percent * 100:0}%";
        }
    }
}
