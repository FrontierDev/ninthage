using Game.Shared.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_QuestRewardsEntry : UI_ListEntry<QuestItemReward>
    {
        [SerializeField] private Image graphics;

        private QuestItemReward data;
        private int index;

        void Awake()
        {
            graphics.material = new Material(graphics.material);
        }

        public override void Initialize(QuestItemReward data, int index)
        {
            this.data = data;
            this.index = index;

            UIShaderProperties.SetTexture(graphics, "_IconTex", data.Item.Icon.texture);
            UIShaderProperties.SetFloat(graphics, "_IconAmount", 1f);
            UIShaderProperties.SetColor(graphics, "_RarityColor", Utility.RarityColor.GetColor(data.Item.Rarity));
            UIShaderProperties.SetFloat(graphics, "_RarityAmount", 1.0f);
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            // No click behavior for quest rewards in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}