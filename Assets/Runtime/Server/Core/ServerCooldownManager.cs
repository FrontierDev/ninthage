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

        private EntityManager entityManager;
        private EntityQuery actorComponentQuery;

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
            entityManager = world.EntityManager;
            actorComponentQuery = entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

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
        /// <param name="actorId">The actor receiving the cooldown.</param>
        /// <param name="spellId">The spell or cooldown identifier.</param>
        /// <param name="duration">The cooldown duration in seconds.</param>
        public void StartCooldown(Guid actorId, string spellId, float duration)
        {
            var fixedSpellId = new FixedString64Bytes(spellId);

            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actorId) continue;

                    var buffer = entityManager.GetBuffer<ECS.CooldownElement>(entity);

                    // Update existing entry or append a new one.
                    bool found = false;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].SpellID == fixedSpellId)
                        {
                            buffer[i] = new ECS.CooldownElement
                            {
                                SpellID = fixedSpellId,
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
                            SpellID = fixedSpellId,
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
        /// <param name="actorId">The actor to query.</param>
        /// <param name="spellId">The spell or cooldown identifier.</param>
        /// <returns><see langword="true"/> when the cooldown is active.</returns>
        public bool IsOnCooldown(Guid actorId, string spellId)
        {
            return GetRemainingCooldown(actorId, spellId) > 0f;
        }

        /// <summary>Gets the remaining duration of an actor's cooldown.</summary>
        /// <param name="actorId">The actor to query.</param>
        /// <param name="spellId">The spell or cooldown identifier.</param>
        /// <returns>The remaining duration in seconds, or zero when inactive.</returns>
        public float GetRemainingCooldown(Guid actorId, string spellId)
        {
            var fixedSpellId = new FixedString64Bytes(spellId);

            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actorId) continue;

                    var buffer = entityManager.GetBuffer<ECS.CooldownElement>(entity, true);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i].SpellID == fixedSpellId)
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
