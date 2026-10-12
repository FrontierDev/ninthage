using Game.Shared.Runtime.Actors;
namespace Game.Shared.Runtime.Spellcasting
{
    /// <summary>Defines a modifier that can scale by talent rank.</summary>
    public interface IScalableModifier : ISpellModifier
    {
        /// <summary>Scales the modifier to a rank.</summary>
        void Scale(int rank);
    }

    /// <summary>Defines a modifier applied to a runtime spell override.</summary>
    public interface ISpellModifier
    {
        /// <summary>Gets the modifier application priority.</summary>
        int Priority { get; }
        /// <summary>Gets the source identifier.</summary>
        string Source { get; }
        /// <summary>Returns whether the modifier applies to a spell.</summary>
        bool AppliesTo(SpellOverride spell);
        /// <summary>Applies the modifier to a spell for an actor.</summary>
        void Apply(SpellOverride spell, Actor actor);
        /// <summary>Combines another modifier into this modifier.</summary>
        void Combine(ISpellModifier other);
    }
}
