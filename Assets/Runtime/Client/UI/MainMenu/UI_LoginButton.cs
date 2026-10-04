using UnityEngine;
using UnityEngine.EventSystems;


namespace Game.Client.UI
{
    public sealed class UI_LoginButton : UI_Button
    {
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

        public override void OnPointerEnter(PointerEventData eventData) => base.OnPointerEnter(eventData);

        public override void OnPointerExit(PointerEventData eventData) => base.OnPointerExit(eventData);
    }
}