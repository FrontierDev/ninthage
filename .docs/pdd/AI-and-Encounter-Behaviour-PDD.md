# Ninth Age — AI and Encounter Behaviour Product Design Document

**Status:** Authoritative design reference  
**Project:** Ninth Age  
**Scope:** NPC perception, aggro, threat, target selection, movement and positioning, group behaviour, ability use, disengagement, resets, reusable behaviour definitions, encounter state and open-world encounter-area readiness  
**Last updated:** 2026-10-05

---

## 1. Purpose and Authority

This document defines the intended behaviour of NPCs and creatures in **Ninth Age** once they exist in the world.

It is authoritative for:

- perception and detection;
- aggro acquisition;
- threat response;
- target selection;
- combat movement and positioning;
- melee, ranged, caster, healer and support behaviour;
- group assistance and linked pulls;
- patrol and roaming behaviour;
- fleeing and surrender;
- crowd-control response;
- stealth and line-of-sight interaction;
- unreachable-target handling;
- disengagement, evasion and reset behaviour;
- reusable behaviour definitions;
- generic encounter states;
- generic boss-phase and scripted-transition support;
- open-world encounter-area readiness;
- server authority for AI decisions.

This document does not define:

- what an NPC or creature fundamentally is — [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md);
- combat resolution, damage, healing or universal threat formulas — [Combat System PDD](Combat-System-PDD.md);
- class-specific player abilities — [Class Design PDD](Class-Design-PDD.md) and [Abilities and Talents PDD](Abilities-and-Talents-PDD.md);
- exact dungeon pacing, encounter composition, boss mechanics or rewards — [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md);
- exact world geography or NPC population density — [World and Zone Design PDD](World-and-Zone-Design-PDD.md);
- world-event rules — [Open-World Events PDD](Open-World-Events-PDD.md);
- detailed faction/reputation rules — [Factions and Reputation PDD](Factions-and-Reputation-PDD.md);
- technical AI implementation architecture.

---

## 2. Design Pillars

### 2.1 Behaviour should be readable

Players should normally be able to understand why an NPC:

- noticed them;
- joined a fight;
- changed target;
- fled;
- reset;
- used an ability.

AI may be challenging without appearing arbitrary.

### 2.2 NPC Definition, Behaviour Definition and Encounter Definition are separate

The system should preserve three distinct layers:

```text
NPC Definition
    = what the unit is

Behaviour Definition
    = how that kind of unit generally acts

Encounter Definition
    = what this particular encounter requires it to do
```

A creature's taxonomic identity must not contain bespoke encounter scripting.

### 2.3 Ordinary AI should be data-driven

Common NPC behaviour should be expressible through reusable behaviour data.

Special-case scripted code should be reserved for behaviour that genuinely cannot be represented through the ordinary system.

### 2.4 Server authority

Perception, aggro, target selection, threat, movement intent, behaviour state and encounter state are server-authoritative.

Clients may present these outcomes but do not decide them.

### 2.5 AI should fail gracefully

An NPC should not remain permanently stuck because:

- a target is unreachable;
- navigation fails;
- the player leaves its valid encounter space;
- its target disappears;
- a scripted condition becomes invalid.

The system must have explicit recovery and reset behaviour.

---

## 3. Behaviour Definitions

NPC definitions reference reusable **Behaviour Definitions**.

A Behaviour Definition describes how an NPC generally acts.

Examples may include:

- Passive Wildlife;
- Territorial;
- Predator;
- Pack Hunter;
- Guard;
- Patrol;
- Melee Combatant;
- Ranged Combatant;
- Caster;
- Healer;
- Support;
- Civilian;
- Boss Controller.

Behaviour Definitions may expose configurable values and rules such as:

- perception parameters;
- assistance rules;
- movement preferences;
- preferred range;
- ability-selection rules;
- flee/surrender behaviour;
- readiness-state reactions;
- target-selection preferences.

An NPC preset may override the referenced behaviour where the variant genuinely behaves differently.

---

