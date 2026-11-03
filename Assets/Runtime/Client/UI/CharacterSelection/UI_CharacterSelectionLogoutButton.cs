using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterSelectionLogoutButton</c> implementation.
    /// </summary>
    public sealed class UI_CharacterSelectionLogoutButton : UI_Button
    {
        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            ClientConnectionManager.Instance.LogoutToMainMenu();
            base.OnClick(eventData);
        }
    }
}
