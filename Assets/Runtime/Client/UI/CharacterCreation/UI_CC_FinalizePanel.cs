using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_FinalizePanel</c> implementation.
    /// </summary>
    public sealed class UI_CC_FinalizePanel : UI_Panel
    {
        private static UI_CC_FinalizePanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
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
        [FormerlySerializedAs("createButton")]
        [SerializeField] private UI_Button _createButton;
        [FormerlySerializedAs("nameInput")]
        [SerializeField] private TMP_InputField _nameInput;

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            UI_CC_NextStageButton.Instance.SetCurrentPanel("FinalizeCharacter");
            UI_CC_NextStageButton.Instance.Hide();

        }

        /// <summary>
        /// Gets the value provided by <c>GetCharacterName</c>.
        /// </summary>
        public string GetCharacterName()
        {
            return _nameInput.text;
        }
    }
}
