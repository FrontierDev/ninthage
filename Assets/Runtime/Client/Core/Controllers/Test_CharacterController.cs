using Game.Shared;
using Debug = Game.Shared.Utility.FormattedDebug;
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
        // Match the server's 20 Hz cadence while leaving movement frame-responsive.
        private const float POSITION_PROPOSAL_INTERVAL_SECONDS = 0.05f;
        // A 1-second heartbeat keeps stationary owners observable without adding
        // meaningful traffic between ordinary movement proposals.
        private const float POSITION_HEARTBEAT_INTERVAL_SECONDS = 1f;
        // Tolerate up to 1 mm of float-origin round-trip error when suppressing sends.
        private const double POSITION_COMPARISON_TOLERANCE_METRES = 0.001d;

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
        private bool _hasSentPositionProposal;
        private float _lastPositionProposalTime;
        private WorldPosition _lastTransmittedPosition;
        private bool _instanceValidationTogglePending;

        private void Awake()
        {
            enabled = false;
            ActorEndpoint.onActorOwnershipTaken += OnActorOwnershipTaken;
            ActorEndpoint.onActorOwnerChanged += OnActorOwnerChanged;
            ActorEndpoint.onClientAcceptedPositionChanged += OnClientAcceptedPositionChanged;
            AccountEndpoint.onEnteringWorld += OnEnteringWorld;
            AccountEndpoint.onEnteredWorld += OnEnteredWorld;

            if (ClientInputController.Instance != null)
            {
                ClientInputController.Instance.onMovementInput += OnMovementInput;
                ClientInputController.Instance.onJumpInput += OnJumpInput;
                ClientInputController.Instance.onInteractInput += OnInteractInput;
                ClientInputController.Instance.onInstanceValidationToggleInput += OnInstanceValidationToggleInput;
            }
            ActorEndpoint.onClientInstanceValidationToggleResult += OnInstanceValidationToggleResult;
        }

        // Movement and proposal sequencing begin only after this object is the local actor owner
        // and the streamed client world is ready.
        private void OnActorOwnershipTaken(Game.Shared.Runtime.Actors.Actor actor)
        {
            TryAcquireLocalControl(actor);
        }

        private void OnClientAcceptedPositionChanged(
            Actor actor,
            ActorPositionSnapshot snapshot)
        {
            if (actor != null && actor.gameObject == gameObject)
                TryAcquireLocalControl(actor);
        }

        private void OnEnteringWorld(Game.Shared.Persistence.CharacterData characterData, string sceneName)
        {
            RevokeLocalControl();
        }

        private void OnEnteredWorld()
        {
            TryAcquireLocalControl(GetComponent<Actor>());
        }

        private void TryAcquireLocalControl(Actor actor)
        {
            if (actor == null
                || actor.gameObject != gameObject
                || NetworkManager.main == null
                || !actor.Owner.HasValue
                || actor.Owner.Value != NetworkManager.main.localPlayer
                || !ClientWorldManager.Initialized
                || !ClientWorldManager.Instance.IsWorldReady)
            {
                return;
            }

            if (_instance == this && _actorMotor != null && _cc != null)
            {
                enabled = true;
                return;
            }

            _actorMotor = actor.GetComponent<ActorMotor>();
            _cc = GetComponent<CharacterController>();
            if (_actorMotor == null || _cc == null)
            {
                enabled = false;
                _actorMotor = null;
                _cc = null;
                Debug.Error($"Cannot acquire local control for actor {actor.ID}: required movement components are missing.");
                return;
            }

            if (!ClientPositionManager.Initialized || ClientPositionManager.Instance == null)
            {
                enabled = false;
                return;
            }

            if (!actor.TryGetAcceptedPositionSnapshot(out ActorPositionSnapshot snapshot))
            {
                enabled = false;
                return;
            }

            Vector3 initialLocalPosition =
                ClientPositionManager.Instance.NetworkToLocal(
                    snapshot.Position.Chunk,
                    snapshot.Position.LocalPosition);
            bool characterControllerWasEnabled = _cc.enabled;
            _cc.enabled = false;
            try
            {
                transform.position = initialLocalPosition;
            }
            finally
            {
                _cc.enabled = characterControllerWasEnabled;
            }

            Physics.SyncTransforms();
            _actorMotor.verticalVelocity = 0f;
            _actorMotor.horizontalVelocity = Vector3.zero;
            _actorMotor.isGrounded = false;

            _ownershipEpoch = checked(_ownershipEpoch + 1);
            _proposalSequence = 0;
            _currentMovementInput = Vector2.zero;
            _jumpRequested = false;
            _instanceValidationTogglePending = false;
            ResetPositionProposalState();
            _instance = this;
            MovementEndpoint.onServerCorrection -= OnServerCorrection;
            MovementEndpoint.onServerCorrection += OnServerCorrection;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Movement/Ownership] actor={actor.ID} epoch={_ownershipEpoch} proposal state reset.");
