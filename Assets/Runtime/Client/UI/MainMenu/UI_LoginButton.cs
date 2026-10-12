using UnityEngine;
using UnityEngine.EventSystems;


namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_LoginButton</c> implementation.
    /// </summary>
    public sealed class UI_LoginButton : UI_Button
    {
        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            var username = UI_MainMenuWindow.Instance.GetUsername();
            var password = UI_MainMenuWindow.Instance.GetPassword();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) { return; }
            else
            {
                ClientConnectionManager.Instance.ConnectToServer(username, password);
                DisableButton();
            }

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        /// <inheritdoc/>
        public override void OnPointerEnter(PointerEventData eventData) => base.OnPointerEnter(eventData);

        /// <inheritdoc/>
        public override void OnPointerExit(PointerEventData eventData) => base.OnPointerExit(eventData);
    }
}