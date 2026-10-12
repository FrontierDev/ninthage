using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_EnterWorldButton</c> implementation.
    /// </summary>
    public class UI_EnterWorldButton : UI_Button
    {
        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            CharacterSelectionManager.Instance.EnterWorld();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}