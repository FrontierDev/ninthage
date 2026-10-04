using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_DialogueOptionList : UI_Panel, IListPanel
    {
        private static UI_DialogueOptionList _instance;
        public static UI_DialogueOptionList Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private DialogueNode currentNode;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_DialogueOptionEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
            _initialized = true;
        }

        public void SetNode(int nodeID)
        {
            currentNode = UI_DialogueWindow.Instance.CurrentDialogue.GetNode(nodeID);
        }

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

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear) ClearEntries();

            int index = 0;
            foreach (DialogueChoice choice in currentNode.Choices)
            {
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_DialogueOptionEntry>();
                entry.Initialize(choice, index);
                entry.Register(this);
                index++;
            }
        }
    }
}