# Ninth Age — Player-versus-Player Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Open-world faction PvP, server PvP rulesets, PvP flagging, Free-For-All PvP, duels, contest-zone objectives, structured PvP support, PvP death, seasonal rank progression, Honor, rewards and PvP-specific balance boundaries  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document defines the intended player-versus-player model for **Ninth Age**.

It is authoritative for:

- the role of PvP in the game;
- two-faction open-world hostility;
- Normal and PvP persistent-world rulesets;
- opt-in PvP flagging;
- always-on PvP worlds;
- Free-For-All PvP;
- duel rules;
- contest-zone world PvP;
- world PvP objectives;
- support requirements for battlegrounds;
- support requirements for arenas;
- structured-PvP instancing;
- PvP eligibility;
- honorable-kill eligibility;
- anti-farming reward rules;
- PvP death/recovery expectations;
- seasonal PvP rank progression;
- Honor and Rank Points;
- PvP reward philosophy;
- PvP-specific balance modifiers;
- cross-faction PvE interaction with PvP.

This document does not define:

- core combat resolution — [Combat System PDD](Combat-System-PDD.md);
- party/raid membership — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- faction reputation/NPC diplomacy — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- guild administration/halls — [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md);
- communication — [Communication Systems PDD](Communication-Systems-PDD.md);
- item stat allocation/general loot — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- world geography — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- detached-map/runtime instance implementation — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- final UI presentation — [UI and UX PDD](UI-and-UX-PDD.md).

Where implementation conflicts with this document, this PDD defines intended product behaviour.

---

## 2. PvP Role

Ninth Age is built around **two primary political factions**.

Player conflict between those factions is a real part of the shared world.

PvP is therefore not an isolated minigame that exists only in queued instances.

The game supports:

- open-world faction PvP;
- consensual duels;
- Free-For-All PvP;
- contest-zone world objectives;
- future battlegrounds;
- future arenas;
- seasonal PvP progression.

The intensity of open-world PvP depends on the persistent world's PvP ruleset.

---

## 3. Design Pillars

### 3.1 Faction conflict should be visible in the world

The two-faction structure must have meaningful player-facing consequences.

Opposing factions can become genuine open-world combatants rather than remaining purely narrative labels.

### 3.2 Server ruleset determines consent model

Normal worlds preserve opt-in open-world PvP.

PvP worlds make open-world faction PvP mandatory.

The same character-combat systems should support both.

### 3.3 PvP worlds are intentionally dangerous

A PvP world does not provide artificial low-level immunity or city sanctuary.

World geography, guards, player behaviour and social organisation provide the consequences.

### 3.4 Attack eligibility and reward eligibility are separate

A player may be a legal PvP target without being worth Honor or Rank Points.

This permits harsh world-PvP rules without incentivising low-level farming.

### 3.5 PvE cooperation remains voluntary across factions

Faction hostility does not prevent players from choosing to cooperate in PvE.

Cross-faction groups remain valid.

### 3.6 Seasonal progression rewards participation without weekly decay punishment

PvP rank progression is seasonal and repeatable.

Progress within a season should not decay merely because a player takes time away.

### 3.7 Structured PvP should be supported without being required at launch

The architecture must support battlegrounds and arenas.

This PDD does not require specific battleground/arena content to ship immediately.

---

## 4. Primary Factions and PvP Hostility

The two primary player factions are politically hostile for ordinary faction PvP.

When two opposing-faction characters are both PvP-eligible:

- they may attack one another;
- hostile-target UI semantics apply;
- PvP combat rules apply.

PvP hostility does not alter either character's persistent faction identity or reputation.

---

## 5. Persistent-World PvP Rulesets

Ninth Age supports at least two persistent-world rulesets:

1. **Normal**
2. **PvP**

The ruleset is a property of the persistent world.

It must be visible to players when selecting/entering that world.

---

## 6. Normal Worlds

On a Normal world:

- characters are not automatically PvP-flagged;
- players may deliberately opt into PvP;
- unflagged opposing-faction players are not ordinary attackable PvP targets;
- players who participate in faction PvP become flagged;
- PvP participation cannot be used to attack and then instantly remove vulnerability.

