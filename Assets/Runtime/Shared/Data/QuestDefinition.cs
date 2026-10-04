using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{
    public enum QuestType
    {
        Normal,
        Story,
        Daily,
        Crafting
    }

    public enum QuestCooldown
    {
        None,
        Daily,
        Weekly,
        Monthly,
    }

    [System.Serializable]
    public class QuestReputationReward
    {
        [SerializeField] private FactionDefinition faction;
        [SerializeField] private int reputationPoints;

        public FactionDefinition Faction => faction;
        public int ReputationPoints => reputationPoints;

        public QuestReputationReward() { }

        public QuestReputationReward(FactionDefinition faction, int reputationPoints)
        {
            this.faction = faction;
            this.reputationPoints = reputationPoints;
        }
    }

    [System.Serializable]
    public class QuestItemReward
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        public QuestItemReward() { }

        public QuestItemReward(ItemDefinition item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }

    /// <summary>
    /// Defines a quest.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinition_", menuName = "NinthAge/Definitions/Quest")]
    public class QuestDefinition : DataDefinition
    {
        /* Quest Categorisation */
        [SerializeField] private Sprite icon;
        [SerializeField] private string description;
        [SerializeField] private int level = 1;
        [SerializeField] private QuestType type = QuestType.Normal;
        [SerializeField] private int recommendedPlayers = 1;
        [SerializeField] private int timerSeconds = 0;

        public Sprite Icon => icon;
        public string Description => description;
        public int Level => level;
        public QuestType Type => type;
        public int RecommendedPlayers => recommendedPlayers;
        public int TimerSeconds => timerSeconds;

        /* Quest Rewards */
        [SerializeField] private int goldReward = 0;
        [SerializeField] private int experienceReward = 0;
        [SerializeField] private QuestReputationReward reputationReward = null;
        [SerializeField] private List<QuestItemReward> itemRewards = new List<QuestItemReward>();
        [SerializeField] bool giveAllItems = false;

        public int GoldReward => goldReward;
        public int ExperienceReward => experienceReward;
        public QuestReputationReward ReputationReward => reputationReward;
        public IReadOnlyList<QuestItemReward> ItemRewards => itemRewards;
        public bool GiveAllItems => giveAllItems;

        /* Repeatable Quests */
        [SerializeField] private QuestCooldown cooldown = QuestCooldown.None;
        [SerializeField] private bool isRepeatable = false;

        public QuestCooldown Cooldown => cooldown;
        public bool IsRepeatable => isRepeatable;

        /* Quest Objectives */
        [SerializeReference] private List<QuestObjective> objectives = new List<QuestObjective>();
        public IReadOnlyList<QuestObjective> Objectives => objectives;

        /* Quest Requirements */
        [SerializeField] private List<ConditionDefinition> requirements = new List<ConditionDefinition>();
        public IReadOnlyList<ConditionDefinition> Requirements => requirements;

        protected override string AssetPrefix => "Quest";
        protected override void OnDefinitionValidate() { }
    }
}