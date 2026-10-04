using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public class UI_FCTManager : MonoBehaviour
    {
        private static UI_FCTManager instance;
        public static UI_FCTManager Instance => instance;

        [Header("Prefab")]
        [SerializeField] private GameObject fctPrefab;

        [Header("Colors")]
        [SerializeField] private Color autoHitColor = Color.white;
        [SerializeField] private Color abilityHitColor = Color.lightGoldenRodYellow;
        [SerializeField] private Color petHitColor = Color.darkOrange;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private Color buffColor = Color.cyan;
        [SerializeField] private Color debuffColor = Color.yellow;
        [SerializeField] private Color experienceColor = Color.magenta;

        [Header("Spawn")]
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float randomSpreadX = 0.3f;

        private void Awake()
        {
            if (instance != null) return;
            instance = this;
        }

        private void Start()
        {
            ActorService.onCombatLogEntryReceived += OnCombatLogEntryReceived;
            CharacterService.onClientExperienceUpdated += OnExperienceUpdated;
        }

        private void OnDestroy()
        {
            ActorService.onCombatLogEntryReceived -= OnCombatLogEntryReceived;
            CharacterService.onClientExperienceUpdated -= OnExperienceUpdated;
        }

        private void OnExperienceUpdated(int level, int experience, int delta)
        {
            string content = $"+{delta} XP";
            Color color = experienceColor;

            Vector3 spawnPos = ClientAccountManager.PlayerActor.transform.position + Vector3.up * heightOffset;
            float spreadX = UnityEngine.Random.Range(-randomSpreadX, randomSpreadX);
            Vector3 direction = (Vector3.up + Vector3.right * spreadX).normalized;

            GameObject fctObj = Instantiate(fctPrefab, spawnPos, Quaternion.identity, UI_WorldSpace.Instance.fctContainer);
            var fct = fctObj.GetComponent<UI_FloatingCombatText>();
            fct.Initialize(content, color, direction, 1, 3);
        }

        private void OnCombatLogEntryReceived(CombatLogEntry entry)
        {
            if (entry.Source != ClientAccountManager.PlayerActor && entry.Target != ClientAccountManager.PlayerActor) return;
            if (entry.Target == null) return;

            string content;
            Color color;
            float sizeMultiplier = 1f;
            float durationMultiplier = 1f;

            switch (entry.EntryType)
            {
                case CombatHistoryEntryType.Damage:
                    color = GetHitTypeColor(entry.HitType);

                    switch (entry.ResultType)
                    {
                        case CombatLogResultType.Miss:
                            content = "Miss";
                            break;
                        case CombatLogResultType.Dodged:
                            content = "Dodge";
                            break;
                        case CombatLogResultType.Parried:
                            content = "Parry";
                            break;
                        case CombatLogResultType.Blocked:
                            content = "Block";
                            break;
                        case CombatLogResultType.Resisted:
                            content = "Resist";
                            break;
                        case CombatLogResultType.Absorbed:
                            content = "Absorb";
                            break;
                        case CombatLogResultType.Reflected:
                            content = "Reflect";
                            break;
                        case CombatLogResultType.CriticalHit:
                            content = entry.Amount.ToString();
                            sizeMultiplier = 1.5f;
                            durationMultiplier = 1.5f;
                            break;
                        default:
                            content = entry.Amount.ToString();
                            break;
                    }
                    break;
                case CombatHistoryEntryType.Heal:
                    content = $"+{entry.Amount}";
                    color = healColor;
                    if (entry.ResultType == CombatLogResultType.CriticalHeal)
                    {
                        sizeMultiplier = 1.5f;
                        durationMultiplier = 1.5f;
                    }
                    break;
                case CombatHistoryEntryType.BuffApplied:
                case CombatHistoryEntryType.BuffRemoved:
                    color = buffColor;
                    content = entry.Amount.ToString();
                    break;
                case CombatHistoryEntryType.DebuffApplied:
                case CombatHistoryEntryType.DebuffRemoved:
                    color = debuffColor;
                    content = entry.Amount.ToString();
                    break;
                default:
                    return;
            }

            Vector3 spawnPos = entry.Target.transform.position + Vector3.up * heightOffset;
            float spreadX = UnityEngine.Random.Range(-randomSpreadX, randomSpreadX);
            Vector3 direction = ((entry.Target != ClientAccountManager.PlayerActor ? Vector3.up : Vector3.down) + Vector3.right * spreadX).normalized;

            GameObject fctObj = Instantiate(fctPrefab, spawnPos, Quaternion.identity, UI_WorldSpace.Instance.fctContainer);
            var fct = fctObj.GetComponent<UI_FloatingCombatText>();
            fct.Initialize(content, color, direction, sizeMultiplier, durationMultiplier);
        }

        private Color GetHitTypeColor(SpellHitType hitType)
        {
            return hitType switch
            {
                SpellHitType.Auto => autoHitColor,
                SpellHitType.Ability => abilityHitColor,
                SpellHitType.Pet => petHitColor,
                _ => autoHitColor
            };
        }
    }
}