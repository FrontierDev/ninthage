namespace Game.Shared
{
    public interface IScalableModifier : ISpellModifier
    {
        void Scale(int rank);
    }

    public interface ISpellModifier
    {
        int Priority { get; }
        string Source { get; }
        bool AppliesTo(SpellOverride spell);
        void Apply(SpellOverride spell, Actor actor);
        void Combine(ISpellModifier other);
    }
}