using Game.Client.Utility;
using Game.Shared;
using Game.Shared.Data;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_CharacterEquipmentEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler
    {
        public ItemInstance item;
        public ItemSlot slotId;
        [SerializeField] private Image graphics;


        private void Awake()
        {
            // Clone the shared material so each slot has its own instance for per-slot shader properties.
            if (graphics != null && graphics.material != null)
                graphics.material = new Material(graphics.material);
        }


        public override void Initialize(ItemInstance _item, int _slotId)
        {
            slotId = (ItemSlot)_slotId;
            SetItem(_item);
        }

        public void SetItem(ItemInstance _item)
        {
            item = _item;

            if (item == null)
            {
                UIShaderProperties.SetTexture(graphics, "_IconTex", null);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 0f);
                UIShaderProperties.SetColor(graphics, "_RarityColor", Color.clear);
                UIShaderProperties.SetFloat(graphics, "_RarityAmount", 0f);

            }
            else
            {
                // Update the UI entry to show the item's icon, name, stats, etc.
                UIShaderProperties.SetTexture(graphics, "_IconTex", item.BaseItem.Icon.texture);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 1f);
                UIShaderProperties.SetColor(graphics, "_RarityColor", RarityColor.GetColor(item.BaseItem.Quality));
                UIShaderProperties.SetFloat(graphics, "_RarityAmount", 1.0f);
            }
        }

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

        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (item == null) return;

            base.OnPointerEnter(eventData);

            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 1f);

            UI_TooltipWindow.Instance.Show(
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionId).DisplayName : "",
                ItemTooltipFactory.GetOrCreateTooltip(item),
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionId).Description : "",
                item != null ? ItemDefinitionLibrary.Instance.GetDefinition(item.BaseItem.DefinitionId).Icon : null
            );

            UI_TooltipWindow.Instance.SetPosition(graphics != null ? graphics.rectTransform : transform, 0f);
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            UI_TooltipWindow.Instance.HideImmediate();
            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 0f);
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnClick(eventData);
        }
    }
}