using Game.Shared;
using Game.Shared.Data;

namespace Game.Client.UI
{
    public class UI_DialogueWindow : UI_Window
    {
        private static UI_DialogueWindow _instance;
        public static UI_DialogueWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        public DialogueDefinition CurrentDialogue { get; private set; }

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }

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