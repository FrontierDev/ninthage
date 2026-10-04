using Game.Shared;
using Game.Shared.Networking;
using PurrNet;
using UnityEngine;

namespace Game.Client
{
    [RequireComponent(typeof(CharacterController))]
    public class Test_CharacterController : MonoBehaviour
    {
        private static Test_CharacterController _instance;
        public static Test_CharacterController Instance => _instance;

        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpPower = 4f;

        private ActorMotor actorMotor;
        private CharacterController _cc;

        private Vector2 _currentMovementInput = Vector2.zero;
        private bool _jumpRequested = false;

        private void Awake()
        {
            enabled = false;
            ActorService.onActorOwnershipTaken += OnActorOwnershipTaken;

            if (ClientInputController.Instance != null)
            {
                ClientInputController.Instance.onMovementInput += OnMovementInput;
                ClientInputController.Instance.onJumpInput += OnJumpInput;
                ClientInputController.Instance.onInteractInput += OnInteractInput;
            }
        }

        private void OnActorOwnershipTaken(Game.Shared.Actor actor)
        {
            if (actor.gameObject != gameObject) return;

            actorMotor = actor.GetComponent<ActorMotor>();
            if (actorMotor == null) return;

            _cc = GetComponent<CharacterController>();
            _instance = this;
            MovementService.onServerCorrection += OnServerCorrection;
            enabled = true;
        }

        private void OnDestroy()
        {
            ActorService.onActorOwnershipTaken -= OnActorOwnershipTaken;
            MovementService.onServerCorrection -= OnServerCorrection;

            if (ClientInputController.Instance != null)
            {
                ClientInputController.Instance.onMovementInput -= OnMovementInput;
                ClientInputController.Instance.onJumpInput -= OnJumpInput;
                ClientInputController.Instance.onInteractInput -= OnInteractInput;
            }
        }

        private void Update()
        {
            var config = GameConfigurationManager.Config;

            bool jump = _jumpRequested && _cc.isGrounded;
            if (jump)
            {
                actorMotor.verticalVelocity = jumpPower;
                _jumpRequested = false;
            }

            float yaw = transform.eulerAngles.y;
            float dt = Time.deltaTime;

            actorMotor.verticalVelocity = MovementSimulation.ApplyGravity(
                actorMotor.verticalVelocity, config.Gravity, config.MaxFallSpeed,
                dt, _cc.isGrounded);

            var delta = MovementSimulation.ComputeMoveDelta(
                _currentMovementInput, yaw, moveSpeed,
                actorMotor.verticalVelocity, dt, _cc.isGrounded,
                out var hVel);

            if (!_cc.isGrounded)
                delta = actorMotor.horizontalVelocity * dt
                      + new Vector3(0f, actorMotor.verticalVelocity * dt, 0f);
            else
                actorMotor.horizontalVelocity = hVel;

            _cc.Move(delta);

            actorMotor.isGrounded = _cc.isGrounded;
            if (_cc.isGrounded && actorMotor.verticalVelocity < 0)
                actorMotor.verticalVelocity = 0;

            var animator = GetComponent<NetworkAnimator>();
            if (animator != null)
            {
                animator.SetInteger("Forward", (int)_currentMovementInput.y);
                animator.SetInteger("Right", (int)_currentMovementInput.x);
            }
        }

        /// <summary>
        /// Called only when the server detects an illegal position (speed hack,
        /// collision penetration, etc.) and forces the client back.
        /// </summary>
        private void OnServerCorrection(Vector2Int chunk, Vector3 localPosition)
        {
            var correctedPos = ClientPositionManager.Instance.NetworkToLocal(chunk, localPosition);
            _cc.enabled = false;
            transform.position = correctedPos;
            _cc.enabled = true;
            actorMotor.verticalVelocity = 0f;
            actorMotor.horizontalVelocity = Vector3.zero;
            actorMotor.isGrounded = true;
        }

        private void OnMovementInput(Vector2 direction) => _currentMovementInput = direction;
        private void OnJumpInput() => _jumpRequested = true;
        private void OnInteractInput() { /* TODO */ }
    }
}
