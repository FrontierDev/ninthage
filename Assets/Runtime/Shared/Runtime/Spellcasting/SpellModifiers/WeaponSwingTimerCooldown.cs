using System;
using System.Linq;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    /// <summary>Sets auto-attack cooldown from the actor's weapon swing timer.</summary>
    [Serializable]
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
