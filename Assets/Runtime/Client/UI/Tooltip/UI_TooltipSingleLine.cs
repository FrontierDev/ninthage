using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TooltipSingleLine</c> implementation.
    /// </summary>
    public sealed class UI_TooltipSingleLine : MonoBehaviour, ITooltipLine
    {
        [SerializeField] private TMP_Text lineText;

        /// <summary>
        /// Sets the value managed by <c>Set</c>.
        /// </summary>
        public void Set(string[] text)
        {
            lineText.text = text[0];
        }
    }
}