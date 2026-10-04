using PurrNet;
using UnityEngine;
using UnityEngine.AI;
using Game.Shared.Utility;
using Guid = System.Guid;
using Game.Shared.Networking;
using Debug = Game.Shared.FormattedDebug;
using System.Collections.Generic;
using Game.Shared.Data;
using System.Linq;
using System;

namespace Game.Shared
{
    public class Actor : NetworkBehaviour, ILevelledActor
    {
        public Guid Id { get; private set; } = System.Guid.NewGuid();
        public Guid SpawnPointGuid { get; private set; }
        public string Name { get; private set; } = "Unknown";
        public PlayerID? Owner { get; private set; }
        public virtual bool IsPlayer => false;

        public bool isDead = false;

        public string CurrentChunk { get; set; }

        private float? _cachedHitboxRadius;

        public Action onAutoAttackHit;

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

        [SerializeField] private Actor target;
        public Actor Target => target;

        [SerializeField] private Transform projectileTarget;
        public Transform ProjectileTarget => projectileTarget;

        /// <summary>
        /// The auras that are active ON the target (not BY the target).
        /// </summary>
        [SerializeField] private Dictionary<int, AuraInstance> activeAuras = new Dictionary<int, AuraInstance>();
        private List<int> expiredAurasThisFrame = new List<int>();

        public void Initialize(Guid id, string name)
        {
            Id = id;
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
                Game.Shared.Networking.ActorService.onActorSpawned?.Invoke(this);
            }
        }

        private void Update()
        {
            foreach (var kvp in activeAuras)
            {
                var aura = kvp.Value;
                aura.Update();

                if (aura.Duration <= 0)
                {
                    expiredAurasThisFrame.Add(kvp.Key);

                }
            }

            foreach (var id in expiredAurasThisFrame)
            {
                ActorService.onClientExpireAura?.Invoke(activeAuras[id], this, id);
                activeAuras.Remove(id);
            }

            expiredAurasThisFrame.Clear();
        }

        [ToDo("Any onActorDespawned logic.")]
        protected override void OnDestroy()
        {
            if (NetworkManager.isServerStatic)
                Game.Shared.Networking.ActorService.onServerActorDespawned?.Invoke(this);

            base.OnDestroy();
            //else
            //Game.Shared.Networking.ActorService.onActorDespawned?.Invoke(this);
        }

        public virtual string GetName()
        {
            return Name;
        }

        public virtual int GetLevel()
        {
            Debug.Warning($"GetLevel() not overridden for actor '{Name}' - defaulting to level 1.");
            return 1;
        }

        public virtual FactionDefinition GetCombatFaction()
        {
            if (TryGetComponent<NPCBehavior>(out var behavior))
            {
                return behavior.CombatFaction;
            }

            return null;
        }

        public virtual FactionRelationState GetFactionRelation(PlayerReputation playerReputation)
        {
            var npcFaction = GetCombatFaction();
            var playerPvPFaction = playerReputation.PvPFaction;

            if (npcFaction == null || playerPvPFaction == null)
                Debug.Warning($"Cannot determine faction relation for actor '{Name}' because either the NPC faction or player PvP faction is null.");

            if (npcFaction.EnemyFactionIDs.Any(x => x == playerPvPFaction.DefinitionId))
                return FactionRelationState.Hostile;
            else if (npcFaction.AlliedFactionIDs.Any(x => x == playerPvPFaction.DefinitionId))
                return FactionRelationState.Allied;
            else if (npcFaction.DefinitionId == playerPvPFaction.DefinitionId)
                return FactionRelationState.Allied;
            else
                return FactionRelationState.Neutral;
        }

        public virtual float GetWeaponDamage(ItemSlot slot)
        {
            return GetComponent<NPCStatProfile>()?.GetWeaponDamage(slot, out float damage) ?? 0;
        }

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
        public void Server_SetTarget(Actor newTarget)
        {
            target = newTarget;
            Observers_SetTarget(newTarget);
        }

        /// <summary>
        /// Server-authoritative target assignment for NPCs (no ownership required).
        /// </summary>
        public void SetTargetFromServer(Actor newTarget)
        {
            target = newTarget;
            Observers_SetTarget(newTarget);
        }

        [ObserversRpc]
        public void Observers_SetTarget(Actor newTarget)
        {
            var oldTarget = target;
            target = newTarget;

            // If this is our own actor, also trigger the late targeted actor changed event 
            // for any UI that needs to update based on the new target.
            if (Owner == NetworkManager.main.localPlayer)
                ActorService.onClientLateTargetedActorChanged?.Invoke(oldTarget, newTarget);
        }

