using Game.Shared.Data;
using UnityEngine;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Runtime.Spellcasting
{
    /// <summary>Represents an aura applied to an actor.</summary>
    public sealed class AuraInstance
    {
        /// <summary>Gets the runtime aura instance identifier.</summary>
        public int InstancedID { get; private set; }
        /// <summary>Gets the aura definition.</summary>
        public AuraDefinition Definition { get; private set; }
        /// <summary>Gets the actor that applied the aura.</summary>
        public Actor Source { get; private set; }
        /// <summary>Gets the remaining duration.</summary>
        public float Duration { get; private set; }
        /// <summary>Gets the current stack count.</summary>
        public int Stacks { get; private set; }

        /// <summary>Initializes an aura instance.</summary>
        public AuraInstance(int instancedID, AuraDefinition definition, Actor source, Actor target, float duration, int stacks)
        {
            InstancedID = instancedID;
            Definition = definition;
            Source = source;
            Duration = duration;
            Stacks = stacks;
        }

        /// <summary>Advances the aura duration by frame time.</summary>
        public void Update()
        {
            Duration -= Time.deltaTime;
        }

        /// <summary>Sets the remaining duration.</summary>
        public void SetDuration(float newDuration)
        {
            Duration = newDuration;
        }

        /// <summary>Sets the stack count.</summary>
        public void SetStacks(int newStacks)
        {
            Stacks = newStacks;
        }
    }
}
