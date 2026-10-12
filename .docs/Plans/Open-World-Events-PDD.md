# Ninth Age — Open-World Events Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Ambient shared-world activity, local event state, profession-linked permit activities, world bosses, encounter chains, server-wide boss availability and crisis events  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines how **shared open-world events and large open-world encounters** should exist within Ninth Age.

The term *event* in this document does not imply a conventional MMORPG public-event presentation.

Ninth Age should avoid turning the open world into a sequence of heavily telegraphed activities that interrupt the player and demand participation.

This document is authoritative for:

- ambient shared-world events;
- how those events are presented to players;
- local event triggering and lifecycle;
- local event success/failure state;
- event chaining at a high level;
- profession-linked shared activities where they interact with world-event infrastructure;
- open-world elite encounter availability;
- world bosses;
- discovery and triggering of world bosses;
- server/world-wide world-boss state;
- broad world-boss reward philosophy;
- crisis-event structure;
- the proposed **Ancient Crisis** concept.

This document does not define:

- detailed profession progression or permit acquisition — [Crafting System PDD](Crafting-System-PDD.md);
- faction-reputation thresholds that grant permit-zone access — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- NPC behaviour — [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- individual boss mechanics — encounter-specific content;
- detailed loot generation/distribution — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- PvP flagging and combat rules — [PvP PDD](PvP-PDD.md);
- quests/dialogue used to reveal or contextualise events — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- technical world streaming or persistence — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md).

---

## 2. Design Pillars

### 2.1 Events are things happening in the world, not activities announced by the game

The central rule is:

> **Open-world events should normally be discovered because the player encounters them, not because the UI tells the player to go and do them.**

An event should first appear as a believable occurrence in the world.

Examples include:

- a woodland fort being attacked;
- a patrol being ambushed;
- creatures overrunning a local location;
- workers defending a quarry;
- an unusual elite appearing;
- a dormant encounter becoming active.

The player may choose to intervene or continue with what they were already doing.

### 2.2 The world should not constantly demand attention

Open-world events must not routinely use:

- zone-wide event banners;
- automatic event trackers;
- large intrusive countdowns;
- mandatory-looking objective lists;
- persistent map-wide alerts;
- UI messaging designed to make players feel they are missing content;
- constant calls to abandon their current activity.

Exceptional systems may justify stronger communication, but this is not the default.

### 2.3 World activity should feel local

Most events should affect a believable local area rather than transforming an entire zone.

Events should enrich the impression that the world continues to function without turning zones into rotating event maps.

### 2.4 Discovery should matter

Players should gain value from:

- travelling;
- observing;
- learning locations;
- talking to other players;
- noticing NPC behaviour;
- following clues;
- remembering previous encounters.

Not every important event needs to be immediately represented as a map icon.

### 2.5 Large encounters should feel significant

World bosses should be:

- intimidating;
- dangerous;
- comparatively uncommon;
- memorable;
- worth organising around.

They should not feel like routine map markers on a short rotation.

---

# 3. Event Categories

The shared event framework should be capable of supporting several distinct categories without forcing all of them into the same presentation model.

Broad categories include:

1. **Ambient Local Activity**
2. **Profession / Permit Activities**
3. **Open-World Elite Encounters**
4. **World Boss Encounters**
5. **Encounter Chains**
6. **Crisis Events**

These categories may share technical foundations while retaining different gameplay rules.

---

# 4. Ambient Local Activity

Ambient events are small or medium-scale occurrences that make locations feel active.

Examples include:

- a fort periodically attacked by hostile NPCs;
- a merchant convoy being ambushed;
- guards fighting creatures near a road;
- a village defending itself from raiders;
- predators attacking livestock;
- a patrol encountering enemies.

The event should ordinarily be understandable simply by observing what is happening.

The game should not require a special event panel to explain that a fort is under attack when the player can see attackers assaulting the fort.

---

# 5. Ambient Event Presentation

Normal ambient events should not automatically interrupt the player's UI.

When a player approaches an active event, presentation may come from:

