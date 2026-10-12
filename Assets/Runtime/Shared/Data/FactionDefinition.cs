using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Classifies a faction by its visibility and role.</summary>
    public enum FactionCategory
    {
        Primary,
        Regional,
        Hidden
    }

    [Serializable]
    /// <summary>Describes a reputation reward threshold.</summary>
    public class FactionRewardDescriptionEntry
    {
        /// <summary>Gets or sets the normalized reputation threshold.</summary>
        [Range(0f, 1f)] public float threshold;
        /// <summary>Gets or sets whether the reward is positive.</summary>
        public bool isPositive;
        /// <summary>Gets or sets the reward icon.</summary>
        public Sprite icon;
        /// <summary>Gets or sets the reward title.</summary>
        public string title;
        /// <summary>Gets or sets the listed benefits.</summary>
        public List<string> benefits = new();
        /// <summary>Gets or sets the listed penalties.</summary>
        public List<string> penalties = new();
    }

    /// <summary>
    /// Defines a faction that actors (both PCs and NPCs) can belong to.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionDefinition_", menuName = "NinthAge/Definitions/Faction Definition")]
    public class FactionDefinition : DataDefinition
    {
        [SerializeField]
        [TextArea(3, 5)]
        [FormerlySerializedAs("description")]
        private string _description;

        [SerializeField]
        [FormerlySerializedAs("category")]
        private FactionCategory _category;

        [SerializeField]
        [FormerlySerializedAs("icon")]
        private Sprite _icon;

        [SerializeField]
        [FormerlySerializedAs("color")]
        private Color _color;

        [SerializeField]
        [FormerlySerializedAs("isPlayable")]
        private bool _isPlayable;

        [SerializeField, FormerlySerializedAs("canWar")] private bool _canWar = false;
        /// <summary>Gets whether this faction can participate in faction wars.</summary>
        public bool CanWar => _canWar;

        [SerializeField, FormerlySerializedAs("maxPoints")] private int _maxPoints;
        /// <summary>Gets the maximum reputation points.</summary>
        public int MaxPoints => _maxPoints;

        [SerializeField, FormerlySerializedAs("alliedFactionIDs")] private List<string> _alliedFactionIDs = new();
        /// <summary>Gets the allied faction identifiers.</summary>
        public IReadOnlyList<string> AlliedFactionIDs => _alliedFactionIDs;

        [SerializeField, FormerlySerializedAs("enemyFactionIDs")] private List<string> _enemyFactionIDs = new();
        /// <summary>Gets the enemy faction identifiers.</summary>
        public IReadOnlyList<string> EnemyFactionIDs => _enemyFactionIDs;

        [SerializeField, FormerlySerializedAs("rewardDescriptions")] private List<FactionRewardDescriptionEntry> _rewardDescriptions = new();
        /// <summary>Gets the reputation reward descriptions.</summary>
        public IReadOnlyList<FactionRewardDescriptionEntry> RewardDescriptions => _rewardDescriptions;

        /// <summary>
        /// Detailed description of this race.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Color associated with this faction.
        /// </summary>
        public Color Color => _color;

        /// <summary>
        /// Category of this faction.
        /// </summary>
        public FactionCategory Category => _category;

        /// <summary>
        /// Indicates if players can obtain reputation with this faction.
        /// </summary>
        public bool IsPlayable => _isPlayable;

        protected override string AssetPrefix => "Faction";

        protected override void OnDefinitionValidate()
        {

#if UNITY_EDITOR
            // Ensure reciprocal relations are kept in sync across all faction definitions.
            if (string.IsNullOrEmpty(DefinitionID))
                return;

            var library = FactionDefinitionLibrary.Instance;
            if (library == null)
                return;

            var allDefs = library.GetAllDefinitions();

            foreach (var other in allDefs)
            {
                if (other == null || other == this)
                    continue;

                var otherId = other.DefinitionID;
                if (string.IsNullOrEmpty(otherId))
                    continue;

                bool thisAllied = _alliedFactionIDs.Contains(otherId);
                bool thisEnemy = _enemyFactionIDs.Contains(otherId);

                bool otherAlliedWithThis = other._alliedFactionIDs.Contains(DefinitionID);
                bool otherEnemyWithThis = other._enemyFactionIDs.Contains(DefinitionID);

                // If this lists other as allied, ensure other lists this as allied and not enemy
                if (thisAllied)
                {
                    if (!otherAlliedWithThis)
                    {
                        other._alliedFactionIDs.Add(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherEnemyWithThis)
                    {
                        other._enemyFactionIDs.Remove(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
                // If this lists other as enemy, ensure other lists this as enemy and not allied
                else if (thisEnemy)
                {
                    if (!otherEnemyWithThis)
                    {
                        other._enemyFactionIDs.Add(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherAlliedWithThis)
                    {
                        other._alliedFactionIDs.Remove(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
                // If this lists neither, ensure other also does not reference this
                else
                {
                    if (otherAlliedWithThis)
                    {
                        other._alliedFactionIDs.Remove(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherEnemyWithThis)
                    {
                        other._enemyFactionIDs.Remove(DefinitionID);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
            }
#endif
        }
    }
}
