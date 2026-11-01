using Game.Client.UI.Configuration;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Associates one skin-owned grid layout group with cell, spacing and padding metric mappings.
    /// </summary>
    [Serializable]
    public sealed class UI_SkinGridLayoutBinding
    {
        [SerializeField] private GridLayoutGroup _target;
        [SerializeField] private bool _applyCellWidth = true;
        [SerializeField] private UITokens.Metric _cellWidthMetric = UITokens.Metric.AbilityIconSize;
        [SerializeField] private bool _applyCellHeight = true;
        [SerializeField] private UITokens.Metric _cellHeightMetric = UITokens.Metric.AbilityIconSize;
        [SerializeField] private bool _applySpacingX = true;
        [SerializeField] private UITokens.Metric _spacingXMetric = UITokens.Metric.SpacingSmall;
        [SerializeField] private bool _applySpacingY = true;
        [SerializeField] private UITokens.Metric _spacingYMetric = UITokens.Metric.SpacingSmall;
        [SerializeField] private bool _applyPadding = false;
        [SerializeField] private UITokens.Metric _paddingMetric = UITokens.Metric.CompactPanelPadding;

        /// <summary>
        /// Creates an empty grid layout binding for Unity serialization.
        /// </summary>
        public UI_SkinGridLayoutBinding()
        {
        }

        /// <summary>
        /// Creates a grid layout binding with explicit cell, spacing and padding mappings.
        /// </summary>
        /// <param name="target">The grid layout group owned by the skin.</param>
        /// <param name="applyCellWidth">Whether the cell-width metric should be applied.</param>
        /// <param name="cellWidthMetric">The metric token used for cell width.</param>
        /// <param name="applyCellHeight">Whether the cell-height metric should be applied.</param>
        /// <param name="cellHeightMetric">The metric token used for cell height.</param>
        /// <param name="applySpacingX">Whether the horizontal-spacing metric should be applied.</param>
        /// <param name="spacingXMetric">The metric token used for horizontal spacing.</param>
        /// <param name="applySpacingY">Whether the vertical-spacing metric should be applied.</param>
        /// <param name="spacingYMetric">The metric token used for vertical spacing.</param>
        /// <param name="applyPadding">Whether the padding metric should be applied.</param>
        /// <param name="paddingMetric">The metric token used for uniform padding.</param>
        public UI_SkinGridLayoutBinding(
            GridLayoutGroup target,
            bool applyCellWidth,
            UITokens.Metric cellWidthMetric,
            bool applyCellHeight,
            UITokens.Metric cellHeightMetric,
            bool applySpacingX,
            UITokens.Metric spacingXMetric,
            bool applySpacingY,
            UITokens.Metric spacingYMetric,
            bool applyPadding,
            UITokens.Metric paddingMetric)
        {
            _target = target;
            _applyCellWidth = applyCellWidth;
            _cellWidthMetric = cellWidthMetric;
            _applyCellHeight = applyCellHeight;
            _cellHeightMetric = cellHeightMetric;
            _applySpacingX = applySpacingX;
            _spacingXMetric = spacingXMetric;
            _applySpacingY = applySpacingY;
            _spacingYMetric = spacingYMetric;
            _applyPadding = applyPadding;
            _paddingMetric = paddingMetric;
        }

        /// <summary>
        /// Gets the target grid layout group.
        /// </summary>
        public GridLayoutGroup Target => _target;

        /// <summary>
        /// Gets whether the cell-width metric should be applied.
        /// </summary>
        public bool ApplyCellWidth => _applyCellWidth;

        /// <summary>
        /// Gets the metric token used for cell width.
        /// </summary>
        public UITokens.Metric CellWidthMetric => _cellWidthMetric;

        /// <summary>
        /// Gets whether the cell-height metric should be applied.
        /// </summary>
        public bool ApplyCellHeight => _applyCellHeight;

        /// <summary>
        /// Gets the metric token used for cell height.
        /// </summary>
        public UITokens.Metric CellHeightMetric => _cellHeightMetric;

        /// <summary>
        /// Gets whether the horizontal-spacing metric should be applied.
        /// </summary>
        public bool ApplySpacingX => _applySpacingX;

        /// <summary>
        /// Gets the metric token used for horizontal spacing.
        /// </summary>
        public UITokens.Metric SpacingXMetric => _spacingXMetric;

        /// <summary>
        /// Gets whether the vertical-spacing metric should be applied.
        /// </summary>
        public bool ApplySpacingY => _applySpacingY;

        /// <summary>
        /// Gets the metric token used for vertical spacing.
        /// </summary>
        public UITokens.Metric SpacingYMetric => _spacingYMetric;

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