- visible combat;
- NPC speech;
- alarms or bells;
- environmental sound;
- altered NPC behaviour;
- local objects or effects;
- contextual dialogue;
- other diegetic cues.

A player who never approaches the event does not need to know that it happened.

Local UI may be used where an interaction genuinely needs it, but the event should not default to an MMO-style public-event overlay.

---

# 6. Event Discovery

Ambient events are normally **proximity-discovered**.

A player learns about an event by being sufficiently close to observe it or by receiving believable in-world information.

Possible discovery methods include:

- direct observation;
- NPC dialogue;
- a nearby warning or alarm;
- quest information;
- player communication;
- finding aftermath or evidence;
- following an encounter chain.

Discovery does not inherently create a permanent objective in the player's UI.

---

# 7. Event Scheduling

Ambient activity may use predictable or semi-predictable schedules internally.

For example, a woodland fort might face an attack approximately once per hour.

The internal schedule does not need to be exposed to the player.

The world may therefore have regular activity without presenting it as a visible timetable.

Exact schedules are content-specific.

---

# 8. Event Lifecycle

A local event may conceptually move through states such as:

```text
Inactive
   ↓
Preparing / Eligible
   ↓
Active
   ↓
Succeeded / Failed / Naturally Ended
   ↓
Recovery
   ↓
Inactive
```

Not every event requires every state.

The event should restore or transition its local area coherently after completion.

---

# 9. Participation

Ambient events should not require players to formally enrol before helping.

If the event supports player participation, appropriate actions may count automatically.

Participation may include:

- defeating hostile NPCs;
- protecting an NPC or object;
- healing participating allies;
- completing an event-specific interaction;
- gathering or delivering resources;
- fulfilling profession-specific actions.

The exact contribution model is deliberately not universal.

A small fort defence does not need the same participation accounting as a world boss or communal quarry.

---

# 10. Failure

Open-world events are allowed to fail.

Failure may result in believable local consequences such as:

- defenders being defeated;
- attackers temporarily occupying a location;
- an NPC becoming unavailable for a period;
- a later part of a chain changing;
- the event entering a recovery state.

Failure should not routinely impose disruptive zone-wide punishment on uninvolved players.

Permanent or major world-state consequences require explicit content design rather than being the default event behaviour.

---

# 11. Locality and Environmental Change

Ordinary events should avoid drastically transforming entire zones.

Temporary local changes are appropriate where they make sense.

Examples include:

- a fort gate closing during an attack;
- hostile NPCs temporarily occupying a camp;
- fires or damage appearing during an assault;
- local defenders moving to combat positions.

The normal world should remain recognisable.

Large-scale or persistent changes are exceptional and require explicit narrative/world-state design.

---

# 12. Relationship to Encounter-Area Readiness

Ambient events may interact with the **Normal / Alert / Fearful** encounter-area readiness system defined by the AI and Encounter Behaviour PDD.

Examples:

- an attack places a fort into Alert;
- severe defender losses make civilians Fearful;
- successful defence returns the area toward Normal.

The event system determines that something happened.

The AI system determines how participating NPCs react to the resulting readiness state.

---

# 13. Profession and Permit Activities

Some profession activities already behave similarly to public events.

The Crafting System PDD defines communal activities such as **quarries** that are:

- communal;
- noncompetitive;
- timed;
- permit- and/or faction-gated where appropriate;
- personally rewarding to participants;
- progressed more quickly by additional participants subject to a cap.

These activities may use the shared open-world event framework.

However, this PDD does not take ownership of:

- gathering progression;
- permit acquisition;
- profession requirements;
- quarry depth/reward rules;
- profession-specific participation mechanics.

Those remain owned by profession design.

---

# 14. Permit Zones

Permit zones may contain shared profession activity that changes over time or through collective player participation.

Permit access may depend on systems such as:

- faction reputation;
- profession progression;
- authored permits;
- other profession-specific requirements.

The existing faction data already supports reputation benefits granting access to low-, medium- and higher-tier permit zones.