## 4. Encounter Definitions

An Encounter Definition represents behaviour or state belonging to a particular encounter rather than to the creature itself.

Encounter Definitions may coordinate:

- linked units;
- encounter start conditions;
- scripted actions;
- phase transitions;
- shared state;
- reset conditions;
- completion/failure state;
- open-world encounter-area readiness where applicable.

A generic Wolf Behaviour Definition should not need to know the script for a particular named boss encounter.

---

## 5. Core AI State

Individual NPC AI should support clear high-level states.

A baseline model is:

```text
Idle / Roaming
      ↓
Alerted / Acquiring
      ↓
Engaged
      ↓
Combat
      ↓
Disengaging / Evading
      ↓
Resetting
      ↓
Idle / Roaming
```

Not every NPC must visibly expose every state.

Additional behaviour-specific substates may exist.

State transitions must be deterministic enough for debugging and content authoring.

---

## 6. Perception

NPC perception is configurable.

Normal perception may consider:

- detection distance;
- line of sight;
- target disposition/faction relationship;
- stealth;
- current encounter state;
- behaviour archetype;
- scripted conditions.

Different NPCs may have different perception capabilities.

Examples:

- a passive civilian may have no hostile acquisition behaviour;
- a guard may have strong awareness;
- an animal may rely heavily on proximity;
- a scripted sentry may monitor a narrower approach.

Exact detection distances and formulas are intentionally deferred.

---

## 7. Line of Sight

Normal detection and targeted actions should respect line of sight unless a particular mechanic explicitly states otherwise.

NPCs should not acquire or attack through solid terrain simply because the target is within a radius.

Line-of-sight checks should be appropriate to the action:

- perception;
- ranged attacks;
- spell casts;
- assistance;
- scripted mechanics.

The exact technical implementation belongs to architecture.

---

## 8. Stealth and Detection

Stealth modifies the ability of NPCs to detect a player rather than granting universal binary invisibility.

Detection may depend on:

- distance;
- NPC perception capability;
- target stealth strength;
- facing or visibility where relevant;
- special detection abilities;
- encounter state.

NPCs should not omnisciently acquire stealthed targets merely because the server knows their position.

Exact stealth/detection formulas belong to the relevant combat/ability design.

---

## 9. Aggro Acquisition

A hostile NPC may enter combat because of:

- perceiving a valid hostile target;
- receiving a hostile action;
- assisting an allied NPC;
- belonging to an explicitly linked encounter group;
- a scripted encounter trigger;
- another deliberate gameplay rule.

Faction relationship determines whether ordinary hostile acquisition is appropriate.

Aggro acquisition must remain separate from the NPC's creature type.

---

## 10. Threat

Combat-capable NPCs may maintain a threat table.

Threat may be generated by systems such as:

- damage;
- healing;
- explicit threat-generating abilities;
- explicit threat-reducing abilities;
- encounter scripting.

The Combat System PDD owns universal threat-generation rules and formulas.

This PDD owns how AI uses threat.

The default combat target should normally be the highest-threat **valid** target.

---

## 11. Target Selection

Threat is the default target-selection mechanism, not an absolute restriction.

Behaviour and encounter rules may deliberately select targets using criteria such as:

- highest threat;
- random valid target;
- nearest target;
- furthest target;
- lowest/highest health;
- current role or capability;
- target currently performing a particular action;
- scripted mark or encounter condition.

A selected target must still be valid for the action being attempted.

Boss and special encounter mechanics may temporarily override normal threat targeting.

---

## 12. Assistance

NPCs may assist allied NPCs.

Assistance should consider appropriate authored rules such as:

- faction relationship;
- encounter/group membership;
- distance;
- line of sight or awareness where applicable;
- behaviour archetype;
- readiness state.

Assistance must be bounded.

A fight should not propagate indefinitely across an entire region through uncontrolled recursive chain aggro.

Exact assist ranges remain configurable.

---

## 13. Linked Pulls

NPC groups may be explicitly linked.

