using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_TooltipSingleLine : MonoBehaviour, ITooltipLine
    {
        [SerializeField] private TMP_Text lineText;

        public void Set(string[] text)
        {
            lineText.text = text[0];
        }
    }
}