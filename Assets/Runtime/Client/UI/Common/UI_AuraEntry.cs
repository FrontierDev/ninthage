using System;
using Game.Shared;
using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_AuraEntry : UI_ListEntry<AuraInstance>
    {
        [SerializeField] private AuraInstance data;
        public AuraInstance Data => data;

        [SerializeField] private Image graphics;
        [SerializeField] private TMP_Text durationText;

        private int index;

        public override void Initialize(AuraInstance data, int index)
        {
            this.data = data;
            this.index = index;

            if (graphics.material != null)
                graphics.material = new Material(graphics.material);

            if (data.Definition.Icon != null)
            {
                UIShaderProperties.SetTexture(graphics, "_IconTex", data.Definition.Icon.texture);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 1.0f);
                UIShaderProperties.SetColor(graphics, "_RarityColor", Color.red);
                UIShaderProperties.SetFloat(graphics, "_RarityAmount", 1.0f);
            }

            else
            {
                UIShaderProperties.SetTexture(graphics, "_IconTex", Texture2D.whiteTexture);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 1.0f);
            }

            UpdateDisplay(data.Duration);
        }

        public void UpdateDisplay(float currentDuration = -1f)
        {
            if (data.Definition.StackBehavior == AuraStackBehavior.Condition_Stack ||
                data.Definition.StackBehavior == AuraStackBehavior.Condition_Extend)
            {
                // Show stack count for condition auras
                durationText.text = $"{data.Stacks}";
            }
            else
            {
                // Show duration for other auras
                if (data.Definition.BaseDuration <= 0)
                    return;
                else if (currentDuration <= 10.0f)
                    durationText.text = $"{currentDuration:F1}";
                else
                    durationText.text = $"{Mathf.CeilToInt(currentDuration)}s";
            }
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            // Do nothing for now.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}