Normal worlds therefore contain open-world PvP without making it compulsory for ordinary world play.

---

## 7. Manual PvP Flagging

On a Normal world, a player can explicitly enable their PvP flag.

The action must be intentional and clearly reflected in the UI.

Once flagged, the character becomes eligible for ordinary faction PvP.

The exact command/button belongs to UI/Input implementation.

---

## 8. Flagging Through Hostile PvP Action

A character that performs an eligible hostile PvP action against another player must be considered PvP-flagged.

The client cannot avoid reciprocal PvP vulnerability by attempting to initiate combat from an unflagged local state.

Flag state is server-authoritative.

---

## 9. PvP Flag Clearing

A Normal-world player may request to disable voluntary PvP.

The flag does not clear immediately while the character is:

- in PvP combat;
- under a recent-PvP-action lock;
- participating in an active PvP objective;
- otherwise in an explicitly PvP-locked state.

The exact timeout is tuning data.

The governing rule is that the player cannot toggle out of PvP to escape an active/recent encounter.

---

## 10. PvP Worlds

On a PvP world:

- player characters are always PvP-flagged in ordinary open-world play;
- opposing-faction players may attack each other wherever normal combat interaction is otherwise possible;
- characters cannot disable ordinary faction PvP.

The world ruleset itself supplies the flag.

---

## 11. No Low-Level Protection on PvP Worlds

PvP worlds provide **no level-based attack immunity**.

A high-level character may attack a much lower-level eligible enemy character.

The system must not prevent the attack solely because:

- the target is low level;
- the attacker is much higher level;
- the encounter is clearly uneven.

Reward eligibility is handled separately.

---

## 12. No City Sanctuary on PvP Worlds

Cities, towns and villages do not automatically provide player-vs-player immunity on PvP worlds.

An opposing-faction player remains a valid PvP target inside a city if normal world interaction permits it.

There is no protected low-level city status.

---

## 13. Guards Are World Consequences, Not PvP Immunity

Faction guards and NPC defenders may respond to hostile intruders according to the NPC/AI/Faction systems.

For example:

- entering an enemy capital may cause guards to attack;
- attacking local players may attract further NPC hostility.

This does not make the player immune from PvP.

A player may choose to attack inside a heavily defended settlement and accept the resulting danger.

---

## 14. Cross-Faction PvE Grouping

Players from opposing political factions may voluntarily group together for PvE.

This preserves the Group and Raid Systems PDD's cross-faction grouping rules.

Grouping does not change:

- faction identity;
- reputation;
- political diplomacy;
- NPC disposition.

---

## 15. Group Friendliness Overrides Ordinary Faction PvP

While validly grouped for cooperative PvE, party/raid members are treated as friendly to one another for ordinary player combat.

Opposing-faction party/raid members cannot accidentally attack each other through normal faction PvP.

This friendliness lasts only while the group relationship remains valid.

---

## 16. Cross-Faction Groups on PvP Worlds

PvP-world always-flagged status does not prevent cross-faction PvE grouping.

Grouped opposing-faction players remain friendly to their group members.

They remain subject to ordinary PvP from other eligible players outside the protected group/guild relationships defined by this PDD.

---

## 17. Free-For-All PvP

Players may deliberately enable **Free-For-All PvP (FFA PvP)**.

FFA PvP overlays the ordinary faction PvP model.

It represents the player choosing to become hostile beyond normal faction boundaries.

---

## 18. FFA Eligibility

An FFA-flagged player is attackable by another PvP-eligible player who is not protected by an allowed friendly relationship.

FFA therefore allows same-faction PvP.

The other player does not need to be FFA-flagged if they are already otherwise PvP-flagged.

---

## 19. FFA Friendly Exceptions

FFA does not make the player hostile to:

- current party members;
- current raid members;
- members of the same guild.

These relationships remain friendly for ordinary PvP targeting.

Future specifically authored PvP modes may override this only if explicitly defined.

---

## 20. FFA Attack Rights

An FFA-flagged player may attack eligible PvP-flagged players outside their:

- party/raid;
- guild.

This includes same-faction characters.

The relationship is reciprocal: a qualifying flagged player may attack the FFA character.

---

## 21. FFA on Normal Worlds

