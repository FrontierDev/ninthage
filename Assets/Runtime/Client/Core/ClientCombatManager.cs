using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.UI;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Data.SpellTargetDefinitions;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

namespace Game.Client
{
    /// <summary>
    /// Provides the client-side <c>ClientCombatManager</c> implementation.
    /// </summary>
    public sealed class ClientCombatManager : MonoBehaviour
    {
        private static ClientCombatManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientCombatManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        // Combat
        private bool _inCombat = false;
        /// <summary>
        /// Gets the <c>InCombat</c> value.
        /// </summary>
        public bool InCombat => _inCombat;

        // Spellcasting
        private SpellDefinition _currentCastingSpell;

        // Cooldowns
        private Dictionary<SpellDefinition, float> _activeCooldowns = new();
        private float _globalCooldownRemaining = 0f;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            ActorEndpoint.onClientTargetedActorChanged += OnTargetChanged;
            ActorEndpoint.onStartGlobalCooldown += OnStartGlobalCooldown;
            ActorEndpoint.onStartCooldown += OnStartCooldown;
            ActorEndpoint.onSpellCastStarted += OnSpellCastStarted;
            ActorEndpoint.onSpellCastCompleted += OnSpellCastComplete;
            ActorEndpoint.onSpellCastInterrupted += OnSpellCastInterrupted;
            CharacterEndpoint.onClientLearnedSpell += OnClientLearnedSpell;

            _initialized = true;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // Update global cooldown
            if (_globalCooldownRemaining > 0f)
            {
                _globalCooldownRemaining -= deltaTime;
                if (_globalCooldownRemaining < 0f)
                    _globalCooldownRemaining = 0f;
            }

            // Update active cooldowns
            var spellsOnCooldown = new List<SpellDefinition>(_activeCooldowns.Keys);
            foreach (var spell in spellsOnCooldown)
            {
                _activeCooldowns[spell] -= deltaTime;
                if (_activeCooldowns[spell] <= 0f)
                    _activeCooldowns.Remove(spell);
            }
        }

        private void OnDestroy()
        {
            _instance = null;
            _initialized = false;

            ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;
            ActorEndpoint.onClientTargetedActorChanged -= OnTargetChanged;
            ActorEndpoint.onStartGlobalCooldown -= OnStartGlobalCooldown;
            ActorEndpoint.onStartCooldown -= OnStartCooldown;
            ActorEndpoint.onSpellCastStarted -= OnSpellCastStarted;
            ActorEndpoint.onSpellCastCompleted -= OnSpellCastComplete;
            ActorEndpoint.onSpellCastInterrupted -= OnSpellCastInterrupted;
            CharacterEndpoint.onClientLearnedSpell -= OnClientLearnedSpell;
        }

        private void OnPlayerActorAssigned(Actor playerActor)
        {
            ClientInputController.Instance.onMovementInput += OnPlayerMovementInput;
            ClientInputController.Instance.onCancelInput += OnCancelInput;
            ClientInputController.Instance.onJumpInput += OnJumpInput;
        }

        private void OnClientLearnedSpell(string spellID)
        {
            ClientAccountManager.PlayerActor.GetComponent<ActorSpellcaster>().LearnSpell(spellID);
        }

        private void OnTargetChanged(Actor newTarget)
        {
            ClientAccountManager.PlayerActor.Server_SetTarget(newTarget);
        }

        private void OnPlayerMovementInput(Vector2 movementVector)
        {
            if (movementVector != Vector2.zero)
                if (_currentCastingSpell != null)
                    if (!_currentCastingSpell.CanMoveWhileCasting)
                        CancelCast();
        }

        private void OnCancelInput()
        {
            if (_currentCastingSpell != null)
                CancelCast();
        }

        private void OnJumpInput()
        {
            if (_currentCastingSpell != null)
                if (!_currentCastingSpell.CanMoveWhileCasting)
                    CancelCast();
        }

        private void OnStartGlobalCooldown()
        {
            _globalCooldownRemaining = GameConfigurationManager.Config.GlobalCooldownDuration;
        }

        private void OnStartCooldown(string spellID, float duration)
        {
            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            if (spellDef != null)
                _activeCooldowns[spellDef] = duration;
        }
        #endregion

        /// <summary>
        /// Executes the client-side <c>CastSpell</c> operation.
        /// </summary>
        public void CastSpell(string spellID, int rank = 1)
        {
            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            if (spellDef == null) return;

            if (spellDef.TriggersGCD && IsOnGlobalCooldown())
            {
                Debug.Log("Cannot cast spell - global cooldown active.");
                return;
            }

            if (_activeCooldowns.ContainsKey(spellDef))
            {
                Debug.Log($"Cannot cast spell '{spellID}' - spell is on cooldown.");
                return;
            }

            var spellcaster = ClientAccountManager.PlayerActor.GetComponent<ActorSpellcaster>();

            bool isGroundTargeted = spellDef.BaseComponents.Any(
                c => c.TargetDefinition is SpellTarget_GroundLocation);

            if (isGroundTargeted)
            {
                if (!TryGetGroundPosition(out var groundPos))
                    groundPos = ClientAccountManager.PlayerActor.transform.position;
                spellcaster.Server_RequestGroundSpellCast(spellID, groundPos);
            }
            else
            {
                spellcaster.Server_RequestSpellCast(spellID);
            }
        }

        private static bool TryGetGroundPosition(out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            var cam = Camera.main;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                groundPosition = hit.point;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Executes the client-side <c>StartAutoAttack</c> operation.
        /// </summary>
        public void StartAutoAttack()
        {
            ClientAccountManager.PlayerActor.Server_StartAutoAttack();
            _inCombat = true;
            ActorEndpoint.onClientEnteredCombat?.Invoke();
            ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetBool("InCombat", true);
        }

        /// <summary>
        /// Executes the client-side <c>StopAutoAttack</c> operation.
        /// </summary>
        public void StopAutoAttack()
        {
            ClientAccountManager.PlayerActor.Server_StopAutoAttack();
            _inCombat = false;
            ActorEndpoint.onClientExitedCombat?.Invoke();
            ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetBool("InCombat", false);
        }

        private void CancelCast()
        {
            ClientAccountManager.PlayerActor.GetComponent<ActorSpellcaster>().Server_CancelSpellCast();
            UI_ClientCastBarWindow.Instance.HideImmediate();
        }

        private void OnSpellCastStarted(Actor actor, string spellID, float castTime)
        {
            if (actor == ClientAccountManager.PlayerActor)
            {
                ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetTrigger("spellcast_start");

                var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
                if (spellDef != null)
                    _currentCastingSpell = spellDef;
            }
        }

        private void OnSpellCastComplete(Actor actor)
        {
            if (actor == ClientAccountManager.PlayerActor && _currentCastingSpell != null)
            {
                _currentCastingSpell = null;
                ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetTrigger("spellcast_end");
            }
        }

        private void OnSpellCastInterrupted(Actor actor)
        {
            if (actor == ClientAccountManager.PlayerActor && _currentCastingSpell != null)
            {
                _currentCastingSpell = null;
                ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetTrigger("spellcast_interrupt");
            }

        }

        /// <summary>
        /// Executes the client-side <c>IsOnGlobalCooldown</c> operation.
        /// </summary>
        public bool IsOnGlobalCooldown()
        {
            return _globalCooldownRemaining > 0f;
        }

        /// <summary>
        /// Executes the client-side <c>IsSpellOnCooldown</c> operation.
        /// </summary>
        public bool IsSpellOnCooldown(SpellDefinition spell)
        {
            return _activeCooldowns.ContainsKey(spell);
        }
    }
}
