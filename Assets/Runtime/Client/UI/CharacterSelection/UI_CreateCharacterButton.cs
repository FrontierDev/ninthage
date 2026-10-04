using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.Client.UI
{
    public class UI_CreateCharacterButton : UI_Button
    {
        public override void OnClick(PointerEventData eventData)
        {
            SceneManager.UnloadSceneAsync("CharacterSelection");
            SceneManager.LoadSceneAsync("CharacterCreation", LoadSceneMode.Additive);
            UI_Manager.Instance.HideWindow("CharacterSelect");
            Game.Client.UI.UI_CharacterCreationWindow.Instance.Show();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}