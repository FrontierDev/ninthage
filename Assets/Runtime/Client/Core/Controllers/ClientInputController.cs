using System;
using Game.Client.UI;
using Game.Shared;
using Game.Shared.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client
{
    public enum InputMode
    {
        GameplayMovement,
        UINavigation,
        TextInput,
        Disabled
    }

    public class ClientInputController : MonoBehaviour
    {
        private static ClientInputController _instance;
        public static ClientInputController Instance => _instance;

        private InputMode _currentMode = InputMode.Disabled;
        public InputMode CurrentMode => _currentMode;

        public event System.Action<InputMode> onInputModeChanged;
        public event System.Action<Vector2> onMovementInput;
        public event System.Action onJumpInput;
        public event System.Action onInteractInput;
        public event System.Action onCancelInput;
        public event System.Action<Game.Shared.IInteractable> onHoverInteractable;
        public event System.Action<int, int> onActionBarInput;
        public event System.Action<string> onInterfaceHotkeyInput;

        private InputActionMap _gameplayMap;
        private InputActionMap _uiNavMap;
        private InputActionMap _interfaceMap;
        private InputAction _moveAction;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            var actions = InputSystem.actions;

            _gameplayMap = actions.FindActionMap("Gameplay");
            _uiNavMap = actions.FindActionMap("UINav");
            _interfaceMap = actions.FindActionMap("Interface");

            _moveAction = _gameplayMap.FindAction("Move");
            _gameplayMap.FindAction("Jump").performed += ctx => onJumpInput?.Invoke();
            _gameplayMap.FindAction("Cancel").performed += ctx => onCancelInput?.Invoke();

            // Bar 0: ActionBar1-ActionBar12 (unmodified keys)
            for (int i = 1; i <= 12; i++)
            {
                int slot = i - 1;
                _gameplayMap.FindAction($"ActionBar1_{i}").performed += ctx => onActionBarInput?.Invoke(0, slot);
            }

            // Interface Hotkeys
            _interfaceMap.FindAction("Inventory").performed += ctx => onInterfaceHotkeyInput?.Invoke("InventoryWindow");
            _interfaceMap.FindAction("Character").performed += ctx => onInterfaceHotkeyInput?.Invoke("CharacterWindow");
            _interfaceMap.FindAction("Talents").performed += ctx => onInterfaceHotkeyInput?.Invoke("TalentWindow");
            _interfaceMap.FindAction("Reputation").performed += ctx => onInterfaceHotkeyInput?.Invoke("ReputationWindow");
            _interfaceMap.FindAction("Quests").performed += ctx => onInterfaceHotkeyInput?.Invoke("QuestLogWindow");

            _gameplayMap.Disable();
            _uiNavMap.Disable();
            _interfaceMap.Disable();

            // Start in disabled mode until the player enters the world.
            ActorService.onPlayerActorAssigned += (playerActor) => SetInputMode(InputMode.GameplayMovement);
            ActorService.onPlayerActorAssigned += (playerActor) => UI_Manager.Instance.Initialize();
        }

        private void OnDestroy()
        {
            _gameplayMap?.Disable();
            _uiNavMap?.Disable();
        }

        public void SetInputMode(InputMode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;

            _gameplayMap.Disable();
            _uiNavMap.Disable();

            switch (mode)
            {
                case InputMode.GameplayMovement:
                    _gameplayMap.Enable();
                    _interfaceMap.Enable();
                    break;
                case InputMode.UINavigation:
                    _uiNavMap.Enable();
                    _interfaceMap.Enable();
                    break;
            }

            if (mode != InputMode.GameplayMovement)
                onMovementInput?.Invoke(Vector2.zero);

            onInputModeChanged?.Invoke(mode);
        }

        private void Update()
        {
            if (_currentMode != InputMode.GameplayMovement) return;

            onMovementInput?.Invoke(_moveAction.ReadValue<Vector2>());

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (pointerOverUI) return;

            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            Game.Shared.IInteractable interactable = null;
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                interactable = hit.collider.GetComponent<Game.Shared.IInteractable>();
                if (interactable != null)
                    interactable.OnPointerEnter(ClientAccountManager.PlayerActor);
                else
                    ActorService.onClientHoveredActorChanged?.Invoke(null);
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (interactable != null)
                    interactable.OnPointerClick(ClientAccountManager.PlayerActor);
                else
                    ActorService.onClientTargetedActorChanged?.Invoke(null);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                var playerActor = ClientAccountManager.PlayerActor;
                if (interactable != null)
                {
                    interactable.OnPointerClick(playerActor);

                    if (interactable is Actor targetActor)
                    {
                        var relation = targetActor.GetFactionRelation(playerActor.GetComponent<PlayerReputation>());
                        if (relation == FactionRelationState.Hostile || relation == FactionRelationState.Neutral)
                            ClientCombatManager.Instance.StartAutoAttack();
                        else
                            UI_DialogueWindow.Instance.StartDialogue(targetActor);
                    }

                }
                else if (playerActor.Target != null)
                {
                    ClientCombatManager.Instance.StopAutoAttack();
                    UI_DialogueWindow.Instance.Hide();
                }
            }
        }
    }
}