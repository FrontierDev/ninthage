using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    public class UI_EnterWorldButton : UI_Button
    {
        public override void OnClick(PointerEventData eventData)
        {
            CharacterSelectionManager.Instance.EnterWorld();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}