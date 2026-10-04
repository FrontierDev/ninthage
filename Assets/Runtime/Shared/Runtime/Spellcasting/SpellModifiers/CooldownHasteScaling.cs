using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    [Serializable]
    public class CooldownHasteScaling : ISpellModifier
    {
        public int Priority => 100;
        public string Source => "CooldownHasteScaling";

        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.CooldownScalesWithHaste;

        public void Apply(SpellOverride spell, Actor actor)
        {
            var hastePct = 0f;
            if (Game.Shared.Runtime.IsServer())
            {
                if (actor.GetComponent<ActorStatContainer>().TryGetStat("haste_pct", out var stat))
                    hastePct = stat.EffectiveMaximum / 100f;
            }
            else
            {
                var hasteRating = actor.GetComponent<ActorStatContainer>().GetStat("haste");

                if (hasteRating == null) return;

                var level = actor.GetLevel();
                var scaling = ActorStatDefinitionLibrary.Instance.GetDefinition("haste").Multiplier;
                hastePct = CombatRatingHelper.RatingToPercent(hasteRating.EffectiveMaximum, level, scaling) / 100;
            }

            spell.Cooldown *= (1 - hastePct);
        }

        public void Combine(ISpellModifier other) { }
    }
}