using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using Game.Shared.Data.SpellEffectDefinitions;
namespace Game.Shared.Runtime.Spellcasting.SpellModifiers
{
    /// <summary>Increases fire spell damage by a percentage.</summary>
    [Serializable]
    [MovedFrom(true, "Game.Shared", "Game.Shared")]
    public class BonusFireDamagePCT : IScalableModifier
    {
        [SerializeField, FormerlySerializedAs("bonusDamagePct")] private float _bonusDamagePCT;

        /// <summary>Gets the percentage fire damage bonus.</summary>
        public float BonusDamagePCT => _bonusDamagePCT;

        /// <summary>Gets the application priority.</summary>
        public int Priority => 50;

        [SerializeField, FormerlySerializedAs("source")] private string _source;

        /// <summary>Gets the modifier source.</summary>
        public string Source => _source;

        /// <summary>Initializes an empty modifier.</summary>
        public BonusFireDamagePCT() { }
        /// <summary>Initializes a percentage fire damage modifier.</summary>
        public BonusFireDamagePCT(string source, float bonusDamagePCT)
        {
            _source = source;
            _bonusDamagePCT = bonusDamagePCT;
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
            if (other is BonusFireDamagePCT otherBonus)
            {
                _bonusDamagePCT += otherBonus._bonusDamagePCT;
            }
        }
    }
}
