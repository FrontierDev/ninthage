using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    public sealed class UI_CC_CreateButton : UI_Button
    {
        public override void OnClick(PointerEventData eventData)
        {
            var characterName = UI_CC_FinalizePanel.Instance.GetCharacterName();

            CharacterCreationManager.Instance.CreateCharacter(characterName);

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}