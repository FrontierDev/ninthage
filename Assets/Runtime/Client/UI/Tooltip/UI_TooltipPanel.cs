using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public struct UI_TooltipLine
    {
        public string[] text;
    }

    public class UI_TooltipPanel : UI_Panel
    {
        [SerializeField] private TMP_Text tooltipHeader;
        [SerializeField] private GameObject tooltipBody;
        [SerializeField] private TMP_Text tooltipFooter;
        [SerializeField] private Image tooltipIcon;

        [SerializeField] private GameObject singleLinePrefab;
        [SerializeField] private GameObject doubleLinePrefab;

        public void SetTooltip(string header, UI_TooltipLine[] lines, string footer, Sprite icon)
        {
            tooltipHeader.text = header;
            tooltipFooter.text = footer;
            tooltipIcon.sprite = icon;

            if (string.IsNullOrEmpty(header))
                tooltipHeader.gameObject.SetActive(false);
            else
                tooltipHeader.gameObject.SetActive(true);

            if (string.IsNullOrEmpty(footer))
                tooltipFooter.gameObject.SetActive(false);
            else
                tooltipFooter.gameObject.SetActive(true);

            if (icon == null)
                tooltipIcon.gameObject.SetActive(false);
            else
                tooltipIcon.gameObject.SetActive(true);

            ClearLines();
            SetBody(lines);

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
        }

        private void ClearLines()
        {
            foreach (Transform child in tooltipBody.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void SetBody(UI_TooltipLine[] lines)
        {
            foreach (var line in lines)
            {
                if (line.text.Length == 1)
                {
                    var entry = Instantiate(singleLinePrefab, tooltipBody.transform);
                    entry.GetComponent<UI_TooltipSingleLine>().Set(line.text);
                }
                else if (line.text.Length == 2)
                {
                    var entry = Instantiate(doubleLinePrefab, tooltipBody.transform);
                    entry.GetComponent<UI_TooltipDoubleLine>().Set(line.text);
                }
            }
        }
    }
}