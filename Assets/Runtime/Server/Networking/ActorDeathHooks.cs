using System.Collections.Generic;
using System.Linq;
using Game.Server.Services;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.Networking
{
    /// <summary>
    /// Server-only hooks related to actor deaths, such as handling experience distribution, 
    /// loot drops, and death notifications.
    /// </summary>
    public static class ActorDeathHooks
    {
        public static void RegisterHooks()
        {
            Game.Shared.Networking.ActorService.onActorDeath += OnActorDeath;
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
                                    if (killObjective.TargetNpcID == (actor as NPCActor).NpcID)
                                    {
                                        quest.Progress[quest.Quest.Objectives.ToList().IndexOf(objective)] += 1;
                                        updateQuest = true;
                                    }
                                }
                            }

                            if (updateQuest)
                            {
                                CharacterService.Client_QuestUpdated(playerActor.Owner.Value, quest.Quest.DefinitionId, quest.Progress, quest.IsCompleted);
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