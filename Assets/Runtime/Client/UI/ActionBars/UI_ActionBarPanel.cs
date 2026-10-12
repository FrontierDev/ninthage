using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ActionBarPanel</c> implementation.
    /// </summary>
    public sealed class UI_ActionBarPanel : UI_Panel
    {
        [SerializeField] private int actionBarIndex;
        private UI_ActionBarButton[] _buttons;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(int index)
        {
            actionBarIndex = index;
            panelName = $"ActionBar{index}";
            _buttons = GetComponentsInChildren<UI_ActionBarButton>(true);

            var actions = InputSystem.actions;
            for (int i = 0; i < _buttons.Length; i++)
            {
                string actionName = $"ActionBar{index + 1}_{i + 1}";
                var action = actions.FindAction(actionName);

                if (action != null)
                {
                    string bindingText = action.GetBindingDisplayString();
                    _buttons[i].SetKeybindText(bindingText);
                }
            }
        }

        /// <summary>
        /// Executes the client-side <c>ActivateSlot</c> operation.
        /// </summary>
        public void ActivateSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _buttons.Length)
                _buttons[slotIndex].OnClick(new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left });
        }
    }
}