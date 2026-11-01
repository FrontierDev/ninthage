using Game.Client.UI.Configuration;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Applies the configuration-backed text and dimension contract for the UI hierarchy rooted at this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UI_Skin : MonoBehaviour
    {
        [SerializeField] private UIConfiguration _configuration;
        [SerializeField] private List<UI_SkinTextBinding> _textBindings = new();
        [SerializeField] private List<UI_SkinRectTransformBinding> _rectTransformBindings = new();

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
        /// Defines the skin application boundary that validates and applies resolved text and dimension tokens.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when any binding is invalid.</exception>
        public void Apply()
        {
            ValidateTextBindings();
            ValidateRectTransformBindings();

            foreach (var binding in _textBindings)
            {
                var style = _configuration.GetTextStyle(binding.TextStyle);
                binding.Text.font = style.Font;
                binding.Text.fontSize = style.FontSize;
                binding.Text.fontStyle = style.FontStyle;
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

        private void ValidateMetric(UITokens.Metric metric, int index, RectTransform target, string axis)
        {
            try
            {
                _configuration.GetMetric(metric);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' RectTransform binding at index {index} for target '{target.name}' has an unsupported {axis} metric.",
                    exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"UI_Skin '{name}' RectTransform binding at index {index} for target '{target.name}' cannot resolve its {axis} metric.",
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
