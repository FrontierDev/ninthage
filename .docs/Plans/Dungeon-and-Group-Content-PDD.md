# Ninth Age — Dungeon and Group Content Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** Dungeons, five-player group content, encounter structure, dungeon pacing, difficulty modes, instance ownership, recovery, rewards, raids at a high level, and the relationship between instanced and open-world boss content  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended design of **dungeons and structured group PvE content in Ninth Age**.

It is authoritative for:

- what constitutes a dungeon;
- standard dungeon group size;
- dungeon duration and encounter count;
- trash density;
- optional and skippable content;
- dungeon entrances and world integration;
- party composition expectations;
- dungeon difficulty modes;
- dungeon instance ownership;
- disconnect and rejoin behaviour;
- dungeon scaling rules;
- wipe recovery;
- checkpoints and shortcuts;
- dungeon quest requirements;
- dungeon boss loot quantity;
- dungeon lockouts;
- broad raid scaling principles;
- the relationship between dungeon bosses, open-world elites and world bosses.

This document does not define:

- detailed group-management UI or invitation rules — [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md);
- individual boss mechanics — encounter-specific content;
- AI behaviour — [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md);
- NPC definitions — [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- technical instance loading — [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md);
- detailed itemisation or loot-distribution rules — [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md);
- quest-system behaviour — [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md);
- detailed raid design beyond the shared principles required here;
- detailed open-world event and crisis-boss mechanics — [Open-World Events PDD](Open-World-Events-PDD.md).

---

## 2. Design Pillars

### 2.1 Dungeons are a content format, not a literal location type

A dungeon is a **player-capped private instance containing structured difficult PvE content**.

It does not need to be underground or resemble a traditional dungeon.

A dungeon may be:

- a fortress;
- cave system;
- manor;
- temple;
- wilderness expedition;
- ruined city section;
- ship;
- mine;
- battlefield;
- other appropriate environment.

### 2.2 Dungeons should respect player time

A normal five-player dungeon should generally take approximately:

**30–45 minutes**

for an appropriately prepared group.

Difficulty should come primarily from encounters and execution rather than excessive travel or large quantities of trivial enemies.

### 2.3 Dungeons belong to the world

Where appropriate, dungeon entrances should have believable physical locations within the open world.

The player should feel that they have entered a place that exists within Ninth Age rather than selecting an isolated combat map from a menu.

### 2.4 Combat density should remain purposeful

Trash exists to:

- establish the location;
- introduce mechanics;
- provide pacing;
- create route decisions;
- support narrative;
- make the dungeon feel inhabited.

Trash should not primarily exist to inflate completion time.

### 2.5 Knowledge should improve dungeon traversal

Experienced groups should be able to improve their run through:

- route knowledge;
- optional skips;
- controlled pulls;
- profession interactions;
- environmental shortcuts.

Not every enemy in a dungeon must be killed.

---

# 3. Dungeon Definition

A **Dungeon** is a player-capped private instance containing, at minimum:

- multiple boss or difficult elite encounters; and
- additional elite enemies or elite groups.

A dungeon must contain a deliberate progression through structured PvE content.

A private interior containing only ordinary NPCs is not automatically a dungeon.

Likewise, a difficult open-world cave containing elites is not a dungeon unless it is operated as player-capped instanced group content.

---

# 4. Standard Party Size

The standard dungeon group size is:

**5 players**

Five-player encounters are designed around the capabilities of a five-player party.

Players may enter or continue with fewer players where technically permitted, but encounters do not become easier merely because the party is undermanned.

---

# 5. Role Expectations

## 5.1 Low-level dungeons

Early character progression provides fewer specialised class tools.

Low-level dungeon design should therefore be comparatively flexible.

A composition such as:

- 1 healer;
- 4 damage-oriented characters;

should be viable.

A dedicated tank should not be mandatory before classes have developed the toolkits necessary to support that role properly.

## 5.2 Mid-level onwards

From approximately the point at which class toolkits support specialised roles, dungeon encounters should assume:

- **1 Tank**
- **1 Healer**
- **3 DPS**

This becomes the standard five-player composition.

## 5.3 Role flexibility

Role expectations define encounter requirements rather than requiring one specific class.

Different classes and specialisations may fulfil the same role differently.

Detailed class-role eligibility belongs to Class and Ability design.

---

# 6. Dungeon Duration

A normal dungeon should target approximately:

**30–45 minutes**

for an appropriately equipped and competent group completing the normal required route.

This is a target rather than a hard timer.

Completion may be faster because of:

- player experience;
- route optimisation;
- skipped trash;
- strong execution;
- optional content being ignored.

It may be longer because of:

- wipes;
- exploration;
- optional bosses;
- quests;
- inexperienced players.

The game should not artificially enforce the duration.

---

# 7. Significant Encounter Count

A normal dungeon should generally contain approximately:

**4–6 significant encounters**

A significant encounter may be:

- a boss;
- miniboss;
- difficult elite pack;
- scripted combat encounter;
- unusual combat challenge.

This does **not** mean every dungeon requires 4–6 bosses.

Example:

```text
Boss
  ↓
Difficult Elite Encounter
  ↓
Boss
  ↓
Optional Miniboss
  ↓
Boss
  ↓
Final Boss
```

Dungeon structure may vary substantially while remaining within the intended overall pacing.

---

# 8. Boss Structure

A dungeon should normally contain multiple boss encounters.

The final boss should usually represent the dungeon's strongest or most narratively important required encounter.

Bosses should provide:

- identifiable encounter mechanics;
- a meaningful challenge increase over ordinary elites;
- clear encounter spaces or states;
- worthwhile rewards.

Bosses should not be defined merely as ordinary enemies with substantially more health.

Detailed boss mechanics belong to encounter content and the AI and Encounter Behaviour PDD.

---

# 9. Minibosses and Difficult Elites

Dungeons may use difficult elite encounters between major bosses.

These may include:

- named elites;
- minibosses;
- elite groups;
- commanders;
- dangerous specialist enemies;
- environmental combat encounters.

These provide encounter variety without requiring every significant fight to have full boss complexity.

---

# 10. Trash Density

Dungeons should avoid excessive quantities of mandatory trash.

Trash should be placed deliberately.

A typical run should not require clearing every room or every visible enemy.

Some trash should be:

- mandatory;
- optional;
- avoidable through route choice;
- avoidable through careful positioning;
- bypassable through dungeon mechanics;
- bypassable through profession-specific opportunities.

Dungeon pacing should alternate between meaningful encounters, movement and quieter periods rather than becoming one continuous chain of minor fights.

---

# 11. Skippable Trash

Skipping trash is legitimate dungeon gameplay.

It is not inherently an exploit.

Players may use:

- positioning;
- route knowledge;
- enemy patrol timing;
- alternate paths;
- profession interactions;
- stealth-capable gameplay where appropriate;

to avoid unnecessary combat.

Dungeon authoring should intentionally provide some opportunities for this.

Skipping enemies must not require exploiting broken navigation or geometry.

---

# 12. Critical Path and Optional Content

Every dungeon should have a clear **required critical path**.

The critical path contains the encounters and objectives necessary to complete the dungeon.

Dungeons may additionally contain:

- optional bosses;
- side rooms;
- hidden areas;
- profession interactions;
- alternate routes;
- treasure;
- lore;
- additional quests;
- optional elite encounters.

Optional content should provide reasons to explore without making the normal completion route unnecessarily long.

---

# 13. Dungeon World Integration

Where geographically appropriate, dungeons should have believable physical entrances in the world.

Examples include:

- caves;
- doors;
- fortress gates;
- mine entrances;
- temples;
- crypts;
- ruined structures;
- tunnels;
- ships.

Approaching and entering the dungeon should preserve the sense that the dungeon occupies a real location.

---

# 14. Seamless Dungeon Entrances

Where appropriate, dungeon entry should use the seamless private-instance model defined by the World Runtime and Instancing PDD.

Conceptually:

```text
Open World
     ↓
Physical Dungeon Entrance
     ↓
Preload / Transition Space
     ↓
Group Instance
```

The transition may resemble the physically integrated instanced entrances used by compact instanced content such as modern WoW Delves: the destination remains private while the entrance still feels physically part of the surrounding world.

Suitable transition spaces may include:

- tunnels;
- corridors;
- caves;
- stairways;
- gates;
- lifts.

Not every dungeon must use seamless entry.

Detached-map transitions remain valid where the fiction or geography calls for them.

---

# 15. Dungeon Instance Ownership

A dungeon instance belongs to the **group**, not to its leader or any individual member.

The instance therefore has a persistent group-instance identity.

Changing:

- party leader;
- party roles;
- party membership;

does not recreate, transfer or reset the dungeon.

Leadership has no special relationship with instance ownership.

---

# 16. Membership Changes

Changing group membership does not inherently change the dungeon instance.

A replacement player joining the qualifying group may enter the group's existing dungeon instance.

A player leaving the group does not cause:

- instance recreation;
- encounter reset;
- instance ownership transfer;
- loss of progress for the remaining group.

Detailed rules for when former group members are removed from or lose access to an instance belong to the Group and Raid Systems PDD.

---

# 17. Disconnect and Rejoin

A player disconnecting does not remove or recreate the group's dungeon instance.

If the player reconnects and remains eligible to join the group instance, they should return to or be able to re-enter the same active dungeon.

The group should not lose dungeon progression because its leader or another member disconnected.

---

# 18. Instance Lifecycle

Dungeon instances follow the logical instance architecture defined by the World Runtime and Instancing PDD.

A dungeon may remain available while temporarily empty according to the instance lifecycle policy.

The gameplay instance identity must remain distinct from the underlying Unity scene.

Multiple parties may simultaneously occupy independent instances created from the same authored dungeon scenes.

---

# 19. Difficulty Modes

Dungeon definitions should structurally support multiple difficulty modes.

However:

**Normal is the only required initial dungeon difficulty.**

The implementation must not require multiple difficulty modes to ship the initial dungeon system.

Future modes may modify:

- NPC difficulty;
- mechanics;
- encounter composition;
- rewards;
- restrictions;
- other encounter parameters.

The exact future difficulty structure is deliberately deferred.

---

# 20. Five-Player Group Scaling

Five-player dungeon encounters do **not** dynamically scale according to the number of players currently present.

A dungeon designed for five remains designed for five.

For example:

```text
5 players → intended difficulty
4 players → same encounter, undermanned
3 players → same encounter, substantially undermanned
```

This preserves meaningful group composition and encounter tuning.

Player count must not dynamically reduce boss health, enemy count or mechanic requirements in normal five-player dungeon content merely because someone leaves.

---

# 21. Raid Scaling

Raids may support player-count scaling.

A raid must nevertheless define a **minimum required player count**.

Scaling must not allow a raid intended as large-group content to collapse into effectively small-party content.

Detailed raid sizes, scaling ranges, compositions and difficulty modes remain for later raid design.

---

# 22. Dungeon Lockouts

Normal five-player dungeons have:

**no lockout**

Players may repeat them according to normal access rules.

Dungeon rewards may still be subject to appropriate anti-exploit or reward-system rules if later required, but ordinary five-player content should not use raid-style lockouts.

---

# 23. Raid Lockouts

Lockouts are reserved for raids.

The detailed raid lockout model remains deferred.

Future raid design should determine:

- lockout duration;
- boss-specific versus instance-wide lockouts;
- group identity implications;
- reward eligibility;
- reset cadence.

---

# 24. Dungeon Quests

**Every dungeon must have quests associated with it.**

Dungeon quests should reinforce the dungeon's relationship to the wider world.

They may provide:

- narrative motivation;
- faction context;
- exploration objectives;
- boss objectives;
- item recovery;
- investigation;
- optional side objectives;
- follow-up consequences.

A dungeon should not feel like a disconnected combat activity with no narrative connection to its region.

Not every quest associated with a dungeon must necessarily require full completion.

---

# 25. Dungeon Loot

In a normal five-player dungeon:

- each non-final boss drops **1 item**;
- the final boss drops **2 items**.

This defines the quantity of item drops generated by boss defeat.

It does not define:

- which players receive them;
- need/greed rules;
- personal versus shared loot;
- item quality;
- item-level scaling;
- trade restrictions.

Those rules belong to the Items, Equipment and Loot PDD.

Optional bosses may follow the normal one-item boss rule unless their content explicitly defines otherwise.

---

# 26. Wipe Recovery

Universal automatic checkpoints are **not** the default dungeon model.

After a full wipe, the group should normally recover from the dungeon entrance or equivalent initial recovery location.

This gives dungeon geography and traversal some continuing consequence.

However, dungeon design should avoid making wipes excessively tedious.

---

# 27. Progress Shortcuts

Dungeon progress may naturally unlock shortcuts.

Examples include:

- opening a previously locked gate;
- lowering a bridge;
- unlocking a door;
- activating a lift;
- clearing a blocked passage;
- opening a one-way route back into the dungeon.

These are part of dungeon environmental progression rather than universal checkpoint teleportation.

A later wipe may therefore require re-entry from the start while allowing players to return to their previous progress substantially faster.

---

# 28. Shadowcraft and Dungeon Recovery

Shadowcraft may provide additional traversal opportunities within dungeons.

Potential examples include:

- climbing routes;
- concealed passages;
- alternate recovery routes;
- bypasses;
- shortcuts.

This may make Shadowcraft useful for reducing recovery time after a wipe or bypassing parts of a route.

Shadowcraft should not be required merely to make ordinary dungeon recovery tolerable.

Exact Shadowcraft mechanics belong to profession design.

---

# 29. Encounter Reset

Bosses and structured encounters should reset coherently after a wipe or failed pull.

Reset should restore the encounter to its authored valid state.

This may include:

- NPC health/resources;
- threat;
- actor positions;
- encounter objects;
- temporary summoned enemies;
- phase state;
- scripted effects.

The AI and Encounter Behaviour PDD owns the generic reset framework.

Dungeon content defines what state each encounter restores.

---

# 30. Encounter Boundaries

A dungeon encounter may have an authored encounter area.

Encounter boundaries can help determine:

- encounter participation;
- wipe state;
- leash behaviour;
- encounter reset;
- scripted mechanics.

Boundaries should make spatial sense and should not feel like arbitrary invisible walls where this can be avoided.

Players should normally be able to understand the physical space in which an encounter occurs.

---

# 31. Death During an Encounter

A player's death does not inherently remove them from the dungeon instance.

Whether resurrection is possible during an active encounter depends on the combat/resurrection systems.

A full party wipe triggers encounter reset behaviour.

The exact player death and resurrection rules belong to the Combat System PDD.

---

# 32. Dungeon Traversal

Dungeons should make meaningful use of physical spaces rather than consisting only of connected combat arenas.

Dungeon traversal may include:

- branching corridors;
- vertical movement;
- caves;
- lifts;
- environmental hazards;
- bridges;
- water;
- locked routes;
- shortcuts;
- profession opportunities.

Traversal should support dungeon identity without overwhelming the intended 30–45 minute duration.

---

# 33. Profession Interactions

Professions may provide optional advantages within dungeons.

Potential examples include:

- alternate routes;
- shortcuts;
- additional resources;
- environmental interactions;
- access to optional rooms;
- alternative solutions to obstacles.

Such advantages should be valuable without making a specific profession mandatory for ordinary completion.

Shadowcraft is particularly suited to traversal and route opportunities.

---

# 34. Encounter Variety

A dungeon should not require every significant encounter to follow the same structure.

Encounter variety may come from:

- boss mechanics;
- elite groups;
- positioning;
- movement;
- environmental interaction;
- target priority;
- interrupts;
- adds;
- defensive requirements;
- healing pressure;
- controlled use of crowd control.

Encounter design should build on the shared combat system rather than requiring every encounter to introduce bespoke rules.

---

# 35. Low-Level Dungeon Design

Early dungeons should account for reduced player toolkits.

They should avoid assuming that players possess:

- mature tank mitigation;
- complete healer toolkits;
- extensive interrupts;
- multiple crowd-control abilities;
- specialised movement tools.

Low-level dungeons should still teach group combat concepts, but requirements should increase alongside character capabilities.

---

# 36. Mid- and High-Level Dungeon Design

As classes gain fuller toolkits, dungeon encounters may increasingly expect:

- tank positioning;
- healing management;
- interrupts;
- defensive cooldowns;
- target prioritisation;
- crowd control;
- movement mechanics;
- class-role coordination.

Difficulty should increase through mechanical expectations rather than primarily through inflated enemy health.

---

# 37. Open-World Elites

Open-world elites are not dungeon encounters merely because they use Elite NPCs.

They remain part of the shared open-world simulation.

Open-world elites may require:

- multiple players;
- careful execution;
- stronger-than-normal characters.

Their availability may use a **server-wide cooldown** rather than per-player or per-party availability where appropriate.

---

# 38. World Bosses

World bosses are shared open-world encounters rather than private dungeon instances.

They may involve substantially larger groups.

World bosses may use server-wide availability or respawn cooldowns.

A defeated world boss should not immediately become independently available to every other party.

Exact cooldown lengths are intentionally deferred.

World bosses use the same general actor, AI and encounter foundations as other PvE content while allowing open-world participation.

---

# 39. Player-Triggered Crisis Bosses

Ninth Age may support **player-triggered server-wide crisis encounters**.

One proposed example is tied to a future **Archaeology** profession system:

1. a player reaches maximum Archaeology progression;
2. the player acquires the knowledge and materials to construct a legendary artefact;
3. activating the artefact begins an interruptible player-driven event;
4. the archaeologist and declared conspirators become exposed to PvP intervention;
5. other players may attempt to prevent the activation;
6. if activation succeeds, a powerful crisis boss is released into the world;
7. the boss roams through the world and attacks players and NPCs indiscriminately;
8. the wider player population may organise to destroy it.

This concept is deliberately recorded here because the resulting boss is group/open-world PvE content.

However, this PDD does **not** own the detailed system.

Detailed ownership is divided between:

- **Open-World Events PDD** — crisis-event behaviour, world effects, participation and boss lifecycle;
- **Crafting/Profession design** — Archaeology progression and legendary artefact creation;
- **PvP PDD** — archaeologist/conspirator PvP exposure and intervention rules;
- **Items/Loot and reward design** — incentives for initiating, preventing and defeating the crisis.

The system requires substantial future reward, balance and anti-exploit design.

---

# 40. Crisis-Event Design Intent

The intended purpose of the proposed crisis system is to create **emergent hybrid PvE/PvP gameplay**.

It should create conflicting player incentives:

```text
Archaeologist + conspirators
            ↓
attempt to unleash crisis
            ↓
other players may intervene through PvP
            ↓
successful prevention
        OR
boss released
            ↓
server-wide PvE response
```

The system should provide meaningful incentives for:

- initiating the event;
- assisting the initiator;
- preventing activation;
- defeating the resulting boss.

It must not reduce to consequence-free griefing.

Detailed design is deferred.

---

# 41. Dungeon Authoring Requirements

A dungeon definition should be able to specify or reference:

- stable dungeon identity;
- name;
- associated world location;
- scene/scene set;
- entrance;
- instance scope;
- player cap;
- target difficulty;
- difficulty mode;
- required encounters;
- optional encounters;
- completion criteria;
- group requirements;
- quest references;
- boss definitions;
- loot references;
- environmental shortcuts;
- optional profession interactions;
- exit/return behaviour.

The exact Unity classes and ScriptableObject structure belong to technical design.

---

# 42. Encounter Authoring Requirements

Each significant encounter should be able to define:

- participating NPCs;
- encounter area;
- start conditions;
- completion conditions;
- failure/reset conditions;
- encounter-specific AI state;
- phases;
- environmental objects;
- reward references;
- required/optional status.

Encounter definitions should reuse the AI and NPC systems rather than duplicate complete actor behaviour.

---

# 43. Dungeon Completion

A dungeon is complete when its required completion criteria have been met.

This will normally include defeating the required final encounter.

A dungeon may contain optional content that remains incomplete without preventing dungeon completion.

Completion state may be used by:

- quests;
- rewards;
- achievements;
- progression;
- statistics.

Detailed reward systems remain outside this PDD.

---

# 44. Current Implementation Relationship

The World Runtime and Instancing PDD already defines:

- party-scoped private runtime instances;
- reusable Unity scene assets;
- seamless private entrances;
- detached-map instances;
- independent runtime simulation contexts;
- lazy instance creation;
- rejoining existing eligible instances.

The dungeon system should use that architecture rather than create a separate instance-loading model.

The NPC and AI PDDs already define:

- reusable NPC definitions;
- Elite/Boss classifications;
- encounter definitions;
- reusable behaviour;
- linked encounter groups;
- phase support;
- wipe/reset capability.

Dungeon content should compose these systems.

---

# 45. Intentionally Deferred Decisions

The following remain deliberately unspecified:

- exact low-level threshold at which tank/healer/DPS roles become mandatory;
- exact number of bosses per dungeon;
- exact trash-pack count;
- exact dungeon physical size;
- future difficulty-mode names;
- future difficulty scaling;
- future difficulty rewards;
- raid size;
- raid maximum size;
- raid scaling range;
- raid role composition;
- raid lockout duration;
- raid loot quantities;
- detailed loot-distribution system;
- detailed resurrection behaviour;
- exact server-wide elite/world-boss cooldowns;
- exact crisis-event mechanics;
- Archaeology implementation;
- crisis-event PvP rules;
- crisis-event rewards.

---

# 46. Locked Design Decisions

The following decisions are locked by this PDD:

1. A dungeon is player-capped private instanced PvE content containing boss/difficult elite encounters and other elite enemies.
2. A dungeon does not need to be a literal underground dungeon.
3. Standard dungeon party size is 5.
4. Normal dungeon duration targets approximately 30–45 minutes.
5. Dungeons should normally contain approximately 4–6 significant encounters.
6. Significant encounters do not all need to be bosses.
7. Dungeons should avoid excessive mandatory trash.
8. Some trash should be intentionally skippable.
9. Skipping properly avoidable trash is legitimate gameplay.
10. Every dungeon has a required critical path.
11. Optional bosses, routes, rooms and other side content are supported.
12. Seamless physically integrated dungeon entrances should be used where appropriate.
13. Detached-map transitions remain valid where appropriate.
14. Low-level dungeons should be viable without a dedicated tank where player toolkits do not yet support the normal role structure.
15. From mid-level onwards, standard dungeon composition assumes 1 Tank, 1 Healer and 3 DPS.
16. Dungeon definitions must structurally support difficulty modes.
17. Only Normal difficulty is required initially.
18. Five-player dungeon encounters do not dynamically scale to current group size.
19. Raids may scale with player count but retain a configured minimum player count.
20. Five-player dungeons have no raid-style lockout.
21. Lockouts are reserved for raids.
22. Every dungeon must have associated quests.
23. Each ordinary five-player dungeon boss drops 1 item.
24. The final boss drops 2 items.
25. Loot-distribution rules remain owned by the Items, Equipment and Loot PDD.
26. Dungeon instance ownership belongs to the group, not the leader.
27. Changing party leadership does not affect instance identity.
28. Changing group membership does not recreate/reset the instance.
29. Disconnecting does not recreate/reset the instance.
30. Eligible reconnecting players can return to the group's existing instance.
31. Universal automatic dungeon checkpoints are not the default.
32. Dungeon progression may unlock physical recovery shortcuts.
33. Shadowcraft may provide additional dungeon traversal and recovery shortcuts.
34. Dungeon design should remain usable without requiring Shadowcraft.
35. Open-world elites and world bosses may use server-wide availability cooldowns.
36. World bosses remain shared open-world encounters rather than private dungeon instances.
37. Player-triggered crisis bosses are a supported future design direction.
38. Detailed crisis-event mechanics belong primarily to Open-World Events, Profession and PvP design.
39. Dungeon and encounter content must use the existing shared runtime, NPC and AI architecture rather than create parallel systems.

---

# 47. Dependencies

This PDD depends on or constrains:

- **World Runtime and Instancing PDD** — dungeon instances, seamless entrances and runtime ownership;
- **World and Zone Design PDD** — dungeon locations and physical world integration;
- **Movement and Traversal PDD** — dungeon traversal;
- **NPC and Creature Design PDD** — elites, bosses and reusable NPC definitions;
- **AI and Encounter Behaviour PDD** — encounters, phases, wipes, resets and NPC combat behaviour;
- **Combat System PDD** — group combat and death/resurrection;
- **Class Design PDD** — tank, healer and DPS capabilities;
- **Abilities and Talents PDD** — role toolkits;
- **Group and Raid Systems PDD** — group identity, membership and group management;
- **Quest, Narrative and Dialogue PDD** — mandatory dungeon-associated quests;
- **Items, Equipment and Loot PDD** — dungeon drops and distribution;
- **Open-World Events PDD** — world bosses and crisis encounters;
- **Crafting/Profession design** — Shadowcraft dungeon traversal and proposed Archaeology crisis system;
- **PvP PDD** — proposed crisis-event PvP intervention.

---

# 48. Validation Criteria

The design satisfies this PDD when:

1. A dungeon can be authored as any suitable environment rather than only a literal underground dungeon.
2. A normal dungeon supports a maximum standard party of five.
3. An appropriately prepared group can reasonably target a 30–45 minute run.
4. A dungeon can contain approximately 4–6 significant encounters without requiring 4–6 bosses.
5. Dungeon progression does not require excessive filler trash.
6. Some enemy groups can intentionally be bypassed.
7. Skipping intended optional enemies does not break dungeon completion.
8. Every dungeon has a clear required critical path.
9. Optional bosses and side content can exist outside that path.
10. A physically integrated dungeon can be entered seamlessly where appropriate.
11. Separate parties entering the same dungeon can receive independent private instances.
12. Early dungeons can function without requiring a dedicated tank.
13. Mid-level and later dungeons can be balanced around 1 Tank, 1 Healer and 3 DPS.
14. A dungeon definition can represent Normal difficulty while remaining extensible to future modes.
15. Removing a player does not cause the remaining encounter to scale down.
16. A four-player group entering a five-player dungeon faces the same encounter intended for five.
17. Dungeon instance identity survives leadership changes.
18. Dungeon instance identity survives ordinary membership changes.
19. Dungeon progress survives an eligible member disconnecting and reconnecting.
20. Normal five-player dungeons can be repeated without a raid-style lockout.
21. Raid content can later use lockouts independently.
22. Every dungeon has at least one related quest chain or quest.
23. Ordinary bosses generate one item drop and the final boss generates two.
24. A wipe can reset an encounter cleanly.
25. Recovery can begin from the entrance without requiring automatic checkpoints.
26. Dungeon progress can unlock physical shortcuts reducing repeated traversal.
27. Shadowcraft can optionally provide additional routes without becoming required for normal completion.
28. Open-world elites and world bosses can use shared server-wide cooldowns rather than per-party availability.
29. World bosses can remain part of the shared world while using the common encounter framework.
30. Future player-triggered crisis bosses can integrate with the open-world encounter framework without being treated as five-player dungeon instances.
31. Dungeon content can be authored by composing the established NPC, AI, world-runtime and encounter systems.
