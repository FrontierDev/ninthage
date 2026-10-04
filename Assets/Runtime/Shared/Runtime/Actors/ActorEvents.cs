using System;
using Game.Shared.Data;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public class ActorEvents : NetworkBehaviour
    {
        // Server events
        public Action<SpellContext, Actor> onServerAutoAttackHit;
        public Action<SpellContext, Actor> onServerAutoAttackTaken;
        public Action<SpellContext> onServerSpellCastComplete;

        // Client events
        public Action<SpellContext, Actor> onClientAutoAttackHit;
        public Action<SpellContext, Actor> onClientAutoAttackTaken;
        public Action<SpellContext> onClientSpellCastComplete;

        public void LoadClassEvents(CharacterData data)
        {
            try
            {
                var classDef = ClassDefinitionLibrary.Instance.GetDefinition(data.ClassID);
                if (classDef.ClassBehaviour != null)
                {
                    classDef.ClassBehaviour.OnActivate(GetComponent<PlayerActor>());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading class events for class ID {data.ClassID}: {e}");
            }

            Debug.Log($"Loaded class events for {data.ClassID} on actor {name}");
        }
    }
}