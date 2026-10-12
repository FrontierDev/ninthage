using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Defines a player class (e.g. Warrior, Mage, Ranger).
    /// </summary>
    [CreateAssetMenu(fileName = "DamageSchoolDefinition_", menuName = "NinthAge/Definitions/Damage School Definition")]
    public class DamageSchoolDefinition : DataDefinition
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private ActorStatDefinition mitigationRatingStat;
        [SerializeField] private float ratingPerPercent = 10f;

        /// <summary>Gets the damage school icon.</summary>
        public Sprite Icon => icon;
        /// <summary>Gets the stat used to calculate mitigation.</summary>
        public ActorStatDefinition MitigationRatingStat => mitigationRatingStat;
        /// <summary>Gets the rating required for one percent mitigation.</summary>
        public float RatingPerPercent => ratingPerPercent;

        /// <summary>Calculates mitigation for the supplied actor stats.</summary>
        public float CalculateMitigation(ActorStatContainer targetStats)
        {
            if (mitigationRatingStat == null) return 0f;

            float rating = targetStats.GetStat(mitigationRatingStat.DefinitionId)?.CurrentValue ?? 0f;
            if (rating <= 0f) return 0f;

            var actor = targetStats.GetComponent<Actor>();
            int level = 1;
            if (actor is PlayerActor playerActor && playerActor.CharacterData != null)
                level = playerActor.GetLevel();
            else if (actor != null && actor.TryGetComponent<NPCActor>(out var npcActor))
                level = npcActor.GetLevel();
            float percent = CombatRatingHelper.RatingToPercent(rating, level, ratingPerPercent);
            return Mathf.Clamp01(percent / 100f);
        }

        protected override string AssetPrefix => "DamageSchool";
    }
}
