using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared
{
    /// <summary>Adds flat fire damage to fire spell effects.</summary>
    [Serializable]
    public class FireDamageBonus : IScalableModifier
    {
        [SerializeField, FormerlySerializedAs("bonusDamage")] private float _bonusDamage;

        public float BonusDamage => _bonusDamage;

        public int Priority => 1;
        public string source;
        public string Source => source;

        public FireDamageBonus() { }
        public FireDamageBonus(string source, float bonusDamage)
        {
            this.source = source;
            _bonusDamage = bonusDamage;
        }

        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.InternalTags.Any(t => t.Equals("fire_damage", StringComparison.OrdinalIgnoreCase));

        public void Apply(SpellOverride spell, Actor actor)
        {
            foreach (var component in spell.Components)
            {
                if (component.EffectDefinition is SpellEffect_Damage fireEffect)
                    fireEffect.BaseDamage += _bonusDamage;
            }
        }

        public void Scale(int rank)
        {
            _bonusDamage *= rank;
        }

        public void Combine(ISpellModifier other)
        {
            if (other is FireDamageBonus otherBonus)
            {
                _bonusDamage += otherBonus._bonusDamage;
            }
        }
    }
}
