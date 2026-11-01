using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Sets supported shader state on an explicitly owned UI material instance.
    /// </summary>
    public static class UIShaderProperties
    {
        private static readonly int FILL_AMOUNT_PROPERTY = Shader.PropertyToID("_FillAmount");
        private static readonly int MAIN_COLOR_PROPERTY = Shader.PropertyToID("_MainColor");
        private static readonly int BORDER_COLOR_PROPERTY = Shader.PropertyToID("_BorderColor");
        private static readonly int SELECTED_AMOUNT_PROPERTY = Shader.PropertyToID("_SelectedAmount");
        private static readonly int DISABLED_AMOUNT_PROPERTY = Shader.PropertyToID("_DisabledAmount");
        private static readonly int ACCENT_COLOR_PROPERTY = Shader.PropertyToID("_AccentColor");
        private static readonly int ICON_TEXTURE_PROPERTY = Shader.PropertyToID("_IconTex");
        private static readonly int ICON_AMOUNT_PROPERTY = Shader.PropertyToID("_IconAmount");
        private static readonly int RARITY_COLOR_PROPERTY = Shader.PropertyToID("_RarityColor");
        private static readonly int RARITY_AMOUNT_PROPERTY = Shader.PropertyToID("_RarityAmount");
        private static readonly int TAB_ACTIVE_PROPERTY = Shader.PropertyToID("_IsActive");
        private static readonly int HOVER_AMOUNT_PROPERTY = Shader.PropertyToID("_HoverAmount");
        private static readonly int PRESSED_AMOUNT_PROPERTY = Shader.PropertyToID("_PressedAmount");

        public static void SetFillAmount(Image image, float value) => SetFloat(image, FILL_AMOUNT_PROPERTY, value);

        public static void SetMainColor(Image image, Color value) => SetColor(image, MAIN_COLOR_PROPERTY, value);

        public static void SetBorderColor(Image image, Color value) => SetColor(image, BORDER_COLOR_PROPERTY, value);

        public static void SetSelected(Image image, bool selected) => SetSelectedAmount(image, selected ? 1f : 0f);

        public static void SetSelectedAmount(Image image, float value) => SetFloat(image, SELECTED_AMOUNT_PROPERTY, value);

        public static void SetDisabled(Image image, bool disabled) => SetFloat(image, DISABLED_AMOUNT_PROPERTY, disabled ? 1f : 0f);

        public static void SetAccentColor(Image image, Color value) => SetColor(image, ACCENT_COLOR_PROPERTY, value);

        public static void SetIcon(Image image, Texture texture) => SetTexture(image, ICON_TEXTURE_PROPERTY, texture);

        public static void SetIconVisible(Image image, bool visible) => SetFloat(image, ICON_AMOUNT_PROPERTY, visible ? 1f : 0f);

        public static void SetRarityColor(Image image, Color value) => SetColor(image, RARITY_COLOR_PROPERTY, value);

        public static void SetRarityVisible(Image image, bool visible) => SetFloat(image, RARITY_AMOUNT_PROPERTY, visible ? 1f : 0f);

        public static void SetTabActive(Image image, bool active) => SetFloat(image, TAB_ACTIVE_PROPERTY, active ? 1f : 0f);

        public static void SetHover(Image image, bool hovered) => SetHoverAmount(image, hovered ? 1f : 0f);

        public static void SetHoverAmount(Image image, float value) => SetFloat(image, HOVER_AMOUNT_PROPERTY, value);

        public static void SetPressed(Image image, bool pressed) => SetPressedAmount(image, pressed ? 1f : 0f);

        public static void SetPressedAmount(Image image, float value) => SetFloat(image, PRESSED_AMOUNT_PROPERTY, value);

        private static Material GetMaterial(Image image)
        {
            return image != null ? image.material : null;
        }

        private static void SetTexture(Image image, int propertyId, Texture texture)
        {
            var material = GetMaterial(image);
            if (material != null)
                material.SetTexture(propertyId, texture != null ? texture : Texture2D.whiteTexture);
        }

        private static void SetFloat(Image image, int propertyId, float value)
        {
            var material = GetMaterial(image);
            if (material != null)
                material.SetFloat(propertyId, value);
        }

        private static void SetColor(Image image, int propertyId, Color value)
        {
            var material = GetMaterial(image);
            if (material != null)
                material.SetColor(propertyId, value);
        }
    }
}
