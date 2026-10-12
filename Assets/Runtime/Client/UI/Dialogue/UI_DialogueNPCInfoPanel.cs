using Game.Shared;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DialogueNPCInfoPanel</c> implementation.
    /// </summary>
    public sealed class UI_DialogueNPCInfoPanel : UI_Panel
    {
        private static UI_DialogueNPCInfoPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_DialogueNPCInfoPanel Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
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

        /// <summary>
        /// Executes the client-side <c>UpdateNPC</c> operation.
        /// </summary>
        public void UpdateNPC(Actor npc)
        {
            npcNameText.text = npc.Name;
            npcSubtext.text = "";
            Refresh();
        }
    }
}