From an event-system perspective, permit-zone activity should:

- exist naturally in the world;
- avoid competitive first-hit ownership;
- permit multiple eligible players to participate;
- avoid intrusive zone-event presentation;
- respect the profession system's own contribution and reward rules.

A permit zone is not automatically an open-world combat event.

---

# 15. Open-World Elite Encounters

Difficult open-world elites may exist outside dungeon instances.

They may be:

- continuously present;
- on a server/world-wide cooldown;
- triggered;
- discoverable;
- part of an event chain.

They remain shared-world actors.

The Dungeon and Group Content PDD establishes that appropriate open-world elites may use server-wide availability cooldowns.

Exact cooldowns are content-specific.

---

# 16. World Bosses

World bosses are major shared-world PvE encounters.

They should feel substantially more intimidating than ordinary open-world elites.

A world boss should normally demand:

- multiple players;
- meaningful coordination;
- appropriate character power;
- understanding of encounter mechanics.

World bosses should not be designed as ordinary elites with inflated health.

---

# 17. World-Boss Discovery

World bosses need not all appear as obvious permanent map markers.

A world boss may be:

- encountered naturally while exploring;
- found in a known but remote location;
- revealed through clues;
- discovered through quests or dialogue;
- exposed by interacting with the environment;
- summoned;
- triggered by another encounter;
- unlocked through an encounter chain.

This supports exploration and player knowledge.

Information about known world-boss locations and triggers may naturally spread through the player community.

---

# 18. World-Boss Triggering

Some world bosses may require deliberate activation.

Potential triggers include:

- interacting with an object;
- defeating prerequisite enemies;
- completing a local encounter;
- assembling items;
- using a profession;
- performing a ritual;
- reaching the end of an encounter chain.

Triggering rules are encounter-specific.

A triggered boss should still exist as a shared event on that persistent world rather than becoming a private copy for the triggering group unless another PDD explicitly defines otherwise.

---

# 19. World-Boss Availability

World-boss availability is shared at the persistent-world/server level.

If a boss is defeated or consumed by a trigger:

- it is not independently available to another party on the same world immediately afterward;
- its cooldown or next eligibility belongs to the shared world state.

This reinforces the idea that the boss is a real creature or encounter within that world rather than private repeatable content.

Exact availability/cooldown periods remain content-specific.

---

# 20. World-Boss Scaling

World bosses may support encounter scaling where required, but scaling is not assumed to be unlimited.

The encounter must retain:

- a meaningful minimum participation expectation;
- mechanical integrity;
- the ability to threaten players.

The exact scaling model, if any, remains deferred.

World bosses should not become trivial merely because only a small number of players are present.

---

# 21. World-Boss Rewards

World bosses should use **more limited loot tables** than comparable dungeon or raid content.

The intent is that a particular boss has a recognisable set of rewards rather than acting as a broad substitute for instanced content.

World-boss items should generally be comparable in power to appropriate same-level dungeon or raid rewards.

Conceptually:

```text
World Boss
    ↓
narrower, more identifiable loot table
    ↓
item power broadly appropriate to comparable
dungeon / raid progression
```

World bosses should therefore be worthwhile without automatically making comparable dungeons or raids obsolete.

Exact:

- drop quantities;
- eligibility;
- distribution;
- item levels;
- rarity;
- cooldown-based reward restrictions;

belong to the Items, Equipment and Loot PDD and later balance work.

---

# 22. Encounter Chains

Open-world encounters may be chained.

A chain means that one in-world outcome reveals, enables or changes another.

Examples include:

```text
discover unusual site
      ↓
defeat guardian
      ↓
recover / activate object
      ↓
new encounter becomes available
      ↓
world boss revealed
```

or:

```text
local event
      ↓
NPC survives
      ↓
information / route becomes available
      ↓
boss trigger discovered
```

Encounter chains should reinforce discovery rather than operate primarily as UI-driven event sequences.

---

# 23. Chain Visibility

The game does not need to display an explicit event-chain progress bar.

Players may understand chains through:

