using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_TooltipWindow : UI_Window
    {
        private static UI_TooltipWindow _instance;
        public static UI_TooltipWindow Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
        }

        public void Show(string header, UI_TooltipLine[] body, string footer, Sprite icon)
        {
            (panels[0] as UI_TooltipPanel).SetTooltip(header, body, footer, icon);
            base.Show();
        }

        public void SetPosition(Transform target, float gap = 0f)
        {
            if (target == null) return;

            RectTransform tooltipRect = GetComponent<RectTransform>();
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // Set pivot to top-left so position = where the top-left corner lands.
            tooltipRect.pivot = new Vector2(0f, 1f);

            // Ensure layout is current so corners reflect actual content size.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            Vector3[] targetCorners = new Vector3[4];
            targetRect.GetWorldCorners(targetCorners);

            Vector3[] tooltipCorners = new Vector3[4];
            tooltipRect.GetWorldCorners(tooltipCorners);

            float tooltipWidth = tooltipCorners[2].x - tooltipCorners[0].x;
            float tooltipHeight = tooltipCorners[1].y - tooltipCorners[0].y;

            // Snap top-left of tooltip to top-right of target.
            Vector3 position = targetCorners[2] + new Vector3(gap, 0f, 0f);

            // If it would leave the right edge, place it to the left of the target instead.
            if (position.x + tooltipWidth > Screen.width)
                position.x = targetCorners[1].x - tooltipWidth - gap;

            // Keep the tooltip fully on screen vertically and horizontally.
            position.x = Mathf.Clamp(position.x, 0f, Screen.width - tooltipWidth);
            position.y = Mathf.Clamp(position.y, tooltipHeight, Screen.height);

            tooltipRect.position = new Vector3(position.x + 32, position.y - 32, tooltipRect.position.z);
        }
    }
}