Engaging one member of a linked group may engage:

- the entire group;
- a configured subset;
- units according to encounter rules.

Linked pulls are distinct from ordinary proximity assistance.

This distinction allows designers to create coherent encounter packs without requiring units to stand unnaturally close together.

---

## 14. Patrol and Roaming Behaviour

NPCs may be:

- stationary;
- waypoint-patrolling;
- route-patrolling;
- bounded random roamers;
- members of a moving group;
- controlled by encounter-specific movement.

NPC movement should remain within the spatial context authored for the NPC or encounter unless combat/encounter rules intentionally take it elsewhere.

Patrol and roaming systems should integrate with the home-area concept defined by the NPC and Creature Design PDD.

---

## 15. Melee Behaviour

A normal melee combatant should:

- approach a valid target;
- attempt to remain within usable melee range;
- use appropriate melee abilities;
- avoid unnecessary constant repositioning once correctly placed;
- recover sensibly if navigation temporarily fails.

Melee AI should not circle or shuffle continuously merely to appear active.

---

## 16. Ranged Behaviour

A ranged combatant may have a preferred range band.

It should generally:

- remain within usable attack range;
- avoid unnecessary movement while positioned correctly;
- reposition if the target moves outside usable range;
- react appropriately if an enemy closes into an undesirable range.

Different ranged archetypes may respond differently to close pressure.

Some may retreat.

Some may switch to melee.

Some may remain in place.

---

## 17. Caster Behaviour

Caster AI should select abilities based on usable game state rather than relying solely on a fixed repeating rotation.

Ability selection may consider:

- cooldown;
- castability;
- range;
- line of sight;
- resource availability;
- target validity;
- priority;
- conditions;
- encounter state;
- weighted choice.

Fixed sequences remain valid where explicitly required by an encounter.

---

## 18. Healer and Support Behaviour

Healer/support NPCs may evaluate allied units and choose actions according to configured priorities.

Potential considerations include:

- missing health;
- critical health thresholds;
- dispellable effects;
- buffs;
- protected/high-priority allies;
- self-preservation;
- ability cooldowns.

A healer should not necessarily repeat one heal on whichever ally has the numerically lowest health forever.

Behaviour definitions should permit meaningful prioritisation.

---

## 19. Ability Selection

Reusable behaviour data should be capable of describing ability selection through combinations of:

- priorities;
- conditions;
- cooldown availability;
- target selectors;
- weights;
- required encounter state;
- scripted triggers.

Common combatants should not require bespoke code simply to choose among a small set of attacks.

Encounter-specific scripted actions may override ordinary ability selection when required.

---

## 20. Group Behaviour

NPC groups may cooperate at an appropriate level.

Potential group behaviour includes:

- linked engagement;
- assistance;
- shared encounter state;
- coordinated movement;
- protection of another unit;
- retreat toward allies;
- pack-style target pressure.

Ordinary groups do not require sophisticated tactical squad simulation.

The behaviour system should support useful cooperation without making every pack expensive or unpredictable.

---

## 21. Fleeing

Fleeing is optional behaviour.

It may be appropriate for:

- civilians;
- weak creatures;
- fearful combatants;
- particular humanoid enemies;
- scripted encounters.

Flee conditions may consider:

- health;
- allies remaining;
- encounter-area readiness;
- behaviour archetype;
- scripted state.

An NPC that flees should have a meaningful destination or escape behaviour rather than running indefinitely in an arbitrary direction.

Many combat NPCs may simply never flee.

---

## 22. Surrender

Surrender is distinct from fleeing and death.

An NPC may surrender when configured conditions are met.

A surrendered NPC may:

- cease hostile actions;
- become temporarily non-combatant;
- participate in quest/dialogue logic;
- await an encounter resolution.

Exact surrender interactions belong to the relevant quest/encounter content.

Not all creature types or behaviours need to support surrender.

---

## 23. Crowd Control

AI must respond coherently to crowd-control effects.

Examples include:

- stun;
- incapacitation;
- root;
- silence;
- fear;
- slow.

