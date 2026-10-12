using Game.Shared.Data;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>
    /// Master game configuration.
    /// Contains only essential tunable parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "NinthAge/Config/Game Configuration", fileName = "GameConfiguration")]
    public class GameConfiguration : ScriptableObject
    {
        [Header("Persistence Settings")]
        [SerializeField]
        /// <summary>Gets or sets the player persistence file path.</summary>
        public string PlayerDataFilePath = "player_data.json";
        [SerializeField]
        /// <summary>Gets or sets whether player data is cleared at startup.</summary>
        public bool ClearPlayerDataOnStart = false;

        [Header("World Settings")]
        [SerializeField]
        /// <summary>Gets or sets the world chunk size.</summary>
        public int ChunkSize = 100;

        [Header("Physics Settings")]
        [SerializeField]
        /// <summary>Gets or sets the movement gravity.</summary>
        public float Gravity = -9.81f;
        [SerializeField]
        /// <summary>Gets or sets the ground detection radius.</summary>
        public float GroundDetectionRadius = 0.5f;
        [SerializeField]
        /// <summary>Gets or sets the layer mask name used for ground detection.</summary>
        public string GroundDetectionMaskName = "Terrain"; // Default to layer 0 (can be changed in the editor)

        [Header("Movement Settings")]
        /// <summary>Gets or sets the maximum fall speed.</summary>
        public float MaxFallSpeed = 50f;

        [Header("Spellcasting Settings")]
        /// <summary>Gets or sets the spell range buffer used during channel ticks.</summary>
        public float SpellCastRangeTickBuffer = 2.0f; // Extra range buffer to account for movement and latency
        /// <summary>Gets or sets the spell range buffer used at cast end.</summary>
        public float SpellCastRangeEndBuffer = 5.0f; // Additional buffer for the final range check at cast end
        /// <summary>Gets or sets the optional spell hit chance stat.</summary>
        public ActorStatDefinition SpellHitChanceStat; // Optional stat to determine hit chance for damaging spells
        /// <summary>Gets or sets the optional melee hit chance stat.</summary>
        public ActorStatDefinition MeleeHitChanceStat; // Optional stat to determine hit chance for melee attacks
        /// <summary>Gets or sets the optional ranged hit chance stat.</summary>
        public ActorStatDefinition RangedHitChanceStat; // Optional stat to determine hit chance for ranged attacks
        /// <summary>Gets or sets the base miss chance percentage.</summary>
        public float BaseMissChance = 5f; // Base miss chance percentage if no stat is defined
        /// <summary>Gets or sets the miss chance penalty per level difference.</summary>
        public float MissChancePerLevelPenalty = 1f; // Additional miss chance per level difference between caster and target
        /// <summary>Gets or sets the maximum miss chance percentage.</summary>
        public float MaximumMissChance = 99f;
        /// <summary>Gets or sets the melee critical chance stat.</summary>
        public ActorStatDefinition MeleeCritChanceStat;
        /// <summary>Gets or sets the ranged critical chance stat.</summary>
        public ActorStatDefinition RangedCritChanceStat;
        /// <summary>Gets or sets the spell critical chance stat.</summary>
        public ActorStatDefinition SpellCritChanceStat;
        /// <summary>Gets or sets the optional critical resistance stat.</summary>
        public ActorStatDefinition CritResistanceStat; // Optional stat to reduce incoming crit chance
        /// <summary>Gets or sets the base critical chance percentage.</summary>
        public float BaseCritChance = 5f;
        /// <summary>Gets or sets the critical chance penalty per level.</summary>
        public float CritChancePerLevelPenalty = 1f; // Crit chance reduction per level the target is above you
        /// <summary>Gets or sets the critical damage multiplier.</summary>
        public float CritDamageMultiplier = 1.5f;

        /// <summary>Gets or sets the global cooldown duration.</summary>
        public float GlobalCooldownDuration = 1.0f;

        [Header("Item Quality Colors")]
        /// <summary>Gets or sets the poor item color.</summary>
        public Color PoorColor = new(0.62f, 0.62f, 0.62f, 1f);
        /// <summary>Gets or sets the common item color.</summary>
        public Color CommonColor = new(1f, 1f, 1f, 1f);
        /// <summary>Gets or sets the uncommon item color.</summary>
        public Color UncommonColor = new(0.12f, 1f, 0f, 1f);
        /// <summary>Gets or sets the rare item color.</summary>
        public Color RareColor = new(0f, 0.44f, 0.87f, 1f);
        /// <summary>Gets or sets the epic item color.</summary>
        public Color EpicColor = new(0.64f, 0.21f, 0.93f, 1f);
        /// <summary>Gets or sets the legendary item color.</summary>
        public Color LegendaryColor = new(1f, 0.5f, 0f, 1f);

        [Header("Auto-Attack Settings")]
        /// <summary>Gets or sets the configured auto-attack spell.</summary>
        public SpellDefinition AutoAttackSpell;

        [Header("Connection Settings")]
        [SerializeField]
        /// <summary>Gets or sets the server address.</summary>
        public string ServerAddress = "127.0.0.1";
        [SerializeField]
        /// <summary>Gets or sets the server port.</summary>
        public ushort ServerPort = 8888;
    }
}