On a Normal world, FFA activation necessarily makes the player PvP-eligible.

FFA cannot function as a hidden state while the player remains protected from PvP.

---

## 22. FFA on PvP Worlds

On a PvP world, everyone is already ordinarily PvP-flagged.

FFA therefore primarily changes same-faction hostility outside:

- party/raid;
- guild.

---

## 23. FFA Clearing

FFA cannot be disabled instantly to escape an encounter.

The server may clear FFA only after:

- FFA combat has ended;
- the configured recent-PvP lock has expired;
- the character is not in another FFA/PvP locked state.

Exact timing remains tuning data.

---

## 24. FFA Presentation

FFA status must be highly visible to the owning player.

Nearby eligible players must be able to determine that the character is FFA-attackable through appropriate target/nameplate/status presentation.

Exact visual treatment belongs to UI/UX.

---

## 25. Duels

Players can challenge other players to a consensual duel.

Duels are supported regardless of:

- faction;
- Normal/PvP world ruleset;
- ordinary PvP flag state;
- FFA state.

A duel creates a temporary explicit PvP relationship between the two duel participants.

---

## 26. Duel Consent

A duel begins only when the challenged player explicitly accepts.

Supported initiation should include:

- player context-menu action;
- `/duel` or equivalent command.

The duel begins after a short visible countdown.

Exact countdown duration is tuning/UI data.

---

## 27. Duel Boundary

A duel occurs around an agreed world location.

The system should define a duel boundary.

Leaving the boundary triggers a short warning/grace period before forfeiture.

Exact radius and grace time remain tuning values.

---

## 28. Duel Defeat

A duel ends before normal world death.

The losing player is brought to an authored duel-defeat threshold/outcome rather than receiving an ordinary lethal PvP death.

The exact Health threshold can be tuned with the Combat system.

---

## 29. Duel Rewards

Duels do not grant:

- Honor;
- Rank Points;
- honorable-kill credit;
- normal PvP progression.

Duels are for:

- practice;
- social competition;
- roleplay;
- informal tournaments.

This prevents duel farming.

---

## 30. Duel Consequences

A duel does not cause:

- PvP durability loss;
- ordinary death penalties;
- corpse recovery;
- faction reputation changes.

Leaving the boundary or other explicit rule failure may forfeit the duel.

---

## 31. Contest Zones

The world should contain authored **contest zones** where faction conflict is expected and supported by world objectives.

Contest zones are normal parts of the shared world.

They are not automatically detached battleground instances.

---

## 32. World PvP Objectives

Contest zones may contain objectives such as:

- forts;
- towers;
- bridges;
- crossroads;
- mines;
- supply depots;
- strategic ruins;
- other authored military/economic locations.

Objective design should give PvP a relationship to geography and faction conflict.

---

## 33. Normal-World Contest-Zone Participation

On a Normal world, simply entering a contest zone does not inherently need to remove opt-in choice.

However, participating in a PvP objective requires the character to be PvP-flagged.

An unflagged player attempting an objective must either:

- opt in;
- be prompted/flagged by the valid participation action.

The system must not permit objective contribution while retaining unflagged immunity.

---

## 34. PvP-World Contest Zones

On a PvP world, all players are already eligible for ordinary faction PvP.

Contest zones therefore require no additional ordinary PvP flagging.

Their purpose is to focus conflict and provide objectives rather than enable combat that is otherwise impossible.

---

## 35. Contest-Zone Rewards

World PvP objectives may provide:

- Honor;
- Rank Points;
- faction PvP progression;
- local temporary access;
- local services;
- transport advantages;
- resource opportunities;
- faction NPC presence;
- cosmetic/trophy progress;
- other authored strategic benefits.

The system should avoid large mandatory general-purpose PvE combat-stat buffs.

---

## 36. Objective Ownership

World PvP objectives may have a persistent or temporary faction owner.

Objective ownership should be server-authoritative and visible in the world/UI.

Exact:

- capture mechanics;
- reset timings;
- ownership duration;
- defence mechanics;

belong to authored PvP content data.

---

## 37. Battleground Support

Ninth Age architecture must support instanced **battlegrounds**.

A battleground may define:

