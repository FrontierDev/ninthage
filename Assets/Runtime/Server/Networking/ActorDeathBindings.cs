using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Services;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using System.Collections.Generic;
using System.Linq;

using Game.Shared.Runtime.Actors;
using Game.Shared.Data.QuestObjectives;
namespace Game.Server.Networking
{
    /// <summary>
    /// Server-only bindings related to actor deaths, such as handling experience distribution,
    /// loot drops, and death notifications.
    /// </summary>
    public static class ActorDeathBindings
    {
        /// <summary>
        /// Registers the server actor-death handler with the shared actor endpoint.
        /// </summary>
        public static void Register()
        {
            ActorEndpoint.onActorDeath += OnActorDeath;
        }

        private static void OnActorDeath(Actor actor)
        {
            if (actor.TryGetComponent<NPCBehavior>(out var npcBehavior))
            {
                var baseExperience = npcBehavior.BaseExperience;
                foreach (var kvp in npcBehavior.ThreatTable)
                {
                    var playerActor = kvp.Key;
                    CharacterExperienceService experienceService =
                        new(playerActor, baseExperience);

                    // For now, we're just going to roll the loot and give it directly to each player 
                    // in the threat table. This is obviously not correct and will have to be reworked
                    // once the party system is impelemented, but it will work for testing purposes.
                    PartyLootService lootService = new(playerActor);
                    lootService.AddLootFromSource(npcBehavior.LootTable);
                    lootService.DistributeLoot();

                    // Does this actor count towards a quest?
                    var playerQuests = playerActor.GetComponent<PlayerQuests>();
                    if (playerQuests != null)
                    {
                        foreach (var quest in playerQuests.ActiveQuests)
                        {
                            bool updateQuest = false;

                            foreach (var objective in quest.Quest.Objectives)
                            {
                                if (objective is Quest_KillObjective killObjective)
                                {
                                    if (killObjective.TargetNPCID == (actor as NPCActor).NPCID)
                                    {
                                        quest.Progress[quest.Quest.Objectives.ToList().IndexOf(objective)] += 1;
                                        updateQuest = true;
                                    }
                                }
                            }

                            if (updateQuest)
                            {
                                CharacterEndpoint.Client_QuestUpdated(playerActor.Owner.Value, quest.Quest.DefinitionID, quest.Progress, quest.IsCompleted);
                            }
                        }
                    }
                }

                // Pass to NPC behaviour to play animations.
                npcBehavior.onDeath?.Invoke();
            }
        }
    }
}
