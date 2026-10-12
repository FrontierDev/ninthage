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
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InventorySlotEntry</c> implementation.
    /// </summary>
    public class UI_InventorySlotEntry : UI_ListEntry<ItemInstance>, IPointerClickHandler,
        IDraggableSlot, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static UI_InventorySlotEntry _dragSource;

        private int _index;
        private ItemInstance _itemInstance;

        [FormerlySerializedAs("graphics")]
        [SerializeField] private Image _graphics;
        [FormerlySerializedAs("stackSizeText")]
        [SerializeField] private TMP_Text _stackSizeText;

        /// <summary>
        /// Gets the <c>SlotIndex</c> value.
        /// </summary>
        public int SlotIndex => _index;
        /// <summary>
        /// Gets the <c>SlotItem</c> value.
        /// </summary>
        public ItemInstance SlotItem => _itemInstance;

        private void Awake()
        {
            // Clone the shared material so each slot has its own instance for per-slot shader properties.
            if (_graphics != null && _graphics.material != null)
                _graphics.material = new Material(_graphics.material);
        }

        /// <inheritdoc/>
        public override void Initialize(ItemInstance data, int index)
        {
            this._index = index;
            UpdateEntry(data);
        }

        void OnDestroy()
        {
            StopCoroutine(FadeOutPressedState());
        }

        private IEnumerator FadeOutPressedState()
        {
            if (_graphics == null || _graphics.material == null) yield break;

            float pressed = _graphics.material.GetFloat("_PressedAmount");
            while (pressed > 0f)
            {
                pressed -= Time.deltaTime * 5f; // Fade out over 0.2 seconds
                _graphics.material.SetFloat("_PressedAmount", Mathf.Max(pressed, 0f));
                yield return null;
            }
        }

        /// <summary>
        /// Executes the client-side <c>UpdateEntry</c> operation.
        /// </summary>
        public void UpdateEntry(ItemInstance newData)
        {
            _itemInstance = newData;

            if (_itemInstance != null)
            {
                var definition = ItemDefinitionLibrary.Instance.GetDefinition(_itemInstance.BaseItem.DefinitionID);
                _stackSizeText.text = _itemInstance.CurrentStackSize > 1 ? _itemInstance.CurrentStackSize.ToString() : "";

                var texture = definition.Icon != null ? definition.Icon.texture : null;
                UIShaderProperties.SetTexture(_graphics, "_IconTex", texture);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 1f);
                UIShaderProperties.SetColor(_graphics, "_RarityColor", RarityColor.GetColor(definition.Quality));
                UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 1.0f);
            }
            else
            {
                _stackSizeText.text = "";
                UIShaderProperties.SetTexture(_graphics, "_IconTex", Texture2D.whiteTexture);
                UIShaderProperties.SetFloat(_graphics, "_IconAmount", 0f);
                UIShaderProperties.SetColor(_graphics, "_RarityColor", Color.clear);
                UIShaderProperties.SetFloat(_graphics, "_RarityAmount", 0f);

            }
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_itemInstance == null) return;

            UIShaderProperties.SetFloat(_graphics, "_PressedAmount", 1f);
            StartCoroutine(FadeOutPressedState());

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                if (_itemInstance.BaseItem.ItemType == ItemType.Weapon || _itemInstance.BaseItem.ItemType == ItemType.Armor)
                {
                    // Attempt to equip the item if it's a weapon or armor.
                    var itemSlot = _itemInstance.BaseItem.ValidSlots.FirstOrDefault();
                    ClientAccountManager.PlayerActor.GetComponent<PlayerEquipment>()?.Server_RequestEquipItem((ItemSlot)itemSlot, _index);
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
            if (_dragSource != null) return;
            if (_itemInstance == null) return;

            base.OnPointerEnter(eventData);

            UIShaderProperties.SetFloat(_graphics, "_HoverAmount", 1f);

            UI_TooltipWindow.Instance.Show(
                _itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(_itemInstance.BaseItem.DefinitionID).DisplayName : "",
                ItemTooltipFactory.GetOrCreateTooltip(_itemInstance),
                _itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(_itemInstance.BaseItem.DefinitionID).Description : "",
                _itemInstance != null ? ItemDefinitionLibrary.Instance.GetDefinition(_itemInstance.BaseItem.DefinitionID).Icon : null
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
            if (_itemInstance == null) return;

            _dragSource = this;
            OnSlotBeginDrag(eventData.position);
        }

        /// <summary>
        /// Handles the <c>OnDrag</c> callback.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (_dragSource != this) return;

            OnSlotDrag(eventData.position);
        }

        /// <summary>
        /// Handles the <c>OnEndDrag</c> callback.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragSource != this) return;

            OnSlotEndDrag();
            _dragSource = null;
        }

        /// <summary>
        /// Handles the <c>OnDrop</c> callback.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (_dragSource == null || _dragSource == this) return;

            OnSlotDrop(_dragSource);
        }

        /// <summary>
        /// Handles the <c>OnSlotBeginDrag</c> callback.
        /// </summary>
        public void OnSlotBeginDrag(Vector2 position)
        {
            UI_TooltipWindow.Instance.HideImmediate();

            var definition = ItemDefinitionLibrary.Instance.GetDefinition(_itemInstance.BaseItem.DefinitionID);
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

            inventory.Server_RequestMoveItem(source.SlotIndex, _index);
        }
    }
}
