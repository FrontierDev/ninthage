using System;
using Game.Shared;
using Game.Shared.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server
{
    public sealed class ServerCooldownManager : MonoBehaviour
    {
        private static ServerCooldownManager _instance;
        public static ServerCooldownManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

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

            ActorService.onServerStartCooldown += StartCooldown;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorService.onServerStartCooldown -= StartCooldown;

            _instance = null;
            _initialized = false;
        }

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

        public bool IsOnCooldown(Guid actorId, string spellId)
        {
            return GetRemainingCooldown(actorId, spellId) > 0f;
        }

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
