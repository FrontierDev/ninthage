using System.Collections.Generic;
using System.Linq;
using Game.Client.UI;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client
{
    public sealed class ClientCombatManager : MonoBehaviour
    {
        private static ClientCombatManager _instance;
        public static ClientCombatManager Instance => _instance;
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        // Combat
        private bool inCombat = false;
        public bool InCombat => inCombat;

        // Spellcasting
        private SpellDefinition _currentCastingSpell;

        // Cooldowns
        private Dictionary<SpellDefinition, float> activeCooldowns = new();
        private float globalCooldownRemaining = 0f;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;

            ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            ActorService.onClientTargetedActorChanged += OnTargetChanged;
            ActorService.onStartGlobalCooldown += OnStartGlobalCooldown;
            ActorService.onStartCooldown += OnStartCooldown;
            ActorService.onSpellCastStarted += OnSpellCastStarted;
            ActorService.onSpellCastCompleted += OnSpellCastComplete;
            ActorService.onSpellCastInterrupted += OnSpellCastInterrupted;
            CharacterService.onClientLearnedSpell += OnClientLearnedSpell;

            _initialized = true;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // Update global cooldown
            if (globalCooldownRemaining > 0f)
            {
                globalCooldownRemaining -= deltaTime;
                if (globalCooldownRemaining < 0f)
                    globalCooldownRemaining = 0f;
            }

            // Update active cooldowns
            var spellsOnCooldown = new List<SpellDefinition>(activeCooldowns.Keys);
            foreach (var spell in spellsOnCooldown)
            {
                activeCooldowns[spell] -= deltaTime;
                if (activeCooldowns[spell] <= 0f)
                    activeCooldowns.Remove(spell);
            }
        }

        private void OnDestroy()
        {
            _instance = null;
            _initialized = false;

            ActorService.onPlayerActorAssigned -= OnPlayerActorAssigned;
            ActorService.onClientTargetedActorChanged -= OnTargetChanged;
            ActorService.onStartGlobalCooldown -= OnStartGlobalCooldown;
            ActorService.onStartCooldown -= OnStartCooldown;
            ActorService.onSpellCastStarted -= OnSpellCastStarted;
            ActorService.onSpellCastCompleted -= OnSpellCastComplete;
            ActorService.onSpellCastInterrupted -= OnSpellCastInterrupted;
            CharacterService.onClientLearnedSpell -= OnClientLearnedSpell;
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
            globalCooldownRemaining = GameConfigurationManager.Config.GlobalCooldownDuration;
        }

        private void OnStartCooldown(string spellID, float duration)
        {
            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            if (spellDef != null)
                activeCooldowns[spellDef] = duration;
        }
        #endregion

        public void CastSpell(string spellID, int rank = 1)
        {
            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            if (spellDef == null) return;

            if (spellDef.TriggersGCD && IsOnGlobalCooldown())
            {
                Debug.Log("Cannot cast spell - global cooldown active.");
                return;
            }

            if (activeCooldowns.ContainsKey(spellDef))
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

        public void StartAutoAttack()
        {
            ClientAccountManager.PlayerActor.Server_StartAutoAttack();
            inCombat = true;
            ActorService.onClientEnteredCombat?.Invoke();
            ClientAccountManager.PlayerActor.GetComponent<NetworkAnimator>().SetBool("InCombat", true);
        }

        public void StopAutoAttack()
        {
            ClientAccountManager.PlayerActor.Server_StopAutoAttack();
            inCombat = false;
            ActorService.onClientExitedCombat?.Invoke();
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

        public bool IsOnGlobalCooldown()
        {
            return globalCooldownRemaining > 0f;
        }

        public bool IsSpellOnCooldown(SpellDefinition spell)
        {
            return activeCooldowns.ContainsKey(spell);
        }
    }
}