- environmental changes;
- dialogue;
- items;
- clues;
- newly available interactions;
- observable NPC actions.

Where quest content intentionally formalises a chain, the Quest PDD may provide more explicit tracking.

The event system itself should not assume that every chain is a quest.

---

# 24. Server/World-Wide State

Shared open-world events may maintain world-specific runtime state.

Examples include:

- whether a boss is available;
- whether a trigger has been consumed;
- current stage of a chain;
- whether a local attack is active;
- local event cooldown;
- active crisis state.

The persistence requirements vary by event.

A short local fort attack may not need to survive a full server restart.

A significant chained world-boss unlock or crisis may require stronger persistence.

Exact persistence belongs to the content definition and persistence architecture.

---

# 25. Crisis Events

A **Crisis Event** is an exceptional high-impact open-world event.

Unlike ordinary ambient events, a crisis may deliberately:

- attract wider player attention;
- travel across a larger area;
- threaten otherwise unrelated NPCs;
- create significant PvE danger;
- interact with PvP;
- remain active for an extended period.

Crisis Events are not the template for ordinary world activity.

They are intentionally exceptional.

---

# 26. Ancient Crisis

**Ancient Crisis** is the current named concept for a future player-triggered crisis system.

The current design direction is:

1. a player reaches maximum **Archaeology** progression;
2. through Archaeology, the player gains the ability to create a legendary artefact associated with the crisis;
3. activating the artefact begins an interruptible process;
4. the archaeologist and explicitly participating conspirators become exposed to PvP intervention;
5. other players can attempt to prevent the activation;
6. if the activation succeeds, a powerful crisis boss is unleashed into the shared world;
7. the boss roams rather than remaining inside a conventional boss arena;
8. it attacks players and NPCs indiscriminately;
9. the wider player population may organise to destroy it.

The detailed Archaeology profession does not yet exist in the Crafting System PDD and must be designed separately before implementation.

---

# 27. Ancient Crisis — Intended Gameplay Loop

Conceptually:

```text
Maximum-skill Archaeologist
        ↓
acquires knowledge / rare materials
        ↓
creates legendary crisis artefact
        ↓
begins activation
        ↓
Archaeologist + declared conspirators
become valid PvP targets
        ↓
other players may intervene
        ↓
┌──────────────────┴──────────────────┐
│                                     │
activation prevented            activation succeeds
│                                     │
crisis prevented                crisis boss released
                                      ↓
                              boss roams the world
                                      ↓
                              server-wide PvE response
                                      ↓
                                  boss defeated
```

This creates a deliberate hybrid of:

- profession progression;
- exploration/acquisition;
- PvP conflict;
- emergent social organisation;
- large-scale PvE.

---

# 28. Ancient Crisis — Conspirators

Conspirators must be explicit participants.

A player should not be able to meaningfully assist the summoner while avoiding the intended PvP exposure simply because they were not the character using the artefact.

The detailed rules for:

- declaring conspirators;
- joining/leaving the conspiracy;
- party/raid requirements;
- PvP eligibility;
- assistance restrictions;

belong to the PvP and group-system designs.

This PDD locks only the principle that meaningful conspirators share the risk of the attempt.

---

# 29. Ancient Crisis — Interruptible Activation

Crisis activation should not be instantaneous.

The attempt should create an opportunity for other players to discover and stop it.

The exact implementation is deferred, but may involve:

- a ritual;
- an excavation;
- activation stages;
- defending a location;
- assembling or powering an object.

The intervention window is fundamental to the hybrid PvE/PvP concept.

---

# 30. Ancient Crisis — Roaming Boss

If successfully released, the crisis boss should not simply wait at its spawn location.

It should be capable of roaming through appropriate world spaces.

It may:

- kill ordinary NPCs;
- attack players;
- attack guards;
- pass through settlements or routes where appropriate;
- generate fear/alert behaviour in nearby encounter areas;
- create emergent player responses.

The roaming behaviour must remain compatible with world streaming, navigation and server-authoritative AI.

The exact route model is deferred.

