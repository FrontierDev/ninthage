using System.Collections.Generic;
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
            if (forceClear) ((IListPanel)this).ClearList();

            if (Game.Client.ClientAccountManager.TryGetCharacterList(out List<Game.Shared.Persistence.CharacterData> characters))
            {
                int index = 0;
                foreach (var character in characters)
                {
                    var entryObj = Instantiate(_entryPrefab, _contentContainer.transform);
                    if (entryObj.TryGetComponent(out UI_CharacterListEntry entry))
                    {
                        entry.Initialize(character, index);
                        currentEntries.Add(entry);
                        index++;
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer.GetComponent<RectTransform>());
        }
    }
}
