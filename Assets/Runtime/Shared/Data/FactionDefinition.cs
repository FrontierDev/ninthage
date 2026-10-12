using System;
using System.Collections.Generic;
using UnityEngine;

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
        private string description;

        [SerializeField]
        private FactionCategory category;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Color color;

        [SerializeField]
        private bool isPlayable;

        [SerializeField] private bool canWar = false;
        /// <summary>Gets whether this faction can participate in faction wars.</summary>
        public bool CanWar => canWar;

        [SerializeField] private int maxPoints;
        /// <summary>Gets the maximum reputation points.</summary>
        public int MaxPoints => maxPoints;

        [SerializeField] private List<string> alliedFactionIDs = new();
        /// <summary>Gets the allied faction identifiers.</summary>
        public IReadOnlyList<string> AlliedFactionIDs => alliedFactionIDs;

        [SerializeField] private List<string> enemyFactionIDs = new();
        /// <summary>Gets the enemy faction identifiers.</summary>
        public IReadOnlyList<string> EnemyFactionIDs => enemyFactionIDs;

        [SerializeField] private List<FactionRewardDescriptionEntry> rewardDescriptions = new();
        /// <summary>Gets the reputation reward descriptions.</summary>
        public IReadOnlyList<FactionRewardDescriptionEntry> RewardDescriptions => rewardDescriptions;

        /// <summary>
        /// Detailed description of this race.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Color associated with this faction.
        /// </summary>
        public Color Color => color;

        /// <summary>
        /// Category of this faction.
        /// </summary>
        public FactionCategory Category => category;

        /// <summary>
        /// Indicates if players can obtain reputation with this faction.
        /// </summary>
        public bool IsPlayable => isPlayable;

        protected override string AssetPrefix => "Faction";

        protected override void OnDefinitionValidate()
        {

#if UNITY_EDITOR
            // Ensure reciprocal relations are kept in sync across all faction definitions.
            if (string.IsNullOrEmpty(DefinitionId))
                return;

            var library = FactionDefinitionLibrary.Instance;
            if (library == null)
                return;

            var allDefs = library.GetAllDefinitions();

            foreach (var other in allDefs)
            {
                if (other == null || other == this)
                    continue;

                var otherId = other.DefinitionId;
                if (string.IsNullOrEmpty(otherId))
                    continue;

                bool thisAllied = alliedFactionIDs.Contains(otherId);
                bool thisEnemy = enemyFactionIDs.Contains(otherId);

                bool otherAlliedWithThis = other.alliedFactionIDs.Contains(DefinitionId);
                bool otherEnemyWithThis = other.enemyFactionIDs.Contains(DefinitionId);

                // If this lists other as allied, ensure other lists this as allied and not enemy
                if (thisAllied)
                {
                    if (!otherAlliedWithThis)
                    {
                        other.alliedFactionIDs.Add(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherEnemyWithThis)
                    {
                        other.enemyFactionIDs.Remove(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
                // If this lists other as enemy, ensure other lists this as enemy and not allied
                else if (thisEnemy)
                {
                    if (!otherEnemyWithThis)
                    {
                        other.enemyFactionIDs.Add(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherAlliedWithThis)
                    {
                        other.alliedFactionIDs.Remove(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
                // If this lists neither, ensure other also does not reference this
                else
                {
                    if (otherAlliedWithThis)
                    {
                        other.alliedFactionIDs.Remove(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }

                    if (otherEnemyWithThis)
                    {
                        other.enemyFactionIDs.Remove(DefinitionId);
                        UnityEditor.EditorUtility.SetDirty(other);
                    }
                }
            }
#endif
        }
    }
}
