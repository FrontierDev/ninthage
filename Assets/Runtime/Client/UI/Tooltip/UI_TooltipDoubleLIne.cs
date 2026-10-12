using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TooltipDoubleLine</c> implementation.
    /// </summary>
    public sealed class UI_TooltipDoubleLine : MonoBehaviour, ITooltipLine
    {
        [FormerlySerializedAs("leftText")]
        [SerializeField] private TMP_Text _leftText;
        [FormerlySerializedAs("rightText")]
        [SerializeField] private TMP_Text _rightText;

        /// <summary>
        /// Sets the value managed by <c>Set</c>.
        /// </summary>
        public void Set(string[] text)
        {
            _leftText.text = text[0];
            _rightText.text = text[1];
        }
    }
}
