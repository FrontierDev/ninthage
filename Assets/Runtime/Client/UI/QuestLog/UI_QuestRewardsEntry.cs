using Game.Shared.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestRewardsEntry</c> implementation.
    /// </summary>
    public sealed class UI_QuestRewardsEntry : UI_ListEntry<QuestItemReward>
    {
        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;

        private QuestItemReward _data;
        private int _index;

        void Awake()
        {
            _graphics.material = new Material(_graphics.material);
        }

        /// <inheritdoc/>
        public override void Initialize(QuestItemReward data, int index)
        {
            this._data = data;
            this._index = index;

            UIShaderProperties.SetTexture(_graphics, "_IconTex", data.Item.Icon.texture);
            UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1f);
            UIShaderProperties.SetColor(_graphics, "_RarityColor", Utility.RarityColor.GetColor(data.Item.Rarity));
            UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 1.0f);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            // No click behavior for quest rewards in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
