using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data.Conditions;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("faction")] private FactionDefinition _faction;
        [SerializeField, FormerlySerializedAs("reputationPoints")] private int _reputationPoints;

        /// <summary>Gets the rewarded faction.</summary>
        public FactionDefinition Faction => _faction;
        /// <summary>Gets the reputation points awarded.</summary>
        public int ReputationPoints => _reputationPoints;

        /// <summary>Initializes an empty reputation reward.</summary>
        public QuestReputationReward() { }

        /// <summary>Initializes a reputation reward.</summary>
        public QuestReputationReward(FactionDefinition faction, int reputationPoints)
        {
            _faction = faction;
            _reputationPoints = reputationPoints;
        }
    }

    [System.Serializable]
    /// <summary>Defines an item reward granted by a quest.</summary>
    public class QuestItemReward
    {
        [SerializeField, FormerlySerializedAs("item")] private ItemDefinition _item;
        [SerializeField, FormerlySerializedAs("quantity")] private int _quantity = 1;

        /// <summary>Gets the rewarded item.</summary>
        public ItemDefinition Item => _item;
        /// <summary>Gets the rewarded quantity.</summary>
        public int Quantity => _quantity;

        /// <summary>Initializes an empty item reward.</summary>
        public QuestItemReward() { }

        /// <summary>Initializes an item reward.</summary>
        public QuestItemReward(ItemDefinition item, int quantity)
        {
            _item = item;
            _quantity = quantity;
        }
    }

    /// <summary>
    /// Defines a quest.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinition_", menuName = "NinthAge/Definitions/Quest")]
    public class QuestDefinition : DataDefinition
    {
        /* Quest Categorisation */
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        [SerializeField, FormerlySerializedAs("description")] private string _description;
        [SerializeField, FormerlySerializedAs("level")] private int _level = 1;
        [SerializeField, FormerlySerializedAs("type")] private QuestType _type = QuestType.Normal;
        [SerializeField, FormerlySerializedAs("recommendedPlayers")] private int _recommendedPlayers = 1;
        [SerializeField, FormerlySerializedAs("timerSeconds")] private int _timerSeconds = 0;

        /// <summary>Gets the quest icon.</summary>
        public Sprite Icon => _icon;
        /// <summary>Gets the quest description.</summary>
        public string Description => _description;
        /// <summary>Gets the minimum quest level.</summary>
        public int Level => _level;
        /// <summary>Gets the quest type.</summary>
        public QuestType Type => _type;
        /// <summary>Gets the recommended player count.</summary>
        public int RecommendedPlayers => _recommendedPlayers;
        /// <summary>Gets the quest timer in seconds.</summary>
        public int TimerSeconds => _timerSeconds;

        /* Quest Rewards */
        [SerializeField, FormerlySerializedAs("goldReward")] private int _goldReward = 0;
        [SerializeField, FormerlySerializedAs("experienceReward")] private int _experienceReward = 0;
        [SerializeField, FormerlySerializedAs("reputationReward")] private QuestReputationReward _reputationReward = null;
        [SerializeField, FormerlySerializedAs("itemRewards")] private List<QuestItemReward> _itemRewards = new();
        [SerializeField] bool giveAllItems = false;

        /// <summary>Gets the gold reward.</summary>
        public int GoldReward => _goldReward;
        /// <summary>Gets the experience reward.</summary>
        public int ExperienceReward => _experienceReward;
        /// <summary>Gets the reputation reward.</summary>
        public QuestReputationReward ReputationReward => _reputationReward;
        /// <summary>Gets the item rewards.</summary>
        public IReadOnlyList<QuestItemReward> ItemRewards => _itemRewards;
        /// <summary>Gets whether all item rewards are granted.</summary>
        public bool GiveAllItems => giveAllItems;

        /* Repeatable Quests */
        [SerializeField, FormerlySerializedAs("cooldown")] private QuestCooldown _cooldown = QuestCooldown.None;
        [SerializeField, FormerlySerializedAs("isRepeatable")] private bool _isRepeatable = false;

        /// <summary>Gets the quest cooldown.</summary>
        public QuestCooldown Cooldown => _cooldown;
        /// <summary>Gets whether the quest can be repeated.</summary>
        public bool IsRepeatable => _isRepeatable;

        /* Quest Objectives */
        [SerializeReference, FormerlySerializedAs("objectives")] private List<QuestObjective> _objectives = new();
        /// <summary>Gets the quest objectives.</summary>
        public IReadOnlyList<QuestObjective> Objectives => _objectives;

        /* Quest Requirements */
        [SerializeField, FormerlySerializedAs("requirements")] private List<ConditionDefinition> _requirements = new();
        /// <summary>Gets the quest requirements.</summary>
        public IReadOnlyList<ConditionDefinition> Requirements => _requirements;

        protected override string AssetPrefix => "Quest";
        protected override void OnDefinitionValidate() { }
    }
}