- team size;
- team composition;
- faction/team rules;
- map;
- objectives;
- score;
- win/loss conditions;
- start/end flow;
- respawn behaviour;
- Honor/Rank rewards;
- matchmaking requirements.

This PDD does not require a specific battleground to be implemented immediately.

---

## 38. Arena Support

Ninth Age architecture must support instanced **arenas**.

An arena may define:

- team size;
- rated/unrated mode;
- map;
- round/match rules;
- elimination/score conditions;
- matchmaking;
- rating changes where rated;
- rewards.

This PDD does not require a specific arena to be implemented immediately.

---

## 39. Structured PvP Runtime

Battlegrounds and arenas are detached-map or otherwise explicitly scoped runtime instances as appropriate.

They fit the World Runtime and Instancing PDD's detached-map architecture.

Structured-PvP state must not leak between simultaneous matches.

---

## 40. Cross-World Structured PvP

Battleground/arena matchmaking must be architecturally capable of using players from multiple persistent worlds.

Structured-PvP queues and matches must not be permanently tied to one open-world server process.

Actual launch implementation may stage this feature.

---

## 41. Structured-PvP Grouping

Structured PvP may temporarily organise players into PvP teams independent of their ordinary political faction where the specific mode requires it.

Faction battlegrounds may instead preserve political-faction teams.

The battleground/arena definition owns this.

Ordinary cross-faction PvE grouping remains unaffected outside the PvP match.

---

## 42. Unrated and Rated Structured PvP

The system should support:

- unrated structured PvP;
- rated structured PvP where appropriate.

A player should not need to participate in rated competition merely to access the underlying PvP mode.

Exact matchmaking/rating algorithm remains a future implementation/tuning decision.

---

## 43. Seasonal PvP Progression

Ninth Age uses a **seasonal PvP rank structure** inspired structurally by WoW Forever.

Seasonal rank progression is separate from permanent character level progression.

A season provides a fresh competitive progression track while preserving permanent historical accomplishments.

---

## 44. PvP Rank Count

The seasonal rank structure uses **14 ranks**.

Each primary faction may use its own authored rank titles.

Both factions use equivalent progression requirements underneath the faction-specific titles.

Exact rank names are content/lore data.

---

## 45. Rank Points

Seasonal progression uses non-spendable **Rank Points**.

Rank Points:

- are earned through qualifying PvP activity;
- determine seasonal rank progression;
- are not a currency;
- cannot be spent;
- do not decay during the active season.

---

## 46. Honor

PvP also uses **Honor** as a spendable PvP currency/reward value.

Honor and Rank Points are separate.

Conceptually:

```text
PvP participation
    ├── Honor
    │     spendable
    │
    └── Rank Points
          seasonal progression
          not spendable
```

Exact Honor vendors/items/costs are content/economy data.

---

## 47. Rank Progression Sources

Rank Points may be earned from:

- honorable player kills;
- contest-zone objectives;
- battleground objectives/results;
- arena participation/results where applicable;
- authored PvP quests/tasks;
- other explicitly approved PvP activities.

Objective contribution should be capable of providing substantial progression.

The system should not reduce PvP rank progression to kill farming.

---

## 48. Progressive Seasonal Rank Cap

The maximum attainable seasonal rank/progress rises over the course of the season.

A player cannot grind directly to the maximum rank immediately at season start.

The system should progressively unlock higher rank ceilings as the season advances.

Exact weekly/periodic cap progression is tuning data.

---

## 49. No Intra-Season Rank Decay

Earned Rank Points do **not** decay during the active season merely because the player:

- takes a week off;
- earns less Honor than other players;
- falls behind a population percentile.

Seasonal progression is against the defined progression track, not a limited fixed-rank population pool.

---

## 50. Seasonal Reset

At the end of a PvP season:

- seasonal Rank Points reset;
- active seasonal rank resets;
- the next season begins a new progression track.

Permanent historical recognition may persist.

---

## 51. Permanent Seasonal History

The game should permanently record appropriate historical PvP accomplishments such as:

- highest rank achieved;
- season identifier;
- seasonal title/cosmetic unlocks;
- achievements;
- trophies.

A seasonal reset should not erase the player's history.

