using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TooltipDoubleLine</c> implementation.
    /// </summary>
    public sealed class UI_TooltipDoubleLine : MonoBehaviour, ITooltipLine
    {
        [SerializeField] private TMP_Text leftText;
        [SerializeField] private TMP_Text rightText;

        /// <summary>
        /// Sets the value managed by <c>Set</c>.
        /// </summary>
        public void Set(string[] text)
        {
            leftText.text = text[0];
            rightText.text = text[1];
        }
    }
}