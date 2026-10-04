using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Shared;

namespace Game.Client.UI
{
    public sealed class UI_DialogueNPCInfoPanel : UI_Panel
    {
        private static UI_DialogueNPCInfoPanel _instance;
        public static UI_DialogueNPCInfoPanel Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text npcSubtext;
        [SerializeField] private Image npcPortrait;

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
            _initialized = true;
        }

        public void UpdateNPC(Actor npc)
        {
            npcNameText.text = npc.Name;
            npcSubtext.text = "";
            Refresh();
        }
    }
}