---

## 52. Honor Across Seasons

Whether spendable Honor resets, partially resets or persists between seasons remains balance/economy data.

The seasonal-rank architecture must not depend on Honor and Rank Points being the same value.

---

## 53. PvP Reward Philosophy

PvP rewards should emphasise:

- faction identity;
- prestige;
- cosmetics;
- titles;
- mounts;
- tabards/heraldry;
- weapon/armour appearances;
- trophies;
- guild-hall trophy displays;
- PvP-relevant consumables/services where appropriate;
- combat equipment where appropriate.

PvP should provide meaningful rewards without making maximum seasonal rank mandatory for ordinary PvE progression.

---

## 54. No Mandatory Seasonal PvP Gear Treadmill for PvE

The system should avoid requiring players to repeatedly attain top seasonal PvP rank to obtain best-in-slot general PvE equipment.

PvP equipment may exist.

It should not create a recurring requirement that non-PvP-focused players grind top PvP rank for optimal raid/dungeon performance.

---

## 55. Honorable-Kill Eligibility

Not every legal PvP kill grants PvP progression.

An **honorable kill** is a reward-eligible PvP kill according to server rules.

Eligibility can consider:

- target level relative to attacker;
- repeat-kill state;
- duel/structured-mode context;
- exploit/farming detection;
- participation/contribution.

Exact thresholds are tuning data.

---

## 56. Low-Level Victims and Honor

On PvP worlds, low-level characters remain attackable.

However, killing a sufficiently low-level target relative to the attacker should grant:

- no Honor;
- no Rank Points;
- no honorable-kill credit.

This distinction is deliberate:

> **Attack eligibility does not imply reward eligibility.**

Exact level-gap thresholds remain tuning data.

---

## 57. Repeated-Kill Diminishing Returns

Repeatedly killing the same victim within a defined period should progressively reduce or eliminate:

- Honor;
- Rank Points;
- honorable-kill credit.

The victim remains a legal PvP target where the world rules allow it.

Only progression reward is diminished.

Exact window/curve remains tuning data.

---

## 58. FFA Reward Abuse

FFA does not provide a loophole for same-faction friends to farm PvP progression.

FFA kill rewards must use the same honorable-kill and anti-farming validation.

The server may apply stricter repeated-kill checks to obvious same-faction/FFA farming patterns.

---

## 59. Duel Reward Exclusion

Duel outcomes never count as honorable kills.

They grant no Honor or Rank Points.

This rule is independent of the participants' ordinary PvP/FFA eligibility outside the duel.

---

## 60. PvP Death

Lethal open-world or structured PvP can produce a normal death state where the mode calls for it.

PvP death must not apply durability loss from the PvP death itself.

This avoids turning PvP participation into an excessive repair-cost punishment.

---

## 61. PvP Resurrection

Open-world PvP should use the normal world resurrection framework where appropriate.

The design must not automatically grant special PvP immunity merely because a player:

- has been killed repeatedly;
- is low level;
- resurrects in a city.

Respawn/graveyard placement must still be authored sensibly enough for world functionality.

---

## 62. No Automatic Anti-Camping Immunity

PvP worlds deliberately do not guarantee protection from corpse camping or repeated enemy-player attack through artificial invulnerability.

Anti-abuse systems may still address out-of-game harassment/exploits, but the PvP combat system itself does not create low-level or repeated-death immunity.

---

## 63. Structured-PvP Respawn

Battlegrounds may define their own respawn rules.

Arenas may use elimination/no-respawn or other match-specific rules.

Structured-PvP definitions own these rules.

They do not rewrite open-world death behaviour.

---

## 64. PvP and Durability

Damage/death caused by PvP should not create normal durability degradation solely because another player caused the death.

The Items PDD remains authoritative for general durability rules.

Exact treatment of durability from non-death PvP damage can be implemented consistently with the intended no-punitive-PvP-repair-cost principle.

---

## 65. PvP Balance

PvP uses the normal class/ability/combat system.

Ninth Age does not maintain an entirely unrelated PvP-only class kit by default.

Abilities should retain recognisable behaviour across PvE and PvP.

---

## 66. PvP-Specific Tuning Modifiers

