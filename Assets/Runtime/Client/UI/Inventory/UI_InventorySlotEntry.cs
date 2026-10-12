using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Utility;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Items;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InventorySlotEntry</c> implementation.
    /// </summary>
    public class UI_InventorySlotEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler,
        IDraggableSlot, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static UI_InventorySlotEntry dragSource;

        private int index;
        private ItemInstance itemInstance;

        [SerializeField] private Image graphics;
        [SerializeField] private TMP_Text stackSizeText;

        /// <summary>
        /// Gets the <c>SlotIndex</c> value.
        /// </summary>
        public int SlotIndex => index;
        /// <summary>
        /// Gets the <c>SlotItem</c> value.
        /// </summary>
        public ItemInstance SlotItem => itemInstance;

        private void Awake()
        {
            // Clone the shared material so each slot has its own instance for per-slot shader properties.
            if (graphics != null && graphics.material != null)
                graphics.material = new Material(graphics.material);
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Executes the client-side <c>UpdateEntry</c> operation.
        /// </summary>
        public void UpdateEntry(ItemInstance newData)
        {
            itemInstance = newData;

            if (itemInstance != null)
            {
                var definition = ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionID);
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (dragSource != null) return;
            if (itemInstance == null) return;

            base.OnPointerEnter(eventData);

            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 1f);

            UI_TooltipWindow.Instance.Show(
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionID).DisplayName : "",
                ItemTooltipFactory.GetOrCreateTooltip(itemInstance),
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionID).Description : "",
                itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionID).Icon : null
            );

            UI_TooltipWindow.Instance.SetPosition(graphics != null ? graphics.rectTransform : transform, 0f);
        }

        /// <inheritdoc/>
        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            UI_TooltipWindow.Instance.HideImmediate();
            UIShaderProperties.SetFloat(graphics, "_HoverAmount", 0f);
        }

        /// <summary>
        /// Handles the <c>OnPointerClick</c> callback.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick(eventData);
        }

        // --- IDraggableSlot / Drag Handlers ---

        /// <summary>
        /// Handles the <c>OnBeginDrag</c> callback.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemInstance == null) return;

            dragSource = this;
            OnSlotBeginDrag(eventData.position);
        }

        /// <summary>
        /// Handles the <c>OnDrag</c> callback.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (dragSource != this) return;

            OnSlotDrag(eventData.position);
        }

        /// <summary>
        /// Handles the <c>OnEndDrag</c> callback.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragSource != this) return;

            OnSlotEndDrag();
            dragSource = null;
        }

        /// <summary>
        /// Handles the <c>OnDrop</c> callback.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (dragSource == null || dragSource == this) return;

            OnSlotDrop(dragSource);
        }

        /// <summary>
        /// Handles the <c>OnSlotBeginDrag</c> callback.
        /// </summary>
        public void OnSlotBeginDrag(Vector2 position)
        {
            UI_TooltipWindow.Instance.HideImmediate();

            var definition = ItemDefinitionLibrary.Instance.GetDefinition(itemInstance.BaseItem.DefinitionID);
            UI_DragGhost.Instance.Show(definition.Icon);
            UI_DragGhost.Instance.UpdatePosition(position);
        }

        /// <summary>
        /// Handles the <c>OnSlotDrag</c> callback.
        /// </summary>
        public void OnSlotDrag(Vector2 position)
        {
            UI_DragGhost.Instance.UpdatePosition(position);
        }

        /// <summary>
        /// Handles the <c>OnSlotEndDrag</c> callback.
        /// </summary>
        public void OnSlotEndDrag()
        {
            UI_DragGhost.Instance.Hide();
        }

        /// <summary>
        /// Handles the <c>OnSlotDrop</c> callback.
        /// </summary>
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
