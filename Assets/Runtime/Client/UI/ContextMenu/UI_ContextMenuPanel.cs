using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ContextMenuPanel</c> implementation.
    /// </summary>
    public sealed class UI_ContextMenuPanel : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject _contentContainer;

        private GameObject _entryPrefab;

        GameObject IListPanel.contentContainer => _contentContainer;
        GameObject IListPanel.entryPrefab => _entryPrefab;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();
        /// <summary>
        /// Gets the <c>ContentRoot</c> value.
        /// </summary>
        public Transform ContentRoot => _contentContainer.transform;

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
            {
                ((IListPanel)this).ClearList();
                currentEntries.Clear();
            }
        }

        /// <summary>
        /// Executes the client-side <c>Populate</c> operation.
        /// </summary>
        public void Populate(GameObject entryPrefab, Action<UI_ContextMenuPanel> populateAction)
        {
            PopulateList(true);
            _entryPrefab = entryPrefab;
            populateAction(this);
            Refresh();
        }
    }
}
