using Game.Shared;
using Game.Shared.Movement;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>Test_CharacterController</c> implementation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Test_CharacterController : MonoBehaviour
    {
        private static Test_CharacterController _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static Test_CharacterController Instance => _instance;

        [FormerlySerializedAs("moveSpeed")]
        [SerializeField] private float _moveSpeed = 5f;
        [FormerlySerializedAs("jumpPower")]
        [SerializeField] private float _jumpPower = 4f;

        private ActorMotor _actorMotor;
        private CharacterController _cc;

        private Vector2 _currentMovementInput = Vector2.zero;
        private bool _jumpRequested = false;
        private long _ownershipEpoch;
        private long _proposalSequence;

        private void Awake()
        {
            enabled = false;
            ActorEndpoint.onActorOwnershipTaken += OnActorOwnershipTaken;

            if (ClientInputController.Instance != null)
            {
                ClientInputController.Instance.onMovementInput += OnMovementInput;
                ClientInputController.Instance.onJumpInput += OnJumpInput;
                ClientInputController.Instance.onInteractInput += OnInteractInput;
            }
        }

        private void OnActorOwnershipTaken(Game.Shared.Runtime.Actors.Actor actor)
        {
            if (actor.gameObject != gameObject) return;

            _actorMotor = actor.GetComponent<ActorMotor>();
            if (_actorMotor == null) return;

            _ownershipEpoch = checked(_ownershipEpoch + 1);
            _proposalSequence = 0;
            _cc = GetComponent<CharacterController>();
            _instance = this;
            MovementEndpoint.onServerCorrection += OnServerCorrection;
            enabled = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onActorOwnershipTaken -= OnActorOwnershipTaken;
            MovementEndpoint.onServerCorrection -= OnServerCorrection;

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
                _actorMotor.verticalVelocity = _jumpPower;
                _jumpRequested = false;
            }

            float yaw = transform.eulerAngles.y;
            float dt = Time.deltaTime;

            _actorMotor.verticalVelocity = MovementSimulation.ApplyGravity(
                _actorMotor.verticalVelocity, config.Gravity, config.MaxFallSpeed,
                dt, _cc.isGrounded);

            var delta = MovementSimulation.ComputeMoveDelta(
                _currentMovementInput, yaw, _moveSpeed,
                _actorMotor.verticalVelocity, dt, _cc.isGrounded,
                out var hVel);

            if (!_cc.isGrounded)
                delta = _actorMotor.horizontalVelocity * dt
                      + new Vector3(0f, _actorMotor.verticalVelocity * dt, 0f);
            else
                _actorMotor.horizontalVelocity = hVel;

            _cc.Move(delta);
            SubmitPositionProposal();

            _actorMotor.isGrounded = _cc.isGrounded;
            if (_cc.isGrounded && _actorMotor.verticalVelocity < 0)
                _actorMotor.verticalVelocity = 0;

            var animator = GetComponent<NetworkAnimator>();
            if (animator != null)
            {
                animator.SetInteger("Forward", (int)_currentMovementInput.y);
                animator.SetInteger("Right", (int)_currentMovementInput.x);
            }
        }

        private void SubmitPositionProposal()
        {
            if (!ClientPositionManager.Initialized || ClientPositionManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Cannot submit a movement proposal before ClientPositionManager is initialized.");
            }

            var (chunk, localPosition) = ClientPositionManager.Instance.LocalToChunkRelative(transform.position);
            var position = WorldPosition.FromChunkLocal(chunk, localPosition);
            var proposalSequence = checked(++_proposalSequence);

            MovementEndpoint.Server_SubmitPositionProposal(
                position,
                _ownershipEpoch,
                proposalSequence);
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
            _actorMotor.verticalVelocity = 0f;
            _actorMotor.horizontalVelocity = Vector3.zero;
            _actorMotor.isGrounded = true;
        }

        private void OnMovementInput(Vector2 direction) => _currentMovementInput = direction;
        private void OnJumpInput() => _jumpRequested = true;
        private void OnInteractInput() { /* TODO */ }
    }
}
