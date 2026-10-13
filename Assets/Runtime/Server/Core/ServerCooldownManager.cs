using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Server
{
    /// <summary>Maintains server-authoritative actor cooldowns.</summary>
    public sealed class ServerCooldownManager : MonoBehaviour
    {
        private static ServerCooldownManager _instance;
        /// <summary>Gets the active server cooldown manager.</summary>
        public static ServerCooldownManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the cooldown manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        /// <summary>The identifier used for the global cooldown.</summary>
        public const string GCD_ID = "GCD";

        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerCooldownManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ECS.ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

            ActorEndpoint.onServerStartCooldown += StartCooldown;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerStartCooldown -= StartCooldown;

            _instance = null;
            _initialized = false;
        }

        /// <summary>Starts or refreshes a cooldown for an actor.</summary>
        /// <param name="actorID">The actor receiving the cooldown.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <param name="duration">The cooldown duration in seconds.</param>
        public void StartCooldown(Guid actorID, string spellID, float duration)
        {
            var fixedSpellID = new FixedString64Bytes(spellID);

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<ECS.CooldownElement>(entity);

                    // Update existing entry or append a new one.
                    bool found = false;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].SpellID == fixedSpellID)
                        {
                            buffer[i] = new ECS.CooldownElement
                            {
                                SpellID = fixedSpellID,
                                RemainingCooldown = duration
                            };
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        buffer.Add(new ECS.CooldownElement
                        {
                            SpellID = fixedSpellID,
                            RemainingCooldown = duration
                        });
                    }

                    return;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        /// <summary>Determines whether an actor currently has a cooldown.</summary>
        /// <param name="actorID">The actor to query.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <returns><see langword="true"/> when the cooldown is active.</returns>
        public bool IsOnCooldown(Guid actorID, string spellID)
        {
            return GetRemainingCooldown(actorID, spellID) > 0f;
        }

        /// <summary>Gets the remaining duration of an actor's cooldown.</summary>
        /// <param name="actorID">The actor to query.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <returns>The remaining duration in seconds, or zero when inactive.</returns>
        public float GetRemainingCooldown(Guid actorID, string spellID)
        {
            var fixedSpellID = new FixedString64Bytes(spellID);

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorID != actorID) continue;

                    var buffer = _entityManager.GetBuffer<ECS.CooldownElement>(entity, true);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].SpellID == fixedSpellID)
                            return buffer[i].RemainingCooldown;
                    }

                    return 0f;
                }
            }
            finally
            {
                entities.Dispose();
            }

            return 0f;
        }
    }
}
