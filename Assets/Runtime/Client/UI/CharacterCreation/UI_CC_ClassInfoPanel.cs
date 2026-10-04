using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using TMPro;
using UnityEngine.UI;
using Game.Shared.Data;

namespace Game.Client.UI
{
    public class UI_CC_ClassInfoPanel : UI_Panel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI classNameText;
        [SerializeField] private TextMeshProUGUI classDescriptionText;
        [SerializeField] private Image classIconImage;

        private void Start()
        {
            UI_CharacterCreationWindow.Instance.onClassSelected += OnClassSelected;
        }

        private void OnClassSelected(ClassDefinition def)
        {
            if (def == null)
            {
                Hide();
                return;
            }

            Show();
            classNameText.text = def.DisplayName;
            classDescriptionText.text = def.Description;
            classIconImage.sprite = def.Icon;
            Refresh();
        }
    }
}