The combat system defines the mechanical effect.

AI should:

- stop or restrict invalid actions while controlled;
- preserve or update target/encounter state appropriately;
- resume from a valid state when control ends.

Crowd control must not leave an NPC permanently stuck in an invalid behaviour state.

---

## 24. Unreachable Targets

An NPC must handle unreachable targets explicitly.

It may:

1. attempt reasonable repositioning/path recovery;
2. wait briefly where appropriate;
3. select another valid target;
4. disengage/reset if the encounter can no longer proceed.

The AI should not:

- run indefinitely into a wall;
- remain in combat forever with no valid path;
- attack through invalid geometry merely to avoid resetting.

Exact path-recovery timing is implementation tuning.

---

## 25. Disengagement

Combat should end when the NPC has no valid hostile targets and its encounter rules permit disengagement.

Possible causes include:

- all targets died;
- all targets escaped;
- all targets left the encounter/home area;
- the NPC can no longer reach any valid target;
- the encounter explicitly resets.

Disengagement should transition through a clear evasion/reset process rather than leaving combat state partially active.

---

## 26. Leashing and Home Areas

NPCs should not pursue players indefinitely through the world unless deliberately authored to do so.

The NPC and Creature PDD defines an authored home context.

AI uses that context to determine when an NPC has been pulled excessively far or the encounter is no longer valid.

Leash/reset decisions may consider:

- distance from home;
- encounter boundaries;
- path validity;
- target validity;
- scripted encounter rules.

Exact leash distances are content/configuration values rather than universal numbers.

---

## 27. Reset and Evade Behaviour

When an encounter resets, affected NPCs should return to a known valid state.

Reset may include:

- clearing hostile targets;
- clearing threat;
- ending inappropriate temporary encounter state;
- restoring health/resources as defined by the encounter;
- returning to a home position/route;
- restoring encounter objects or scripted state where applicable.

Boss and structured encounter resets should be deterministic.

A player should not be able to preserve unintended partial boss state simply by exploiting leash boundaries.

---

# 28. Open-World Encounter Areas

The open world may contain authored **Encounter Areas**.

An Encounter Area groups NPC populations and shared behavioural context without requiring the content to become an instanced encounter.

Examples include:

- a bandit camp;
- military checkpoint;
- hostile village;
- wolf territory;
- guarded ruin;
- cultist camp.

Encounter Areas may maintain shared runtime state.

One important shared state is **Readiness**.

---

## 29. Encounter-Area Readiness

Open-world Encounter Areas support three readiness levels:

- **Normal**
- **Alert**
- **Fearful**

Readiness belongs to the area.

Individual Behaviour Definitions interpret the area's readiness according to the NPC's identity and role.

Conceptually:

```text
Encounter Area Readiness
        ↓
Normal / Alert / Fearful
        ↓
Behaviour Definition interprets state
        ↓
Individual NPC response
```

This allows different NPCs in the same area to respond differently.

For example:

- civilians may hide;
- guards may become more defensive;
- fanatics may ignore fear;
- constructs or mindless undead may ignore readiness entirely.

---

## 30. Normal Readiness

**Normal** represents the area's ordinary state.

NPCs use their standard:

- patrols;
- perception;
- assist rules;
- flee/surrender tendencies;
- idle behaviours.

Normal is the default readiness unless content states otherwise.

---

## 31. Alert Readiness

**Alert** means the area believes there is a credible, manageable threat.

NPC reactions may include:

- greater likelihood of assisting nearby allies;
- greater likelihood of chained engagement;
- reduced willingness to flee;
- reduced willingness to surrender;
- increased patrol/search activity;
- shorter reaction delays where appropriate;
- movement toward defensive or prepared positions;
- interruption of some ordinary idle behaviours.

Alert does not necessarily mean every NPC immediately knows the exact location of every player.

Perception rules still apply unless the encounter explicitly shares that information.

---

## 32. Fearful Readiness

