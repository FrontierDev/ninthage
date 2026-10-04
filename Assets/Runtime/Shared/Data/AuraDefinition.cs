using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    public abstract class AuraBehaviour : ScriptableObject
    {
        public abstract string Description { get; }
        public abstract void OnApplied(Actor caster, Actor target, int stacks);
        public abstract void OnRemoved(Actor caster, Actor target);
    }

    [Serializable]
    public enum AuraPhase
    {
        OnApply,
        OnExpire,
        OnTick,
        OnDispel
    }

    [Serializable]
    public enum AuraStackBehavior
    {
        RefreshDuration,
        StackPerCaster,
        FullyStacking,
        Unique,
        Condition_Extend,
        Condition_Stack
    }

    [System.Serializable]
    public sealed class AuraComponent : ISerializationCallbackReceiver
    {
        public string Guid;

        [SerializeField] public AuraPhase CastPhase;
        [SerializeReference] public AuraEffectDefinition EffectDefinition;
        [SerializeReference] public AuraTargetDefinition TargetDefinition;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(Guid))
                Guid = System.Guid.NewGuid().ToString();
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }

    /// <summary>
    /// Defines an aura that can be applied to an actor.
    /// </summary>
    [CreateAssetMenu(fileName = "AuraDefinition_", menuName = "NinthAge/Definitions/Aura Definition")]
    public class AuraDefinition : DataDefinition
    {
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool isDebuff;
        [SerializeField] private float baseDuration;
        [SerializeField] private float baseTickInterval;
        [SerializeField] private AuraStackBehavior stackBehavior = AuraStackBehavior.RefreshDuration;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private List<AuraComponent> components = new();

        [SerializeField] private AuraBehaviour auraBehaviour;
        public AuraBehaviour AuraBehaviour => auraBehaviour;

        public string Description => description;
        public Sprite Icon => icon;
        public bool IsDebuff => isDebuff;
        public float BaseDuration => baseDuration;
        public float BaseTickInterval => baseTickInterval;
        public AuraStackBehavior StackBehavior => stackBehavior;
        public int MaxStacks => maxStacks;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<AuraComponent> Components => components;

        protected override string AssetPrefix => "Aura";
    }
}