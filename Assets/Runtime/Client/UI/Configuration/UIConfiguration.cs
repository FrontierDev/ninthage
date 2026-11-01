using System;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.Configuration
{
    /// <summary>
    /// Owns the client UI's reusable typography, metric and palette design tokens.
    /// </summary>
    [CreateAssetMenu(
        menuName = "NinthAge/UI/UI Configuration",
        fileName = "UIConfiguration")]
    public sealed class UIConfiguration : ScriptableObject
    {
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset _headingFont;
        [SerializeField] private TMP_FontAsset _bodyFont;
        [SerializeField] private TMP_FontAsset _numericFont;

        [Header("Text Styles")]
        [SerializeField] private TextStyleDefinition _windowTitle = new(UITokens.FontRole.Heading, 18f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _sectionHeading = new(UITokens.FontRole.Heading, 16f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _unitName = new(UITokens.FontRole.Heading, 14f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _body = new(UITokens.FontRole.Body, 14f, FontStyles.Normal, FontWeight.Regular);
        [SerializeField] private TextStyleDefinition _secondary = new(UITokens.FontRole.Body, 12f, FontStyles.Normal, FontWeight.Regular);
        [SerializeField] private TextStyleDefinition _small = new(UITokens.FontRole.Body, 11f, FontStyles.Normal, FontWeight.Regular);
        [SerializeField] private TextStyleDefinition _numeric = new(UITokens.FontRole.Numeric, 14f, FontStyles.Normal, FontWeight.Regular);
        [SerializeField] private TextStyleDefinition _keybind = new(UITokens.FontRole.Body, 11f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _auraTimer = new(UITokens.FontRole.Numeric, 11f, FontStyles.Normal, FontWeight.Medium);
        [SerializeField] private TextStyleDefinition _majorScreenHeading = new(UITokens.FontRole.Heading, 24f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _importantValue = new(UITokens.FontRole.Numeric, 14f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _smallNumeric = new(UITokens.FontRole.Numeric, 11f, FontStyles.Normal, FontWeight.Medium);
        [SerializeField] private TextStyleDefinition _floatingCombatText = new(UITokens.FontRole.Body, 18f, FontStyles.Normal, FontWeight.SemiBold);
        [SerializeField] private TextStyleDefinition _floatingCombatCritical = new(UITokens.FontRole.Body, 24f, FontStyles.Normal, FontWeight.Bold);

        [Header("Metrics")]
        [SerializeField, Min(0.01f)] private float _spacingSmall = 4f;
        [SerializeField, Min(0.01f)] private float _spacingMedium = 8f;
        [SerializeField, Min(0.01f)] private float _spacingLarge = 12f;
        [SerializeField, Min(0.01f)] private float _windowPadding = 16f;
        [SerializeField, Min(0.01f)] private float _standardRowHeight = 32f;
        [SerializeField, Min(0.01f)] private float _windowTitleBarHeight = 40f;
        [SerializeField, Min(0.01f)] private float _abilityIconSize = 40f;
        [SerializeField, Min(0.01f)] private float _auraIconSize = 28f;
        [SerializeField, Min(0.01f)] private float _inventoryIconSize = 44f;
        [SerializeField, Min(0.01f)] private float _talentIconSize = 40f;
        [SerializeField, Min(0.01f)] private float _utilityIconSize = 24f;
        [SerializeField, Min(0.01f)] private float _unitPortraitSize = 50f;
        [SerializeField, Min(0.01f)] private float _unitFrameWidth = 420f;
        [SerializeField, Min(0.01f)] private float _unitFrameHeight = 70f;
        [SerializeField, Min(0.01f)] private float _healthBarHeight = 26f;
        [SerializeField, Min(0.01f)] private float _resourceBarHeight = 22f;
        [SerializeField, Min(0.01f)] private float _castBarHeight = 22f;
        [SerializeField, Min(0.01f)] private float _experienceBarHeight = 12f;
        [SerializeField, Min(0.01f)] private float _borderWidth = 2f;
        [SerializeField, Min(0.01f)] private float _cornerRadius = 4f;
        [SerializeField, Min(0.01f)] private float _spacingExtraLarge = 16f;
        [SerializeField, Min(0.01f)] private float _spacing20 = 20f;
        [SerializeField, Min(0.01f)] private float _spacing24 = 24f;
        [SerializeField, Min(0.01f)] private float _spacing32 = 32f;
        [SerializeField, Min(0.01f)] private float _compactPanelPadding = 12f;
        [SerializeField, Min(0.01f)] private float _compactButtonHeight = 28f;
        [SerializeField, Min(0.01f)] private float _standardButtonHeight = 36f;
        [SerializeField, Min(0.01f)] private float _largeButtonHeight = 40f;
        [SerializeField, Min(0.01f)] private float _compactRowHeight = 28f;
        [SerializeField, Min(0.01f)] private float _largeRowHeight = 40f;
        [SerializeField, Min(0.01f)] private float _inputFieldHeight = 36f;
        [SerializeField, Min(0.01f)] private float _toggleTargetSize = 24f;
        [SerializeField, Min(0.01f)] private float _smallAuxiliaryIconSize = 16f;
        [SerializeField, Min(0.01f)] private float _smallGameplayIconSize = 20f;
        [SerializeField, Min(0.01f)] private float _importantAuraIconSize = 32f;
        [SerializeField, Min(0.01f)] private float _equipmentIconSize = 48f;
        [SerializeField, Min(0.01f)] private float _nameplateWidth = 220f;
        [SerializeField, Min(0.01f)] private float _nameplateHealthBarHeight = 16f;
        [SerializeField, Min(0.01f)] private float _nameplateCastBarHeight = 12f;
        [SerializeField, Min(0.01f)] private float _nameplateLevelIconSize = 16f;
        [SerializeField, Min(0.01f)] private float _innerBorderWidth = 1f;
        [SerializeField, Min(0.01f)] private float _dividerWidth = 1f;

        [Header("Radiant Slate Colors")]
        [SerializeField] private UnityEngine.Color _background = new(30f / 255f, 34f / 255f, 42f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _secondaryBackground = new(35f / 255f, 42f / 255f, 51f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _panel = new(42f / 255f, 48f / 255f, 58f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _elevatedPanel = new(53f / 255f, 62f / 255f, 74f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _raisedSurface = new(65f / 255f, 75f / 255f, 89f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _border = new(75f / 255f, 85f / 255f, 102f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _secondaryBorder = new(90f / 255f, 102f / 255f, 121f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _primaryText = new(230f / 255f, 234f / 255f, 242f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _secondaryText = new(182f / 255f, 190f / 255f, 204f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _mutedText = new(139f / 255f, 148f / 255f, 165f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _accentGold = new(224f / 255f, 191f / 255f, 114f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _success = new(56f / 255f, 185f / 255f, 134f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _warning = new(232f / 255f, 138f / 255f, 82f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _information = new(86f / 255f, 168f / 255f, 239f / 255f, 1f);
        [SerializeField] private UnityEngine.Color _danger = new(201f / 255f, 106f / 255f, 99f / 255f, 1f);

        /// <summary>
        /// Resolves the configured font for a semantic font role.
        /// </summary>
        /// <param name="role">The font role to resolve.</param>
        /// <returns>The configured font asset for the role.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="role"/> is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the selected role has no configured font.</exception>
        public TMP_FontAsset GetFont(UITokens.FontRole role)
        {
            TMP_FontAsset font;
            switch (role)
            {
                case UITokens.FontRole.Heading:
                    font = _headingFont;
                    break;
                case UITokens.FontRole.Body:
                    font = _bodyFont;
                    break;
                case UITokens.FontRole.Numeric:
                    font = _numericFont;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported UI font role.");
            }

            if (font == null)
                throw new InvalidOperationException($"UIConfiguration '{name}' has no font configured for role '{role}'.");

            return font;
        }

        /// <summary>
        /// Resolves a semantic text-style token through the configured font roles.
        /// </summary>
        /// <param name="token">The text-style token to resolve.</param>
        /// <returns>The resolved font, size and style.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="token"/> is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the definition or font size is invalid.</exception>
        public UIResolvedTextStyle GetTextStyle(UITokens.TextStyle token)
        {
            TextStyleDefinition definition;
            switch (token)
            {
                case UITokens.TextStyle.WindowTitle:
                    definition = _windowTitle;
                    break;
                case UITokens.TextStyle.SectionHeading:
                    definition = _sectionHeading;
                    break;
                case UITokens.TextStyle.UnitName:
                    definition = _unitName;
                    break;
                case UITokens.TextStyle.Body:
                    definition = _body;
                    break;
                case UITokens.TextStyle.Secondary:
                    definition = _secondary;
                    break;
                case UITokens.TextStyle.Small:
                    definition = _small;
                    break;
                case UITokens.TextStyle.Numeric:
                    definition = _numeric;
                    break;
                case UITokens.TextStyle.Keybind:
                    definition = _keybind;
                    break;
                case UITokens.TextStyle.AuraTimer:
                    definition = _auraTimer;
                    break;
                case UITokens.TextStyle.MajorScreenHeading:
                    definition = _majorScreenHeading;
                    break;
                case UITokens.TextStyle.ImportantValue:
                    definition = _importantValue;
                    break;
                case UITokens.TextStyle.SmallNumeric:
                    definition = _smallNumeric;
                    break;
                case UITokens.TextStyle.FloatingCombatText:
                    definition = _floatingCombatText;
                    break;
                case UITokens.TextStyle.FloatingCombatCritical:
                    definition = _floatingCombatCritical;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, "Unsupported UI text-style token.");
            }

            if (definition == null)
                throw new InvalidOperationException($"UIConfiguration '{name}' has no definition for text-style token '{token}'.");
            if (!IsFinitePositive(definition.FontSize))
                throw new InvalidOperationException(
                    $"UIConfiguration '{name}' text-style token '{token}' has an invalid font size '{definition.FontSize}'.");

            var font = GetFont(definition.FontRole);
            return new UIResolvedTextStyle(font, definition.FontSize, definition.FontStyle, definition.FontWeight);
        }

        /// <summary>
        /// Resolves a scalar metric token without normalization.
        /// </summary>
        /// <param name="token">The metric token to resolve.</param>
        /// <returns>The authored metric value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="token"/> is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the authored value is not finite and positive.</exception>
        public float GetMetric(UITokens.Metric token)
        {
            float value;
            switch (token)
            {
                case UITokens.Metric.SpacingSmall:
                    value = _spacingSmall;
                    break;
                case UITokens.Metric.SpacingMedium:
                    value = _spacingMedium;
                    break;
                case UITokens.Metric.SpacingLarge:
                    value = _spacingLarge;
                    break;
                case UITokens.Metric.WindowPadding:
                    value = _windowPadding;
                    break;
                case UITokens.Metric.StandardRowHeight:
                    value = _standardRowHeight;
                    break;
                case UITokens.Metric.WindowTitleBarHeight:
                    value = _windowTitleBarHeight;
                    break;
                case UITokens.Metric.AbilityIconSize:
                    value = _abilityIconSize;
                    break;
                case UITokens.Metric.AuraIconSize:
                    value = _auraIconSize;
                    break;
                case UITokens.Metric.InventoryIconSize:
                    value = _inventoryIconSize;
                    break;
                case UITokens.Metric.TalentIconSize:
                    value = _talentIconSize;
                    break;
                case UITokens.Metric.UtilityIconSize:
                    value = _utilityIconSize;
                    break;
                case UITokens.Metric.UnitPortraitSize:
                    value = _unitPortraitSize;
                    break;
                case UITokens.Metric.UnitFrameWidth:
                    value = _unitFrameWidth;
                    break;
                case UITokens.Metric.UnitFrameHeight:
                    value = _unitFrameHeight;
                    break;
                case UITokens.Metric.HealthBarHeight:
                    value = _healthBarHeight;
                    break;
                case UITokens.Metric.ResourceBarHeight:
                    value = _resourceBarHeight;
                    break;
                case UITokens.Metric.CastBarHeight:
                    value = _castBarHeight;
                    break;
                case UITokens.Metric.BorderWidth:
                    value = _borderWidth;
                    break;
                case UITokens.Metric.CornerRadius:
                    value = _cornerRadius;
                    break;
                case UITokens.Metric.ExperienceBarHeight:
                    value = _experienceBarHeight;
                    break;
                case UITokens.Metric.SpacingExtraLarge:
                    value = _spacingExtraLarge;
                    break;
                case UITokens.Metric.Spacing20:
                    value = _spacing20;
                    break;
                case UITokens.Metric.Spacing24:
                    value = _spacing24;
                    break;
                case UITokens.Metric.Spacing32:
                    value = _spacing32;
                    break;
                case UITokens.Metric.CompactPanelPadding:
                    value = _compactPanelPadding;
                    break;
                case UITokens.Metric.CompactButtonHeight:
                    value = _compactButtonHeight;
                    break;
                case UITokens.Metric.StandardButtonHeight:
                    value = _standardButtonHeight;
                    break;
                case UITokens.Metric.LargeButtonHeight:
                    value = _largeButtonHeight;
                    break;
                case UITokens.Metric.CompactRowHeight:
                    value = _compactRowHeight;
                    break;
                case UITokens.Metric.LargeRowHeight:
                    value = _largeRowHeight;
                    break;
                case UITokens.Metric.InputFieldHeight:
                    value = _inputFieldHeight;
                    break;
                case UITokens.Metric.ToggleTargetSize:
                    value = _toggleTargetSize;
                    break;
                case UITokens.Metric.SmallAuxiliaryIconSize:
                    value = _smallAuxiliaryIconSize;
                    break;
                case UITokens.Metric.SmallGameplayIconSize:
                    value = _smallGameplayIconSize;
                    break;
                case UITokens.Metric.ImportantAuraIconSize:
                    value = _importantAuraIconSize;
                    break;
                case UITokens.Metric.EquipmentIconSize:
                    value = _equipmentIconSize;
                    break;
                case UITokens.Metric.NameplateWidth:
                    value = _nameplateWidth;
                    break;
                case UITokens.Metric.NameplateHealthBarHeight:
                    value = _nameplateHealthBarHeight;
                    break;
                case UITokens.Metric.NameplateCastBarHeight:
                    value = _nameplateCastBarHeight;
                    break;
                case UITokens.Metric.NameplateLevelIconSize:
                    value = _nameplateLevelIconSize;
                    break;
                case UITokens.Metric.InnerBorderWidth:
                    value = _innerBorderWidth;
                    break;
                case UITokens.Metric.DividerWidth:
                    value = _dividerWidth;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, "Unsupported UI metric token.");
            }

            if (!IsFinitePositive(value))
                throw new InvalidOperationException(
                    $"UIConfiguration '{name}' metric token '{token}' has an invalid value '{value}'.");

            return value;
        }

        /// <summary>
        /// Resolves a Radiant Slate palette color token.
        /// </summary>
        /// <param name="token">The color token to resolve.</param>
        /// <returns>The authored color value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="token"/> is unsupported.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a color component is not finite.</exception>
        public UnityEngine.Color GetColor(UITokens.Color token)
        {
            UnityEngine.Color value;
            switch (token)
            {
                case UITokens.Color.Background:
                    value = _background;
                    break;
                case UITokens.Color.SecondaryBackground:
                    value = _secondaryBackground;
                    break;
                case UITokens.Color.Panel:
                    value = _panel;
                    break;
                case UITokens.Color.ElevatedPanel:
                    value = _elevatedPanel;
                    break;
                case UITokens.Color.RaisedSurface:
                    value = _raisedSurface;
                    break;
                case UITokens.Color.Border:
                    value = _border;
                    break;
                case UITokens.Color.SecondaryBorder:
                    value = _secondaryBorder;
                    break;
                case UITokens.Color.PrimaryText:
                    value = _primaryText;
                    break;
                case UITokens.Color.SecondaryText:
                    value = _secondaryText;
                    break;
                case UITokens.Color.MutedText:
                    value = _mutedText;
                    break;
                case UITokens.Color.AccentGold:
                    value = _accentGold;
                    break;
                case UITokens.Color.Success:
                    value = _success;
                    break;
                case UITokens.Color.Warning:
                    value = _warning;
                    break;
                case UITokens.Color.Information:
                    value = _information;
                    break;
                case UITokens.Color.Danger:
                    value = _danger;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, "Unsupported UI color token.");
            }

            if (!IsFinite(value.r) || !IsFinite(value.g) || !IsFinite(value.b) || !IsFinite(value.a))
                throw new InvalidOperationException(
                    $"UIConfiguration '{name}' color token '{token}' has a non-finite component.");

            return value;
        }

        private static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        [Serializable]
        private sealed class TextStyleDefinition
        {
            [SerializeField] private UITokens.FontRole _fontRole;
            [SerializeField, Min(1f)] private float _fontSize;
            [SerializeField] private FontStyles _fontStyle;
            [SerializeField] private FontWeight _fontWeight = FontWeight.Regular;

            /// <summary>
            /// Creates an empty text-style definition for Unity serialization.
            /// </summary>
            public TextStyleDefinition()
            {
            }

            /// <summary>
            /// Creates a text-style definition with the supplied authored values.
            /// </summary>
            /// <param name="fontRole">The semantic font role.</param>
            /// <param name="fontSize">The authored font size.</param>
            /// <param name="fontStyle">The authored TextMeshPro font style.</param>
            /// <param name="fontWeight">The authored TextMeshPro font weight.</param>
            public TextStyleDefinition(
                UITokens.FontRole fontRole,
                float fontSize,
                FontStyles fontStyle,
                FontWeight fontWeight)
            {
                _fontRole = fontRole;
                _fontSize = fontSize;
                _fontStyle = fontStyle;
                _fontWeight = fontWeight;
            }

            /// <summary>
            /// Gets the semantic font role.
            /// </summary>
            public UITokens.FontRole FontRole => _fontRole;

            /// <summary>
            /// Gets the authored font size.
            /// </summary>
            public float FontSize => _fontSize;

            /// <summary>
            /// Gets the authored TextMeshPro font style.
            /// </summary>
            public FontStyles FontStyle => _fontStyle;

            /// <summary>
            /// Gets the authored TextMeshPro font weight.
            /// </summary>
            public FontWeight FontWeight => _fontWeight;
        }
    }
}
