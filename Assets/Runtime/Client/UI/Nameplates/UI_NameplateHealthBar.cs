using Game.Shared;
using Game.Shared.Runtime;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_NameplateHealthBar</c> implementation.
    /// </summary>
    public sealed class UI_NameplateHealthBar : UI_ProgressBar
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private TMP_Text levelText;

        [SerializeField]
        private Color hostileColor = new(0.878f, 0.749f, 0.447f);
        [SerializeField]
        private Color alliedColor = new(0.878f, 0.749f, 0.447f);
        [SerializeField]
        private Color neutralColor = new(0.118f, 0.133f, 0.165f);

        private void Awake()
        {
            graphics.material = new Material(graphics.material);
        }

        /// <summary>
        /// Sets the value managed by <c>SetRelation</c>.
        /// </summary>
        [ToDo("Consider adding a method to set the relation which updates the bar color and text colors accordingly.")]
        public void SetRelation(FactionRelationState state)
        {
            switch (state)
            {
                case FactionRelationState.Hostile:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.red);
                    nameText.color = hostileColor;
                    percentText.color = hostileColor;
                    break;
                case FactionRelationState.Allied:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.green);
                    nameText.color = alliedColor;
                    percentText.color = alliedColor;
                    break;
                case FactionRelationState.Neutral:
                    UIShaderProperties.SetColor(graphics, "_MainColor", Color.softYellow);
                    nameText.color = neutralColor;
                    percentText.color = neutralColor;
                    break;
            }
        }

        /// <summary>
        /// Sets the value managed by <c>SetName</c>.
        /// </summary>
        public void SetName(string name)
        {
            if (nameText != null)
                nameText.text = name;
        }

        /// <summary>
        /// Sets the value managed by <c>SetLevel</c>.
        /// </summary>
        public void SetLevel(int level)
        {
            if (levelText != null)
                levelText.text = level.ToString();
        }

        /// <summary>
        /// Sets the value managed by <c>SetHealthPercent</c>.
        /// </summary>
        public void SetHealthPercent(float percent)
        {
            UIShaderProperties.SetFloat(graphics, "_FillAmount", percent);
            percentText.text = $"{percent * 100:0}%";
        }
    }
}
