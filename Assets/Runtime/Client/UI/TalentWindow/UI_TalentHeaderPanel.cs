using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentHeaderPanel</c> implementation.
    /// </summary>
    public sealed class UI_TalentHeaderPanel : UI_Panel
    {
        [FormerlySerializedAs("classIconImage")]
        [SerializeField] private Image _classIconImage;
        [FormerlySerializedAs("classNameText")]
        [SerializeField] private TMP_Text _classNameText;
        [FormerlySerializedAs("characterLevelText")]
        [SerializeField] private TMP_Text _characterLevelText;

        [FormerlySerializedAs("unspentPointsText")]
        [SerializeField] private TMP_Text _unspentPointsText;
        [FormerlySerializedAs("spentPointsText")]
        [SerializeField] private TMP_Text _spentPointsText;

        override protected void Awake()
        {
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterEndpoint.onClientExperienceUpdated += OnClientExperienceUpdated;
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            var characterData = ClientAccountManager.ActiveCharacter;
            if (characterData != null)
            {
                var classID = characterData.ClassID;
                var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classID);
                _classIconImage.sprite = classDef.Icon;
                _classNameText.text = classID;

                var level = characterData.Level;
                _characterLevelText.text = $"level {level}";
            }
        }

        private void OnClientExperienceUpdated(int level, int experience, int delta)
        {
            _characterLevelText.text = $"level {level}";
            SetTalentPoints();
        }

        private void SetTalentPoints()
        {
            var playerActor = ClientAccountManager.PlayerActor;
            if (playerActor != null)
            {
                var talents = playerActor.GetComponent<PlayerTalents>();
                _unspentPointsText.text = $"{talents.GetUnspentTalentPoints()}";
                _spentPointsText.text = $"{talents.SpentTalentPoints}/{talents.GetMaximumTalentPoints()}";
            }
        }
    }
}
