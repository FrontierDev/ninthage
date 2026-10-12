using Game.Shared.Data;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Spellcasting;
using PurrNet;
using System;
using UnityEngine;

using Game.Shared.Data.ClassBehaviours;
namespace Game.Shared.Runtime.Actors
{
    /// <summary>Exposes actor combat and spell events to shared systems.</summary>
    public class ActorEvents : NetworkBehaviour
    {
        // Server events
        /// <summary>Raised when the actor's auto attack hits.</summary>
        public Action<SpellContext, Actor> onServerAutoAttackHit;
        /// <summary>Raised when the actor is hit by an auto attack.</summary>
        public Action<SpellContext, Actor> onServerAutoAttackTaken;
        /// <summary>Raised when the actor finishes casting a spell.</summary>
        public Action<SpellContext> onServerSpellCastComplete;

        // Client events
        /// <summary>Raised when the actor's auto attack hits on the client.</summary>
        public Action<SpellContext, Actor> onClientAutoAttackHit;
        /// <summary>Raised when the actor is hit by an auto attack on the client.</summary>
        public Action<SpellContext, Actor> onClientAutoAttackTaken;
        /// <summary>Raised when the actor finishes casting a spell on the client.</summary>
        public Action<SpellContext> onClientSpellCastComplete;

        /// <summary>Loads the class behavior event handlers from persisted character data.</summary>
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