Where required for PvP balance, the system may support explicit PvP-specific modifiers such as:

- duration modifiers;
- damage/healing modifiers;
- diminishing returns;
- target-specific effect rules;
- other clearly authored PvP adjustments.

These modifiers should be used deliberately rather than allowing PvP to silently diverge from PvE.

---

## 67. PvP Balance Transparency

If an ability behaves differently against players, that difference should be discoverable in:

- tooltip;
- PvP rules/reference;
- other clear UI presentation.

Do not rely on hidden PvP coefficients players cannot reasonably understand.

---

## 68. Crowd Control

The PvP system must be capable of supporting PvP-specific crowd-control rules if testing requires them.

Exact:

- diminishing-return categories;
- maximum PvP durations;
- immunity windows;

remain combat-balance decisions and are not assigned arbitrary values here.

---

## 69. Guilds and PvP

Guilds may:

- coordinate faction PvP;
- earn PvP-related trophies/achievements;
- display PvP accomplishments in guild halls.

This PDD does **not** introduce formal guild-war/territorial-war rules.

Guild halls remain social rented properties rather than automatic capturable PvP forts.

---

## 70. Contest-Zone Guild Participation

Guilds can participate collectively in faction objectives through ordinary groups/raids.

World objective ownership normally belongs to the faction/objective system, not automatically to the guild that contributed most.

A future specific content design may create guild-linked objectives only through explicit approval.

---

## 71. Communication During PvP

The Communication PDD remains authoritative.

Cross-faction communication continues to function normally unless a specific structured PvP mode explicitly restricts a channel for match integrity.

PvP hostility does not create a general language barrier.

---

## 72. PvP Flag and Social UI

The client must clearly communicate:

- ordinary PvP flag;
- PvP-world permanent eligibility;
- FFA PvP;
- duel state;
- contest objective participation;
- structured-PvP team state.

Players should not need to infer whether they can be attacked from subtle colour changes alone.

Exact presentation belongs to UI/UX.

---

## 73. Server Authority

The server is authoritative for:

- world PvP ruleset;
- PvP flag state;
- FFA flag state;
- flag-clear timing;
- legal hostile-player targeting;
- group/guild friendliness;
- duel state;
- duel outcome;
- contest objective eligibility;
- honorable-kill eligibility;
- repeated-kill diminishing returns;
- Honor;
- Rank Points;
- seasonal rank;
- structured-PvP match state;
- ratings where used;
- PvP rewards.

Clients may predict/present state but cannot authoritatively grant PvP eligibility/rewards.

---

## 74. Persistence

Persistent PvP state includes, where applicable:

- seasonal Rank Points;
- current seasonal rank;
- spendable Honor;
- highest/historical rank;
- seasonal accomplishments;
- PvP achievements/titles/cosmetics;
- ratings;
- other durable PvP rewards.

Temporary combat/flag timers persist/reconstruct according to the Account/Persistence principle that reconnect/restart must not provide a benefit.

A player must not clear PvP consequences by relogging.

---

## 75. Disconnect and Relog

Disconnecting/relogging must not be a method of:

- instantly clearing PvP combat;
- clearing FFA lock state;
- avoiding a recent-PvP flag timeout;
- escaping a valid duel outcome;
- preventing a legitimate PvP death already resolved by the server.

The Account/Persistence PDD's continuity rules apply.

---

## 76. Structured-PvP Matchmaking Support

Future matchmaking should support inputs such as:

- mode;
- team size;
- rating;
- party size;
- faction/team constraints;
- world/region latency constraints;
- other mode-specific rules.

The exact matchmaker is a technical/future-content decision.

---

## 77. AFK and Deserter Support

Battleground/arena infrastructure should support:

- AFK detection;
- voluntary match leaving;
- deserter/queue penalties where required;
- reconnect to an active match where appropriate.

Exact penalty values are content/tuning decisions.

These systems are not required until structured PvP is implemented.

---

## 78. Spectator Support

Arena/battleground runtime architecture should not preclude future spectator support.

Spectator functionality is not required for initial PvP implementation.

If added, spectators must not receive information that can be used to compromise match integrity beyond the authored spectator rules.

---

## 79. Content Authoring Requirements

