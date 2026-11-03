using System.Collections.Generic;
using Game.Shared.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterListPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterListPanel : UI_Panel, IListPanel
    {
        private static UI_CharacterListPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterListPanel Instance => _instance;

        [Header("Character List Settings")]
        [FormerlySerializedAs("contentContainer")]
        [SerializeField] private GameObject _contentContainer;
        [FormerlySerializedAs("entryPrefab")]
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private TMP_Text _characterCountText;
        [SerializeField] private UI_Button _createCharacterButton;
        GameObject IListPanel.contentContainer => _contentContainer;
        GameObject IListPanel.entryPrefab => _entryPrefab;

        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        /// <inheritdoc/>
        public override void Refresh()
        {
            PopulateList(true);
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (!Game.Client.ClientAccountManager.TryGetCharacterList(
                    out List<CharacterData> characters))
            {
                characters = new List<CharacterData>();
            }

            PopulateList(characters);
        }

        /// <summary>Populates the panel with the supplied account roster in order.</summary>
        public void PopulateList(IReadOnlyList<CharacterData> characters)
        {
            ((IListPanel)this).ClearList();
            currentEntries.Clear();

            if (characters != null)
            {
                int index = 0;
                for (int characterIndex = 0; characterIndex < characters.Count; characterIndex++)
                {
                    CharacterData character = characters[characterIndex];
                    var entryObj = Instantiate(_entryPrefab, _contentContainer.transform);
                    if (entryObj.TryGetComponent(out UI_CharacterListEntry entry))
                    {
                        entry.Initialize(character, index);
                        currentEntries.Add(entry);
                        index++;
                    }
                }
            }

            if (_characterCountText != null)
                _characterCountText.text = $"{currentEntries.Count} / 12";

            if (_createCharacterButton != null)
            {
                if (currentEntries.Count >= 12)
                    _createCharacterButton.DisableButton();
                else
                    _createCharacterButton.EnableButton();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer.GetComponent<RectTransform>());
        }
    }
}
