using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_ClassInfoPanel</c> implementation.
    /// </summary>
    public class UI_CC_ClassInfoPanel : UI_Panel
    {
        [Header("UI References")]
        [FormerlySerializedAs("classNameText")]
        [SerializeField] private TextMeshProUGUI _classNameText;
        [FormerlySerializedAs("classDescriptionText")]
        [SerializeField] private TextMeshProUGUI _classDescriptionText;
        [FormerlySerializedAs("classIconImage")]
        [SerializeField] private Image _classIconImage;

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
            _classNameText.text = def.DisplayName;
            _classDescriptionText.text = def.Description;
            _classIconImage.sprite = def.Icon;
            Refresh();
        }
    }
}
