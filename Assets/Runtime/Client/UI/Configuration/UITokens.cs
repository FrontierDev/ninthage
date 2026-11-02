namespace Game.Client.UI.Configuration
{
    /// <summary>
    /// Contains the semantic keys used by the client UI design-token configuration.
    /// </summary>
    public static class UITokens
    {
        /// <summary>
        /// Identifies a font-family role.
        /// </summary>
        public enum FontRole
        {
            /// <summary>
            /// Identifies the heading font family.
            /// </summary>
            Heading,

            /// <summary>
            /// Identifies the body font family.
            /// </summary>
            Body,

            /// <summary>
            /// Identifies the numeric font family.
            /// </summary>
            Numeric
        }

        /// <summary>
        /// Identifies a resolved text-style token.
        /// </summary>
        public enum TextStyle
        {
            /// <summary>
            /// Identifies the window title text style.
            /// </summary>
            WindowTitle,

            /// <summary>
            /// Identifies the section heading text style.
            /// </summary>
            SectionHeading,

            /// <summary>
            /// Identifies the unit name text style.
            /// </summary>
            UnitName,

            /// <summary>
            /// Identifies the standard body text style.
            /// </summary>
            Body,

            /// <summary>
            /// Identifies the secondary text style.
            /// </summary>
            Secondary,

            /// <summary>
            /// Identifies the small text style.
            /// </summary>
            Small,

            /// <summary>
            /// Identifies the numeric text style.
            /// </summary>
            Numeric,

            /// <summary>
            /// Identifies the keybind text style.
            /// </summary>
            Keybind,

            /// <summary>
            /// Identifies the aura timer text style.
            /// </summary>
            AuraTimer,
            /// <summary>
            /// Identifies a major screen heading text style.
            /// </summary>
            MajorScreenHeading,
            /// <summary>
            /// Identifies an important numeric value text style.
            /// </summary>
            ImportantValue,
            /// <summary>
            /// Identifies a small numeric text style.
            /// </summary>
            SmallNumeric,
            /// <summary>
            /// Identifies normal floating combat text.
            /// </summary>
            FloatingCombatText,
            /// <summary>
            /// Identifies critical floating combat text.
            /// </summary>
            FloatingCombatCritical,
            /// <summary>
            /// Identifies the tab label text style.
            /// </summary>
            TabLabel,
        }

        /// <summary>
        /// Identifies a scalar layout metric token.
        /// </summary>
        public enum Metric
        {
            /// <summary>Identifies the small spacing metric.</summary>
            SpacingSmall,
            /// <summary>Identifies the medium spacing metric.</summary>
            SpacingMedium,
            /// <summary>Identifies the large spacing metric.</summary>
            SpacingLarge,
            /// <summary>Identifies the window padding metric.</summary>
            WindowPadding,
            /// <summary>Identifies the standard row height metric.</summary>
            StandardRowHeight,
            /// <summary>Identifies the window title bar height metric.</summary>
            WindowTitleBarHeight,
            /// <summary>Identifies the ability icon size metric.</summary>
            AbilityIconSize,
            /// <summary>Identifies the aura icon size metric.</summary>
            AuraIconSize,
            /// <summary>Identifies the inventory icon size metric.</summary>
            InventoryIconSize,
            /// <summary>Identifies the talent icon size metric.</summary>
            TalentIconSize,
            /// <summary>Identifies the utility icon size metric.</summary>
            UtilityIconSize,
            /// <summary>Identifies the unit portrait size metric.</summary>
            UnitPortraitSize,
            /// <summary>Identifies the unit-frame width metric.</summary>
            UnitFrameWidth,
            /// <summary>Identifies the unit-frame height metric.</summary>
            UnitFrameHeight,
            /// <summary>Identifies the health-bar height metric.</summary>
            HealthBarHeight,
            /// <summary>Identifies the resource-bar height metric.</summary>
            ResourceBarHeight,
            /// <summary>Identifies the cast-bar height metric.</summary>
            CastBarHeight,
            /// <summary>Identifies the border width metric.</summary>
            BorderWidth,
            /// <summary>Identifies the corner radius metric.</summary>
            CornerRadius,
            /// <summary>Identifies the experience-bar height metric.</summary>
            ExperienceBarHeight,
            /// <summary>Identifies the extra-large spacing metric.</summary>
            SpacingExtraLarge,
            /// <summary>Identifies the 20-pixel spacing metric.</summary>
            Spacing20,
            /// <summary>Identifies the 24-pixel spacing metric.</summary>
            Spacing24,
            /// <summary>Identifies the 32-pixel spacing metric.</summary>
            Spacing32,
            /// <summary>Identifies the compact panel padding metric.</summary>
            CompactPanelPadding,
            /// <summary>Identifies the compact button height metric.</summary>
            CompactButtonHeight,
            /// <summary>Identifies the standard button height metric.</summary>
            StandardButtonHeight,
            /// <summary>Identifies the large button height metric.</summary>
            LargeButtonHeight,
            /// <summary>Identifies the compact row height metric.</summary>
            CompactRowHeight,
            /// <summary>Identifies the large row height metric.</summary>
            LargeRowHeight,
            /// <summary>Identifies the input-field height metric.</summary>
            InputFieldHeight,
            /// <summary>Identifies the toggle target size metric.</summary>
            ToggleTargetSize,
            /// <summary>Identifies the small auxiliary icon size metric.</summary>
            SmallAuxiliaryIconSize,
            /// <summary>Identifies the small gameplay icon size metric.</summary>
            SmallGameplayIconSize,
            /// <summary>Identifies the important aura icon size metric.</summary>
            ImportantAuraIconSize,
            /// <summary>Identifies the equipment icon size metric.</summary>
            EquipmentIconSize,
            /// <summary>Identifies the nameplate width metric.</summary>
            NameplateWidth,
            /// <summary>Identifies the nameplate health-bar height metric.</summary>
            NameplateHealthBarHeight,
            /// <summary>Identifies the nameplate cast-bar height metric.</summary>
            NameplateCastBarHeight,
            /// <summary>Identifies the nameplate level-icon size metric.</summary>
            NameplateLevelIconSize,
            /// <summary>Identifies the inner-border width metric.</summary>
            InnerBorderWidth,
            /// <summary>Identifies the divider width metric.</summary>
            DividerWidth
        }

        /// <summary>
        /// Identifies a Radiant Slate palette color token.
        /// </summary>
        public enum Color
        {
            /// <summary>Identifies the background color.</summary>
            Background,
            /// <summary>Identifies the secondary background color.</summary>
            SecondaryBackground,
            /// <summary>Identifies the panel color.</summary>
            Panel,
            /// <summary>Identifies the elevated panel color.</summary>
            ElevatedPanel,
            /// <summary>Identifies the raised surface color.</summary>
            RaisedSurface,
            /// <summary>Identifies the border color.</summary>
            Border,
            /// <summary>Identifies the secondary border color.</summary>
            SecondaryBorder,
            /// <summary>Identifies the primary text color.</summary>
            PrimaryText,
            /// <summary>Identifies the secondary text color.</summary>
            SecondaryText,
            /// <summary>Identifies the muted text color.</summary>
            MutedText,
            /// <summary>Identifies the accent gold color.</summary>
            AccentGold,
            /// <summary>Identifies the success color.</summary>
            Success,
            /// <summary>Identifies the warning color.</summary>
            Warning,
            /// <summary>Identifies the information color.</summary>
            Information,
            /// <summary>Identifies the danger color.</summary>
            Danger
        }
    }
}
