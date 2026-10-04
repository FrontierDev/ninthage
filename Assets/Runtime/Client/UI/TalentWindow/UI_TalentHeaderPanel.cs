using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_TalentHeaderPanel : UI_Panel
    {
        [SerializeField] private Image classIconImage;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text characterLevelText;

        [SerializeField] private TMP_Text unspentPointsText;
        [SerializeField] private TMP_Text spentPointsText;

        override protected void Awake()
        {
            Game.Shared.Networking.ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterService.onClientExperienceUpdated += OnClientExperienceUpdated;
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            var characterData = ClientAccountManager.ActiveCharacter;
            if (characterData != null)
            {
                var classID = characterData.ClassID;
                var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classID);
                classIconImage.sprite = classDef.Icon;
                classNameText.text = classID;

                var level = characterData.Level;
                characterLevelText.text = $"level {level}";
            }
        }

        private void OnClientExperienceUpdated(int level, int experience, int delta)
        {
            characterLevelText.text = $"level {level}";
            SetTalentPoints();
        }

        private void SetTalentPoints()
        {
            var playerActor = ClientAccountManager.PlayerActor;
            if (playerActor != null)
            {
                var talents = playerActor.GetComponent<PlayerTalents>();
                unspentPointsText.text = $"{talents.GetUnspentTalentPoints()}";
                spentPointsText.text = $"{talents.SpentTalentPoints}/{talents.GetMaximumTalentPoints()}";
            }
        }
    }
}