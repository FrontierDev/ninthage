using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Defines behaviour executed when an aura changes state.</summary>
    public abstract class AuraBehaviour : ScriptableObject
    {
        /// <summary>Gets the behaviour description shown in the editor.</summary>
        public abstract string Description { get; }
        /// <summary>Applies the behaviour when the aura is applied.</summary>
        public abstract void OnApplied(Actor caster, Actor target, int stacks);
        /// <summary>Removes the behaviour when the aura is removed.</summary>
        public abstract void OnRemoved(Actor caster, Actor target);
    }

    /// <summary>Identifies the aura lifecycle phase for a component.</summary>
    [Serializable]
    public enum AuraPhase
    {
        OnApply,
        OnExpire,
        OnTick,
        OnDispel
    }

    /// <summary>Defines how aura stacks are combined.</summary>
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

    /// <summary>Describes one effect and target pair in an aura.</summary>
    [System.Serializable]
    public sealed class AuraComponent : ISerializationCallbackReceiver
    {
        /// <summary>Gets or sets the stable component identifier.</summary>
        public string Guid;

        /// <summary>Gets or sets when the component executes.</summary>
        [SerializeField] public AuraPhase CastPhase;
        /// <summary>Gets or sets the effect executed by the component.</summary>
        [SerializeReference] public AuraEffectDefinition EffectDefinition;
        /// <summary>Gets or sets the target definition used by the component.</summary>
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
        /// <summary>Gets the behaviour associated with this aura.</summary>
        public AuraBehaviour AuraBehaviour => auraBehaviour;

        /// <summary>Gets the aura description.</summary>
        public string Description => description;
        /// <summary>Gets the aura icon.</summary>
        public Sprite Icon => icon;
        /// <summary>Gets whether the aura is harmful.</summary>
        public bool IsDebuff => isDebuff;
        /// <summary>Gets the base aura duration.</summary>
        public float BaseDuration => baseDuration;
        /// <summary>Gets the base interval between ticks.</summary>
        public float BaseTickInterval => baseTickInterval;
        /// <summary>Gets the stack behaviour.</summary>
        public AuraStackBehavior StackBehavior => stackBehavior;
        /// <summary>Gets the maximum number of stacks.</summary>
        public int MaxStacks => maxStacks;
        /// <summary>Gets the aura tags.</summary>
        public IReadOnlyList<string> Tags => tags;
        /// <summary>Gets the aura components.</summary>
        public IReadOnlyList<AuraComponent> Components => components;

        protected override string AssetPrefix => "Aura";
    }
}
