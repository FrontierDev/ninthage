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
        private Material _materialInstance;

        private void Awake()
        {
            if (_graphics != null && _graphics.material != null)
            {
                _materialInstance = new Material(_graphics.material);
                _graphics.material = _materialInstance;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
                Destroy(_materialInstance);
        }

        /// <inheritdoc/>
        public override void Initialize(QuestItemReward data, int index)
        {
            this._data = data;
            this._index = index;

            UIShaderProperties.SetIcon(_graphics, data.Item.Icon.texture);
            UIShaderProperties.SetIconVisible(_graphics, true);
            UIShaderProperties.SetRarityColor(_graphics, Utility.RarityColor.GetColor(data.Item.Rarity));
            UIShaderProperties.SetRarityVisible(_graphics, true);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            // No click behavior for quest rewards in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
