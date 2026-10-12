using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_ClassInfoPanel</c> implementation.
    /// </summary>
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
