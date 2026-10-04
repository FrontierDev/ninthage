using UnityEngine;

namespace Game.Client.UI
{
    public abstract class UI_WorldSpaceUIManager : MonoBehaviour
    {

        private readonly Vector3[] cornerBuffer = new Vector3[4];
        protected virtual float GetScreenHeight(UI_Nameplate nameplate, Camera cam)
        {
            // Compute bounds from all child RectTransforms to get the actual rendered extents,
            // since the root RectTransform may have stale layout from ContentSizeFitter.
            float minScreenY = float.MaxValue;
            float maxScreenY = float.MinValue;

            var children = nameplate.GetComponentsInChildren<RectTransform>(false);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].GetWorldCorners(cornerBuffer);
                for (int c = 0; c < 4; c++)
                {
                    Vector3 sp = cam.WorldToScreenPoint(cornerBuffer[c]);
                    if (sp.z <= 0f) return 0f;
                    if (sp.y < minScreenY) minScreenY = sp.y;
                    if (sp.y > maxScreenY) maxScreenY = sp.y;
                }
            }

            if (minScreenY >= maxScreenY) return 0f;
            return maxScreenY - minScreenY;
        }
    }
}