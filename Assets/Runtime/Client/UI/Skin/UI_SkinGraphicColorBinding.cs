using Game.Client.UI.Configuration;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one skin-owned UI graphic with a semantic colour token.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinGraphicColorBinding
    {
        [SerializeField] private Graphic _target;
        [SerializeField] private UITokens.Color _color = UITokens.Color.PrimaryText;

        /// <summary>
        /// Creates an empty graphic colour binding for Unity serialization.
        /// </summary>
        public UI_SkinGraphicColorBinding()
        {
        }

        /// <summary>
        /// Creates a graphic colour binding with the supplied target and semantic colour token.
        /// </summary>
        /// <param name="target">The UI graphic owned by the skin.</param>
        /// <param name="color">The semantic colour token for the graphic.</param>
        public UI_SkinGraphicColorBinding(Graphic target, UITokens.Color color)
        {
            _target = target;
            _color = color;
        }

        /// <summary>
        /// Gets the target UI graphic.
        /// </summary>
        public Graphic Target => _target;

        /// <summary>
        /// Gets the semantic colour token assigned to the graphic.
        /// </summary>
        public UITokens.Color Color => _color;
    }
}
