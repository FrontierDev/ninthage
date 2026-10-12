using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_CreateButton</c> implementation.
    /// </summary>
    public sealed class UI_CC_CreateButton : UI_Button
    {
        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            var characterName = UI_CC_FinalizePanel.Instance.GetCharacterName();

            CharacterCreationManager.Instance.CreateCharacter(characterName);

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}