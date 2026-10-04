using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using TMPro;
using UnityEngine.UI;
using Game.Shared.Data;

namespace Game.Client.UI
{
    public class UI_CC_RaceInfoPanel : UI_Panel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI raceNameText;
        [SerializeField] private TextMeshProUGUI raceDescriptionText;
        [SerializeField] private Image raceIconImage;

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
            raceNameText.text = def.DisplayName;
            raceDescriptionText.text = def.Description;
            raceIconImage.sprite = def.Icon;
            Refresh();
        }
    }
}