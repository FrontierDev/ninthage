using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared
{
    [Serializable]
    public class FireDamageBonus : IScalableModifier
    {
        [SerializeField] private float bonusDamage;

        public float BonusDamage => bonusDamage;

        public int Priority => 1;
        public string source;
        public string Source => source;

        public FireDamageBonus() { }
        public FireDamageBonus(string source, float bonusDamage)
        {
            this.source = source;
            this.bonusDamage = bonusDamage;
        }

        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.InternalTags.Any(t => t.Equals("fire_damage", StringComparison.OrdinalIgnoreCase));

        public void Apply(SpellOverride spell, Actor actor)
        {
            foreach (var component in spell.Components)
            {
                if (component.EffectDefinition is SpellEffect_Damage fireEffect)
                    fireEffect.BaseDamage += bonusDamage;
            }
        }

        public void Scale(int rank)
        {
            bonusDamage *= rank;
        }

        public void Combine(ISpellModifier other)
        {
            if (other is FireDamageBonus otherBonus)
            {
                bonusDamage += otherBonus.bonusDamage;
            }
        }
    }
}