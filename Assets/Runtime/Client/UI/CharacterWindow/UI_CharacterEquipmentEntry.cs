using Game.Client.Utility;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Items;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterEquipmentEntry</c> implementation.
    /// </summary>
    public sealed class UI_CharacterEquipmentEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler
    {
        /// <summary>
        /// Stores the <c>item</c> value.
        /// </summary>
        public ItemInstance item;
        /// <summary>
        /// Stores the <c>slotId</c> value.
        /// </summary>
        public ItemSlot slotId;
        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;


        private void Awake()
        {
            // Clone the shared material so each slot has its own instance for per-slot shader properties.
            if (_graphics != null && _graphics.material != null)
                _graphics.material = new Material(_graphics.material);
        }


        /// <inheritdoc/>
        public override void Initialize(ItemInstance _item, int _slotID)
        {
            slotId = (ItemSlot)_slotID;
            SetItem(_item);
        }

        /// <summary>
        /// Sets the value managed by <c>SetItem</c>.
        /// </summary>
        public void SetItem(ItemInstance _item)
        {
            item = _item;

            if (item == null)
            {
                UIShaderProperties.SetTexture(_graphics, "_IconTex", null);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 0f);
                UIShaderProperties.SetColor(_graphics, "_RarityColor", Color.clear);
                UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 0f);

            }
            else
            {
                // Update the UI entry to show the item's icon, name, stats, etc.
                UIShaderProperties.SetTexture(_graphics, "_IconTex", item.BaseItem.Icon.texture);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1f);
                UIShaderProperties.SetColor(_graphics, "_RarityColor", RarityColor.GetColor(item.BaseItem.Quality));
                UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 1.0f);
            }
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (item == null) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Attempt to unequip the item when right-clicking on an equipped item.
                ClientAccountManager.PlayerActor.GetComponent<PlayerEquipment>().Server_RequestUnequipItem(slotId);
                Debug.Log($"Requested unequip item from slot {slotId}");
            }

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        /// <inheritdoc/>
        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (item == null) return;

            base.OnPointerEnter(eventData);

            UIShaderProperties.SetFloat(_graphics, "_HoverAmount", 1f);

            UI_TooltipWindow.Instance.Show(
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionID).DisplayName : "",
                ItemTooltipFactory.GetOrCreateTooltip(item),
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionID).Description : "",
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionID).Icon : null
            );

            UI_TooltipWindow.Instance.SetPosition(_graphics != null ? _graphics.rectTransform : transform, 0f);
        }

        /// <inheritdoc/>
        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            UI_TooltipWindow.Instance.HideImmediate();
            UIShaderProperties.SetFloat(_graphics, "_HoverAmount", 0f);
        }

        /// <summary>
        /// Handles the <c>OnPointerClick</c> callback.
        /// </summary>
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnClick(eventData);
        }
    }
}
