using Game.Shared.Data;
using Game.Shared.Data.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DialogueBodyPanel</c> implementation.
    /// </summary>
    public sealed class UI_DialogueBodyPanel : UI_Panel
    {
        private static UI_DialogueBodyPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_DialogueBodyPanel Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private UI_DialogueOptionList optionList;

        /// <summary>
        /// Gets the <c>CurrentNodeID</c> value.
        /// </summary>
        public int CurrentNodeID { get; private set; }

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
            _initialized = true;
        }

        /// <summary>
        /// Executes the client-side <c>ShowDialogue</c> operation.
        /// </summary>
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

        /// <summary>
        /// Executes the client-side <c>GoToNode</c> operation.
        /// </summary>
        public void GoToNode(int nodeID)
        {
            CurrentNodeID = nodeID;
            ShowDialogue(UI_DialogueWindow.Instance.CurrentDialogue, nodeID);
        }
    }
}
