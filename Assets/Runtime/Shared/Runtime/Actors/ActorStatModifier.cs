using Game.Shared.Data;
using System.Collections.Generic;

namespace Game.Shared
{
    /// <summary>Defines a stat modifier used by actor systems.</summary>
    public sealed class ActorStatModifier
    {
        /// <summary>Gets or sets the affected stat.</summary>
        public ActorStatDefinition stat;
        /// <summary>Gets or sets the flat stat bonus.</summary>
        public float flatBonus;
        /// <summary>Gets or sets the percentage stat bonus.</summary>
        public float percentBonus;
    }
}
