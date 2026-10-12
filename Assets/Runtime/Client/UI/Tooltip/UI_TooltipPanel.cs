using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Represents the client-side <c>UI_TooltipLine</c> value.
    /// </summary>
    public struct UI_TooltipLine
    {
        /// <summary>
        /// Stores the <c>text</c> value.
        /// </summary>
        public string[] text;
    }

    /// <summary>
    /// Provides the client-side <c>UI_TooltipPanel</c> implementation.
    /// </summary>
    public class UI_TooltipPanel : UI_Panel
    {
        [FormerlySerializedAs("tooltipHeader")]
        [SerializeField] private TMP_Text _tooltipHeader;
        [FormerlySerializedAs("tooltipBody")]
        [SerializeField] private GameObject _tooltipBody;
        [FormerlySerializedAs("tooltipFooter")]
        [SerializeField] private TMP_Text _tooltipFooter;
        [FormerlySerializedAs("tooltipIcon")]
        [SerializeField] private Image _tooltipIcon;

        [FormerlySerializedAs("singleLinePrefab")]
        [SerializeField] private GameObject _singleLinePrefab;
        [FormerlySerializedAs("doubleLinePrefab")]
        [SerializeField] private GameObject _doubleLinePrefab;

        /// <summary>
        /// Sets the value managed by <c>SetTooltip</c>.
        /// </summary>
        public void SetTooltip(string header, UI_TooltipLine[] lines, string footer, Sprite icon)
        {
            _tooltipHeader.text = header;
            _tooltipFooter.text = footer;
            _tooltipIcon.sprite = icon;

            if (string.IsNullOrEmpty(header))
                _tooltipHeader.gameObject.SetActive(false);
            else
                _tooltipHeader.gameObject.SetActive(true);

            if (string.IsNullOrEmpty(footer))
                _tooltipFooter.gameObject.SetActive(false);
            else
                _tooltipFooter.gameObject.SetActive(true);

            if (icon == null)
                _tooltipIcon.gameObject.SetActive(false);
            else
                _tooltipIcon.gameObject.SetActive(true);

            ClearLines();
            SetBody(lines);

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
        }

        private void ClearLines()
        {
            foreach (Transform child in _tooltipBody.transform)
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
                    var entry = Instantiate(_singleLinePrefab, _tooltipBody.transform);
                    entry.GetComponent<UI_TooltipSingleLine>().Set(line.text);
                }
                else if (line.text.Length == 2)
                {
                    var entry = Instantiate(_doubleLinePrefab, _tooltipBody.transform);
                    entry.GetComponent<UI_TooltipDoubleLine>().Set(line.text);
                }
            }
        }
    }
}
