using Game.Client.UI.Configuration;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one skin-owned LayoutElement with explicit minimum and preferred dimension metrics.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinLayoutElementBinding
    {
        [SerializeField] private LayoutElement _target;
        [SerializeField] private bool _applyMinWidth;
        [SerializeField] private UITokens.Metric _minWidthMetric = UITokens.Metric.StandardRowHeight;
        [SerializeField] private bool _applyMinHeight;
        [SerializeField] private UITokens.Metric _minHeightMetric = UITokens.Metric.StandardRowHeight;
        [SerializeField] private bool _applyPreferredWidth;
        [SerializeField] private UITokens.Metric _preferredWidthMetric = UITokens.Metric.StandardRowHeight;
        [SerializeField] private bool _applyPreferredHeight = true;
        [SerializeField] private UITokens.Metric _preferredHeightMetric = UITokens.Metric.StandardRowHeight;

        /// <summary>
        /// Creates an empty LayoutElement binding for Unity serialization.
        /// </summary>
        public UI_SkinLayoutElementBinding()
        {
        }

        /// <summary>
        /// Creates a LayoutElement binding with explicit dimension mappings.
        /// </summary>
        /// <param name="target">The LayoutElement owned by the skin.</param>
        /// <param name="applyMinWidth">Whether the minimum-width metric should be applied.</param>
        /// <param name="minWidthMetric">The metric token used for minimum width.</param>
        /// <param name="applyMinHeight">Whether the minimum-height metric should be applied.</param>
        /// <param name="minHeightMetric">The metric token used for minimum height.</param>
        /// <param name="applyPreferredWidth">Whether the preferred-width metric should be applied.</param>
        /// <param name="preferredWidthMetric">The metric token used for preferred width.</param>
        /// <param name="applyPreferredHeight">Whether the preferred-height metric should be applied.</param>
        /// <param name="preferredHeightMetric">The metric token used for preferred height.</param>
        public UI_SkinLayoutElementBinding(
            LayoutElement target,
            bool applyMinWidth,
            UITokens.Metric minWidthMetric,
            bool applyMinHeight,
            UITokens.Metric minHeightMetric,
            bool applyPreferredWidth,
            UITokens.Metric preferredWidthMetric,
            bool applyPreferredHeight,
            UITokens.Metric preferredHeightMetric)
        {
            _target = target;
            _applyMinWidth = applyMinWidth;
            _minWidthMetric = minWidthMetric;
            _applyMinHeight = applyMinHeight;
            _minHeightMetric = minHeightMetric;
            _applyPreferredWidth = applyPreferredWidth;
            _preferredWidthMetric = preferredWidthMetric;
            _applyPreferredHeight = applyPreferredHeight;
            _preferredHeightMetric = preferredHeightMetric;
        }

        /// <summary>
        /// Gets the target LayoutElement.
        /// </summary>
        public LayoutElement Target => _target;

        /// <summary>
        /// Gets whether the minimum-width metric should be applied.
        /// </summary>
        public bool ApplyMinWidth => _applyMinWidth;

        /// <summary>
        /// Gets the metric token used for minimum width.
        /// </summary>
        public UITokens.Metric MinWidthMetric => _minWidthMetric;

        /// <summary>
        /// Gets whether the minimum-height metric should be applied.
        /// </summary>
        public bool ApplyMinHeight => _applyMinHeight;

        /// <summary>
        /// Gets the metric token used for minimum height.
        /// </summary>
        public UITokens.Metric MinHeightMetric => _minHeightMetric;

        /// <summary>
        /// Gets whether the preferred-width metric should be applied.
        /// </summary>
        public bool ApplyPreferredWidth => _applyPreferredWidth;

        /// <summary>
        /// Gets the metric token used for preferred width.
        /// </summary>
        public UITokens.Metric PreferredWidthMetric => _preferredWidthMetric;

        /// <summary>
        /// Gets whether the preferred-height metric should be applied.
        /// </summary>
        public bool ApplyPreferredHeight => _applyPreferredHeight;

        /// <summary>
        /// Gets the metric token used for preferred height.
        /// </summary>
        public UITokens.Metric PreferredHeightMetric => _preferredHeightMetric;
    }
}