World PvP content needs data-driven authoring for:

- contest-zone identity;
- objective locations;
- capture rules;
- ownership;
- eligibility;
- contribution;
- rewards;
- reset/duration;
- faction-specific world state;
- map/UI markers.

Structured PvP definitions need data-driven authoring for:

- map/instance;
- team sizes;
- queue type;
- score;
- objectives;
- respawn rules;
- win conditions;
- rating usage;
- reward tables.

---

## 80. Provisional / Tuning Values

The following remain content/balance values rather than locked architecture:

- Normal-world PvP flag-clear timeout;
- FFA flag-clear timeout;
- duel boundary radius;
- duel boundary grace period;
- duel start countdown;
- exact honorable-kill level-gap threshold;
- repeated-kill diminishing-return window/curve;
- exact Honor values;
- exact Rank Point values;
- exact rank thresholds;
- exact season length;
- exact progressive rank-cap schedule;
- exact faction-specific rank titles;
- Honor persistence/reset policy across seasons;
- structured-PvP team sizes;
- battleground/arena rating formulas;
- AFK/deserter timings;
- individual world PvP objectives/rewards;
- PvP-specific ability tuning.

These values can change without redesigning the PvP architecture.

---

## 81. Locked Design Decisions

The following decisions are locked by this PDD:

1. Ninth Age has two primary player factions with meaningful open-world PvP hostility.
2. Persistent worlds support Normal and PvP rulesets.
3. Normal worlds use opt-in open-world PvP.
4. PvP worlds permanently flag players for ordinary open-world faction PvP.
5. PvP-world players cannot disable ordinary faction PvP.
6. PvP worlds have no level-based attack protection.
7. PvP worlds have no automatic city/town/village PvP sanctuary.
8. Enemy-faction guards may still punish intruders through normal world/AI rules.
9. Attack eligibility and Honor/Rank reward eligibility are separate concepts.
10. Cross-faction PvE grouping remains supported.
11. Valid party/raid members are friendly to each other for ordinary PvP even when politically opposed.
12. Players can opt into Free-For-All PvP.
13. FFA allows same-faction PvP outside party/raid and guild relationships.
14. FFA characters are attackable by qualifying PvP-flagged non-party/non-raid/non-guild players.
15. FFA status cannot be toggled off immediately to escape combat.
16. Duels are supported across factions and server rulesets.
17. Duels require explicit consent.
18. Duels do not grant Honor, Rank Points or honorable-kill credit.
19. Duel defeat does not use ordinary lethal world-death punishment.
20. Contest zones and world PvP objectives are part of the world design.
21. On Normal worlds, world-objective participation requires opting into PvP.
22. Battleground architecture is supported but specific battleground content is not required immediately.
23. Arena architecture is supported but specific arena content is not required immediately.
24. Structured PvP can operate cross-world.
25. PvP uses a seasonal rank progression structure.
26. The seasonal rank structure has 14 ranks.
27. Factions may have different rank titles over equivalent underlying progression.
28. Rank Points are seasonal, non-spendable progression.
29. Rank Points do not decay during an active season.
30. The maximum attainable seasonal rank/progress rises as the season advances.
31. Seasonal Rank Points and active rank reset at season end.
32. Historical seasonal accomplishments persist.
33. Honor is separate from Rank Points and is spendable.
34. Honorable kills can be limited by level difference and anti-farming validation.
35. Low-level characters remain attackable on PvP worlds even when they grant no Honor/Rank Points.
36. Repeated kills of the same victim diminish/remove PvP progression rewards.
37. Duels cannot be used to farm progression.
38. PvP death does not cause durability loss from the death itself.
39. PvP worlds do not automatically grant repeated-death or low-level PvP immunity.
40. PvP uses the normal combat/class ability system.
41. Explicit PvP-specific balance modifiers are allowed where needed.
42. PvP-specific behaviour should be transparent to players.
43. PvP rewards should not force maximum seasonal PvP rank as a recurring requirement for ordinary PvE optimisation.
44. Formal guild-war/territorial-war rules are not part of the current PvP design.
45. Guild halls are not automatically capturable PvP objectives.
46. Cross-faction communication remains allowed.
47. PvP flag/reward/match state is server-authoritative.
48. Disconnect/relog must not clear PvP consequences in an exploitable way.

