using Game.Shared;
using Game.Shared.Runtime.Items;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Defines the client-side <c>IDraggableSlot</c> contract.
    /// </summary>
    public interface IDraggableSlot
    {
        /// <summary>Gets the inventory slot index.</summary>
        int SlotIndex { get; }

        /// <summary>Gets the item currently held by the slot.</summary>
        ItemInstance SlotItem { get; }

        /// <summary>Handles the beginning of a slot drag.</summary>
        void OnSlotBeginDrag(Vector2 position);

        /// <summary>Handles movement during a slot drag.</summary>
        void OnSlotDrag(Vector2 position);

        /// <summary>Handles the end of a slot drag.</summary>
        void OnSlotEndDrag();

        /// <summary>Handles dropping another draggable slot onto this slot.</summary>
        void OnSlotDrop(IDraggableSlot source);
    }
}
