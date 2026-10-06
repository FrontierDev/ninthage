# Ninth Age — MMORPG Master Product Design Document

**Status:** Authoritative design index and project-level design baseline  
**Project:** Ninth Age  
**Scope:** Product-wide MMORPG design, subsystem ownership, PDD hierarchy, cross-system principles and unresolved product decisions  
**Last updated:** 2026-10-06

---

## 1. Purpose and Authority

This document is the master Product Design Document for **Ninth Age**.

Its purpose is not to contain the complete design of every game system. Instead, it:

- defines the project-wide design foundations that apply across the MMORPG;
- divides the game into explicit design domains;
- identifies the PDD responsible for each domain;
- records which subsystem PDDs already exist and which still need to be written;
- defines how design authority is resolved when multiple documents interact;
- prevents major systems from being implemented from assumptions that have never been explicitly designed;
- provides a single starting point for future design and implementation work.

Detailed rules belong in the relevant subsystem PDD.

Where a dedicated PDD exists only as a placeholder, the system must still be treated as **requiring further design**. The presence of a placeholder PDD in this index does not by itself approve any proposed or conventional MMORPG feature within that category.

No implementation should silently invent permanent product behaviour for an unresolved design decision.

---

## 2. Document Hierarchy

Ninth Age separates product design, technical architecture and implementation planning.

### 2.1 Master Product Design Document

This document is authoritative for:

- project-level design pillars;
- the boundaries between major game systems;
- the ownership of design decisions by subsystem PDDs;
- cross-system expectations;
- the list of required design documents;
- project-wide unresolved questions.

It should remain relatively stable and should not become a dumping ground for subsystem detail.

### 2.2 Subsystem Product Design Documents

Subsystem PDDs are authoritative within their stated scope.

Examples include:

- class design;
- combat;
- crafting;
- items and loot;
- world design;
- quests and narrative;
- dungeons;
- social systems.

If a detailed rule belongs to one of these systems, it should be defined in that system's PDD rather than duplicated here.

### 2.3 Technical Architecture Documents

Technical documents describe or prescribe how the game is implemented.

Current technical references include:

