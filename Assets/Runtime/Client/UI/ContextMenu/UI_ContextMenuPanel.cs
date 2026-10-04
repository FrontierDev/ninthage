using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_ContextMenuPanel : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject _contentContainer;

        private GameObject _entryPrefab;

        GameObject IListPanel.contentContainer => _contentContainer;
        GameObject IListPanel.entryPrefab => _entryPrefab;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();
        public Transform ContentRoot => _contentContainer.transform;

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
            {
                ((IListPanel)this).ClearList();
                currentEntries.Clear();
            }
        }

        public void Populate(GameObject entryPrefab, Action<UI_ContextMenuPanel> populateAction)
        {
            PopulateList(true);
            _entryPrefab = entryPrefab;
            populateAction(this);
            Refresh();
        }
    }
}