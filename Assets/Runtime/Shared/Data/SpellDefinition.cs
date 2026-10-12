using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("Guid")]
        public string GUID;

        /// <summary>Gets or sets the phase when the component executes.</summary>
        [SerializeField] public SpellCastPhase CastPhase;
        /// <summary>Gets or sets the component effect.</summary>
        [SerializeReference] public SpellEffectDefinition EffectDefinition;
        /// <summary>Gets or sets the component target definition.</summary>
        [SerializeReference] public SpellTargetDefinition TargetDefinition;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(GUID))
                GUID = System.Guid.NewGuid().ToString();
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
        [SerializeField, FormerlySerializedAs("spellName")] private string _spellName;
        [SerializeField, FormerlySerializedAs("description")] private string _description;
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        [SerializeField, FormerlySerializedAs("tags")] private List<string> _tags = new();
        [SerializeField, FormerlySerializedAs("internalTags")] private List<string> _internalTags = new();
        [SerializeField, FormerlySerializedAs("baseCastTime")] private float _baseCastTime;
        [Tooltip("The cooldown of the spell in seconds. Global cooldown always takes priority. If the spell uses cooldown charges, this is the cooldown per charge.")]

        // Cooldown settings
        [SerializeField, FormerlySerializedAs("baseCooldown")] private float _baseCooldown;
        [SerializeField, FormerlySerializedAs("baseCharges")] private float _baseCharges;
        [SerializeField, FormerlySerializedAs("useCooldownCharges")] private bool _useCooldownCharges;
        [SerializeField, FormerlySerializedAs("cooldownScalesWithHaste")] private bool _cooldownScalesWithHaste = false;
        [SerializeField, FormerlySerializedAs("triggersGCD")] private bool _triggersGCD = true;
        [SerializeField, FormerlySerializedAs("baseRange")] private float _baseRange;

        [SerializeField, FormerlySerializedAs("isChanneled")] private bool _isChanneled;
        [SerializeField, FormerlySerializedAs("canMoveWhileCasting")] private bool _canMoveWhileCasting;
        [SerializeField, FormerlySerializedAs("baseTotalTicks")] private float _baseTotalTicks;

        [SerializeField, FormerlySerializedAs("baseResourceCosts")] private List<SpellResourceCost> _baseResourceCosts = new();
        [SerializeField, FormerlySerializedAs("baseComponents")] private List<SpellComponent> _baseComponents = new();

        [SerializeField, FormerlySerializedAs("racialVariants")] private List<SpellNameVariant> _racialVariants = new();

        [SerializeField, FormerlySerializedAs("sfxCastingLoop")] private string _sfxCastingLoop;

        // Public properties
        /// <summary>Gets the spell name.</summary>
        public string SpellName => _spellName;
        /// <summary>Gets the spell description.</summary>
        public string Description => _description;
        /// <summary>Gets the spell icon.</summary>
        public Sprite Icon => _icon;
        /// <summary>Gets the authored spell tags.</summary>
        public IReadOnlyList<string> Tags => _tags;
        /// <summary>Gets the generated internal tags.</summary>
        public IReadOnlyList<string> InternalTags => _internalTags;

        /// <summary>Gets whether the spell triggers the global cooldown.</summary>
        public bool TriggersGCD => _triggersGCD;
        /// <summary>Gets whether the spell uses charges.</summary>
        public bool UseCooldownCharges => _useCooldownCharges;
        /// <summary>Gets whether cooldown scales with haste.</summary>
        public bool CooldownScalesWithHaste => _cooldownScalesWithHaste;
        /// <summary>Gets the base charge count.</summary>
        public float BaseCharges => _baseCharges;
        /// <summary>Gets whether the spell is channeled.</summary>
        public bool IsChanneled => _isChanneled;
        /// <summary>Gets whether movement is allowed while casting.</summary>
        public bool CanMoveWhileCasting => _canMoveWhileCasting;
        /// <summary>Gets the total number of channel ticks.</summary>
        public float BaseTotalTicks => _baseTotalTicks;
        /// <summary>Gets racial spell variants.</summary>
        public IReadOnlyList<SpellNameVariant> RacialVariants => _racialVariants;

        /// <summary>Gets the base cast time.</summary>
        public float BaseCastTime => _baseCastTime;
        /// <summary>Gets the base cooldown.</summary>
        public float BaseCooldown => _baseCooldown;
        /// <summary>Gets the base range.</summary>
        public float BaseRange => _baseRange;
        /// <summary>Gets the base resource costs.</summary>
        public IReadOnlyList<SpellResourceCost> BaseResourceCosts => _baseResourceCosts;
        /// <summary>Gets the base spell components.</summary>
        public IReadOnlyList<SpellComponent> BaseComponents => _baseComponents;

        [SerializeField, FormerlySerializedAs("associatedClasses")] private List<ClassDefinition> _associatedClasses = new();
        /// <summary>Gets the classes associated with the spell.</summary>
        public IReadOnlyList<ClassDefinition> AssociatedClasses => _associatedClasses;

        protected override string AssetPrefix => "Spell";

        /// <summary>Adds a class association if it is not already present.</summary>
        public void AddAssociatedClass(ClassDefinition classDef)
        {
            if (!_associatedClasses.Contains(classDef))
                _associatedClasses.Add(classDef);
        }

        protected override void OnDefinitionValidate()
        {
            // Clear existing tags.
            _internalTags = new();

            foreach (SpellResourceCost cost in _baseResourceCosts)
            {
                if (cost.ResourceType != null)
                    _internalTags.Add($"costs_{cost.ResourceType.DefinitionID.ToLower()}");
            }

            foreach (SpellComponent component in _baseComponents)
            {
                if (component.EffectDefinition is SpellEffect_Damage damageEffect)
                {
                    foreach (DamageSchoolDefinition damageSchool in damageEffect.DamageSchools)
                    {
                        _internalTags.Add($"{damageSchool.DefinitionID.ToLower()}_damage");
                    }
                }
            }
        }
    }
}
