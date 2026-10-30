using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Spellcasting;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_AuraEntry</c> implementation.
    /// </summary>
    public sealed class UI_AuraEntry : UI_ListEntry<AuraInstance>
    {
        [FormerlySerializedAs("data")]
        [SerializeField] private AuraInstance _data;
        /// <summary>
        /// Gets the <c>Data</c> value.
        /// </summary>
        public AuraInstance Data => _data;

        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;
        [FormerlySerializedAs("durationText")]
        [SerializeField] private TMP_Text _durationText;

        private int _index;

        /// <inheritdoc/>
        public override void Initialize(AuraInstance _data, int index)
        {
            this._data = _data;
            this._index = index;

            if (_graphics.material != null)
                _graphics.material = new Material(_graphics.material);

            if (_data.Definition.Icon != null)
            {
                UIShaderProperties.SetTexture(_graphics, "_IconTex", _data.Definition.Icon.texture);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1.0f);
                UIShaderProperties.SetColor(_graphics, "_RarityColor", Color.red);
                UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 1.0f);
            }

            else
            {
                UIShaderProperties.SetTexture(_graphics, "_IconTex", Texture2D.whiteTexture);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1.0f);
            }

            UpdateDisplay(_data.Duration);
        }

        /// <summary>
        /// Executes the client-side <c>UpdateDisplay</c> operation.
        /// </summary>
        public void UpdateDisplay(float currentDuration = -1f)
        {
            if (_data.Stacks > 1)
            {
                _durationText.text = $"{_data.Stacks}";
            }
            else
            {
                if (_data.Definition.BaseDuration <= 0)
                    return;
                else if (currentDuration <= 10.0f)
                    _durationText.text = $"{currentDuration:F1}";
                else
                    _durationText.text = $"{Mathf.CeilToInt(currentDuration)}s";
            }
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            // Do nothing for now.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
