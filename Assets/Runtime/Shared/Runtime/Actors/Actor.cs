using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Spellcasting;
using Game.Shared.Utility;
using Guid = System.Guid;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>Base networked actor shared by players and NPCs.</summary>
    public class Actor : NetworkBehaviour, ILevelledActor
    {
        /// <summary>Gets the actor's unique identifier.</summary>
        public Guid ID { get; private set; } = System.Guid.NewGuid();
        /// <summary>Gets the associated spawn point identifier.</summary>
        public Guid SpawnPointGUID { get; private set; }
        /// <summary>Gets the actor's display name.</summary>
        public string Name { get; private set; } = "Unknown";
        /// <summary>Gets the owning player, when one exists.</summary>
        public PlayerID? Owner { get; private set; }
        /// <summary>Gets whether this actor represents a player.</summary>
        public virtual bool IsPlayer => false;

        /// <summary>Gets or sets whether the actor is dead.</summary>
        public bool isDead = false;

        /// <summary>Gets or sets the current streamed world chunk.</summary>
        public string CurrentChunk { get; set; }

        [SerializeField]
        private readonly SyncVar<ActorPositionSnapshot> _acceptedPositionSnapshot = new();
        private long _lastClientAcceptedPositionTick = long.MinValue;
        private bool _serverDespawnPublished;

        private float? _cachedHitboxRadius;

        /// <summary>Raised when the actor's auto attack hits.</summary>
        public Action onAutoAttackHit;

        /// <summary>Gets the transform used as the actor's hit location.</summary>
        [SerializeField] public Transform hitLocation;

        /// <summary>
        /// Returns the actor's hitbox radius from NavMeshAgent, CharacterController, or CapsuleCollider.
        /// </summary>
        public float HitboxRadius
        {
            get
            {
                if (_cachedHitboxRadius.HasValue) return _cachedHitboxRadius.Value;

                if (TryGetComponent<NavMeshAgent>(out var agent))
                    _cachedHitboxRadius = agent.radius + 0.5f;
                else if (TryGetComponent<CharacterController>(out var cc))
                    _cachedHitboxRadius = cc.radius + 0.5f;
                else if (TryGetComponent<CapsuleCollider>(out var capsule))
                    _cachedHitboxRadius = capsule.radius + 0.5f;
                else
                    _cachedHitboxRadius = 0.5f;

                return _cachedHitboxRadius.Value;
            }
        }

        /// <summary>
        /// Returns the surface-to-surface distance between this actor and another,
        /// accounting for both hitbox radii.
        /// </summary>
        public float DistanceTo(Actor other)
        {
            float raw = Vector3.Distance(transform.position, other.transform.position);
            return Mathf.Max(0f, raw - HitboxRadius - other.HitboxRadius);
        }

        private ActorSpatialContext _spatialContext;
        /// <summary>Gets the server-assigned authoritative world, map and instance context.</summary>
        public ActorSpatialContext SpatialContext => _spatialContext;

        /// <summary>Assigns the actor's authoritative spatial context on the server.</summary>
        /// <param name="context">The valid server-owned world, map and instance context.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called outside the server or after a context has already been assigned.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        public void SetAuthoritativeSpatialContext(ActorSpatialContext context)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may assign an actor spatial context.");
            if (!context.IsValid)
                throw new ArgumentException("A valid actor spatial context is required.", nameof(context));
            if (_spatialContext.IsValid)
                throw new InvalidOperationException("The actor already has an authoritative spatial context.");

            _spatialContext = context;
        }

        /// <summary>
        /// Transfers this actor's authoritative server-owned spatial context after verifying its current context.
        /// </summary>
        /// <param name="expectedCurrentContext">The context the server expects this actor to currently have.</param>
        /// <param name="destinationContext">The valid server-owned destination context.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called outside the server, when the actor has no current context, or when the expected
        /// context does not match the actor's current context.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the expected or destination context is invalid.
        /// </exception>
        public void TransferAuthoritativeSpatialContext(
            ActorSpatialContext expectedCurrentContext,
            ActorSpatialContext destinationContext)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may transfer an actor spatial context.");
            if (!expectedCurrentContext.IsValid)
                throw new ArgumentException(
                    "A valid expected actor spatial context is required.",
                    nameof(expectedCurrentContext));
            if (!destinationContext.IsValid)
                throw new ArgumentException(
                    "A valid destination actor spatial context is required.",
                    nameof(destinationContext));
            if (!_spatialContext.IsValid)
                throw new InvalidOperationException(
                    "The actor has no authoritative spatial context to transfer.");
            if (!_spatialContext.Equals(expectedCurrentContext))
            {
                throw new InvalidOperationException(
                    $"Actor '{ID}' spatial context mismatch: expected "
                    + $"({expectedCurrentContext.WorldID}, '{expectedCurrentContext.MapID}', "
                    + $"{expectedCurrentContext.InstanceID}), current "
                    + $"({_spatialContext.WorldID}, '{_spatialContext.MapID}', {_spatialContext.InstanceID}).");
            }
            if (destinationContext.Equals(expectedCurrentContext))
                return;

            _spatialContext = destinationContext;
        }

        /// <summary>
        /// Updates the replicated presentation snapshot from the server's accepted position.
        /// </summary>
        public void SetAcceptedPositionSnapshotFromServer(
            WorldPosition position,
            long tickID)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may assign an accepted actor position snapshot.");
            if (!position.IsFinite)
                throw new InvalidOperationException($"Actor {ID} received a non-finite accepted position.");
            if (tickID < 0)
                throw new InvalidOperationException($"Actor {ID} received an invalid accepted position tick {tickID}.");

            var currentSnapshot = _acceptedPositionSnapshot.value;
            if (currentSnapshot.IsValid
                && currentSnapshot.Position.x == position.x
                && currentSnapshot.Position.y == position.y
                && currentSnapshot.Position.z == position.z)
            {
                return;
            }

            _acceptedPositionSnapshot.value = new ActorPositionSnapshot
            {
                IsValid = true,
                TickID = tickID,
                Position = position
            };
        }

        /// <summary>Gets the last server-published accepted-position snapshot when it is valid.</summary>
        /// <param name="snapshot">Receives the valid snapshot, or <see langword="default"/> when unavailable.</param>
        /// <returns>Whether a valid accepted-position snapshot is available.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a valid snapshot contains an invalid tick or non-finite position.
        /// </exception>
        public bool TryGetAcceptedPositionSnapshot(out ActorPositionSnapshot snapshot)
        {
            snapshot = _acceptedPositionSnapshot.value;
            if (!snapshot.IsValid)
            {
                snapshot = default;
                return false;
            }

            if (snapshot.TickID < 0 || !snapshot.Position.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Actor {ID} has a corrupt accepted position snapshot at tick {snapshot.TickID}.");
            }

            return true;
        }

        [SerializeField, FormerlySerializedAs("target")] private Actor _target;
        /// <summary>Gets the actor's current target.</summary>
        public Actor Target => _target;

        [SerializeField, FormerlySerializedAs("projectileTarget")] private Transform _projectileTarget;
        /// <summary>Gets the transform used for projectile targeting.</summary>
        public Transform ProjectileTarget => _projectileTarget;

        /// <summary>
        /// The auras that are active ON the target (not BY the target).
        /// </summary>
        [SerializeField, FormerlySerializedAs("activeAuras")] private Dictionary<int, AuraInstance> _activeAuras = new();
        private List<int> _expiredAurasThisFrame = new();

        /// <summary>Initializes the actor identity.</summary>
        public void Initialize(Guid id, string name)
        {
            ID = id;
            Name = name;
        }

        private void Start()
        {
            if (NetworkManager.isServerStatic) return;
            var identity = GetComponent<NetworkIdentity>();
            if (identity != null && identity.hasOwner)
                SetOwner(identity.owner);

            if (!Game.Shared.Runtime.Runtime.IsServer())
            {
                Game.Shared.Networking.ActorEndpoint.onActorSpawned?.Invoke(this);
            }
        }

        private void Update()
        {
            foreach (var kvp in _activeAuras)
            {
                var aura = kvp.Value;
                aura.Update();

                if (aura.Duration <= 0)
                {
                    _expiredAurasThisFrame.Add(kvp.Key);

                }
            }

            foreach (var id in _expiredAurasThisFrame)
            {
                ActorEndpoint.onClientExpireAura?.Invoke(_activeAuras[id], this, id);
                _activeAuras.Remove(id);
            }

            _expiredAurasThisFrame.Clear();

            if (!NetworkManager.isServerStatic)
            {
                var snapshot = _acceptedPositionSnapshot.value;
                if (snapshot.IsValid
                    && snapshot.TickID >= 0
                    && snapshot.TickID != _lastClientAcceptedPositionTick)
                {
                    if (!snapshot.Position.IsFinite)
                    {
                        throw new InvalidOperationException(
                            $"Actor {ID} received a non-finite accepted position snapshot.");
                    }

                    if (_lastClientAcceptedPositionTick != long.MinValue
                        && snapshot.TickID < _lastClientAcceptedPositionTick)
                    {
                        throw new InvalidOperationException(
                            $"Actor {ID} received accepted position tick {snapshot.TickID} "
                            + $"after {_lastClientAcceptedPositionTick}.");
                    }

                    _lastClientAcceptedPositionTick = snapshot.TickID;
                    ActorEndpoint.onClientAcceptedPositionChanged?.Invoke(this, snapshot);
                }
            }
        }

        /// <summary>Publishes authoritative server retirement once for this actor.</summary>
        public void RetireFromServer()
        {
            if (_serverDespawnPublished)
                return;

            _serverDespawnPublished = true;
            Game.Shared.Networking.ActorEndpoint.onServerActorDespawned?.Invoke(this);
        }

        protected override void OnDestroy()
        {
            if (NetworkManager.isServerStatic)
                RetireFromServer();
            else
                Game.Shared.Networking.ActorEndpoint.onActorDespawned?.Invoke(this);

            base.OnDestroy();
        }

        /// <summary>Gets the actor's display name.</summary>
        public virtual string GetName()
        {
            return Name;
        }

        /// <summary>Gets the actor's level.</summary>
        public virtual int GetLevel()
        {
            Debug.Warning($"GetLevel() not overridden for actor '{Name}' - defaulting to level 1.");
            return 1;
        }

        /// <summary>Gets the faction used for combat relations.</summary>
        public virtual FactionDefinition GetCombatFaction()
        {
            if (TryGetComponent<NPCBehavior>(out var behavior))
            {
                return behavior.CombatFaction;
            }

            return null;
        }

        /// <summary>Gets the actor's relation to a player's faction.</summary>
        public virtual FactionRelationState GetFactionRelation(PlayerReputation playerReputation)
        {
            var npcFaction = GetCombatFaction();
            var playerPVPFaction = playerReputation.PVPFaction;

            if (npcFaction == null || playerPVPFaction == null)
                Debug.Warning($"Cannot determine faction relation for actor '{Name}' because either the NPC faction or player PvP faction is null.");

            if (npcFaction.EnemyFactionIDs.Any(x => x == playerPVPFaction.DefinitionID))
                return FactionRelationState.Hostile;
            else if (npcFaction.AlliedFactionIDs.Any(x => x == playerPVPFaction.DefinitionID))
                return FactionRelationState.Allied;
            else if (npcFaction.DefinitionID == playerPVPFaction.DefinitionID)
                return FactionRelationState.Allied;
            else
                return FactionRelationState.Neutral;
        }

        /// <summary>Gets weapon damage for an equipment slot.</summary>
        public virtual float GetWeaponDamage(ItemSlot slot)
        {
            return GetComponent<NPCStatProfile>()?.GetWeaponDamage(slot, out float damage) ?? 0;
        }

        /// <summary>Gets the weapon swing timer for an equipment slot.</summary>
        public virtual float GetWeaponSwingTimer(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.MainHand:
                    return GetComponent<NPCStatProfile>()?.GetWeaponSwingTimer(ItemSlot.MainHand) ?? 2.0f;
                case ItemSlot.OffHand:
                    return GetComponent<NPCStatProfile>()?.GetWeaponSwingTimer(ItemSlot.OffHand) ?? 2.0f;
                default:
                    return 2.0f; // Default swing timer
            }
        }

        [ServerRpc(requireOwnership: true)]
        /// <summary>Requests a target assignment from the server.</summary>
        public void Server_SetTarget(Actor newTarget)
        {
            _target = newTarget;
            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(GetComponent<NetworkIdentity>());
            Observers_SetTarget(newTarget);
        }

        /// <summary>
        /// Server-authoritative target assignment for NPCs (no ownership required).
        /// </summary>
        public void SetTargetFromServer(Actor newTarget)
        {
            _target = newTarget;
            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(GetComponent<NetworkIdentity>());
            Observers_SetTarget(newTarget);
        }

        [ObserversRpc]
        /// <summary>Replicates an actor target to observers.</summary>
        public void Observers_SetTarget(Actor newTarget)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_TARGET_HANDLER_INVOCATION_COUNT);
            var oldTarget = _target;
            _target = newTarget;

            // If this is our own actor, also trigger the late targeted actor changed event 
            // for any UI that needs to update based on the new target.
            if (Owner == NetworkManager.main.localPlayer)
                ActorEndpoint.onClientLateTargetedActorChanged?.Invoke(oldTarget, newTarget);
        }

        [ServerRpc(requireOwnership: true)]
        /// <summary>Requests a spell cast from the server.</summary>
        public void Server_RequestSpellCast(string spellID, RPCInfo rpcInfo = default)
        {
            GetComponent<ActorSpellcaster>()?.Server_RequestSpellCast(spellID);
        }

        [ServerRpc(requireOwnership: true)]
        /// <summary>Requests that the server start auto attacking.</summary>
        public void Server_StartAutoAttack(RPCInfo rpcInfo = default)
        {
            ActorEndpoint.onServerStartAutoAttack?.Invoke(this);
        }

        [ServerRpc(requireOwnership: true)]
        /// <summary>Requests that the server stop auto attacking.</summary>
        public void Server_StopAutoAttack(RPCInfo rpcInfo = default)
        {
            ActorEndpoint.onServerStopAutoAttack?.Invoke(this);
        }

        /// <summary>Assigns the actor's owner and raises ownership callbacks.</summary>
        public void SetOwner(PlayerID? newOwner)
        {
            var oldOwner = Owner;
            Owner = newOwner;

            if (NetworkManager.isServerStatic)
            {
                Game.Shared.Networking.ActorEndpoint.onServerActorOwnerChanged?.Invoke(this);
            }
            else
            {
                Game.Shared.Networking.ActorEndpoint.onActorOwnerChanged?.Invoke(this);
            }

            if (Owner == NetworkManager.main.localPlayer && !NetworkManager.isServerStatic)
            {
                Game.Shared.Networking.ActorEndpoint.onActorOwnershipTaken?.Invoke(this);
            }
        }

        /// <summary>Sets the actor's display name.</summary>
        public void SetDisplayName(string newName)
        {
            Name = newName;
        }

        /// <summary>Sets the actor's spawn point identifier.</summary>
        public void SetSpawnPointGUID(Guid spawnPointGUID)
        {
            SpawnPointGUID = spawnPointGUID;
        }

        /// <summary>Publishes a combat-log entry to this actor's current observers.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishCombatLogToObservers(CombatLogEntry entry)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish combat-log entries.");

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(GetComponent<NetworkIdentity>());
            Observers_ReceivedCombatLogEntry(entry);
        }

        /// <summary>Publishes a combat-log entry to a specific player.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishCombatLogToTarget(PlayerID playerID, CombatLogEntry entry)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish combat-log entries.");

            NetworkingPerformanceMetrics.RecordCombatCriticalTargetPublication();
            Client_ReceiveCombatLogEntry(playerID, entry);
        }

        [ObserversRpc]
        /// This is called on the CASTER of the spell.
        public void Observers_ReceivedCombatLogEntry(CombatLogEntry entry, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT);
            ActorEndpoint.onCombatLogEntryReceived?.Invoke(entry);
        }

        [TargetRpc]
        public void Client_ReceiveCombatLogEntry(PlayerID player, CombatLogEntry entry, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_COMBAT_LOG_HANDLER_INVOCATION_COUNT);
            ActorEndpoint.onCombatLogEntryReceived?.Invoke(entry);
        }

        [ObserversRpc]
        public void Observers_ApplyAura(string auraID, int instancedID, float duration, int stacks, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT);
            // Get the aura definition.
            AuraDefinition def = AuraDefinitionLibrary.Instance.GetDefinition(auraID);

            if (_activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                existingAura.SetDuration(duration);
                existingAura.SetStacks(stacks);

                _activeAuras[instancedID] = existingAura; // Update the dictionary entry.

                ActorEndpoint.onClientUpdateAura?.Invoke(existingAura, this, instancedID);
            }
            else
            {
                // If it's a new aura, create an instance and add it to the dictionary.
                var newAura = new AuraInstance(instancedID, def, null, this, duration, stacks);
                _activeAuras.Add(instancedID, newAura);

                ActorEndpoint.onClientApplyAura?.Invoke(newAura, this, instancedID);
            }
        }

        #region Aura RPCs
        public List<AuraInstance> GetActiveAuras()
        {
            return new List<AuraInstance>(_activeAuras.Values);
        }

        public float GetRemainingAuraTime(int id)
        {
            return _activeAuras.TryGetValue(id, out var aura) ? aura.Duration : 0f;
        }

        [ObserversRpc]
        public void Observers_ExpireAura(int instancedID, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT);
            if (_activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                _activeAuras.Remove(instancedID);
                ActorEndpoint.onClientExpireAura?.Invoke(existingAura, this, instancedID);
            }
        }

        [ObserversRpc]
        public void Observers_TickAura(int instancedID, int stacks, float duration, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT);
            if (_activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                existingAura.SetStacks(stacks);
                existingAura.SetDuration(duration);
                ActorEndpoint.onClientTickAura?.Invoke(existingAura, this, instancedID);
            }
        }

        [ObserversRpc]
        public void Observers_DispelAura(int instancedID, RPCInfo info = default)
        {
            ActorEndpoint.RecordRpcHandlerInvocation(NetworkingPerformanceMetrics.RPC_ACTOR_CLIENT_AURA_HANDLER_INVOCATION_COUNT);
            if (_activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                _activeAuras.Remove(instancedID);
                ActorEndpoint.onClientDispelAura?.Invoke(existingAura, this, instancedID);
            }
        }
        #endregion

        #region Client-side Interaction
        public virtual void OnPointerEnter(Actor interactor)
        {
            ActorEndpoint.onClientHoveredActorChanged?.Invoke(this);
        }

        public virtual void OnPointerClick(Actor interactor)
        {
            ActorEndpoint.onClientTargetedActorChanged?.Invoke(this);
        }
        #endregion
    }
}
