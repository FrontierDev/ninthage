using Game.Client.UI.Configuration;
using System;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one TextMeshPro text component with its semantic skin text style.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinTextBinding
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private UITokens.TextStyle _textStyle = UITokens.TextStyle.Body;

        /// <summary>
        /// Creates an empty text binding for Unity serialization.
        /// </summary>
        public UI_SkinTextBinding()
        {
        }

        /// <summary>
        /// Creates a text binding with the supplied semantic text style.
        /// </summary>
        /// <param name="text">The TextMeshPro component owned by the binding.</param>
        /// <param name="textStyle">The semantic text-style token for the component.</param>
        public UI_SkinTextBinding(TMP_Text text, UITokens.TextStyle textStyle)
        {
            _text = text;
            _textStyle = textStyle;
        }

        /// <summary>
        /// Gets the TextMeshPro component owned by the binding.
        /// </summary>
        public TMP_Text Text => _text;

        /// <summary>
        /// Gets the semantic text-style token assigned to the text.
        /// </summary>
        public UITokens.TextStyle TextStyle => _textStyle;
    }
}
