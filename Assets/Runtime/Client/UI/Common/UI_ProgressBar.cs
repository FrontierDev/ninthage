using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ProgressBar</c> implementation.
    /// </summary>
    public abstract class UI_ProgressBar : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] internal Image graphics;

        /// <summary>
        /// Sets the value managed by <c>SetMaterial</c>.
        /// </summary>
        public virtual void SetMaterial(Material material)
        {
            if (graphics != null && material != null)
                graphics.material = new Material(material);
        }

        /// <summary>
        /// Executes the client-side <c>Show</c> operation.
        /// </summary>
        public void Show()
        {
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Executes the client-side <c>Hide</c> operation.
        /// </summary>
        public void Hide()
        {
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Sets the value managed by <c>SetProgress</c>.
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            UIShaderProperties.SetFloat(graphics, "_FillAmount", progress);
        }

        /// <summary>
        /// Sets the value managed by <c>SetColor</c>.
        /// </summary>
        public void SetColor(Color color)
        {
            UIShaderProperties.SetColor(graphics, "_MainColor", color);
        }
    }
}