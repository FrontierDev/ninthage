using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    /// <summary>Identifies when a spell component executes.</summary>
    public enum SpellCastPhase
    {
        OnCastStart,
        OnCastEnd,
        OnChannelTick
    }

    [Serializable]
    /// <summary>Identifies the source category of a spell hit.</summary>
    public enum SpellHitType
    {
        Auto, Ability, Pet
    }

    /// <summary>Identifies the damage classification of a spell.</summary>
    public enum SpellDamageType
    {
        Melee, Ranged, Spell
    }

    [System.Serializable]
    /// <summary>Defines one target/effect component of a spell.</summary>
    public sealed class SpellComponent : ISerializationCallbackReceiver
    {
        /// <summary>Gets or sets the stable component identifier.</summary>
        public string Guid;

        /// <summary>Gets or sets the phase when the component executes.</summary>
        [SerializeField] public SpellCastPhase CastPhase;
        /// <summary>Gets or sets the component effect.</summary>
        [SerializeReference] public SpellEffectDefinition EffectDefinition;
        /// <summary>Gets or sets the component target definition.</summary>
        [SerializeReference] public SpellTargetDefinition TargetDefinition;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(Guid))
                Guid = System.Guid.NewGuid().ToString();
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }

    [Serializable]
    /// <summary>Defines a resource cost for a spell cast.</summary>
    public sealed class SpellResourceCost
    {
        /// <summary>Gets or sets the resource consumed.</summary>
        public ActorStatDefinition ResourceType;  // e.g., mana, stamina
        /// <summary>Gets or sets the cast phase when the cost is applied.</summary>
        public SpellCastPhase CastPhase;  // When the cost is applied
        [Range(0f, 1f)]
        /// <summary>Gets or sets the fraction refunded on interruption.</summary>
        public float RefundOnInterrupt = 0.0f;  // Fraction to refund if the cast is interrupted
        /// <summary>Gets or sets the resource amount.</summary>
        public float Amount;
    }

    [Serializable]
    /// <summary>Defines a race-specific spell name and presentation override.</summary>
    public sealed class SpellNameVariant
    {
        /// <summary>Gets or sets the affected race.</summary>
        public RaceDefinition Race;
        /// <summary>Gets or sets the replacement name.</summary>
        public string NameOverride;
        /// <summary>Gets or sets the replacement description.</summary>
        public string DescriptionOverride;
        /// <summary>Gets or sets the replacement icon.</summary>
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
        /// <summary>Gets the spell name.</summary>
        public string SpellName => spellName;
        /// <summary>Gets the spell description.</summary>
        public string Description => description;
        /// <summary>Gets the spell icon.</summary>
        public Sprite Icon => icon;
        /// <summary>Gets the authored spell tags.</summary>
        public IReadOnlyList<string> Tags => tags;
        /// <summary>Gets the generated internal tags.</summary>
        public IReadOnlyList<string> InternalTags => internalTags;

        /// <summary>Gets whether the spell triggers the global cooldown.</summary>
        public bool TriggersGCD => triggersGCD;
        /// <summary>Gets whether the spell uses charges.</summary>
        public bool UseCooldownCharges => useCooldownCharges;
        /// <summary>Gets whether cooldown scales with haste.</summary>
        public bool CooldownScalesWithHaste => cooldownScalesWithHaste;
        /// <summary>Gets the base charge count.</summary>
        public float BaseCharges => baseCharges;
        /// <summary>Gets whether the spell is channeled.</summary>
        public bool IsChanneled => isChanneled;
        /// <summary>Gets whether movement is allowed while casting.</summary>
        public bool CanMoveWhileCasting => canMoveWhileCasting;
        /// <summary>Gets the total number of channel ticks.</summary>
        public float BaseTotalTicks => baseTotalTicks;
        /// <summary>Gets racial spell variants.</summary>
        public IReadOnlyList<SpellNameVariant> RacialVariants => racialVariants;

        /// <summary>Gets the base cast time.</summary>
        public float BaseCastTime => baseCastTime;
        /// <summary>Gets the base cooldown.</summary>
        public float BaseCooldown => baseCooldown;
        /// <summary>Gets the base range.</summary>
        public float BaseRange => baseRange;
        /// <summary>Gets the base resource costs.</summary>
        public IReadOnlyList<SpellResourceCost> BaseResourceCosts => baseResourceCosts;
        /// <summary>Gets the base spell components.</summary>
        public IReadOnlyList<SpellComponent> BaseComponents => baseComponents;

        [SerializeField] private List<ClassDefinition> associatedClasses = new();
        /// <summary>Gets the classes associated with the spell.</summary>
        public IReadOnlyList<ClassDefinition> AssociatedClasses => associatedClasses;

        protected override string AssetPrefix => "Spell";

        /// <summary>Adds a class association if it is not already present.</summary>
        public void AddAssociatedClass(ClassDefinition classDef)
        {
            if (!associatedClasses.Contains(classDef))
                associatedClasses.Add(classDef);
        }

        protected override void OnDefinitionValidate()
        {
            // Clear existing tags.
            internalTags = new();

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
