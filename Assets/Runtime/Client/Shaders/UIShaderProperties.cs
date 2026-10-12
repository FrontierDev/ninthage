using Debug = Game.Shared.Utility.FormattedDebug;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UIShaderProperties</c> implementation.
    /// </summary>
    public static class UIShaderProperties
    {
        // ── UI Image helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Sets the value managed by <c>SetTexture</c>.
        /// </summary>
        public static void SetTexture(Image image, string propertyName, Texture2D texture)
        {
            var mat = image.material;
            if (mat == null) return;

            mat.SetTexture(propertyName, texture != null ? texture : Texture2D.whiteTexture); // Set a default texture to avoid shader issues when texture is null
        }

        /// <summary>
        /// Sets the value managed by <c>SetFloat</c>.
        /// </summary>
        public static void SetFloat(Image image, string propertyName, float value)
        {
            var mat = image.material;
            if (mat == null) return;

            mat.SetFloat(propertyName, value);
        }

        /// <summary>
        /// Sets the value managed by <c>SetColor</c>.
        /// </summary>
        public static void SetColor(Image image, string propertyName, Color color)
        {
            var mat = image.material;
            if (mat == null) return;

            mat.SetColor(propertyName, color);
        }

        // ── MeshRenderer helpers ─────────────────────────────────────────────
        // These overloads follow the same pattern but operate on a MeshRenderer's
        // instanced material so VFX components can share this utility.

        /// <summary>
        /// Sets the value managed by <c>SetFloat</c>.
        /// </summary>
        public static void SetFloat(MeshRenderer renderer, string propertyName, float value)
        {
            var mat = renderer != null ? renderer.material : null;
            if (mat == null) return;

            mat.SetFloat(propertyName, value);
        }

        /// <summary>
        /// Sets the value managed by <c>SetColor</c>.
        /// </summary>
        public static void SetColor(MeshRenderer renderer, string propertyName, Color color)
        {
            var mat = renderer != null ? renderer.material : null;
            if (mat == null) return;

            mat.SetColor(propertyName, color);
        }

        /// <summary>
        /// Sets the value managed by <c>SetTexture</c>.
        /// </summary>
        public static void SetTexture(MeshRenderer renderer, string propertyName, Texture2D texture)
        {
            var mat = renderer != null ? renderer.material : null;
            if (mat == null) return;

            mat.SetTexture(propertyName, texture != null ? texture : Texture2D.whiteTexture);
        }
    }
}
