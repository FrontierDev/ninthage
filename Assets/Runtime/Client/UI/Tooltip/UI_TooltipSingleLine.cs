using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TooltipSingleLine</c> implementation.
    /// </summary>
    public sealed class UI_TooltipSingleLine : MonoBehaviour, ITooltipLine
    {
        [FormerlySerializedAs("lineText")]
        [SerializeField] private TMP_Text _lineText;

        /// <summary>
        /// Sets the value managed by <c>Set</c>.
        /// </summary>
        public void Set(string[] text)
        {
            _lineText.text = text[0];
        }
    }
}