        [ServerRpc(requireOwnership: true)]
        public void Server_RequestSpellCast(string spellID, RPCInfo rpcInfo = default)
        {
            GetComponent<ActorSpellcaster>()?.Server_RequestSpellCast(spellID);
        }

        [ServerRpc(requireOwnership: true)]
        public void Server_StartAutoAttack(RPCInfo rpcInfo = default)
        {
            ActorService.onServerStartAutoAttack?.Invoke(this);
        }

        [ServerRpc(requireOwnership: true)]
        public void Server_StopAutoAttack(RPCInfo rpcInfo = default)
        {
            ActorService.onServerStopAutoAttack?.Invoke(this);
        }

        public void SetOwner(PlayerID? newOwner)
        {
            var oldOwner = Owner;
            Owner = newOwner;

            if (NetworkManager.isServerStatic)
            {
                Game.Shared.Networking.ActorService.onServerActorOwnerChanged?.Invoke(this);
            }
            else
            {
                Game.Shared.Networking.ActorService.onActorOwnerChanged?.Invoke(this);
            }

            if (Owner == NetworkManager.main.localPlayer && !NetworkManager.isServerStatic)
            {
                Game.Shared.Networking.ActorService.onActorOwnershipTaken?.Invoke(this);
            }
        }

        public void SetDisplayName(string newName)
        {
            Name = newName;
        }

        public void SetSpawnPointGuid(Guid spawnPointGuid)
        {
            SpawnPointGuid = spawnPointGuid;
        }

        [ObserversRpc]
        /// This is called on the CASTER of the spell.
        public void Observers_ReceivedCombatLogEntry(CombatLogEntry entry, RPCInfo info = default)
        {
            ActorService.onCombatLogEntryReceived?.Invoke(entry);
        }

        [TargetRpc]
        public void Client_ReceiveCombatLogEntry(PlayerID player, CombatLogEntry entry, RPCInfo info = default)
        {
            ActorService.onCombatLogEntryReceived?.Invoke(entry);
        }

        [ObserversRpc]
        public void Observers_ApplyAura(string auraID, int instancedID, float duration, int stacks, RPCInfo info = default)
        {
            // Get the aura definition.
            AuraDefinition def = AuraDefinitionLibrary.Instance.GetDefinition(auraID);

            if (activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                existingAura.SetDuration(duration);
                existingAura.SetStacks(stacks);

                activeAuras[instancedID] = existingAura; // Update the dictionary entry.

                ActorService.onClientUpdateAura?.Invoke(existingAura, this, instancedID);
            }
            else
            {
                // If it's a new aura, create an instance and add it to the dictionary.
                var newAura = new AuraInstance(instancedID, def, null, this, duration, stacks);
                activeAuras.Add(instancedID, newAura);

                ActorService.onClientApplyAura?.Invoke(newAura, this, instancedID);
            }
        }

        #region Aura RPCs
        public List<AuraInstance> GetActiveAuras()
        {
            return new List<AuraInstance>(activeAuras.Values);
        }

        public float GetRemainingAuraTime(int id)
        {
            return activeAuras.TryGetValue(id, out var aura) ? aura.Duration : 0f;
        }

        [ObserversRpc]
        public void Observers_ExpireAura(int instancedID, RPCInfo info = default)
        {
            if (activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                activeAuras.Remove(instancedID);
                ActorService.onClientExpireAura?.Invoke(existingAura, this, instancedID);
            }
        }

        [ObserversRpc]
        public void Observers_TickAura(int instancedID, int stacks, float duration, RPCInfo info = default)
        {
            if (activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                existingAura.SetStacks(stacks);
                existingAura.SetDuration(duration);
                ActorService.onClientTickAura?.Invoke(existingAura, this, instancedID);
            }
        }

        [ObserversRpc]
        public void Observers_DispelAura(int instancedID, RPCInfo info = default)
        {
            if (activeAuras.TryGetValue(instancedID, out var existingAura))
            {
                activeAuras.Remove(instancedID);
                ActorService.onClientDispelAura?.Invoke(existingAura, this, instancedID);
            }
        }
        #endregion

        #region Client-side Interaction
        public virtual void OnPointerEnter(Actor interactor)
        {
            ActorService.onClientHoveredActorChanged?.Invoke(this);
        }

        public virtual void OnPointerClick(Actor interactor)
        {
            ActorService.onClientTargetedActorChanged?.Invoke(this);
        }
        #endregion
    }
}