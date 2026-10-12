using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
namespace Game.Shared.Data
{
    /// <summary>
    /// Defines a player class (e.g. Warrior, Mage, Ranger).
    /// </summary>
    [CreateAssetMenu(fileName = "DamageSchoolDefinition_", menuName = "NinthAge/Definitions/Damage School Definition")]
    public class DamageSchoolDefinition : DataDefinition
    {
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        [SerializeField, FormerlySerializedAs("mitigationRatingStat")] private ActorStatDefinition _mitigationRatingStat;
        [SerializeField, FormerlySerializedAs("ratingPerPercent")] private float _ratingPerPercent = 10f;

        /// <summary>Gets the damage school icon.</summary>
        public Sprite Icon => _icon;
        /// <summary>Gets the stat used to calculate mitigation.</summary>
        public ActorStatDefinition MitigationRatingStat => _mitigationRatingStat;
        /// <summary>Gets the rating required for one percent mitigation.</summary>
        public float RatingPerPercent => _ratingPerPercent;

        /// <summary>Calculates mitigation for the supplied actor stats.</summary>
        public float CalculateMitigation(ActorStatContainer targetStats)
        {
            if (_mitigationRatingStat == null) return 0f;

            float rating = targetStats.GetStat(_mitigationRatingStat.DefinitionID)?.CurrentValue ?? 0f;
            if (rating <= 0f) return 0f;

            var actor = targetStats.GetComponent<Actor>();
            int level = 1;
            if (actor is PlayerActor playerActor && playerActor.CharacterData != null)
                level = playerActor.GetLevel();
            else if (actor != null && actor.TryGetComponent<NPCActor>(out var npcActor))
                level = npcActor.GetLevel();
            float percent = CombatRatingHelper.RatingToPercent(rating, level, _ratingPerPercent);
            return Mathf.Clamp01(percent / 100f);
        }

        protected override string AssetPrefix => "DamageSchool";
    }
}