---

# 31. Ancient Crisis — Presentation

The crisis is exceptional enough that stronger world communication may eventually be appropriate.

However, even a crisis should originate from an in-world occurrence rather than simply appearing as an arbitrary scheduled UI event.

Possible communication mechanisms may include:

- NPC reactions;
- bells or alarms;
- rumours/dialogue;
- environmental signs;
- player communication;
- limited exceptional UI if later justified.

The exact crisis-notification level remains open.

The ordinary-event prohibition on intrusive UI remains unchanged.

---

# 32. Ancient Crisis — Incentives

The Ancient Crisis requires deliberate incentive design.

There must be meaningful reasons for players to:

- create the crisis artefact;
- become a conspirator;
- oppose the activation;
- fight the crisis boss if released.

The system must not reduce to consequence-free griefing.

Potential reward categories may eventually include:

- Archaeology progression or prestige;
- unique Archaeology rewards;
- rare materials;
- achievements;
- titles;
- boss rewards;
- prevention rewards;
- other progression.

No exact rewards are locked by this PDD.

Reward design must avoid creating a dominant incentive where players always intentionally allow activation because stopping it is economically irrational.

---

# 33. Crisis Frequency

Crisis Events must be rare enough to remain significant.

The system should prevent repeated crisis activation from turning the world into a permanently disrupted state.

Possible controls may include:

- expensive/rare activation requirements;
- server-wide cooldowns;
- one active crisis at a time;
- lengthy acquisition chains;
- other explicit gating.

The exact frequency controls remain deferred.

---

# 34. Crisis Failure and Resolution

A crisis must have an explicit resolution model.

At minimum:

- preventing activation ends the attempted crisis;
- defeating the released crisis boss ends the active boss phase.

Further consequences may depend on the specific crisis.

The game should return affected areas to valid world state after resolution.

Longer-lived narrative consequences require explicit content authoring.

---

# 35. Event Rewards

Ordinary ambient events do not require large standalone reward packages merely because they are events.

Rewards should fit the activity.

An ambient fort defence may reward through:

- NPC access;
- quest progress;
- local loot;
- reputation;
- ordinary combat rewards;
- no special event reward at all.

The absence of a special event chest or currency is acceptable.

The event should not need an artificial reward structure merely to justify its existence.

---

# 36. Participation and Reward Eligibility

There is no single universal open-world contribution algorithm locked by this PDD.

Different content may require different rules.

For example:

- a communal quarry uses profession-specific shared progression and personal rewards;
- an ambient fort attack may require little or no formal participation tracking;
- a world boss requires robust reward eligibility;
- a crisis may require separate rewards for initiators, defenders and boss participants.

Contribution should therefore be defined by the content category rather than forcing all open-world activity into one generic progress-bar model.

---

# 37. Player Count and Scaling

Normal ambient events should not aggressively reshape themselves simply because more players arrive.

Some events may support bounded scaling where needed.

Profession communal activities may already scale progress rate according to their own rules.

World bosses and crises may require larger-scale participation handling.

The exact player-count scaling rules remain content-specific.

---

# 38. Event Authoring

Designers should be able to author a shared-world event through reusable data rather than bespoke code for every occurrence.

An event definition should be able to specify or reference:

- stable event identity;
- category;
- location or encounter area;
- eligibility/trigger conditions;
- activation schedule or trigger;
- participating NPC groups;
- encounter definition;
- start state;
- success conditions;
- failure conditions;
- end conditions;
- recovery/cooldown;
- chained follow-up events;
- world-state persistence requirement;
- optional profession integration;
- optional quest integration;
- optional reward definition;
- presentation policy.

The exact ScriptableObject/component architecture belongs to technical design.

---

# 39. Presentation Policy as Authored Data

Event presentation should be explicit rather than assuming all events announce themselves.

A content definition should be able to distinguish concepts such as:

- **Diegetic Only**
- **Local Context**
- **Exceptional / Wider Notification**

The exact enum or implementation is not locked.

The important rule is that ordinary events default toward low-intrusion presentation.

