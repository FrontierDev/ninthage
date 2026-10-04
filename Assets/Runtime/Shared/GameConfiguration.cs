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
        public string PlayerDataFilePath = "player_data.json";
        [SerializeField]
        public bool ClearPlayerDataOnStart = false;

        [Header("World Settings")]
        [SerializeField]
        public int ChunkSize = 100;

        [Header("Physics Settings")]
        [SerializeField]
        public float Gravity = -9.81f;
        [SerializeField]
        public float GroundDetectionRadius = 0.5f;
        [SerializeField]
        public string GroundDetectionMaskName = "Terrain"; // Default to layer 0 (can be changed in the editor)

        [Header("Movement Settings")]
        public float MaxFallSpeed = 50f;

        [Header("Spellcasting Settings")]
        public float SpellCastRangeTickBuffer = 2.0f; // Extra range buffer to account for movement and latency
        public float SpellCastRangeEndBuffer = 5.0f; // Additional buffer for the final range check at cast end
        public ActorStatDefinition SpellHitChanceStat; // Optional stat to determine hit chance for damaging spells
        public ActorStatDefinition MeleeHitChanceStat; // Optional stat to determine hit chance for melee attacks
        public ActorStatDefinition RangedHitChanceStat; // Optional stat to determine hit chance for ranged attacks
        public float BaseMissChance = 5f; // Base miss chance percentage if no stat is defined
        public float MissChancePerLevelPenalty = 1f; // Additional miss chance per level difference between caster and target
        public float MaximumMissChance = 99f;
        public ActorStatDefinition MeleeCritChanceStat;
        public ActorStatDefinition RangedCritChanceStat;
        public ActorStatDefinition SpellCritChanceStat;
        public ActorStatDefinition CritResistanceStat; // Optional stat to reduce incoming crit chance
        public float BaseCritChance = 5f;
        public float CritChancePerLevelPenalty = 1f; // Crit chance reduction per level the target is above you
        public float CritDamageMultiplier = 1.5f;

        public float GlobalCooldownDuration = 1.0f;

        [Header("Item Quality Colors")]
        public Color PoorColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        public Color CommonColor = new Color(1f, 1f, 1f, 1f);
        public Color UncommonColor = new Color(0.12f, 1f, 0f, 1f);
        public Color RareColor = new Color(0f, 0.44f, 0.87f, 1f);
        public Color EpicColor = new Color(0.64f, 0.21f, 0.93f, 1f);
        public Color LegendaryColor = new Color(1f, 0.5f, 0f, 1f);

        [Header("Auto-Attack Settings")]
        public SpellDefinition AutoAttackSpell;

        [Header("Connection Settings")]
        [SerializeField]
        public string ServerAddress = "127.0.0.1";
        [SerializeField]
        public ushort ServerPort = 8888;
    }
}