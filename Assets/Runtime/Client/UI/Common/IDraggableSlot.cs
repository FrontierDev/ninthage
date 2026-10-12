using Game.Shared;
using UnityEngine;

using Game.Shared.Runtime.Items;
namespace Game.Client.UI
{
    public interface IDraggableSlot
    {
        int SlotIndex { get; }
        ItemInstance SlotItem { get; }
        void OnSlotBeginDrag(Vector2 position);
        void OnSlotDrag(Vector2 position);
        void OnSlotEndDrag();
        void OnSlotDrop(IDraggableSlot source);
    }
}
