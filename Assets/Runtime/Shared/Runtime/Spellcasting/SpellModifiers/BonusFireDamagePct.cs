using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared
{
    /// <summary>Increases fire spell damage by a percentage.</summary>
    [Serializable]
    public class BonusFireDamagePct : IScalableModifier
    {
        [SerializeField, FormerlySerializedAs("bonusDamagePct")] private float _bonusDamagePCT;

        /// <summary>Gets the percentage fire damage bonus.</summary>
        public float BonusDamagePct => _bonusDamagePCT;

        /// <summary>Gets the application priority.</summary>
        public int Priority => 50;

        [SerializeField, FormerlySerializedAs("source")] private string _source;

        /// <summary>Gets the modifier source.</summary>
        public string Source => _source;

        /// <summary>Initializes an empty modifier.</summary>
        public BonusFireDamagePct() { }
        /// <summary>Initializes a percentage fire damage modifier.</summary>
        public BonusFireDamagePct(string source, float bonusDamagePct)
        {
            _source = source;
            _bonusDamagePCT = bonusDamagePct;
        }

        /// <summary>Returns whether the modifier applies to a spell.</summary>
        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.InternalTags.Any(t => t.Equals("fire_damage", StringComparison.OrdinalIgnoreCase));

        /// <summary>Applies the percentage bonus to fire damage effects.</summary>
        public void Apply(SpellOverride spell, Actor actor)
        {
            foreach (var component in spell.Components)
            {
                if (component.EffectDefinition is SpellEffect_Damage fireEffect)
                    fireEffect.BaseDamage *= 1 + (_bonusDamagePCT / 100f);
            }
        }

        /// <summary>Scales the bonus by rank.</summary>
        public void Scale(int rank)
        {
            _bonusDamagePCT *= rank;
        }

        /// <summary>Combines another percentage bonus.</summary>
        public void Combine(ISpellModifier other)
        {
            if (other is BonusFireDamagePct otherBonus)
            {
                _bonusDamagePCT += otherBonus._bonusDamagePCT;
            }
        }
    }
}
