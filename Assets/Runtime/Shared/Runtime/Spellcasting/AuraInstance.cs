using UnityEngine;
using Game.Shared.Data;

namespace Game.Shared
{
    public sealed class AuraInstance
    {
        public int InstancedID { get; private set; }
        public AuraDefinition Definition { get; private set; }
        public Actor Source { get; private set; }
        public float Duration { get; private set; }
        public int Stacks { get; private set; }

        public AuraInstance(int instancedID, AuraDefinition definition, Actor source, Actor target, float duration, int stacks)
        {
            InstancedID = instancedID;
            Definition = definition;
            Source = source;
            Duration = duration;
            Stacks = stacks;
        }

        public void Update()
        {
            Duration -= Time.deltaTime;
        }

        public void SetDuration(float newDuration)
        {
            Duration = newDuration;
        }

        public void SetStacks(int newStacks)
        {
            Stacks = newStacks;
        }
    }
}