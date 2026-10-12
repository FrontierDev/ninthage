using Debug = Game.Shared.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Utility;
using Guid = System.Guid;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Game.Shared
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

        /// <summary>
        /// Double-precision authoritative world position.
        /// Server sets this; clients derive local transforms via floating origin.
        /// </summary>
        public WorldPosition WorldPos { get; set; }

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

            if (!Runtime.IsServer())
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
        }

        [ToDo("Any onActorDespawned logic.")]
        protected override void OnDestroy()
        {
            if (NetworkManager.isServerStatic)
                Game.Shared.Networking.ActorEndpoint.onServerActorDespawned?.Invoke(this);

            base.OnDestroy();
            //else
            //Game.Shared.Networking.ActorEndpoint.onActorDespawned?.Invoke(this);
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
            Observers_SetTarget(newTarget);
        }

        /// <summary>
        /// Server-authoritative target assignment for NPCs (no ownership required).
        /// </summary>
        public void SetTargetFromServer(Actor newTarget)
        {
            _target = newTarget;
            Observers_SetTarget(newTarget);
        }

        [ObserversRpc]
        /// <summary>Replicates an actor target to observers.</summary>
        public void Observers_SetTarget(Actor newTarget)
        {
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

        [ObserversRpc]
        /// This is called on the CASTER of the spell.
        public void Observers_ReceivedCombatLogEntry(CombatLogEntry entry, RPCInfo info = default)
        {
            ActorEndpoint.onCombatLogEntryReceived?.Invoke(entry);
        }

        [TargetRpc]
        public void Client_ReceiveCombatLogEntry(PlayerID player, CombatLogEntry entry, RPCInfo info = default)
        {
            ActorEndpoint.onCombatLogEntryReceived?.Invoke(entry);
        }

        [ObserversRpc]
        public void Observers_ApplyAura(string auraID, int instancedID, float duration, int stacks, RPCInfo info = default)
        {
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
            if (_activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                _activeAuras.Remove(instancedID);
                ActorEndpoint.onClientExpireAura?.Invoke(existingAura, this, instancedID);
            }
        }

        [ObserversRpc]
        public void Observers_TickAura(int instancedID, int stacks, float duration, RPCInfo info = default)
        {
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
