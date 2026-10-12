using Game.Shared.Data;
using Game.Shared.Data.Dialogue;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DialogueOptionList</c> implementation.
    /// </summary>
    public sealed class UI_DialogueOptionList : UI_Panel, IListPanel
    {
        private static UI_DialogueOptionList _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_DialogueOptionList Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        private DialogueNode _currentNode;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_DialogueOptionEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
            _initialized = true;
        }

        /// <summary>
        /// Sets the value managed by <c>SetNode</c>.
        /// </summary>
        public void SetNode(int nodeID)
        {
            _currentNode = UI_DialogueWindow.Instance.CurrentDialogue.GetNode(nodeID);
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            PopulateList(true);
            base.Refresh();
        }

        private void ClearEntries()
        {
            foreach (var entry in currentEntries)
            {
                Destroy(entry.gameObject);
            }
            currentEntries.Clear();
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ClearEntries();

            int index = 0;
            foreach (DialogueChoice choice in _currentNode.Choices)
            {
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_DialogueOptionEntry>();
                entry.Initialize(choice, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
