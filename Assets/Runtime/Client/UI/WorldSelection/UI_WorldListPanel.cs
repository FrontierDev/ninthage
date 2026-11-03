using System;
using System.Collections.Generic;
using Game.Client;
using Game.Client.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.WorldSelection
{
    /// <summary>Displays the filtered World rows supplied by World Selection.</summary>
    public sealed class UI_WorldListPanel : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject _contentContainer;
        [SerializeField] private GameObject _entryPrefab;

        GameObject IListPanel.contentContainer => _contentContainer;
        GameObject IListPanel.entryPrefab => _entryPrefab;

        /// <summary>Gets the list entries currently displayed by the panel.</summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private List<ClientMasterSelectionProtocol.WorldSelectionWorld> _currentRows = new();

        /// <summary>Replaces the visible rows with the supplied filtered view.</summary>
        internal void PopulateRows(
            IReadOnlyList<ClientMasterSelectionProtocol.WorldSelectionWorld> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            _currentRows = new List<ClientMasterSelectionProtocol.WorldSelectionWorld>(rows);
            PopulateList(true);
        }

        /// <summary>Clears the current rows and their instantiated entries.</summary>
        internal void ClearRows()
        {
            ClearEntries();
            _currentRows.Clear();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            PopulateList(true);
        }

        /// <inheritdoc/>
        public void PopulateList(bool forceClear = false)
        {
            if (_contentContainer == null || _entryPrefab == null)
                throw new InvalidOperationException(
                    "UI_WorldListPanel requires content-container and entry-prefab references.");

            ClearEntries();
            foreach (ClientMasterSelectionProtocol.WorldSelectionWorld world in _currentRows)
            {
                GameObject entryObject = Instantiate(_entryPrefab, _contentContainer.transform);
                if (!entryObject.TryGetComponent(out UI_WorldListEntry entry))
                {
                    Destroy(entryObject);
                    throw new InvalidOperationException(
                        "World List entry prefab does not contain a UI_WorldListEntry component.");
                }

                entry.Initialize(world, currentEntries.Count);
                entry.Register(this);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _contentContainer.GetComponent<RectTransform>());
        }

        private void ClearEntries()
        {
            foreach (UI_ListEntry entry in currentEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }

            currentEntries.Clear();
        }
    }
}