- [C# Architecture](../CSharp-Architecture.md)
- [C# Style Conventions](../CSharp-Style-Conventions.md)

These documents do not replace game-design decisions.

A technical limitation that materially affects product design must be surfaced back into the relevant PDD rather than silently changing player-facing behaviour.

### 2.4 Implementation Plans

Implementation plans describe how approved design and architecture will be turned into code or content.

They are not design authority.

The current example is:

- [C# Restyling Plan](CSharp-Restyling-Plan.md)

Implementation plans may sequence work, identify files and define validation, but should not create new game rules unless the relevant PDD is explicitly updated.

---

## 3. Conflict and Ownership Rules

When documents overlap, the following rules apply:

1. **This Master PDD owns project-wide principles and system boundaries.**
2. **The relevant subsystem PDD owns detailed player-facing behaviour inside its scope.**
3. **Technical architecture documents own implementation architecture, not product rules.**
4. **Implementation plans own sequencing and execution, not product rules.**
5. If two subsystem PDDs conflict, the conflict must be resolved explicitly rather than choosing whichever document was implemented first.
6. If a subsystem PDD conflicts with a project-wide principle in this document, both documents must be reviewed and intentionally reconciled.
7. Open design questions must remain visibly open until resolved.

---

## 4. Project-Level Product Foundations

The following principles form the current high-level product baseline.

### 4.1 MMORPG Structure

Ninth Age is a persistent multiplayer role-playing game built around:

- player characters;
- distinct playable classes;
- long-term character progression;
- a shared game world;
- cooperative and social play;
- combat against world and dungeon content;
- equipment and item progression;
- gathering and crafting;
- persistent player and world state.

The project may use familiar MMORPG conventions where they are useful, but individual systems should have a deliberate Ninth Age identity rather than existing only because another MMORPG uses them.

### 4.2 Large Seamless World

The world is intended to feel large, continuous and explorable.

The player-facing world should not feel like a disconnected collection of level-select maps.

Dungeons and other contained content should, where appropriate, feel geographically and fictionally connected to the wider world even if their runtime implementation requires separate scenes, instances or streamed content.

Detailed world structure belongs in the **World and Zone Design PDD** and **Dungeon and Group Content PDD**.

### 4.3 Persistent Client–Server Game

The game uses a graphical client and a headless authoritative server.

The exact technical model is defined by the C# architecture documentation. Product PDDs must nevertheless identify:

- which gameplay state persists;
- what players are allowed to predict locally;
- what interactions require authoritative validation;
- what behaviour must remain consistent in multiplayer.

Product design should not assume that important world or combat outcomes can exist only on a client.

### 4.4 Visual Direction

The authoritative visual direction is defined by:

- [Graphical Approach PDD](../Graphical-Approach-PDD.md)

The central visual principle is:

> **The authored world is mid-poly. The presentation of that world is high-fidelity.**

The game uses a grounded, mature fantasy presentation with modern lighting, atmosphere and effects while keeping world assets economical enough for a large streamed MMORPG environment.

The world also requires a day–night cycle.

### 4.5 Systemic Depth Without Needless Friction

Ninth Age should support long-term depth, mastery and player specialisation.

Complexity is justified when it creates:

- meaningful decisions;
- different player strategies;
- social interaction;
- mastery;
- world identity;
- long-term progression.

Complexity should not exist merely to create inconvenience, excessive maintenance or opaque rules.

### 4.6 Social Systems Should Create Meaningful Cooperation

Where systems require or reward cooperation, players should have something meaningful to contribute.

The Crafting PDD already establishes this principle for cooperative crafting. The same philosophy should be considered when designing:

- group combat;
- guild activities;
- world events;
- gathering;
- dungeons;
- raids;
- social progression.

Forced proximity without meaningful participation is not sufficient social gameplay.

### 4.7 Open Decisions Must Stay Open

Subsystem PDDs should distinguish between:

- **Locked design decisions**
- **Provisional design direction**
- **Open design decisions**

Unresolved details must not quietly become de facto design because a temporary implementation happened to ship first.

---

## 5. Master PDD Map

The intended product-document structure is:

```text
MMORPG Master PDD
│
├── Character and Combat
│   ├── Class Design PDD                         [EXISTS]
│   ├── Combat System PDD                       [EXISTS]
│   ├── Character Stats and Progression PDD     [EXISTS]
│   ├── Abilities and Talents PDD               [EXISTS]
│   └── Items, Equipment and Loot PDD            [EXISTS]
│
├── World and PvE Content
│   ├── World and Zone Design PDD               [EXISTS]
│   ├── Movement and Traversal PDD               [EXISTS]
│   ├── NPC and Creature Design PDD              [EXISTS]
│   ├── AI and Encounter Behaviour PDD           [EXISTS]
│   ├── Quest, Narrative and Dialogue PDD        [EXISTS]
│   ├── Dungeon and Group Content PDD            [EXISTS]
│   ├── Open-World Events PDD                    [EXISTS]
│   └── Factions and Reputation PDD              [EXISTS]
│
├── Economy and Professions
│   ├── Crafting System PDD                      [EXISTS]
│   ├── Items, Equipment and Loot PDD            [EXISTS / SHARED DEPENDENCY]
│   └── Economy, Trade and Markets PDD           [EXISTS]
│
├── Multiplayer and Social
│   ├── Group and Raid Systems PDD               [EXISTS]
│   ├── Guild and Social Systems PDD             [EXISTS]
│   ├── Communication Systems PDD                [EXISTS]
│   └── PvP PDD                                  [EXISTS]
│
├── Player Experience
│   ├── UI and UX PDD                            [EXISTS]
│   ├── Character Creation and Identity PDD      [EXISTS]
│   ├── Audio and Music PDD                      [PLACEHOLDER]
│   └── Accessibility and Input PDD              [EXISTS]
│
├── Presentation
│   └── Graphical Approach PDD                   [EXISTS]
│
└── Technical/Product Boundary
    ├── Account, Character and Persistence PDD   [EXISTS]
    ├── World Runtime and Instancing PDD         [EXISTS]
    └── Live Content and Versioning PDD           [PLACEHOLDER]
```

The status labels above describe documentation state, not implementation state.

---

# 6. Character and Combat PDDs

## 6.1 Class Design PDD — Existing

**Document:**

- [Class Design PDD](Class-Design-PDD.md)

This document owns:

- playable class roster;
- class fantasies;
- class power sources;
- proposed specialisations;
- broad role identities;
- class-specific mechanic direction;
- unresolved class-design questions.

It does **not** own the complete combat system, universal stat model, itemisation or final talent architecture.

Several class mechanics remain intentionally unresolved and must not be implemented as final systems solely from the current summary.

---

## 6.2 Combat System PDD — Existing

**Document:**

- [Combat System PDD](Combat-System-PDD.md)

This should define the fundamental moment-to-moment combat model.

It should cover at minimum:

- targeting model;
- attack model;
- action and ability execution;
- global and local cooldown philosophy;
- cast times and channels;
- melee and ranged attack rules;
- movement during combat;
- interruption;
- crowd control;
- damage and healing resolution;
- avoidance and mitigation;
- critical effects;
- threat and aggro;
- resource interaction at the combat-system level;
- death, defeat, resurrection and recovery;
- combat state entry and exit;
- player-versus-NPC expectations;
- group-combat readability;
- server authority and client responsiveness requirements;
- encounter pacing targets.

### Further consideration required

The project still needs explicit decisions on the exact baseline combat model before individual class kits become implementation contracts.

---

## 6.3 Character Stats and Progression PDD — Existing

**Document:**

- [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md)

This document defines:

- level 1–60 character progression;
- the permanent level-60 cap;
- the RPE2-derived initial + (level - 1) × per-level progression architecture;
- race and class stat/resource progression;
- Strength, Dexterity, Stamina, Intelligence and Willpower;
- multi-source derived statistics;
- intrinsic versus effective resource maxima;
- Health/Mana hybrid progression;
- level-relative combat ratings with permanent level-60 reference;
- weapon-skill progression;
- talent-point entitlement from level 10 through 60;
- maximum-level progression without further character levels;
- account-wide Legacy progression constraints.

Exact numerical class endpoints, XP pacing, rating coefficients and Legacy perk values remain balance data.

---

## 6.4 Abilities and Talents PDD — Existing

**Document:**

- [Abilities and Talents PDD](Abilities-and-Talents-PDD.md)

This document defines:

- class-trainer acquisition of class-wide abilities;
- active specialisation selection at level 10;
- free talent investment across all three specialisation trees;
- points-spent-in-tree depth gating;
- 1/2/3/5-rank talents;
- passive and active talent abilities;
- specialisation-specific known-ability ownership;
- respecialisation availability and post-level-20 fee requirement;
- spell/ability ranks that improve base effects before stat scaling;
- unrestricted traditional action-bar binding;
- server-authoritative talent, specialisation and learned-ability validation.

Class-specific talent trees and ability lists may receive subordinate content documents.

---

## 6.5 Items, Equipment and Loot PDD — Existing

**Document:**

- [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md)

This should define:

- item categories;
- equipment slots;
- weapon categories;
- armour categories;
- class/equipment relationships;
- item quality tiers;
- stat allocation;
- item level or equivalent power model;
- loot generation;
- deterministic versus random properties;
- bind rules;
- durability, repair or their absence;
- unique/equipped restrictions;
- consumables;
- containers;
- loot ownership and group-loot rules;
- dropped versus crafted item relationships.

This PDD must be reconciled directly with the Crafting PDD.

---

# 7. World and PvE Content PDDs

## 7.1 World and Zone Design PDD — Existing

**Document:**

- [World and Zone Design PDD](World-and-Zone-Design-PDD.md)

This should define the player-facing structure of the world rather than the technical streaming implementation.

It should cover:

- continents, regions and zones;
- zone identity;
- open-world continuity;
- settlement structure;
- wilderness structure;
- biome design;
- points of interest;
- player navigation;
- world density;
- level/progression relationships between zones;
- dangerous and safe areas;
- verticality;
- exploration rewards;
- environmental storytelling;
- relationship between open world and dungeon spaces.

The visual implementation must remain compatible with the Graphical Approach PDD.

---

## 7.2 Movement and Traversal PDD — Existing

**Document:**

- [Movement and Traversal PDD](Movement-and-Traversal-PDD.md)

This should define:

- baseline movement;
- sprinting or equivalent;
- jumping;
- falling;
- swimming;
- climbing if supported;
- traversal abilities;
- mounted travel if supported;
- fast travel;
- transport networks;
- movement restrictions in combat;
- movement-affecting status effects;
- world-boundary behaviour;
- death-related travel if applicable.

It should specify desired player experience independently of the technical movement-validation implementation.

---

## 7.3 NPC and Creature Design PDD — Existing

**Document:**

- [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md)

This should define:

- creature categories;
- friendly, neutral and hostile NPC behaviour expectations;
- creature families;
- ranks or difficulty classifications;
- NPC statistics;
- spawning philosophy;
- leash and reset behaviour;
- interaction rules;
- merchants and service NPCs;
- named and rare creatures;
- bosses;
- creature rewards;
- ambient world populations.

This document owns what NPCs and creatures are intended to be.

The AI PDD owns how behaviour is structured.

---

## 7.4 AI and Encounter Behaviour PDD — Existing

**Document:**

- [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md)

This should define:

- aggro and perception;
- threat response;
- combat decision-making;
- ability selection;
- movement and positioning;
- group behaviours;
- retreat/reset logic;
- encounter phases;
- boss-mechanic principles;
- navigation requirements;
- server simulation expectations;
- scalability for large numbers of world actors.

The exact AI implementation belongs in technical architecture documentation.

---

## 7.5 Quest, Narrative and Dialogue PDD — Existing

**Document:**

- [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md)

This should define:

- narrative delivery philosophy;
- quest structure;
- quest acquisition;
- objectives;
- quest tracking;
- branching;
- dialogue;
- rewards;
- repeatable content;
- world-state consequences;
- party participation;
- shared credit;
- narrative persistence;
- use of cutscenes or in-world presentation;
- integration with the existing dialogue-authoring tooling.

---

## 7.6 Dungeon and Group Content PDD — Existing

**Document:**

- [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md)

This should define:

- what constitutes a dungeon;
- group-size expectations;
- dungeon length and pacing;
- entrances and relationship to the open world;
- instancing philosophy;
- reset rules;
- encounter structure;
- boss expectations;
- trash/combat density;
- checkpoints;
- rewards;
- lockouts;
- difficulty modes if any;
- role expectations;
- failure and recovery.

A key project goal is that dungeons should feel as though they belong to the world rather than existing only as detached menu content.

The technical scene/instance implementation belongs in the World Runtime and Instancing PDD.

---

## 7.7 Open-World Events PDD — Existing

**Document:**

- [Open-World Events PDD](Open-World-Events-PDD.md)

Potential scope includes:

- dynamic events;
- public objectives;
- world bosses;
- participation and contribution;
- scaling;
- event chains;
- failure states;
- rewards;
- event persistence.

The exact feature set remains open.

---

## 7.8 Factions and Reputation PDD — Existing

**Document:**

- [Factions and Reputation PDD](Factions-and-Reputation-PDD.md)

Potential scope includes:

- world factions;
- reputation progression;
- hostility;
- rewards;
- faction services;
- faction narrative;
- account versus character progression.

The exact role of reputation in Ninth Age remains to be designed.

---

# 8. Economy and Profession PDDs

## 8.1 Crafting System PDD — Existing

**Document:**

- [Crafting System PDD](Crafting-System-PDD.md)

This document already owns:

- gathering skills;
- crafting professions;
- profession-slot rules;
- recipe progression;
- exact-recipe mastery;
- Standard, Superior, Masterwork and Legendary crafting;
- binding rules associated with crafted items and components;
- cooperative crafting;
- social gathering principles;
- related crafting UX.

Its locked design decisions should not be redefined by future economy or item PDDs without an explicit reconciliation.

---

## 8.2 Economy, Trade and Markets PDD — Existing

**Document:**

- [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md)

This should define:

- currencies;
- player-to-player trade;
- market or auction systems if present;
- vendor economy;
- item sinks;
- currency sinks;
- repair or service costs where applicable;
- price discovery;
- trade restrictions;
- crafted-item market participation;
- bind-rule economic effects;
- anti-abuse economic rules at a product level.

It must be designed jointly with Crafting and Items/Loot.

---

# 9. Multiplayer and Social PDDs

## 9.1 Group and Raid Systems PDD — Existing

**Document:**

- [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md)

This should define:

- party size;
- raid size if raids exist;
- party leadership;
- invitations;
- group roles;
- group visibility;
- loot interaction;
- shared quest credit;
- ready checks;
- markers;
- group persistence;
- group formation;
- matchmaking or group-finder systems if supported.

---

## 9.2 Guild and Social Systems PDD — Existing

**Document:**

- [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md)

This document defines:

- global cross-world guild identity and stable GuildIDs;
- character-owned guild membership;
- custom ranks and granular permissions;
- guild storage, treasury and auditability;
- no mandatory guild-level XP treadmill or passive combat-stat bonuses;
- world-specific rented guild halls;
- authored settlement properties ranging from camps/houses to manors/fortified compounds;
- weekly rent, recoverable investment, arrears and two-missed-payment eviction;
- dedicated hall-rent funding;
- optional transparent guild tariffs on eligible economic activity;
- guild halls as gathering/trophy spaces rather than replacements for public crafting, banking and market services;
- guild trophy persistence;
- friend/social-presence direction;
- server-enforced ignore/block relationships.

Exact membership caps, founding costs, rank counts, bank sizes, tariff limits and rent/refund percentages remain provisional balancing values.

---

## 9.3 Communication Systems PDD — Existing

**Document:**

- [Communication Systems PDD](Communication-Systems-PDD.md)

This document defines:

- proximity Say, Yell and free-text emotes;
- `/e` and `/me` as aliases;
- cross-world online whispers;
- party/raid/guild communication routing;
- permission-controlled private guild chat;
- world-specific General, Trade and Looking for Group channels;
- cross-world custom player channels;
- normal cross-faction communication;
- account-level mutual friends;
- account-level server-enforced blocks;
- rich item/ability/quest/player/guild/location links;
- local finite chat history;
- optional client profanity filtering;
- server-side spam/flood protection;
- reporting with authoritative message context;
- no built-in voice-chat requirement for the initial system.

---

## 9.4 PvP PDD — Existing

**Document:**

- [PvP PDD](PvP-PDD.md)

This document defines:

- two-faction open-world PvP;
- Normal worlds with opt-in PvP;
- PvP worlds with permanent open-world PvP flagging;
- no low-level or city PvP immunity on PvP worlds;
- cross-faction PvE grouping and temporary group friendliness;
- Free-For-All PvP outside party/raid and guild relationships;
- consensual duels;
- contest-zone world PvP objectives;
- runtime support for future battlegrounds and arenas;
- cross-world structured-PvP support;
- seasonal 14-rank PvP progression;
- non-decaying seasonal Rank Points and separate spendable Honor;
- honorable-kill and repeat-kill reward validation;
- PvP death/durability expectations;
- explicit PvP-specific balance modifiers where needed.

Exact rank titles, season length, Honor/Rank Point curves and individual PvP objectives remain balance/content data.

---

# 10. Player Experience PDDs

## 10.1 UI and UX PDD — Existing

**Document:**

- [UI and UX PDD](UI-and-UX-PDD.md)

This document defines:

- Radiant Slate as the authoritative UI design system;
- shared typography, colour, spacing, icon and component-size tokens;
- shader-first UI presentation using the existing ArcaneSlate family;
- Manrope as the functional UI typeface;
- shared interaction states and Radiant Gold focus/selection;
- HUD Edit Mode and discrete module scaling;
- traditional multi-bar 12-slot action bars;
- low-profile player/target frames, nameplates, auras and cast bars;
- floating combat text and combat-log presentation;
- inventory/equipment slot sizing and drag/drop conventions;
- tooltip and item-comparison presentation;
- character, talent, quest and reputation window conventions;
- minimap/world-map presentation;
- contextual non-blocking tips;
- LIFO window/Escape behaviour;
- local UI settings and persistence expectations.

---

## 10.2 Character Creation and Identity PDD — Existing

**Document:**

- [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md)

This document defines:

- Race → Class → Appearance → Name → Review/Create flow;
- Level-1 starts with no active specialisation or talent allocation;
- all playable races able to select all playable classes by default, with explicit exceptions only;
- race-derived faction, starting reputation and starting region;
- minimal Level-1 Standard starter equipment and 0 starting gold;
- free initial Level-1 class abilities learned through class trainers;
- no tutorial island, tutorial mode or mandatory handholding sequence;
- contextual non-blocking UI tips for onboarding;
- cosmetic-only sex/body/height/build/face/hair and race-specific appearance;
- required FirstName and LastName, each 2–12 characters, with globally unique normalized full-name pairs;
- post-creation appearance changes and renaming;
- no ordinary race-change or class-change service;
- 12-slot character-selection and deleted-character restoration presentation.

---

## 10.3 Audio and Music PDD — Placeholder

**Document:**

- [Audio and Music PDD](Audio-and-Music-PDD.md)

This should eventually define:

- world ambience;
- combat audio;
- ability readability;
- UI audio;
- music structure;
- zone music;
- dungeon music;
- day/night response;
- voice presentation where applicable;
- performance and concurrency expectations.

---

## 10.4 Accessibility and Input PDD — Existing

**Document:**

- [Accessibility and Input PDD](Accessibility-and-Input-PDD.md)

This document defines:

- keyboard/mouse as the reference scheme with semantic Input Actions;
- runtime remapping, multiple bindings, modifier bindings and mouse-button bindings;
- keyboard-only UI navigation and visible focus;
- architecture for complete controller operation;
- input-device switching and controller sensitivity/dead-zone requirements;
- camera sensitivity, inversion, recentering, shake, bob/sway and motion-blur controls;
- 75–200% global UI scaling;
- independent text scaling to 200%;
- chat/tooltip readability options;
- subtitles and non-dialogue closed captions;
- non-colour cues and semantic-colour accessibility;
- flashing/VFX/screen-effect reduction;
- Floating Combat Text accessibility;
- independent accessibility-relevant audio categories;
- account/client-level accessibility/input persistence;
- refactoring current direct keyboard/mouse polling into remappable semantic actions.

---

# 11. Presentation PDD

## 11.1 Graphical Approach PDD — Existing

**Document:**

- [Graphical Approach PDD](../Graphical-Approach-PDD.md)

This is the authoritative source for:

- visual identity;
- fidelity targets;
- terrain presentation;
- materials;
- modular environment art;
- lighting;
- atmosphere;
- weather;
- day–night presentation;
- VFX;
- graphical performance philosophy;
- UI visual cohesion where relevant.

Other PDDs should reference it rather than invent independent visual styles.

---

# 12. Technical/Product Boundary PDDs

These documents are needed because several systems contain both player-facing rules and significant server/runtime constraints.

## 12.1 Account, Character and Persistence PDD — Existing

**Document:**

- [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md)

This document defines:

- immutable AccountID and CharacterID identity;
- 12 character slots per account;
- account-owned HomeWorldID and Legacy state;
- globally unique two-part character names;
- one-account/one-session authority;
- login/logout and 30-second disconnect-grace behaviour;
- combat/damage restrictions on logout and world transfer;
- persistence-neutral Health/resource/cooldown/aura semantics;
- 30-day soft character deletion and restoration;
- transactional ownership/progression persistence;
- migration away from whole-file JSON storage;
- relational SQL persistence with PostgreSQL / EF-Core-style architecture based on the `FrontierDev/webrpgapp` reference;
- schema migration, integrity and auditability requirements.

---

## 12.2 World Runtime and Instancing PDD — Existing

**Document:**

- [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md)

This should define the product requirements that sit between world design and technical scene streaming:

- what players perceive as one continuous world;
- world partition expectations;
- dungeon/instance boundaries;
- who shares a world instance;
- transitions;
- persistence across transitions;
- group behaviour across instances;
- respawn/reset behaviour;
- population-capacity expectations at the design level.

Technical chunk loading, Addressables, additive scenes, interest management and server scene management remain architecture concerns.

---

## 12.3 Live Content and Versioning PDD — Placeholder

**Document:**

- [Live Content and Versioning PDD](Live-Content-and-Versioning-PDD.md)

This should eventually define:

- compatibility expectations between client and server versions;
- content-data versioning;
- migration of persistent data;
- patch behaviour;
- scheduled content changes;
- live-event content if supported;
- rollback expectations at the product level.

This should be written before live persistent player data makes incompatible design changes expensive.

---

# 13. Cross-System Dependencies

The game systems are not independent.

The following dependencies should be treated as particularly important.

| System | Major Dependencies |
|---|---|
| Classes | Combat, Stats, Abilities/Talents, Items |
| Combat | Stats, Classes, Items, NPCs, AI, UI |
| Stats/Progression | Classes, Items, Combat, PvE progression |
| Abilities/Talents | Classes, Combat, Stats, UI |
| Items/Loot | Stats, Combat, Classes, Crafting, Economy |
| Crafting | Items/Loot, Economy, Gathering/world resources |
| World/Zones | Graphics, Traversal, Quests, NPCs, Events |
| Dungeons | Combat, Classes, AI, Groups, Loot, World Runtime |
| NPCs | Stats, Combat, AI, Quests, Economy |
| Quests/Narrative | World, NPCs, Groups, Persistence |
| Economy | Crafting, Items, Vendors, Persistence |
| Groups/Raids | Combat, Dungeons, UI, Social |
| Guilds/Social | Economy, World Runtime, World/Zones, UI, Communication, Persistence |
| Communication | Guilds/Social, Groups/Raids, World Runtime, UI, Persistence |
| PvP | Combat, Groups/Raids, Factions, World/Zones, World Runtime, Items, UI, Persistence |
| UI/UX | Nearly all player-facing systems |
| Accessibility/Input | UI/UX, Combat, Abilities, Communication, Audio, Graphics, Client Settings |
| Persistence | Character progression, Items, Quests, Crafting, Social |
| World Runtime | World design, Groups, Dungeons, Persistence |

A change to a foundational PDD should therefore include a review of its dependent documents.

---

# 14. Required Structure for Future Subsystem PDDs

Future PDDs should use a consistent structure where applicable.

Each PDD should contain:

## 14.1 Purpose and Authority

State:

- what the document owns;
- what it does not own;
- whether it is authoritative, provisional or exploratory.

## 14.2 Design Pillars

Define the principles by which future detailed decisions should be judged.

## 14.3 Locked Design Decisions

Separate decisions that implementation should treat as requirements.

## 14.4 Open Design Decisions

Explicitly list unresolved questions.

Open questions should not be hidden inside general prose.

## 14.5 Core Player Loop

Describe what the player repeatedly does and why.

## 14.6 Rules and State

Define the player-facing system state and the rules that transform it.

## 14.7 Progression

Where applicable, describe:

- acquisition;
- advancement;
- mastery;
- reset/respec behaviour;
- long-term progression.

## 14.8 Multiplayer Behaviour

Define:

- shared state;
- ownership;
- group interaction;
- competition/cooperation;
- disconnect and rejoin expectations where relevant.

## 14.9 Persistence

Identify what must survive:

- logout;
- disconnect;
- server restart;
- content update.

## 14.10 UX Requirements

State what information the player must be able to understand and control.

## 14.11 Content Authoring Requirements

Where relevant, describe what designers need to author:

- definitions;
- assets;
- encounters;
- recipes;
- quests;
- dialogue;
- zones;
- loot tables.

## 14.12 Technical Constraints

Record only technical constraints that materially affect the product design.

Detailed code architecture belongs elsewhere.

## 14.13 Dependencies

Link directly to other PDDs whose rules the system relies on.

## 14.14 Validation Criteria

State the conditions under which the design can be considered correctly implemented.

---

# 15. Naming and File Conventions

Product design documents should use:

```text
.docs/Plans/<System-Name>-PDD.md
```

Examples:

```text
.docs/Plans/Combat-System-PDD.md
.docs/Plans/Items-Equipment-and-Loot-PDD.md
.docs/Plans/World-and-Zone-Design-PDD.md
```

Exceptions may remain outside `.docs/Plans` where already established, such as the existing Graphical Approach PDD.

The Master PDD should link to the canonical document rather than requiring files to be moved solely for directory consistency.

---

# 16. Current Documentation Status

## 16.1 Existing Product Design Documents

The project currently has the following major product-design documents:

| PDD | Status | Scope |
|---|---|---|
| [Graphical Approach PDD](../Graphical-Approach-PDD.md) | Authoritative | Visual identity and graphical presentation |
| [Crafting System PDD](Crafting-System-PDD.md) | Authoritative | Gathering, professions, mastery and cooperative crafting |
| [Class Design PDD](Class-Design-PDD.md) | Draft / design baseline | Class roster, identity and unresolved class mechanics |
| **MMORPG Master PDD** | Authoritative index / baseline | Product-wide design hierarchy and subsystem map |
| [Combat System PDD](Combat-System-PDD.md) | Authoritative | Core combat model, combat state, targeting, threat and combat resolution |
| [World Runtime and Instancing PDD](World-Runtime-and-Instancing-PDD.md) | Authoritative | Persistent worlds, streaming, floating origin, actor visibility and runtime instances |
| [Movement and Traversal PDD](Movement-and-Traversal-PDD.md) | Authoritative | Ground movement, traversal, mounts, transport and lodestone travel |
| [World and Zone Design PDD](World-and-Zone-Design-PDD.md) | Authoritative | World structure, zones, routes, wilderness, exploration and geography |
| [NPC and Creature Design PDD](NPC-and-Creature-Design-PDD.md) | Authoritative | NPC taxonomy, factions, populations, spawning and reusable creature authoring |
| [AI and Encounter Behaviour PDD](AI-and-Encounter-Behaviour-PDD.md) | Authoritative | NPC perception, threat, behaviour, encounter state and open-world readiness |

## 16.2 Placeholder Subsystem PDDs

The following subsystem documents now exist as placeholders. Their presence reserves design ownership and file location; it does **not** mean their systems are designed or approved.

| PDD | Status | Intended scope |
|---|---|---|
| [Character Stats and Progression PDD](Character-Stats-and-Progression-PDD.md) | Authoritative | Character levels, experience, attributes, derived statistics, scaling, progression pacing and endgame character progression. |
| [Items, Equipment and Loot PDD](Items-Equipment-and-Loot-PDD.md) | Authoritative | Item categories, equipment, weapons, armour, item power and quality, loot generation, binding and loot ownership. |
| [Abilities and Talents PDD](Abilities-and-Talents-PDD.md) | Authoritative | Ability acquisition and structure, specialisations, talents, loadouts, scaling, respecialisation and action-bar expectations. |
| [Quest, Narrative and Dialogue PDD](Quest-Narrative-and-Dialogue-PDD.md) | Authoritative | Narrative delivery, quests, objectives, dialogue, branching, rewards, shared credit, world-state consequences and narrative persistence. |
| [Dungeon and Group Content PDD](Dungeon-and-Group-Content-PDD.md) | Authoritative | Dungeon structure, group expectations, world integration, encounters, bosses, checkpoints, difficulty, rewards and lockouts. |
| [Open-World Events PDD](Open-World-Events-PDD.md) | Authoritative | Dynamic public events, world bosses, contribution, scaling, event chains, failure states, rewards and persistence. |
| [Factions and Reputation PDD](Factions-and-Reputation-PDD.md) | Authoritative | World factions, reputation, hostility, faction rewards and services, narrative relationships and account/character progression. |
| [Economy, Trade and Markets PDD](Economy-Trade-and-Markets-PDD.md) | Authoritative | Currencies, vendors, player trade, markets, economic sinks, restrictions and the relationship between crafting and the player economy. |
| [Group and Raid Systems PDD](Group-and-Raid-Systems-PDD.md) | Authoritative | Party and raid structure, leadership, invitations, roles, group visibility, shared credit, markers and group formation. |
| [Guild and Social Systems PDD](Guild-and-Social-Systems-PDD.md) | Authoritative | Global guild identity, membership, ranks/permissions, guild storage, world-specific rentable halls, rent/tariffs/trophies and persistent social relationships. |
| [Communication Systems PDD](Communication-Systems-PDD.md) | Authoritative | Spatial/public/social chat, whispers, group/guild/custom channels, account friends/blocks, rich links, spam protection, reporting and moderation context. |
| [PvP PDD](PvP-PDD.md) | Authoritative | Two-faction open-world PvP, Normal/PvP world rulesets, FFA, duels, contest objectives, structured-PvP support and seasonal ranks/Honor. |
| [UI and UX PDD](UI-and-UX-PDD.md) | Authoritative | Radiant Slate design tokens, shader-driven UI, HUD/action bars/frames, RPG windows, maps, tooltips, combat feedback, contextual tips and interaction conventions. |
| [Character Creation and Identity PDD](Character-Creation-and-Identity-PDD.md) | Authoritative | Race/class selection, appearance, two-part naming, starting state/location, contextual onboarding tips and identity-change services. |
| [Audio and Music PDD](Audio-and-Music-PDD.md) | Placeholder / design required | World ambience, combat and UI audio, music structure, zone and dungeon music, day/night response and voice presentation. |
| [Accessibility and Input PDD](Accessibility-and-Input-PDD.md) | Authoritative | Semantic/remappable input, keyboard/controller navigation, global/text scaling, subtitles/captions, colour/motion accessibility and camera/input options. |
| [Account, Character and Persistence PDD](Account-Character-and-Persistence-PDD.md) | Authoritative | Account/character ownership, session continuity, login/logout, disconnect recovery, state persistence and relational database requirements. |
| [Live Content and Versioning PDD](Live-Content-and-Versioning-PDD.md) | Placeholder / design required | Client/server compatibility, content-data versioning, persistent-data migration, patch behaviour, live content and rollback expectations. |

## 16.3 Existing Supporting Technical Documents

| Document | Role |
|---|---|
| [C# Architecture](../CSharp-Architecture.md) | Current code architecture reference |
| [C# Style Conventions](../CSharp-Style-Conventions.md) | C# style authority |
| [C# Restyling Plan](CSharp-Restyling-Plan.md) | Implementation/refactoring plan |

---

# 17. Highest-Priority Placeholder PDDs

All listed subsystem PDD files exist. The remaining placeholder PDDs should be filled in approximately this order:

1. **Audio and Music PDD**
2. **Live Content and Versioning PDD**

This order is a design-dependency recommendation, not an implementation roadmap.
---

# 18. Project-Level Further Consideration Required

The following major product decisions remain unresolved or are not yet captured by an authoritative PDD.

## 18.1 Combat

The project still needs a locked definition of:

- the baseline combat model;
- targeting;
- action economy;
- cooldown philosophy;
- threat;
- death and resurrection;
- expected encounter pacing.

## 18.2 Character Progression

The project still needs explicit decisions on:

- level structure;
- level cap;
- experience pacing;
- primary and secondary statistics;
- endgame progression;
- respecialisation.

## 18.3 Equipment and Loot

The relationship between:

- dropped items;
- crafted items;
- item quality;
- item power;
- bind rules;
- class equipment restrictions

must be defined.

## 18.4 World Structure

The project still needs a product-level definition of:

- world regions;
- zone progression;
- settlement density;
- exploration;
- fast travel;
- dungeon/world relationships.

## 18.5 Group Content

The project must define:

- party size;
- whether raids exist and at what scale;
- dungeon difficulty model;
- group formation;
- lockouts and reward cadence.

## 18.6 PvP

PvP is now formally defined as a two-faction world system with:

- Normal-world opt-in PvP;
- always-flagged PvP worlds without level/city immunity;
- FFA PvP;
- duels;
- contest-zone objectives;
- future battleground/arena support;
- seasonal rank/Honor progression;
- continued cross-faction PvE cooperation.

Exact PvP rank titles, progression curves, season length, structured-PvP content and balance tuning remain content/tuning decisions rather than unresolved architecture.

## 18.7 Social Structure

Guild identity, membership, ranks/permissions, world-specific guild halls and guild storage are defined by the Guild and Social Systems PDD.

Spatial/public/social chat, cross-world communication, account-level friends/blocks, spam protection and reporting are defined by the Communication Systems PDD.

Detailed presence/privacy option tuning may continue during Accessibility/UI implementation without requiring a redesign of the social architecture.

## 18.8 Endgame

The project does not yet have a master definition of what players do after completing the main character-level progression.

Endgame should eventually emerge coherently from:

- group PvE;
- class/character progression;
- crafting mastery;
- economy;
- world content;
- social systems;
- PvP if adopted.

It should not be designed as an isolated feature.

---

# 19. Definition of a Designed System

A system should not be considered ready for full implementation merely because:

- a prototype exists;
- data structures exist;
- another MMORPG has an equivalent feature;
- the feature is technically straightforward;
- an editor tool already exposes fields for it.

A major player-facing system is considered **design-ready** when:

1. its owning PDD exists;
2. its design pillars are clear;
3. its core player loop is defined;
4. important cross-system dependencies are resolved;
5. locked and open decisions are explicitly separated;
6. its multiplayer and persistence requirements are understood;
7. its required UX is identified;
8. implementation would not require developers to invent major product behaviour.

Prototypes may precede this state, but prototypes should remain explicitly provisional.

---

# 20. Master Design Goal

Ninth Age should become a coherent MMORPG rather than a collection of individually functional MMORPG systems.

Every major feature should answer three questions:

1. **What does this add to the player's experience?**
2. **How does it interact with the rest of the game?**
3. **Why is it designed this way in Ninth Age?**

The purpose of the PDD hierarchy is to keep those answers explicit as the project grows.

This Master PDD should be updated whenever:

- a major new product domain is approved;
- a subsystem PDD is created, renamed or retired;
- responsibility for a design decision moves between documents;
- a project-wide design principle changes.
