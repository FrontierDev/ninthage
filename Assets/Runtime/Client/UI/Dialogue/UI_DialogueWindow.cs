using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Data.Dialogue;
using Game.Shared.Runtime.Actors;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DialogueWindow</c> implementation.
    /// </summary>
    public class UI_DialogueWindow : UI_Window
    {
        private static UI_DialogueWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_DialogueWindow Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Gets the <c>CurrentDialogue</c> value.
        /// </summary>
        public DialogueDefinition CurrentDialogue { get; private set; }

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }

        /// <summary>
        /// Executes the client-side <c>StartDialogue</c> operation.
        /// </summary>
        public void StartDialogue(Actor npc)
        {
            if (npc.TryGetComponent<NPCBehavior>(out var npcBehavior))
            {
                CurrentDialogue = npcBehavior.Dialogue;

                if (CurrentDialogue != null)
                {
                    UI_DialogueNPCInfoPanel.Instance.UpdateNPC(npc);
                    UI_DialogueBodyPanel.Instance.ShowDialogue(CurrentDialogue);
                    Show();
                }
            }
        }
    }
}