---

# 40. Integration with World Streaming

Events may cross technical world-content boundaries without being defined by those boundaries.

An event's logical area must remain independent from:

- terrain tiles;
- floating-origin cells;
- additive-scene boundaries.

Active event actors follow the server-authoritative actor/runtime model.

An event should not reset merely because one nearby environment tile unloads for one client.

---

# 41. Integration with AI

Event definitions may influence:

- encounter-area readiness;
- NPC goals;
- encounter groups;
- boss phases;
- scripted actions;
- retreat/reset state.

They should reuse the AI and Encounter Behaviour framework.

A world event must not create a parallel NPC AI system.

---

# 42. Integration with Quests

A world event may have related quests.

A quest may:

- direct a player toward an event;
- explain the event;
- react to success/failure;
- form part of an encounter chain.

However, not every event is a quest.

The event must still make sense as world activity when encountered independently.

---

# 43. Open Design Decisions

The following remain deliberately unresolved:

- exact ambient-event schedules;
- exact event cooldowns;
- event persistence across full server restarts;
- whether/how event state is exposed on the map;
- exact local-context UI;
- formal contribution rules for ordinary combat events;
- ambient-event reward conventions;
- world-boss minimum player expectations;
- world-boss scaling formulas;
- world-boss cooldown lengths;
- exact world-boss loot quantity and reward eligibility;
- exact encounter-chain structures;
- full permit-zone design beyond existing profession/faction principles;
- Archaeology profession design;
- Ancient Crisis artefact recipe/acquisition;
- Ancient Crisis activation duration;
- Ancient Crisis location restrictions;
- crisis PvP flag rules;
- conspirator declaration rules;
- crisis boss route behaviour;
- crisis notification;
- crisis cooldown;
- crisis rewards and penalties;
- whether multiple different crises may coexist.

These require later system-specific design and balance work.

---

# 44. Locked Design Decisions

The following decisions are locked by this PDD:

1. Open-world events normally appear as things happening in the world rather than UI-announced activities.
2. Ninth Age should avoid GW2-style or modern zone-event presentation that repeatedly prompts players to participate.
3. Ordinary events should not automatically produce zone-wide banners, automatic objective tracking or persistent calls to action.
4. Most ambient events are discovered locally through proximity or in-world information.
5. A player elsewhere in the zone does not inherently need to know an ambient event is occurring.
6. Ordinary events should generally affect believable local areas rather than drastically transforming whole zones.
7. Ambient events may run on internal schedules without exposing those schedules to players.
8. Ambient events may succeed or fail without requiring player participation.
9. Event failure should not routinely punish uninvolved players across an entire zone.
10. Event state may interact with AI encounter-area readiness.
11. Profession/permit activities may use the shared event framework without transferring profession-system ownership into this PDD.
12. Communal profession activities remain noncompetitive where their profession design says so.
13. Permit-zone activity should not require intrusive event presentation.
14. Open-world elites may use server/world-wide cooldowns.
15. World bosses are shared-world encounters, not private group instances.
16. World bosses should be intimidating and challenging.
17. World bosses may be naturally discoverable, revealed, summoned or otherwise triggered.
18. World bosses may form part of encounter chains.
19. World-boss availability/cooldowns are shared across the persistent world.
20. World-boss loot tables should generally be narrower than comparable dungeon/raid loot tables.
21. World-boss rewards should broadly occupy the appropriate power range of comparable same-level dungeon/raid content.
22. Encounter chains need not expose explicit UI progress.
23. Crisis Events are exceptional high-impact world events and are not the template for ordinary ambient activity.
24. Ancient Crisis is a supported future crisis-event concept.
25. Ancient Crisis is tied conceptually to future maximum-level Archaeology progression and a legendary artefact.
26. Ancient Crisis activation should be interruptible.
27. The archaeologist and meaningful declared conspirators share the intended PvP exposure of the attempt.
28. Other players may intervene to prevent Ancient Crisis activation.
29. Successful Ancient Crisis activation unleashes a powerful shared-world boss.
30. The crisis boss should roam through appropriate world areas and attack players/NPCs indiscriminately.
31. Ancient Crisis is intended as hybrid profession/PvP/PvE emergent gameplay.
32. Ancient Crisis requires meaningful incentives for initiators, conspirators, defenders and boss participants.
33. The crisis reward model must not make deliberate non-intervention the universally rational choice.
34. Crisis frequency must be constrained so crisis state remains exceptional.
35. Open-world events reuse the existing NPC, AI, world-runtime, combat and loot systems rather than creating parallel implementations.

