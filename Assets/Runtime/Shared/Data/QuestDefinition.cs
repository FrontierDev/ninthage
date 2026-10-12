using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Identifies the category of a quest.</summary>
    public enum QuestType
    {
        Normal,
        Story,
        Daily,
        Crafting
    }

    /// <summary>Identifies the repeat cooldown of a quest.</summary>
    public enum QuestCooldown
    {
        None,
        Daily,
        Weekly,
        Monthly,
    }

    [System.Serializable]
    /// <summary>Defines a reputation reward granted by a quest.</summary>
    public class QuestReputationReward
    {
        [SerializeField] private FactionDefinition faction;
        [SerializeField] private int reputationPoints;

        /// <summary>Gets the rewarded faction.</summary>
        public FactionDefinition Faction => faction;
        /// <summary>Gets the reputation points awarded.</summary>
        public int ReputationPoints => reputationPoints;

        /// <summary>Initializes an empty reputation reward.</summary>
        public QuestReputationReward() { }

        /// <summary>Initializes a reputation reward.</summary>
        public QuestReputationReward(FactionDefinition faction, int reputationPoints)
        {
            this.faction = faction;
            this.reputationPoints = reputationPoints;
        }
    }

    [System.Serializable]
    /// <summary>Defines an item reward granted by a quest.</summary>
    public class QuestItemReward
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;

        /// <summary>Gets the rewarded item.</summary>
        public ItemDefinition Item => item;
        /// <summary>Gets the rewarded quantity.</summary>
        public int Quantity => quantity;

        /// <summary>Initializes an empty item reward.</summary>
        public QuestItemReward() { }

        /// <summary>Initializes an item reward.</summary>
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

        /// <summary>Gets the quest icon.</summary>
        public Sprite Icon => icon;
        /// <summary>Gets the quest description.</summary>
        public string Description => description;
        /// <summary>Gets the minimum quest level.</summary>
        public int Level => level;
        /// <summary>Gets the quest type.</summary>
        public QuestType Type => type;
        /// <summary>Gets the recommended player count.</summary>
        public int RecommendedPlayers => recommendedPlayers;
        /// <summary>Gets the quest timer in seconds.</summary>
        public int TimerSeconds => timerSeconds;

        /* Quest Rewards */
        [SerializeField] private int goldReward = 0;
        [SerializeField] private int experienceReward = 0;
        [SerializeField] private QuestReputationReward reputationReward = null;
        [SerializeField] private List<QuestItemReward> itemRewards = new();
        [SerializeField] bool giveAllItems = false;

        /// <summary>Gets the gold reward.</summary>
        public int GoldReward => goldReward;
        /// <summary>Gets the experience reward.</summary>
        public int ExperienceReward => experienceReward;
        /// <summary>Gets the reputation reward.</summary>
        public QuestReputationReward ReputationReward => reputationReward;
        /// <summary>Gets the item rewards.</summary>
        public IReadOnlyList<QuestItemReward> ItemRewards => itemRewards;
        /// <summary>Gets whether all item rewards are granted.</summary>
        public bool GiveAllItems => giveAllItems;

        /* Repeatable Quests */
        [SerializeField] private QuestCooldown cooldown = QuestCooldown.None;
        [SerializeField] private bool isRepeatable = false;

        /// <summary>Gets the quest cooldown.</summary>
        public QuestCooldown Cooldown => cooldown;
        /// <summary>Gets whether the quest can be repeated.</summary>
        public bool IsRepeatable => isRepeatable;

        /* Quest Objectives */
        [SerializeReference] private List<QuestObjective> objectives = new();
        /// <summary>Gets the quest objectives.</summary>
        public IReadOnlyList<QuestObjective> Objectives => objectives;

        /* Quest Requirements */
        [SerializeField] private List<ConditionDefinition> requirements = new();
        /// <summary>Gets the quest requirements.</summary>
        public IReadOnlyList<ConditionDefinition> Requirements => requirements;

        protected override string AssetPrefix => "Quest";
        protected override void OnDefinitionValidate() { }
    }
}
