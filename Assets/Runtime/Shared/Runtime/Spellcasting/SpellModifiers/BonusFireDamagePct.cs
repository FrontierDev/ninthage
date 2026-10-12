using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Increases fire spell damage by a percentage.</summary>
    [Serializable]
    public class BonusFireDamagePct : IScalableModifier
    {
        [SerializeField] private float bonusDamagePct;

        /// <summary>Gets the percentage fire damage bonus.</summary>
        public float BonusDamagePct => bonusDamagePct;

        /// <summary>Gets the application priority.</summary>
        public int Priority => 50;

        [SerializeField] private string source;

        /// <summary>Gets the modifier source.</summary>
        public string Source => source;

        /// <summary>Initializes an empty modifier.</summary>
        public BonusFireDamagePct() { }
        /// <summary>Initializes a percentage fire damage modifier.</summary>
        public BonusFireDamagePct(string source, float bonusDamagePct)
        {
            this.source = source;
            this.bonusDamagePct = bonusDamagePct;
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
                    fireEffect.BaseDamage *= 1 + (bonusDamagePct / 100f);
            }
        }

        /// <summary>Scales the bonus by rank.</summary>
        public void Scale(int rank)
        {
            bonusDamagePct *= rank;
        }

        /// <summary>Combines another percentage bonus.</summary>
        public void Combine(ISpellModifier other)
        {
            if (other is BonusFireDamagePct otherBonus)
            {
                bonusDamagePct += otherBonus.bonusDamagePct;
            }
        }
    }
}
