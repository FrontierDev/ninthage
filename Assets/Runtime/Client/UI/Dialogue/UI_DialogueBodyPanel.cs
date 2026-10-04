using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Shared.Data;

namespace Game.Client.UI
{
    public sealed class UI_DialogueBodyPanel : UI_Panel
    {
        private static UI_DialogueBodyPanel _instance;
        public static UI_DialogueBodyPanel Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private UI_DialogueOptionList optionList;

        public int CurrentNodeID { get; private set; }

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
            _initialized = true;
        }

        public void ShowDialogue(DialogueDefinition dialogue, int nodeID = 0)
        {
            CurrentNodeID = nodeID;

            var node = dialogue.GetNode(nodeID);
            dialogueText.text = node != null ? node.DialogueText : $"[Missing dialogue text for node ID {nodeID}]";

            if (node.Choices.Count > 0)
            {
                optionList.gameObject.SetActive(true);
                optionList.SetNode(nodeID);
                optionList.Refresh();
            }
            else
            {
                optionList.gameObject.SetActive(false);
            }

            Refresh();
        }

        public void GoToNode(int nodeID)
        {
            CurrentNodeID = nodeID;
            ShowDialogue(UI_DialogueWindow.Instance.CurrentDialogue, nodeID);
        }
    }
}