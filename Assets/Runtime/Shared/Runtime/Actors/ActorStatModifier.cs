using System.Collections.Generic;
using Game.Shared.Data;

namespace Game.Shared
{
    public sealed class ActorStatModifier
    {
        public ActorStatDefinition stat;
        public float flatBonus;
        public float percentBonus;
    }
}