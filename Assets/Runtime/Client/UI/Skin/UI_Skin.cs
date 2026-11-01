using Game.Client.UI.Configuration;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Applies the configuration-backed text, colour and dimension contract for the UI hierarchy rooted at this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UI_Skin : MonoBehaviour
    {
        [SerializeField] private UIConfiguration _configuration;
        [SerializeField] private List<UI_SkinTextBinding> _textBindings = new();
        [SerializeField] private List<UI_SkinRectTransformBinding> _rectTransformBindings = new();
        [SerializeField] private List<UI_SkinGraphicColorBinding> _graphicColorBindings = new();
        [SerializeField] private List<UI_SkinLinearLayoutBinding> _linearLayoutBindings = new();
        [SerializeField] private List<UI_SkinGridLayoutBinding> _gridLayoutBindings = new();
        [SerializeField] private List<UI_SkinLayoutElementBinding> _layoutElementBindings = new();

        /// <summary>
        /// Gets the client UI configuration used to resolve this skin's text styles.
        /// </summary>
        public UIConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the bindings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinTextBinding> TextBindings => _textBindings;

        /// <summary>
        /// Gets the explicit RectTransform metric mappings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinRectTransformBinding> RectTransformBindings => _rectTransformBindings;

        /// <summary>
        /// Gets the explicit semantic colour mappings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinGraphicColorBinding> GraphicColorBindings => _graphicColorBindings;

        /// <summary>
        /// Gets the explicit linear layout metric mappings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinLinearLayoutBinding> LinearLayoutBindings => _linearLayoutBindings;

        /// <summary>
        /// Gets the explicit grid layout metric mappings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinGridLayoutBinding> GridLayoutBindings => _gridLayoutBindings;

        /// <summary>
        /// Gets the explicit LayoutElement metric mappings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinLayoutElementBinding> LayoutElementBindings => _layoutElementBindings;

        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Synchronizes this skin's bindings with its currently owned descendant text components.
        /// </summary>
        public void RefreshTextBindings()
        {
            var ownedTexts = new List<TMP_Text>();
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsOwnedByThisSkin(text.transform))
                    ownedTexts.Add(text);
            }

            ownedTexts.Sort((left, right) => string.CompareOrdinal(GetTextHierarchyPath(left), GetTextHierarchyPath(right)));

            var existingBindings = new Dictionary<TMP_Text, UI_SkinTextBinding>();
            if (_textBindings != null)
            {
                foreach (var binding in _textBindings)
                {
                    if (binding == null || binding.Text == null || !IsOwnedByThisSkin(binding.Text.transform))
                        continue;

                    if (!existingBindings.ContainsKey(binding.Text))
                        existingBindings.Add(binding.Text, binding);
                }
            }

            var refreshedBindings = new List<UI_SkinTextBinding>(ownedTexts.Count);
            foreach (var text in ownedTexts)
            {
                if (!existingBindings.TryGetValue(text, out var binding))
                {
                    binding = new UI_SkinTextBinding(text, UITokens.TextStyle.Body);
                }

                refreshedBindings.Add(binding);
            }

            _textBindings = refreshedBindings;
        }

        /// <summary>
        /// Defines the skin application boundary that validates and applies resolved text, colour and dimension tokens.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when any binding is invalid.</exception>
        public void Apply()
        {
            ValidateTextBindings();
            ValidateRectTransformBindings();
            ValidateGraphicColorBindings();
            ValidateLinearLayoutBindings();
            ValidateGridLayoutBindings();
            ValidateLayoutElementBindings();

            foreach (var binding in _textBindings)
            {
                var style = _configuration.GetTextStyle(binding.TextStyle);
                binding.Text.font = style.Font;
                binding.Text.fontSize = style.FontSize;
                binding.Text.fontStyle = style.FontStyle;
                binding.Text.fontWeight = style.FontWeight;
            }

            foreach (var binding in _rectTransformBindings)
            {
                if (binding.ApplyWidth)
                {
                    binding.Target.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal,
                        _configuration.GetMetric(binding.WidthMetric));
                }

                if (binding.ApplyHeight)
                {
                    binding.Target.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        _configuration.GetMetric(binding.HeightMetric));
                }
            }

            foreach (var binding in _graphicColorBindings)
                binding.Target.color = _configuration.GetColor(binding.Color);

            foreach (var binding in _linearLayoutBindings)
            {
                if (binding.ApplySpacing)
                    binding.Target.spacing = _configuration.GetMetric(binding.SpacingMetric);

                if (binding.ApplyPadding)
                {
                    var padding = (int)_configuration.GetMetric(binding.PaddingMetric);
                    binding.Target.padding = new RectOffset(padding, padding, padding, padding);
                }
            }

            foreach (var binding in _gridLayoutBindings)
            {
                if (binding.ApplyCellWidth || binding.ApplyCellHeight)
                {
                    var cellSize = binding.Target.cellSize;
                    if (binding.ApplyCellWidth)
                        cellSize.x = _configuration.GetMetric(binding.CellWidthMetric);
                    if (binding.ApplyCellHeight)
                        cellSize.y = _configuration.GetMetric(binding.CellHeightMetric);

                    binding.Target.cellSize = cellSize;
                }

                if (binding.ApplySpacingX || binding.ApplySpacingY)
                {
                    var spacing = binding.Target.spacing;
                    if (binding.ApplySpacingX)
                        spacing.x = _configuration.GetMetric(binding.SpacingXMetric);
                    if (binding.ApplySpacingY)
                        spacing.y = _configuration.GetMetric(binding.SpacingYMetric);

                    binding.Target.spacing = spacing;
                }

                if (binding.ApplyPadding)
                {
                    var padding = (int)_configuration.GetMetric(binding.PaddingMetric);
                    binding.Target.padding = new RectOffset(padding, padding, padding, padding);
                }
            }

            foreach (var binding in _layoutElementBindings)
            {
                if (binding.ApplyMinWidth)
                    binding.Target.minWidth = _configuration.GetMetric(binding.MinWidthMetric);
                if (binding.ApplyMinHeight)
                    binding.Target.minHeight = _configuration.GetMetric(binding.MinHeightMetric);
                if (binding.ApplyPreferredWidth)
                    binding.Target.preferredWidth = _configuration.GetMetric(binding.PreferredWidthMetric);
                if (binding.ApplyPreferredHeight)
                    binding.Target.preferredHeight = _configuration.GetMetric(binding.PreferredHeightMetric);
            }
        }

        private void ValidateTextBindings()
        {
            if (_configuration == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has no UIConfiguration assigned.");
            if (_textBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null text binding list.");

            var boundTexts = new HashSet<TMP_Text>();
            for (var index = 0; index < _textBindings.Count; index++)
            {
                var binding = _textBindings[index];
                if (binding == null)
                    throw new InvalidOperationException($"UI_Skin '{name}' has a null text binding at index {index}.");

                var text = binding.Text;
                if (text == null)
                    throw new InvalidOperationException($"UI_Skin '{name}' has a binding at index {index} with no TMP_Text.");

                if (!IsOwnedByThisSkin(text.transform))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} references TMP_Text '{text.name}' that is not owned by this skin.");

                if (!boundTexts.Add(text))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate TMP_Text '{text.name}' at binding index {index}.");

                try
                {
                    _configuration.GetTextStyle(binding.TextStyle);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} for TMP_Text '{text.name}' has an unsupported text-style token.",
                        exception);
                }
                catch (InvalidOperationException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} for TMP_Text '{text.name}' cannot resolve its text-style token.",
                        exception);
                }
            }
        }

        private void ValidateRectTransformBindings()
        {
            if (_rectTransformBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null RectTransform binding list.");

            var boundTargets = new HashSet<RectTransform>();
            for (var index = 0; index < _rectTransformBindings.Count; index++)
            {
                var binding = _rectTransformBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a null RectTransform binding at index {index}.");

                var target = binding.Target;
                if (target == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a RectTransform binding at index {index} with no target.");

                if (!IsOwnedByThisSkin(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' RectTransform binding at index {index} references target '{target.name}' that is not owned by this skin.");

                if (!boundTargets.Add(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate RectTransform target '{target.name}' at binding index {index}.");

                if (!binding.ApplyWidth && !binding.ApplyHeight)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' RectTransform binding at index {index} for target '{target.name}' has no enabled axis.");

                if (binding.ApplyWidth)
                    ValidateMetric(binding.WidthMetric, index, target, "width");
                if (binding.ApplyHeight)
                    ValidateMetric(binding.HeightMetric, index, target, "height");
            }
        }

        private void ValidateGraphicColorBindings()
        {
            if (_graphicColorBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null graphic colour binding list.");

            var boundTargets = new HashSet<Graphic>();
            for (var index = 0; index < _graphicColorBindings.Count; index++)
            {
                var binding = _graphicColorBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a null graphic colour binding at index {index}.");

                var target = binding.Target;
                if (target == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a graphic colour binding at index {index} with no target.");

                if (!IsOwnedByThisSkin(target.transform))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' graphic colour binding at index {index} references target '{target.name}' that is not owned by this skin.");

                if (!boundTargets.Add(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate graphic colour target '{target.name}' at binding index {index}.");

                try
                {
                    _configuration.GetColor(binding.Color);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' graphic colour binding at index {index} for target '{target.name}' has an unsupported colour token.",
                        exception);
                }
                catch (InvalidOperationException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' graphic colour binding at index {index} for target '{target.name}' cannot resolve its colour token.",
                        exception);
                }
            }
        }

        private void ValidateLinearLayoutBindings()
        {
            if (_linearLayoutBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null linear layout binding list.");

            var boundTargets = new HashSet<HorizontalOrVerticalLayoutGroup>();
            for (var index = 0; index < _linearLayoutBindings.Count; index++)
            {
                var binding = _linearLayoutBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a null linear layout binding at index {index}.");

                var target = binding.Target;
                if (target == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a linear layout binding at index {index} with no target.");

                if (!IsOwnedByThisSkin(target.transform))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' linear layout binding at index {index} references target '{target.name}' that is not owned by this skin.");

                if (!boundTargets.Add(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate linear layout target '{target.name}' at binding index {index}.");

                if (!binding.ApplySpacing && !binding.ApplyPadding)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' linear layout binding at index {index} for target '{target.name}' has no enabled property.");

                if (binding.ApplySpacing)
                    ValidateMetric(binding.SpacingMetric, index, target, "linear layout spacing");
                if (binding.ApplyPadding)
                    ValidatePaddingMetric(binding.PaddingMetric, index, target, "linear layout padding");
            }
        }

        private void ValidateGridLayoutBindings()
        {
            if (_gridLayoutBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null grid layout binding list.");

            var boundTargets = new HashSet<GridLayoutGroup>();
            for (var index = 0; index < _gridLayoutBindings.Count; index++)
            {
                var binding = _gridLayoutBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a null grid layout binding at index {index}.");

                var target = binding.Target;
                if (target == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a grid layout binding at index {index} with no target.");

                if (!IsOwnedByThisSkin(target.transform))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' grid layout binding at index {index} references target '{target.name}' that is not owned by this skin.");

                if (!boundTargets.Add(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate grid layout target '{target.name}' at binding index {index}.");

                if (!binding.ApplyCellWidth && !binding.ApplyCellHeight &&
                    !binding.ApplySpacingX && !binding.ApplySpacingY && !binding.ApplyPadding)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' grid layout binding at index {index} for target '{target.name}' has no enabled property.");
                }

                if (binding.ApplyCellWidth)
                    ValidateMetric(binding.CellWidthMetric, index, target, "grid cell width");
                if (binding.ApplyCellHeight)
                    ValidateMetric(binding.CellHeightMetric, index, target, "grid cell height");
                if (binding.ApplySpacingX)
                    ValidateMetric(binding.SpacingXMetric, index, target, "grid spacing X");
                if (binding.ApplySpacingY)
                    ValidateMetric(binding.SpacingYMetric, index, target, "grid spacing Y");
                if (binding.ApplyPadding)
                    ValidatePaddingMetric(binding.PaddingMetric, index, target, "grid padding");
            }
        }

        private void ValidateLayoutElementBindings()
        {
            if (_layoutElementBindings == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has a null LayoutElement binding list.");

            var boundTargets = new HashSet<LayoutElement>();
            for (var index = 0; index < _layoutElementBindings.Count; index++)
            {
                var binding = _layoutElementBindings[index];
                if (binding == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a null LayoutElement binding at index {index}.");

                var target = binding.Target;
                if (target == null)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has a LayoutElement binding at index {index} with no target.");

                if (!IsOwnedByThisSkin(target.transform))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' LayoutElement binding at index {index} references target '{target.name}' that is not owned by this skin.");

                if (!boundTargets.Add(target))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate LayoutElement target '{target.name}' at binding index {index}.");

                if (!binding.ApplyMinWidth && !binding.ApplyMinHeight &&
                    !binding.ApplyPreferredWidth && !binding.ApplyPreferredHeight)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' LayoutElement binding at index {index} for target '{target.name}' has no enabled property.");
                }

                if (binding.ApplyMinWidth)
                    ValidateMetric(binding.MinWidthMetric, index, target, "LayoutElement minimum width");
                if (binding.ApplyMinHeight)
                    ValidateMetric(binding.MinHeightMetric, index, target, "LayoutElement minimum height");
                if (binding.ApplyPreferredWidth)
                    ValidateMetric(binding.PreferredWidthMetric, index, target, "LayoutElement preferred width");
                if (binding.ApplyPreferredHeight)
                    ValidateMetric(binding.PreferredHeightMetric, index, target, "LayoutElement preferred height");
            }
        }

        private void ValidatePaddingMetric(
            UITokens.Metric metric,
            int index,
            Component target,
            string propertyName)
        {
            float value;
            try
            {
                value = _configuration.GetMetric(metric);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' binding at index {index} for target '{target.name}' has an unsupported {propertyName} metric.",
                    exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' binding at index {index} for target '{target.name}' cannot resolve its {propertyName} metric.",
                    exception);
            }

            if ((double)value > int.MaxValue || Math.Abs(value - Mathf.Round(value)) > 0.0001f)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' binding at index {index} for target '{target.name}' has a {propertyName} metric value '{value}' that is not a whole logical pixel within the supported integer range.");
            }
        }

        private void ValidateMetric(UITokens.Metric metric, int index, Component target, string propertyName)
        {
            try
            {
                _configuration.GetMetric(metric);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' binding at index {index} for target '{target.name}' has an unsupported {propertyName} metric.",
                    exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' binding at index {index} for target '{target.name}' cannot resolve its {propertyName} metric.",
                    exception);
            }
        }

        private bool IsOwnedByThisSkin(Transform target)
        {
            if (target == null || (target != transform && !target.IsChildOf(transform)))
                return false;

            var current = target;
            while (current != null)
            {
                var skin = current.GetComponent<UI_Skin>();
                if (skin != null)
                    return skin == this;

                if (current == transform)
                    break;

                current = current.parent;
            }

            return false;
        }

        private string GetTextHierarchyPath(TMP_Text text)
        {
            var pathSegments = new List<string>();
            var current = text.transform;
            while (current != null && current != transform)
            {
                pathSegments.Add($"{current.GetSiblingIndex():D8}:{current.name}");
                current = current.parent;
            }

            pathSegments.Reverse();
            return string.Join("/", pathSegments);
        }
    }
}