#endif
            enabled = true;
        }

        private void OnActorOwnerChanged(Game.Shared.Runtime.Actors.Actor actor)
        {
            if (actor == null || actor.gameObject != gameObject)
                return;

            if (NetworkManager.main != null
                && actor.Owner.HasValue
                && actor.Owner.Value == NetworkManager.main.localPlayer)
            {
                return;
            }

            RevokeLocalControl();
        }

        private void RevokeLocalControl()
        {
            enabled = false;
            _currentMovementInput = Vector2.zero;
            _jumpRequested = false;
            _instanceValidationTogglePending = false;
            ResetPositionProposalState();
            MovementEndpoint.onServerCorrection -= OnServerCorrection;

            if (_instance == this)
                _instance = null;

            _actorMotor = null;
            _cc = null;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onActorOwnershipTaken -= OnActorOwnershipTaken;
            ActorEndpoint.onActorOwnerChanged -= OnActorOwnerChanged;
            ActorEndpoint.onClientAcceptedPositionChanged -= OnClientAcceptedPositionChanged;
            AccountEndpoint.onEnteringWorld -= OnEnteringWorld;
            AccountEndpoint.onEnteredWorld -= OnEnteredWorld;
            RevokeLocalControl();
            ActorEndpoint.onClientInstanceValidationToggleResult -= OnInstanceValidationToggleResult;

            if (ClientInputController.Instance != null)
            {
                ClientInputController.Instance.onMovementInput -= OnMovementInput;
                ClientInputController.Instance.onJumpInput -= OnJumpInput;
                ClientInputController.Instance.onInteractInput -= OnInteractInput;
                ClientInputController.Instance.onInstanceValidationToggleInput -= OnInstanceValidationToggleInput;
            }
        }

        private void Update()
        {
            // Keep the captured server position stable while the destination interest grid loads.
            if (_instanceValidationTogglePending)
                return;

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
            // Recenter after movement so the next proposal uses the updated origin frame.
            if (!ClientPositionManager.Initialized || ClientPositionManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Cannot update the floating origin before ClientPositionManager is initialized.");
            }

            var positionManager = ClientPositionManager.Instance;
            var chunk = positionManager.LocalToChunkRelative(transform.position).chunk;
            if (chunk.x != positionManager.OriginChunk.x || chunk.y != positionManager.OriginChunk.y)
                positionManager.SetOriginChunk(chunk);

            TrySubmitPositionProposal();

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

        // Samples the post-Move pose in the current floating-origin frame, then sends at most
        // once per proposal interval when the absolute position changed or the heartbeat is due.
        private void TrySubmitPositionProposal()
        {
            float proposalTime = Time.time;
            if (_hasSentPositionProposal
                && proposalTime - _lastPositionProposalTime < POSITION_PROPOSAL_INTERVAL_SECONDS)
                return;

            if (!ClientPositionManager.Initialized || ClientPositionManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Cannot submit a movement proposal before ClientPositionManager is initialized.");
            }

            var positionManager = ClientPositionManager.Instance;
            var originChunk = positionManager.OriginChunk;
            var (chunk, localPosition) = positionManager.LocalToChunkRelative(transform.position);
            var position = WorldPosition.FromChunkLocal(chunk, localPosition);
            bool positionChanged = !_hasSentPositionProposal || !IsSamePosition(position, _lastTransmittedPosition);
            bool heartbeatDue = !_hasSentPositionProposal
                || proposalTime - _lastPositionProposalTime >= POSITION_HEARTBEAT_INTERVAL_SECONDS;
            if (!positionChanged && !heartbeatDue)
                return;

            string sendReason = !_hasSentPositionProposal
                ? "initial"
                : positionChanged ? "moved" : "heartbeat";
            var proposalSequence = checked(++_proposalSequence);

            MovementEndpoint.Server_SubmitPositionProposal(
                position,
                _ownershipEpoch,
                proposalSequence);

            _lastTransmittedPosition = position;
            _lastPositionProposalTime = proposalTime;
            _hasSentPositionProposal = true;
        }

        // Prevents a previous owner's cadence and position baseline from affecting this owner epoch.
        private void ResetPositionProposalState()
        {
            _hasSentPositionProposal = false;
            _lastPositionProposalTime = 0f;
            _lastTransmittedPosition = default;
        }

        // Treats small float-origin reconstruction differences as unchanged while preserving
        // deliberate chunk transitions as movement.
        private static bool IsSamePosition(WorldPosition left, WorldPosition right)
        {
            if (left.Chunk.x != right.Chunk.x || left.Chunk.y != right.Chunk.y)
                return false;

            return Math.Abs(left.x - right.x) <= POSITION_COMPARISON_TOLERANCE_METRES
                && Math.Abs(left.y - right.y) <= POSITION_COMPARISON_TOLERANCE_METRES
                && Math.Abs(left.z - right.z) <= POSITION_COMPARISON_TOLERANCE_METRES;
        }

        /// <summary>
        /// Called only when the server detects an illegal position (speed hack,
        /// collision penetration, etc.) and forces the client back.
        /// </summary>
        private void OnServerCorrection(Vector2Int chunk, Vector3 localPosition)
        {
            if (!enabled || _cc == null || _actorMotor == null)
                return;

            var correctedPos = ClientPositionManager.Instance.NetworkToLocal(chunk, localPosition);
            _cc.enabled = false;
            transform.position = correctedPos;
            _cc.enabled = true;
            _actorMotor.verticalVelocity = 0f;
            _actorMotor.horizontalVelocity = Vector3.zero;
            _actorMotor.isGrounded = true;
        }

        private void OnMovementInput(Vector2 direction)
        {
            if (enabled)
                _currentMovementInput = direction;
        }

        private void OnJumpInput()
        {
            if (enabled)
                _jumpRequested = true;
        }
        private void OnInteractInput() { /* TODO */ }

        private void OnInstanceValidationToggleInput()
        {
            if (!enabled || _actorMotor == null || _instanceValidationTogglePending)
                return;

            Actor playerActor = ClientAccountManager.PlayerActor;
            if (playerActor == null
                || playerActor.gameObject != gameObject
                || !playerActor.Owner.HasValue
                || NetworkManager.main == null
                || playerActor.Owner.Value != NetworkManager.main.localPlayer)
            {
                return;
            }

            _instanceValidationTogglePending = true;
            _currentMovementInput = Vector2.zero;
            _jumpRequested = false;
            ActorEndpoint.Server_RequestInstanceValidationToggle();
        }

        private void OnInstanceValidationToggleResult(
            bool succeeded,
            string instanceLabel,
            Guid instanceID)
        {
            if (!enabled
                || ClientAccountManager.PlayerActor == null
                || ClientAccountManager.PlayerActor.gameObject != gameObject)
            {
                return;
            }

            _instanceValidationTogglePending = false;
            if (succeeded)
                Debug.Log($"[InstanceValidation] Switched player to instance {instanceLabel} ({instanceID}).");
            else
                Debug.Log("[InstanceValidation] Instance toggle failed.");
        }
    }
}
