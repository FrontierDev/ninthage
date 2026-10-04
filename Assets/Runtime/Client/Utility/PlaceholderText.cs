using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Shared.Data;
using Game.Client;
using Game.Shared;

namespace Game.Client.Utility
{
    /// <summary>
    /// Resolves $Category.Id$ placeholders in text strings.
    /// Register custom handlers per category using RegisterResolver.
    /// The "stat" category is built-in; set LocalContainer to resolve live values.
    /// </summary>
    public static class PlaceholderText
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\$[^$]+\$");
        private static readonly Dictionary<string, Func<string, string>> resolvers = new();

        /// <summary>
        /// The local player's stat container. Set this when the local player spawns
        /// so that $stat.X$ placeholders resolve to actual values.
        /// </summary>
        public static ActorStatContainer LocalContainer { get; set; }

        /// <summary>
        /// Registers a resolver for a given category (case-insensitive).
        /// The resolver receives the id portion and returns the resolved string.
        /// </summary>
        public static void RegisterResolver(string category, Func<string, string> resolver)
        {
            resolvers[category.ToLower()] = resolver;
        }

        public static string ResolvePlaceholder(string placeholder)
        {
            if (placeholder.Length < 3 || placeholder[0] != '$' || placeholder[^1] != '$')
                return placeholder;

            var key = placeholder.Substring(1, placeholder.Length - 2);
            var parts = key.Split('.', 2);

            if (parts.Length != 2)
                return placeholder;

            var category = parts[0].ToLower();
            var id = parts[1];

            if (category == "stat")
                return ResolveStat(id);

            if (category == "mit")
                return ResolveMitigation(id);

            if (resolvers.TryGetValue(category, out var resolver))
                return resolver(id);

            return placeholder; // No resolver registered for this category
        }

        private static string ResolveStat(string id)
        {
            var def = ActorStatDefinitionLibrary.Instance.GetDefinition(id);
            if (def == null) return $"$PH ERROR: {id}$";

            if (LocalContainer == null)
                return def.DisplayName;

            // Chance stats are not stored in the container — compute from the source rating stat
            if (def.Tags != null && def.Tags.Contains("chance") &&
                def.BaseValueMode == ActorStatBaseValueMode.Rating &&
                def.SourceStat != null)
            {
                if (!LocalContainer.TryGetStat(def.SourceStat.DefinitionId, out var sourceStat))
                    return def.DisplayName;

                int level = ClientAccountManager.PlayerActor.GetLevel();
                float percent = CombatRatingHelper.RatingToPercent(sourceStat.EffectiveMaximum, level, def.Multiplier);
                return $"{percent.ToString("F1")}%";
            }

            if (!LocalContainer.TryGetStat(id, out var stat))
                return def.DisplayName;

            return stat.EffectiveMaximum.ToString("F1");
        }

        private static string ResolveMitigation(string id)
        {
            var school = DamageSchoolDefinitionLibrary.Instance.GetDefinition(id);
            if (school == null) return $"$PH ERROR: {id}$";

            if (LocalContainer == null)
                return school.DisplayName;

            int level = ClientAccountManager.PlayerActor.GetLevel();
            if (school.MitigationRatingStat == null) return "0.0%";
            if (!LocalContainer.TryGetStat(school.MitigationRatingStat.DefinitionId, out var ratingStat))
                return "0.0%";

            float mitigation = CombatRatingHelper.RatingToPercent(ratingStat.EffectiveMaximum, level, school.RatingPerPercent);
            return $"{mitigation.ToString("F1")}%";
        }

        public static string ResolveAll(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return PlaceholderRegex.Replace(text, match => ResolvePlaceholder(match.Value));
        }
    }
}