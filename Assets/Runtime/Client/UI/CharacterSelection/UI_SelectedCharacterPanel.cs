using Game.Shared.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("enterWorldButton")]
        [SerializeField] private UI_Button _enterWorldButton;
        [FormerlySerializedAs("characterNameText")]
        [SerializeField] private TMP_Text _characterNameText;
        [FormerlySerializedAs("characterInfoText")]
        [SerializeField] private TMP_Text _characterInfoText;

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
            _characterNameText.text = characterData.Name;
            _characterInfoText.text = $"Level {characterData.Level} {characterData.RaceID} {characterData.ClassID}";
        }
    }
}
