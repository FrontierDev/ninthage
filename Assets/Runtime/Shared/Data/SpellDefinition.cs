using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    public enum SpellCastPhase
    {
        OnCastStart,
        OnCastEnd,
        OnChannelTick
    }

    [Serializable]
    public enum SpellHitType
    {
        Auto, Ability, Pet
    }

    public enum SpellDamageType
    {
        Melee, Ranged, Spell
    }

    [System.Serializable]
    public sealed class SpellComponent : ISerializationCallbackReceiver
    {
        public string Guid;

        [SerializeField] public SpellCastPhase CastPhase;
        [SerializeReference] public SpellEffectDefinition EffectDefinition;
        [SerializeReference] public SpellTargetDefinition TargetDefinition;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(Guid))
                Guid = System.Guid.NewGuid().ToString();
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }

    [Serializable]
    public sealed class SpellResourceCost
    {
        public ActorStatDefinition ResourceType;  // e.g., mana, stamina
        public SpellCastPhase CastPhase;  // When the cost is applied
        [Range(0f, 1f)]
        public float RefundOnInterrupt = 0.0f;  // Fraction to refund if the cast is interrupted
        public float Amount;
    }

    [Serializable]
    public sealed class SpellNameVariant
    {
        public RaceDefinition Race;
        public string NameOverride;
        public string DescriptionOverride;
        public Sprite IconOverride;
    }

    /// <summary>
    /// Defines a spell that an actor can have.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellDefinition_", menuName = "NinthAge/Definitions/Spell Definition")]
    public class SpellDefinition : DataDefinition
    {
        // Private fields
        [SerializeField] private string spellName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private List<string> internalTags = new();
        [SerializeField] private float baseCastTime;
        [Tooltip("The cooldown of the spell in seconds. Global cooldown always takes priority. If the spell uses cooldown charges, this is the cooldown per charge.")]

        // Cooldown settings
        [SerializeField] private float baseCooldown;
        [SerializeField] private float baseCharges;
        [SerializeField] private bool useCooldownCharges;
        [SerializeField] private bool cooldownScalesWithHaste = false;
        [SerializeField] private bool triggersGCD = true;
        [SerializeField] private float baseRange;

        [SerializeField] private bool isChanneled;
        [SerializeField] private bool canMoveWhileCasting;
        [SerializeField] private float baseTotalTicks;

        [SerializeField] private List<SpellResourceCost> baseResourceCosts = new();
        [SerializeField] private List<SpellComponent> baseComponents = new();

        [SerializeField] private List<SpellNameVariant> racialVariants = new();

        [SerializeField] private string sfxCastingLoop;

        // Public properties
        public string SpellName => spellName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<string> InternalTags => internalTags;

        public bool TriggersGCD => triggersGCD;
        public bool UseCooldownCharges => useCooldownCharges;
        public bool CooldownScalesWithHaste => cooldownScalesWithHaste;
        public float BaseCharges => baseCharges;
        public bool IsChanneled => isChanneled;
        public bool CanMoveWhileCasting => canMoveWhileCasting;
        public float BaseTotalTicks => baseTotalTicks;
        public IReadOnlyList<SpellNameVariant> RacialVariants => racialVariants;

        public float BaseCastTime => baseCastTime;
        public float BaseCooldown => baseCooldown;
        public float BaseRange => baseRange;
        public IReadOnlyList<SpellResourceCost> BaseResourceCosts => baseResourceCosts;
        public IReadOnlyList<SpellComponent> BaseComponents => baseComponents;

        [SerializeField] private List<ClassDefinition> associatedClasses = new();
        public IReadOnlyList<ClassDefinition> AssociatedClasses => associatedClasses;

        protected override string AssetPrefix => "Spell";

        public void AddAssociatedClass(ClassDefinition classDef)
        {
            if (!associatedClasses.Contains(classDef))
                associatedClasses.Add(classDef);
        }

        protected override void OnDefinitionValidate()
        {
            // Clear existing tags.
            internalTags = new List<string>();

            foreach (SpellResourceCost cost in baseResourceCosts)
            {
                if (cost.ResourceType != null)
                    internalTags.Add($"costs_{cost.ResourceType.DefinitionId.ToLower()}");
            }

            foreach (SpellComponent component in baseComponents)
            {
                if (component.EffectDefinition is SpellEffect_Damage damageEffect)
                {
                    foreach (DamageSchoolDefinition damageSchool in damageEffect.DamageSchools)
                    {
                        internalTags.Add($"{damageSchool.DefinitionId.ToLower()}_damage");
                    }
                }
            }
        }
    }
}