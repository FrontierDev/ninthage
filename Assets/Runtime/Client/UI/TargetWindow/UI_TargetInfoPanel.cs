using Game.Shared;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_TargetInfoPanel : UI_Panel
    {
        [SerializeField] private TMP_Text actorNameText;
        [SerializeField] private TMP_Text actorInfoText;

        protected override void Awake()
        {
            base.Awake();

            Game.Shared.Networking.ActorService.onClientLateTargetedActorChanged += OnTargetChanged;
        }

        private void OnTargetChanged(Game.Shared.Actor oldTarget, Game.Shared.Actor newTarget)
        {
            if (newTarget != null)
            {
                actorInfoText.text = string.Empty;

                if (newTarget.TryGetComponent<Game.Shared.NPCActor>(out var npc))
                {
                    actorNameText.text = npc.GetName();
                    actorInfoText.text = $"Level {npc.GetLevel()}";
                }
                else
                {
                    PlayerActor player = newTarget as PlayerActor;
                    actorNameText.text = player.GetName();

                    var raceDef = Game.Shared.Data.RaceDefinitionLibrary.Instance.GetDefinition(player.raceID.value);
                    var classDef = Game.Shared.Data.ClassDefinitionLibrary.Instance.GetDefinition(player.classID.value);

                    actorInfoText.text = $"Level {player.level.value} {raceDef.DisplayName} {classDef.DisplayName}";
                }
            }
            else
            {
                actorNameText.text = string.Empty;
                actorInfoText.text = string.Empty;
            }
        }
    }
}