using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_RaceInfoPanel</c> implementation.
    /// </summary>
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