**Fearful** means the area's population believes the threat substantially exceeds its ability to respond safely.

NPC reactions may include:

- increased likelihood of fleeing;
- increased likelihood of surrender;
- retreat toward safer positions;
- abandonment of exposed patrol routes;
- clustering in defensible or sheltered places;
- hiding;
- crouching;
- reluctance to initiate combat unless cornered.

Hiding or crouching may initially be primarily visual behaviour.

It does not need to provide a mechanical stealth bonus unless a future design explicitly adds one.

The visual reaction is still valuable because it communicates that the world has responded to player activity.

---

## 33. Readiness Is Not a Strict Escalation Ladder

Fearful is not simply "more Alert".

Alert and Fearful represent different assessments of danger.

Conceptually:

```text
Normal
  ├── credible/manageable threat → Alert
  └── overwhelming threat/severe losses → Fearful

Alert
  ├── threat passes → Normal
  └── situation deteriorates → Fearful

Fearful
  ├── danger passes → Normal
  └── confidence/reinforcements return → Alert
```

A particular event may transition directly between any appropriate states.

---

## 34. Readiness Triggers

Encounter-area readiness may change because of events such as:

- players being detected;
- alarms being raised;
- repeated NPC deaths;
- discovery of defeated allies;
- attacks on an important NPC or objective;
- severe losses;
- arrival or loss of reinforcements;
- scripted world events;
- quest state;
- other authored triggers.

The precise trigger set is configurable per encounter/content type.

Readiness should not be hard-coded to one universal kill-count system.

---

## 35. Readiness Recovery

Readiness should normally be able to decay or resolve when the threat is no longer present.

An area may return:

- Alert → Normal;
- Fearful → Alert;
- Fearful → Normal;

depending on context.

Recovery may depend on:

- elapsed time;
- no hostile players remaining;
- reinforcements;
- reset of the encounter area;
- scripted conditions.

Exact decay times are deliberately unspecified.

---

## 36. Readiness and Persistence

Encounter-area readiness is runtime world state.

Whether it survives:

- area unload;
- server restart;
- world restart;

depends on the content.

Ordinary temporary alarm/fear state does not inherently require permanent persistence.

Long-lived world-event or narrative state may opt into persistence through the appropriate system.

---

## 37. Boss and Encounter Phases

The AI framework must support generic encounter phase transitions.

Transitions may be triggered by:

- health thresholds;
- elapsed time;
- ability use;
- actor death;
- object state;
- position;
- external encounter events;
- other explicit conditions.

A phase may alter:

- available abilities;
- target selection;
- behaviour definition;
- movement;
- spawned actors;
- encounter objectives;
- scripted actions.

This section defines framework capability only.

Individual boss mechanics belong to their content definitions and the Dungeon/Group Content PDD.

---

## 38. Scripted Actions

Encounter scripting may require actions that take priority over ordinary AI.

Examples include:

- moving to a specific location;
- casting a mandatory ability;
- becoming temporarily untargetable;
- summoning units;
- changing phase;
- interacting with an encounter object.

Scripted actions should be explicit and visible in encounter authoring.

When the scripted sequence ends, the NPC must return to a valid ordinary or encounter-specific AI state.

---

## 39. Wipe and Encounter Reset

Structured encounters should be able to determine when no valid participating players remain.

A wipe/reset may occur when:

- all participating players are dead;
- all surviving players have left the valid encounter area;
- the encounter becomes unrecoverably invalid.

On reset, the encounter should restore its defined initial or checkpoint state.

Exact checkpoint and dungeon recovery rules belong to the Dungeon and Group Content PDD.

---

## 40. Behaviour and Content Authoring

Designers should be able to configure ordinary AI without writing C#.

Authoring should support, directly or through referenced definitions:

- behaviour archetype;
- perception parameters;
- preferred combat range;
- target-selection rules;
- ability-selection rules;
- assist behaviour;
- flee/surrender behaviour;
- readiness-state responses;
- patrol/roam mode;
- encounter references.

