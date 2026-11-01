using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.Skin
{
    /// <summary>
    /// Owns the typography contract for the UI hierarchy rooted at this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UI_Skin : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _headingFont;
        [SerializeField] private TMP_FontAsset _bodyFont;
        [SerializeField] private TMP_FontAsset _numericFont;
        [SerializeField] private List<UI_SkinTextBinding> _textBindings = new();

        /// <summary>
        /// Gets the bindings owned by this skin.
        /// </summary>
        public IReadOnlyList<UI_SkinTextBinding> TextBindings => _textBindings;

        private void Awake()
        {
            ApplyTextBindings();
        }

        /// <summary>
        /// Resolves the configured font for a semantic role.
        /// </summary>
        /// <param name="role">The semantic font role to resolve.</param>
        /// <returns>The configured TextMeshPro font asset for the role.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="role"/> is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the selected role has no configured font.</exception>
        public TMP_FontAsset GetFont(UI_SkinFontRole role)
        {
            TMP_FontAsset font;
            switch (role)
            {
                case UI_SkinFontRole.Heading:
                    font = _headingFont;
                    break;
                case UI_SkinFontRole.Body:
                    font = _bodyFont;
                    break;
                case UI_SkinFontRole.Numeric:
                    font = _numericFont;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported UI skin font role.");
            }

            if (font == null)
                throw new InvalidOperationException($"UI_Skin '{name}' has no font configured for role '{role}'.");

            return font;
        }

        /// <summary>
        /// Synchronizes this skin's bindings with its currently owned descendant text components.
        /// </summary>
        public void RefreshTextBindings()
        {
            var ownedTexts = new List<TMP_Text>();
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsTextOwnedByThisSkin(text))
                    ownedTexts.Add(text);
            }

            ownedTexts.Sort((left, right) => string.CompareOrdinal(GetTextHierarchyPath(left), GetTextHierarchyPath(right)));

            var existingBindings = new Dictionary<TMP_Text, UI_SkinTextBinding>();
            if (_textBindings != null)
            {
                foreach (var binding in _textBindings)
                {
                    if (binding == null || binding.Text == null || !IsTextOwnedByThisSkin(binding.Text))
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
                    binding = new UI_SkinTextBinding(text, UI_SkinFontRole.Body, text.fontSize, text.fontStyle);
                }

                refreshedBindings.Add(binding);
            }

            _textBindings = refreshedBindings;
        }

        /// <summary>
        /// Validates and applies this skin's configured typography to every bound text component.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when any binding is invalid.</exception>
        public void ApplyTextBindings()
        {
            ValidateTextBindings();

            foreach (var binding in _textBindings)
            {
                var text = binding.Text;
                text.font = GetFont(binding.FontRole);
                text.fontSize = binding.FontSize;
                text.fontStyle = binding.FontStyle;
            }
        }

        private void ValidateTextBindings()
        {
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

                if (!IsTextOwnedByThisSkin(text))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} references TMP_Text '{text.name}' that is not owned by this skin.");

                if (!boundTexts.Add(text))
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' has duplicate TMP_Text '{text.name}' at binding index {index}.");

                if (float.IsNaN(binding.FontSize) || float.IsInfinity(binding.FontSize) || binding.FontSize <= 0f)
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} for TMP_Text '{text.name}' has an invalid font size '{binding.FontSize}'.");

                try
                {
                    GetFont(binding.FontRole);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} for TMP_Text '{text.name}' has an unsupported font role.",
                        exception);
                }
                catch (InvalidOperationException exception)
                {
                    throw new InvalidOperationException(
                        $"UI_Skin '{name}' binding at index {index} for TMP_Text '{text.name}' cannot resolve its font.",
                        exception);
                }
            }
        }

        private bool IsTextOwnedByThisSkin(TMP_Text text)
        {
            if (text == null || (text.transform != transform && !text.transform.IsChildOf(transform)))
                return false;

            var current = text.transform;
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
