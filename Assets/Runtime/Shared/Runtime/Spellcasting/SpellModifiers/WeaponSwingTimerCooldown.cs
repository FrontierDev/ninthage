using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
namespace Game.Shared.Runtime.Spellcasting.SpellModifiers
{
    /// <summary>Sets auto-attack cooldown from the actor's weapon swing timer.</summary>
    [Serializable]
    [MovedFrom(true, "Game.Shared", "Game.Shared")]
    public class WeaponSwingTimerCooldown : ISpellModifier
    {
        public int Priority => 0; // runs first — sets the base cooldown
        public string Source => "WeaponSwingTimerCooldown";

        public bool AppliesTo(SpellOverride spell)
            => spell.BaseDefinition.Tags.Any(t => t.Equals("autoattack", StringComparison.OrdinalIgnoreCase));

        public void Apply(SpellOverride spell, Actor actor)
        {
            spell.Cooldown = actor.GetWeaponSwingTimer(ItemSlot.MainHand);
        }

        public void Combine(ISpellModifier other) { }
    }
}