The editor should make inherited/default behaviour versus local overrides clear where definitions are reused.

---

## 41. Encounter-Area Authoring

A world designer should be able to author an Encounter Area in Unity.

An Encounter Area should be able to reference:

- spatial bounds or membership rules;
- participating NPC groups/spawns;
- default readiness;
- readiness triggers;
- readiness recovery rules;
- optional shared encounter state;
- optional encounter definition.

The exact component/ScriptableObject structure belongs to technical architecture.

Encounter-area boundaries should be authorable independently from world-streaming tiles.

---

## 42. Scalability

Open-world AI must be designed with large NPC populations in mind.

Not every NPC requires full-rate expensive decision-making at all times.

The architecture may use techniques such as:

- lower-frequency decisions;
- simplified distant simulation;
- sleeping/inactive behaviour;
- relevance-based updates;
- encounter-area activation.

Any optimisation must preserve server authority and produce believable results when players interact with the NPC.

Exact optimisation architecture belongs to technical design.

---

## 43. Current Implementation Baseline

The current repository already provides relevant foundations:

- `NPCBehavior` contains a threat table;
- NPCs can reference a combat faction;
- NPCs currently expose aggro range and aggro delay;
- `Actor` supports authoritative target assignment;
- actors participate in server-owned combat and world systems;
- `NavMeshAgent` is already used by current NPC actors;
- NPC definitions/spawn authoring are being formalised separately by the NPC and Creature Design PDD.

Current values and component boundaries are implementation baseline, not automatically locked product design.

The completed system should migrate toward reusable behaviour and encounter definitions rather than accumulating per-prefab bespoke logic.

---

## 44. Intentionally Unspecified Values

The following values remain deliberately unspecified:

- default detection radius;
- detection-angle rules;
- exact stealth/detection formula;
- assist radius;
- linked-pull range where spatial;
- leash distance;
- reset delay;
- path-recovery timeout;
- readiness transition thresholds;
- readiness decay times;
- flee thresholds;
- surrender probabilities;
- healer priority weights;
- ability-selection weights;
- AI update frequency.

These should be configurable and selected through content design, balancing and performance testing.

---

## 45. Locked Design Decisions

The following decisions are locked by this PDD:

1. NPC Definition, Behaviour Definition and Encounter Definition are separate concepts.
2. Common NPC behaviour should be reusable and primarily data-driven.
3. AI decisions are server-authoritative.
4. Normal perception may use distance, line of sight, faction relationship, stealth and behaviour state.
5. Stealth modifies detection rather than providing universal binary invisibility.
6. Hostile NPCs may aggro through perception, hostile actions, assistance, linked groups or scripted triggers.
7. Threat is the default basis for combat target selection where applicable.
8. Behaviour/encounter rules may deliberately override normal highest-threat targeting.
9. Assistance must be bounded and must not create uncontrolled region-wide recursive pulls.
10. Explicit linked pulls are supported independently of ordinary assistance.
11. Stationary, patrol and bounded roaming behaviour are supported.
12. Melee, ranged, caster, healer and support behaviours may have distinct positioning/ability logic.
13. Ability selection may use priorities, conditions, cooldowns, target selectors and weights.
14. Fleeing is optional behaviour rather than universal behaviour.
15. Surrender is distinct from fleeing and death.
16. AI must respond coherently to crowd-control states.
17. NPCs must recover from unreachable targets rather than remain permanently stuck.
18. NPCs normally have bounded pursuit/home/reset behaviour.
19. Structured encounter resets should be deterministic.
20. The open world may contain authored Encounter Areas with shared runtime state.
21. Encounter Areas support Normal, Alert and Fearful readiness levels.
22. Alert increases cooperative/defensive readiness and generally reduces fleeing/surrender.
23. Fearful generally increases self-preservation behaviours such as fleeing, surrendering, hiding or crouching.
24. Hiding/crouching may initially be visual-only behaviour.
25. Individual behaviour definitions decide how a unit reacts to encounter-area readiness.
26. Some NPCs, including appropriate constructs, undead, fanatics or bosses, may ignore fear-related responses.
27. Fearful is not merely a numerically stronger Alert state.
28. Readiness may move directly between appropriate states according to authored events.
29. Readiness may recover toward Alert or Normal when danger passes.
30. Generic boss/encounter phase transitions are supported by reusable encounter state.
31. Individual boss mechanics remain content-specific.
32. Ordinary behaviour must be authorable without bespoke C# for each NPC.
33. AI scalability may use reduced/simplified simulation where players are not actively interacting with the NPC.

