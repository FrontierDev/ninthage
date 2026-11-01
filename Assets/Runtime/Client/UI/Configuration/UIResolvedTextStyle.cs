using TMPro;

namespace Game.Client.UI.Configuration
{
    /// <summary>
    /// Represents one text style after its font role has been resolved by a <see cref="UIConfiguration"/>.
    /// </summary>
    public readonly struct UIResolvedTextStyle
    {
        /// <summary>
        /// Initializes a resolved text style.
        /// </summary>
        /// <param name="font">The resolved TextMeshPro font asset.</param>
        /// <param name="fontSize">The resolved font size.</param>
        /// <param name="fontStyle">The resolved TextMeshPro font style.</param>
        /// <param name="fontWeight">The resolved TextMeshPro font weight.</param>
        public UIResolvedTextStyle(TMP_FontAsset font, float fontSize, FontStyles fontStyle, FontWeight fontWeight)
        {
            Font = font;
            FontSize = fontSize;
            FontStyle = fontStyle;
            FontWeight = fontWeight;
        }

        /// <summary>
        /// Gets the resolved TextMeshPro font asset.
        /// </summary>
        public TMP_FontAsset Font { get; }

        /// <summary>
        /// Gets the resolved font size.
        /// </summary>
        public float FontSize { get; }

        /// <summary>
        /// Gets the resolved TextMeshPro font style.
        /// </summary>
        public FontStyles FontStyle { get; }

        /// <summary>
        /// Gets the resolved TextMeshPro font weight.
        /// </summary>
        public FontWeight FontWeight { get; }
    }
}
