using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterResourceBar</c> implementation.
    /// </summary>
    public class UI_CharacterResourceBar : UI_ProgressBar
    {
        [FormerlySerializedAs("playerStatContainer")]
        [SerializeField] private ActorStatContainer _playerStatContainer;
        [FormerlySerializedAs("valueText")]
        [SerializeField] private TMP_Text _valueText;
        [FormerlySerializedAs("percentageText")]
        [SerializeField] private TMP_Text _percentageText;
        [FormerlySerializedAs("trackedResources")]
        [SerializeField] private List<ActorStatDefinition> _trackedResources = new();

        private void Awake()
        {
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            SetMaterial(graphics.material);
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            if (playerActor != null)
            {
                _playerStatContainer = playerActor.GetComponent<ActorStatContainer>();
                if (_playerStatContainer != null)
                {
                    StatEndpoint.onStatsUpdated += OnStatsUpdated;
                    Refresh();
                }
            }
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            if (actor == ClientAccountManager.PlayerActor)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_playerStatContainer == null) return;

            // For now we just display the first resource stat we find, if any.
            foreach (var def in _trackedResources)
            {
                if (_playerStatContainer.TryGetStat(def.DefinitionID, out var stat))
                {
                    var currentValue = stat.CurrentValue;
                    var maxValue = stat.EffectiveMaximum;

                    SetProgress(currentValue / maxValue);
                    if (_valueText != null)
                        _valueText.text = $"{currentValue:0}";

                    if (_percentageText != null)
                        _percentageText.text = $"{(maxValue > 0 ? (currentValue / maxValue) * 100 : 0):0}%";

                    return;
                }
            }
        }
    }
}