---

# 45. Dependencies

This PDD depends on or constrains:

- **World and Zone Design PDD** — event locations, local geography and preservation of world identity;
- **World Runtime and Instancing PDD** — persistent-world state, actor simulation and streaming;
- **NPC and Creature Design PDD** — event NPCs, elites and bosses;
- **AI and Encounter Behaviour PDD** — encounter state, readiness, boss AI and scripted behaviour;
- **Dungeon and Group Content PDD** — world-boss relationship to instanced content and crisis concept;
- **Combat System PDD** — combat participation and resolution;
- **Crafting System PDD** — communal gathering activities, quarries and permit-gated profession content;
- **Factions and Reputation PDD** — permit access and faction consequences;
- **Items, Equipment and Loot PDD** — world-boss/event rewards;
- **Quest, Narrative and Dialogue PDD** — event narrative, clues and quest integration;
- **PvP PDD** — Ancient Crisis PvP exposure/intervention;
- **Group and Raid Systems PDD** — large-group organisation and crisis conspirators;
- **Account, Character and Persistence PDD** — persistence of important world-event state.

---

# 46. Validation Criteria

The system satisfies this PDD when:

1. A local world event can occur without notifying every player in the zone.
2. A player can discover an event simply by encountering it.
3. An ambient event can communicate its state through NPC/environment behaviour rather than mandatory event UI.
4. A scheduled event can run without exposing its internal timer to players.
5. A player can ignore an ambient event without the UI continuing to demand participation.
6. A fort or settlement can experience a local attack without transforming the entire zone into an event state.
7. An event can resolve successfully or unsuccessfully with coherent local recovery.
8. Event failure does not inherently impose zone-wide penalties on uninvolved players.
9. Event state can drive Normal/Alert/Fearful NPC behaviour through the AI system.
10. A communal profession activity such as a quarry can use shared event infrastructure while retaining profession-owned contribution/reward rules.
11. Permit-zone access can coexist with faction/reputation requirements.
12. Permit activities can remain noncompetitive and low-intrusion.
13. An open-world elite can use shared persistent-world availability.
14. A world boss can exist as a shared-world encounter rather than a party instance.
15. Defeating a world boss makes it unavailable to other groups on that world until its shared availability rules permit it again.
16. A world boss can be discovered without a permanent obvious map marker.
17. A world boss can be deliberately triggered.
18. One encounter can unlock or reveal another encounter.
19. An encounter chain can operate without requiring a visible global progress tracker.
20. A world boss can use a narrow, recognisable loot table while providing rewards appropriate to comparable level content.
21. Ordinary ambient events do not require a universal contribution algorithm.
22. Different event categories can define different contribution and reward rules.
23. A crisis event can have wider impact than an ordinary ambient event without redefining normal world-event presentation.
24. Ancient Crisis can begin through a future Archaeology-owned legendary artefact.
25. Ancient Crisis activation can provide an intervention window before the boss appears.
26. Meaningful conspirators can be included in the PvP risk model.
27. Successful activation can create one shared roaming crisis boss.
28. The crisis boss can move through appropriate world spaces and attack both NPCs and players.
29. Nearby NPC areas can react to a crisis through established readiness behaviour.
30. Preventing the crisis and defeating an active crisis can be treated as distinct outcomes.
31. Crisis frequency can be limited at the shared-world level.
32. Event authoring can reference existing NPC, encounter, AI, loot, profession and quest data rather than duplicating those systems.
