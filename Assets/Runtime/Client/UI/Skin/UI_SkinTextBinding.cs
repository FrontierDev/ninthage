using System;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Identifies the font-family source used by a <see cref="UI_SkinTextBinding"/>.
    /// </summary>
    public enum UI_SkinFontRole
    {
        /// <summary>
        /// Uses the skin's heading font.
        /// </summary>
        Heading,

        /// <summary>
        /// Uses the skin's body font.
        /// </summary>
        Body,

        /// <summary>
        /// Uses the skin's numeric font.
        /// </summary>
        Numeric
    }

    /// <summary>
    /// Associates one TextMeshPro text component with its hierarchy-local skin typography.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinTextBinding
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private UI_SkinFontRole _fontRole = UI_SkinFontRole.Body;
        [SerializeField, Min(1f)] private float _fontSize = 13f;
        [SerializeField] private FontStyles _fontStyle = FontStyles.Normal;

        /// <summary>
        /// Creates an empty text binding for Unity serialization.
        /// </summary>
        public UI_SkinTextBinding()
        {
        }

        /// <summary>
        /// Creates a text binding with the supplied typography values.
        /// </summary>
        /// <param name="text">The TextMeshPro component owned by the binding.</param>
        /// <param name="fontRole">The semantic font role for the component.</param>
        /// <param name="fontSize">The authored font size.</param>
        /// <param name="fontStyle">The authored font style.</param>
        public UI_SkinTextBinding(TMP_Text text, UI_SkinFontRole fontRole, float fontSize, FontStyles fontStyle)
        {
            _text = text;
            _fontRole = fontRole;
            _fontSize = fontSize;
            _fontStyle = fontStyle;
        }

        /// <summary>
        /// Gets the TextMeshPro component owned by the binding.
        /// </summary>
        public TMP_Text Text => _text;

        /// <summary>
        /// Gets the semantic font role assigned to the text.
        /// </summary>
        public UI_SkinFontRole FontRole => _fontRole;

        /// <summary>
        /// Gets the font size assigned to the text.
        /// </summary>
        public float FontSize => _fontSize;

        /// <summary>
        /// Gets the font style assigned to the text.
        /// </summary>
        public FontStyles FontStyle => _fontStyle;
    }
}