---

## 46. Dependencies

This PDD depends on or constrains:

- **NPC and Creature Design PDD** — NPC identity, faction, difficulty, behaviour references, groups and home-area authoring;
- **Combat System PDD** — threat generation, combat state, crowd control and combat resolution;
- **World and Zone Design PDD** — encounter-area placement, terrain, settlements, wilderness and NPC-free roleplaying spaces;
- **World Runtime and Instancing PDD** — actor visibility, streamed regions and server simulation context;
- **Movement and Traversal PDD** — navigation and movement-affecting effects;
- **Dungeon and Group Content PDD** — structured encounters, bosses, wipes and checkpoints;
- **Open-World Events PDD** — event-driven changes to encounter areas and world populations;
- **Factions and Reputation PDD** — relationship changes affecting hostility/detection;
- **Quest, Narrative and Dialogue PDD** — scripted behavioural changes and surrender/dialogue outcomes.

---

## 47. Validation Criteria

The system satisfies this PDD when:

1. An NPC can use a reusable Behaviour Definition independent of its creature definition.
2. A particular encounter can modify behaviour without changing the underlying creature identity.
3. Hostile NPCs can detect appropriate targets through configured perception.
4. Solid terrain can block ordinary detection and relevant targeted actions.
5. Stealthed players are not automatically detected merely because their server position is known.
6. Damage/hostile actions can cause appropriate aggro.
7. NPCs can assist nearby allies under bounded rules.
8. Designers can explicitly link a group so one pull engages the intended pack.
9. Threat-based NPCs normally target the highest-threat valid target.
10. Abilities can intentionally select targets through non-threat selectors.
11. Patrol, stationary and roaming NPCs can be authored.
12. Melee NPCs can maintain usable melee positioning without constant pointless movement.
13. Ranged NPCs can maintain an authored preferred range.
14. Casters can choose valid abilities based on state rather than requiring a fixed rotation.
15. Healers/support NPCs can evaluate multiple allies through configurable priorities.
16. NPCs can flee when their behaviour requires it.
17. NPCs can surrender independently of fleeing/death.
18. Crowd control cannot leave an NPC permanently stuck in an invalid state.
19. An unreachable target causes recovery, retargeting or reset rather than permanent wall-running.
20. NPCs disengage/reset when no valid encounter targets remain.
21. Reset clears inappropriate combat state and returns the NPC/encounter to a valid authored state.
22. An open-world Encounter Area can share readiness state across multiple NPCs.
23. A Normal area uses ordinary behaviour.
24. An Alert area can increase assistance/chained-pull tendency and reduce fleeing/surrender.
25. A Fearful area can cause applicable NPCs to flee, surrender, hide, crouch or retreat.
26. Different NPC archetypes in the same Fearful area can respond differently.
27. Fear-immune/indifferent NPCs can ignore Fearful behaviour.
28. Readiness can transition based on authored events such as alarms, player detection, losses or reinforcements.
29. Readiness can recover when the threat passes.
30. Hiding/crouching can be presented visually even before a mechanical concealment system exists.
31. Boss encounters can transition phases from generic conditions such as health, time or actor state.
32. Encounter scripting can temporarily override ordinary AI and then return the NPC to a valid state.
33. A structured encounter can detect a wipe and reset coherently.
34. Designers can configure ordinary behaviour and Encounter Areas without writing bespoke gameplay code.
35. Server authority is preserved for AI targets, decisions, movement intent and encounter state.
