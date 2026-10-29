using Debug = Game.Shared.Utility.FormattedDebug;
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
            if (!UI_CC_FinalizePanel.Instance.TryGetCharacterNames(out string firstName, out string lastName))
            {
                Debug.Warning("Character creation requires a first and last name separated by one space.");
            }
            else
            {
                CharacterCreationManager.Instance.CreateCharacter(firstName, lastName);
            }

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
