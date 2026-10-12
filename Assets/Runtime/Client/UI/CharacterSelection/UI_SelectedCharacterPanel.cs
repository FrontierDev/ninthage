using Game.Shared.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_SelectedCharacterPanel</c> implementation.
    /// </summary>
    public sealed class UI_SelectedCharacterPanel : UI_Panel
    {
        private static UI_SelectedCharacterPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_SelectedCharacterPanel Instance => _instance;

        [Header("UI References - Character Info")]
        [SerializeField] private UI_Button enterWorldButton;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text characterInfoText;

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            base.Awake();
        }

        private void Start()
        {
            UI_CharacterSelectWindow.Instance.onCharacterSelected += SetCharacterInfo;
        }

        private void SetCharacterInfo(CharacterData characterData)
        {
            if (characterData == null)
            {
                HideImmediate();
                return;
            }

            Show();
            characterNameText.text = characterData.Name;
            characterInfoText.text = $"Level {characterData.Level} {characterData.RaceID} {characterData.ClassID}";
        }
    }
}