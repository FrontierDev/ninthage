using System;
using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;
using UnityEngine.AI;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server
{
    public sealed class ServerAutoAttackManager : MonoBehaviour
    {
        private static ServerAutoAttackManager _instance;
        public static ServerAutoAttackManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private SpellDefinition _autoAttackSpell;

        private struct AutoAttackState
        {
            public Actor Actor;
            public NavMeshAgent Agent;
        }

        private readonly Dictionary<Guid, AutoAttackState> _activeAutoAttackers = new();
        private readonly List<Guid> _toRemove = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerAutoAttackManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            _autoAttackSpell = GameConfigurationManager.Config.AutoAttackSpell;
            if (_autoAttackSpell == null)
                Debug.Warning("GameConfiguration.AutoAttackSpell is not assigned. Auto-attacks will not function.");

            ActorService.onServerStartAutoAttack += OnStartAutoAttack;
            ActorService.onServerStopAutoAttack += OnStopAutoAttack;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorService.onServerStartAutoAttack -= OnStartAutoAttack;
            ActorService.onServerStopAutoAttack -= OnStopAutoAttack;

            _activeAutoAttackers.Clear();

            _instance = null;
            _initialized = false;
        }

        private void OnStartAutoAttack(Actor actor)
        {
            if (_autoAttackSpell == null) return;
            if (actor.Target == null) return;

            _activeAutoAttackers[actor.Id] = new AutoAttackState
            {
                Actor = actor,
                Agent = actor.GetComponent<NavMeshAgent>()
            };
        }

        private void OnStopAutoAttack(Actor actor)
        {
            _activeAutoAttackers.Remove(actor.Id);
        }

        private void Update()
        {
            if (_autoAttackSpell == null) return;

            _toRemove.Clear();

            foreach (var kvp in new List<KeyValuePair<Guid, AutoAttackState>>(_activeAutoAttackers))
            {
                var actorId = kvp.Key;
                var state = kvp.Value;

                if (state.Actor == null)
                {
                    _toRemove.Add(actorId);
                    continue;
                }

                if (state.Actor.Target == null)
                {
                    _toRemove.Add(actorId);
                    continue;
                }

                // NPC chase — continuously update NavMeshAgent destination and stopping distance.
                if (state.Agent != null && state.Agent.enabled)
                {
                    state.Agent.stoppingDistance = _autoAttackSpell.BaseRange
                        + state.Actor.HitboxRadius + state.Actor.Target.HitboxRadius;
                    state.Agent.SetDestination(state.Actor.Target.transform.position);
                }

                // Pause auto-attack while casting an ability.
                if (ServerSpellCastManager.Instance.IsCasting(actorId))
                    continue;

                // Check range — skip swing but don't remove (player may walk back).
                float distance = state.Actor.DistanceTo(state.Actor.Target);
                if (distance > _autoAttackSpell.BaseRange)
                    continue;

                // Wait for the auto-attack cooldown to expire before swinging again.
                if (ServerCooldownManager.Instance.IsOnCooldown(actorId, _autoAttackSpell.DefinitionId))
                    continue;

                state.Actor.GetComponent<ActorSpellcaster>()
                    ?.ServerOnly_RequestSpellCast(_autoAttackSpell.DefinitionId);
            }

            for (int i = 0; i < _toRemove.Count; i++)
                _activeAutoAttackers.Remove(_toRemove[i]);
        }
    }
}
