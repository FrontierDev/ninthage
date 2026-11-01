using Game.Client.UI.Configuration;
using System;
using UnityEngine;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one skin-owned RectTransform with explicit width and height metric mappings.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinRectTransformBinding
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private bool _applyWidth = true;
        [SerializeField] private UITokens.Metric _widthMetric = UITokens.Metric.AbilityIconSize;
        [SerializeField] private bool _applyHeight = true;
        [SerializeField] private UITokens.Metric _heightMetric = UITokens.Metric.AbilityIconSize;

        /// <summary>
        /// Creates an empty RectTransform binding for Unity serialization.
        /// </summary>
        public UI_SkinRectTransformBinding()
        {
        }

        /// <summary>
        /// Creates a RectTransform binding with explicit axis mappings.
        /// </summary>
        /// <param name="target">The RectTransform owned by the skin.</param>
        /// <param name="applyWidth">Whether the width metric should be applied.</param>
        /// <param name="widthMetric">The metric token used for width.</param>
        /// <param name="applyHeight">Whether the height metric should be applied.</param>
        /// <param name="heightMetric">The metric token used for height.</param>
        public UI_SkinRectTransformBinding(
            RectTransform target,
            bool applyWidth,
            UITokens.Metric widthMetric,
            bool applyHeight,
            UITokens.Metric heightMetric)
        {
            _target = target;
            _applyWidth = applyWidth;
            _widthMetric = widthMetric;
            _applyHeight = applyHeight;
            _heightMetric = heightMetric;
        }

        /// <summary>
        /// Gets the target RectTransform.
        /// </summary>
        public RectTransform Target => _target;

        /// <summary>
        /// Gets whether the width metric should be applied.
        /// </summary>
        public bool ApplyWidth => _applyWidth;

        /// <summary>
        /// Gets the metric token used for width.
        /// </summary>
        public UITokens.Metric WidthMetric => _widthMetric;

        /// <summary>
        /// Gets whether the height metric should be applied.
        /// </summary>
        public bool ApplyHeight => _applyHeight;

        /// <summary>
        /// Gets the metric token used for height.
        /// </summary>
        public UITokens.Metric HeightMetric => _heightMetric;
    }
}
