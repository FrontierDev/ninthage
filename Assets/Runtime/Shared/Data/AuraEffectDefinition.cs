using System.Collections.Generic;

namespace Game.Shared.Data
{
    public interface IAutoDescription
    {
        string Description { get; }
    }

    public enum AuraActorEvent
    {
        onAuraApply
    }

    public abstract class AuraEffectDefinition : IAutoDescription
    {
        public abstract void Execute(AuraContext ctx, Actor target);
        public abstract string Description { get; }
    }
}