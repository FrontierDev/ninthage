namespace Game.Shared
{
    /// <summary>Stores runtime multipliers and bonuses for a spell component.</summary>
    public sealed class SpellComponentOverrides
    {
        /// <summary>Gets or sets the damage multiplier.</summary>
        public float DamageMultiplier = 1f;
        /// <summary>Gets or sets the healing multiplier.</summary>
        public float HealingMultiplier = 1f;
        /// <summary>Gets or sets the flat damage bonus.</summary>
        public float FlatDamageBonus = 0f;
        /// <summary>Gets or sets the flat healing bonus.</summary>
        public float FlatHealingBonus = 0f;
    }
}
