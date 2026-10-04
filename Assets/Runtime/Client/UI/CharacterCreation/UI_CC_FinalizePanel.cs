using UnityEngine;
using TMPro;

namespace Game.Client.UI
{
    public sealed class UI_CC_FinalizePanel : UI_Panel
    {
        private static UI_CC_FinalizePanel _instance;
        public static UI_CC_FinalizePanel Instance => _instance;

        protected override void Awake()
        {
            base.Awake();
            if (_instance != null)
            {
                Debug.LogError("Multiple instances of UI_CC_FinalizePanel detected! This should never happen.");
                Destroy(this);
                return;
            }
            _instance = this;
        }

        [Header("UI References - Finalize Character")]
        [SerializeField] private UI_Button createButton;
        [SerializeField] private TMP_InputField nameInput;

        public override void Show()
        {
            base.Show();
            UI_CC_NextStageButton.Instance.SetCurrentPanel("FinalizeCharacter");
            UI_CC_NextStageButton.Instance.Hide();

        }

        public string GetCharacterName()
        {
            return nameInput.text;
        }
    }
}