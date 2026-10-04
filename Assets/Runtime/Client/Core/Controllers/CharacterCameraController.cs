using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client
{
    public sealed class CharacterCameraController : MonoBehaviour
    {
        private static CharacterCameraController _instance;
        public static CharacterCameraController Instance => _instance;

        [SerializeField] private CinemachineInputAxisController cameraInputController;
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

        [Header("Right-click character rotation")]
        [SerializeField] private float characterRotationSpeed = 0.2f;

        private bool _leftHeld;
        private bool _rightHeld;
        private bool _movementInput;

        private InputAction _lookAction;

        private void Awake()
        {
            if (cameraInputController == null)
                cameraInputController = GetComponent<CinemachineInputAxisController>();
            if (orbitalFollow == null)
                orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

            if (cameraInputController == null)
            {
                Debug.Error("No CinemachineInputAxisController assigned.");
                enabled = false;
                return;
            }

            // Intercept Cinemachine's input reading so we can gate axes per mouse button.
            cameraInputController.ReadControlValueOverride = FilterInput;

            // Enable horizontal recentering so Cinemachine dynamically updates
            // HorizontalAxis.Center to the angle behind the tracking target.
            // Without this, Center stays at its initial value and never tracks the character.
            if (orbitalFollow != null)
            {
                var h = orbitalFollow.HorizontalAxis;
                h.Recentering.Enabled = true;
                h.Recentering.Wait = 1f;
                h.Recentering.Time = 0.5f;
                orbitalFollow.HorizontalAxis = h;
            }
        }

        private void OnDestroy()
        {
            if (cameraInputController != null)
                cameraInputController.ReadControlValueOverride = null;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (pointerOverUI) return;

            _leftHeld = mouse.leftButton.isPressed;
            _rightHeld = mouse.rightButton.isPressed;

            // Check for WASD movement input.
            var kb = Keyboard.current;
            _movementInput = kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);

            // RMB: rotate character yaw with mouse X delta;
            // snap camera yaw behind the character so it stays directly behind.
            if (_rightHeld)
            {
                var character = Test_CharacterController.Instance;
                if (character != null)
                {
                    float deltaX = mouse.delta.ReadValue().x;
                    character.transform.Rotate(0f, deltaX * characterRotationSpeed, 0f, Space.World);
                }

                // Force camera horizontal axis to its center (which Cinemachine keeps
                // aligned behind the tracking target) so the camera follows the character.
                if (orbitalFollow != null)
                    orbitalFollow.HorizontalAxis.Value = orbitalFollow.HorizontalAxis.Center;
            }

            // WASD movement: smoothly recenter camera behind the character.
            // Cancel the recentering when keys are released so it doesn't keep drifting.
            if (_movementInput && !_leftHeld && orbitalFollow != null)
                orbitalFollow.HorizontalAxis.TriggerRecentering();
            else if (!_movementInput && !_leftHeld && !_rightHeld && orbitalFollow != null)
                orbitalFollow.HorizontalAxis.CancelRecentering();
        }

        private float FilterInput(
            InputAction action,
            IInputAxisOwner.AxisDescriptor.Hints hint,
            Object context,
            CinemachineInputAxisController.Reader.ControlValueReader defaultReader)
        {
            // Cache the Look action on first call so we can distinguish it from the zoom action.
            _lookAction ??= action;

            // Any action that is NOT the Look action (i.e. zoom/scroll) always passes through.
            if (action != _lookAction)
                return defaultReader(action, hint, context, null);

            bool wantHorizontal = _leftHeld;               // LMB → camera yaw
            bool wantVertical = _leftHeld || _rightHeld;    // LMB or RMB → camera pitch

            if (hint == IInputAxisOwner.AxisDescriptor.Hints.X && !wantHorizontal)
                return 0f;
            if (hint == IInputAxisOwner.AxisDescriptor.Hints.Y && !wantVertical)
                return 0f;

            return defaultReader(action, hint, context, null);
        }
    }
}