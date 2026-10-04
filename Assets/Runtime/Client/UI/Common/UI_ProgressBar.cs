using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public abstract class UI_ProgressBar : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] internal Image graphics;

        public virtual void SetMaterial(Material material)
        {
            if (graphics != null && material != null)
                graphics.material = new Material(material);
        }

        public void Show()
        {
            canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            UIShaderProperties.SetFloat(graphics, "_FillAmount", progress);
        }

        public void SetColor(Color color)
        {
            UIShaderProperties.SetColor(graphics, "_MainColor", color);
        }
    }
}