---

## 82. Dependencies

This PDD depends on or constrains:

- [MMORPG Master PDD](MMORPG-Master-PDD.md);
- [Combat System PDD](Combat-System-PDD.md);
- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md);
- [Communication Systems PDD](Communication-Systems-PDD.md);
- [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md);
- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md);
- [UI and UX PDD](UI-and-UX-PDD.md).

---

## 83. Validation Criteria

The PvP design is correctly implemented when:

1. A Normal-world player begins ordinary world play without forced PvP eligibility.
2. A Normal-world player can deliberately enable PvP.
3. A recent/active PvP participant cannot instantly clear their flag to avoid retaliation.
4. A PvP-world player is always ordinarily PvP-eligible in the open world.
5. A Level 60 character can legally attack a much lower-level enemy character on a PvP world.
6. Entering a city on a PvP world does not create player-vs-player immunity.
7. City guards can still react through normal NPC hostility.
8. Opposing-faction PvP-eligible players can attack each other.
9. Two opposing-faction players can voluntarily group for PvE.
10. Those grouped players are friendly to each other while validly grouped.
11. Grouping does not change either character's persistent faction/reputation.
12. A player can enable FFA PvP.
13. An FFA player can fight an eligible same-faction non-group/non-guild player.
14. A qualifying PvP-flagged same-faction player can attack an FFA player.
15. Party/raid members remain friendly under ordinary FFA rules.
16. Guild members remain friendly under ordinary FFA rules.
17. FFA cannot be disabled instantly during/recently after PvP combat.
18. Two players can start a consensual duel regardless of faction/world ruleset.
19. Duel completion grants no Honor/Rank Points.
20. A duel can end through defeat/forfeit without ordinary lethal-death penalties.
21. A contest-zone objective can require/produce PvP participation on a Normal world.
22. A player cannot contribute to a PvP objective while preserving unflagged immunity.
23. A contest objective can track authoritative faction ownership.
24. The runtime can represent an isolated battleground match.
25. The runtime can represent an isolated arena match.
26. Structured-PvP architecture can source players from more than one persistent world.
27. Seasonal PvP progression contains 14 ranks.
28. Rank Points can increase without being spent.
29. Rank Points do not decay merely because a player skips a week.
30. The season can enforce a progressively rising maximum attainable rank.
31. Season rollover resets active Rank Points/rank.
32. Historical highest/seasonal achievements persist after reset.
33. Honor can exist separately from Rank Points.
34. A legal low-level kill can grant zero Honor/Rank Points without making the target immune.
35. Repeated kills of the same victim can become unrewarded.
36. FFA kills cannot trivially bypass anti-farming validation.
37. PvP death itself does not apply durability loss.
38. Relogging does not instantly clear recent PvP state/reward consequences.
39. PvP-specific ability modifiers can be authored and surfaced clearly.
40. PvE content remains playable by voluntary cross-faction groups.
41. Guild halls remain non-capturable unless a future explicit design changes that rule.
42. A battleground/arena implementation can be added later without redesigning the open-world PvP model.

---

## 84. Design Summary

Ninth Age treats faction conflict as part of the shared world rather than only as queued side content.

**Normal worlds make open-world PvP optional. PvP worlds make it unavoidable.**

PvP worlds deliberately provide no low-level immunity and no city sanctuary. World consequences such as faction guards still matter, but the rules do not artificially protect a player from another eligible player.

Cross-faction PvE remains valid, allowing political enemies to cooperate deliberately without erasing their faction identities.

FFA PvP allows players to extend hostility to otherwise friendly faction members while protecting their party/raid and guild.

Duels provide consensual practice without progression exploitation.

Contest zones create geographically meaningful faction conflict, while the runtime remains capable of supporting future battlegrounds and arenas.

Seasonal PvP progression uses a 14-rank non-decaying Rank Point track with progressively unlocked seasonal rank ceilings, a separate spendable Honor value and persistent historical recognition.

The intended result is:

> **Faction conflict should make the world dangerous and politically meaningful without preventing players from choosing cooperation when they want it.**
