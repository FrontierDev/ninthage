using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>StageButton</c> implementation.
    /// </summary>
    public class StageButton : UI_Button
    {
        [SerializeField] private string[] openPanels;
        [SerializeField] private string[] closePanels;

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            foreach (var panel in closePanels)
                UI_CharacterCreationWindow.Instance.HidePanel(panel);

            foreach (var panel in openPanels)
                UI_CharacterCreationWindow.Instance.ShowPanel(panel);

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}