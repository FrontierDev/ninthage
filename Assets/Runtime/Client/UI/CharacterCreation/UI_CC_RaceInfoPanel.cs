using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_RaceInfoPanel</c> implementation.
    /// </summary>
    public class UI_CC_RaceInfoPanel : UI_Panel
    {
        [Header("UI References")]
        [FormerlySerializedAs("raceNameText")]
        [SerializeField] private TextMeshProUGUI _raceNameText;
        [FormerlySerializedAs("raceDescriptionText")]
        [SerializeField] private TextMeshProUGUI _raceDescriptionText;
        [FormerlySerializedAs("raceIconImage")]
        [SerializeField] private Image _raceIconImage;

        private void Start()
        {
            UI_CharacterCreationWindow.Instance.onRaceSelected += OnRaceSelected;
        }

        private void OnRaceSelected(RaceDefinition def)
        {
            if (def == null)
            {
                Hide();
                return;
            }

            Show();
            _raceNameText.text = def.DisplayName;
            _raceDescriptionText.text = def.Description;
            _raceIconImage.sprite = def.Icon;
            Refresh();
        }
    }
}
