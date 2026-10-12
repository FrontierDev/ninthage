using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>StageButton</c> implementation.
    /// </summary>
    public class StageButton : UI_Button
    {
        [FormerlySerializedAs("openPanels")]
        [SerializeField] private string[] _openPanels;
        [FormerlySerializedAs("closePanels")]
        [SerializeField] private string[] _closePanels;

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            foreach (var panel in _closePanels)
                UI_CharacterCreationWindow.Instance.HidePanel(panel);

            foreach (var panel in _openPanels)
                UI_CharacterCreationWindow.Instance.ShowPanel(panel);

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
