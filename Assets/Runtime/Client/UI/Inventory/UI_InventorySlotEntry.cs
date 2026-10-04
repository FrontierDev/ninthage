using Game.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using System.Linq;
using Game.Client.Utility;
using System.Collections;

namespace Game.Client.UI
{
    public class UI_InventorySlotEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler,
        IDraggableSlot, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static UI_InventorySlotEntry dragSource;

        private int index;
        private ItemInstance itemInstance;

        [SerializeField] private Image graphics;
        [SerializeField] private TMP_Text stackSizeText;

        public int SlotIndex => index;
        public ItemInstance SlotItem => itemInstance;

        private void Awake()
        {
            // Clone the shared material so each slot has its own instance for per-slot shader properties.
            if (graphics != null && graphics.material != null)
                graphics.material = new Material(graphics.material);
        }

        public override void Initialize(ItemInstance data, int index)
        {
            this.index = index;
            UpdateEntry(data);
        }

        void OnDestroy()
        {
            StopCoroutine(FadeOutPressedState());
        }

        private IEnumerator FadeOutPressedState()
        {
            if (graphics == null || graphics.material == null) yield break;

            float pressed = graphics.material.GetFloat("_PressedAmount");
            while (pressed > 0f)
            {
                pressed -= Time.deltaTime * 5f; // Fade out over 0.2 seconds
                graphics.material.SetFloat("_PressedAmount", Mathf.Max(pressed, 0f));
                yield return null;
            }
        }

        public void UpdateEntry(ItemInstance newData)
        {
            itemInstance = newData;

            if (itemInstance != null)
            {
                var definition = ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionId);
                stackSizeText.text = itemInstance.CurrentStackSize > 1 ? itemInstance.CurrentStackSize.ToString() : "";

                var texture = definition.Icon != null ? definition.Icon.texture : null;
                UIShaderProperties.SetTexture(graphics, "_IconTex", texture);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 1f);
                UIShaderProperties.SetColor(graphics, "_RarityColor", RarityColor.GetColor(definition.Quality));
                UIShaderProperties.SetFloat(graphics, "_RarityAmount", 1.0f);
            }
            else
            {
                stackSizeText.text = "";
                UIShaderProperties.SetTexture(graphics, "_IconTex", Texture2D.whiteTexture);
                UIShaderProperties.SetFloat(graphics, "_IconAmount", 0f);
                UIShaderProperties.SetColor(graphics, "_RarityColor", Color.clear);
                UIShaderProperties.SetFloat(graphics, "_RarityAmount", 0f);

            }
        }

        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (itemInstance == null) return;

            UIShaderProperties.SetFloat(graphics, "_PressedAmount", 1f);
            StartCoroutine(FadeOutPressedState());

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                if (itemInstance.BaseItem.ItemType == ItemType.Weapon || itemInstance.BaseItem.ItemType == ItemType.Armor)
                {
                    // Attempt to equip the item if it's a weapon or armor.
                    var itemSlot = itemInstance.BaseItem.ValidSlots.FirstOrDefault();
                    ClientAccountManager.PlayerActor.GetComponent<PlayerEquipment>()?.Server_RequestEquipItem((ItemSlot)itemSlot, index);
                }
                else
                {
                    // Other item types... TO DO
                }
            }

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (dragSource != null) return;
            if (itemInstance == null) return;

            base.OnPointerEnter(eventData);

            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 1f);

            UI_TooltipWindow.Instance.Show(
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionId).DisplayName : "",
                ItemTooltipFactory.GetOrCreateTooltip(itemInstance),
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionId).Description : "",
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionId).Icon : null
            );

            UI_TooltipWindow.Instance.SetPosition(graphics != null ? graphics.rectTransform : transform, 0f);
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            UI_TooltipWindow.Instance.HideImmediate();
            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 0f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick(eventData);
        }

        // --- IDraggableSlot / Drag Handlers ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemInstance == null) return;

            dragSource = this;
            OnSlotBeginDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragSource != this) return;

            OnSlotDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragSource != this) return;

            OnSlotEndDrag();
            dragSource = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (dragSource == null || dragSource == this) return;

            OnSlotDrop(dragSource);
        }

        public void OnSlotBeginDrag(Vector2 position)
        {
            UI_TooltipWindow.Instance.HideImmediate();

            var definition = ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionId);
            UI_DragGhost.Instance.Show(definition.Icon);
            UI_DragGhost.Instance.UpdatePosition(position);
        }

        public void OnSlotDrag(Vector2 position)
        {
            UI_DragGhost.Instance.UpdatePosition(position);
        }

        public void OnSlotEndDrag()
        {
            UI_DragGhost.Instance.Hide();
        }

        public void OnSlotDrop(IDraggableSlot source)
        {
            var playerActor = ClientAccountManager.PlayerActor;
            if (playerActor == null) return;

            var inventory = playerActor.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            inventory.Server_RequestMoveItem(source.SlotIndex, index);
        }
    }
}