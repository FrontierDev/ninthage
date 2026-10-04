using UnityEngine;
using TMPro;

namespace Game.Client.UI
{
    public sealed class UI_TooltipDoubleLine : MonoBehaviour, ITooltipLine
    {
        [SerializeField] private TMP_Text leftText;
        [SerializeField] private TMP_Text rightText;

        public void Set(string[] text)
        {
            leftText.text = text[0];
            rightText.text = text[1];
        }
    }
}