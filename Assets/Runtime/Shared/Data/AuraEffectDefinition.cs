using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data
{
    /// <summary>Provides a description for an authored effect.</summary>
    public interface IAutoDescription
    {
        /// <summary>Gets the effect description.</summary>
        string Description { get; }
    }

    /// <summary>Identifies actor events raised by aura effects.</summary>
    public enum AuraActorEvent
    {
        onAuraApply
    }

    /// <summary>Defines an effect applied by an aura.</summary>
    public abstract class AuraEffectDefinition : IAutoDescription
    {
        /// <summary>Executes the effect against a target.</summary>
        public abstract void Execute(AuraContext ctx, Actor target);
        /// <summary>Gets the effect description.</summary>
        public abstract string Description { get; }
    }
}
