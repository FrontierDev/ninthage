using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_CharacterListPanel : UI_Panel, IListPanel
    {
        private static UI_CharacterListPanel _instance;
        public static UI_CharacterListPanel Instance => _instance;

        [Header("Character List Settings")]
        [SerializeField] private GameObject contentContainer;
        [SerializeField] private GameObject entryPrefab;
        GameObject IListPanel.contentContainer => contentContainer;
        GameObject IListPanel.entryPrefab => entryPrefab;

        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        public override void Refresh()
        {
            PopulateList(true);
        }

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ((IListPanel)this).ClearList();

            if (Game.Client.ClientAccountManager.TryGetCharacterList(out List<Game.Shared.Persistence.CharacterData> characters))
            {
                int index = 0;
                foreach (var character in characters)
                {
                    var entryObj = Instantiate(entryPrefab, contentContainer.transform);
                    if (entryObj.TryGetComponent(out UI_CharacterListEntry entry))
                    {
                        entry.Initialize(character, index);
                        currentEntries.Add(entry);
                        index++;
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
        }
    }
}