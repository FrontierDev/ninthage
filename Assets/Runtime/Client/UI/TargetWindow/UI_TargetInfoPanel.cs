using Game.Shared;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TargetInfoPanel</c> implementation.
    /// </summary>
    public sealed class UI_TargetInfoPanel : UI_Panel
    {
        [FormerlySerializedAs("actorNameText")]
        [SerializeField] private TMP_Text _actorNameText;
        [FormerlySerializedAs("actorInfoText")]
        [SerializeField] private TMP_Text _actorInfoText;

        protected override void Awake()
        {
            base.Awake();

            Game.Shared.Networking.ActorEndpoint.onClientLateTargetedActorChanged += OnTargetChanged;
        }

        private void OnTargetChanged(Game.Shared.Runtime.Actors.Actor oldTarget, Game.Shared.Runtime.Actors.Actor newTarget)
        {
            if (newTarget != null)
            {
                _actorInfoText.text = string.Empty;

                if (newTarget.TryGetComponent<Game.Shared.Runtime.Actors.NPCActor>(out var npc))
                {
                    _actorNameText.text = npc.GetName();
                    _actorInfoText.text = $"Level {npc.GetLevel()}";
                }
                else
                {
                    PlayerActor player = newTarget as PlayerActor;
                    _actorNameText.text = player.GetName();

                    var raceDef = Game.Shared.Data.RaceDefinitionLibrary.Instance.GetDefinition(player.raceID.value);
                    var classDef = Game.Shared.Data.ClassDefinitionLibrary.Instance.GetDefinition(player.classID.value);

                    _actorInfoText.text = $"Level {player.level.value} {raceDef.DisplayName} {classDef.DisplayName}";
                }
            }
            else
            {
                _actorNameText.text = string.Empty;
                _actorInfoText.text = string.Empty;
            }
        }
    }
}
