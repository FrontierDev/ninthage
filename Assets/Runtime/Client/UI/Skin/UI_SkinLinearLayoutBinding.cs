using Game.Client.UI.Configuration;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one skin-owned linear layout group with spacing and padding metric mappings.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinLinearLayoutBinding
    {
        [SerializeField] private HorizontalOrVerticalLayoutGroup _target;
        [SerializeField] private bool _applySpacing = true;
        [SerializeField] private UITokens.Metric _spacingMetric = UITokens.Metric.SpacingMedium;
        [SerializeField] private bool _applyPadding = true;
        [SerializeField] private UITokens.Metric _paddingMetric = UITokens.Metric.WindowPadding;

        /// <summary>
        /// Creates an empty linear layout binding for Unity serialization.
        /// </summary>
        public UI_SkinLinearLayoutBinding()
        {
        }

        /// <summary>
        /// Creates a linear layout binding with explicit spacing and padding mappings.
        /// </summary>
        /// <param name="target">The linear layout group owned by the skin.</param>
        /// <param name="applySpacing">Whether the spacing metric should be applied.</param>
        /// <param name="spacingMetric">The metric token used for spacing.</param>
        /// <param name="applyPadding">Whether the padding metric should be applied.</param>
        /// <param name="paddingMetric">The metric token used for uniform padding.</param>
        public UI_SkinLinearLayoutBinding(
            HorizontalOrVerticalLayoutGroup target,
            bool applySpacing,
            UITokens.Metric spacingMetric,
            bool applyPadding,
            UITokens.Metric paddingMetric)
        {
            _target = target;
            _applySpacing = applySpacing;
            _spacingMetric = spacingMetric;
            _applyPadding = applyPadding;
            _paddingMetric = paddingMetric;
        }

        /// <summary>
        /// Gets the target linear layout group.
        /// </summary>
        public HorizontalOrVerticalLayoutGroup Target => _target;

        /// <summary>
        /// Gets whether the spacing metric should be applied.
        /// </summary>
        public bool ApplySpacing => _applySpacing;

        /// <summary>
        /// Gets the metric token used for spacing.
        /// </summary>
        public UITokens.Metric SpacingMetric => _spacingMetric;

        /// <summary>
        /// Gets whether the padding metric should be applied.
        /// </summary>
        public bool ApplyPadding => _applyPadding;

        /// <summary>
        /// Gets the metric token used for uniform padding.
        /// </summary>
        public UITokens.Metric PaddingMetric => _paddingMetric;
    }
}
