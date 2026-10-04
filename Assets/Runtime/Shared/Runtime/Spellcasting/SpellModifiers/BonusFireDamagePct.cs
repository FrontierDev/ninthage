using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared
{
    [Serializable]
    public class BonusFireDamagePct : IScalableModifier
    {
        [SerializeField] private float bonusDamagePct;

        public float BonusDamagePct => bonusDamagePct;

        public int Priority => 50;

        [SerializeField] private string source;

        public string Source => source;

        public BonusFireDamagePct() { }
        public BonusFireDamagePct(string source, float bonusDamagePct)
        {
            this.source = source;
            this.bonusDamagePct = bonusDamagePct;
        }

        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.InternalTags.Any(t => t.Equals("fire_damage", StringComparison.OrdinalIgnoreCase));

        public void Apply(SpellOverride spell, Actor actor)
        {
            foreach (var component in spell.Components)
            {
                if (component.EffectDefinition is SpellEffect_Damage fireEffect)
                    fireEffect.BaseDamage *= 1 + (bonusDamagePct / 100f);
            }
        }

        public void Scale(int rank)
        {
            bonusDamagePct *= rank;
        }

        public void Combine(ISpellModifier other)
        {
            if (other is BonusFireDamagePct otherBonus)
            {
                bonusDamagePct += otherBonus.bonusDamagePct;
            }
        }
    }
}